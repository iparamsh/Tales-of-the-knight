using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    public const string MainMenuSceneName = "UI Scene";
    public const string GameplaySceneName = "Playtest Scene";

    [SerializeField] private UIDocument uiDocument;

    private VisualElement mainMenuContainer;
    private VisualElement mainMenuPage;
    private VisualElement mainMenuControlsPage;

    private Button continueButton;
    private Button newGameButton;
    private Button controlsButton;
    private Button backButton;

    private bool controlsVisible;

    public static bool IsMainMenuSceneActive => SceneManager.GetActiveScene().name == MainMenuSceneName;

    void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("MainMenuUI: No UIDocument found!");
            return;
        }

        BindUiReferences();

        if (!IsMainMenuSceneActive)
        {
            HideAllMenuLayers();
            enabled = false;
            return;
        }

        if (continueButton != null) continueButton.clicked += ContinueGame;
        if (newGameButton != null) newGameButton.clicked += StartNewGame;
        if (controlsButton != null) controlsButton.clicked += ShowControls;
        if (backButton != null) backButton.clicked += ShowTitleScreen;

        HideGameplayLayers();
        DisableLegacyOverlays();
        PauseStateManager.RequestPause(this);
        ShowTitleScreen();
    }

    void OnDestroy()
    {
        if (continueButton != null) continueButton.clicked -= ContinueGame;
        if (newGameButton != null) newGameButton.clicked -= StartNewGame;
        if (controlsButton != null) controlsButton.clicked -= ShowControls;
        if (backButton != null) backButton.clicked -= ShowTitleScreen;

        PauseStateManager.ReleasePause(this);
    }

    void Update()
    {
        if (!IsMainMenuSceneActive || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (controlsVisible)
            ShowTitleScreen();
    }

    private void BindUiReferences()
    {
        var root = uiDocument.rootVisualElement;

        mainMenuContainer = root.Q<VisualElement>("MainMenuContainer");
        mainMenuPage = root.Q<VisualElement>("MainMenuPage");
        mainMenuControlsPage = root.Q<VisualElement>("MainMenuControlsPage");

        continueButton = root.Q<Button>("MainMenuContinueButton");
        newGameButton = root.Q<Button>("MainMenuNewGameButton");
        controlsButton = root.Q<Button>("MainMenuControlsButton");
        backButton = root.Q<Button>("MainMenuBackButton");
    }

    private void HideGameplayLayers()
    {
        HideElement("InteractionPromptContainer");
        HideElement("InteractionHint");
        HideElement("PlayerHUD");
        HideElement("BossHUD");
        HideElement("VictoryOverlay");
        HideElement("Star");
        HideElement("BonfireMenuContainer");
        HideElement("PauseMenuContainer");
        HideElement("VignetteOverlay");
    }

    private void DisableLegacyOverlays()
    {
        HUDOverlay[] overlays = FindObjectsByType<HUDOverlay>();
        foreach (HUDOverlay overlay in overlays)
            overlay.enabled = false;
    }

    private void HideAllMenuLayers()
    {
        if (mainMenuContainer != null)
            mainMenuContainer.style.display = DisplayStyle.None;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.None;

        if (mainMenuControlsPage != null)
            mainMenuControlsPage.style.display = DisplayStyle.None;
    }

    private void HideElement(string elementName)
    {
        var element = uiDocument.rootVisualElement.Q<VisualElement>(elementName);
        if (element != null)
            element.style.display = DisplayStyle.None;
    }

    private void ShowTitleScreen()
    {
        controlsVisible = false;

        if (mainMenuContainer != null)
            mainMenuContainer.style.display = DisplayStyle.Flex;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.Flex;

        if (mainMenuControlsPage != null)
            mainMenuControlsPage.style.display = DisplayStyle.None;

        if (continueButton != null)
            continueButton.Focus();
        else if (newGameButton != null)
            newGameButton.Focus();
    }

    private void ShowControls()
    {
        controlsVisible = true;

        if (mainMenuContainer != null)
            mainMenuContainer.style.display = DisplayStyle.Flex;

        if (mainMenuPage != null)
            mainMenuPage.style.display = DisplayStyle.None;

        if (mainMenuControlsPage != null)
            mainMenuControlsPage.style.display = DisplayStyle.Flex;

        if (backButton != null)
            backButton.Focus();
    }

    private void ContinueGame()
    {
        PauseStateManager.ClearAll();
        SceneManager.LoadScene(GameplaySceneName);
    }

    private void StartNewGame()
    {
        RespawnManager.Reset();
        PauseStateManager.ClearAll();
        SceneManager.LoadScene(GameplaySceneName);
    }
}
