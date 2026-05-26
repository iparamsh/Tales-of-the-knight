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

    public void Show(string message = "Shadow Warrior Defeated")
    {
        if (isShowing) return;
        isShowing = true;
        StartCoroutine(ShowSequence(message));
    }

    IEnumerator ShowSequence(string message)
    {
        // Set message text
        Label victoryText = victoryOverlay?.Q<Label>("VictoryText");
        if (victoryText != null)
            victoryText.text = message;

        yield return StartCoroutine(FadeOverlay(0f, 1f));
        yield return new WaitForSeconds(lingerDuration);
        yield return StartCoroutine(FadeOverlay(1f, 0f));

        isShowing = false;
    }

    IEnumerator FadeOverlay(float from, float to)
    {
        if (victoryOverlay == null) yield break;

        victoryOverlay.style.display = DisplayStyle.Flex;
        victoryOverlay.style.opacity = from;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            victoryOverlay.style.opacity = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        victoryOverlay.style.opacity = to;

        if (to <= 0f)
            victoryOverlay.style.display = DisplayStyle.None;
    }

    public System.Collections.IEnumerator ShowAndWait(string message = "Shadow Warrior Defeated")
    {
        if (isShowing) yield break;
        isShowing = true;
        yield return StartCoroutine(ShowSequence(message));
    }
}