using System;
using UnityEngine;

// Volumes escolhidos pelo jogador (0 a 1), guardados entre partidas.
public static class AudioVolumes
{
    private const string MasterKey = "volume";
    private const string MusicKey = "volumeMusica";
    private const string EffectsKey = "volumeEfeitos";

    public static event Action Changed;

    public static float Master
    {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set
        {
            AudioListener.volume = value;
            Save(MasterKey, value);
        }
    }

    public static float Music
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set => Save(MusicKey, value);
    }

    public static float Effects
    {
        get => PlayerPrefs.GetFloat(EffectsKey, 1f);
        set => Save(EffectsKey, value);
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        Changed?.Invoke();
    }
}
