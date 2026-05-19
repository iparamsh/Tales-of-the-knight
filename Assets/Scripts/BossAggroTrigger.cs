using UnityEngine;

public class BossAggroTrigger : MonoBehaviour
{
    [Header("References")]
    public BossController boss;
    //public UIBossBar bossHealthBar; 

    private bool hasTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartBossFight();
            gameObject.SetActive(false);
        }
    }

    void StartBossFight()
    {
        // Wake boss up via teleport reappear entrance
        if (boss != null)
            boss.BossEntrance();

        // Show boss health bar
        // boss health bar show call goes here once implemented

        Debug.Log("Boss fight started");
    }
}