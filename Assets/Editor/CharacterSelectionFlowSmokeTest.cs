using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Run in an isolated project copy with -batchmode -executeMethod
// CharacterSelectionFlowSmokeTest.Run (without -quit). Does not save scenes.
[InitializeOnLoad]
public static class CharacterSelectionFlowSmokeTest
{
    private const string Running = "Olimpandas.CharacterSelectionFlowSmokeTest";

    static CharacterSelectionFlowSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                new GameObject("Character selection smoke test").AddComponent<CharacterSelectionFlowRunner>();
        };
    }

    public static void Run()
    {
        foreach (string scene in new[] { "MainMenu", "CharacterSelect", "HurdlesRace" })
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == $"Assets/Scenes/{scene}.unity"))
                throw new Exception("Missing build scene: " + scene);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    public static void Complete(bool success)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.Exit(success ? 0 : 1);
    }
}

public class CharacterSelectionFlowRunner : MonoBehaviour
{
    private bool failed;

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
        IEnumerator test = ExerciseFlow();
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
                CharacterSelectionFlowSmokeTest.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("CharacterSelectionFlowSmokeTest " + (failed ? "FAILED (see errors)" : "PASSED"));
        CharacterSelectionFlowSmokeTest.Complete(!failed);
    }

    private IEnumerator ExerciseFlow()
    {
        yield return null;
        // Exercise the actual persistent MainMenu button event.
        Button play = FindObjectsByType<Button>().Single(b =>
            Enumerable.Range(0, b.onClick.GetPersistentEventCount())
                .Any(i => b.onClick.GetPersistentMethodName(i) == "Jogar"));
        play.onClick.Invoke();
        yield return null;
        yield return null;

        foreach (CharacterType p1 in Enum.GetValues(typeof(CharacterType)))
        foreach (CharacterType p2 in Enum.GetValues(typeof(CharacterType)))
        {
            Check(SceneManager.GetActiveScene().name == "CharacterSelect", "Selection navigation");
            CharacterSelectMenu menu = FindAnyObjectByType<CharacterSelectMenu>();
            var panel1 = Field<CharacterSelectMenu.PlayerPanel>(menu, "player1");
            var panel2 = Field<CharacterSelectMenu.PlayerPanel>(menu, "player2");
            Button start = Field<Button>(menu, "startButton");
            Check(!start.interactable, "Start before confirmation");
            Choose(panel1, p1);
            Choose(panel2, p2);
            panel1.confirmButton.onClick.Invoke();
            Check(!start.interactable && !panel1.nextButton.interactable, "P1 confirmation isolation");
            panel2.confirmButton.onClick.Invoke();
            Check(start.interactable, "Both ready");
            panel2.confirmButton.onClick.Invoke();
            Check(!start.interactable, "Cancel confirmation");
            panel2.confirmButton.onClick.Invoke();
            start.onClick.Invoke();
            yield return null;
            yield return null;
            var selection = CharacterSelectionManager.Instance;
            var score = GameManager.Instance;
            Check(score != null && selection != null, "Managers initialized");
            Check(score.GetScore() == "0 x 0", "New MD3 score");

            // Both players win rounds; alternate the final winner across combinations.
            int champion = p1 == CharacterType.PandaGigante ? 1 : 2;
            foreach (int winner in new[] { champion, 3 - champion, champion })
            {
                yield return new WaitForSeconds(4.1f); // Real countdown completes.
                Check(CharacterSelectionManager.Instance == selection && GameManager.Instance == score,
                    "Persistent manager identity");
                Check(FindObjectsByType<CharacterSelectionManager>().Length == 1 &&
                    FindObjectsByType<GameManager>().Length == 1, "Duplicate managers");
                Check(selection.IsSelectionLocked, "Selection locked during race");
                CheckVisual(1, p1);
                CheckVisual(2, p2);
                FinishLine finish = FindAnyObjectByType<FinishLine>();
                PlayerController player = FindObjectsByType<PlayerController>()
                    .Single(p => p.PlayerId == winner);
                Call(finish, "OnTriggerEnter2D", player.GetComponent<Collider2D>());
                string after = score.GetScore();
                Call(finish, "OnTriggerEnter2D", player.GetComponent<Collider2D>());
                Check(score.GetScore() == after, "Repeated finish awarded twice");
                yield return new WaitForSeconds(1.2f);
                Check(Field<GameObject>(finish, "victoryPanel").activeInHierarchy, "Victory panel");
                Check(Field<GameObject>(finish, "changeCharacterButton").activeSelf == score.IsMatchFinished(),
                    "Change characters visibility");
                if (!score.IsMatchFinished())
                {
                    ClickResult("NextRound");
                    yield return null;
                    yield return null;
                    Check(score.GetScore() == after, "Score preserved between rounds");
                }
            }
            Check(score.GetMedals(champion) == 2 && score.GetMedals(3 - champion) == 1, "MD3 2-1");
            ClickResult("NextRound"); // Same serialized button becomes REVANCHE.
            yield return null;
            yield return null;
            Check(score.GetScore() == "0 x 0" && selection.IsSelectionLocked, "Rematch reset and lock");
            CheckVisual(1, p1);
            CheckVisual(2, p2);
            // Complete a second MD3 quickly to exercise the actual change button.
            score.AddMedal(champion);
            yield return new WaitForSeconds(4.1f);
            Call(FindAnyObjectByType<FinishLine>(), "OnTriggerEnter2D",
                FindObjectsByType<PlayerController>()
                    .Single(p => p.PlayerId == champion).GetComponent<Collider2D>());
            yield return new WaitForSeconds(1.2f);
            ClickResult("ChangeCharacters");
            yield return null;
            yield return null;
            Check(!selection.IsSelectionLocked && score.GetScore() == "0 x 0", "Change reset and unlock");
            Debug.Log($"Combination passed: {p1} x {p2}");
        }

        var finalMenu = FindAnyObjectByType<CharacterSelectMenu>();
        GameManager.Instance.AddMedal(1); // Stale state must not survive back/new selection.
        Field<Button>(finalMenu, "backButton").onClick.Invoke();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "MainMenu", "Back to main menu");
        Check(GameManager.Instance.GetScore() == "0 x 0" && !CharacterSelectionManager.Instance.IsSelectionLocked,
            "Main menu state");
        FindAnyObjectByType<MainMenu>().Jogar();
        yield return null;
        yield return null;
        Check(!Field<Button>(FindAnyObjectByType<CharacterSelectMenu>(), "startButton").interactable,
            "Ready state cleared on return");

        Destroy(GameManager.Instance.gameObject);
        Destroy(CharacterSelectionManager.Instance.gameObject);
        yield return null;
        Check(GameManager.Instance == null && CharacterSelectionManager.Instance == null, "Singleton cleanup");
        SceneManager.LoadScene("HurdlesRace");
        yield return null;
        yield return null;
        Check(GameManager.Instance != null && CharacterSelectionManager.Instance.IsSelectionLocked,
            "Direct race bootstrap");
        CheckVisual(1, CharacterType.PandaGigante);
        CheckVisual(2, CharacterType.UrsoPolar);
    }

    private static void Choose(CharacterSelectMenu.PlayerPanel panel, CharacterType character)
    {
        if (CharacterSelectionManager.Instance.GetCharacter(panel.playerId) != character)
            panel.nextButton.onClick.Invoke();
        Check(CharacterSelectionManager.Instance.GetCharacter(panel.playerId) == character, "Independent choice");
    }

    private static void CheckVisual(int id, CharacterType character)
    {
        PlayerController player = FindObjectsByType<PlayerController>().Single(p => p.PlayerId == id);
        var visual = player.GetComponent<PlayerCharacterVisual>();
        GameObject source = Field<GameObject>(visual,
            character == CharacterType.PandaGigante ? "pandaGigantePrefab" : "ursoPolarPrefab");
        Check(source != null, "Character prefab reference");
        Transform target = player.transform.Find("Visual");
        Transform expected = source.transform.Find("Visual");
        Check(target.GetComponent<Animator>().runtimeAnimatorController == expected.GetComponent<Animator>().runtimeAnimatorController,
            "Selected animator");
        Check(target.localScale == expected.localScale && target.localPosition == expected.localPosition, "Visual transform");
        Check(CharacterSelectionManager.Instance.GetCharacter(id) == character, "Persisted character");
    }

    private static void ClickResult(string method)
    {
        Button button = FindObjectsByType<Button>().Single(b =>
            Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == method));
        button.onClick.Invoke();
    }

    private static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

    private static void Call(object instance, string method, object argument) =>
        instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, new[] { argument });

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Character selection: " + message);
    }
}
