using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Efeitos sonoros do jogo: Sfx.Play(SfxId.Jump).
/// Toca pelo nome do arquivo em Assets/Audio/SFX/Resources/Sfx; finais _01, _02...
/// são variações sorteadas do mesmo som. Nome sem arquivo não toca nada: para trocar
/// ou completar um som (ex.: voz_panda_vitoria), basta pôr o arquivo na pasta.
/// Cria-se sozinho ao iniciar o jogo, sem precisar estar em nenhuma cena.
/// O volume geral é o AudioListener.volume (slider de opções).
/// </summary>
public class Sfx : MonoBehaviour
{
    public const string ResourcesFolder = "Sfx";

    private const int SourceCount = 16;
    private const float AmbienceVolume = 0.5f;
    // O mesmo som pedido duas vezes quase junto (P1 e P2) toca uma vez só.
    private const float RepeatGap = 0.04f;

    private static readonly Dictionary<string, string> SceneAmbience = new()
    {
        { "HurdlesRace", SfxId.StadiumAmbience },
        { "LongJump", SfxId.StadiumAmbience },
        { "Podium", SfxId.StadiumAmbience },
        { "Canoeing", SfxId.RiverAmbience },
        { "Fishing", SfxId.RiverAmbience },
    };

    private static Sfx instance;

    private readonly Dictionary<string, List<AudioClip>> sounds = new();
    private readonly Dictionary<string, float> lastPlayed = new();
    private readonly AudioSource[] sources = new AudioSource[SourceCount];
    private AudioSource ambience;
    private int nextSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (instance != null)
            return;

        var holder = new GameObject(nameof(Sfx));
        DontDestroyOnLoad(holder);
        instance = holder.AddComponent<Sfx>();
    }

    public static void Play(string id, float volume = 1f, float pitch = 1f, float delay = 0f)
    {
        if (instance != null)
            instance.PlayInternal(id, volume, pitch, delay);
    }

    // Bip em "3", "2" e "1", largada em "VAI!". Outros textos ficam em silêncio.
    public static void Countdown(string step)
    {
        if (step == "VAI!")
            Play(SfxId.CountdownGo);
        else if (step.Length == 1 && char.IsDigit(step[0]))
            Play(SfxId.CountdownBeep);
    }

    // Voz do urso escolhido pelo jogador (ex.: voz_polar_dano).
    public static void PlayVoice(int playerId, string line)
    {
        if (CharacterSelectionManager.Instance != null)
            Play($"voz_{BearName(CharacterSelectionManager.Instance.GetCharacter(playerId))}_{line}");
    }

    // "sfx_pulo_02" -> "sfx_pulo"
    public static string IdOf(string clipName) => Regex.Replace(clipName, @"_\d\d$", "");

    private static string BearName(CharacterType character) => character switch
    {
        CharacterType.UrsoPolar => "polar",
        CharacterType.Grizzly => "grizzly",
        CharacterType.UrsoDeOculos => "oculos",
        _ => "panda"
    };

    private void Awake()
    {
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>(ResourcesFolder))
        {
            string id = IdOf(clip.name);
            if (!sounds.TryGetValue(id, out List<AudioClip> variations))
                sounds[id] = variations = new List<AudioClip>();
            variations.Add(clip);
            // Sem atraso na primeira vez que tocar.
            clip.LoadAudioData();
        }

        for (int i = 0; i < sources.Length; i++)
            sources[i] = CreateSource();

        ambience = CreateSource();
        ambience.loop = true;
        ApplyVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioVolumes.Changed += ApplyVolume;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioVolumes.Changed -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        ambience.volume = AmbienceVolume * AudioVolumes.Effects;
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void PlayInternal(string id, float volume, float pitch, float delay)
    {
        if (!sounds.TryGetValue(id, out List<AudioClip> variations))
            return;

        // Tempo real: a câmera lenta do Salto não pode segurar os sons.
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < RepeatGap)
            return;
        lastPlayed[id] = now;

        AudioSource source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;
        source.clip = variations[Random.Range(0, variations.Count)];
        source.volume = volume * AudioVolumes.Effects;
        source.pitch = pitch;
        source.PlayDelayed(Mathf.Max(0f, delay));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        AudioClip clip = SceneAmbience.TryGetValue(scene.name, out string id) &&
            sounds.TryGetValue(id, out List<AudioClip> variations) ? variations[0] : null;

        if (ambience.clip == clip && ambience.isPlaying)
            return;

        ambience.clip = clip;
        if (clip != null)
            ambience.Play();
        else
            ambience.Stop();
    }
}
