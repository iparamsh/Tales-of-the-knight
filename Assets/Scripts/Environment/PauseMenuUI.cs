using UnityEngine;
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

    void Start()
    {
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
        PauseStateManager.ClearAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        PauseStateManager.ClearAll();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
