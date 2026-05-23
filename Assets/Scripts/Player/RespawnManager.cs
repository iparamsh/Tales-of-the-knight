using UnityEngine;
using UnityEngine.SceneManagement;

public static class RespawnManager
{
    public static bool HasBonfirePosition { get; private set; }
    public static Vector3 BonfirePosition { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        HasBonfirePosition = false;
    }

    public static void SetBonfirePosition(Vector3 position)
    {
        HasBonfirePosition = true;
        BonfirePosition = position;
    }

    public static void Reset()
    {
        HasBonfirePosition = false;
    }

    public static void Respawn(PlayerController player)
    {
        if (HasBonfirePosition)
        {
            player.transform.position = BonfirePosition;
            player.Revive();
        }
        else
        {
            PauseStateManager.ClearAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
