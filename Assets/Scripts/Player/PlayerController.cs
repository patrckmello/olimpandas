using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Jogador")]
    [SerializeField] private int playerId = 1;

    [Header("Controles")]
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode jumpKey = KeyCode.W;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.30f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer dashTrail;

    private float horizontal;
    private float lastDirection = 1f;

    private bool isGrounded;
    private bool isDashing;
    private bool canDash = true;

    private bool isStunned = false;

    private float movementMultiplier = 1f;

    private bool airDashAvailable = true;

    private float? forcedRunSpeed;

    private readonly HashSet<Collider2D>
        groundColliders = new();

    public int PlayerId => playerId;
    public float FacingDirection => lastDirection;
    public bool IsStunned => isStunned;
    public bool IsGrounded => isGrounded;
    public KeyCode LeftKey => leftKey;
    public KeyCode RightKey => rightKey;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Transform visual =
            transform.Find("Visual");

        if (visual != null)
        {
            animator =
                visual.GetComponent<Animator>();

            spriteRenderer =
                visual.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError(
                $"{name}: objeto filho 'Visual' não encontrado!"
            );
        }

        Transform dashTrailObject =
            transform.Find("DashTrail");

        if (dashTrailObject != null)
        {
            dashTrail =
                dashTrailObject
                    .GetComponent<TrailRenderer>();

            if (dashTrail != null)
            {
                dashTrail.emitting = false;
                dashTrail.Clear();
            }
        }
    }

    private void Update()
    {
        // Stun bloqueia todos os comandos.
        if (isStunned)
        {
            horizontal = 0f;

            UpdateAnimations();

            return;
        }

        ReadMovementInput();
        UpdateAnimations();
        UpdateDirection();
        HandleJump();
        HandleDash();
    }

    private void FixedUpdate()
    {
        if (isDashing)
            return;

        float speed =
            forcedRunSpeed.HasValue && horizontal != 0f
                ? forcedRunSpeed.Value
                : horizontal * moveSpeed;

        rb.linearVelocity = new Vector2(
            speed *
            movementMultiplier,

            rb.linearVelocity.y
        );
    }

    // Provas com corrida própria (ex.: Salto a distância) definem a
    // velocidade para frente; null devolve o controle normal por A/D.
    public void SetForcedRunSpeed(float? speed)
    {
        forcedRunSpeed = speed;
    }

    private void ReadMovementInput()
    {
        if (forcedRunSpeed.HasValue)
        {
            horizontal = forcedRunSpeed.Value > 0.01f ? 1f : 0f;
            return;
        }

        horizontal = 0f;

        if (Input.GetKey(leftKey))
        {
            horizontal = -1f;
        }
        else if (Input.GetKey(rightKey))
        {
            horizontal = 1f;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "isMoving",
            Mathf.Abs(horizontal) > 0.01f
        );

        animator.SetFloat(
            "verticalVelocity",
            rb.linearVelocity.y
        );

        animator.SetBool(
            "isGrounded",
            isGrounded
        );

        animator.SetBool(
            "isDashing",
            isDashing
        );
    }

    private void UpdateDirection()
    {
        if (horizontal == 0f)
            return;

        lastDirection =
            Mathf.Sign(horizontal);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX =
                horizontal < 0f;
        }
    }

    private void HandleJump()
    {
        if (!Input.GetKeyDown(jumpKey))
            return;

        if (!isGrounded)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        groundColliders.Clear();
        isGrounded = false;
    }

    private void HandleDash()
    {
        if (!Input.GetKeyDown(dashKey))
            return;

        if (!canDash || isDashing)
            return;

        if (!isGrounded)
        {
            if (!airDashAvailable)
                return;

            airDashAvailable = false;
        }

        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        if (animator != null)
        {
            animator.SetBool(
                "isDashing",
                true
            );
        }

        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        rb.linearVelocity = new Vector2(
            lastDirection *
            dashSpeed *
            movementMultiplier,
            0f
        );

        yield return new WaitForSeconds(
            dashDuration
        );

        isDashing = false;

        if (animator != null)
        {
            animator.SetBool(
                "isDashing",
                false
            );
        }

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
        }

        yield return new WaitForSeconds(
            dashCooldown
        );

        canDash = true;
    }

    public void SetStunned(bool stunned)
    {
        isStunned = stunned;

        if (!stunned)
            return;

        horizontal = 0f;

        // Interrompe visualmente o Dash.
        isDashing = false;

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
        }

        if (animator != null)
        {
            animator.SetBool(
                "isMoving",
                false
            );

            animator.SetBool(
                "isDashing",
                false
            );
        }

        // Para somente movimento horizontal.
        rb.linearVelocity = new Vector2(
            0f,
            rb.linearVelocity.y
        );
    }

    public void SetMovementMultiplier(
        float multiplier
    )
    {
        movementMultiplier =
            Mathf.Clamp(
                multiplier,
                0.1f,
                1f
            );
    }

    public void FinishRace(bool victorious)
    {
        horizontal = 0f;
        isDashing = false;

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.Clear();
        }

        if (animator != null)
        {
            animator.SetBool(
                "isMoving",
                false
            );

            animator.SetBool(
                "isDashing",
                false
            );

            animator.SetFloat(
                "verticalVelocity",
                0f
            );

            animator.SetBool(
                "isGrounded",
                true
            );

            if (victorious)
            {
                animator.SetTrigger(
                    "Victory"
                );
            }
        }
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        CheckGroundCollision(collision);
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        CheckGroundCollision(collision);
    }

    private void OnCollisionExit2D(
        Collision2D collision
    )
    {
        groundColliders.Remove(
            collision.collider
        );

        isGrounded =
            groundColliders.Count > 0;
    }

    private void CheckGroundCollision(
        Collision2D collision
    )
    {
        foreach (
            ContactPoint2D contact
            in collision.contacts
        )
        {
            if (contact.normal.y > 0.5f)
            {
                groundColliders.Add(
                    collision.collider
                );

                isGrounded = true;

                airDashAvailable = true;

                return;
            }
        }
    }
}