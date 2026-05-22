using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private VisualElement promptContainer;
    private VisualElement bonfireMenuContainer;
    private Label promptTitle;
    private VisualElement buttonContainer;
    private Button bonfireRestButton;
    private Button bonfireMoreButton;
    private Button bonfireExitButton;
    private List<Button> promptButtons = new List<Button>();
    private Action bonfireRestAction;
    private Action bonfireMoreAction;
    private Action bonfireExitAction;
    public bool IsBonfireMenuOpen { get; private set; }
    public bool ConsumeEscapeThisFrame { get; private set; }

    void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("InteractionPromptUI: No UIDocument found!");
            return;
        }

        BindUiReferences();

        if (promptContainer != null)
            promptContainer.style.display = DisplayStyle.None;

        if (bonfireMenuContainer != null)
            bonfireMenuContainer.style.display = DisplayStyle.None;
    }

    void OnDestroy()
    {
        if (bonfireRestButton != null) bonfireRestButton.clicked -= OnBonfireRestClicked;
        if (bonfireMoreButton != null) bonfireMoreButton.clicked -= OnBonfireMoreClicked;
        if (bonfireExitButton != null) bonfireExitButton.clicked -= OnBonfireExitClicked;
    }

    void Update()
    {
        if (!IsBonfireMenuOpen || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        ConsumeEscapeThisFrame = true;
        HideBonfireMenu();
    }

    void LateUpdate()
    {
        ConsumeEscapeThisFrame = false;
    }

    // Show a prompt with buttons and their callbacks
    public void ShowPrompt(string title, params (string label, System.Action callback)[] options)
    {
        BindUiReferences();

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
        HideBonfireMenuInternal(false);
        promptContainer.style.display = DisplayStyle.Flex;
        PauseStateManager.RequestPause(this);
    }

    public void ShowBonfireMenu(Action onRest, Action onMore, Action onExit)
    {
        BindUiReferences();

        if (bonfireMenuContainer == null)
        {
            Debug.LogError("InteractionPromptUI: Bonfire menu container not found in UI!");
            return;
        }

        bonfireRestAction = onRest;
        bonfireMoreAction = onMore;
        bonfireExitAction = onExit;
        ConsumeEscapeThisFrame = false;

        if (promptContainer != null)
            promptContainer.style.display = DisplayStyle.None;

        IsBonfireMenuOpen = true;
        bonfireMenuContainer.style.display = DisplayStyle.Flex;
        PauseStateManager.RequestPause(this);

        if (bonfireRestButton != null)
        {
            bonfireRestButton.SetEnabled(true);
            bonfireRestButton.Focus();
        }
    }

    public void HidePrompt()
    {
        if (promptContainer != null)
            promptContainer.style.display = DisplayStyle.None;
        
        PauseStateManager.ReleasePause(this);
    }

    public void HideBonfireMenu()
    {
        HideBonfireMenuInternal(true);
    }

    private void OnBonfireRestClicked()
    {
        bonfireRestAction?.Invoke();
        HideBonfireMenu();
    }

    private void OnBonfireMoreClicked()
    {
        bonfireMoreAction?.Invoke();
        HideBonfireMenu();
    }

    private void OnBonfireExitClicked()
    {
        bonfireExitAction?.Invoke();
        HideBonfireMenu();
    }

    private void HideBonfireMenuInternal(bool releasePause)
    {
        if (bonfireMenuContainer != null)
            bonfireMenuContainer.style.display = DisplayStyle.None;

        IsBonfireMenuOpen = false;
        bonfireRestAction = null;
        bonfireMoreAction = null;
        bonfireExitAction = null;

        if (releasePause)
            PauseStateManager.ReleasePause(this);
    }

    private void BindUiReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            return;

        var root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        promptContainer ??= root.Q<VisualElement>("InteractionPromptContainer");
        bonfireMenuContainer ??= root.Q<VisualElement>("BonfireMenuContainer");
        promptTitle ??= root.Q<Label>("PromptTitle");
        buttonContainer ??= root.Q<VisualElement>("ButtonContainer");

        var nextRestButton = root.Q<Button>("BonfireRestButton");
        if (bonfireRestButton == null && nextRestButton != null)
        {
            bonfireRestButton = nextRestButton;
            bonfireRestButton.clicked += OnBonfireRestClicked;
        }

        var nextMoreButton = root.Q<Button>("BonfireMoreButton");
        if (bonfireMoreButton == null && nextMoreButton != null)
        {
            bonfireMoreButton = nextMoreButton;
            bonfireMoreButton.clicked += OnBonfireMoreClicked;
        }

        var nextExitButton = root.Q<Button>("BonfireExitButton");
        if (bonfireExitButton == null && nextExitButton != null)
        {
            bonfireExitButton = nextExitButton;
            bonfireExitButton.clicked += OnBonfireExitClicked;
        }
    }
}
