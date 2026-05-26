using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public float damage = 20f;
    public float poiseDamage = 15f;

    private BoxCollider2D boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void OnEnable()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
    }

    public bool CheckHitBoss()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) return false;

        Vector2 center = (Vector2)transform.TransformPoint(boxCollider.offset);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxCollider.size, 0f, LayerMask.GetMask("Hurtbox"));

        Debug.Log("CheckHitBoss — hits found: " + hits.Length + " center: " + center + " size: " + boxCollider.size);
        foreach (Collider2D hit in hits)
            Debug.Log("  hit: " + hit.gameObject.name + " tag: " + hit.tag);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("BossHurtbox")) continue;

            BossController boss = hit.GetComponentInParent<BossController>();
            if (boss == null) continue;

            boss.TakeDamage(damage, poiseDamage);
            return true;
        }

        return false;
    }
}