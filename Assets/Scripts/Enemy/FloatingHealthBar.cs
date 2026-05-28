using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("References")]
    public Image backgroundBar;
    public Image fillBar;

    [Header("Settings")]
    public float maxWidth = 80f;
    public float fadeDelay = 3f;
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private float fadeTimer = 0f;
    private bool isFading = false;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        if (backgroundBar != null)
        {
            backgroundBar.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            backgroundBar.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            backgroundBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            backgroundBar.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (fillBar != null)
        {
            fillBar.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fillBar.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fillBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            fillBar.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void UpdateHealth(float current, float max)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        float fillAmount = Mathf.Clamp01(current / max);
        if (fillBar != null)
            fillBar.rectTransform.sizeDelta = new Vector2(maxWidth * fillAmount, 30f);
        if (backgroundBar != null)
            backgroundBar.rectTransform.sizeDelta = new Vector2(maxWidth, 30f);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAfterDelay());
    }

    IEnumerator FadeAfterDelay()
    {
        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}