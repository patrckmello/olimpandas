using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in an isolated project copy with -batchmode -executeMethod
// FishingSmokeTest.Run (without -quit). Does not save scenes.
[InitializeOnLoad]
public static class FishingSmokeTest
{
    private const string Running = "Olimpandas.FishingSmokeTest";
    public const string ScenePath = "Assets/Scenes/Fishing.unity";

    static FishingSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                new GameObject("Fishing smoke test").AddComponent<FishingSmokeRunner>();
        };
    }

    public static void Run()
    {
        if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
            throw new Exception("Fishing is missing from Build Settings.");

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>()
            .OrderBy(p => p.PlayerId).ToArray();
        FishingManager manager = UnityEngine.Object.FindAnyObjectByType<FishingManager>();

        Check(players.Length == 2 && players[0].PlayerId == 1 && players[1].PlayerId == 2, "One P1 and one P2");
        Check(players[0].transform.position.x < 0f &&
            Mathf.Abs(players[0].transform.position.x + players[1].transform.position.x) < 0.01f,
            "P1 left, P2 right, same distance from the river");
        Check(manager != null, "FishingManager present");
        Check(UnityEngine.Object.FindObjectsByType<Hurdle>().Length == 0, "No hurdles");
        Check(UnityEngine.Object.FindObjectsByType<RaceCountdownController>().Length == 0,
            "Race countdown removed (manager runs its own)");

        var data = new SerializedObject(manager);
        SerializedProperty fishers = data.FindProperty("players");
        Check(fishers.arraySize == 2, "Two fishers");
        for (int i = 0; i < 2; i++)
        {
            FishingPlayer fisher = players[i].GetComponent<FishingPlayer>();
            Check(fisher != null && fishers.GetArrayElementAtIndex(i).objectReferenceValue == fisher, "Fisher order");
            var fields = new SerializedObject(fisher);
            foreach (string field in new[] { "rodTip", "bobber", "line" })
                Check(fields.FindProperty(field).objectReferenceValue != null, field + " on P" + (i + 1));
        }
        Check(Key(players[0], "upKey") == KeyCode.W && Key(players[0], "downKey") == KeyCode.S, "P1 arrow keys");
        Check(Key(players[1], "upKey") == KeyCode.UpArrow && Key(players[1], "downKey") == KeyCode.DownArrow,
            "P2 arrow keys");

        foreach (string field in new[] { "fishPrefab", "statusText", "scoreText", "resultScreen" })
            Check(data.FindProperty(field).objectReferenceValue != null, "Manager " + field);
        var result = (FinishLine)data.FindProperty("resultScreen").objectReferenceValue;
        Check(!result.GetComponent<Collider2D>().enabled, "Result screen without finish trigger");

        SerializedProperty kinds = data.FindProperty("kinds");
        int junk = 0;
        for (int i = 0; i < kinds.arraySize; i++)
        {
            SerializedProperty kind = kinds.GetArrayElementAtIndex(i);
            Check(kind.FindPropertyRelative("sprite").objectReferenceValue != null &&
                kind.FindPropertyRelative("sequenceLength").intValue > 0, "Fish kind " + i);
            if (kind.FindPropertyRelative("points").intValue < 0) junk++;
        }
        Check(kinds.arraySize - junk >= 3 && junk >= 1, "Three fish sizes or more, plus junk");
        var prefab = new SerializedObject((RiverFish)data.FindProperty("fishPrefab").objectReferenceValue);
        Check(prefab.FindProperty("arrowSprites").arraySize == 4, "One sprite per arrow");

        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    private static KeyCode Key(PlayerController player, string field) =>
        (KeyCode)new SerializedObject(player.GetComponent<FishingPlayer>()).FindProperty(field).intValue;

    public static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Check failed: " + message);
    }

    public static void Complete(bool success)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.Exit(success ? 0 : 1);
    }
}

public class FishingSmokeRunner : MonoBehaviour
{
    private bool failed;
    private FishingManager manager;
    private FishingPlayer p1;
    private FishingPlayer p2;
    private List<RiverFish> river;

    private void Awake() => Application.logMessageReceived += OnLog;
    private void OnDestroy() => Application.logMessageReceived -= OnLog;

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            failed = true;
    }

    private IEnumerator Start()
    {
        IEnumerator test = Exercise();
        while (true)
        {
            object current;
            try
            {
                if (!test.MoveNext()) break;
                current = test.Current;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                FishingSmokeTest.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("FishingSmokeTest " + (failed ? "FAILED (see errors)" : "PASSED"));
        FishingSmokeTest.Complete(!failed);
    }

    private IEnumerator Exercise()
    {
        manager = FindAnyObjectByType<FishingManager>();
        FishingPlayer[] players = FindObjectsByType<FishingPlayer>().OrderBy(p => p.PlayerId).ToArray();
        p1 = players[0];
        p2 = players[1];
        river = Field<List<RiverFish>>(manager, "river");
        FishKind[] kinds = Field<FishKind[]>(manager, "kinds");
        FishKind fishKind = kinds.First(k => k.points == 2);
        FishKind junk = kinds.First(k => k.IsJunk);
        var misses = new List<int>();
        var catches = new List<int>();
        manager.Missed += misses.Add;
        manager.Caught += (id, _) => catches.Add(id);

        yield return null;
        FishingSmokeTest.Check(players.All(p => !p.Player.enabled), "Run controller disabled while fishing");
        p1.Press(FishArrow.Up);
        FishingSmokeTest.Check(misses.Count == 0, "Presses ignored during the countdown");
        Vector3 parked = p1.transform.position;
        Vector3 home1 = Bobber(p1);
        Vector3 home2 = Bobber(p2);
        yield return WaitUntil(() => manager.IsFishing, 6f, "Round start");
        FishingSmokeTest.Check((p1.transform.position - parked).magnitude < 0.01f, "Fishers stay on the bank");

        // Sorteio: até 4 peixes, cada um com uma primeira seta diferente.
        ClearRiver();
        for (int i = 0; i < 10; i++)
            Call(manager, "SpawnRandom");
        FishingSmokeTest.Check(river.Count == 4 && river.Select(f => f.Sequence[0]).Distinct().Count() == 4,
            "Four fish with distinct first arrows, got " + river.Count);

        // Pesca: sequência certa pontua.
        yield return StopSpawns();
        manager.Spawn(fishKind, Arrows("UDL"), Vector2.zero);
        Type(p1, "UDL");
        FishingSmokeTest.Check(manager.GetScore(1) == 0, "Catch waits for both players to play the frame");
        yield return Frames(2);
        FishingSmokeTest.Check(manager.GetScore(1) == 2 && manager.GetScore(2) == 0 && catches.SequenceEqual(new[] { 1 }),
            "P1 scores the fish, got " + manager.GetScore(1));

        // Erro: solta o peixe, avisa e trava por um instante.
        yield return StopSpawns();
        manager.Spawn(fishKind, Arrows("LRL"), Vector2.zero);
        Type(p1, "LU");
        FishingSmokeTest.Check(p1.Target == null && p1.IsLocked && misses.SequenceEqual(new[] { 1 }), "Wrong arrow releases and locks");
        Type(p1, "LRL");
        yield return Frames(2);
        FishingSmokeTest.Check(manager.GetScore(1) == 2, "Locked player cannot catch");

        // Disputa: quem completa primeiro leva; o outro solta sem punição.
        Type(p2, "LR");
        FishingSmokeTest.Check(p2.Target != null && p2.Progress == 2, "P2 halfway");
        yield return new WaitForSeconds(0.6f);
        FishingSmokeTest.Check((Bobber(p2) - p2.Target.HookPoint).magnitude < 0.2f &&
            (p2.GetComponentInChildren<LineRenderer>().GetPosition(1) - Bobber(p2)).magnitude < 0.01f,
            "Bobber and line follow the hooked fish");
        FishingSmokeTest.Check((Bobber(p1) - home1).magnitude < 0.01f, "Other bobber stays home");
        RiverFish reeled = p2.Target;
        Type(p1, "LRL");
        yield return Frames(2); // captura no Update do manager, boia no LateUpdate
        FishingSmokeTest.Check(reeled != null && reeled.IsCaught &&
            (Bobber(p1) - reeled.HookPoint).magnitude < 0.8f, "Bobber comes back with the caught fish");
        FishingSmokeTest.Check(manager.GetScore(1) == 4 && manager.GetScore(2) == 0, "First to finish takes the fish");
        FishingSmokeTest.Check(p2.Target == null && misses.Count == 1, "Losing the fish is not a miss");
        yield return new WaitForSeconds(1.5f);
        FishingSmokeTest.Check((Bobber(p1) - home1).magnitude < 0.01f && (Bobber(p2) - home2).magnitude < 0.01f,
            "Bobbers return home");

        // Mesmo quadro: os dois pontuam, sem vantagem de ordem.
        yield return new WaitForSeconds(0.6f);
        yield return StopSpawns();
        manager.Spawn(fishKind, Arrows("UU"), Vector2.zero);
        Type(p1, "UU");
        Type(p2, "UU");
        yield return Frames(2);
        FishingSmokeTest.Check(manager.GetScore(1) == 6 && manager.GetScore(2) == 2, "Same frame scores both");

        // Lixo tira pontos e o placar não fica negativo.
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.3f);
            yield return StopSpawns();
            manager.Spawn(junk, Arrows("DD"), Vector2.zero);
            Type(p2, "DD");
            yield return Frames(2);
        }
        FishingSmokeTest.Check(manager.GetScore(2) == 0, "Junk floors at zero, got " + manager.GetScore(2));

        // Peixe que foge some do rio sem punir.
        RiverFish escaping = manager.Spawn(fishKind, Arrows("RR"), new Vector2(0f, Field<float>(manager, "zoneBottom") + 0.2f));
        Type(p2, "R");
        FishingSmokeTest.Check(p2.Target == escaping, "P2 hooks the escaping fish");
        int missesBefore = misses.Count;
        yield return WaitUntil(() => !river.Contains(escaping), 2f, "Fish escapes");
        FishingSmokeTest.Check(p2.Target == null && misses.Count == missesBefore, "Escape is not a miss");

        // Empate no fim do tempo: morte súbita sem lixo, próximo peixe decide.
        Field<int[]>(manager, "scores")[0] = 3;
        Field<int[]>(manager, "scores")[1] = 3;
        yield return StopSpawns();
        manager.Spawn(junk, Arrows("DD"), Vector2.zero);
        Property(manager, "RemainingTime", 0.01f);
        yield return WaitUntil(() => manager.IsSuddenDeath, 1f, "Sudden death");
        FishingSmokeTest.Check(manager.IsFishing && river.All(f => !f.Kind.IsJunk), "No junk in sudden death");
        for (int i = 0; i < 40; i++)
            FishingSmokeTest.Check(!((FishKind)Call(manager, "PickKind")).IsJunk, "Sudden death never draws junk");
        FishingSmokeTest.Check(GameManager.Instance.GetScore() == "0 x 0", "No medal on tie");
        yield return new WaitForSeconds(0.6f);
        yield return StopSpawns();
        manager.Spawn(fishKind, Arrows("UL"), Vector2.zero);
        Type(p2, "UL");
        yield return WaitUntil(() => !manager.IsFishing, 1f, "Sudden death decided");
        yield return new WaitForSeconds(1.5f);
        FishingSmokeTest.Check(GameManager.Instance.GetScore() == "0 x 1", "P2 medal, got " + GameManager.Instance.GetScore());
        FinishLine result = Field<FinishLine>(manager, "resultScreen");
        FishingSmokeTest.Check(Field<GameObject>(result, "victoryPanel").activeInHierarchy, "Victory panel");
        FishingSmokeTest.Check(river.Count == 0, "River cleared at the end");
    }

    // Limpa o rio e adia o próximo peixe aleatório, para o teste controlar a cena.
    private IEnumerator StopSpawns()
    {
        ClearRiver();
        typeof(FishingManager).GetField("nextSpawn", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, Time.time + 1000f);
        yield return null;
    }

    private void ClearRiver()
    {
        foreach (RiverFish fish in river.ToList())
            Call(manager, "Remove", fish);
    }

    private static Vector3 Bobber(FishingPlayer player) => Field<Transform>(player, "bobber").position;

    private static void Type(FishingPlayer player, string keys)
    {
        foreach (FishArrow arrow in Arrows(keys))
            player.Press(arrow);
    }

    private static FishArrow[] Arrows(string keys) => keys.Select(k =>
        k == 'U' ? FishArrow.Up : k == 'D' ? FishArrow.Down : k == 'L' ? FishArrow.Left : FishArrow.Right).ToArray();

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++)
            yield return null;
    }

    private static IEnumerator WaitUntil(Func<bool> condition, float timeout, string label)
    {
        float start = Time.time;
        while (!condition())
        {
            if (Time.time - start > timeout)
                throw new Exception("Timed out: " + label);
            yield return null;
        }
    }

    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static object Call(object target, string name, params object[] arguments) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

    private static void Property(object target, string name, object value) =>
        target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target, new[] { value });
}
