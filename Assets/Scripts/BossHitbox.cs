using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    public float damage = 15f;
    public float poiseDamage = 10f;

    private bool hasHit = false;

    void OnEnable()
    {
        hasHit = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (hasHit) return;
        if (!other.CompareTag("PlayerHurtbox")) return;

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        if (pc.IsInvincible) return;

        hasHit = true;
        pc.SetHealth(pc.GetHealth() - damage);
        pc.GetComponent<StaminaSystem>()?.InterruptRegen();
    }
}