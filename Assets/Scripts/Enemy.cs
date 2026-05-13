using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    protected float currentHealth;

    // =============================================
    // Public interface for UI and other systems
    // =============================================
    public float GetHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    // =============================================
    // Virtual methods — override in subclasses
    // =============================================
    protected virtual void OnStart() { }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + " died");
        Destroy(gameObject);
    }

    void Start()
    {
        currentHealth = maxHealth;
        OnStart();
    }
}