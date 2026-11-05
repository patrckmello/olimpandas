using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Modalidade já disputada no circuito e quem levou a medalha.
public readonly struct MinigameResult
{
    public readonly string SceneName;
    public readonly int WinnerId;

    public MinigameResult(string sceneName, int winnerId)
    {
        SceneName = sceneName;
        WinnerId = winnerId;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuração da partida")]
    [SerializeField] private int medalsToWin = 2;

    public int Player1Medals { get; private set; }
    public int Player2Medals { get; private set; }

    private readonly List<MinigameResult> results = new();
    private readonly System.Random random = new();

    // Histórico do circuito atual, na ordem das provas.
    public IReadOnlyList<MinigameResult> Results => results;

    private void Awake()
    {
        // Garante que exista apenas um GameManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mantém o placar quando trocarmos de prova/cena
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool AddMedal(int playerId)
    {
        if (playerId == 1)
        {
            Player1Medals++;
        }
        else if (playerId == 2)
        {
            Player2Medals++;
        }

        Debug.Log($"PLACAR: P1 {Player1Medals} x {Player2Medals} P2");

        return HasWinner(playerId);
    }

    // Só o vencedor da modalidade ganha medalha do circuito.
    public bool AwardMinigame(string sceneName, int playerId)
    {
        results.Add(new MinigameResult(sceneName, playerId));
        return AddMedal(playerId);
    }

    // Próxima modalidade do circuito (null se nenhuma estiver disponível).
    public Minigame DrawNextMinigame()
    {
        return MinigameRotation.Draw(
            MinigameCatalog.Available(),
            results.Select(r => r.SceneName).ToList(),
            random
        );
    }

    public int GetMedals(int playerId)
    {
        return playerId == 1 ? Player1Medals : Player2Medals;
    }

    public bool HasWinner(int playerId)
    {
        return GetMedals(playerId) >= medalsToWin;
    }

    public string GetScore()
    {
        return $"{Player1Medals} x {Player2Medals}";
    }

    public void ResetMatch()
    {
        Player1Medals = 0;
        Player2Medals = 0;
        results.Clear();

        Debug.Log("Nova partida iniciada.");
    }

    public bool IsMatchFinished()
    {
    return Player1Medals >= medalsToWin ||
           Player2Medals >= medalsToWin;
    }
}