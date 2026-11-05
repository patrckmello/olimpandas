using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in an isolated project copy with -batchmode -executeMethod
// LongJumpSmokeTest.Run (without -quit). Does not save scenes.
[InitializeOnLoad]
public static class LongJumpSmokeTest
{
    private const string Running = "Olimpandas.LongJumpSmokeTest";
    public const string ScenePath = "Assets/Scenes/LongJump.unity";

    static LongJumpSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                new GameObject("Long jump smoke test").AddComponent<LongJumpSmokeRunner>();
        };
    }

    public static void Run()
    {
        if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
            throw new Exception("LongJump is missing from Build Settings.");

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>()
            .OrderBy(p => p.PlayerId).ToArray();
        LongJumpManager manager = UnityEngine.Object.FindAnyObjectByType<LongJumpManager>();

        Check(players.Length == 2 && players[0].PlayerId == 1 && players[1].PlayerId == 2, "One P1 and one P2");
        Check(Mathf.Abs(players[0].transform.position.x - players[1].transform.position.x) < 0.01f, "Equal starts");
        foreach (PlayerController player in players)
            Check(new SerializedObject(player).FindProperty("dashKey").intValue == (int)KeyCode.None,
                "Dash disabled for " + player.name);
        Check(manager != null, "LongJumpManager present");
        Check(UnityEngine.Object.FindObjectsByType<Hurdle>().Length == 0, "No hurdles");
        Check(UnityEngine.Object.FindObjectsByType<RaceCountdownController>().Length == 0,
            "Race countdown removed (manager runs its own)");

        // Cada jogador precisa ignorar o piso da outra raia, senão o P2 bate a cabeça
        // no Ground_P1 e salta mais baixo (marca injusta).
        var lanes = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<HurdlesLaneSetup>());
        Check(LaneHas(lanes, "upperLane", "Ground_P1") && LaneHas(lanes, "lowerLane", "Ground_P2"),
            "Lane floors isolated");

        var data = new SerializedObject(manager);
        Check(data.FindProperty("players").arraySize == 2, "Manager players");
        for (int i = 0; i < 2; i++)
            Check(data.FindProperty("players").GetArrayElementAtIndex(i).objectReferenceValue == players[i],
                "Manager player order");
        Transform foulLine = (Transform)data.FindProperty("foulLine").objectReferenceValue;
        FinishLine result = (FinishLine)data.FindProperty("resultScreen").objectReferenceValue;
        Check(foulLine != null && foulLine.position.x > players[0].transform.position.x + 8f, "Foul line after runway");
        Check(data.FindProperty("statusText").objectReferenceValue != null, "Status text");
        Check(result != null && !result.GetComponent<Collider2D>().enabled, "Result screen without finish trigger");
        Check(data.FindProperty("attempts").intValue == 3, "Three attempts");

        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    private static bool LaneHas(SerializedObject lanes, string property, string name)
    {
        SerializedProperty array = lanes.FindProperty(property);
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).objectReferenceValue is Collider2D c && c.name == name)
                return true;
        return false;
    }

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

public class LongJumpSmokeRunner : MonoBehaviour
{
    private bool failed;
    private readonly List<(int attempt, int player, bool valid, float mark)> results = new();

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
                LongJumpSmokeTest.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("LongJumpSmokeTest " + (failed ? "FAILED (see errors)" : "PASSED"));
        LongJumpSmokeTest.Complete(!failed);
    }

    private IEnumerator Exercise()
    {
        LongJumpManager manager = FindAnyObjectByType<LongJumpManager>();
        PlayerController[] players = FindObjectsByType<PlayerController>().OrderBy(p => p.PlayerId).ToArray();
        float lineX = Field<Transform>(manager, "foulLine").position.x;
        manager.AttemptResolved += (player, valid, mark) =>
            results.Add((manager.CurrentAttempt, player, valid, mark));

        // Tentativa 1: os dois saltam e caem no mesmo ponto -> marcas iguais.
        yield return WaitForAttempt(manager, players, 1);
        foreach (PlayerController player in players)
            Lift(player, lineX - 3f);
        yield return WaitAirborne(players);
        foreach (PlayerController player in players)
            MoveX(player, lineX + 3f);
        yield return WaitUntil(() => Count(1) == 2, 3f, "Attempt 1 landings");
        foreach (var r in results.Where(r => r.attempt == 1))
            LongJumpSmokeTest.Check(r.valid && Mathf.Abs(r.mark - 6f) < 0.3f, $"P{r.player} valid ~6 m, got {r.mark}");

        // Tentativas 2 e 3: ninguém salta -> falta por tempo.
        yield return WaitUntil(() => Count(3) == 2, 30f, "Attempts 2-3 time out");
        LongJumpSmokeTest.Check(results.Where(r => r.attempt >= 2).All(r => !r.valid), "Timeouts are fouls");
        LongJumpSmokeTest.Check(GameManager.Instance.GetScore() == "0 x 0", "No medal on tie");

        // Desempate: P1 salta, P2 pisa além da linha.
        yield return WaitForAttempt(manager, players, 4);
        LongJumpSmokeTest.Check(manager.IsTiebreak, "Tie goes to extra attempt");
        Lift(players[0], lineX - 3f);
        MoveX(players[1], lineX + 1f);
        yield return WaitAirborne(new[] { players[0] });
        MoveX(players[0], lineX + 2f);
        yield return WaitUntil(() => Count(4) == 2, 3f, "Tiebreak resolved");
        LongJumpSmokeTest.Check(results.Single(r => r.attempt == 4 && r.player == 2).valid == false, "P2 foul over line");

        yield return new WaitForSeconds(3.5f); // marcas + atraso do painel
        LongJumpSmokeTest.Check(GameManager.Instance.GetScore() == "1 x 0", "P1 medal, got " + GameManager.Instance.GetScore());
        FinishLine result = Field<FinishLine>(manager, "resultScreen");
        LongJumpSmokeTest.Check(Field<GameObject>(result, "victoryPanel").activeInHierarchy, "Victory panel");
        LongJumpSmokeTest.Check(manager.GetBestMark(1) > 5.5f && manager.GetBestMark(2) > 5.5f, "Best marks kept");
    }

    private int Count(int attempt) => results.Count(r => r.attempt == attempt);

    private static IEnumerator WaitForAttempt(LongJumpManager manager, PlayerController[] players, int attempt)
    {
        yield return WaitUntil(() => manager.CurrentAttempt == attempt && players.All(p => p.enabled),
            10f, "Attempt " + attempt + " start");
    }

    private static IEnumerator WaitAirborne(PlayerController[] players)
    {
        yield return WaitUntil(() => players.All(p => !p.IsGrounded), 1f, "Airborne");
        yield return null;
        yield return null;
    }

    private static void Lift(PlayerController player, float x)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, body.position.y + 2f);
        body.WakeUp();
    }

    private static void MoveX(PlayerController player, float frontX)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        float halfWidth = player.GetComponent<Collider2D>().bounds.extents.x;
        body.position = new Vector2(frontX - halfWidth, body.position.y);
        body.WakeUp();
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
}
