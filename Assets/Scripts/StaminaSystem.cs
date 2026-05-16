using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float regenRate = 20f;
    public float regenDelay = 1f;

    [Header("Stamina Costs")]
    public float rollCost = 25f;
    public float lightAttackCost = 15f;
    public float heavyAttackCost = 25f;
    public float blockCost = 0f;
    public float sprintDrainRate = 10f;
    public bool sprintDrainsStamina = false;

    [Header("Exhaustion")]
    public float exhaustionDuration = 1.5f;

    // =============================================
    // Public state
    // =============================================
    public float CurrentStamina { get; private set; }
    public float MaxStamina => maxStamina;
    public bool IsExhausted { get; private set; }

    // =============================================
    // Private state
    // =============================================
    private float regenDelayTimer = 0f;
    private float exhaustionTimer = 0f;
    private PlayerController playerController;
    private PlayerStats playerStats;

    void Start()
    {
        CurrentStamina = maxStamina;
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
        playerStats?.InitStamina(maxStamina, maxStamina);
    }

    void Update()
    {
        HandleExhaustion();
        HandleSprintDrain();
        HandleRegen();
    }

    void HandleExhaustion()
    {
        if (!IsExhausted) return;
        exhaustionTimer -= Time.deltaTime;
        if (exhaustionTimer <= 0f)
            IsExhausted = false;
    }

    void HandleSprintDrain()
    {
        if (!sprintDrainsStamina) return;
        if (playerController == null) return;

        bool isSprinting = playerController.SprintAction != null
            && playerController.SprintAction.enabled
            && playerController.SprintAction.IsPressed()
            && playerController.MoveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f;

        if (isSprinting && !IsExhausted)
        {
            regenDelayTimer = regenDelay;
            ConsumeStamina(sprintDrainRate * Time.deltaTime);
        }
    }

    void HandleRegen()
    {
        if (IsExhausted) return;
        if (CurrentStamina >= maxStamina) return;
        if (regenDelayTimer > 0f)
        {
            regenDelayTimer -= Time.deltaTime;
            return;
        }

        CurrentStamina = Mathf.Min(CurrentStamina + regenRate * Time.deltaTime, maxStamina);
        NotifyStaminaChanged();
    }

    public bool ConsumeStamina(float amount)
    {
        if (IsExhausted) return false;

        CurrentStamina = Mathf.Max(CurrentStamina - amount, 0f);
        regenDelayTimer = regenDelay;
        NotifyStaminaChanged();

        if (CurrentStamina <= 0f)
        {
            TriggerExhaustion();
            return false;
        }

        return true;
    }

    public void InterruptRegen()
    {
        regenDelayTimer = regenDelay;
    }

    void TriggerExhaustion()
    {
        IsExhausted = true;
        exhaustionTimer = exhaustionDuration;
        Debug.Log("Player exhausted");
    }

    public bool TryRoll()
    {
        if (IsExhausted) return false;
        if (CurrentStamina < rollCost) return false;
        return ConsumeStamina(rollCost);
    }

    void NotifyStaminaChanged()
    {
        Debug.Log($"[StaminaSystem] NotifyStaminaChanged called, stamina: {CurrentStamina:F1}");
        playerStats?.SetStamina(CurrentStamina);
    }
}