using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Run with -batchmode -quit -executeMethod CircuitRotationSmokeTest.Run.
// Edit mode only; does not save scenes.
public static class CircuitRotationSmokeTest
{
    private const string DrawScene = "CircuitDraw";

    public static void Run()
    {
        CheckRotationRules();
        CheckCatalogAndFlow();
        Debug.Log("CircuitRotationSmokeTest passed: rotation rules, catalog build scenes, selection/next buttons and draw scene.");
    }

    private static void CheckRotationRules()
    {
        Minigame[] four = "ABCD".Select(c => new Minigame(c.ToString(), c.ToString())).ToArray();
        Minigame[] two = four.Take(2).ToArray();
        var random = new System.Random(1);

        for (int trial = 0; trial < 200; trial++)
        {
            var played = new List<string>();
            for (int i = 0; i < four.Length; i++)
                played.Add(MinigameRotation.Draw(four, played, random).SceneName);
            Check(played.Distinct().Count() == four.Length, "No repeat while unused modalities remain");
            Check(MinigameRotation.Draw(four, played, random).SceneName != played.Last(),
                "New cycle avoids back-to-back repeat");

            var circuit = new List<string>();
            for (int i = 0; i < 3; i++)
                circuit.Add(MinigameRotation.Draw(two, circuit, random).SceneName);
            Check(circuit[0] != circuit[1] && circuit[2] == circuit[0],
                "Two modalities in a 3-event circuit: A B A, got " + string.Join(" ", circuit));
        }

        var firstDraws = Enumerable.Range(0, 200)
            .Select(_ => MinigameRotation.Draw(four, new List<string>(), random).SceneName);
        Check(firstDraws.Distinct().Count() == four.Length, "Draw reaches every modality");
        Check(MinigameRotation.Draw(two, new List<string> { "PrototypeRace" }, random) != null,
            "Scenes outside the catalog do not block the draw");
        Check(MinigameRotation.Draw(four.Take(1).ToArray(), new List<string> { "A" }, random).SceneName == "A",
            "Single modality repeats");
        Check(MinigameRotation.Draw(Array.Empty<Minigame>(), new List<string>(), random) == null,
            "Nothing available returns null");
    }

    private static void CheckCatalogAndFlow()
    {
        Check(MinigameCatalog.All.Count >= 2, "At least two modalities in the circuit");
        Check(MinigameCatalog.All.Select(m => m.SceneName).Distinct().Count() == MinigameCatalog.All.Count,
            "Unique catalog scenes");
        foreach (string scene in MinigameCatalog.All.Select(m => m.SceneName).Append(DrawScene))
            Check(EditorBuildSettings.scenes.Any(s => s.enabled && Path.GetFileNameWithoutExtension(s.path) == scene),
                "Enabled build scene: " + scene);

        EditorSceneManager.OpenScene("Assets/Scenes/CharacterSelect.unity");
        Check(StringField(Object.FindAnyObjectByType<CharacterSelectMenu>(), "gameSceneName") == DrawScene,
            "Character selection starts the circuit draw");

        foreach (Minigame minigame in MinigameCatalog.All)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{minigame.SceneName}.unity");
            NextRoundButton[] buttons = Object.FindObjectsByType<NextRoundButton>(FindObjectsInactive.Include);
            Check(buttons.Length > 0 && buttons.All(b => StringField(b, "drawSceneName") == DrawScene),
                minigame.SceneName + " result buttons go to the draw");
            Check(Object.FindObjectsByType<FinishLine>(FindObjectsInactive.Include).Length == 1,
                minigame.SceneName + " declares the modality winner through FinishLine");
            Check(Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length == 1,
                minigame.SceneName + " bootstraps a GameManager when opened directly");
        }

        EditorSceneManager.OpenScene($"Assets/Scenes/{DrawScene}.unity");
        CircuitDrawScreen screen = Object.FindAnyObjectByType<CircuitDrawScreen>();
        Check(screen != null, "Draw screen component");
        Check(Object.FindObjectsByType<GameManager>().Length == 1, "Draw scene bootstraps a GameManager");
        var data = new SerializedObject(screen);
        foreach (string text in new[] { "eventText", "minigameText", "scoreText" })
            Check(data.FindProperty(text).objectReferenceValue != null, "Draw screen " + text);
    }

    private static string StringField(Object target, string name) =>
        new SerializedObject(target).FindProperty(name).stringValue;

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Circuit: " + message);
    }
}
