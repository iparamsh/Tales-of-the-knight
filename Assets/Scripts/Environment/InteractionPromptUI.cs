using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private VisualElement promptContainer;
    private Label promptTitle;
    private VisualElement buttonContainer;
    private List<Button> promptButtons = new List<Button>();

    void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("InteractionPromptUI: No UIDocument found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        promptContainer = root.Q<VisualElement>("InteractionPromptContainer");
        promptTitle = root.Q<Label>("PromptTitle");
        buttonContainer = root.Q<VisualElement>("ButtonContainer");

        if (promptContainer != null)
            promptContainer.style.display = DisplayStyle.None;
    }

    // Show a prompt with buttons and their callbacks
    public void ShowPrompt(string title, params (string label, System.Action callback)[] options)
    {
        if (promptContainer == null)
        {
            Debug.LogError("InteractionPromptUI: Prompt container not found in UI!");
            return;
        }

        // Clear previous buttons
        buttonContainer.Clear();
        promptButtons.Clear();

        // Set title
        if (promptTitle != null)
            promptTitle.text = title;

        // Create buttons for each option
        foreach (var (label, callback) in options)
        {
            var button = new Button(() => 
            {
                callback?.Invoke();
                HidePrompt();
            })
            {
                text = label
            };
            button.AddToClassList("prompt-button");
            buttonContainer.Add(button);
            promptButtons.Add(button);
        }

        // Show the prompt
        promptContainer.style.display = DisplayStyle.Flex;
        PauseStateManager.RequestPause(this);
    }

    public void HidePrompt()
    {
        if (promptContainer != null)
            promptContainer.style.display = DisplayStyle.None;
        
        PauseStateManager.ReleasePause(this);
    }
}
