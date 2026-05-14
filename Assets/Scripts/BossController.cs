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
    public float backstepSpeed = 1.5f;
    public float backstepDuration = 0.8f;

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
        Backstepping,
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

        // Only evaluate when idle
        if (currentState == BossState.Idle)
        {
            if (evaluationTimer > 0f)
            {
                evaluationTimer -= Time.deltaTime;
            }
            else
            {
                EvaluateBehavior();
            }
        }

        // Continuous movement
        if (currentState == BossState.Walking)
        {
            MoveTowardPlayer(walkSpeed);
        }
        else if (currentState == BossState.Sprinting)
        {
            MoveTowardPlayer(sprintSpeed);
        }
        else if (currentState == BossState.Backstepping)
        {
            MoveAwayFromPlayer(backstepSpeed);
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
        if (roll < 0.60f)
            StartWalk();
        else if (roll < 0.75f)
            Evaluate();
        else if (roll < 0.88f && dashTimer <= 0f)
            StartCoroutine(DashAttack());
        else if (teleportTimer <= 0f && Random.value < 0.4f)
            StartCoroutine(TeleportAttack());
        else
            StartWalk();
    }

    void EvaluateMidRange()
    {
        float roll = Random.value;
        if (roll < 0.45f)
            StartWalk();
        else if (roll < 0.60f)
            if (Random.value < 0.2f)
                StartCoroutine(Backstep());
            else
                Evaluate();
        else if (roll < 0.78f && dashTimer <= 0f)
            StartCoroutine(DashAttack());
        else if (teleportTimer <= 0f && Random.value < 0.4f)
            StartCoroutine(TeleportAttack());
        else
            StartWalk();
    }

    void EvaluateCloseRange()
    {
        float roll = Random.value;

        if (roll < 0.55f)
        {
            // heavily favor attacks at close range
            float comboRoll = Random.value;
            if (comboRoll < 0.5f && attack1Timer <= 0f)
                StartCoroutine(Attack1());
            else if (attack2Timer <= 0f)
                StartCoroutine(Attack2());
            else if (attack1Timer <= 0f)
                StartCoroutine(Attack1());
            else
                if (Random.value < 0.35f)
                    StartCoroutine(Backstep());
                else
                    Evaluate();
        }
        else if (roll < 0.72f && rollAttackTimer <= 0f)
        {
            StartCoroutine(RollAttack());
        }
        else if (roll < 0.84f && jumpAttackTimer <= 0f)
        {
            StartCoroutine(JumpAttack());
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
        StartCoroutine(WalkThenEvaluate(Random.Range(0.5f, 1.2f)));
    }

    void StartSprint()
    {
        currentState = BossState.Sprinting;
        animator.SetBool("isRunning", true);
        animator.SetBool("isWalking", false);
        StartCoroutine(WalkThenEvaluate(Random.Range(0.4f, 0.8f)));
    }

    void Evaluate()
    {
        currentState = BossState.Idle;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator WalkThenEvaluate(float maxWalkTime)
    {
        float elapsed = 0f;

        while (elapsed < maxWalkTime)
        {
            elapsed += Time.deltaTime;

            float distance = Vector2.Distance(transform.position, player.position);

            // If he's walked into a closer range zone, stop and evaluate immediately
            if (currentState == BossState.Sprinting && distance <= farRange)
            {
                break;
            }
            if (currentState == BossState.Walking && distance <= midRange)
            {
                break;
            }

            yield return null;
        }

        if (currentState == BossState.Walking || currentState == BossState.Sprinting)
        {
            currentState = BossState.Idle;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            evaluationTimer = 0f;
        }
    }

    IEnumerator Backstep()
    {
        currentState = BossState.Backstepping;
        float elapsed = 0f;

        while (elapsed < backstepDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    void MoveTowardPlayer(float speed)
    {
        float directionX = player.position.x > transform.position.x ? 1f : -1f;
        transform.position += new Vector3(directionX * speed * Time.deltaTime, 0f, 0f);
    }

    void MoveAwayFromPlayer(float speed)
    {
        float directionX = player.position.x > transform.position.x ? -1f : 1f;
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
        evaluationTimer = 0f;
    }

    IEnumerator Attack2()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("Attack2");
        attack2Timer = attack2Cooldown;

        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack2"));

        currentState = BossState.Idle;
        evaluationTimer = 0f;
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
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            transform.position += new Vector3(dirX * 8f * Time.deltaTime, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        currentState = BossState.Idle;
        evaluationTimer = 0f;
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
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            transform.position += new Vector3(dirX * (dashDistance / dashDuration) * Time.deltaTime, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        // Wait for spin at end of dash
        yield return new WaitForSeconds(0.4f);

        currentState = BossState.Idle;
        evaluationTimer = 0f;
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
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            transform.position += new Vector3(dirX * (jumpAttackDistance / jumpAttackDuration) * Time.deltaTime, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        currentState = BossState.Idle;
        evaluationTimer = 0f;
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

        // Move to new position near player
        float offsetX = Random.Range(teleportMinDistance, teleportMaxDistance);
        offsetX *= Random.value > 0.5f ? 1f : -1f;
        transform.position = new Vector3(
            player.position.x + offsetX,
            transform.position.y,
            transform.position.z
        );

        // Trigger reappear while still invisible
        animator.SetTrigger("TeleportReappear");

        // Wait one frame then show
        yield return null;
        spriteRenderer.enabled = true;

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportReappear"));

        currentState = BossState.Idle;
        evaluationTimer = 0f;
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