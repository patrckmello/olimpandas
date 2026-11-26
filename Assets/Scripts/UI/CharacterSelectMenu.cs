using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela de seleção de personagens (P1 e P2 ao mesmo tempo, podem repetir).
/// Cada jogador usa as próprias teclas; os botões continuam funcionando no mouse.
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
        public GameObject readyStamp;
        [Tooltip("Cursor deste jogador em cada carta, na ordem do enum.")]
        public GameObject[] cursors;

        [Header("Teclado")]
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode confirmKey = KeyCode.W;
        public KeyCode cancelKey = KeyCode.S;

        [HideInInspector] public int index;
        [HideInInspector] public bool ready;
        [NonSerialized] public float animationTime;
        [NonSerialized] public float punch;
        [NonSerialized] public Vector3 previewScale;
    }

    [Serializable]
    public class CharacterEntry
    {
        public string displayName;
        public Sprite[] idle;
        public Sprite[] victory;
    }

    [Header("Painéis")]
    [SerializeField] private PlayerPanel player1 = new PlayerPanel { playerId = 1 };
    [SerializeField] private PlayerPanel player2 = new PlayerPanel { playerId = 2 };

    [Header("Personagens (mesma ordem do enum CharacterType)")]
    [SerializeField] private CharacterEntry[] entries;
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float victoryFps = 5f;

    [Header("Navegação")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private string gameSceneName = "CircuitDraw";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private CharacterType[] characters;
    private bool navigating;

    private void Awake()
    {
        characters = (CharacterType[])Enum.GetValues(typeof(CharacterType));
        if (startButton != null) startButton.interactable = false;
    }

    private void Start()
    {
        if (!IsPanelConfigured(player1, 1) || !IsPanelConfigured(player2, 2) ||
            startButton == null || backButton == null)
        {
            Debug.LogError("Referências obrigatórias da seleção não configuradas.", this);
            enabled = false;
            return;
        }

        EnsureSelectionManager();
        // Uma entrada na seleção inicia uma nova MD3, inclusive após voltar ao menu.
        if (GameManager.Instance != null) GameManager.Instance.ResetMatch();

        // Entrando num fluxo de nova escolha: libera a seleção.
        CharacterSelectionManager.Instance.UnlockSelection();

        SetupPanel(player1);
        SetupPanel(player2);

        startButton.onClick.AddListener(StartMatch);
        backButton.onClick.AddListener(BackToMainMenu);

        RefreshAll();
    }

    private static bool IsPanelConfigured(PlayerPanel panel, int playerId)
    {
        return panel != null && panel.playerId == playerId &&
            panel.previousButton != null && panel.nextButton != null &&
            panel.confirmButton != null;
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
        if (p.previewImage != null)
            p.previewScale = p.previewImage.transform.localScale;

        p.previousButton.onClick.AddListener(() => Cycle(p, -1));
        p.nextButton.onClick.AddListener(() => Cycle(p, +1));
        p.confirmButton.onClick.AddListener(() => ToggleReady(p));

        Apply(p);
    }

    private void Update()
    {
        Animate(player1);
        Animate(player2);

        // Nada fica selecionado: senão as setas e o Enter também mexeriam nos botões.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        ReadKeys(player1);
        ReadKeys(player2);

        if (Input.GetKeyDown(KeyCode.Escape))
            BackToMainMenu();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                 Input.GetKeyDown(KeyCode.Space))
            StartMatch();
    }

    private void ReadKeys(PlayerPanel p)
    {
        if (Input.GetKeyDown(p.leftKey)) Cycle(p, -1);
        if (Input.GetKeyDown(p.rightKey)) Cycle(p, +1);
        if (Input.GetKeyDown(p.confirmKey) && !p.ready) ToggleReady(p);
        if (Input.GetKeyDown(p.cancelKey) && p.ready) ToggleReady(p);
    }

    private void Cycle(PlayerPanel p, int direction)
    {
        if (p.ready || navigating) return;

        p.index = (p.index + direction + characters.Length) % characters.Length;
        Sfx.Play(SfxId.UiConfirm, 0.6f);
        Restart(p, 0.1f);
        Apply(p);
        RefreshAll();
    }

    private void ToggleReady(PlayerPanel p)
    {
        if (navigating) return;

        p.ready = !p.ready;
        Sfx.Play(p.ready ? SfxId.Ready : SfxId.UiBack);
        Restart(p, 0.16f);
        Apply(p);
        RefreshAll();
    }

    private static void Restart(PlayerPanel p, float punch)
    {
        p.animationTime = 0f;
        p.punch = punch;
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

        startButton.interactable = player1.ready && player2.ready;
    }

    private void RefreshPanel(PlayerPanel p)
    {
        if (p.characterNameLabel != null)
            p.characterNameLabel.text = entries != null && p.index < entries.Length
                ? entries[p.index].displayName
                : characters[p.index].ToString();

        if (p.confirmButtonLabel != null)
            p.confirmButtonLabel.text = p.ready ? "CANCELAR" : "CONFIRMAR";

        if (p.statusLabel != null)
            p.statusLabel.text = p.ready ? "PRONTO!" : "Escolhendo...";

        if (p.readyStamp != null)
            p.readyStamp.SetActive(p.ready);

        if (p.cursors != null)
            for (int i = 0; i < p.cursors.Length; i++)
                p.cursors[i].SetActive(i == p.index);

        p.previousButton.interactable = !p.ready;
        p.nextButton.interactable = !p.ready;
    }

    // Idle enquanto escolhe, comemoração quando confirma.
    private void Animate(PlayerPanel p)
    {
        if (p.previewImage == null || entries == null || p.index >= entries.Length)
            return;

        Sprite[] frames = p.ready ? entries[p.index].victory : entries[p.index].idle;
        if (frames == null || frames.Length == 0)
            return;

        p.animationTime += Time.deltaTime;
        int frame = (int)(p.animationTime * (p.ready ? victoryFps : idleFps));
        p.previewImage.sprite = frames[frame % frames.Length];

        p.punch = Mathf.MoveTowards(p.punch, 0f, 0.6f * Time.deltaTime);
        p.previewImage.transform.localScale = p.previewScale * (1f + p.punch);
    }

    private void StartMatch()
    {
        if (navigating || !(player1.ready && player2.ready)) return;
        navigating = true;
        startButton.interactable = false;
        Sfx.Play(SfxId.UiConfirm);

        // Garante as duas escolhas no manager, trava e carrega a prova.
        var selection = CharacterSelectionManager.Instance;
        selection.SelectCharacter(1, characters[player1.index]);
        selection.SelectCharacter(2, characters[player2.index]);
        selection.ConfirmSelection();

        SceneManager.LoadScene(gameSceneName);
    }

    private void BackToMainMenu()
    {
        if (navigating) return;
        navigating = true;
        Sfx.Play(SfxId.UiBack);
        CharacterSelectionManager.Instance.UnlockSelection();
        if (GameManager.Instance != null) GameManager.Instance.ResetMatch();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
