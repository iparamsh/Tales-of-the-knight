using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [SerializeField] private float maxFP = 100f;
    [SerializeField] private float currentFP = 100f;

    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnFPChanged;
    public event Action<float, float> OnStaminaChanged;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentFP => currentFP;
    public float MaxFP => maxFP;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

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

    // Stamina methods
    public void InitStamina(float maxV, float curV)
    {
        maxStamina = Mathf.Max(0f, maxV);
        currentStamina = Mathf.Clamp(curV, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void ChangeStamina(float amount)
    {
        SetStamina(currentStamina + amount);
    }

    public void SetMaxStamina(float value)
    {
        maxStamina = Mathf.Max(0f, value);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
