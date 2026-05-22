using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 8f;
    public Vector2 offset = new Vector2(0f, 2f);

    [Header("Bounds")]
    public float minX, maxX, minY, maxY;
    public bool useBounds = true;

    [Header("Axes")]
    public bool followY = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void FixedUpdate()
    {
        if (target == null) return;

        float targetX = target.position.x + offset.x;
        float targetY = target.position.y + offset.y;

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            targetX = Mathf.Clamp(targetX, minX + halfW, maxX - halfW);
            targetY = Mathf.Clamp(targetY, minY + halfH, maxY - halfH);
        }

        float newX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.fixedDeltaTime);
        float newY = followY 
            ? Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.fixedDeltaTime)
            : transform.position.y;

        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}