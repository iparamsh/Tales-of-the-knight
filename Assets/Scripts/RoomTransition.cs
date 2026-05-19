using UnityEngine;
using System.Collections;

public class RoomTransition : MonoBehaviour
{
    [Header("References")]
    public CameraFollow cameraFollow;
    public CanvasGroup fadePanel;
    public Transform player;
    public DoorController door;

    [Header("Boss Room Settings")]
    public Transform bossRoomSpawnPoint;
    public float fadeDuration = 0.8f;

    [Header("Boss Room Camera Bounds")]
    public float bossMinX;
    public float bossMaxX;
    public float bossMinY;
    public float bossMaxY;

    private bool hasTransitioned = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTransitioned)
        {
            hasTransitioned = true;
            StartCoroutine(TransitionToBossRoom());
        }
    }

    IEnumerator TransitionToBossRoom()
    {
        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        // Move player to boss room spawn
        player.position = bossRoomSpawnPoint.position;

        // Update camera bounds to boss room
        cameraFollow.minX = bossMinX;
        cameraFollow.maxX = bossMaxX;
        cameraFollow.minY = bossMinY;
        cameraFollow.maxY = bossMaxY;

        // Lock door behind player
        if (door != null)
            door.LockDoor();

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadePanel.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = to;
    }
}