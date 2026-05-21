using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    [Header("UI References")]
    public UIDocument uiDocument;
    public float hudFadeDuration = 0.3f;

    private VisualElement playerHUD;
    private VisualElement bossHUD;
    private VisualElement starContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (uiDocument == null) uiDocument = FindFirstObjectByType<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Adjust these names to match your actual UI element names
        playerHUD = root.Q<VisualElement>("PlayerHUD");
        bossHUD = root.Q<VisualElement>("BossHUD");
        starContainer = root.Q<VisualElement>("StarContainer");
    }

    // =============================================
    // Public methods — call from anywhere
    // =============================================

    public void HideHUD(float duration = -1f)
    {
        float d = duration < 0f ? hudFadeDuration : duration;
        StartCoroutine(FadeHUD(0f, d));
    }

    public void ShowHUD(float duration = -1f)
    {
        float d = duration < 0f ? hudFadeDuration : duration;
        StartCoroutine(FadeHUD(1f, d));
    }

    // =============================================
    // Internal
    // =============================================

    IEnumerator FadeHUD(float targetOpacity, float duration)
    {
        float startOpacity = playerHUD != null ? playerHUD.resolvedStyle.opacity : 1f;
        float elapsed = 0f;

        bool hiding = targetOpacity < 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (playerHUD != null)
                playerHUD.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, t);
            if (bossHUD != null)
                bossHUD.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, t);

            yield return null;
        }

        if (playerHUD != null) playerHUD.style.opacity = targetOpacity;
        if (bossHUD != null) bossHUD.style.opacity = targetOpacity;

        // Use display none/flex for star since opacity won't override inline style
        if (starContainer != null)
            starContainer.style.display = hiding ? DisplayStyle.None : DisplayStyle.Flex;
    }
}