using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// Pescaria 1v1: os dois disputam os mesmos peixes do rio. Quem completar a
// sequência primeiro pesca (no mesmo quadro, os dois pontuam). Maior pontuação
// no fim do tempo vence; empate vai a morte súbita, sem lixo no rio.
// Roda depois dos jogadores para P1 não levar vantagem na ordem do Update.
[DefaultExecutionOrder(100)]
public class FishingManager : MonoBehaviour
{
    [Header("Jogadores (ordem P1, P2)")]
    [SerializeField] private FishingPlayer[] players;

    [Header("Rodada")]
    [SerializeField] private float roundTime = 60f;

    [Header("Rio")]
    [SerializeField] private RiverFish fishPrefab;
    [SerializeField] private FishKind[] kinds;
    [SerializeField] private float riverHalfWidth = 2.3f;
    [SerializeField] private float spawnY = 7.5f;
    [SerializeField] private float zoneTop = 4.6f;
    [SerializeField] private float zoneBottom = -5.4f;
    [SerializeField] private float spawnInterval = 1.1f;
    [Tooltip("No máximo 4: cada peixe no rio começa com uma seta diferente.")]
    [SerializeField, Range(1, 4)] private int maxFish = 4;

    [Header("Interface")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string instruction = "DIGITE AS SETAS\nDO PEIXE!";
    [SerializeField] private FinishLine resultScreen;

    [Header("Tempos")]
    [SerializeField] private float instructionDuration = 1.6f;
    [SerializeField] private float stepDuration = 0.7f;
    [SerializeField] private float goDuration = 0.8f;
    [SerializeField] private float suddenDeathDuration = 1.5f;

    // Dados expostos para HUD/feedback (contrato com a UI).
    public event Action RoundStarted;
    public event Action SuddenDeathStarted;
    public event Action<int, FishKind> Caught;       // playerId, tipo
    public event Action<int, int, int> ScoreChanged; // playerId, pontos, variação
    public event Action<int> Missed;                 // playerId

    public bool IsFishing { get; private set; }
    public bool IsSuddenDeath { get; private set; }
    public float RemainingTime { get; private set; }
    public float RoundTime => roundTime;

    private static readonly FishArrow[] AllArrows = (FishArrow[])Enum.GetValues(typeof(FishArrow));

    private readonly List<RiverFish> river = new();
    private readonly List<(FishingPlayer player, RiverFish fish)> completed = new();
    private readonly int[] scores = new int[2];
    private float nextSpawn;
    private float hideStatusAt = float.PositiveInfinity;

    public int GetScore(int playerId) => scores[playerId - 1];

    // Peixe alcançável cuja sequência começa com a seta (o mais perto de escapar).
    public RiverFish FindFish(FishArrow first) => river
        .Where(f => IsReachable(f) && f.Sequence[0] == first)
        .OrderBy(f => f.transform.position.y)
        .FirstOrDefault();

    // Registrado agora e resolvido no Update do manager, depois dos dois jogadores.
    public void Complete(FishingPlayer player, RiverFish fish)
    {
        completed.Add((player, fish));
    }

    public RiverFish Spawn(FishKind kind, FishArrow[] sequence, Vector2 position)
    {
        RiverFish fish = Instantiate(fishPrefab, transform);
        fish.Launch(kind, sequence, position);
        river.Add(fish);
        return fish;
    }

    private IEnumerator Start()
    {
        foreach (FishingPlayer player in players)
        {
            int playerId = player.PlayerId;
            player.Missed += () => Missed?.Invoke(playerId);

            // Parado na margem: o controle de corrida disputaria as mesmas teclas.
            player.Player.FinishRace(false);
            player.Player.enabled = false;
        }

        RemainingTime = roundTime;
        UpdateScoreText();

        yield return ShowStep(instruction, instructionDuration);
        yield return ShowStep("3", stepDuration);
        yield return ShowStep("2", stepDuration);
        yield return ShowStep("1", stepDuration);
        Announce("VAI!", goDuration);

        foreach (FishingPlayer player in players)
            player.Begin(this);
        IsFishing = true;
        RoundStarted?.Invoke();

        while (RemainingTime > 0f)
        {
            yield return null;
            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        }

        if (scores[0] == scores[1])
        {
            IsSuddenDeath = true;
            foreach (RiverFish fish in river.Where(f => f.Kind.IsJunk).ToList())
                Remove(fish);
            Announce("DESEMPATE!\nO PRÓXIMO PEIXE VENCE", suddenDeathDuration);
            SuddenDeathStarted?.Invoke();
            while (scores[0] == scores[1])
                yield return null;
        }

        IsFishing = false;
        SetStatus(null);
        UpdateScoreText();
        foreach (FishingPlayer player in players)
            player.Stop();
        foreach (RiverFish fish in river.ToList())
            Remove(fish);

        resultScreen.DeclareWinner((scores[0] > scores[1] ? players[0] : players[1]).Player);
    }

    private void Update()
    {
        if (!IsFishing)
            return;

        if (Time.time >= hideStatusAt)
            SetStatus(null);

        ResolveCatches();

        foreach (RiverFish fish in river.Where(f => f.transform.position.y < zoneBottom).ToList())
            Remove(fish);

        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + spawnInterval;
            SpawnRandom();
        }

        UpdateScoreText();
    }

    private void ResolveCatches()
    {
        foreach (var group in completed.GroupBy(c => c.fish))
        {
            RiverFish fish = group.Key;
            if (!river.Remove(fish))
                continue;

            foreach (var (player, _) in group)
            {
                player.Release();
                AddPoints(player.PlayerId, fish.Kind.points);
                Caught?.Invoke(player.PlayerId, fish.Kind);
            }

            foreach (FishingPlayer player in players)
                if (player.Target == fish)
                    player.LoseTarget();

            group.First().player.Reel(fish);
        }
        completed.Clear();
    }

    private void AddPoints(int playerId, int points)
    {
        int before = scores[playerId - 1];
        scores[playerId - 1] = Mathf.Max(0, before + points);
        ScoreChanged?.Invoke(playerId, scores[playerId - 1], scores[playerId - 1] - before);
    }

    private void SpawnRandom()
    {
        if (river.Count >= maxFish)
            return;

        // Nunca dois peixes com a mesma primeira seta: a tecla escolhe o peixe.
        FishArrow[] free = AllArrows.Where(a => river.All(f => f.Sequence[0] != a)).ToArray();
        FishKind kind = PickKind();
        if (free.Length == 0 || kind == null)
            return;

        var sequence = new FishArrow[Mathf.Max(1, kind.sequenceLength)];
        sequence[0] = free[UnityEngine.Random.Range(0, free.Length)];
        for (int i = 1; i < sequence.Length; i++)
            sequence[i] = AllArrows[UnityEngine.Random.Range(0, AllArrows.Length)];

        Spawn(kind, sequence,
            new Vector2(UnityEngine.Random.Range(-riverHalfWidth, riverHalfWidth), spawnY));
    }

    private FishKind PickKind()
    {
        FishKind[] options = kinds.Where(k => k.weight > 0f && !(IsSuddenDeath && k.IsJunk)).ToArray();
        float roll = UnityEngine.Random.value * options.Sum(k => k.weight);
        foreach (FishKind kind in options)
        {
            roll -= kind.weight;
            if (roll <= 0f)
                return kind;
        }
        return options.LastOrDefault();
    }

    private void Remove(RiverFish fish)
    {
        river.Remove(fish);
        foreach (FishingPlayer player in players)
            if (player.Target == fish)
                player.LoseTarget();
        Destroy(fish.gameObject);
    }

    private bool IsReachable(RiverFish fish) =>
        !fish.IsCaught && fish.transform.position.y <= zoneTop && fish.transform.position.y >= zoneBottom;

    private void UpdateScoreText()
    {
        if (scoreText == null)
            return;

        string time = IsSuddenDeath ? "DESEMPATE" : Mathf.CeilToInt(RemainingTime).ToString();
        scoreText.text = $"P1  {scores[0]}      {time}      {scores[1]}  P2";
    }

    private IEnumerator ShowStep(string text, float duration)
    {
        SetStatus(text);
        yield return new WaitForSeconds(duration);
    }

    private void Announce(string text, float duration)
    {
        SetStatus(text);
        hideStatusAt = Time.time + duration;
    }

    private void SetStatus(string text)
    {
        hideStatusAt = float.PositiveInfinity;
        if (statusText == null)
            return;

        statusText.gameObject.SetActive(text != null);
        if (text != null)
            statusText.text = text;
    }
}
