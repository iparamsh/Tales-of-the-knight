using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Input Actions")]
    public InputAction LightAttackAction;
    public InputAction HeavyAttackAction;
    public InputAction PlungeAction;

    [Header("Light Attack Cooldowns")]
    public float lightShortCooldown = 0.4f;
    public float lightFullComboCooldown = 1.2f;
    public float lightComboLinkDelay = 0.1f;

    [Header("Heavy Attack Cooldowns")]
    public float heavyShortCooldown = 0.7f;
    public float heavyFullComboCooldown = 1.8f;

    [Header("Plunge Attack")]
    public float plungeCooldown = 2.5f;
    public float plungeLaunchForce = 18f;
    public float plungeSlamForce = 28f;
    public float plungeRiseTime = 0.5f;

    [Header("Combo Window")]
    public float comboWindowDuration = 0.8f;

    // =============================================
    // Private state
    // =============================================
    public enum CombatState
    {
        None,
        LightAttack1,
        LightAttack2,
        LightAttack3,
        LightAttack4,
        HeavyAttack1,
        HeavyAttack2,
        Plunge
    }

    private CombatState state = CombatState.None;
    private bool comboQueued = false;
    private float cooldownTimer = 0f;
    private Coroutine activeAttack;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private StaminaSystem staminaSystem;
    private PlayerStats playerStats;
    private float inputBlockTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        staminaSystem = GetComponent<StaminaSystem>();
        playerStats = GetComponent<PlayerStats>();

        LightAttackAction.Enable();
        HeavyAttackAction.Enable();
        PlungeAction.Enable();

        LightAttackAction.performed += OnLightAttack;
        HeavyAttackAction.performed += OnHeavyAttack;
        PlungeAction.performed += OnPlunge;

        if (playerStats != null)
            playerStats.OnDeath += OnPlayerDied;
    }

    void OnDestroy()
    {
        LightAttackAction.performed -= OnLightAttack;
        HeavyAttackAction.performed -= OnHeavyAttack;
        PlungeAction.performed -= OnPlunge;
        LightAttackAction.Disable();
        HeavyAttackAction.Disable();
        PlungeAction.Disable();

        if (playerStats != null)
            playerStats.OnDeath -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (activeAttack != null)
            StopCoroutine(activeAttack);

        state = CombatState.None;
        comboQueued = false;
        cooldownTimer = 0f;

        if (playerController != null)
            playerController.IsPlunging = false;

        LightAttackAction.Disable();
        HeavyAttackAction.Disable();
        PlungeAction.Disable();
    }

    public void ReviveActions()
    {
        state = CombatState.None;
        comboQueued = false;
        cooldownTimer = 0f;

        LightAttackAction.Enable();
        HeavyAttackAction.Enable();
        PlungeAction.Enable();
    }

    void Update()
    {
        if (inputBlockTimer > 0f)
            inputBlockTimer -= Time.unscaledDeltaTime;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // =============================================
    // Input handlers
    // =============================================

    void OnLightAttack(InputAction.CallbackContext ctx)
    {
        // Don't fire light attack if shift is held — that's heavy attack
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.IsOnLadder) return;

        if (inputBlockTimer > 0f) return;
        if (state == CombatState.LightAttack1 || state == CombatState.LightAttack2 || state == CombatState.LightAttack3)
        {
            comboQueued = true;
            return;
        }

        if (state != CombatState.None || cooldownTimer > 0f) return;
        if (staminaSystem == null || !staminaSystem.TryLightAttack()) return;

        activeAttack = StartCoroutine(LightAttack1());
    }

    void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.IsOnLadder) return;
        
        // Queue combo while attack 1 is active
        if (state == CombatState.HeavyAttack1)
        {
            comboQueued = true;
            return;
        }

        if (state != CombatState.None || cooldownTimer > 0f) return;
        if (staminaSystem == null || !staminaSystem.TryHeavyAttack()) return;

        activeAttack = StartCoroutine(HeavyAttack1());
    }

    void OnPlunge(InputAction.CallbackContext ctx)
    {
        if (state != CombatState.None || cooldownTimer > 0f) return;
        if (staminaSystem == null || !staminaSystem.TryPlunge()) return;
        if (!TryConsumeFP(staminaSystem.plungeAttackFPCost)) return;

        activeAttack = StartCoroutine(PlungeAttack());
    }

    // =============================================
    // Light attack chain
    // =============================================

    IEnumerator LightAttack1()
    {
        state = CombatState.LightAttack1;
        comboQueued = false;
        animator.SetTrigger("LightAttack1");

        GameObject hitboxObj = transform.Find("Hitbox_LightAttack1")?.gameObject;
        PlayerHitbox ph = hitboxObj?.GetComponent<PlayerHitbox>();

        // Startup
        yield return new WaitForSeconds(0.3f);

        Debug.Log("LightAttack1 active window — ph: " + (ph != null ? "found" : "NULL") + " hitboxObj: " + (hitboxObj != null ? "found" : "NULL"));

        // Active — check every frame while listening for combo
        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.2f)
        {
            if (!hasHit && ph != null)
                hasHit = ph.CheckHitBoss();
            if (comboQueued) break;
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery — still listen for combo
        float recoveryElapsed = 0f;
        while (recoveryElapsed < 0.1f)
        {
            if (comboQueued) break;
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryLightAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(LightAttack2());
            yield break;
        }

        EnterCooldown(lightShortCooldown);
    }

    IEnumerator LightAttack2()
    {
        state = CombatState.LightAttack2;
        comboQueued = false;
        animator.SetTrigger("LightAttack2");

        GameObject hitboxObj = transform.Find("Hitbox_LightAttack2")?.gameObject;
        PlayerHitbox ph = hitboxObj?.GetComponent<PlayerHitbox>();

        // Startup
        yield return new WaitForSeconds(0.1f);

        // Active
        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.2f)
        {
            if (!hasHit && ph != null)
                hasHit = ph.CheckHitBoss();
            if (comboQueued) break;
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery
        float recoveryElapsed = 0f;
        while (recoveryElapsed < 0.1f)
        {
            if (comboQueued) break;
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryLightAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(LightAttack3());
            yield break;
        }

        EnterCooldown(lightShortCooldown);
    }

    IEnumerator LightAttack3()
    {
        state = CombatState.LightAttack3;
        comboQueued = false;
        animator.SetTrigger("LightAttack3");

        GameObject hitboxObj = transform.Find("Hitbox_LightAttack3")?.gameObject;
        PlayerHitbox ph = hitboxObj?.GetComponent<PlayerHitbox>();

        // Startup
        yield return new WaitForSeconds(0.1f);

        // Active
        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.3f)
        {
            if (!hasHit && ph != null)
                hasHit = ph.CheckHitBoss();
            if (comboQueued) break;
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery
        float recoveryElapsed = 0f;
        while (recoveryElapsed < 0.1f)
        {
            if (comboQueued) break;
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryLightAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(LightAttack4());
            yield break;
        }

        EnterCooldown(lightShortCooldown);
    }

    IEnumerator LightAttack4()
    {
        state = CombatState.LightAttack4;
        comboQueued = false;
        animator.SetTrigger("LightAttack4");

        GameObject hitboxObj = transform.Find("Hitbox_LightAttack4")?.gameObject;
        PlayerHitbox ph = hitboxObj?.GetComponent<PlayerHitbox>();

        // Startup
        yield return new WaitForSeconds(0.2f);

        // Active
        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.3f)
        {
            if (!hasHit && ph != null)
                hasHit = ph.CheckHitBoss();
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery
        yield return new WaitForSeconds(0.1f);

        EnterCooldown(lightFullComboCooldown);
    }

    // =============================================
    // Heavy attack chain
    // =============================================

    IEnumerator HeavyAttack1()
    {
        state = CombatState.HeavyAttack1;
        comboQueued = false;
        animator.SetTrigger("HeavyAttack1");

        GameObject hitboxObj1a = transform.Find("Hitbox_HeavyAttack1a")?.gameObject;
        PlayerHitbox ph1a = hitboxObj1a?.GetComponent<PlayerHitbox>();
        GameObject hitboxObj1b = transform.Find("Hitbox_HeavyAttack1b")?.gameObject;
        PlayerHitbox ph1b = hitboxObj1b?.GetComponent<PlayerHitbox>();

        // First hit — no startup, fires immediately
        float activeElapsed = 0f;
        bool hasHit1a = false;
        while (activeElapsed < 0.25f)
        {
            if (!hasHit1a && ph1a != null)
                hasHit1a = ph1a.CheckHitBoss();
            if (comboQueued) break;
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Gap
        float gapElapsed = 0f;
        while (gapElapsed < 0.25f)
        {
            if (comboQueued) break;
            gapElapsed += Time.deltaTime;
            yield return null;
        }

        // Second hit
        activeElapsed = 0f;
        bool hasHit1b = false;
        while (activeElapsed < 0.25f)
        {
            if (!hasHit1b && ph1b != null)
                hasHit1b = ph1b.CheckHitBoss();
            if (comboQueued) break;
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery
        float recoveryElapsed = 0f;
        while (recoveryElapsed < 0.125f)
        {
            if (comboQueued) break;
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryHeavyAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(HeavyAttack2());
            yield break;
        }

        EnterCooldown(heavyShortCooldown);
    }

    IEnumerator HeavyAttack2()
    {
        state = CombatState.HeavyAttack2;
        comboQueued = false;
        animator.SetTrigger("HeavyAttack2");

        yield return new WaitForSeconds(GetAnimationLength("Player_HeavyAttack2"));

        EnterCooldown(heavyFullComboCooldown);
    }

    // =============================================
    // Plunge
    // =============================================

    IEnumerator PlungeAttack()
    {
        state = CombatState.Plunge;
        playerController.IsPlunging = true;

        // Phase 1: launch upward
        animator.SetBool("isMoving", false);
        rb.linearVelocity = new Vector2(0f, plungeLaunchForce);
        animator.SetTrigger("Jump");

        yield return new WaitForSeconds(plungeRiseTime);

        // Phase 2: slam downward
        rb.linearVelocity = new Vector2(0f, -plungeSlamForce);
        animator.SetTrigger("Plunge");

        GameObject hitboxObj = transform.Find("Hitbox_Plunge")?.gameObject;
        PlayerHitbox ph = hitboxObj?.GetComponent<PlayerHitbox>();

        // Startup
        yield return new WaitForSeconds(0.286f);

        // Active
        float activeElapsed = 0f;
        bool hasHit = false;
        while (activeElapsed < 0.429f)
        {
            if (!hasHit && ph != null)
                hasHit = ph.CheckHitBoss();
            activeElapsed += Time.deltaTime;
            yield return null;
        }

        // Recovery
        yield return new WaitForSeconds(0.286f);

        playerController.IsPlunging = false;
        EnterCooldown(plungeCooldown);
    }

    // =============================================
    // Interrupt (called by damage system on hit)
    // =============================================

    public void InterruptAttack()
    {
        bool interruptible = state == CombatState.LightAttack1
            || state == CombatState.LightAttack2
            || state == CombatState.LightAttack3
            || state == CombatState.LightAttack4
            || state == CombatState.HeavyAttack2;

        if (!interruptible) return;

        if (activeAttack != null)
            StopCoroutine(activeAttack);

        state = CombatState.None;
        comboQueued = false;
        cooldownTimer = 0f;
        // Hit stun animation is expected to be triggered by the damage system
    }

    // =============================================
    // Utility
    // =============================================

    void EnterCooldown(float duration)
    {
        state = CombatState.None;
        comboQueued = false;
        cooldownTimer = duration;
    }

    bool TryConsumeFP(float amount)
    {
        if (playerStats == null) return true;
        if (playerStats.CurrentFP < amount) return false;
        playerStats.ChangeFP(-amount);
        return true;
    }

    float GetAnimationLength(string clipName)
    {
        if (animator == null) return 0.5f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        Debug.LogWarning($"[PlayerCombat] Animation clip not found: {clipName}");
        return 0.5f;
    }

    public void BlockInputBriefly(float duration = 0.2f)
    {
        inputBlockTimer = duration;
    }
    
    public CombatState GetState()
    {
        return state;
    }
}
