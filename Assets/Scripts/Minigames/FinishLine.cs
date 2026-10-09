
using System.Collections;
using UnityEngine;
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

    private bool raceFinished = false;

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

        raceFinished = true;

        int playerId = winner.PlayerId;

        bool matchFinished =
            GameManager.Instance.AddMedal(playerId);

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

            nextButtonText.text = "REVANCHE";

            if (changeCharacterButton != null)
            {
                changeCharacterButton.SetActive(true);
            }
        }
        else
        {
            victoryText.text =
                $"PLAYER {playerId}\nVENCEU!";

            nextButtonText.text = "PRÓXIMA PROVA";

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

    private IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(
            victoryDisplayDelay
        );

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

