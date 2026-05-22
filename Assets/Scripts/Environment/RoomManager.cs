using UnityEngine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("References")]
    public CameraFollow cameraFollow;
    public CanvasGroup fadePanel;
    public Transform player;
    public float fadeDuration = 0.8f;

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TransitionToRoom(Room room)
    {
        if (isTransitioning) return;
        StartCoroutine(DoTransition(room));
    }

    IEnumerator DoTransition(Room room)
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        // Move player
        if (room.spawnPoint != null)
            player.position = room.spawnPoint.position;

        // Update camera bounds
        room.ApplyBounds(cameraFollow);

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
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