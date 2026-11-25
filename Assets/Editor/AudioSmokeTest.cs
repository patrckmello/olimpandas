using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Run with -batchmode -quit -executeMethod AudioSmokeTest.Run.
// Edit mode only; does not save scenes.
public static class AudioSmokeTest
{
    public static void Run()
    {
        CheckSounds();
        CheckMusic();
        Debug.Log("AudioSmokeTest passed: every SfxId has a clip, no orphan clips, music set for every scene.");
    }

    private static void CheckSounds()
    {
        string[] ids = typeof(SfxId).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()).ToArray();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(Sfx.ResourcesFolder);

        Check(ids.Distinct().Count() == ids.Length, "Unique sound ids");
        foreach (string id in ids)
            Check(clips.Any(c => Sfx.IdOf(c.name) == id), "Clip exists for " + id);
        foreach (AudioClip clip in clips)
            Check(clip.name.StartsWith("voz_") || ids.Contains(Sfx.IdOf(clip.name)), "Clip is used: " + clip.name);
    }

    private static void CheckMusic()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        MusicManager manager = Object.FindAnyObjectByType<MusicManager>();
        Check(manager != null, "MusicManager in MainMenu");

        SerializedProperty entries = new SerializedObject(manager).FindProperty("sceneMusics");
        var music = new Dictionary<string, Object>();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            music[entry.FindPropertyRelative("sceneName").stringValue] =
                entry.FindPropertyRelative("clip").objectReferenceValue;
        }

        IEnumerable<string> scenes = MinigameCatalog.All.Select(m => m.SceneName)
            .Concat(new[] { "MainMenu", "CharacterSelect", "CircuitDraw" });
        foreach (string scene in scenes)
            Check(music.TryGetValue(scene, out Object clip) && clip != null, "Music set for " + scene);
        Check(MinigameCatalog.All.Select(m => music[m.SceneName]).Distinct().Count() == MinigameCatalog.All.Count,
            "Each event has its own music");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("AudioSmokeTest failed: " + message);
    }
}
