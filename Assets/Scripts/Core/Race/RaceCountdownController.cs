using System.Collections;
using TMPro;
using UnityEngine;

public class RaceCountdownController : MonoBehaviour
{
    [Header("Jogadores")]
    [SerializeField] private PlayerController[] players;

    [Header("UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Cronômetro")]
    [SerializeField] private Timer timer;

    [Header("Tempo")]
    [SerializeField] private float stepDuration = 1f;
    [SerializeField] private float goDuration = 0.8f;

    private IEnumerator Start()
    {
        SetPlayersEnabled(false);

        yield return ShowStep("3");
        yield return ShowStep("2");
        yield return ShowStep("1");

        if (countdownText != null)
        {
            countdownText.text = "VAI!";
            countdownText.gameObject.SetActive(true);
        }

        SetPlayersEnabled(true);

        if (timer != null)
        {
            timer.StartTimer();
        }

        yield return new WaitForSeconds(goDuration);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowStep(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
            countdownText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(stepDuration);
    }

    private void SetPlayersEnabled(bool enabled)
    {
        foreach (PlayerController player in players)
        {
            if (player == null)
                continue;

            Rigidbody2D rb =
                player.GetComponent<Rigidbody2D>();

            if (!enabled && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            player.enabled = enabled;
        }
    }
}