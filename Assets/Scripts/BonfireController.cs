using UnityEngine;

public class BonfireController : MonoBehaviour
{
    // Called via Interactable onInteract event
    public void Rest()
    {
        // Placeholder — UI partner hooks healing and UI here
        Debug.Log("Player rested at bonfire");

        // TODO: restore player health and FP
        // TODO: trigger rest animation/UI
    }
}