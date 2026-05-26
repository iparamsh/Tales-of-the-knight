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
    private VisualElement vignetteOverlay;

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
        vignetteOverlay = root.Q<VisualElement>("VignetteOverlay");
        if (vignetteOverlay != null)
            vignetteOverlay.style.display = DisplayStyle.None;
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
    }

    public void PulseVignette(int pulseCount = 2, float pulseDuration = 0.3f)
    {
        StartCoroutine(VignettePulse(pulseCount, pulseDuration));
    }

    IEnumerator VignettePulse(int pulseCount, float pulseDuration)
    {
        if (vignetteOverlay == null) yield break;

        vignetteOverlay.style.display = DisplayStyle.Flex;
        vignetteOverlay.style.opacity = 0f;

        for (int i = 0; i < pulseCount; i++)
        {
            float elapsed = 0f;
            float halfDuration = pulseDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                vignetteOverlay.style.opacity = Mathf.Lerp(0f, 0.4f, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                vignetteOverlay.style.opacity = Mathf.Lerp(0.4f, 0f, elapsed / halfDuration);
                yield return null;
            }
        }

        vignetteOverlay.style.display = DisplayStyle.None;
        vignetteOverlay.style.opacity = 0f;
    }
}