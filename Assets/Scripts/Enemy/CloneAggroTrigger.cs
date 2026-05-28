using UnityEngine;

public class CloneAggroTrigger : MonoBehaviour
{
    public ShadowCloneEnemy clone;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("AggroTrigger hit by: " + other.gameObject.name + " tag: " + other.tag);
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        clone?.Aggro();
    }
}