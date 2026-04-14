using UnityEngine;

/// <summary>
/// Manages the game's time system - day/night cycles, time speed, and game clock.
/// Works in conjunction with GameManager to provide clean separation of concerns.
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    public int currentDay = 0;                    // Number of in-game days passed
    public float timeOfDay = 0.25f;              // Current time: 0-1 where 0=midnight, 0.25=6am, 0.5=noon, 0.75=6pm
    public float dayDuration = 240f;             // How long one day lasts in real seconds

    [Header("References")]
    public UIManager uiManager;                   // UI that displays time

    // Public properties for accessing time state
    public GameManager.TimeSpeed CurrentTimeSpeed { get; private set; } = GameManager.TimeSpeed.Normal;
    public bool IsGameplayPaused { get; private set; } = false;

    // Callbacks for external systems
    public delegate void OnDayChangedDelegate();
    public OnDayChangedDelegate OnDayChanged;

    /// <summary>
    /// Updates time progression based on current game speed.
    /// Should be called from GameManager.Update()
    /// </summary>
    public void UpdateTime()
    {
        // Don't advance time if paused or paused game state
        if (IsGameplayPaused || CurrentTimeSpeed == GameManager.TimeSpeed.Paused)
        {
            return;
        }

        // Get time multiplier based on current speed
        float speedMultiplier = TimeSystemUtils.GetSpeedMultiplier(CurrentTimeSpeed);
        float deltaTime = Time.deltaTime * speedMultiplier;

        // Track time progression
        timeOfDay += deltaTime / dayDuration;

        // Check if a full day has passed
        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f;      // Reset to midnight
            currentDay++;         // Increment day counter
            OnDayChanged?.Invoke(); // Notify listeners of day change
        }

        // Update UI with current time
        if (uiManager != null)
        {
            uiManager.UpdateTimeDisplay(currentDay, timeOfDay);
        }
    }

    /// <summary>
    /// Changes the game speed
    /// </summary>
    /// <param name="speed">New speed setting</param>
    public void SetTimeSpeed(GameManager.TimeSpeed speed)
    {
        CurrentTimeSpeed = speed;
        IsGameplayPaused = (speed == GameManager.TimeSpeed.Paused);
    }

    /// <summary>
    /// Pauses or resumes gameplay time progression
    /// </summary>
    /// <param name="paused">True to pause, false to resume</param>
    public void SetGameplayPaused(bool paused)
    {
        IsGameplayPaused = paused;
    }

    /// <summary>
    /// Resets time to start of game
    /// </summary>
    public void ResetTime()
    {
        // Auto-wire missing references
        if (uiManager == null) uiManager = UIManager.Instance;
        
        currentDay = 0;
        timeOfDay = 0.25f; // Start at 6 AM
        CurrentTimeSpeed = GameManager.TimeSpeed.Normal;
        IsGameplayPaused = false;
    }

    /// <summary>
    /// Gets the sun brightness multiplier based on time of day (0=night, 1=noon)
    /// </summary>
    public float GetDayNightMultiplier()
    {
        // Simplified sun cycle: peaks at 0.5 (noon), lowest at 0/1 (midnight)
        return Mathf.Abs(Mathf.Sin(timeOfDay * Mathf.PI));
    }
}
