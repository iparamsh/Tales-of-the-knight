using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Camera Bounds")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    public void ApplyBounds(CameraFollow camera)
    {
        camera.minX = minX;
        camera.maxX = maxX;
        camera.minY = minY;
        camera.maxY = maxY;
    }
}