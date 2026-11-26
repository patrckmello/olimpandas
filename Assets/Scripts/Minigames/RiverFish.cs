using System;
using System.Collections.Generic;
using UnityEngine;

public enum FishArrow
{
    Up,
    Down,
    Left,
    Right
}

// Tipo de peixe da Pescaria. Pontos negativos = lixo (bota): pescar tira pontos.
[Serializable]
public class FishKind
{
    public string displayName;
    public Sprite sprite;
    public int points = 1;
    public int sequenceLength = 2;
    public float speed = 1.8f;
    [Tooltip("Chance relativa no sorteio.")]
    public float weight = 1f;

    public bool IsJunk => points < 0;
}

// Peixe que desce o rio mostrando a sequência de setas que o pesca.
public class RiverFish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Transform prompt;
    [Tooltip("Modelo desligado: seta com filhos P1 e P2 (marcas de progresso).")]
    [SerializeField] private SpriteRenderer arrowTemplate;
    [Tooltip("Ordem: cima, baixo, esquerda, direita.")]
    [SerializeField] private Sprite[] arrowSprites;
    [SerializeField] private float arrowSpacing = 0.68f;
    [SerializeField] private Color idleMark = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color player1Mark = new Color(0.18f, 0.5f, 0.88f);
    [SerializeField] private Color player2Mark = new Color(0.89f, 0.23f, 0.21f);
    [SerializeField] private float sway = 0.35f;
    [SerializeField] private float catchSpeed = 14f;

    public FishKind Kind { get; private set; }
    public IReadOnlyList<FishArrow> Sequence => sequence;
    public bool IsCaught { get; private set; }

    // Boca do peixe (o desenho olha para a direita).
    public Vector3 HookPoint => new Vector3(body.bounds.max.x - 0.08f, transform.position.y, 0f);

    private readonly List<SpriteRenderer> arrows = new();
    private FishArrow[] sequence = Array.Empty<FishArrow>();
    private float baseX;
    private float swayPhase;
    private Vector3 catchTarget;

    public void Launch(FishKind kind, FishArrow[] arrowSequence, Vector2 position)
    {
        Kind = kind;
        sequence = arrowSequence;
        baseX = position.x;
        swayPhase = position.x;
        transform.position = position;
        body.sprite = kind.sprite;

        arrowTemplate.gameObject.SetActive(false);
        float first = -(sequence.Length - 1) * arrowSpacing / 2f;
        for (int i = 0; i < sequence.Length; i++)
        {
            SpriteRenderer arrow = Instantiate(arrowTemplate, prompt);
            arrow.sprite = arrowSprites[(int)sequence[i]];
            arrow.transform.localPosition = new Vector3(first + i * arrowSpacing, 0f, 0f);
            arrow.gameObject.SetActive(true);
            arrows.Add(arrow);
        }

        SetProgress(1, 0);
        SetProgress(2, 0);
    }

    // Acende as marcas do jogador nas setas que ele já acertou.
    public void SetProgress(int playerId, int count)
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            Transform mark = arrows[i].transform.Find("P" + playerId);
            if (mark != null)
                mark.GetComponent<SpriteRenderer>().color =
                    i < count ? (playerId == 1 ? player1Mark : player2Mark) : idleMark;
        }
    }

    public void CatchTo(Vector3 destination)
    {
        IsCaught = true;
        catchTarget = destination;
        prompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Kind == null)
            return;

        if (IsCaught)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, catchTarget, catchSpeed * Time.deltaTime);
            if (transform.position == catchTarget)
                Destroy(gameObject);
            return;
        }

        float y = transform.position.y - Kind.speed * Time.deltaTime;
        transform.position = new Vector3(baseX + Mathf.Sin(y + swayPhase) * sway, y, transform.position.z);
    }
}
