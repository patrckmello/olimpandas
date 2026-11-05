using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tela PROVISÓRIA de sorteio entre provas do Circuito Olímpico.
// O visual final é do Gabriel: basta usar GameManager.DrawNextMinigame(),
// GameManager.Results / GetScore() e carregar Minigame.SceneName.
public class CircuitDrawScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text eventText;
    [SerializeField] private TMP_Text minigameText;
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private float revealDelay = 0.8f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private string fallbackSceneName = "MainMenu";

    private IEnumerator Start()
    {
        GameManager game = GameManager.Instance;
        int eventNumber = game.Results.Count + 1;
        Minigame next = game.DrawNextMinigame();

        eventText.text = $"PROVA {eventNumber}";
        scoreText.text = $"MEDALHAS\nP1  {game.GetScore()}  P2";
        minigameText.text = "SORTEANDO...";

        if (next == null)
        {
            Debug.LogError("Nenhuma modalidade disponível no Build Settings para o circuito.");
            minigameText.text = "NENHUMA PROVA DISPONÍVEL";
            yield return new WaitForSeconds(displayDuration);
            SceneManager.LoadScene(fallbackSceneName);
            yield break;
        }

        yield return new WaitForSeconds(revealDelay);
        minigameText.text = next.DisplayName.ToUpperInvariant();
        yield return new WaitForSeconds(displayDuration);
        SceneManager.LoadScene(next.SceneName);
    }
}
