using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Canoagem 1v1: cada canoa na sua raia do rio, obstáculos iguais nas duas.
// Primeira proa a cruzar a chegada vence. No tempo limite vence quem estiver
// na frente; empatados, a prova segue até alguém abrir vantagem.
public class CanoeRaceManager : MonoBehaviour
{
    [Header("Canoas (ordem P1, P2)")]
    [SerializeField] private CanoePaddler[] canoes;

    [Header("Prova")]
    [SerializeField] private Transform finish;
    [SerializeField] private float timeLimit = 60f;

    [Header("Interface")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string instruction = "ALTERNE OS LADOS\nPARA REMAR!";
    [SerializeField] private FinishLine resultScreen;
    [SerializeField] private Timer timer;

    [Header("Tempos")]
    [SerializeField] private float instructionDuration = 1.6f;
    [SerializeField] private float stepDuration = 0.7f;
    [SerializeField] private float goDuration = 0.8f;

    // Dados expostos para HUD/feedback (contrato com a UI).
    public event Action RaceStarted;
    public event Action<int> Collided; // playerId

    public bool IsRacing { get; private set; }
    public float ElapsedTime { get; private set; }
    public float TimeLimit => timeLimit;

    private float startX;

    // 0 na largada, 1 na chegada.
    public float GetProgress(int playerId)
    {
        CanoePaddler canoe = Find(playerId);
        return canoe == null ? 0f : Mathf.InverseLerp(startX, finish.position.x, canoe.BowX);
    }

    // 0 parada, 1 na velocidade máxima.
    public float GetSpeedRatio(int playerId)
    {
        CanoePaddler canoe = Find(playerId);
        return canoe == null ? 0f : canoe.Speed / canoe.MaxSpeed;
    }

    private IEnumerator Start()
    {
        foreach (CanoePaddler canoe in canoes)
        {
            int playerId = canoe.PlayerId;
            canoe.Hit += () => Collided?.Invoke(playerId);

            // O controle de corrida brigaria com a canoa pela velocidade do corpo.
            canoe.Player.FinishRace(false);
            canoe.Player.enabled = false;
            canoe.Stop();
        }

        startX = canoes[0].BowX;

        yield return ShowStep(instruction, instructionDuration);
        yield return ShowStep("3", stepDuration);
        yield return ShowStep("2", stepDuration);
        yield return ShowStep("1", stepDuration);
        SetStatus("VAI!");
        Sfx.Countdown("VAI!");

        foreach (CanoePaddler canoe in canoes)
            canoe.Begin();
        if (timer != null)
            timer.StartTimer();
        IsRacing = true;
        RaceStarted?.Invoke();

        CanoePaddler winner = null;
        while (winner == null)
        {
            // Confere depois do passo de física para as duas canoas contarem igual.
            yield return new WaitForFixedUpdate();
            ElapsedTime += Time.fixedDeltaTime;

            if (ElapsedTime > goDuration)
                SetStatus(null);

            winner = FindWinner();
        }

        IsRacing = false;
        SetStatus(null);
        foreach (CanoePaddler canoe in canoes)
            canoe.Stop();
        resultScreen.DeclareWinner(winner.Player, Timer.Format(ElapsedTime));
    }

    private CanoePaddler FindWinner()
    {
        float a = canoes[0].BowX;
        float b = canoes[1].BowX;
        bool finished = Mathf.Max(a, b) >= finish.position.x;

        // Empate exato: segue até alguém abrir vantagem.
        if ((!finished && ElapsedTime < timeLimit) || Mathf.Approximately(a, b))
            return null;

        return a > b ? canoes[0] : canoes[1];
    }

    private CanoePaddler Find(int playerId) =>
        Array.Find(canoes, c => c.PlayerId == playerId);

    private IEnumerator ShowStep(string text, float duration)
    {
        Sfx.Countdown(text);
        SetStatus(text);
        yield return new WaitForSeconds(duration);
    }

    private void SetStatus(string text)
    {
        if (statusText == null)
            return;

        statusText.gameObject.SetActive(text != null);
        if (text != null)
            statusText.text = text;
    }
}
