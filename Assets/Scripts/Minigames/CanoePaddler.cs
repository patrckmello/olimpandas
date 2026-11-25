using System;
using UnityEngine;

// Canoa da prova de Canoagem. Cada remada empurra e vira a proa para o lado
// oposto: alternar mantém a canoa reta e rápida, insistir num lado só rende
// pouco e tira a canoa do rumo. Cima/baixo desvia dos obstáculos.
// O PlayerController fica desligado; ele só fornece as teclas e o personagem.
[RequireComponent(typeof(PlayerController), typeof(Rigidbody2D))]
public class CanoePaddler : MonoBehaviour
{
    public const int Left = -1;
    public const int Right = 1;

    [Header("Controles de desvio")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;

    [Header("Remada")]
    [SerializeField] private float strokeBoost = 1.3f;
    [Tooltip("Remar mais rápido que isto não rende mais: o impulso é proporcional ao intervalo.")]
    [SerializeField] private float idealInterval = 0.22f;
    [SerializeField, Range(0f, 1f)] private float sameSideEfficiency = 0.35f;
    [SerializeField] private float drag = 0.8f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float riverCurrent = 0.8f;
    [Tooltip("Distância do centro até a ponta da proa, usada na chegada.")]
    [SerializeField] private float bowOffset = 1.1f;

    [Header("Rumo (graus)")]
    [SerializeField] private float turnPerStroke = 5f;
    [SerializeField] private float maxHeading = 30f;
    [SerializeField] private float headingRecovery = 14f;

    [Header("Desvio e margens")]
    [SerializeField] private float steerSpeed = 2.4f;
    [SerializeField] private float laneHalfHeight = 1.5f;
    [SerializeField] private float bankDrag = 1.5f;

    [Header("Colisão")]
    [SerializeField] private float hitPush = 3f;
    [SerializeField] private float hitPushDecay = 6f;
    [SerializeField] private float hitRecovery = 0.9f;
    [SerializeField] private Color hitColor = new Color(1f, 0.45f, 0.4f);

    [Header("Visual (opcional)")]
    [SerializeField] private SpriteRenderer hull;
    [SerializeField] private Transform paddle;
    [SerializeField] private float paddleAngle = 35f;
    [SerializeField] private float paddleSpeed = 14f;
    [SerializeField] private ParticleSystem splash;
    [SerializeField] private int strokeParticles = 6;
    [SerializeField] private int hitParticles = 18;

    public event Action<int> Stroked; // lado (-1 esquerda, 1 direita)
    public event Action Hit;

    public int PlayerId => player.PlayerId;
    public PlayerController Player => player;
    public float Speed { get; private set; }
    public float MaxSpeed => maxSpeed;
    public float Heading { get; private set; }
    // Ponto fixo: a caixa do collider muda de largura quando a canoa gira.
    public float BowX => body.position.x + bowOffset;
    public bool InputEnabled { get; set; } = true;

    private PlayerController player;
    private Rigidbody2D body;
    private float laneCenterY;
    private bool racing;
    private int lastSide;
    private float lastStrokeTime;
    private float steer;
    private float push;
    private float lastHitTime;
    private Color hullColor;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        laneCenterY = transform.position.y;
        if (hull != null)
            hullColor = hull.color;
    }

    public void Begin()
    {
        Speed = 0f;
        Heading = 0f;
        lastSide = 0;
        steer = 0f;
        push = 0f;
        lastStrokeTime = float.NegativeInfinity;
        lastHitTime = float.NegativeInfinity;
        racing = true;
    }

    public void Stop()
    {
        racing = false;
        Speed = 0f;
        Heading = 0f;
        body.linearVelocity = Vector2.zero;
        body.SetRotation(0f);
        if (hull != null)
            hull.color = hullColor;
    }

    public void RegisterStroke(int side)
    {
        if (!racing)
            return;

        // Sem teto por toque, só por ritmo: metralhar a tecla não passa da cadência ideal.
        float rhythm = Mathf.Clamp01((Time.time - lastStrokeTime) / idealInterval);
        float efficiency = side == lastSide ? sameSideEfficiency : 1f;
        Speed = Mathf.Min(maxSpeed, Speed + strokeBoost * rhythm * efficiency);

        // Remar à esquerda joga a proa para a direita (baixo na tela) e vice-versa.
        Heading = Mathf.Clamp(Heading + side * turnPerStroke, -maxHeading, maxHeading);

        lastSide = side;
        lastStrokeTime = Time.time;
        Emit(strokeParticles);
        Sfx.Play(side == Left ? SfxId.PaddleLeft : SfxId.PaddleRight, 0.7f);
        Stroked?.Invoke(side);
    }

    public void SetSteer(float direction)
    {
        steer = Mathf.Clamp(direction, -1f, 1f);
    }

    private void Update()
    {
        UpdateVisual();

        if (!racing || !InputEnabled)
            return;

        if (Input.GetKeyDown(player.LeftKey))
            RegisterStroke(Left);
        if (Input.GetKeyDown(player.RightKey))
            RegisterStroke(Right);

        SetSteer((Input.GetKey(upKey) ? 1f : 0f) - (Input.GetKey(downKey) ? 1f : 0f));
    }

    private void FixedUpdate()
    {
        if (!racing || !body.simulated)
            return;

        float dt = Time.fixedDeltaTime;
        Speed = Mathf.Max(0f, Speed - Speed * drag * dt);
        Heading = Mathf.MoveTowards(Heading, 0f, headingRecovery * dt);
        push = Mathf.MoveTowards(push, 0f, hitPushDecay * dt);

        float radians = Heading * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(
            Mathf.Cos(radians) * Speed + riverCurrent,
            Mathf.Sin(radians) * Speed + steer * steerSpeed + push);

        // A raia é o limite: encostar na margem tira velocidade e endireita a canoa.
        float nextY = body.position.y + velocity.y * dt;
        float clampedY = Mathf.Clamp(nextY, laneCenterY - laneHalfHeight, laneCenterY + laneHalfHeight);
        if (!Mathf.Approximately(nextY, clampedY))
        {
            velocity.y = (clampedY - body.position.y) / dt;
            Speed = Mathf.Max(0f, Speed - Speed * bankDrag * dt);
            Heading = Mathf.MoveTowards(Heading, 0f, 4f * headingRecovery * dt);
        }

        body.linearVelocity = velocity;
        body.SetRotation(Heading);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!racing || Time.time - lastHitTime < hitRecovery)
            return;

        CanoeObstacle obstacle = other.GetComponentInParent<CanoeObstacle>();
        if (obstacle == null)
            return;

        lastHitTime = Time.time;
        Speed *= obstacle.SpeedMultiplier;

        // Desvia para o lado livre; batida em cheio vai para o centro da raia.
        float offset = body.position.y - other.bounds.center.y;
        if (Mathf.Abs(offset) < 0.05f)
            offset = laneCenterY - body.position.y;
        push = (offset >= 0f ? 1f : -1f) * hitPush;

        Emit(hitParticles);
        Sfx.Play(SfxId.CanoeHit);
        Sfx.PlayVoice(PlayerId, "dano");
        Hit?.Invoke();
    }

    private void UpdateVisual()
    {
        if (paddle != null)
        {
            // Pá para cima quando rema à esquerda, para baixo quando rema à direita.
            Quaternion target = Quaternion.Euler(0f, 0f, -lastSide * paddleAngle);
            paddle.localRotation = Quaternion.Slerp(paddle.localRotation, target, paddleSpeed * Time.deltaTime);
        }

        if (hull != null && racing)
        {
            float left = hitRecovery - (Time.time - lastHitTime);
            hull.color = left > 0f
                ? Color.Lerp(hullColor, hitColor, left / hitRecovery)
                : hullColor;
        }
    }

    private void Emit(int count)
    {
        if (splash != null)
            splash.Emit(count);
    }
}
