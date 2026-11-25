using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string nomeCenaJogo = "CharacterSelect";
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Toggle toggleTelaCheia;

    private const string ChaveVolume = "volume";

    private void Start()
    {
        painelOpcoes.SetActive(false);

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
        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        Sfx.Play(SfxId.UiBack);
        painelOpcoes.SetActive(false);
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
