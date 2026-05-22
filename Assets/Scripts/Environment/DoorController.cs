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
        spriteRenderer.sprite = closedSprite;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = true;

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isLocked = true;
        }

        RoomDoor roomDoor = GetComponent<RoomDoor>();
        if (roomDoor != null)
            roomDoor.Lock();

        isOpen = false;
        isLocked = true;
    }

    public void UnlockDoor()
    {
        isLocked = false;
        isOpen = true;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = false;

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isLocked = false;
            interactable.enabled = false;
            interactable.HidePrompt();
        }

        spriteRenderer.sprite = openSprite;

        RoomDoor roomDoor = GetComponent<RoomDoor>();
        if (roomDoor != null)
            roomDoor.Unlock();
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}