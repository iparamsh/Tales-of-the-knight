using UnityEngine;
using System.Collections;


public class BossHitbox : MonoBehaviour
{
    public float damage = 15f;
    public float poiseDamage = 10f;
    public bool isMultiHit = false;
    public float multiHitCooldown = 0.1f;

    private bool hasHit = false;
    private float multiHitTimer = 0f;
    private BoxCollider2D boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void OnEnable()
    {
        hasHit = false;
        multiHitTimer = 0f;
        StartCoroutine(InitialOverlapCheck());
    }

    IEnumerator InitialOverlapCheck()
    {
        yield return new WaitForFixedUpdate();
        CheckOverlap();
    }

    void Update()
    {
        if (isMultiHit && multiHitTimer > 0f)
            multiHitTimer -= Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void CheckOverlap()
    {
        if (boxCollider == null) return;

        Vector2 center = (Vector2)transform.position + boxCollider.offset;
        Collider2D hit = Physics2D.OverlapBox(
            center,
            boxCollider.size,
            0f,
            LayerMask.GetMask("Hurtbox")
        );

        if (hit != null)
            TryHit(hit);
    }

    void TryHit(Collider2D other)
    {
        if (!isMultiHit && hasHit) return;
        if (isMultiHit && multiHitTimer > 0f) return;
        if (!other.CompareTag("PlayerHurtbox")) return;

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        if (pc.IsInvincible) return;

        if (isMultiHit)
            multiHitTimer = multiHitCooldown;
        else
            hasHit = true;

        pc.SetHealth(pc.GetHealth() - damage);
        pc.GetComponent<StaminaSystem>()?.InterruptRegen();
    }
}