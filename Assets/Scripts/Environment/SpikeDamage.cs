using UnityEngine;

public class SpikeDamage : MonoBehaviour
{
    public float damagePerSecond = 1000f;

    private Collider2D playerInSpikes;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerHurtbox")) return;
        playerInSpikes = other;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerHurtbox")) return;
        playerInSpikes = null;
    }

    void Update()
    {
        if (playerInSpikes == null) return;

        PlayerController pc = playerInSpikes.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        pc.SetHealth(pc.GetHealth() - damagePerSecond * Time.deltaTime);
    }
}