using UnityEngine;
using System.Collections;

public class BossController : Enemy
{
    [Header("Boss Identity")]
    public string bossName = "Shadow Warrior Malec";
    public string bossNamePhaseTwo = "The Beast Within";

    [Header("Range Thresholds")]
    public float extremeRange = 15f;
    public float farRange = 10f;
    public float midRange = 6f;
    public float closeRange = 3f;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 6f;

    [Header("Human Attack Cooldowns")]
    public float dashCooldown = 7f;
    public float rollAttackCooldown = 3f;
    public float attack1Cooldown = 2f;
    public float attack2Cooldown = 4f;
    public float attack3Cooldown = 6f;
    public float jumpAttackCooldown = 5f;
    public float teleportCooldown = 8f;

    [Header("Beast Attack Cooldowns")]
    public float beastAtk1Cooldown = 2f;
    public float beastAtk2Cooldown = 3f;
    public float beastAtk3Cooldown = 5f;
    public float beastOverheadCooldown = 4f;
    public float beastSpecialCooldown = 6f;
    public float beastSpecialChompCooldown = 4f;

    [Header("Attack Durations")]
    public float dashDistance = 12f;
    public float dashDuration = 0.15f;
    public float jumpAttackDistance = 4f;
    public float jumpAttackDuration = 0.6f;
    public float teleportMinDistance = 3f;
    public float teleportMaxDistance = 6f;
    public float teleportInvisibleDuration = 0.5f;

    [Header("Shadow Clone")]
    public float cloneDashDistance = 12f;
    public float cloneDashDuration = 0.15f;
    public GameObject shadowClonePrefab;

    [Header("Phase Transition")]
    public float transitionAOERadius = 4f;
    public float transitionAOEDamage = 20f;
    public float transitionAOEDuration = 2f;
    public float transformationSpeed = 0.6f;

    [Header("Evaluation")]
    public float minEvaluationTime = 0.1f;
    public float maxEvaluationTime = 0.4f;
    public int movesBeforeFormSwitch = 4;

    [Header("Poise")]
    public float maxPoise = 50f;
    public float poiseRegenRate = 10f;
    public float poiseRegenDelay = 2f;
    public float staggerDuration = 0.5f;

    [Header("Combat")]
    public float aggroRange = 12f;
    public float contactDamage = 10f;
    public float backstepSpeed = 1.5f;
    public float backstepDuration = 0.8f;
    public float arenaMinX = -2f;
    public float arenaMaxX = 40f;

    [Header("Death")]
    public GameObject bonfirePrefab;
    public Transform bonfireSpawnPoint;

    // =============================================
    // Public state
    // =============================================
    public string GetBossName() { return isBeastForm ? bossNamePhaseTwo : bossName; }
    public bool IsPhaseTwo { get; private set; }
    public event System.Action<float, float> OnHealthChanged;

    // =============================================
    // Private state
    // =============================================
    private enum BossState
    {
        Dormant,
        Idle,
        Walking,
        Sprinting,
        Backstepping,
        Attacking,
        Staggered,
        Dead
    }

    private BossState currentState = BossState.Dormant;
    private Coroutine walkCoroutine;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;

    // Form tracking
    private bool isBeastForm = false;
    private bool isTransitioning = false;
    private bool phaseTwoUnlocked = false;
    private bool phase30Triggered = false;
    private bool phase15Triggered = false;
    private int moveCounter = 0;

    // Human cooldown timers
    private float dashTimer = 0f;
    private float rollAttackTimer = 0f;
    private float attack1Timer = 0f;
    private float attack2Timer = 0f;
    private float attack3Timer = 0f;
    private float jumpAttackTimer = 0f;
    private float teleportTimer = 0f;

    // Beast cooldown timers
    private float beastAtk1Timer = 0f;
    private float beastAtk2Timer = 0f;
    private float beastAtk3Timer = 0f;
    private float beastOverheadTimer = 0f;
    private float beastSpecialTimer = 0f;
    private float beastSpecialChompTimer = 0f;

    private float evaluationTimer = 0f;

    // Poise tracking
    private float currentPoise;
    private float poiseRegenDelayTimer = 0f;
    private bool isDashing = false;

    private bool isDead = false;

    protected override void OnStart()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentPoise = maxPoise;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
        spriteRenderer.enabled = false;
    }

    void Update()
    {
        if (currentState == BossState.Dormant)
        {
            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                if (distanceToPlayer <= aggroRange)
                    BossEntrance();
            }
            return;
        }

        // TEMP TESTING — remove before final build
        // TEMP TESTING — remove before final build
        TakeDamage(2f * Time.deltaTime, 0f);

        if (currentState == BossState.Dead) return;
        if (currentState == BossState.Staggered) return;
        if (currentState == BossState.Attacking) return;
        if (isTransitioning) return;
        if (player == null) return;

        if (currentState == BossState.Idle || currentState == BossState.Walking || currentState == BossState.Sprinting || currentState == BossState.Backstepping)
            HandleFacing();
        TickCooldowns();
        TickPoise();
        CheckPhaseTransitions();

        if (currentState == BossState.Idle)
        {
            if (evaluationTimer > 0f)
                evaluationTimer -= Time.deltaTime;
            else
                EvaluateBehavior();
        }

        if (currentState == BossState.Idle && isBeastForm)
            Debug.Log("Beast idle — isBeastRunning: " + animator.GetBool("isBeastRunning"));

        if (currentState == BossState.Walking)
            MoveTowardPlayer(walkSpeed);
        else if (currentState == BossState.Sprinting)
            MoveTowardPlayer(sprintSpeed);
        else if (currentState == BossState.Backstepping)
            MoveAwayFromPlayer(backstepSpeed);
    }

    void CheckPhaseTransitions()
    {
        float healthPercent = GetHealth() / GetMaxHealth();

        // 50% — initial phase two transformation
        if (!phaseTwoUnlocked && healthPercent <= 0.5f)
        {
            phaseTwoUnlocked = true;
            IsPhaseTwo = true;
            StartCoroutine(PhaseTransition());
            return;
        }

        // 30% — forced switch back to human
        if (phaseTwoUnlocked && isBeastForm && !phase30Triggered && healthPercent <= 0.3f)
        {
            phase30Triggered = true;
            StartCoroutine(SwitchToHuman());
            return;
        }

        // 15% — forced switch back to beast
        if (phaseTwoUnlocked && !isBeastForm && phase30Triggered && !phase15Triggered && healthPercent <= 0.15f)
        {
            phase15Triggered = true;
            StartCoroutine(SwitchToBeast());
            return;
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
        if (attack3Timer > 0f) attack3Timer -= Time.deltaTime;
        if (jumpAttackTimer > 0f) jumpAttackTimer -= Time.deltaTime;
        if (teleportTimer > 0f) teleportTimer -= Time.deltaTime;
        if (beastAtk1Timer > 0f) beastAtk1Timer -= Time.deltaTime;
        if (beastAtk2Timer > 0f) beastAtk2Timer -= Time.deltaTime;
        if (beastAtk3Timer > 0f) beastAtk3Timer -= Time.deltaTime;
        if (beastOverheadTimer > 0f) beastOverheadTimer -= Time.deltaTime;
        if (beastSpecialTimer > 0f) beastSpecialTimer -= Time.deltaTime;
        if (beastSpecialChompTimer > 0f) beastSpecialChompTimer -= Time.deltaTime;
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

    // =============================================
    // Evaluation
    // =============================================

    void EvaluateBehavior()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        // Check free switching at 15% and below
        if (phase15Triggered && moveCounter >= movesBeforeFormSwitch)
        {
            if (Random.value < 0.5f)
            {
                moveCounter = 0;
                if (isBeastForm)
                    StartCoroutine(SwitchToHuman());
                else
                    StartCoroutine(SwitchToBeast());
                return;
            }
            else
            {
                moveCounter = 0;
            }
        }

        if (isBeastForm)
            EvaluateBeastBehavior(distance);
        else
            EvaluateHumanBehavior(distance);
    }

    void EvaluateHumanBehavior(float distance)
    {
        if (distance > extremeRange)
            EvaluateExtremeRange();
        else if (distance > farRange)
            EvaluateFarRange();
        else if (distance > midRange)
            EvaluateMidRange();
        else
            EvaluateCloseRange();
    }

    void EvaluateBeastBehavior(float distance)
    {
        if (distance > extremeRange)
            EvaluateBeastExtremeRange();
        else if (distance > farRange)
            EvaluateBeastFarRange();
        else if (distance > midRange)
            EvaluateBeastMidRange();
        else
            EvaluateBeastCloseRange();
    }

    // =============================================
    // Human range evaluations
    // =============================================

    void EvaluateExtremeRange()
    {
        float roll = Random.value;
        if (roll < 0.85f) StartSprint();
        else StartWalk();
    }

    void EvaluateFarRange()
    {
        float roll = Random.value;
        if (roll < 0.60f)
            StartWalk();
        else if (roll < 0.75f)
            Evaluate();
        else if (roll < 0.88f && dashTimer <= 0f)
            StartCoroutine(IsPhaseTwo ? ShadowCloneDash() : DashAttack());
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
        {
            if (Random.value < 0.2f) StartCoroutine(Backstep());
            else Evaluate();
        }
        else if (roll < 0.78f && dashTimer <= 0f)
            StartCoroutine(IsPhaseTwo ? ShadowCloneDash() : DashAttack());
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
            float comboRoll = Random.value;
            if (IsPhaseTwo)
            {
                // Phase two combo weights — Attack3 available
                if (comboRoll < 0.2f && attack1Timer <= 0f)
                    StartCoroutine(Attack1());
                else if (comboRoll < 0.6f && attack2Timer <= 0f)
                    StartCoroutine(Attack2());
                else if (attack3Timer <= 0f)
                    StartCoroutine(Attack3());
                else if (attack1Timer <= 0f)
                    StartCoroutine(Attack1());
                else
                    Evaluate();
            }
            else
            {
                // Phase one combo weights
                if (comboRoll < 0.5f && attack1Timer <= 0f)
                    StartCoroutine(Attack1());
                else if (attack2Timer <= 0f)
                    StartCoroutine(Attack2());
                else if (attack1Timer <= 0f)
                    StartCoroutine(Attack1());
                else
                    Evaluate();
            }
        }
        else if (roll < 0.72f && rollAttackTimer <= 0f)
            StartCoroutine(RollAttack());
        else if (roll < 0.84f && jumpAttackTimer <= 0f)
            StartCoroutine(JumpAttack());
        else
        {
            if (Random.value < 0.35f) StartCoroutine(Backstep());
            else Evaluate();
        }
    }

    // =============================================
    // Beast range evaluations
    // =============================================

    void EvaluateBeastExtremeRange()
    {
        float roll = Random.value;
        if (roll < 0.85f) StartSprint();
        else StartWalk();
    }

    void EvaluateBeastFarRange()
    {
        float roll = Random.value;
        if (roll < 0.65f)
            StartWalk();
        else if (roll < 0.80f)
            Evaluate();
        else if (beastSpecialTimer <= 0f)
            StartCoroutine(BeastSpecial());
        else
            StartWalk();
    }

    void EvaluateBeastMidRange()
    {
        float roll = Random.value;
        if (roll < 0.45f)
            StartWalk();
        else if (roll < 0.60f)
            Evaluate();
        else if (roll < 0.78f && beastOverheadTimer <= 0f)
            StartCoroutine(BeastOverhead());
        else if (beastSpecialTimer <= 0f && Random.value < 0.3f)
            StartCoroutine(BeastSpecial());
        else
            StartWalk();
    }

    void EvaluateBeastCloseRange()
    {
        float roll = Random.value;

        if (roll < 0.55f)
        {
            float comboRoll = Random.value;
            if (comboRoll < 0.35f && beastAtk1Timer <= 0f)
                StartCoroutine(BeastAtk1());
            else if (comboRoll < 0.65f && beastAtk2Timer <= 0f)
                StartCoroutine(BeastAtk2());
            else if (beastAtk3Timer <= 0f)
                StartCoroutine(BeastAtk3());
            else if (beastAtk1Timer <= 0f)
                StartCoroutine(BeastAtk1());
            else
                Evaluate();
        }
        else if (roll < 0.70f && beastOverheadTimer <= 0f)
            StartCoroutine(BeastOverhead());
        else if (roll < 0.82f && beastSpecialChompTimer <= 0f)
            StartCoroutine(BeastSpecialChomp());
        else
        {
            if (Random.value < 0.25f) StartCoroutine(Backstep());
            else Evaluate();
        }
    }

    // =============================================
    // Movement
    // =============================================

    void StartWalk()
    {
        if (walkCoroutine != null) StopCoroutine(walkCoroutine);
        currentState = BossState.Walking;
        if (isBeastForm)
        {
            animator.SetBool("isBeastRunning", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
        else
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isRunning", false);
        }
        walkCoroutine = StartCoroutine(WalkThenEvaluate(Random.Range(0.5f, 1.2f)));
    }

    void StartSprint()
    {
        if (walkCoroutine != null) StopCoroutine(walkCoroutine);
        currentState = BossState.Sprinting;
        if (isBeastForm)
        {
            animator.SetBool("isBeastRunning", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
        else
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
        }
        walkCoroutine = StartCoroutine(WalkThenEvaluate(Random.Range(0.4f, 0.8f)));
    }

    void Evaluate()
    {
        currentState = BossState.Idle;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetBool("isBeastRunning", false);
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator WalkThenEvaluate(float maxWalkTime)
    {
        float elapsed = 0f;

        while (elapsed < maxWalkTime)
        {
            elapsed += Time.deltaTime;
            float distance = Vector2.Distance(transform.position, player.position);

            if (currentState == BossState.Sprinting && distance <= farRange) break;
            if (currentState == BossState.Walking && distance <= midRange) break;

            yield return null;
        }

        if (currentState == BossState.Walking || currentState == BossState.Sprinting)
        {
            currentState = BossState.Idle;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isBeastRunning", false);
            evaluationTimer = 0f;
        }
        walkCoroutine = null;
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
        Vector3 newPos = transform.position + new Vector3(directionX * speed * Time.deltaTime, 0f, 0f);
        newPos.x = Mathf.Clamp(newPos.x, arenaMinX, arenaMaxX);
        transform.position = newPos;
    }

    void MoveAwayFromPlayer(float speed)
    {
        float directionX = player.position.x > transform.position.x ? -1f : 1f;
        transform.position += new Vector3(directionX * speed * Time.deltaTime, 0f, 0f);
    }

    // =============================================
    // Human Attack Coroutines
    // =============================================

    IEnumerator Attack1()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack1");
        attack1Timer = attack1Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack1"));
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator Attack2()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack2");
        attack2Timer = attack2Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack2"));
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator Attack3()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack3");
        attack3Timer = attack3Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_Attack3"));
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator RollAttack()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("RollAttack");
        rollAttackTimer = rollAttackCooldown;
        moveCounter++;

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
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        isDashing = true;
        animator.SetTrigger("Dash");
        dashTimer = dashCooldown;
        moveCounter++;

        yield return new WaitForSeconds(0.607f);

        float dirX = player.position.x > transform.position.x ? 1f : -1f;
        float dashSpeed = dashDistance / dashDuration;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            Vector3 newPos = transform.position + new Vector3(dirX * dashSpeed * Time.deltaTime, 0f, 0f);
            newPos.x = Mathf.Clamp(newPos.x, arenaMinX, arenaMaxX);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(0.442f);
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator ShadowCloneDash()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        isDashing = true;
        animator.SetTrigger("Dash");
        dashTimer = dashCooldown;
        moveCounter++;

        // Spawn shadow clone at current position
        GameObject clone = null;
        if (shadowClonePrefab != null)
        {
            clone = Instantiate(shadowClonePrefab, transform.position, transform.rotation);
            SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
            if (cloneRenderer != null)
            {
                cloneRenderer.color = Color.black;
                cloneRenderer.enabled = false; // hidden until dash fires
            }
        }

        yield return new WaitForSeconds(0.607f);

        float dirX = player.position.x > transform.position.x ? 1f : -1f;
        float dashSpeed = dashDistance / dashDuration;
        float elapsed = 0f;
        Vector3 bossDestination = Vector3.zero;

        // Boss dashes forward
        while (elapsed < dashDuration)
        {
            Vector3 newPos = transform.position + new Vector3(dirX * dashSpeed * Time.deltaTime, 0f, 0f);
            newPos.x = Mathf.Clamp(newPos.x, arenaMinX, arenaMaxX);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        bossDestination = transform.position;
        isDashing = false;

        // Clone dashes toward boss destination
        if (clone != null)
        {
            SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
            if (cloneRenderer != null) cloneRenderer.enabled = true;
            float cloneElapsed = 0f;
            float cloneSpeed = cloneDashDistance / cloneDashDuration;
            Vector3 cloneStart = clone.transform.position;

            while (cloneElapsed < cloneDashDuration)
            {
                float cloneDirX = bossDestination.x > clone.transform.position.x ? 1f : -1f;
                clone.transform.position += new Vector3(cloneDirX * cloneSpeed * Time.deltaTime, 0f, 0f);
                cloneElapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(clone);
        }

        // Recovery — boss stands still, longer punish window
        yield return new WaitForSeconds(0.8f);
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator JumpAttack()
    {
        currentState = BossState.Attacking;
        animator.SetTrigger("JumpAttack");
        jumpAttackTimer = jumpAttackCooldown;
        moveCounter++;

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
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("TeleportDisappear");
        teleportTimer = teleportCooldown;
        moveCounter++;

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportDisappear"));

        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(teleportInvisibleDuration);

        float offsetX = Random.Range(teleportMinDistance, teleportMaxDistance);
        offsetX *= Random.value > 0.5f ? 1f : -1f;
        transform.position = new Vector3(
            player.position.x + offsetX,
            transform.position.y,
            transform.position.z
        );

        animator.SetTrigger("TeleportReappear");
        yield return null;
        spriteRenderer.enabled = true;

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportReappear"));
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    // =============================================
    // Beast Attack Coroutines
    // =============================================

    IEnumerator BeastAtk1()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastAtk1");
        beastAtk1Timer = beastAtk1Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastAtk1") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator BeastAtk2()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastAtk2");
        beastAtk2Timer = beastAtk2Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastAtk2") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator BeastAtk3()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastAtk3");
        beastAtk3Timer = beastAtk3Cooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastAtk3") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator BeastOverhead()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastOverhead");
        beastOverheadTimer = beastOverheadCooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastOverhead") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator BeastSpecial()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastSpecial");
        beastSpecialTimer = beastSpecialCooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastSpecial") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator BeastSpecialChomp()
    {
        currentState = BossState.Attacking;
        animator.SetBool("isBeastRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetTrigger("BeastSpecialChomp");
        beastSpecialChompTimer = beastSpecialChompCooldown;
        moveCounter++;
        yield return new WaitForSeconds(GetAnimationLength("Boss_BeastSpecialChomp") + 0.1f);
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    // =============================================
    // Phase Transitions
    // =============================================

    IEnumerator PhaseTransition()
    {
        isTransitioning = true;

        if (walkCoroutine != null)
            StopCoroutine(walkCoroutine);
        
        currentState = BossState.Attacking;
        Debug.Log("PhaseTransition started — Transform clip length: " + GetAnimationLength("Boss_Transform"));

        // Fire transformation trigger
        animator.speed = transformationSpeed;
        animator.SetTrigger("TransformToDemon");

        // Start health bar fade immediately
        BossHealthBar healthBar = FindFirstObjectByType<BossHealthBar>();
        float transformLength = GetAnimationLength("Boss_Transform") / transformationSpeed;
        healthBar?.FadeOutAndChangeName(bossNamePhaseTwo, transformLength * 0.6f);

        // AOE damage zone during black hole portion
        float aoeElapsed = 0f;
        while (aoeElapsed < transitionAOEDuration)
        {
            // Check if player is within AOE radius
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= transitionAOERadius)
                {
                    PlayerController pc = player.GetComponent<PlayerController>();
                    if (pc != null && !pc.IsInvincible)
                        pc.SetHealth(pc.GetHealth() - transitionAOEDamage * Time.deltaTime);
                }
            }
            aoeElapsed += Time.deltaTime;
            yield return null;
        }

        // Wait for full transform animation
        yield return new WaitForSeconds(GetAnimationLength("Boss_Transform") - transitionAOEDuration);

        animator.speed = 1f; // restore normal speed
        isBeastForm = true;

        // Switch to beast form
        isBeastForm = true;
        animator.SetBool("isBeast", true);

        isTransitioning = false;
        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    IEnumerator SwitchToBeast()
    {
        isTransitioning = true;

        if (walkCoroutine != null)
            StopCoroutine(walkCoroutine);
        
        currentState = BossState.Attacking;
        animator.SetTrigger("TransformToDemon");

        yield return new WaitForSeconds(GetAnimationLength("Boss_Transform"));

        isBeastForm = true;
        animator.SetBool("isBeast", true);
        moveCounter = 0;

        isTransitioning = false;
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    IEnumerator SwitchToHuman()
    {
        isTransitioning = true;
        currentState = BossState.Attacking;
        animator.SetTrigger("TransformToHuman");

        yield return new WaitForSeconds(GetAnimationLength("Boss_Beast2Human"));

        isBeastForm = false;
        animator.SetBool("isBeast", false);
        moveCounter = 0;

        isTransitioning = false;
        currentState = BossState.Idle;
        evaluationTimer = 0f;
    }

    // =============================================
    // Damage and Poise
    // =============================================

    public void TakeDamage(float damage, float poiseDamage = 0f)
    {
        if (currentState == BossState.Dead) return;
        if (isTransitioning) return;

        SetHealth(GetHealth() - damage);
        OnHealthChanged?.Invoke(GetHealth(), GetMaxHealth());

        if (!isDashing)
        {
            currentPoise -= poiseDamage;
            poiseRegenDelayTimer = poiseRegenDelay;

            if (currentPoise <= 0f)
                StartCoroutine(Stagger());
        }
    }

    IEnumerator Stagger()
    {
        currentState = BossState.Staggered;
        animator.SetTrigger(isBeastForm ? "BeastTakeHit" : "TakeHit");
        currentPoise = maxPoise;

        yield return new WaitForSeconds(staggerDuration);

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = BossState.Dead;
        StopAllCoroutines();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Handle beast form — transform back to human first
        if (isBeastForm)
        {
            animator.SetBool("isDying", true);
            animator.SetTrigger("DeathTransform");
            yield return new WaitForSeconds(GetAnimationLength("Boss_Beast2Human"));
            isBeastForm = false;
            animator.SetBool("isDying", false);
        }

        // Play death animation
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(GetAnimationLength("Boss_Death"));

        // Fade out boss sprite and health bar simultaneously
        BossHealthBar healthBar = FindFirstObjectByType<BossHealthBar>();
        healthBar?.FadeOut();

        float fadeDuration = 2.5f;
        float elapsed = 0f;
        Color spriteColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteColor.a = alpha;
            spriteRenderer.color = spriteColor;
            yield return null;
        }

        spriteRenderer.enabled = false;

        // Unlock door after boss death
        DoorController door = FindFirstObjectByType<DoorController>();
        if (door != null)
        {
            door.UnlockDoor();
        }

        // Spawn bonfire
        if (bonfirePrefab != null)
        {
            GameObject bonfire = Instantiate(bonfirePrefab, bonfireSpawnPoint.position, Quaternion.identity);
            bonfire.SetActive(true);
        }

        // Show victory screen
        VictoryScreen victory = FindFirstObjectByType<VictoryScreen>();
        victory?.Show();

        // Cleanup
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

    // =============================================
    // Entrance
    // =============================================

    public void BossEntrance()
    {
        if (currentState != BossState.Dormant) return;
        StartCoroutine(EntranceSequence());
    }

    IEnumerator EntranceSequence()
    {
        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.5f);

        animator.SetTrigger("TeleportReappear");
        yield return null;
        spriteRenderer.enabled = true;

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportReappear"));

        BossHealthBar healthBar = FindFirstObjectByType<BossHealthBar>();
        healthBar?.ShowHealthBar();
        OnHealthChanged?.Invoke(GetHealth(), GetMaxHealth());

        GameEventManager.Instance?.HideHUD();

        currentState = BossState.Idle;
        evaluationTimer = Random.Range(minEvaluationTime, maxEvaluationTime);
    }
}