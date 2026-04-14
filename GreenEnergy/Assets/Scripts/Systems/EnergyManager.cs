using UnityEngine;

/// <summary>
/// Manages the game's energy production, storage, and consumption system.
/// </summary>
public class EnergyManager : MonoBehaviour
{
    [Header("Energy Settings")]
    public float currentEnergy = 0f;              // Energy we currently have available
    public float maxEnergyStorage = 1000f;        // Maximum energy we can store
    public float energyProductionRate = 0f;       // Energy generated per second

    [Header("References")]
    public GeneratorManager generatorManager;     // For getting total production
    public TimeManager timeManager;               // For getting current time of day
    public UIManager uiManager;                   // For updating displays

    /// <summary>
    /// Updates energy production and storage based on current generators and time of day.
    /// Should be called from GameManager.Update()
    /// </summary>
    public void UpdateEnergy()
    {
        if (generatorManager == null || timeManager == null)
        {
            return;
        }

        // Calculate current energy production from all generators
        energyProductionRate = generatorManager.GetTotalEnergyProduction(timeManager.timeOfDay);

        // Add energy based on production rate, accounting for game speed
        float speedMultiplier = TimeSystemUtils.GetSpeedMultiplier(timeManager.CurrentTimeSpeed);
        currentEnergy += energyProductionRate * Time.deltaTime * speedMultiplier;

        // Cap energy at maximum storage
        currentEnergy = Mathf.Min(currentEnergy, maxEnergyStorage);

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateEnergyDisplay(currentEnergy, maxEnergyStorage);
        }
    }

    /// <summary>
    /// Attempts to spend energy - returns false if not enough available
    /// </summary>
    /// <param name="amount">Amount of energy to spend</param>
    /// <returns>True if successful, false if insufficient energy</returns>
    public bool TryConsumeEnergy(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning("EnergyManager: Cannot consume negative energy!");
            return false;
        }

        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds energy to the system (not typical but useful for testing/events)
    /// </summary>
    /// <param name="amount">Amount of energy to add</param>
    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergyStorage);
    }

    /// <summary>
    /// Resets energy to starting state
    /// </summary>
    public void ResetEnergy()
    {
        // Auto-wire missing references
        if (generatorManager == null) generatorManager = GeneratorManager.Instance;
        if (timeManager == null) timeManager = GetComponent<TimeManager>();
        if (uiManager == null) uiManager = UIManager.Instance;
        
        currentEnergy = 120f; // Starting amount
        energyProductionRate = 0f;
    }

    /// <summary>
    /// Gets the current energy percentage (0-1)
    /// </summary>
    public float GetEnergyPercentage()
    {
        return maxEnergyStorage > 0f ? currentEnergy / maxEnergyStorage : 0f;
    }
}
