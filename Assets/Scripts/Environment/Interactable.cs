using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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
    public bool isLocked = false;

    private bool playerInRange = false;
    private VisualElement hintElement;

    void Start()
    {
        // Try to find the hint element from the UI document
        var uiDoc = FindAnyObjectByType<UIDocument>();
        if (uiDoc != null)
        {
            hintElement = uiDoc.rootVisualElement.Q<VisualElement>("InteractionHint");
        }
    }

    void Update()
    {
        if (isLocked) 
        {
            HidePrompt();
            return;
        }
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
            if (!isLocked)
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
    // UI Methods
    // =============================================

    public void ShowPrompt()
    {
        if (hintElement != null)
        {
            hintElement.style.display = DisplayStyle.Flex;
        }
        else if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (hintElement != null)
        {
            hintElement.style.display = DisplayStyle.None;
        }
        else if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }
}