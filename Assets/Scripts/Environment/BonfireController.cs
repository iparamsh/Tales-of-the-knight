using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BonfireController : MonoBehaviour
{
    private InteractionPromptUI promptUI;
    private PlayerStats playerStats;
    private PlayerController playerController;
    private PauseMenuUI pauseMenuUI;
    private StaminaSystem staminaSystem;

    [Header("Rest Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.8f;

    void Start()
    {
        promptUI = FindAnyObjectByType<InteractionPromptUI>();
        playerStats = FindAnyObjectByType<PlayerStats>();
        playerController = FindAnyObjectByType<PlayerController>();
        pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();
        staminaSystem = FindAnyObjectByType<StaminaSystem>();
    }

    public void ShowBonfirePrompt()
    {
        if (promptUI == null)
            promptUI = FindAnyObjectByType<InteractionPromptUI>();

        if (pauseMenuUI == null)
            pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();

        promptUI.ShowBonfireMenu(OnRest, OpenMoreMenu, OnExit);
    }

    private void OnRest()
    {
        RespawnManager.SetBonfirePosition(transform.position);
        StartCoroutine(RestSequence());
    }

    IEnumerator RestSequence()
    {
        // Fade to black
        if (fadePanel != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            fadePanel.alpha = 1f;
        }

        // Replenish all stats
        if (playerStats != null)
        {
            playerStats.SetHealth(playerStats.MaxHealth);
            playerStats.SetFP(playerStats.MaxFP);
        }

        if (staminaSystem != null)
            staminaSystem.RefillStamina();

        // Fade back in
        if (fadePanel != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            fadePanel.alpha = 0f;
        }
    }

    private void OnExit()
    {
        // Dialog closes, player continues
    }

    private void OpenMoreMenu()
    {
        if (pauseMenuUI == null)
            pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();

        if (pauseMenuUI != null)
            pauseMenuUI.ShowMenu();
    }
}