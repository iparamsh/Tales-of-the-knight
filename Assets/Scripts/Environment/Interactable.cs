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
    public GameObject interactPromptUI;

    [Header("On Interact Event")]
    public UnityEvent onInteract;
    public bool isLocked = false;

    private bool playerInRange = false;
    private VisualElement hintElement;
    private string originalPromptText;
    private Label hintPrefix;
    private Label hintSuffix;

    void Start()
    {
        originalPromptText = promptText;

        var uiDoc = FindAnyObjectByType<UIDocument>();
        if (uiDoc != null)
        {
            hintElement = uiDoc.rootVisualElement.Q<VisualElement>("InteractionHint");
            hintPrefix = hintElement?.Q<Label>("HintPrefix");
            hintSuffix = hintElement?.Q<Label>("HintSuffix");
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
            onInteract?.Invoke();
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
            // Reset to original prompt text on exit
            promptText = originalPromptText;
        }
    }

    public void SetPromptText(string text)
    {
        promptText = text;
        if (playerInRange)
        {
            if (hintPrefix != null)
                hintPrefix.text = "";
            if (hintSuffix != null)
                hintSuffix.text = text;
        }
    }

    public void ShowPrompt()
    {
        if (hintElement != null)
        {
            hintElement.style.display = DisplayStyle.Flex;
            if (hintPrefix != null)
                hintPrefix.text = "";
            if (hintSuffix != null)
                hintSuffix.text = promptText;
        }
        else if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (hintElement != null)
            hintElement.style.display = DisplayStyle.None;
        else if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }
}