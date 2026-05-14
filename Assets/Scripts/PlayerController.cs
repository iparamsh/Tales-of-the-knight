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
    // Should match the length of your roll animation clip
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float idleDelay = 0.2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float idleTimer = 0f;

    private Rigidbody2D rb;
    private StaminaSystem staminaSystem;
    private bool isRolling = false;
    private Vector2 rollDirection;
    [SerializeField] private float rollCooldown = 10f;
    private float rollCooldownTimer = 0f;
    private float jumpCooldownTimer = 0f;

    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    // =============================================
    // Public variables to be used by other objects
    // =============================================
    public bool IsInvincible { get; private set; }

    public float GetRollCooldown() { return rollCooldown; }

    public float GetRollCooldownRemaining() { return rollCooldownTimer; }

    public Vector2 GetPosition() { return transform.position; }

    public float GetHealth() { return currentHealth; }

    public void SetHealth(float value)
    {
        if (value < currentHealth && IsInvincible) return;
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    public float GetMaxHealth() { return maxHealth; }

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(0f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    // =============================================
    // Public variables to be used by other objects
    // =============================================

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        staminaSystem = GetComponent<StaminaSystem>();
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
        if (rollCooldownTimer > 0)
            rollCooldownTimer -= Time.deltaTime;

        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= Time.deltaTime;


        // Block normal movement and animation while rolling
        if (isRolling) return;

        Vector2 move = MoveAction.ReadValue<Vector2>();
        HandleMovement(move);
        HandleAnimation(move);
        HandleFacing(move);
    }

    private void OnRoll(InputAction.CallbackContext ctx)
    {
        if (isRolling || rollCooldownTimer > 0) return;

        StaminaSystem stamina = GetComponent<StaminaSystem>();
        if (stamina != null && !stamina.TryRoll()) return;

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

        // Stop the roll momentum when finished
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isRolling = false;
        IsInvincible = false;
        rollCooldownTimer = rollCooldown;
    }

    private void HandleMovement(Vector2 move)
    {
        StaminaSystem stamina = staminaSystem;
        bool isExhausted = stamina != null && stamina.IsExhausted;
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

    // Flips the sprite so the character faces the direction of movement
    private void HandleFacing(Vector2 move)
    {
        if (move.x < 0)
            spriteRenderer.flipX = true;
        else if (move.x > 0)
            spriteRenderer.flipX = false;
    }
}
