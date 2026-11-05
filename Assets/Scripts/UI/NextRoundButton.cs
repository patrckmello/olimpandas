
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextRoundButton : MonoBehaviour
{
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string drawSceneName = "CircuitDraw";

    // Próxima prova do circuito ou, se ele acabou, revanche
    // (novo circuito com os mesmos personagens).
    public void NextRound()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsMatchFinished())
        {
            GameManager.Instance.ResetMatch();
        }

        SceneManager.LoadScene(drawSceneName);
    }

    // Inicia uma revanche diretamente.
    public void Rematch()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch();

        // Mantém os personagens selecionados e bloqueados.
        SceneManager.LoadScene(drawSceneName);
    }

    // Prepara uma nova seleção.
    public void ChangeCharacters()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch();

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.UnlockSelection();
        }

        SceneManager.LoadScene(characterSelectSceneName);
    }
}
