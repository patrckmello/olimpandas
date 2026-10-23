
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextRoundButton : MonoBehaviour
{
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";

    // Próxima rodada ou revanche com os mesmos personagens.
    public void NextRound()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsMatchFinished())
        {
            GameManager.Instance.ResetMatch();
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // Inicia uma revanche diretamente.
    public void Rematch()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch();

        // Mantém os personagens selecionados e bloqueados.
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
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
