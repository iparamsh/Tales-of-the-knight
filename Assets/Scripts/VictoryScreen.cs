using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class VictoryScreen : MonoBehaviour
{
    public UIDocument uiDocument;
    public float fadeDuration = 0.8f;
    public float lingerDuration = 3f;

    private VisualElement victoryOverlay;
    private bool isShowing = false;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;
        victoryOverlay = root.Q<VisualElement>("VictoryOverlay");

        if (victoryOverlay != null)
        {
            victoryOverlay.style.opacity = 0f;
            victoryOverlay.style.display = DisplayStyle.None;
        }
    }

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;
        StartCoroutine(ShowSequence());
    }

    IEnumerator ShowSequence()
    {
        // Make visible before fading in
        if (victoryOverlay != null)
            victoryOverlay.style.display = DisplayStyle.Flex;

        // Fade in overlay
        yield return StartCoroutine(FadeOverlay(0f, 1f));

        // Linger
        yield return new WaitForSeconds(lingerDuration);

        // Fade out overlay
        yield return StartCoroutine(FadeOverlay(1f, 0f));
    }

    IEnumerator FadeOverlay(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (victoryOverlay != null)
                victoryOverlay.style.opacity = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        if (victoryOverlay != null)
            victoryOverlay.style.opacity = to;
    }
}