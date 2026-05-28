using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("Settings")]
    public bool requiresCloneDefeated = false;
    public string lockedMessage = "Something blocks your path...";

    [Header("Reward")]
    public bool givesFpFlask = false;
    public int fpFlaskAmount = 2;

    private SpriteRenderer spriteRenderer;
    private Interactable interactable;
    private bool isOpen = false;
    private bool cloneDefeated = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        interactable = GetComponent<Interactable>();

        if (closedSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = closedSprite;
    }

    public void NotifyCloneDefeated()
    {
        cloneDefeated = true;
    }

    public void OnInteract()
    {
        if (isOpen) return;

        if (requiresCloneDefeated && !cloneDefeated)
        {
            InteractionPromptUI promptUI = FindFirstObjectByType<InteractionPromptUI>();
            promptUI?.ShowPrompt(lockedMessage, ("Ok", () => { }));
            return;
        }

        OpenChest();
    }

    void OpenChest()
    {
        isOpen = true;

        if (openSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = openSprite;

        if (givesFpFlask)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            pc?.GiveFpFlasks(fpFlaskAmount);
            KeyPickupOverlay.Instance?.Show(fpFlaskAmount + "x Ether Shard (restores FP)", KeyPickupOverlay.Instance.etherShardSprite);
        }
        else
        {
            PlayerInventory.Instance?.AddDungeonKey();
            KeyPickupOverlay.Instance?.Show("1x Dungeon Master's Key", KeyPickupOverlay.Instance.keySprite);
        }

        if (interactable != null)
        {
            interactable.HidePrompt();
            interactable.enabled = false;
        }
    }
}