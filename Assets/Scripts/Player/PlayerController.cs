using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction RollAction;
    public InputAction JumpAction;
    public InputAction SprintAction;
    public InputAction HealAction;
    public InputAction ClimbAction;

    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCooldown = 1f;
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float idleDelay = 0.2f;
    [SerializeField] private float rollCooldown = 10f;
    [SerializeField] private float deathAnimationDuration = 1.5f;
    [SerializeField] private float respawnDelay = 0.5f;

    [Header("Healing")]
    [SerializeField] private int maxHeals = 2;
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float healApplyDelay = 0.4f;

    [SerializeField] private float climbSpeed = 3f;
    private bool isOnLadder = false;
    public bool IsOnLadder => isOnLadder;
    private int ladderContactCount = 0;
    private float originalGravityScale;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private StaminaSystem staminaSystem;
    private PlayerStats playerStats;

    private float idleTimer = 0f;
    private bool isRolling = false;
    private bool isHealing = false;
    private Vector2 rollDirection;
    private float rollCooldownTimer = 0f;
    private float jumpCooldownTimer = 0f;
    private int jumpsRemaining = 0;
    private int maxJumps = 2;
    private int currentHeals;

    [Header("FP Flasks")]
    [SerializeField] private int maxFpFlasks = 0;
    [SerializeField] private float fpRestoreAmount = 50f;
    [SerializeField] private float fpApplyDelay = 0.5f;
    private int currentFpFlasks = 0;
    private bool isUsingFpFlask = false;

    public int CurrentFpFlasks => currentFpFlasks;
    public int MaxFpFlasks => maxFpFlasks;

    [Header("Flask Selection")]
    public InputAction ScrollAction;
    private bool healSelected = true; // true = heal flask, false = fp flask
    public bool HealSelected => healSelected;

    // =============================================
    // Public interface
    // =============================================
    public bool IsInvincible { get; private set; }
    public bool IsPlunging { get; set; }
    public int CurrentHeals => currentHeals;
    public int MaxHeals => maxHeals;
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
        originalGravityScale = rb.gravityScale;
        staminaSystem = GetComponent<StaminaSystem>();
        playerStats = GetComponent<PlayerStats>();

        currentHeals = maxHeals;

        MoveAction.Enable();
        RollAction.Enable();
        JumpAction.Enable();
        SprintAction.Enable();
        HealAction.Enable();
        ClimbAction.Enable();
        ScrollAction.Enable();
        ScrollAction.performed += OnScroll;

        RollAction.performed += OnRoll;
        JumpAction.performed += OnJump;
        HealAction.performed += OnHeal;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerStats != null)
            playerStats.OnDeath += OnPlayerDied;
    }

    void OnDestroy()
    {
        RollAction.performed -= OnRoll;
        JumpAction.performed -= OnJump;
        HealAction.performed -= OnHeal;
        SprintAction.Disable();
        HealAction.Disable();
        ClimbAction.Disable();
        ScrollAction.Disable();
        ScrollAction.performed -= OnScroll;

        if (playerStats != null)
            playerStats.OnDeath -= OnPlayerDied;
    }

    void Update()
    {
        if (rollCooldownTimer > 0) rollCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
        if (isRolling) return;

        Vector2 move = MoveAction.ReadValue<Vector2>();

        Debug.Log("Update — isOnLadder: " + isOnLadder + " ladderContactCount: " + ladderContactCount + " move.y: " + move.y);

        float climbInput = ClimbAction.ReadValue<float>();
        if (!isOnLadder && ladderContactCount > 0 && Mathf.Abs(climbInput) > 0.1f)
            EnterLadder();

        if (isOnLadder)
        {
            HandleLadderMovement(move);
            return;
        }

        HandleMovement(move);
        HandleAnimation(move);
        HandleFacing(move);
    }

    private void OnPlayerDied()
    {
        StartCoroutine(DieCoroutine());
    }

    private void OnHeal(InputAction.CallbackContext ctx)
    {
        if (PauseStateManager.IsPaused) return;
        if (playerStats != null && playerStats.IsDead) return;
        if (isRolling) return;

        if (healSelected)
        {
            if (isHealing) return;
            if (currentHeals <= 0) return;
            if (playerStats != null && playerStats.CurrentHealth >= playerStats.MaxHealth) return;
            StartCoroutine(HealCoroutine());
        }
        else
        {
            if (isUsingFpFlask) return;
            if (currentFpFlasks <= 0) return;
            if (playerStats != null && playerStats.CurrentFP >= playerStats.MaxFP) return;
            StartCoroutine(FpFlaskCoroutine());
        }
    }

    private IEnumerator HealCoroutine()
    {
        isHealing = true;
        currentHeals--;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);

        // Wait one frame for animator to be ready before triggering
        yield return null;
        animator.SetTrigger("Heal");

        // Wait for fourth key before applying health
        yield return new WaitForSeconds(healApplyDelay);
        playerStats?.ChangeHealth(healAmount);

        float remaining = GetAnimationLength("Heal") - healApplyDelay;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        isHealing = false;
    }

    private IEnumerator FpFlaskCoroutine()
    {
        isUsingFpFlask = true;
        currentFpFlasks--;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);

        yield return null;
        animator.SetTrigger("Heal"); // reuse heal animation

        yield return new WaitForSeconds(fpApplyDelay);
        playerStats?.ChangeFP(fpRestoreAmount);

        float remaining = GetAnimationLength("Heal") - fpApplyDelay;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        isUsingFpFlask = false;
    }

    public void RefillHeals()
    {
        currentHeals = maxHeals;
    }

    public void GiveFpFlasks(int amount)
    {
        maxFpFlasks += amount;
        currentFpFlasks += amount;
        if (currentFpFlasks > maxFpFlasks)
            currentFpFlasks = maxFpFlasks;

        // Switch to FP flask selection if we just got our first ones
        if (currentFpFlasks > 0 && maxFpFlasks == amount)
            healSelected = true; // stay on heal by default
    }

    public void RefillFpFlasks()
    {
        currentFpFlasks = maxFpFlasks;
    }

    private float GetAnimationLength(string clipName)
    {
        if (animator == null) return 1f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 1f;
    }

    private IEnumerator DieCoroutine()
    {
        isHealing = false;
        IsPlunging = false;
        rb.linearVelocity = Vector2.zero;
        MoveAction.Disable();
        RollAction.Disable();
        JumpAction.Disable();
        SprintAction.Disable();
        HealAction.Disable();

        animator.SetBool("isMoving", false);
        animator.SetTrigger("Death");

        yield return new WaitForSeconds(deathAnimationDuration);

        // Show death screen
        VictoryScreen victoryScreen = FindFirstObjectByType<VictoryScreen>();
        if (victoryScreen != null)
            yield return StartCoroutine(victoryScreen.ShowAndWait("You Died"));
        else
            yield return new WaitForSeconds(respawnDelay);

        RespawnManager.Respawn(this);
    }

    public void Revive()
    {
        MoveAction.Enable();
        RollAction.Enable();
        JumpAction.Enable();
        SprintAction.Enable();
        HealAction.Enable();

        GetComponent<PlayerCombat>()?.ReviveActions();

        playerStats.Revive();
        currentHeals = maxHeals;
        currentFpFlasks = maxFpFlasks;

        animator.SetBool("isMoving", false);
        animator.Play("Idle");
    }

    private void OnRoll(InputAction.CallbackContext ctx)
    {
        if (PauseStateManager.IsPaused)
            return;

        if (isHealing || isRolling || rollCooldownTimer > 0) return;
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
        if (isRolling) return;

        if (isOnLadder)
        {
            ExitLadder();
            jumpsRemaining = 0;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCooldownTimer = jumpCooldown;
            animator.SetTrigger("Jump");
            return;
        }

        if (jumpsRemaining <= 0) return;
        jumpsRemaining--;
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                jumpsRemaining = maxJumps;
                break;
            }
        }
    }

    private void HandleMovement(Vector2 move)
    {
        bool isExhausted = staminaSystem != null && staminaSystem.IsExhausted;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        bool isHeavyAttacking = combat != null && (
            combat.GetState() == PlayerCombat.CombatState.HeavyAttack1 ||
            combat.GetState() == PlayerCombat.CombatState.HeavyAttack2);

        if (isHeavyAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

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

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered: " + other.gameObject.name + " layer: " + LayerMask.LayerToName(other.gameObject.layer));
        if (other.GetComponent<Ladder>() != null || other.GetComponentInParent<Ladder>() != null)
        {
            ladderContactCount++;
            Debug.Log("Ladder contact count: " + ladderContactCount);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Ladder>() != null || other.GetComponentInParent<Ladder>() != null)
        {
            ladderContactCount--;
            if (ladderContactCount <= 0)
            {
                ladderContactCount = 0;
                if (isOnLadder)
                    ExitLadder();
            }
        }
    }

    void EnterLadder()
    {
        Debug.Log("EnterLadder called — setting gravityScale to 0");
        isOnLadder = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isClimbing", true);
        animator.SetBool("isMoving", false);
    }

    void ExitLadder()
    {
        isOnLadder = false;
        rb.gravityScale = originalGravityScale;
        animator.SetBool("isClimbing", false);
    }

    private void HandleLadderMovement(Vector2 move)
    {
        float climbInput = ClimbAction.ReadValue<float>();
        rb.linearVelocity = new Vector2(0f, climbInput * climbSpeed);
        animator.SetBool("isMoving", false);
    }

    private void OnScroll(InputAction.CallbackContext ctx)
    {
        if (currentFpFlasks <= 0) return; // no FP flasks, no switching
        float scroll = ctx.ReadValue<float>();
        if (scroll != 0f)
            healSelected = !healSelected;
    }
}
