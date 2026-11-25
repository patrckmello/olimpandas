using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run in an isolated project copy with -batchmode -executeMethod
// CanoeSmokeTest.Run (without -quit). Does not save scenes.
[InitializeOnLoad]
public static class CanoeSmokeTest
{
    private const string Running = "Olimpandas.CanoeSmokeTest";
    public const string ScenePath = "Assets/Scenes/Canoeing.unity";

    static CanoeSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                new GameObject("Canoe smoke test").AddComponent<CanoeSmokeRunner>();
        };
    }

    public static void Run()
    {
        if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
            throw new Exception("Canoeing is missing from Build Settings.");

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>()
            .OrderBy(p => p.PlayerId).ToArray();
        CanoeRaceManager manager = UnityEngine.Object.FindAnyObjectByType<CanoeRaceManager>();

        Check(players.Length == 2 && players[0].PlayerId == 1 && players[1].PlayerId == 2, "One P1 and one P2");
        Check(Mathf.Abs(players[0].transform.position.x - players[1].transform.position.x) < 0.01f, "Equal starts");
        Check(manager != null, "CanoeRaceManager present");
        Check(UnityEngine.Object.FindObjectsByType<Hurdle>().Length == 0, "No hurdles");
        Check(UnityEngine.Object.FindObjectsByType<RaceCountdownController>().Length == 0,
            "Race countdown removed (manager runs its own)");

        var data = new SerializedObject(manager);
        SerializedProperty canoes = data.FindProperty("canoes");
        Check(canoes.arraySize == 2, "Two canoes");
        for (int i = 0; i < 2; i++)
        {
            CanoePaddler canoe = players[i].GetComponent<CanoePaddler>();
            Check(canoe != null && canoes.GetArrayElementAtIndex(i).objectReferenceValue == canoe, "Canoe order");
            var paddler = new SerializedObject(canoe);
            foreach (string field in new[] { "hull", "paddle", "splash" })
                Check(paddler.FindProperty(field).objectReferenceValue != null, field + " on P" + (i + 1));
        }
        Check(Key(players[0], "upKey") == KeyCode.W && Key(players[0], "downKey") == KeyCode.S, "P1 steering keys");
        Check(Key(players[1], "upKey") == KeyCode.UpArrow && Key(players[1], "downKey") == KeyCode.DownArrow,
            "P2 steering keys");

        foreach (string field in new[] { "finish", "statusText", "resultScreen", "timer" })
            Check(data.FindProperty(field).objectReferenceValue != null, "Manager " + field);
        var result = (FinishLine)data.FindProperty("resultScreen").objectReferenceValue;
        Check(!result.GetComponent<Collider2D>().enabled, "Result screen without finish trigger");

        // Prova justa: mesmos obstáculos, na mesma posição relativa, nas duas raias.
        CanoeObstacle[] obstacles = UnityEngine.Object.FindObjectsByType<CanoeObstacle>();
        Check(obstacles.Length > 0 && obstacles.All(o => o.GetComponent<Collider2D>().isTrigger), "Trigger obstacles");
        string[] lanes = players.Select(p => string.Join("|", obstacles
            .Where(o => Mathf.Sign(o.transform.position.y) == Mathf.Sign(p.transform.position.y))
            .Select(o => $"{o.name}:{o.transform.position.x:0.00}:{o.transform.position.y - p.transform.position.y:0.00}")
            .OrderBy(s => s))).ToArray();
        Check(lanes[0].Length > 0 && lanes[0] == lanes[1], "Identical obstacles in both lanes");

        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    private static KeyCode Key(PlayerController player, string field) =>
        (KeyCode)new SerializedObject(player.GetComponent<CanoePaddler>()).FindProperty(field).intValue;

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

public class CanoeSmokeRunner : MonoBehaviour
{
    private bool failed;
    private CanoeRaceManager manager;
    private CanoePaddler[] canoes;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
    }

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
                CanoeSmokeTest.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("CanoeSmokeTest " + (failed ? "FAILED (see errors)" : "PASSED"));
        CanoeSmokeTest.Complete(!failed);
    }

    private IEnumerator Exercise()
    {
        Bind();
        yield return null;
        CanoeSmokeTest.Check(canoes.All(c => !c.Player.enabled), "Run controller disabled in the canoe");
        canoes[0].RegisterStroke(CanoePaddler.Left);
        CanoeSmokeTest.Check(canoes[0].Speed == 0f, "Strokes ignored during the countdown");
        yield return WaitUntil(() => manager.IsRacing, 6f, "Race start");
        CanoeSmokeTest.Check(Mathf.Abs(manager.GetProgress(1) - manager.GetProgress(2)) < 0.001f, "Equal start progress");

        // Alternar rende mais do que insistir num lado, que ainda tira a canoa do rumo.
        for (int i = 0; i < 4; i++)
        {
            canoes[0].RegisterStroke(i % 2 == 0 ? CanoePaddler.Left : CanoePaddler.Right);
            canoes[1].RegisterStroke(CanoePaddler.Left);
            yield return new WaitForSeconds(0.3f);
        }
        CanoeSmokeTest.Check(canoes[0].Speed > canoes[1].Speed * 1.4f,
            $"Alternating is faster: {canoes[0].Speed} vs {canoes[1].Speed}");
        CanoeSmokeTest.Check(manager.GetProgress(1) > manager.GetProgress(2) && manager.GetProgress(1) > 0f, "Progress follows speed");
        CanoeSmokeTest.Check(Mathf.Abs(canoes[0].Heading) < 6f, "Alternating keeps course, got " + canoes[0].Heading);
        CanoeSmokeTest.Check(canoes[1].Heading < -3f, "Left strokes turn the bow down, got " + canoes[1].Heading);

        // Metralhar a tecla não passa do ritmo ideal.
        float before = canoes[0].Speed;
        for (int i = 0; i < 20; i++)
            canoes[0].RegisterStroke(i % 2 == 0 ? CanoePaddler.Left : CanoePaddler.Right);
        CanoeSmokeTest.Check(canoes[0].Speed - before < 1.5f, "Mashing is not rewarded, got +" + (canoes[0].Speed - before));

        // Margem: não sai da raia.
        float laneY = canoes[1].transform.position.y;
        canoes[1].InputEnabled = false;
        canoes[1].SetSteer(-1f);
        yield return new WaitForSeconds(1.6f);
        float limit = Field<float>(canoes[1], "laneHalfHeight");
        CanoeSmokeTest.Check(canoes[1].transform.position.y < laneY - 0.3f &&
            canoes[1].transform.position.y > Field<float>(canoes[1], "laneCenterY") - limit - 0.1f,
            "Steering stops at the bank, y=" + canoes[1].transform.position.y);
        canoes[1].SetSteer(0f);

        // Obstáculo: perde velocidade, desvia e avisa a UI.
        int hits = 0;
        manager.Collided += id => hits += id == 1 ? 1 : 100;
        CanoeObstacle obstacle = FindObjectsByType<CanoeObstacle>()
            .Where(o => o.transform.position.y > 0f && o.transform.position.x > canoes[0].BowX + 8f)
            .OrderBy(o => o.transform.position.x).First();
        Vector3 rock = obstacle.GetComponent<Collider2D>().bounds.center;
        Teleport(canoes[0], rock.x - 2.6f, Mathf.Clamp(rock.y + 0.2f,
            Field<float>(canoes[0], "laneCenterY") - limit, Field<float>(canoes[0], "laneCenterY") + limit));
        Property(canoes[0], "Speed", 7f);
        yield return WaitUntil(() => hits > 0, 2f, "Obstacle hit");
        CanoeSmokeTest.Check(hits == 1, "Only P1 hit");
        CanoeSmokeTest.Check(canoes[0].Speed < 3.5f, "Hit slows the canoe, got " + canoes[0].Speed);

        // Tempo limite: empate exato não decide, vantagem decide.
        Property(canoes[0], "Speed", 0f);
        Property(canoes[1], "Speed", 0f);
        Teleport(canoes[0], 0f, Field<float>(canoes[0], "laneCenterY"));
        Teleport(canoes[1], 0f, Field<float>(canoes[1], "laneCenterY"));
        yield return new WaitForFixedUpdate();
        Teleport(canoes[0], 0f, Field<float>(canoes[0], "laneCenterY"));
        Teleport(canoes[1], 0f, Field<float>(canoes[1], "laneCenterY"));
        Property(manager, "ElapsedTime", manager.TimeLimit + 1f);
        yield return new WaitForSeconds(0.3f);
        CanoeSmokeTest.Check(manager.IsRacing && GameManager.Instance.GetScore() == "0 x 0", "Exact tie keeps racing");
        Teleport(canoes[0], 3f, Field<float>(canoes[0], "laneCenterY"));
        yield return WaitUntil(() => !manager.IsRacing, 1f, "Time limit decides");
        yield return new WaitForSeconds(1.5f);
        CanoeSmokeTest.Check(GameManager.Instance.GetScore() == "1 x 0", "Leader wins at the time limit, got " +
            GameManager.Instance.GetScore());

        // Segunda prova: cruzar a chegada vence e mostra o resultado.
        SceneManager.LoadScene("Canoeing");
        yield return null;
        yield return null;
        Bind();
        yield return WaitUntil(() => manager.IsRacing, 6f, "Second race start");
        Transform finish = Field<Transform>(manager, "finish");
        Teleport(canoes[1], finish.position.x - 1f, Field<float>(canoes[1], "laneCenterY"));
        yield return WaitUntil(() => !manager.IsRacing, 2f, "Finish line crossed");
        CanoeSmokeTest.Check(Mathf.Approximately(manager.GetProgress(2), 1f), "Winner at full progress");
        yield return new WaitForSeconds(1.5f);
        CanoeSmokeTest.Check(GameManager.Instance.GetScore() == "1 x 1", "P2 medal, got " + GameManager.Instance.GetScore());
        CanoeSmokeTest.Check(GameManager.Instance.Results.All(r => r.SceneName == "Canoeing"), "Circuit history");
        FinishLine result = Field<FinishLine>(manager, "resultScreen");
        CanoeSmokeTest.Check(Field<GameObject>(result, "victoryPanel").activeInHierarchy, "Victory panel");
        CanoeSmokeTest.Check(canoes.All(c => Mathf.Abs(c.transform.eulerAngles.z) < 0.5f), "Canoes straight at the end");
    }

    private void Bind()
    {
        manager = FindAnyObjectByType<CanoeRaceManager>();
        canoes = FindObjectsByType<CanoePaddler>().OrderBy(c => c.PlayerId).ToArray();
    }

    private static void Teleport(CanoePaddler canoe, float x, float y)
    {
        Rigidbody2D body = canoe.GetComponent<Rigidbody2D>();
        body.position = new Vector2(x, y);
        body.linearVelocity = Vector2.zero;
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

    private static void Property(object target, string name, object value) =>
        target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target, new[] { value });
}
