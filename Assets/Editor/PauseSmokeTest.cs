using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run in an isolated project copy with -batchmode -executeMethod
// PauseSmokeTest.Run (without -quit). Does not save scenes.
// Pause, restart and quit with confirmation; event HUDs follow their managers.
[InitializeOnLoad]
public static class PauseSmokeTest
{
    private const string Running = "Olimpandas.PauseSmokeTest";

    static PauseSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                new GameObject("Pause smoke test").AddComponent<PauseSmokeRunner>();
        };
    }

    public static void Run()
    {
        foreach (Minigame minigame in MinigameCatalog.All)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{minigame.SceneName}.unity");
            Check(UnityEngine.Object.FindObjectsByType<PauseMenu>(FindObjectsInactive.Include).Length == 1,
                "One pause menu in " + minigame.SceneName);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/HurdlesRace.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    public static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Pause: " + message);
    }

    public static void Complete(bool success)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.Exit(success ? 0 : 1);
    }
}

public class PauseSmokeRunner : MonoBehaviour
{
    private bool failed;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) failed = true; };
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
                PauseSmokeTest.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("PauseSmokeTest " + (failed ? "FAILED (see errors)" : "PASSED"));
        PauseSmokeTest.Complete(!failed);
    }

    private IEnumerator Exercise()
    {
        yield return new WaitForSeconds(4.2f); // contagem da corrida
        GameManager.Instance.AddMedal(2);
        PauseMenu pause = FindAnyObjectByType<PauseMenu>();
        PlayerController p1 = FindObjectsByType<PlayerController>().Single(p => p.PlayerId == 1);
        Vector3 before = p1.transform.position;

        pause.Pause();
        PauseSmokeTest.Check(PauseMenu.IsPaused && Time.timeScale == 0f, "Pause stops time");
        PauseSmokeTest.Check(Field<GameObject>(pause, "panel").activeSelf, "Pause panel");
        yield return new WaitForSecondsRealtime(0.3f);
        PauseSmokeTest.Check((p1.transform.position - before).magnitude < 0.001f, "Nothing moves while paused");
        pause.Resume();
        PauseSmokeTest.Check(!PauseMenu.IsPaused && Time.timeScale == 1f, "Resume restores time");

        // Reiniciar a prova mantém o placar do circuito.
        pause.Pause();
        pause.Restart();
        yield return null;
        yield return null;
        PauseSmokeTest.Check(SceneManager.GetActiveScene().name == "HurdlesRace" && Time.timeScale == 1f && !PauseMenu.IsPaused,
            "Restart reloads the event");
        PauseSmokeTest.Check(GameManager.Instance.GetScore() == "0 x 1", "Restart keeps the score");

        // Sair pede confirmação; NÃO volta à pausa, SIM vai ao menu e zera o placar.
        pause = FindAnyObjectByType<PauseMenu>();
        pause.Pause();
        pause.AskQuit();
        PauseSmokeTest.Check(Field<GameObject>(pause, "confirmPanel").activeSelf, "Quit asks first");
        pause.CancelQuit();
        PauseSmokeTest.Check(!Field<GameObject>(pause, "confirmPanel").activeSelf && PauseMenu.IsPaused, "Cancel keeps paused");
        pause.AskQuit();
        pause.ConfirmQuit();
        yield return null;
        yield return null;
        PauseSmokeTest.Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1f, "Quit goes to the menu");
        PauseSmokeTest.Check(GameManager.Instance.GetScore() == "0 x 0", "Quit resets the circuit");

        // HUDs: o Salto mostra a tentativa; a Canoagem mexe as canoas da barra.
        SceneManager.LoadScene("LongJump");
        yield return new WaitForSeconds(0.5f);
        PauseSmokeTest.Check(Field<TMP_Text>(FindAnyObjectByType<LongJumpHud>(), "attemptText").text == "TENTATIVA 1/3", "Long jump attempt HUD");
        SceneManager.LoadScene("Canoeing");
        yield return new WaitUntil(() => FindAnyObjectByType<CanoeRaceManager>()?.IsRacing == true);
        CanoeHud canoeHud = FindAnyObjectByType<CanoeHud>();
        RectTransform marker = Field<RectTransform>(canoeHud, "markerP1");
        float start = marker.anchoredPosition.x;
        Rigidbody2D body = FindObjectsByType<CanoePaddler>().Single(c => c.PlayerId == 1).GetComponent<Rigidbody2D>();
        body.position += Vector2.right * 40f;
        yield return new WaitForSeconds(0.2f);
        PauseSmokeTest.Check(marker.anchoredPosition.x > start + 100f, "Canoe progress HUD moves");
    }

    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
