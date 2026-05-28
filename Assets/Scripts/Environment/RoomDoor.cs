using UnityEngine;
using System.Collections;


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

        Vector3 playerPos = other.transform.root.position;
        bool playerInRoomA = playerPos.x >= roomA.minX && playerPos.x <= roomA.maxX &&
                            playerPos.y >= roomA.minY && playerPos.y <= roomA.maxY;

        Room destination = playerInRoomA ? roomB : roomA;
        currentRoom = destination;

        transitionCooldown = 1.8f;
        RoomManager.Instance.TransitionToRoom(destination);

        // Lock door if entering boss arena
        // Lock door if entering boss arena
        if (destination == roomB)
            StartCoroutine(LockBossDoorDelayed());
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

    public void ResetToRoomA()
    {
        Debug.Log("ResetToRoomA — roomA: " + roomA?.name + " setting currentRoom");
        currentRoom = roomA;
        isLocked = false;
        transitionCooldown = 0f;
        Debug.Log("ResetToRoomA complete — currentRoom: " + currentRoom?.name);
    }
    
    IEnumerator LockBossDoorDelayed()
    {
        yield return new WaitForSeconds(RoomManager.Instance.fadeDuration * 2f + 0.1f);
        DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        foreach (DoorController door in doors)
        {
            if (door.isBossDoor)
            {
                door.LockDoor();
                break;
            }
        }
    }
}