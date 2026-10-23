using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela de seleção de personagens (P1 e P2 independentes, podem repetir).
/// Fica numa cena própria (ex.: "CharacterSelect"), entre MainMenu e HurdlesRace.
/// Usa apenas a API pública do CharacterSelectionManager:
/// Instance, SelectCharacter, GetCharacter, ConfirmSelection, UnlockSelection.
/// </summary>
public class CharacterSelectMenu : MonoBehaviour
{
    [Serializable]
    public class PlayerPanel
    {
        [Tooltip("1 para P1, 2 para P2")]
        public int playerId = 1;

        public Button previousButton;
        public Button nextButton;
        public Button confirmButton;

        public Image previewImage;
        public TMP_Text characterNameLabel;
        public TMP_Text confirmButtonLabel;
        public TMP_Text statusLabel;

        [HideInInspector] public int index;
        [HideInInspector] public bool ready;
    }

    [Header("Painéis")]
    [SerializeField] private PlayerPanel player1 = new PlayerPanel { playerId = 1 };
    [SerializeField] private PlayerPanel player2 = new PlayerPanel { playerId = 2 };

    [Header("Personagens (mesma ordem do enum CharacterType)")]
    [Tooltip("Ordem: PandaGigante, UrsoPolar")]
    [SerializeField] private Sprite[] previews;
    [SerializeField] private string[] displayNames = { "Panda Gigante", "Urso Polar" };

    [Header("Navegação")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private string gameSceneName = "HurdlesRace";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private CharacterType[] characters;

    private void Awake()
    {
        characters = (CharacterType[])Enum.GetValues(typeof(CharacterType));
    }

    private void Start()
    {
        EnsureSelectionManager();

        // Entrando num fluxo de nova escolha: libera a seleção.
        CharacterSelectionManager.Instance.UnlockSelection();

        SetupPanel(player1);
        SetupPanel(player2);

        if (startButton != null) startButton.onClick.AddListener(StartMatch);
        if (backButton != null) backButton.onClick.AddListener(BackToMainMenu);

        RefreshAll();
    }

    // O manager é persistente (DontDestroyOnLoad) e descarta duplicatas.
    // Só criamos um se ele ainda não existir (ex.: abrir esta cena direto no Editor).
    private void EnsureSelectionManager()
    {
        if (CharacterSelectionManager.Instance != null) return;

        var go = new GameObject("CharacterSelectionManager");
        go.AddComponent<CharacterSelectionManager>();
    }

    private void SetupPanel(PlayerPanel p)
    {
        // Parte da escolha atual do manager (se houver), senão do primeiro personagem.
        p.index = Mathf.Max(0, Array.IndexOf(characters,
            CharacterSelectionManager.Instance.GetCharacter(p.playerId)));
        p.ready = false;

        p.previousButton.onClick.AddListener(() => Cycle(p, -1));
        p.nextButton.onClick.AddListener(() => Cycle(p, +1));
        p.confirmButton.onClick.AddListener(() => ToggleReady(p));

        Apply(p);
    }

    private void Cycle(PlayerPanel p, int direction)
    {
        if (p.ready) return;

        p.index = (p.index + direction + characters.Length) % characters.Length;
        Apply(p);
        RefreshAll();
    }

    private void ToggleReady(PlayerPanel p)
    {
        p.ready = !p.ready;
        Apply(p);
        RefreshAll();
    }

    // Grava a escolha do jogador no manager persistente.
    private void Apply(PlayerPanel p)
    {
        CharacterSelectionManager.Instance.SelectCharacter(p.playerId, characters[p.index]);
    }

    private void RefreshAll()
    {
        RefreshPanel(player1);
        RefreshPanel(player2);

        if (startButton != null)
            startButton.interactable = player1.ready && player2.ready;
    }

    private void RefreshPanel(PlayerPanel p)
    {
        if (p.previewImage != null && previews != null && p.index < previews.Length)
            p.previewImage.sprite = previews[p.index];

        if (p.characterNameLabel != null)
            p.characterNameLabel.text = p.index < displayNames.Length
                ? displayNames[p.index]
                : characters[p.index].ToString();

        if (p.confirmButtonLabel != null)
            p.confirmButtonLabel.text = p.ready ? "CANCELAR" : "CONFIRMAR";

        if (p.statusLabel != null)
            p.statusLabel.text = p.ready ? "PRONTO!" : "Escolhendo...";

        p.previousButton.interactable = !p.ready;
        p.nextButton.interactable = !p.ready;
    }

    private void StartMatch()
    {
        if (!(player1.ready && player2.ready)) return;

        // Garante as duas escolhas no manager, trava e carrega a prova.
        var selection = CharacterSelectionManager.Instance;
        selection.SelectCharacter(1, characters[player1.index]);
        selection.SelectCharacter(2, characters[player2.index]);
        selection.ConfirmSelection();

        SceneManager.LoadScene(gameSceneName);
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}