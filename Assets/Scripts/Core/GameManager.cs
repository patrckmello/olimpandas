using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuração da partida")]
    [SerializeField] private int medalsToWin = 2;

    public int Player1Medals { get; private set; }
    public int Player2Medals { get; private set; }

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

        Debug.Log("Nova partida iniciada.");
    }

    public bool IsMatchFinished()
    {
    return Player1Medals >= medalsToWin ||
           Player2Medals >= medalsToWin;
    }
}