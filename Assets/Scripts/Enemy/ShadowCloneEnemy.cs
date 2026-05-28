using UnityEngine;
using System.Collections;

public class ShadowCloneEnemy : Enemy
{
    [Header("Clone Identity")]
    public string cloneName = "Shadow Clone";

    [Header("Patrol")]
    public float patrolMinX = 0f;
    public float patrolMaxX = 10f;
    public float patrolSpeed = 1.5f;

    [Header("Combat")]
    public float aggroRange = 12f;
    public float attackRange = 3f;
    public float attack1Cooldown = 2f;
    public float attack2Cooldown = 4f;
    public float jumpAttackCooldown = 5f;
    public float jumpAttackDistance = 4f;
    public float jumpAttackDuration = 0.6f;

    [Header("Attack Damage")]
    public float attack1Damage = 10f;
    public float attack1PoiseDamage = 8f;
    public float attack2aDamage = 10f;
    public float attack2aPoiseDamage = 8f;
    public float attack2bDamage = 10f;
    public float attack2bPoiseDamage = 8f;
    public float jumpAttackDamage = 15f;
    public float jumpAttackPoiseDamage = 10f;

    [Header("Death")]
    public string deathDialogue = "...you've grown stronger... I will be waiting...";
    public float deathDialogueDelay = 0.5f;

    [Header("References")]
    public Chest guardedChest;
    public FloatingHealthBar floatingHealthBar;

    private enum CloneState
    {
        Dormant,
        Patrolling,
        Attacking,
        Dead
    }

    private CloneState currentState = CloneState.Dormant;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isDead = false;
    private bool isAggroed = false;

    private float attack1Timer = 0f;
    private float attack2Timer = 0f;
    private float jumpAttackTimer = 0f;
    private float evaluationTimer = 0f;
    private float patrolDirection = 1f;

    protected override void OnStart()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start invisible
        spriteRenderer.enabled = false;

        // Apply dark purple tint
        spriteRenderer.color = new Color(0.15f, 0.05f, 0.2f, 1f);
    }

    public void Aggro()
    {
        Debug.Log("Aggro called — isAggroed: " + isAggroed + " isDead: " + isDead + " clone: " + gameObject.name);
        if (isAggroed || isDead) return;
        isAggroed = true;
        StartCoroutine(AggroSequence());
    }

    IEnumerator AggroSequence()
    {
        Debug.Log("AggroSequence started — animator: " + (animator != null ? "found" : "NULL") + " spriteRenderer: " + (spriteRenderer != null ? "found" : "NULL"));
        animator.SetTrigger("TeleportReappear");
        yield return null;
        spriteRenderer.enabled = true;
        Debug.Log("Sprite enabled — clip length: " + GetAnimationLength("Boss_TeleportReappear"));

        yield return new WaitForSeconds(GetAnimationLength("Boss_TeleportReappear"));

        currentState = CloneState.Patrolling;
        evaluationTimer = 0f;
        Debug.Log("AggroSequence complete — currentState: Patrolling");
    }

    void Patrol()
    {
        transform.position += new Vector3(patrolDirection * patrolSpeed * Time.deltaTime, 0f, 0f);

        if (transform.position.x >= patrolMaxX)
            patrolDirection = -1f;
        else if (transform.position.x <= patrolMinX)
            patrolDirection = 1f;

        animator.SetBool("isWalking", true);
    }

    void Update()
    {
        if (currentState == CloneState.Dormant || currentState == CloneState.Dead) return;
        if (currentState == CloneState.Attacking) return;
        if (player == null) return;

        TickCooldowns();
        HandleFacing();

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= aggroRange)
        {
            if (distance > attackRange)
            {
                // Chase player every frame
                float dirX = player.position.x > transform.position.x ? 1f : -1f;
                transform.position += new Vector3(dirX * patrolSpeed * 2f * Time.deltaTime, 0f, 0f);
                animator.SetBool("isWalking", true);
            }
            else
            {
                // In attack range — evaluate
                animator.SetBool("isWalking", false);
                if (evaluationTimer > 0f)
                    evaluationTimer -= Time.deltaTime;
                else
                    EvaluateBehavior();
            }
        }
        else
        {
            Patrol();
        }
    }

    void EvaluateBehavior()
    {
        float roll = Random.value;
        if (roll < 0.4f && attack1Timer <= 0f)
            StartCoroutine(Attack1());
        else if (roll < 0.7f && attack2Timer <= 0f)
            StartCoroutine(Attack2());
        else if (jumpAttackTimer <= 0f)
            StartCoroutine(JumpAttack());
        else
            evaluationTimer = Random.Range(0.2f, 0.5f);
    }

    void HandleFacing()
    {
        if (player != null)
            spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    void TickCooldowns()
    {
        if (attack1Timer > 0f) attack1Timer -= Time.deltaTime;
        if (attack2Timer > 0f) attack2Timer -= Time.deltaTime;
        if (jumpAttackTimer > 0f) jumpAttackTimer -= Time.deltaTime;
    }

    // =============================================
    // Attacks — reuse same hitbox GameObjects as boss
    // =============================================

    bool CheckHitPlayer(GameObject hitboxObj, float damage, float poiseDamage)
    {
        if (hitboxObj == null) return false;
        BoxCollider2D col = hitboxObj.GetComponent<BoxCollider2D>();
        if (col == null) return false;

        Vector2 center = (Vector2)hitboxObj.transform.TransformPoint(col.offset);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, col.size, 0f, LayerMask.GetMask("Hurtbox"));

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("PlayerHurtbox")) continue;
            PlayerController pc = hit.GetComponentInParent<PlayerController>();
            if (pc == null || pc.IsInvincible) continue;
            pc.SetHealth(pc.GetHealth() - damage);
            pc.GetComponent<StaminaSystem>()?.InterruptRegen();
            return true;
        }
        return false;
    }

    float GetAnimationLength(string clipName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 1f;
    }

    IEnumerator Attack1()
    {
        currentState = CloneState.Attacking;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("Attack1");
        attack1Timer = attack1Cooldown;

        GameObject hitbox = transform.Find("Hitbox_Attack1")?.gameObject;

        yield return new WaitForSeconds(0.43f);

        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.15f)
        {
            if (!hasHit)
                hasHit = CheckHitPlayer(hitbox, attack1Damage, attack1PoiseDamage);
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.285f);
        currentState = CloneState.Patrolling;
        evaluationTimer = Random.Range(0.1f, 0.4f);
    }

    IEnumerator Attack2()
    {
        currentState = CloneState.Attacking;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("Attack2");
        attack2Timer = attack2Cooldown;

        GameObject hitbox2a = transform.Find("Hitbox_Attack2a")?.gameObject;
        GameObject hitbox2b = transform.Find("Hitbox_Attack2b")?.gameObject;

        yield return new WaitForSeconds(0.428f);

        float activeElapsed = 0f;
        bool hasHit2a = false;
        while (activeElapsed < 0.071f)
        {
            if (!hasHit2a)
                hasHit2a = CheckHitPlayer(hitbox2a, attack2aDamage, attack2aPoiseDamage);
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.428f);

        activeElapsed = 0f;
        bool hasHit2b = false;
        while (activeElapsed < 0.214f)
        {
            if (!hasHit2b)
                hasHit2b = CheckHitPlayer(hitbox2b, attack2bDamage, attack2bPoiseDamage);
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.285f);
        currentState = CloneState.Patrolling;
        evaluationTimer = Random.Range(0.1f, 0.4f);
    }

    IEnumerator JumpAttack()
    {
        currentState = CloneState.Attacking;
        animator.SetTrigger("JumpAttack");
        jumpAttackTimer = jumpAttackCooldown;

        GameObject hitbox = transform.Find("Hitbox_JumpAttack")?.gameObject;

        float elapsed = 0f;
        while (elapsed < jumpAttackDuration)
        {
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            Vector3 newPos = transform.position + new Vector3(dirX * (jumpAttackDistance / jumpAttackDuration) * Time.deltaTime, 0f, 0f);
            newPos.x = Mathf.Clamp(newPos.x, patrolMinX, patrolMaxX);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        float activeElapsed2 = 0f;
        bool hasHit = false;
        while (activeElapsed2 < 0.143f)
        {
            if (!hasHit)
                hasHit = CheckHitPlayer(hitbox, jumpAttackDamage, jumpAttackPoiseDamage);
            activeElapsed2 += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.571f);
        currentState = CloneState.Patrolling;
        evaluationTimer = Random.Range(0.1f, 0.4f);
    }

    // =============================================
    // Damage and Death
    // =============================================

    public void TakeDamage(float damage, float poiseDamage = 0f)
    {
        if (isDead) return;

        SetHealth(GetHealth() - damage);

        Debug.Log("Clone TakeDamage — health: " + GetHealth() + " max: " + GetMaxHealth() + " healthbar: " + (floatingHealthBar != null ? "found" : "NULL"));

        if (floatingHealthBar != null)
            floatingHealthBar.UpdateHealth(GetHealth(), GetMaxHealth());

        if (GetHealth() <= 0f)
            Die();
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = CloneState.Dead;
        StopAllCoroutines();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(deathDialogueDelay);

        // Show death dialogue
        DeathDialogueOverlay overlay = FindFirstObjectByType<DeathDialogueOverlay>();
        if (overlay != null)
            yield return StartCoroutine(overlay.Show(deathDialogue));

        // Notify chest
        if (guardedChest != null)
            guardedChest.NotifyCloneDefeated();

        // Fade out sprite
        float elapsed = 0f;
        float fadeDuration = 1.5f;
        Color c = spriteRenderer.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = c;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}