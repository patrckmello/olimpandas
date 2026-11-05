using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

// Salto a distância 1v1 simultâneo: cada jogador na sua raia,
// N tentativas, maior marca válida vence. Empate gera tentativa extra
// (morte súbita) até existir vencedor.
public class LongJumpManager : MonoBehaviour
{
    private enum JumpState { Running, Airborne, Done }

    private class Jumper
    {
        public PlayerController controller;
        public Rigidbody2D body;
        public Collider2D collider;
        public Vector2 startPosition;
        public JumpState state;
        public float airTime;
        public float lastMark = NoMark;
        public float bestMark = NoMark;
    }

    public const float NoMark = -1f;

    [Header("Jogadores")]
    [SerializeField] private PlayerController[] players;

    [Header("Prova")]
    [SerializeField] private Transform foulLine;
    [SerializeField] private int attempts = 3;
    [SerializeField] private float metersPerUnit = 2f;
    [SerializeField] private float attemptTimeLimit = 6f;
    [SerializeField] private float minAirTime = 0.1f;
    [SerializeField] private float maxAirTime = 2f;

    [Header("Interface")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private FinishLine resultScreen;

    [Header("Tempos")]
    [SerializeField] private float stepDuration = 0.7f;
    [SerializeField] private float markDisplayDuration = 2f;

    // Dados expostos para HUD/feedback (contrato com a UI).
    public event Action<int> AttemptStarted;               // número da tentativa
    public event Action<int, bool, float> AttemptResolved; // playerId, válida, marca (m)

    public int CurrentAttempt { get; private set; }
    public int MaxAttempts => attempts;
    public bool IsTiebreak => CurrentAttempt > attempts;

    private Jumper[] jumpers;

    public float GetBestMark(int playerId)
    {
        Jumper jumper = Find(playerId);
        return jumper != null ? jumper.bestMark : NoMark;
    }

    private IEnumerator Start()
    {
        jumpers = new Jumper[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            jumpers[i] = new Jumper
            {
                controller = players[i],
                body = players[i].GetComponent<Rigidbody2D>(),
                collider = players[i].GetComponent<Collider2D>(),
                startPosition = players[i].transform.position
            };
        }

        for (int attempt = 1; attempt <= attempts; attempt++)
            yield return RunAttempt(attempt);

        Jumper winner = Best(j => j.bestMark);
        int extra = attempts;
        while (winner == null)
        {
            extra++;
            yield return RunAttempt(extra);
            winner = Best(j => j.lastMark);
        }

        SetStatus(null);
        resultScreen.DeclareWinner(winner.controller);
    }

    private IEnumerator RunAttempt(int attempt)
    {
        CurrentAttempt = attempt;

        foreach (Jumper jumper in jumpers)
        {
            Freeze(jumper);
            jumper.body.position = jumper.startPosition;
            // Acorda o corpo para assentar no chão durante a contagem.
            jumper.body.WakeUp();
            jumper.lastMark = NoMark;
        }

        yield return ShowStep(IsTiebreak ? "DESEMPATE" : $"TENTATIVA {attempt}/{attempts}");
        yield return ShowStep("3");
        yield return ShowStep("2");
        yield return ShowStep("1");
        SetStatus("VAI!");

        foreach (Jumper jumper in jumpers)
        {
            jumper.state = JumpState.Running;
            jumper.controller.enabled = true;
        }

        AttemptStarted?.Invoke(attempt);

        float elapsed = 0f;
        while (Array.Exists(jumpers, j => j.state != JumpState.Done))
        {
            yield return null;
            elapsed += Time.deltaTime;

            if (elapsed > stepDuration && statusText != null && statusText.text == "VAI!")
                SetStatus(null);

            foreach (Jumper jumper in jumpers)
                Track(jumper, elapsed);
        }

        SetStatus($"P1  {FormatMark(jumpers[0].lastMark)}\nP2  {FormatMark(jumpers[1].lastMark)}");
        yield return new WaitForSeconds(markDisplayDuration);
    }

    private void Track(Jumper jumper, float elapsed)
    {
        float lineX = foulLine.position.x;

        switch (jumper.state)
        {
            case JumpState.Running:
                // Pisar além da linha (ou sair do chão além dela) é falta.
                if (jumper.collider.bounds.max.x > lineX)
                    Resolve(jumper, NoMark);
                else if (!jumper.controller.IsGrounded)
                {
                    jumper.state = JumpState.Airborne;
                    jumper.airTime = 0f;
                }
                else if (elapsed > attemptTimeLimit)
                    Resolve(jumper, NoMark);
                break;

            case JumpState.Airborne:
                jumper.airTime += Time.deltaTime;
                // Ignora oscilação de contato logo após o pulo.
                if (jumper.airTime >= minAirTime && jumper.controller.IsGrounded)
                {
                    float mark = (jumper.collider.bounds.max.x - lineX) * metersPerUnit;
                    Resolve(jumper, mark > 0f ? mark : NoMark);
                }
                // Caiu fora da pista: a tentativa não pode travar a prova.
                else if (jumper.airTime > maxAirTime)
                    Resolve(jumper, NoMark);
                break;
        }
    }

    private void Resolve(Jumper jumper, float mark)
    {
        jumper.state = JumpState.Done;
        jumper.lastMark = mark;
        if (mark > jumper.bestMark)
            jumper.bestMark = mark;

        Freeze(jumper);
        AttemptResolved?.Invoke(jumper.controller.PlayerId, mark != NoMark, mark);
    }

    private static void Freeze(Jumper jumper)
    {
        jumper.controller.FinishRace(false);
        jumper.controller.enabled = false;
        jumper.body.linearVelocity = Vector2.zero;
    }

    // Retorna o jogador com maior marca (em centímetros) ou null se empatado.
    private Jumper Best(Func<Jumper, float> markOf)
    {
        int a = Centimeters(markOf(jumpers[0]));
        int b = Centimeters(markOf(jumpers[1]));
        if (a == b)
            return null;
        return a > b ? jumpers[0] : jumpers[1];
    }

    private static int Centimeters(float mark) =>
        mark == NoMark ? -1 : Mathf.RoundToInt(mark * 100f);

    private static string FormatMark(float mark) =>
        mark == NoMark
            ? "FALTA"
            : (Centimeters(mark) / 100f).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',') + " m";

    private Jumper Find(int playerId) =>
        jumpers == null ? null : Array.Find(jumpers, j => j.controller.PlayerId == playerId);

    private IEnumerator ShowStep(string text)
    {
        SetStatus(text);
        yield return new WaitForSeconds(stepDuration);
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
