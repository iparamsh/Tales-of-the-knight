using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public float damage = 20f;
    public float poiseDamage = 15f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("BossHurtbox")) return;

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss == null) return;

        boss.TakeDamage(damage, poiseDamage);
        Debug.Log("Player hit boss — attack: " + gameObject.name + " damage: " + damage + " poise: " + poiseDamage);
    }
}