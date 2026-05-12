using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Hook this into the Interactable onInteract event via inspector
    public void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;
        spriteRenderer.sprite = openSprite;

        // Disable the blocking collider so player can walk through
        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = false;
    }

    // Will be called after door opens - room transition goes here
    // RoomTransition system to be added later
    public void OnDoorOpened()
    {
        Debug.Log("Door opened - room transition placeholder");
    }
}