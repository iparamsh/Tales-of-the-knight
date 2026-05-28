using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class DeathDialogueOverlay : MonoBehaviour
{
    public UIDocument uiDocument;
    public float displayDuration = 3f;
    public float fadeDuration = 0.4f;

    private VisualElement overlayContainer;
    private Label dialogueLabel;

    void Start()
    {
        if (uiDocument == null) uiDocument = FindFirstObjectByType<UIDocument>();
        var root = uiDocument.rootVisualElement;
        overlayContainer = root.Q<VisualElement>("DeathDialogueOverlay");
        dialogueLabel = root.Q<Label>("DeathDialogueLabel");

        if (overlayContainer != null)
        {
            overlayContainer.style.display = DisplayStyle.None;
            overlayContainer.style.opacity = 0f;
        }
    }

    public IEnumerator Show(string text)
    {
        if (overlayContainer == null) yield break;

        if (dialogueLabel != null)
            dialogueLabel.text = text;

        overlayContainer.style.display = DisplayStyle.Flex;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            overlayContainer.style.opacity = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            overlayContainer.style.opacity = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        overlayContainer.style.display = DisplayStyle.None;
    }
}