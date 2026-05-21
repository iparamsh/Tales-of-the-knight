using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Header("Room Connections")]
    public Room roomA; // room on the left/entry side
    public Room roomB; // room on the right/exit side

    [Header("Settings")]
    public bool isLocked = false;

    public DoorController doorController;
    private Room currentRoom; // which room the player is currently in
    private float transitionCooldown = 0f;

    void Start()
    {
        currentRoom = roomA; // player starts in roomA
    }

    public void SetCurrentRoom(Room room)
    {
        currentRoom = room;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isLocked) return;
        if (doorController != null && !doorController.IsOpen()) return;
        if (RoomManager.Instance == null) return;
        if (transitionCooldown > 0f) return;

        transitionCooldown = 0.8f;
        Room destination = currentRoom == roomA ? roomB : roomA;
        Debug.Log("Transitioning from: " + currentRoom.name + " to: " + destination.name);
        currentRoom = destination;
        RoomManager.Instance.TransitionToRoom(destination);
    }

    void Update()
    {
        if (transitionCooldown > 0f)
            transitionCooldown -= Time.deltaTime;
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }
}