using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [SerializeField] private float maxFP = 100f;
    [SerializeField] private float currentFP = 100f;

    // Stamina is now managed by StaminaSystem, not stored locally
    private StaminaSystem staminaSystem;
    private float lastStaminaValue = -1f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnFPChanged;
    public event Action<float, float> OnStaminaChanged;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentFP => currentFP;
    public float MaxFP => maxFP;
    
    // Stamina values now proxy from StaminaSystem
    public float CurrentStamina => staminaSystem != null ? staminaSystem.CurrentStamina : 0f;
    public float MaxStamina => staminaSystem != null ? staminaSystem.MaxStamina : 0f;

    void Start()
    {
        // Find or create StaminaSystem on the same GameObject
        staminaSystem = GetComponent<StaminaSystem>();
        if (staminaSystem == null)
        {
            staminaSystem = gameObject.AddComponent<StaminaSystem>();
        }
        
        // Ensure initial health/FP/stamina notifications are sent to UI
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnFPChanged?.Invoke(CurrentFP, MaxFP);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        
        lastStaminaValue = CurrentStamina;
    }

    void Update()
    {
        // Monitor StaminaSystem for changes and trigger OnStaminaChanged
        if (staminaSystem != null && Math.Abs(lastStaminaValue - CurrentStamina) > 0.01f)
        {
            lastStaminaValue = CurrentStamina;
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }
    }

    public void InitHealth(float maxH, float curH)
    {
        maxHealth = Mathf.Max(0f, maxH);
        currentHealth = Mathf.Clamp(curH, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ChangeHealth(float amount)
    {
        SetHealth(currentHealth + amount);
    }

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(0f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // FP methods
    public void InitFP(float maxV, float curV)
    {
        maxFP = Mathf.Max(0f, maxV);
        currentFP = Mathf.Clamp(curV, 0f, maxFP);
        OnFPChanged?.Invoke(currentFP, maxFP);
    }

    public void SetFP(float value)
    {
        currentFP = Mathf.Clamp(value, 0f, maxFP);
        OnFPChanged?.Invoke(currentFP, maxFP);
    }

    public void ChangeFP(float amount)
    {
        SetFP(currentFP + amount);
    }

    public void SetMaxFP(float value)
    {
        maxFP = Mathf.Max(0f, value);
        currentFP = Mathf.Clamp(currentFP, 0f, maxFP);
        OnFPChanged?.Invoke(currentFP, maxFP);
    }

    // Stamina methods (deprecated - use StaminaSystem directly)
    // These are kept for backwards compatibility but are now no-ops
    public void InitStamina(float maxV, float curV)
    {
        // MaxStamina is read-only; configured through StaminaSystem inspector settings
        Debug.LogWarning("PlayerStats.InitStamina() is deprecated. Configure max stamina on StaminaSystem component.");
    }

    public void SetStamina(float value)
    {
        if (staminaSystem != null)
        {
            // Cannot set stamina directly; use StaminaSystem methods instead
            Debug.LogWarning("PlayerStats.SetStamina() is deprecated. Use StaminaSystem.ConsumeStamina() instead.");
        }
    }

    public void ChangeStamina(float amount)
    {
        if (staminaSystem != null)
        {
            if (amount < 0)
            {
                staminaSystem.ConsumeStamina(-amount);
            }
            else
            {
                // Cannot directly add stamina; only happens through regeneration
                Debug.LogWarning("PlayerStats.ChangeStamina() for positive values is deprecated. Stamina regenerates automatically.");
            }
        }
    }

    public void SetMaxStamina(float value)
    {
        // MaxStamina is read-only; configured through StaminaSystem inspector settings
        Debug.LogWarning("PlayerStats.SetMaxStamina() is deprecated. Configure max stamina on StaminaSystem component.");
    }
}
