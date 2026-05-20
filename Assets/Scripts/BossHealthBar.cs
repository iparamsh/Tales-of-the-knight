using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class BossHealthBar : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;
    public BossController boss;

    [Header("Settings")]
    public float fadeInDuration = 0.8f;
    public float maxBarWidth = 600f;

    private VisualElement bossHUD;
    private VisualElement bossBarFill;
    private VisualElement bossBarBackground;
    private Label bossNameLabel;

    private float healthFillTarget;
    private float currentHealthFill;
    private float animTimer;
    private float animDuration = 0.3f;

    private bool isVisible = false;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;
        bossHUD = root.Q<VisualElement>("BossHUD");
        bossBarFill = root.Q<VisualElement>("BossBarFill");
        bossBarBackground = root.Q<VisualElement>("BossBarBackground");
        bossNameLabel = root.Q<Label>("BossName");

        // Start hidden
        if (bossHUD != null)
            bossHUD.style.opacity = 0f;

        // Set background to full width
        if (bossBarBackground != null)
            bossBarBackground.style.width = maxBarWidth;

        // Set boss name
        if (bossNameLabel != null && boss != null)
            bossNameLabel.text = boss.GetBossName();

        // Initialize fill
        if (boss != null)
        {
            currentHealthFill = 1f;
            healthFillTarget = 1f;
            SetFill(currentHealthFill);
        }

        // Subscribe to boss health changes
        if (boss != null)
            boss.OnHealthChanged += UpdateHealth;
    }

    void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged -= UpdateHealth;
    }

    void UpdateHealth(float current, float max)
    {
        healthFillTarget = max > 0f ? current / max : 0f;
        animTimer = 0f;
    }

    void SetFill(float fill)
    {
        if (bossBarFill != null)
            bossBarFill.style.width = (maxBarWidth - 48) * fill;
    }

    public void ShowHealthBar()
    {
        if (isVisible) return;
        isVisible = true;
        StartCoroutine(FadeIn());
    }

    public void FadeOut()
    {
        StartCoroutine(FadeOutSequence());
    }

    IEnumerator FadeOutSequence()
    {
        float elapsed = 0f;
        float duration = 2.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (bossHUD != null)
                bossHUD.style.opacity = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        if (bossHUD != null)
            bossHUD.style.opacity = 0f;
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (bossHUD != null)
                bossHUD.style.opacity = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        if (bossHUD != null)
            bossHUD.style.opacity = 1f;
    }

    void Update()
    {
        if (!isVisible) return;

        if (animTimer < animDuration)
        {
            animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animTimer / animDuration);
            currentHealthFill = Mathf.Lerp(currentHealthFill, healthFillTarget, t);
            SetFill(currentHealthFill);
        }
    }

    public void FadeOutAndChangeName(string newName, float duration = 1f)
    {
        StartCoroutine(NameChangeSequence(newName, duration));
    }

    IEnumerator NameChangeSequence(string newName, float totalDuration)
    {
        float fadeDuration = 0.15f; // very snappy fade

        // Quick fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (bossHUD != null)
                bossHUD.style.opacity = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        // Change name while invisible
        if (bossNameLabel != null)
            bossNameLabel.text = newName;

        if (boss != null)
            SetFill(boss.GetHealth() / boss.GetMaxHealth());

        // Stay invisible for remaining middle duration
        yield return new WaitForSeconds(totalDuration - fadeDuration * 2f);

        // Quick fade back in
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (bossHUD != null)
                bossHUD.style.opacity = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        if (bossHUD != null)
            bossHUD.style.opacity = 1f;
    }
}