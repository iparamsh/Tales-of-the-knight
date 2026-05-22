using System.Collections.Generic;
using UnityEngine;

public static class PauseStateManager
{
    private static readonly HashSet<int> pauseSources = new HashSet<int>();

    public static bool IsPaused => pauseSources.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        pauseSources.Clear();
        Time.timeScale = 1f;
    }

    public static void RequestPause(Object source)
    {
        if (source == null) return;

        if (pauseSources.Add(source.GetInstanceID()))
            ApplyPauseState();
    }

    public static void ReleasePause(Object source)
    {
        if (source == null) return;

        if (pauseSources.Remove(source.GetInstanceID()))
            ApplyPauseState();
    }

    public static void ClearAll()
    {
        if (pauseSources.Count == 0)
            return;

        pauseSources.Clear();
        ApplyPauseState();
    }

    private static void ApplyPauseState()
    {
        Time.timeScale = pauseSources.Count > 0 ? 0f : 1f;
    }
}