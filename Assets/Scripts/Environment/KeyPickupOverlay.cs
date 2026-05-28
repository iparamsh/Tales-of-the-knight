using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class KeyPickupOverlay : MonoBehaviour
{
    public static KeyPickupOverlay Instance { get; private set; }

    public UIDocument uiDocument;
    public float displayDuration = 2.5f;
    public float fadeDuration = 0.3f;
    public Sprite keySprite;
    public Sprite etherShardSprite;

    private VisualElement overlayContainer;
    private Label keyLabel;

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
        overlayContainer = root.Q<VisualElement>("KeyPickupOverlay");
        keyLabel = root.Q<Label>("KeyPickupLabel");

        if (overlayContainer != null)
        {
            overlayContainer.style.display = DisplayStyle.None;
            overlayContainer.style.opacity = 0f;
        }
    }

    public void Show(string message, Sprite icon = null)
    {
        StartCoroutine(ShowSequence(message, icon));
    }

    IEnumerator ShowSequence(string message, Sprite icon)
    {
        if (overlayContainer == null) yield break;

        if (keyLabel != null)
            keyLabel.text = message;

        // Swap icon if provided
        VisualElement iconElement = overlayContainer.Q<VisualElement>("KeyIcon");
        if (iconElement != null && icon != null)
            iconElement.style.backgroundImage = new StyleBackground(icon);

        overlayContainer.style.display = DisplayStyle.Flex;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            overlayContainer.style.opacity = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        overlayContainer.style.opacity = 1f;

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