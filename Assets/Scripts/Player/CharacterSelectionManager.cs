
using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    public static CharacterSelectionManager Instance
    {
        get;
        private set;
    }

    [Header("Personagens selecionados")]
    [SerializeField] private CharacterType player1Character =
        CharacterType.PandaGigante;

    [SerializeField] private CharacterType player2Character =
        CharacterType.UrsoPolar;

    public CharacterType Player1Character => player1Character;
    public CharacterType Player2Character => player2Character;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public CharacterType GetCharacter(int playerId)
    {
        return playerId == 2
            ? player2Character
            : player1Character;
    }

    public void SelectCharacter(
        int playerId,
        CharacterType character)
    {
        if (playerId == 1)
            player1Character = character;
        else if (playerId == 2)
            player2Character = character;
    }
}

