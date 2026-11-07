using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Toca música de fundo e a mantém entre as cenas.
/// Coloque UM objeto com este script na primeira cena (MainMenu).
/// Cada cena pode ter a sua faixa; cenas que dividem a mesma faixa
/// (ex.: MainMenu e CharacterSelect) não reiniciam a música ao trocar.
/// O volume é controlado pelo AudioListener.volume (o slider de opções já faz isso).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
    }

    public static MusicManager Instance { get; private set; }

    [SerializeField] private SceneMusic[] sceneMusics;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

    private AudioSource source;

    private void Awake()
    {
        // Singleton persistente: descarta duplicatas ao voltar para o menu.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = musicVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
{
    // A primeira cena já carregou antes do evento ser registrado.
    PlayForScene(SceneManager.GetActiveScene().name);
    Invoke(nameof(LogAudioState), 1f);
}

private void LogAudioState()
{
    Debug.Log("MusicManager: isPlaying=" + source.isPlaying
        + " | mute=" + source.mute
        + " | volume=" + source.volume
        + " | listenerVolume=" + AudioListener.volume
        + " | listenerPause=" + AudioListener.pause
        + " | time=" + source.time);
}

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        foreach (var entry in sceneMusics)
        {
            Debug.Log("MusicManager: cena " + sceneName + " | clip: " + entry.clip);
            
            if (entry.sceneName != sceneName) continue;

            // Mesma faixa já tocando: não reinicia.
            if (source.clip == entry.clip && source.isPlaying) return;

            source.clip = entry.clip;
            if (entry.clip != null) source.Play();
            else source.Stop();
            return;
        }

        // Cena sem faixa configurada: mantém o que já está tocando.
    }

    /// <summary>Troca a faixa manualmente (ex.: música da tela de vitória).</summary>
    public void PlayClip(AudioClip clip)
    {
        if (clip == null || (source.clip == clip && source.isPlaying)) return;

        source.clip = clip;
        source.Play();
    }

    public void Stop()
    {
        source.Stop();
    }
}
