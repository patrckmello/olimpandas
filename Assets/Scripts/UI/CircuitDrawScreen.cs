using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sorteio entre provas do Circuito Olímpico: o carrossel de cartas gira, freia
// na modalidade sorteada e mostra objetivo e controles antes de começar.
public class CircuitDrawScreen : MonoBehaviour
{
    [Serializable]
    public class EventCard
    {
        public string sceneName;
        public Sprite art;
    }

    [Header("Textos")]
    [SerializeField] private TMP_Text eventText;
    [SerializeField] private TMP_Text minigameText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Medalhas (por jogador)")]
    [SerializeField] private Image[] medalsP1;
    [SerializeField] private Image[] medalsP2;
    [SerializeField] private Sprite medalOn;
    [SerializeField] private Sprite medalOff;

    [Header("Carrossel")]
    [SerializeField] private RectTransform strip;
    [Tooltip("Modelo desligado: Image com a arte e um TMP_Text filho com o nome.")]
    [SerializeField] private RectTransform cardTemplate;
    [SerializeField] private EventCard[] cards;
    [SerializeField] private RectTransform pointer;
    [SerializeField] private float cardSpacing = 440f;
    [Tooltip("Quantas cartas passam, no mínimo, antes de parar.")]
    [SerializeField] private int minimumCards = 18;
    [SerializeField] private float spinDuration = 2.8f;
    [SerializeField] private float revealDuration = 0.45f;

    [Header("Instruções")]
    [SerializeField] private CanvasGroup infoPanel;
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private TMP_Text controlsP1Text;
    [SerializeField] private TMP_Text controlsP2Text;
    [SerializeField] private float infoDuration = 4.5f;
    [SerializeField] private string fallbackSceneName = "MainMenu";

    private readonly List<RectTransform> spawned = new();
    private int pointedCard = -1;

    private IEnumerator Start()
    {
        GameManager game = GameManager.Instance;
        Minigame next = game.DrawNextMinigame();

        eventText.text = $"PROVA {game.Results.Count + 1}";
        scoreText.text = "2 MEDALHAS VENCEM O CIRCUITO";
        minigameText.text = "SORTEANDO...";
        ShowMedals(medalsP1, game.GetMedals(1));
        ShowMedals(medalsP2, game.GetMedals(2));
        infoPanel.alpha = 0f;

        if (next == null)
        {
            Debug.LogError("Nenhuma modalidade disponível no Build Settings para o circuito.");
            minigameText.text = "NENHUMA PROVA DISPONÍVEL";
            yield return new WaitForSeconds(infoDuration);
            SceneManager.LoadScene(fallbackSceneName);
            yield break;
        }

        int stop = BuildStrip(next);
        yield return Spin(stop);
        yield return Reveal(next, stop);

        // Dá para ler com calma ou pular com Enter/Espaço.
        float end = Time.time + infoDuration;
        while (Time.time < end && !Input.GetKeyDown(KeyCode.Return) &&
               !Input.GetKeyDown(KeyCode.KeypadEnter) && !Input.GetKeyDown(KeyCode.Space))
            yield return null;

        SceneManager.LoadScene(next.SceneName);
    }

    // Monta a fileira repetindo as provas e devolve o índice onde ela deve parar.
    private int BuildStrip(Minigame next)
    {
        List<Minigame> pool = MinigameCatalog.Available();
        int target = pool.FindIndex(m => m.SceneName == next.SceneName);
        int stop = minimumCards;
        while (stop % pool.Count != target)
            stop++;

        cardTemplate.gameObject.SetActive(false);
        for (int i = 0; i <= stop + 3; i++)
        {
            Minigame minigame = pool[i % pool.Count];
            RectTransform card = Instantiate(cardTemplate, strip);
            card.anchoredPosition = new Vector2(i * cardSpacing, 0f);
            EventCard art = Array.Find(cards, c => c.sceneName == minigame.SceneName);
            if (art != null)
                card.GetComponent<Image>().sprite = art.art;
            card.GetComponentInChildren<TMP_Text>(true).text = minigame.DisplayName.ToUpperInvariant();
            card.gameObject.SetActive(true);
            spawned.Add(card);
        }
        return stop;
    }

    private IEnumerator Spin(int stop)
    {
        float from = 2f;
        for (float t = 0f; t < spinDuration; t += Time.deltaTime)
        {
            float eased = 1f - Mathf.Pow(1f - t / spinDuration, 4f);
            Place(Mathf.Lerp(from, stop, eased), 1f - eased);
            yield return null;
        }
        Place(stop, 0f);
    }

    private void Place(float position, float speed)
    {
        strip.anchoredPosition = new Vector2(-position * cardSpacing, strip.anchoredPosition.y);
        for (int i = 0; i < spawned.Count; i++)
            spawned[i].localScale = Vector3.one * Mathf.Lerp(1.08f, 0.82f, Mathf.Abs(i - position));

        int card = Mathf.RoundToInt(position);
        if (card != pointedCard)
        {
            pointedCard = card;
            Sfx.Play(SfxId.DrawTick, 0.6f);
        }

        // O ponteiro bate em cada carta que passa.
        if (pointer != null)
            pointer.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Abs(Mathf.Sin(position * Mathf.PI)) * 16f * Mathf.Clamp01(speed * 3f));
    }

    private IEnumerator Reveal(Minigame next, int stop)
    {
        minigameText.text = next.DisplayName.ToUpperInvariant();
        Sfx.Play(SfxId.DrawReveal);
        goalText.text = next.Goal;
        controlsP1Text.text = next.ControlsP1;
        controlsP2Text.text = next.ControlsP2;

        for (float t = 0f; t < revealDuration; t += Time.deltaTime)
        {
            ShowReveal(t / revealDuration, stop);
            yield return null;
        }
        ShowReveal(1f, stop);
    }

    private void ShowReveal(float t, int stop)
    {
        infoPanel.alpha = t;
        for (int i = 0; i < spawned.Count; i++)
        {
            if (i == stop)
                // Passa um pouco do tamanho final e volta.
                spawned[i].localScale = Vector3.one * (1.08f + 0.1f * t + 0.16f * Mathf.Sin(t * Mathf.PI));
            else
                spawned[i].GetComponent<Image>().color = Color.Lerp(Color.white, new Color(0.45f, 0.4f, 0.55f), t);
        }
        spawned[stop].SetAsLastSibling();
    }

    private void ShowMedals(Image[] slots, int medals)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].sprite = i < medals ? medalOn : medalOff;
    }
}
