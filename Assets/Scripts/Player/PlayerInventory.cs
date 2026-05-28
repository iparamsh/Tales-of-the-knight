using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private bool hasDungeonKey = false;
    public bool HasDungeonKey => hasDungeonKey;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddDungeonKey()
    {
        hasDungeonKey = true;
    }

    public bool UseDungeonKey()
    {
        if (!hasDungeonKey) return false;
        hasDungeonKey = false;
        return true;
    }
}