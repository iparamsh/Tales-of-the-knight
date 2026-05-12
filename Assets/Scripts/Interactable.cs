using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public string promptText = "Press E to interact";

    [Header("UI hooks - assign in inspector")]
    // Your UI partner drags their prompt GameObject here
    public GameObject interactPromptUI;

    [Header("On Interact Event")]
    // Your UI partner or whoever can hook functions into this via inspector
    public UnityEvent onInteract;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            onInteract?.Invoke();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            ShowPrompt();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HidePrompt();
        }
    }

    // =============================================
    // UI Methods - Adhitya starts work in here
    // =============================================

    public void ShowPrompt()
    {
        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);

        // Placeholder until UI partner implements proper prompt
        Debug.Log(promptText);
    }

    public void HidePrompt()
    {
        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    // =============================================
    // UI Methods - Adhitya starts work in here
    // =============================================
}