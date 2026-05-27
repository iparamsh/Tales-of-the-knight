using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement pauseContainer;
    private VisualElement mainMenuPage;
    private VisualElement controlsPage;

    private Button continueButton;
    private Button newGameButton;
    private Button controlsButton;
    private Button quitButton;
    private Button backButton;
    private InteractionPromptUI interactionPromptUI;

    private bool isOpen;
    private bool controlsVisible;
    private PlayerCombat playerCombat;

    void Start()
    {
        if (MainMenuUI.IsMainMenuSceneActive)
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var mainMenuRoot = uiDocument.rootVisualElement;
                var inactivePauseContainer = mainMenuRoot.Q<VisualElement>("PauseMenuContainer");
                if (inactivePauseContainer != null)
                    inactivePauseContainer.style.display = DisplayStyle.None;
            }

            enabled = false;
            return;
        }

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("PauseMenuUI: No UIDocument found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        pauseContainer = root.Q<VisualElement>("PauseMenuContainer");
        mainMenuPage = root.Q<VisualElement>("PauseMainPage");
        controlsPage = root.Q<VisualElement>("PauseControlsPage");
        interactionPromptUI = FindAnyObjectByType<InteractionPromptUI>();
        playerCombat = FindAnyObjectByType<PlayerCombat>();

        continueButton = root.Q<Button>("ContinueButton");
        newGameButton = root.Q<Button>("NewGameButton");
        controlsButton = root.Q<Button>("ControlsButton");
        quitButton = root.Q<Button>("QuitButton");
        backButton = root.Q<Button>("BackButton");

        if (continueButton != null) continueButton.clicked += ResumeGame;
        if (newGameButton != null) newGameButton.clicked += RestartGame;
        if (controlsButton != null) controlsButton.clicked += ShowControlsPage;
        if (quitButton != null) quitButton.clicked += QuitGame;
        if (backButton != null) backButton.clicked += ShowMainMenu;

        HideMenu();
    }

    void OnDestroy()
    {
        if (continueButton != null) continueButton.clicked -= ResumeGame;
        if (newGameButton != null) newGameButton.clicked -= RestartGame;
        if (controlsButton != null) controlsButton.clicked -= ShowControlsPage;
        if (quitButton != null) quitButton.clicked -= QuitGame;
        if (backButton != null) backButton.clicked -= ShowMainMenu;

        if (isOpen)
            PauseStateManager.ReleasePause(this);
    }

    void Update()
    {
        if (MainMenuUI.IsMainMenuSceneActive)
            return;

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (interactionPromptUI == null)
            interactionPromptUI = FindAnyObjectByType<InteractionPromptUI>();

        if (interactionPromptUI != null && (interactionPromptUI.IsBonfireMenuOpen || interactionPromptUI.ConsumeEscapeThisFrame))
            return;

        if (!isOpen)
        {
            ShowMenu();
            return;
        }

        if (controlsVisible)
        {
            ShowMainMenu();
            return;
        }

        HideMenu();
    }

    public void ShowMenu()
    {
        if (isOpen)
        {
            ShowMainMenu();
            return;
        }

        isOpen = true;
        controlsVisible = false;
        PauseStateManager.RequestPause(this);
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
            playerCombat.LightAttackAction.Disable();
            playerCombat.HeavyAttackAction.Disable();
            playerCombat.PlungeAction.Disable();
        }

        Interactable[] interactables = FindObjectsByType<Interactable>();
        foreach (Interactable i in interactables)
        {
            i.HidePrompt();
            i.enabled = false;
        }

        if (pauseContainer != null)
            pauseContainer.style.display = DisplayStyle.Flex;

        ShowMainMenu();

        if (continueButton != null)
            continueButton.Focus();
    }

    public void HideMenu()
    {
        if (!isOpen)
        {
            if (pauseContainer != null)
                pauseContainer.style.display = DisplayStyle.None;

            if (mainMenuPage != null)
                mainMenuPage.style.display = DisplayStyle.None;

            if (controlsPage != null)
                controlsPage.style.display = DisplayStyle.None;
            return;
        }

        isOpen = false;
        controlsVisible = false;

        if (pauseContainer != null)
            pauseContainer.style.display = DisplayStyle.None;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.None;

        if (controlsPage != null)
            controlsPage.style.display = DisplayStyle.None;

        PauseStateManager.ReleasePause(this);
        if (playerCombat != null)
        StartCoroutine(ReenableCombatDelayed());

        Interactable[] interactables = FindObjectsByType<Interactable>();
        foreach (Interactable i in interactables)
            i.enabled = true;
    }

    private void ShowMainMenu()
    {
        if (!isOpen)
            ShowMenu();

        controlsVisible = false;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.Flex;

        if (controlsPage != null)
            controlsPage.style.display = DisplayStyle.None;

        if (continueButton != null)
            continueButton.Focus();
    }

    private void ShowControlsPage()
    {
        if (!isOpen)
            ShowMenu();

        controlsVisible = true;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.None;

        if (controlsPage != null)
            controlsPage.style.display = DisplayStyle.Flex;

        if (backButton != null)
            backButton.Focus();
    }

    private void ResumeGame()
    {
        HideMenu();
    }

    private void RestartGame()
    {
        RespawnManager.Reset();
        PauseStateManager.ClearAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator ReenableCombatDelayed()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        if (playerCombat != null)
        {
            // Disable and re-enable to flush buffered inputs
            playerCombat.LightAttackAction.Disable();
            playerCombat.HeavyAttackAction.Disable();
            playerCombat.PlungeAction.Disable();

            yield return new WaitForSecondsRealtime(0.05f);

            playerCombat.LightAttackAction.Enable();
            playerCombat.HeavyAttackAction.Enable();
            playerCombat.PlungeAction.Enable();
            playerCombat.enabled = true;
            playerCombat.BlockInputBriefly(0.3f);
        }
    }

    private void QuitGame()
    {
        RespawnManager.Reset();
        PauseStateManager.ClearAll();
        SceneManager.LoadScene(MainMenuUI.MainMenuSceneName);
    }
}
