using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Tune this in the Inspector — increase if Idle still flashes during direction changes
    [SerializeField] private float idleDelay = 0.2f;
    private float idleTimer = 0f;

    void Start()
    {
        MoveAction.Enable();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Vector2 move = MoveAction.ReadValue<Vector2>();
        HandleMovement(move);
        HandleAnimation(move);
        HandleFacing(move);
    }

    // Moves the player based on input — framerate-independent via Time.deltaTime
    private void HandleMovement(Vector2 move)
    {
        Vector2 position = (Vector2)transform.position + move * 4.5f * Time.deltaTime;
        transform.position = position;
    }

    // Switches between Idle and Run with a grace period to avoid flashing on direction changes
    private void HandleAnimation(Vector2 move)
    {
        if (move.sqrMagnitude > 0.01f)
        {
            idleTimer = idleDelay;
            animator.SetBool("isMoving", true);
        }
        else
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
                animator.SetBool("isMoving", false);
        }
    }

    // Flips the sprite so the character faces the direction of movement
    private void HandleFacing(Vector2 move)
    {
        if (move.x < 0)
            spriteRenderer.flipX = true;
        else if (move.x > 0)
            spriteRenderer.flipX = false;
    }
}