using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pódio depois de cada prova: os blocos sobem, o vencedor recebe a medalha e os
// botões seguem o circuito. Lê tudo do GameManager e do CharacterSelectionManager.
public class PodiumScreen : MonoBehaviour
{
    [Header("Palco")]
    [SerializeField] private Transform firstBlock;
    [SerializeField] private Transform secondBlock;
    [SerializeField] private SpriteRenderer winnerActor;
    [SerializeField] private SpriteRenderer loserActor;
    [SerializeField] private Transform medal;
    [SerializeField] private Transform rays;
    [SerializeField] private ParticleSystem confetti;
    [Tooltip("Ordem do enum CharacterType.")]
    [SerializeField] private GameObject[] characterPrefabs;
    [Tooltip("Ordem do enum CharacterType.")]
    [SerializeField] private MedalAnchors[] medalAnchors;

    [Header("Textos")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Medalhas (por jogador)")]
    [SerializeField] private Image[] medalsP1;
    [SerializeField] private Image[] medalsP2;
    [SerializeField] private Sprite medalOn;
    [SerializeField] private Sprite medalOff;

    [Header("Botões")]
    [SerializeField] private CanvasGroup buttons;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image nextImage;
    [SerializeField] private Sprite nextSprite;
    [SerializeField] private Sprite rematchSprite;
    [SerializeField] private GameObject changeCharacterButton;

    [Header("Tempos")]
    [SerializeField] private float riseDuration = 0.6f;
    [SerializeField] private float medalDuration = 0.7f;
    [SerializeField] private float buttonsDelay = 0.5f;
    [SerializeField] private float victoryInterval = 2.4f;
    [SerializeField] private float raysSpeed = 18f;

    // Centro do peito em cada quadro de vitória, em pixels do quadro de 256 (origem no topo).
    [Serializable]
    public class MedalAnchors
    {
        public Vector2[] victory;
    }

    private const float FrameSize = 256f;

    public bool IsReady { get; private set; }

    private Animator winnerAnimator;
    private Vector2[] chest;
    private float medalDrop;
    private bool medalActive;

    private IEnumerator Start()
    {
        GameManager game = GameManager.Instance;
        bool hasResult = game != null && game.Results.Count > 0;
        MinigameResult result = hasResult ? game.Results[game.Results.Count - 1] : new MinigameResult("", 1);
        int winner = result.WinnerId;
        bool champion = hasResult && game.HasWinner(winner);

        winnerAnimator = Dress(winnerActor, winner);
        chest = medalAnchors[(int)CharacterOf(winner)].victory;
        Dress(loserActor, 3 - winner);

        titleText.text = champion ? $"JOGADOR {winner} É CAMPEÃO!" : $"JOGADOR {winner} VENCEU!";
        Minigame minigame = MinigameCatalog.All.FirstOrDefault(m => m.SceneName == result.SceneName);
        subtitleText.text = champion ? "CIRCUITO OLÍMPICO" : minigame?.DisplayName.ToUpperInvariant() ?? "PÓDIO";
        if (!champion && result.Detail != "")
            subtitleText.text += "  •  " + result.Detail;

        nextImage.sprite = champion ? rematchSprite : nextSprite;
        changeCharacterButton.SetActive(champion);
        buttons.alpha = 0f;
        buttons.interactable = false;

        // Placar antes da medalha nova; ela entra junto com a animação.
        int before1 = hasResult ? game.GetMedals(1) - (winner == 1 ? 1 : 0) : 0;
        int before2 = hasResult ? game.GetMedals(2) - (winner == 2 ? 1 : 0) : 0;
        ShowMedals(medalsP1, before1);
        ShowMedals(medalsP2, before2);

        medal.gameObject.SetActive(false);
        rays.gameObject.SetActive(false);

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetDucked(true);
        // O acento da fanfarra (1,1 s) cai junto com os confetes.
        if (champion)
            Sfx.Play(SfxId.Champion, delay: riseDuration + 0.2f + medalDuration - 1.1f);

        StartCoroutine(Rise(secondBlock, loserActor.transform, 0.15f));
        yield return Rise(firstBlock, winnerActor.transform, 0f);
        yield return new WaitForSeconds(0.2f);

        rays.gameObject.SetActive(true);
        if (!champion)
            Sfx.Play(SfxId.EventWon);
        StartCoroutine(Celebrate());
        yield return DropMedal();

        confetti.Play();
        Sfx.Play(SfxId.CrowdCheer, 0.7f);
        if (champion)
        {
            // Campeão: a festa não para.
            ParticleSystem.MainModule main = confetti.main;
            main.loop = true;
        }

        Image[] slots = winner == 1 ? medalsP1 : medalsP2;
        int index = Mathf.Min(winner == 1 ? before1 : before2, slots.Length - 1);
        slots[index].sprite = medalOn;
        yield return Animate(0.4f, t =>
            slots[index].transform.localScale = Vector3.one * (1f + 0.6f * Mathf.Sin(t * Mathf.PI)));

        yield return new WaitForSeconds(buttonsDelay);
        buttons.interactable = true;
        yield return Animate(0.25f, t => buttons.alpha = t);

        // Enter/Espaço já avançam, sem precisar do mouse.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        IsReady = true;
    }

    private void Update()
    {
        rays.Rotate(0f, 0f, raysSpeed * Time.deltaTime);
    }

    // Depois do Animator: a medalha acompanha o peito quadro a quadro.
    private void LateUpdate()
    {
        if (medalActive)
            medal.position = ChestPosition() + Vector3.up * medalDrop;
    }

    private Vector3 ChestPosition()
    {
        string sprite = winnerActor.sprite.name;
        int frame = sprite.Contains("Victory")
            ? int.Parse(sprite.Substring(sprite.LastIndexOf('_') + 1))
            : 0;
        Vector2 pixel = chest[Mathf.Min(frame, chest.Length - 1)];
        Vector3 scale = winnerActor.transform.lossyScale;
        return winnerActor.transform.position + new Vector3(
            (pixel.x / FrameSize - 0.5f) * scale.x,
            (0.5f - pixel.y / FrameSize) * scale.y,
            0f);
    }

    private static CharacterType CharacterOf(int playerId) => CharacterSelectionManager.Instance != null
        ? CharacterSelectionManager.Instance.GetCharacter(playerId)
        : CharacterType.PandaGigante;

    // Veste o ator com a aparência do personagem escolhido pelo jogador.
    private Animator Dress(SpriteRenderer actor, int playerId)
    {
        Transform visual = characterPrefabs[(int)CharacterOf(playerId)].transform.Find("Visual");
        Animator animator = actor.GetComponent<Animator>();
        actor.sprite = visual.GetComponent<SpriteRenderer>().sprite;
        animator.runtimeAnimatorController = visual.GetComponent<Animator>().runtimeAnimatorController;
        animator.SetBool("isGrounded", true);
        return animator;
    }

    private IEnumerator Rise(Transform block, Transform actor, float delay)
    {
        Vector3 blockEnd = block.position, actorEnd = actor.position;
        Vector3 down = Vector3.down * 8f;
        block.position = blockEnd + down;
        actor.position = actorEnd + down;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return Animate(riseDuration, t =>
        {
            Vector3 offset = Vector3.LerpUnclamped(down, Vector3.zero, EaseOutBack(t));
            block.position = blockEnd + offset;
            actor.position = actorEnd + offset;
        });
    }

    private IEnumerator DropMedal()
    {
        Vector3 scale = medal.localScale;
        medalDrop = 5f;
        medalActive = true;
        medal.gameObject.SetActive(true);
        yield return Animate(medalDuration, t =>
        {
            medalDrop = 5f * (1f - EaseOutBack(t));
            medal.localScale = scale * Mathf.Lerp(1.8f, 1f, t);
        });
    }

    private IEnumerator Celebrate()
    {
        winnerAnimator.SetTrigger("Victory");
        while (true)
        {
            yield return new WaitForSeconds(victoryInterval);
            // O estado de vitória não tem saída: recomeça o clipe.
            winnerAnimator.Play(winnerAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        }
    }

    private void ShowMedals(Image[] slots, int medals)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].sprite = i < medals ? medalOn : medalOff;
    }

    private static IEnumerator Animate(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.4f;
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }
}
