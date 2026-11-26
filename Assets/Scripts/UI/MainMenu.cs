using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string nomeCenaJogo = "CharacterSelect";
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Toggle toggleTelaCheia;
    [Tooltip("Escondidos enquanto as opções estão abertas.")]
    [SerializeField] private GameObject[] botoesMenu = {};

    private const string ChaveVolume = "volume";

    private void Start()
    {
        MostrarOpcoes(false);

        float volume = PlayerPrefs.GetFloat(ChaveVolume, 1f);
        AudioListener.volume = volume;
        sliderVolume.SetValueWithoutNotify(volume);
        toggleTelaCheia.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public void Jogar()
    {
        Sfx.Play(SfxId.UiConfirm);
        SceneManager.LoadScene(nomeCenaJogo);
    }

    public void AbrirOpcoes()
    {
        Sfx.Play(SfxId.UiConfirm);
        MostrarOpcoes(true);
    }

    public void FecharOpcoes()
    {
        Sfx.Play(SfxId.UiBack);
        MostrarOpcoes(false);
    }

    private void MostrarOpcoes(bool abertas)
    {
        painelOpcoes.SetActive(abertas);
        foreach (GameObject botao in botoesMenu)
            botao.SetActive(!abertas);
    }

    public void DefinirVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ChaveVolume, valor);
    }

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
