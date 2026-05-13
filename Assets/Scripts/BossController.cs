using UnityEngine;
using System.Collections;

public class BossController : Enemy
{
    [Header("Boss Identity")]
    public string bossName = "The Shadow Warrior";

    [Header("Range Thresholds")]
    public float extremeRange = 15f;
    public float farRange = 10f;
    public float midRange = 6f;
    public float closeRange = 3f;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 6f;

    [Header("Attack Cooldowns")]
    public float dashCooldown = 7f;
    public float rollAttackCooldown = 3f;
    public float attack1Cooldown = 2f;
    public float attack2Cooldown = 4f;
    public float jumpAttackCooldown = 5f;
    public float teleportCooldown = 8f;

    [Header("Attack Durations")]
    public float dashDistance = 8f;
    public float dashDuration = 0.4f;
    public float jumpAttackDistance = 4f;
    public float jumpAttackDuration = 0.6f;
    public float teleportMinDistance = 3f;
    public float teleportMaxDistance = 6f;
    public float teleportInvisibleDuration = 0.5f;

    [Header("Evaluation")]
    public float minEvaluationTime = 1f;
    public float maxEvaluationTime = 2.5f;

    [Header("Poise")]
    public float maxPoise = 50f;
    public float poiseRegenRate = 10f;
    public float poiseRegenDelay = 2f;
    public float staggerDuration = 0.5f;

    [Header("Combat")]
    public float contactDamage = 10f;

    // =============================================
    // Public state readable by UI and other systems
    // =============================================
    public string GetBossName() { return bossName; }
    public bool IsPhaseTwo { get; private set; }

    // =============================================
    // Private state
    // =============================================
    private enum BossState
    {
        Idle,
        Walking,
        Sprinting,
        Attacking,
        Staggered,
        Dead
    }

    private BossState currentState = BossState.Idle;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    // Cooldown timers
    private float dashTimer = 0f;
    private float rollAttackTimer = 0f;
    private float attack1Timer = 0f;
    private float attack2Timer = 0f;
    private float jumpAttackTimer = 0f;
    private float teleportTimer = 0f;
    private float evaluationTimer = 0f;

    // Poise tracking
    private float currentPoise;
    private float poiseRegenDelayTimer = 0f;
    private bool isDashing = false;

    protected override void OnStart()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentPoise = maxPoise;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    void Update()
    {
        if (currentState == BossState.Dead) return;
        if (currentState == BossState.Staggered) return;
        if (currentState == BossState.Attacking) return;
        if (player == null) return;

        HandleFacing();
        TickCooldowns();
        TickPoise();

        if (evaluationTimer > 0f)
        {
            evaluationTimer -= Time.deltaTime;
        }
        else
        {
            EvaluateBehavior();
        }

        // Continuous movement — runs every frame regardless of evaluation
        if (currentState == BossState.Walking)
        {
            MoveTowardPlayer(walkSpeed);
        }
        else if (currentState == BossState.Sprinting)
        {
            MoveTowardPlayer(sprintSpeed);
        }
    }

    void HandleFacing()
    {
        if (player.position.x < transform.position.x)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }

    void TickCooldowns()
    {
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (rollAttackTimer > 0f) rollAttackTimer -= Time.deltaTime;
        if (attack1Timer > 0f) attack1Timer -= Time.deltaTime;
        if (attack2Timer > 0f) attack2Timer -= Time.deltaTime;
        if (jumpAttackTimer > 0f) jumpAttackTimer -= Time.deltaTime;
        if (teleportTimer > 0f) teleportTimer -= Time.deltaTime;
    }

    void TickPoise()
    {
        if (poiseRegenDelayTimer > 0f)
        {
            poiseRegenDelayTimer -= Time.deltaTime;
            return;
        }

        if (currentPoise < maxPoise)
            currentPoise += poiseRegenRate * Time.deltaTime;
    }

    void EvaluateBehavior()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > extremeRange)
        {
            EvaluateExtremeRange();
        }
        else if (distance > farRange)
        {
            EvaluateFarRange();
        }
        else if (distance > midRange)
        {
            EvaluateMidRange();
        }
        else
        {
            EvaluateCloseRange();
        }
    }

    void EvaluateExtremeRange()
    {
        float roll = Random.value;
        if (roll < 0.85f)
            StartSprint();
        else
            StartWalk();
    }

    void EvaluateFarRange()
    {
        float roll = Random.value;
        if (roll < 0.75f)
            StartWalk();
        else if (roll < 0.90f)
            Evaluate();
        else if (dashTimer <= 0f)
            StartCoroutine(DashAttack());
        else
            StartWalk();
    }

    void EvaluateMidRange()
    {
        float roll = Random.value;
        if (roll < 0.50f)
            StartWalk();
        else if (roll < 0.75f)
            Evaluate();
        else if (dashTimer <= 0f)
            StartCoroutine(DashAttack());
        else
            StartWalk();
    }

    void EvaluateCloseRange()
    {
        float roll = Random.value;

        if (roll < 0.35f)
        {
            // combo decision
            float comboRoll = Random.value;
            if (comboRoll < 0.4f && attack1Timer <= 0f)
                StartCoroutine(Attack1());
            else if (attack2Timer <= 0f)
                StartCoroutine(Attack2());
            else
                Evaluate();
        }
        else if (roll < 0.60f && rollAttackTimer <= 0f)
        {
            StartCoroutine(RollAttack());
        }
        else if (roll < 0.70f && jumpAttackTimer <= 0f)
        {
            StartCoroutine(JumpAttack());
        }
        else if (roll < 0.80f && teleportTimer <= 0f)
        {
            StartCoroutine(TeleportAttack());
        }
        else
        {
            Evaluate();
        }
    }

    void StartWalk()
    {
        currentState = BossState.Walking;
        animator.SetBool("isWalking", true);
        animator.SetBool("isRunning", false);
        MoveTowardPlayer(walkSpeed);
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    void StartSprint()
    {
        currentState = BossState.Sprinting;
        animator.SetBool("isRunning", true);
        animator.SetBool("isWalking", false);
        MoveTowardPlayer(sprintSpeed);
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    void Evaluate()
    {
        currentState = BossState.Idle;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    void MoveTowardPlayer(float speed)
    {
        float directionX = player.position.x > transform.position.x ? 1f : -1f;
        transform.position += new Vector3(directionX * speed * Time.deltaTime, 0f, 0f);
    }

    // =============================================
    // Attack Coroutines
    // =============================================

    IEnumerator Attack1()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("Attack1");
        attack1Timer = attack1Cooldown;

        // Wait for animation to finish
        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack1"));

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator Attack2()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("Attack2");
        attack2Timer = attack2Cooldown;

        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack2"));

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator RollAttack()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("RollAttack");
        rollAttackTimer = rollAttackCooldown;

        // Move through the player during roll
        Vector2 direction = (player.position - transform.position).normalized;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            transform.position += (Vector3)(direction * 8f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator DashAttack()
    {
        currentState = BossState.Attacking;
        isDashing = true;
        animator.SetTrigger("Dash");
        dashTimer = dashCooldown;

        // Dash toward player
        Vector2 direction = (player.position - transform.position).normalized;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            transform.position += (Vector3)(direction * (dashDistance / dashDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        // Wait for spin at end of dash
        yield return new WaitForSeconds(0.4f);

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator JumpAttack()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("JumpAttack");
        jumpAttackTimer = jumpAttackCooldown;

        Vector2 direction = (player.position - transform.position).normalized;
        float elapsed = 0f;

        while (elapsed < jumpAttackDuration)
        {
            transform.position += (Vector3)(direction * (jumpAttackDistance / jumpAttackDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator TeleportAttack()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("TeleportDisappear");
        teleportTimer = teleportCooldown;

        // Wait for disappear animation
        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportDisappear"));

        // Go invisible
        spriteRenderer.enabled = false;

        // Wait invisible
        yield return new WaitForSeconds(teleportInvisibleDuration);

        // Pick new position near player
        float offsetX = Random.Range(teleportMinDistance, teleportMaxDistance);
        offsetX *= Random.value > 0.5f ? 1f : -1f;
        Vector3 newPos = new Vector3(
            player.position.x + offsetX,
            transform.position.y,
            transform.position.z
        );
        transform.position = newPos;

        // Reappear
        spriteRenderer.enabled = true;
        yield return null; // wait one frame before triggering
        animator.SetTrigger("TeleportReappear");

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportReappear"));

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    // =============================================
    // Damage and Poise
    // =============================================

    public void TakeDamage(float damage, float poiseDamage = 0f)
    {
        if (currentState == BossState.Dead) return;

        SetHealth(GetHealth() - damage);

        // Poise check — cant stagger during dash
        if (!isDashing)
        {
            currentPoise -= poiseDamage;
            poiseRegenDelayTimer = poiseRegenDelay;

            if (currentPoise <= 0f)
            {
                StartCoroutine(Stagger());
            }
        }
    }

    IEnumerator Stagger()
    {
        currentState = BossState.Staggered;
        animator.SetTrigger("TakeHit");
        currentPoise = maxPoise;

        yield return new WaitForSeconds(staggerDuration);

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    protected override void Die()
    {
        currentState = BossState.Dead;
        animator.SetTrigger("Death");
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(GetAnimationLength("Boss_Death"));
        Debug.Log(bossName + " defeated");
        // Cleanup, rewards, etc. to be added later
        gameObject.SetActive(false);
    }

    // =============================================
    // Utility
    // =============================================

    float GetAnimationLength(string clipName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        Debug.LogWarning("Animation clip not found: " + clipName);
        return 1f;
    }
}