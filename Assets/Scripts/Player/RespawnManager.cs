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

            BossController boss = GameObject.FindFirstObjectByType<BossController>();
            boss?.ResetBoss();

            // Reset camera to bonfire room bounds
            Room[] rooms = GameObject.FindObjectsByType<Room>(FindObjectsSortMode.None);
            foreach (Room room in rooms)
            {
                if (BonfirePosition.x >= room.minX && BonfirePosition.x <= room.maxX &&
                    BonfirePosition.y >= room.minY && BonfirePosition.y <= room.maxY)
                {
                    CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                    if (cam != null)
                    {
                        room.ApplyBounds(cam);
                        cam.transform.position = new Vector3(
                            Mathf.Clamp(player.transform.position.x, room.minX, room.maxX),
                            Mathf.Clamp(player.transform.position.y, room.minY, room.maxY),
                            cam.transform.position.z
                        );
                    }
                    break;
                }
            }
        }
        else
        {
            PauseStateManager.ClearAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
