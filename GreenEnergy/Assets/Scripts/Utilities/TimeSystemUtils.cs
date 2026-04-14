using UnityEngine;

/// <summary>
/// Utility class for time system calculations and conversions.
/// </summary>
public static class TimeSystemUtils
{
    /// <summary>
    /// Gets the game speed multiplier based on the current time speed setting.
    /// </summary>
    /// <param name="timeSpeed">The time speed to convert</param>
    /// <returns>Speed multiplier as a float (0, 1, 2, 5, 10, etc.)</returns>
    public static float GetSpeedMultiplier(GameManager.TimeSpeed timeSpeed)
    {
        switch (timeSpeed)
        {
            case GameManager.TimeSpeed.Paused: return 0f;
            case GameManager.TimeSpeed.Normal: return 1f;
            case GameManager.TimeSpeed.Speed2x: return 2f;
            case GameManager.TimeSpeed.Speed5x: return 5f;
            case GameManager.TimeSpeed.Speed10x: return 10f;
            default: return 1f;
        }
    }
}
