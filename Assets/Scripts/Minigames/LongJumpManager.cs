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
        public LongJumpRunner runner;
        public Rigidbody2D body;
        public Collider2D collider;
        public Vector2 startPosition;
        public JumpState state;
        public float airTime;
        public bool perfect;
        public float lastMark = NoMark;
        public float bestMark = NoMark;
    }

    public const float NoMark = -1f;

    [Header("Jogadores (ordem P1, P2)")]
    [SerializeField] private PlayerController[] players;

    [Header("Prova")]
    [SerializeField] private Transform foulLine;
    [SerializeField] private int attempts = 3;
    [SerializeField] private float metersPerUnit = 1.4f;
    [SerializeField] private float attemptTimeLimit = 8f;
    [SerializeField] private float minAirTime = 0.1f;
    [SerializeField] private float maxAirTime = 2f;

    [Header("Tábua perfeita")]
    [SerializeField] private float perfectZone = 0.4f;
    [SerializeField] private float perfectBoost = 1.05f;
    [SerializeField] private SpriteRenderer[] foulBoards;
    [SerializeField] private Color perfectFlashColor = new Color(1f, 0.82f, 0.2f);
    [SerializeField] private float perfectFlashDuration = 0.5f;

    [Header("Areia")]
    [SerializeField] private SpriteRenderer sandPit;
    [SerializeField] private Transform[] bestMarkers;
    [SerializeField] private SpriteRenderer[] landingImprints;
    [SerializeField] private ParticleSystem landingDust;
    [SerializeField] private int dustParticles = 24;

    [Header("Câmera")]
    [SerializeField] private MultiplayerCameraFollow cameraFollow;
    [SerializeField] private float flightZoom = 3.8f;
    [SerializeField] private float slowMotionScale = 0.4f;

    [Header("Interface")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private FinishLine resultScreen;

    [Header("Tempos")]
    [SerializeField] private float stepDuration = 0.7f;
    [SerializeField] private float markDisplayDuration = 2f;

    // Dados expostos para HUD/feedback (contrato com a UI).
    public event Action<int> AttemptStarted;               // número da tentativa
    public event Action<int> PerfectTakeoff;               // playerId
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
                runner = players[i].GetComponent<LongJumpRunner>(),
                body = players[i].GetComponent<Rigidbody2D>(),
                collider = players[i].GetComponent<Collider2D>(),
                startPosition = players[i].transform.position
            };
        }

        foreach (Transform marker in bestMarkers)
            if (marker != null)
                marker.gameObject.SetActive(false);

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

    private void OnDestroy()
    {
        // Nunca deixar a câmera lenta vazar para outra cena.
        Time.timeScale = 1f;
    }

    private IEnumerator RunAttempt(int attempt)
    {
        CurrentAttempt = attempt;
        SetCinematic(false);

        foreach (Jumper jumper in jumpers)
        {
            Freeze(jumper);
            jumper.body.position = jumper.startPosition;
            // Acorda o corpo para assentar no chão durante a contagem.
            jumper.body.WakeUp();
            jumper.lastMark = NoMark;
            jumper.perfect = false;

            // Areia rastelada entre tentativas.
            SpriteRenderer imprint = Lane(landingImprints, jumper);
            if (imprint != null)
                imprint.gameObject.SetActive(false);
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
            jumper.runner.Begin();
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

            // Câmera lenta só quando ninguém mais está decidindo (todos no ar ou prontos).
            SetCinematic(
                !Array.Exists(jumpers, j => j.state == JumpState.Running) &&
                Array.Exists(jumpers, j => j.state == JumpState.Airborne));
        }

        Time.timeScale = 1f;
        SetStatus($"P1  {Describe(jumpers[0])}\nP2  {Describe(jumpers[1])}");
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
                    TakeOff(jumper, lineX - jumper.collider.bounds.max.x);
                else if (elapsed > attemptTimeLimit)
                    Resolve(jumper, NoMark);
                break;

            case JumpState.Airborne:
                jumper.airTime += Time.deltaTime;
                // Ignora oscilação de contato logo após o pulo.
                if (jumper.airTime >= minAirTime && jumper.controller.IsGrounded)
                    Land(jumper, lineX);
                // Caiu fora da pista: a tentativa não pode travar a prova.
                else if (jumper.airTime > maxAirTime)
                    Resolve(jumper, NoMark);
                break;
        }
    }

    private void TakeOff(Jumper jumper, float gapToLine)
    {
        jumper.state = JumpState.Airborne;
        jumper.airTime = 0f;

        if (gapToLine > perfectZone)
            return;

        jumper.perfect = true;
        jumper.runner.Boost(perfectBoost);
        SpriteRenderer board = Lane(foulBoards, jumper);
        if (board != null)
            StartCoroutine(Flash(board));
        PerfectTakeoff?.Invoke(jumper.controller.PlayerId);
    }

    private void Land(Jumper jumper, float lineX)
    {
        Bounds bounds = jumper.collider.bounds;
        float mark = (bounds.max.x - lineX) * metersPerUnit;
        Vector3 feet = new Vector3(bounds.center.x, bounds.min.y, 0f);

        // Poeira e marca só quando cai dentro da caixa de areia.
        if (sandPit == null || (feet.x > sandPit.bounds.min.x && feet.x < sandPit.bounds.max.x))
        {
            if (landingDust != null)
            {
                var emit = new ParticleSystem.EmitParams { position = feet, applyShapeToPosition = true };
                landingDust.Emit(emit, dustParticles);
            }

            SpriteRenderer imprint = Lane(landingImprints, jumper);
            if (imprint != null)
            {
                imprint.transform.position = new Vector3(feet.x, feet.y, imprint.transform.position.z);
                imprint.gameObject.SetActive(true);
            }
        }

        bool valid = mark > 0f;
        if (valid && mark > jumper.bestMark)
        {
            Transform marker = Lane(bestMarkers, jumper);
            if (marker != null)
            {
                marker.position = new Vector3(bounds.max.x, marker.position.y, marker.position.z);
                marker.gameObject.SetActive(true);
            }
        }

        Resolve(jumper, valid ? mark : NoMark);
    }

    // Elemento da raia do jogador num array por raia (ordem P1, P2).
    private T Lane<T>(T[] perLane, Jumper jumper) where T : UnityEngine.Object
    {
        int index = Array.IndexOf(jumpers, jumper);
        return perLane != null && index < perLane.Length ? perLane[index] : null;
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
        if (jumper.runner != null)
            jumper.runner.Stop();
        jumper.controller.FinishRace(false);
        jumper.controller.enabled = false;
        jumper.body.linearVelocity = Vector2.zero;
    }

    private void SetCinematic(bool on)
    {
        Time.timeScale = on ? slowMotionScale : 1f;
        if (cameraFollow != null)
            cameraFollow.SetZoomOverride(on ? flightZoom : (float?)null);
    }

    private IEnumerator Flash(SpriteRenderer board)
    {
        Color original = board.color;
        board.color = perfectFlashColor;
        yield return new WaitForSecondsRealtime(perfectFlashDuration);
        board.color = original;
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

    private static string Describe(Jumper jumper)
    {
        if (jumper.lastMark == NoMark)
            return "FALTA";

        string mark = (Centimeters(jumper.lastMark) / 100f)
            .ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',') + " m";
        return jumper.perfect ? mark + "  PERFEITO!" : mark;
    }

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
