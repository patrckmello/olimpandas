
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextRoundButton : MonoBehaviour
{
    // Próxima rodada ou revanche com os mesmos personagens.
    public void NextRound()
    {
        if (GameManager.Instance.IsMatchFinished())
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
        GameManager.Instance.ResetMatch();

        // Mantém os personagens selecionados e bloqueados.
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // Prepara uma nova seleção.
    public void ChangeCharacters()
    {
        GameManager.Instance.ResetMatch();

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.UnlockSelection();
        }

        // A navegação para a tela de seleção será
        // conectada pelo responsável pelo menu.
        Debug.Log(
            "Nova seleção liberada. Aguardando integração do menu."
        );
    }
}

