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

    [Header("Plunge Cooldown")]
    public float plungeCooldown = 2.5f;

    // =============================================
    // Private state
    // =============================================
    private enum CombatState
    {
        None,
        LightAttack1,
        LightAttack2,
        LightAttack3,
        HeavyAttack1,
        HeavyAttack2,
        Plunge
    }

    private CombatState state = CombatState.None;
    private bool comboQueued = false;
    private float cooldownTimer = 0f;
    private Coroutine activeAttack;

    private Animator animator;
    private StaminaSystem staminaSystem;
    private PlayerStats playerStats;

    void Start()
    {
        animator = GetComponent<Animator>();
        staminaSystem = GetComponent<StaminaSystem>();
        playerStats = GetComponent<PlayerStats>();

        LightAttackAction.Enable();
        HeavyAttackAction.Enable();
        PlungeAction.Enable();

        LightAttackAction.performed += OnLightAttack;
        HeavyAttackAction.performed += OnHeavyAttack;
        PlungeAction.performed += OnPlunge;
    }

    void OnDestroy()
    {
        LightAttackAction.performed -= OnLightAttack;
        HeavyAttackAction.performed -= OnHeavyAttack;
        PlungeAction.performed -= OnPlunge;
        LightAttackAction.Disable();
        HeavyAttackAction.Disable();
        PlungeAction.Disable();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // =============================================
    // Input handlers
    // =============================================

    void OnLightAttack(InputAction.CallbackContext ctx)
    {
        // Queue combo while attack 1 or 2 is active
        if (state == CombatState.LightAttack1 || state == CombatState.LightAttack2)
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

        float duration = GetAnimationLength("LightAttack1");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (comboQueued) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryLightAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(LightAttack2());
        }
        else
        {
            if (elapsed < duration)
                yield return new WaitForSeconds(duration - elapsed);
            EnterCooldown(lightShortCooldown);
        }
    }

    IEnumerator LightAttack2()
    {
        state = CombatState.LightAttack2;
        comboQueued = false;
        animator.SetTrigger("LightAttack2");

        float duration = GetAnimationLength("LightAttack2");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (comboQueued) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (comboQueued && staminaSystem.TryLightAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(LightAttack3());
        }
        else
        {
            if (elapsed < duration)
                yield return new WaitForSeconds(duration - elapsed);
            EnterCooldown(lightShortCooldown);
        }
    }

    IEnumerator LightAttack3()
    {
        state = CombatState.LightAttack3;
        comboQueued = false;
        animator.SetTrigger("LightAttack3");

        yield return new WaitForSeconds(GetAnimationLength("LightAttack3"));

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

        yield return new WaitForSeconds(GetAnimationLength("Player_HeavyAttack1"));

        if (comboQueued && staminaSystem.TryHeavyAttack())
        {
            comboQueued = false;
            activeAttack = StartCoroutine(HeavyAttack2());
        }
        else
        {
            EnterCooldown(heavyShortCooldown);
        }
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
        animator.SetTrigger("Plunge");

        yield return new WaitForSeconds(GetAnimationLength("Player_Plunge"));

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
}
