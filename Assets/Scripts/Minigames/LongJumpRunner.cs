using UnityEngine;

// Corrida do Salto a distância: alternar esquerda/direita acelera e
// parar de alternar desacelera. No ar a velocidade fica travada.
[RequireComponent(typeof(PlayerController))]
public class LongJumpRunner : MonoBehaviour
{
    [Header("Ritmo")]
    [SerializeField] private float tapBoost = 0.8f;
    [SerializeField] private float maxSpeed = 9f;
    [SerializeField] private float decay = 1.5f;

    [Header("Animação de corrida")]
    [SerializeField] private float minAnimationSpeed = 0.6f;
    [SerializeField] private float maxAnimationSpeed = 1.6f;

    private PlayerController player;
    private Animator animator;
    private int lastSide;
    private bool running;

    public float Speed { get; private set; }
    public float MaxSpeed => maxSpeed;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        Transform visual = transform.Find("Visual");
        if (visual != null)
            animator = visual.GetComponent<Animator>();
    }

    public void Begin()
    {
        Speed = 0f;
        lastSide = 0;
        running = true;
        Apply();
    }

    public void Stop()
    {
        Speed = 0f;
        running = false;
        Apply();
    }

    // side: -1 esquerda, 1 direita. Só a alternância acelera.
    public void RegisterTap(int side)
    {
        if (!running || side == lastSide || !player.IsGrounded)
            return;

        lastSide = side;
        Speed = Mathf.Min(maxSpeed, Speed + tapBoost);
        Apply();
    }

    public void Boost(float multiplier)
    {
        Speed *= multiplier;
        Apply();
    }

    private void Update()
    {
        if (!running || !player.enabled)
            return;

        if (Input.GetKeyDown(player.LeftKey))
            RegisterTap(-1);
        if (Input.GetKeyDown(player.RightKey))
            RegisterTap(1);

        if (player.IsGrounded)
            Speed = Mathf.Max(0f, Speed - decay * Time.deltaTime);

        Apply();
    }

    private void Apply()
    {
        player.SetForcedRunSpeed(Speed);

        if (animator != null)
            animator.speed = running && player.IsGrounded
                ? Mathf.Lerp(minAnimationSpeed, maxAnimationSpeed, Speed / maxSpeed)
                : 1f;
    }
}
