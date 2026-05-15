using UnityEngine;

public class BonfireController : MonoBehaviour
{
    private InteractionPromptUI promptUI;
    private PlayerStats playerStats;

    void Start()
    {
        promptUI = FindAnyObjectByType<InteractionPromptUI>();
        playerStats = FindAnyObjectByType<PlayerStats>();
    }

    // Called via Interactable onInteract event
    public void ShowBonfirePrompt()
    {
        if (promptUI == null)
            promptUI = FindAnyObjectByType<InteractionPromptUI>();

        promptUI.ShowPrompt("Bonfire",
            ("Rest", OnRest),
            ("Exit", OnExit)
        );
    }

    private void OnRest()
    {
        // Restore player health and FP
        if (playerStats != null)
        {
            playerStats.SetHealth(playerStats.MaxHealth);
            playerStats.SetFP(playerStats.MaxFP);
        }

        Debug.Log("Player rested at bonfire");
        // TODO: trigger rest animation/UI
    }

    private void OnExit()
    {
        Debug.Log("Player exited bonfire prompt");
        // Dialog closes, player continues
    }
}