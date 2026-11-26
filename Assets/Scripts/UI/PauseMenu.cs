using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Pausa das provas (Esc): continuar, reiniciar a prova ou sair para o menu.
// Sair pede confirmação, porque o placar do circuito é perdido.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject firstButton;
    [SerializeField] private GameObject confirmNoButton;
    [SerializeField] private string menuSceneName = "MainMenu";

    // Lido pelos controles das provas: pausado, ninguém recebe tecla.
    public static bool IsPaused { get; private set; }

    private FinishLine finish;
    private float scaleBeforePause = 1f;

    private void Awake()
    {
        IsPaused = false;
        panel.SetActive(false);
        confirmPanel.SetActive(false);
        finish = FindAnyObjectByType<FinishLine>();
    }

    private void OnDestroy()
    {
        if (!IsPaused)
            return;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (confirmPanel.activeSelf)
            CancelQuit();
        else if (IsPaused)
            Resume();
        else if (finish == null || !finish.IsFinished)
            Pause();
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        scaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        panel.SetActive(true);
        Select(firstButton);
        Sfx.Play(SfxId.Pause);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        Unpause();
        Sfx.Play(SfxId.Resume);
    }

    // Recomeça só esta prova; o placar do circuito continua.
    public void Restart()
    {
        Sfx.Play(SfxId.UiConfirm);
        Unpause();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AskQuit()
    {
        Sfx.Play(SfxId.UiConfirm);
        confirmPanel.SetActive(true);
        Select(confirmNoButton);
    }

    public void CancelQuit()
    {
        Sfx.Play(SfxId.UiBack);
        confirmPanel.SetActive(false);
        Select(firstButton);
    }

    public void ConfirmQuit()
    {
        Sfx.Play(SfxId.QuitConfirm);
        Unpause();
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch();
        if (CharacterSelectionManager.Instance != null)
            CharacterSelectionManager.Instance.UnlockSelection();
        SceneManager.LoadScene(menuSceneName);
    }

    private void Unpause()
    {
        IsPaused = false;
        Time.timeScale = scaleBeforePause;
        panel.SetActive(false);
        confirmPanel.SetActive(false);
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target);
    }
}
