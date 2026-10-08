using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HurdlesRaceSceneSmokeTest
{
    public static void Run()
    {
        HurdleSmokeTest.Run();
        HurdleSmokeTest.RunSlowOnPlayerImpact();
        HurdlesLaneSetupSmokeTest.Run();

        const string path = "Assets/Scenes/HurdlesRace.unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        PlayerController[] players = FindAll<PlayerController>(scene)
            .OrderBy(player => player.PlayerId).ToArray();
        Hurdle[] hurdles = FindAll<Hurdle>(scene);
        HurdlesLaneSetup lanes = FindOne<HurdlesLaneSetup>(scene);
        FinishLine finish = FindOne<FinishLine>(scene);
        RaceCountdownController countdown = FindOne<RaceCountdownController>(scene);
        MultiplayerCameraFollow camera = FindOne<MultiplayerCameraFollow>(scene);
        FindOne<GameManager>(scene);
        FindOne<Canvas>(scene);

        if (players.Length != 2 || players[0].PlayerId != 1 || players[1].PlayerId != 2)
            throw new Exception("The race needs one P1 and one P2.");
        if (Mathf.Abs(players[0].transform.position.x - players[1].transform.position.x) > 0.01f)
            throw new Exception("Players do not start at the same distance.");
        if (hurdles.Length != 10)
            throw new Exception("The race needs five hurdles per lane.");
        foreach (Hurdle hurdle in hurdles)
        {
            SerializedObject hurdleData = new SerializedObject(hurdle);
            float multiplier = hurdleData.FindProperty("impactSlowMultiplier").floatValue;
            float duration = hurdleData.FindProperty("impactSlowDuration").floatValue;
            if (multiplier <= 0f || multiplier >= 1f || duration <= 0f)
                throw new Exception("A hurdle has invalid Slow settings.");
            SerializedProperty frames = hurdleData.FindProperty("fallFrames");
            if (frames.arraySize != 5)
                throw new Exception("A hurdle is missing the five falling sprites.");
            for (int i = 0; i < frames.arraySize; i++)
                if (frames.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new Exception("A hurdle has an empty falling sprite.");
            Transform art = hurdle.transform.Find("Visual/HurdleArt");
            if (art == null || art.GetComponent<SpriteRenderer>().sprite == null ||
                Mathf.Abs(art.localScale.x - 0.3f) > 0.001f)
                throw new Exception("A hurdle is missing its aligned artwork.");
        }
        foreach (float x in new[] { -7f, 1f, 9f, 17f, 25f })
            if (hurdles.Count(h => Mathf.Abs(h.transform.position.x - x) < 0.01f) != 2)
                throw new Exception("A hurdle pair is misaligned at X=" + x);

        if (FindNamed(scene, "Single stadium backdrop") != 1 ||
            FindNamed(scene, "Shared athletics track") != 1 ||
            FindNamed(scene, "Lane divider") != 1)
            throw new Exception("The scene must contain one shared stadium and track.");
        if (finish.GetComponent<BoxCollider2D>() == null ||
            !finish.GetComponent<BoxCollider2D>().isTrigger)
            throw new Exception("The shared finish trigger is missing.");

        SerializedObject laneData = new SerializedObject(lanes);
        foreach (string property in new[] { "player1", "player2" })
            if (laneData.FindProperty(property).objectReferenceValue == null)
                throw new Exception("Lane setup is missing " + property);
        foreach (string property in new[] { "upperLane", "lowerLane" })
        {
            SerializedProperty colliders = laneData.FindProperty(property);
            if (colliders.arraySize != 6)
                throw new Exception("Lane setup needs a floor and five hurdles in " + property);
            for (int i = 0; i < colliders.arraySize; i++)
                if (colliders.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new Exception("Lane setup has an empty collider reference.");
        }
        if (new SerializedObject(countdown).FindProperty("players").arraySize != 2 ||
            new SerializedObject(camera).FindProperty("targets").arraySize != 2)
            throw new Exception("Countdown or camera is not connected to both players.");

        SerializedObject finishData = new SerializedObject(finish);
        foreach (string property in new[] { "victoryPanel", "victoryText", "medalText", "nextButtonText" })
            if (finishData.FindProperty(property).objectReferenceValue == null)
                throw new Exception("Finish UI is missing " + property);
        if (!EditorBuildSettings.scenes.Any(entry => entry.path == path && entry.enabled))
            throw new Exception("HurdlesRace is missing from Build Settings.");

        Debug.Log("HurdlesRaceSceneSmokeTest passed: shared stadium and track, two fair lanes, ten hurdles, finish, countdown, camera, UI and build scene.");
    }

    private static T[] FindAll<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static T FindOne<T>(Scene scene) where T : Component
    {
        T[] found = FindAll<T>(scene);
        if (found.Length != 1)
            throw new Exception($"Expected one {typeof(T).Name}, found {found.Length}.");
        return found[0];
    }

    private static int FindNamed(Scene scene, string name) =>
        scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Count(item => item.name == name);
}
