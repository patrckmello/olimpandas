using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string nomeCenaJogo = "CharacterSelect";

    [Header("Painéis")]
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelControles;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private AudioClip musicaCreditos;
    [Tooltip("Escondidos enquanto um painel está aberto. O primeiro recebe o foco do teclado.")]
    [SerializeField] private GameObject[] botoesMenu = {};
    [SerializeField] private GameObject voltarOpcoes;
    [SerializeField] private GameObject voltarControles;
    [SerializeField] private GameObject voltarCreditos;

    [Header("Volume")]
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfeitos;
    [SerializeField] private TMP_Text textoVolume;
    [SerializeField] private TMP_Text textoMusica;
    [SerializeField] private TMP_Text textoEfeitos;

    [Header("Tela")]
    [SerializeField] private Toggle toggleTelaCheia;

    private void Start()
    {
        AudioListener.volume = AudioVolumes.Master;
        sliderVolume.SetValueWithoutNotify(AudioVolumes.Master);
        sliderMusica.SetValueWithoutNotify(AudioVolumes.Music);
        sliderEfeitos.SetValueWithoutNotify(AudioVolumes.Effects);
        toggleTelaCheia.SetIsOnWithoutNotify(Screen.fullScreen);
        AtualizarTextos();

        FecharPaineis();
    }

    private void Update()
    {
        bool painelAberto = painelOpcoes.activeSelf || painelControles.activeSelf || painelCreditos.activeSelf;
        if (painelAberto && Input.GetKeyDown(KeyCode.Escape))
        {
            Sfx.Play(SfxId.UiBack);
            FecharPaineis();
            return;
        }

        // Clique fora tira o foco: qualquer direção devolve, para o teclado nunca ficar sem alvo.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Input.GetAxisRaw("Vertical") != 0f || Input.GetAxisRaw("Horizontal") != 0f))
            Focar(painelOpcoes.activeSelf ? voltarOpcoes
                : painelControles.activeSelf ? voltarControles
                : painelCreditos.activeSelf ? voltarCreditos
                : botoesMenu.Length > 0 ? botoesMenu[0] : null);
    }

    public void Jogar()
    {
        Sfx.Play(SfxId.UiConfirm);
        SceneManager.LoadScene(nomeCenaJogo);
    }

    public void AbrirOpcoes() => Abrir(painelOpcoes, voltarOpcoes);

    public void AbrirControles() => Abrir(painelControles, voltarControles);

    public void FecharOpcoes()
    {
        Sfx.Play(SfxId.UiBack);
        FecharPaineis();
    }

    public void FecharControles() => FecharOpcoes();

    public void AbrirCreditos()
    {
        Abrir(painelCreditos, voltarCreditos);
        if (MusicManager.Instance != null && musicaCreditos != null)
            MusicManager.Instance.PlayClip(musicaCreditos);
    }

    public void FecharCreditos() => FecharOpcoes();

    private void Abrir(GameObject painel, GameObject foco)
    {
        Sfx.Play(SfxId.UiConfirm);
        painel.SetActive(true);
        foreach (GameObject botao in botoesMenu)
            botao.SetActive(false);
        Focar(foco);
    }

    private void FecharPaineis()
    {
        if (painelCreditos.activeSelf && MusicManager.Instance != null)
            MusicManager.Instance.RestoreSceneMusic();
        painelOpcoes.SetActive(false);
        painelControles.SetActive(false);
        painelCreditos.SetActive(false);
        foreach (GameObject botao in botoesMenu)
            botao.SetActive(true);
        if (botoesMenu.Length > 0)
            Focar(botoesMenu[0]);
    }

    private static void Focar(GameObject alvo)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(alvo);
    }

    public void DefinirVolume(float valor)
    {
        AudioVolumes.Master = valor;
        AtualizarTextos();
    }

    public void DefinirMusica(float valor)
    {
        AudioVolumes.Music = valor;
        AtualizarTextos();
    }

    public void DefinirEfeitos(float valor)
    {
        AudioVolumes.Effects = valor;
        AtualizarTextos();
        // Amostra no volume novo.
        Sfx.Play(SfxId.UiConfirm);
    }

    private void AtualizarTextos()
    {
        textoVolume.text = Porcentagem(AudioVolumes.Master);
        textoMusica.text = Porcentagem(AudioVolumes.Music);
        textoEfeitos.text = Porcentagem(AudioVolumes.Effects);
    }

    private static string Porcentagem(float valor) => Mathf.RoundToInt(valor * 100f) + "%";

    public void DefinirTelaCheia(bool ativo)
    {
        Screen.fullScreen = ativo;
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
