using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction RollAction;
    public InputAction JumpAction;
    public InputAction SprintAction;

    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCooldown = 1f;
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float idleDelay = 0.2f;
    [SerializeField] private float rollCooldown = 10f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private StaminaSystem staminaSystem;
    private PlayerStats playerStats;

    private float idleTimer = 0f;
    private bool isRolling = false;
    private Vector2 rollDirection;
    private float rollCooldownTimer = 0f;
    private float jumpCooldownTimer = 0f;

    // =============================================
    // Public interface
    // =============================================
    public bool IsInvincible { get; private set; }
    public float GetRollCooldown() { return rollCooldown; }
    public float GetRollCooldownRemaining() { return rollCooldownTimer; }
    public Vector2 GetPosition() { return transform.position; }

    public float GetHealth() { return playerStats != null ? playerStats.CurrentHealth : 0f; }
    public float GetMaxHealth() { return playerStats != null ? playerStats.MaxHealth : 0f; }

    public void SetHealth(float value)
    {
        if (value < GetHealth() && IsInvincible) return;
        playerStats?.SetHealth(value);
    }

    public void SetMaxHealth(float value)
    {
        playerStats?.SetMaxHealth(value);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        staminaSystem = GetComponent<StaminaSystem>();
        playerStats = GetComponent<PlayerStats>();

        MoveAction.Enable();
        RollAction.Enable();
        JumpAction.Enable();
        SprintAction.Enable();
        RollAction.performed += OnRoll;
        JumpAction.performed += OnJump;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        RollAction.performed -= OnRoll;
        JumpAction.performed -= OnJump;
        SprintAction.Disable();
    }

    void Update()
    {
        if (rollCooldownTimer > 0) rollCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
        if (isRolling) return;

        Vector2 move = MoveAction.ReadValue<Vector2>();
        HandleMovement(move);
        HandleAnimation(move);
        HandleFacing(move);
    }

    private void OnRoll(InputAction.CallbackContext ctx)
    {
        if (isRolling || rollCooldownTimer > 0) return;
        // StaminaSystem handles all stamina checks and deduction
        if (staminaSystem != null && !staminaSystem.TryRoll()) return;

        Vector2 move = MoveAction.ReadValue<Vector2>();
        rollDirection = move.sqrMagnitude > 0.01f
            ? move.normalized
            : (spriteRenderer.flipX ? Vector2.left : Vector2.right);

        StartCoroutine(RollCoroutine());
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (isRolling || jumpCooldownTimer > 0) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpCooldownTimer = jumpCooldown;
        animator.SetTrigger("Jump");
    }

    private IEnumerator RollCoroutine()
    {
        isRolling = true;
        IsInvincible = true;
        spriteRenderer.flipX = rollDirection.x < 0;
        animator.SetTrigger("Roll");

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            rb.linearVelocity = new Vector2(rollDirection.x * rollSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isRolling = false;
        IsInvincible = false;
        rollCooldownTimer = rollCooldown;
    }

    private void HandleMovement(Vector2 move)
    {
        bool isExhausted = staminaSystem != null && staminaSystem.IsExhausted;
        bool isSprinting = !isExhausted
            && SprintAction != null
            && SprintAction.enabled
            && SprintAction.IsPressed()
            && move.sqrMagnitude > 0.01f;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = new Vector2(move.x * currentSpeed, rb.linearVelocity.y);
    }

    private void HandleAnimation(Vector2 move)
    {
        if (move.sqrMagnitude > 0.01f)
        {
            idleTimer = idleDelay;
            animator.SetBool("isMoving", true);
        }
        else
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
                animator.SetBool("isMoving", false);
        }
    }


    private void HandleFacing(Vector2 move)
    {
        if (move.x < 0) spriteRenderer.flipX = true;
        else if (move.x > 0) spriteRenderer.flipX = false;
    }
}
