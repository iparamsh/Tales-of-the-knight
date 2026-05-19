using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;
    private InteractionPromptUI promptUI;
    private bool isLocked = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        promptUI = FindAnyObjectByType<InteractionPromptUI>();
    }

    // Hook this into the Interactable onInteract event via inspector
    public void ShowDoorPrompt()
    {
        if (promptUI == null)
            promptUI = FindAnyObjectByType<InteractionPromptUI>();

        if (isOpen)
        {
            Debug.Log("Door already open");
            return;
        }

        promptUI.ShowPrompt("Door",
            ("Open", OpenDoor),
            ("Cancel", OnCancel)
        );
    }

    public void OpenDoor()
    {
        if (isOpen || isLocked) return;

        isOpen = true;
        spriteRenderer.sprite = openSprite;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = false;
    }

    private void OnCancel()
    {
        Debug.Log("Player chose not to open door");
        // Prompt closes, player continues
    }

    // Will be called after door opens - room transition goes here
    // RoomTransition system to be added later
    public void OnDoorOpened()
    {
        Debug.Log("Door opened - room transition placeholder");
    }

    public void LockDoor()
    {
        // Swap to closed sprite
        spriteRenderer.sprite = closedSprite;

        // Re-enable the blocking collider
        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = true;

        // Disable interaction so player cant open from inside
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.HidePrompt();
            interactable.enabled = false;
        }

        if (interactable != null && interactable.interactPromptUI != null)
        interactable.interactPromptUI.SetActive(false);

        isOpen = false;
        isLocked = true;
    }
}