using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("Key Requirement")]
    public bool requiresKey = false;
    public bool unlockFromLeftSide = true;

    public bool isBossDoor = false;
    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;
    private bool isLocked = false;
    private InteractionPromptUI promptUI;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        promptUI = FindAnyObjectByType<InteractionPromptUI>();
    }

    public void ShowDoorPrompt()
    {
        if (promptUI == null)
            promptUI = FindAnyObjectByType<InteractionPromptUI>();

        if (isOpen) return;

        if (requiresKey)
        {
            bool correctSide = IsPlayerOnCorrectSide();
            bool hasKey = PlayerInventory.Instance != null && PlayerInventory.Instance.HasDungeonKey;

            if (!correctSide)
            {
                promptUI.ShowPrompt("This door is locked",
                    ("Cancel", OnCancel)
                );
                return;
            }

            if (!hasKey)
            {
                promptUI.ShowPrompt("Requires Dungeon Master's Key",
                    ("Cancel", OnCancel)
                );
                return;
            }

            promptUI.ShowPrompt("Unlock Door",
                ("Unlock", UnlockWithKey),
                ("Cancel", OnCancel)
            );
            return;
        }

        promptUI.ShowPrompt("Door",
            ("Open", OpenDoor),
            ("Cancel", OnCancel)
        );
    }

    bool IsPlayerOnCorrectSide()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return false;

        if (unlockFromLeftSide)
            return player.transform.position.x < transform.position.x;
        else
            return player.transform.position.x > transform.position.x;
    }

    void UnlockWithKey()
    {
        PlayerInventory.Instance?.UseDungeonKey();
        OpenDoor();
    }

    public void OpenDoor()
    {
        if (isOpen || isLocked) return;

        isOpen = true;
        spriteRenderer.sprite = openSprite;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = false;

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.enabled = false;
    }

    private void OnCancel() { }

    public void LockDoor()
    {
        spriteRenderer.sprite = closedSprite;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = true;

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.isLocked = true;

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

    public void ResetDoor()
    {
        isLocked = false;
        isOpen = false;

        spriteRenderer.sprite = closedSprite;

        BoxCollider2D blockingCollider = GetComponent<BoxCollider2D>();
        if (blockingCollider != null)
            blockingCollider.enabled = true;

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isLocked = false;
            interactable.enabled = true;
            interactable.HidePrompt();
        }

        RoomDoor roomDoor = GetComponent<RoomDoor>();
        if (roomDoor != null)
        {
            roomDoor.Unlock();
            roomDoor.ResetToRoomA();
        }
    }

    public bool IsOpen() { return isOpen; }
}