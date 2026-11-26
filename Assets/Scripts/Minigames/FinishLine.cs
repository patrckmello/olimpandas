
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FinishLine : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text medalText;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("Botão de troca de personagens")]
    [SerializeField] private GameObject changeCharacterButton;

    [Header("Câmera")]
    [SerializeField]
    private MultiplayerCameraFollow multiplayerCamera;

    [Header("Tempos")]
    [SerializeField]
    private float victoryDisplayDelay = 1.1f;

    [Header("Pódio")]
    [SerializeField]
    private string podiumSceneName = "Podium";

    private bool raceFinished = false;

    public bool IsFinished => raceFinished;

    private void Awake()
    {
        if (multiplayerCamera == null)
        {
            multiplayerCamera =
                FindFirstObjectByType<MultiplayerCameraFollow>();
        }

        // O botão extra começa escondido.
        if (changeCharacterButton != null)
        {
            changeCharacterButton.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (raceFinished)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerController winner =
            other.GetComponent<PlayerController>();

        if (winner == null)
            return;

        DeclareWinner(winner);
    }

    // Também usado por provas sem linha de chegada (ex.: Salto a distância).
    // detail: tempo, marca ou placar mostrado no pódio. Sem ele, usa o cronômetro da cena.
    public void DeclareWinner(PlayerController winner, string detail = null)
    {
        if (raceFinished || winner == null)
            return;

        raceFinished = true;

        StopAllTimers();
        if (detail == null)
        {
            Timer timer = FindAnyObjectByType<Timer>();
            detail = timer != null && timer.ElapsedTime > 0f ? Timer.Format(timer.ElapsedTime) : "";
        }

        int playerId = winner.PlayerId;

        bool matchFinished =
            GameManager.Instance.AwardMinigame(
                SceneManager.GetActiveScene().name,
                playerId,
                detail
            );

        Sfx.Play(SfxId.CrowdCheer, 0.7f);
        Sfx.PlayVoice(playerId, "vitoria");
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetDucked(true);

        // Congela os jogadores e dispara a vitória.
        FreezeAllPlayers(winner);

        // Aproxima a câmera do vencedor.
        if (multiplayerCamera != null)
        {
            multiplayerCamera.FocusOnWinner(
                winner.transform
            );
        }

        if (matchFinished)
        {
            victoryText.text =
                $"PLAYER {playerId}\nCAMPEÃO!";

            SetNextLabel("REVANCHE");

            if (changeCharacterButton != null)
            {
                changeCharacterButton.SetActive(true);
            }
        }
        else
        {
            victoryText.text =
                $"PLAYER {playerId}\nVENCEU!";

            SetNextLabel("PRÓXIMA PROVA");

            if (changeCharacterButton != null)
            {
                changeCharacterButton.SetActive(false);
            }
        }

        medalText.text =
            $"MEDALHAS\n{GameManager.Instance.GetScore()}";

        StartCoroutine(ShowVictoryPanel());

        Debug.Log(
            $"PLAYER {playerId} VENCEU A PROVA!"
        );
    }

    // Opcional: botão com o texto desenhado na imagem não tem label.
    private void SetNextLabel(string label)
    {
        if (nextButtonText != null)
            nextButtonText.text = label;
    }

    private void StopAllTimers()
    {
        Timer[] timers =
            FindObjectsByType<Timer>(FindObjectsSortMode.None);

        foreach (Timer timer in timers)
        {
            if (timer != null)
            {
                timer.StopTimer();
            }
        }
    }

    private IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(
            victoryDisplayDelay
        );

        // Sem o pódio no Build Settings, mostra o painel da própria cena.
        if (Application.CanStreamedLevelBeLoaded(podiumSceneName))
            SceneManager.LoadScene(podiumSceneName);
        else
            victoryPanel.SetActive(true);
    }

    private void FreezeAllPlayers(PlayerController winner)
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(
                FindObjectsSortMode.None
            );

        foreach (PlayerController player in players)
        {
            bool isWinner = player == winner;

            player.FinishRace(isWinner);

            Rigidbody2D rb =
                player.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            player.enabled = false;
        }
    }
}

