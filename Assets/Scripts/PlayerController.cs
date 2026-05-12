using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction RollAction;

    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float rollSpeed = 8f;
    // Should match the length of your roll animation clip
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float idleDelay = 0.2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float idleTimer = 0f;

    private Rigidbody2D rb;
    private bool isRolling = false;
    private Vector2 rollDirection;
    [SerializeField] private float rollCooldown = 10f;
    private float rollCooldownTimer = 0f;

    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    // =============================================
    // Public variables to be used by other objects
    // =============================================
    public bool IsInvincible { get; private set; }

    // Returns the total roll cooldown duration configured in the Inspector
    public float GetRollCooldown() { return rollCooldown; }

    // Returns how many seconds remain before the player can roll again
    public float GetRollCooldownRemaining()
    {
        return rollCooldownTimer;
    }

    // Returns the current world position of the player
    public Vector2 GetPosition() { return transform.position; }

    // Returns current health
    public float GetHealth() { return currentHealth; }

    // Sets current health — damage (decrease) is ignored while invincible, heals always apply
    public void SetHealth(float value)
    {
        if (value < currentHealth && IsInvincible) return;
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    // Returns the maximum health configured in the Inspector
    public float GetMaxHealth() { return maxHealth; }

    // Sets the maximum health — also clamps current health if it now exceeds the new max
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
        MoveAction.Enable();
        RollAction.Enable();
        // Subscribe to the roll input event
        RollAction.performed += OnRoll;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        // Unsubscribe when the GameObject is destroyed to avoid null-reference errors
        RollAction.performed -= OnRoll;
    }

    void Update()
    {
        // Tick the cooldown down every frame
        if (rollCooldownTimer > 0)
            rollCooldownTimer -= Time.deltaTime;

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

        // Roll in the movement direction, or forward if standing still
        Vector2 move = MoveAction.ReadValue<Vector2>();
        rollDirection = move.sqrMagnitude > 0.01f
            ? move.normalized
            : (spriteRenderer.flipX ? Vector2.left : Vector2.right);

        StartCoroutine(RollCoroutine());
    }

    // Coroutine: runs across multiple frames so we can move the player and track
    // elapsed time without freezing the game. yield return null means "pause here,
    // resume on the next frame."
    private IEnumerator RollCoroutine()
    {
        isRolling = true;
        IsInvincible = true;
        // Flip the sprite to face the roll direction before playing the animation
        spriteRenderer.flipX = rollDirection.x < 0;
        animator.SetTrigger("Roll");

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            // Push the player in the roll direction each frame
            transform.position += (Vector3)(rollDirection * rollSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null; // Wait one frame, then continue the loop
        }

        isRolling = false;
        IsInvincible = false;
        // Start the cooldown after the roll finishes
        rollCooldownTimer = rollCooldown;
    }

    // Moves the player using RigidBody
    private void HandleMovement(Vector2 move)
    {
        rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);
    }

    // Switches between Idle and Run with a grace period to avoid flashing on direction changes
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