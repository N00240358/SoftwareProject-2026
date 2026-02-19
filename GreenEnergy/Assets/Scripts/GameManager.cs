using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Main game manager - controls the entire game state, time, energy, and carbon levels.
/// This is a singleton, meaning only one instance exists in the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance - access this from anywhere with GameManager.Instance
    public static GameManager Instance { get; private set; }

    // GAME STATE
    [Header("Game State")]
    public GameState currentGameState = GameState.Playing; // Current state: Playing, Won, or Lost
    
    // CARBON SYSTEM
    public float carbonLevel; // Current CO2 in parts per million (ppm)
    public const float TARGET_CARBON = 280f; // Win condition: pre-industrial CO2 level
    public const float STARTING_CARBON = 420f; // Starting level: current real-world CO2
    public float carbonReductionRate = 0f; // How fast we're reducing carbon (ppm per second)
    
    // TIME MANAGEMENT
    [Header("Time Management")]
    public int currentDay = 0; // Number of in-game days passed
    public float timeOfDay = 0f; // Current time: 0-1 where 0=midnight, 0.25=6am, 0.5=noon, 0.75=6pm
    public float dayDuration = 300f; // How long one day lasts in real seconds (5 minutes)
    public TimeSpeed currentTimeSpeed = TimeSpeed.Normal; // Current game speed
    
    // ENERGY SYSTEM
    [Header("Energy System")]
    public float currentEnergy = 0f; // Energy we have available right now
    public float maxEnergyStorage = 1000f; // Maximum energy we can store (increases with battery research)
    public float energyProductionRate = 0f; // Energy generated per second from all generators
    
    // REFERENCES TO OTHER MANAGERS
    [Header("References")]
    public UIManager uiManager; // Handles all UI updates and displays
    public MapGenerator mapGenerator; // Generates the tile-based map
    public ResearchManager researchManager; // Manages tech tree and research
    public GeneratorManager generatorManager; // Manages placing and upgrading generators
    public PDASystem pdaSystem; // Educational content system
    public SaveLoadSystem saveLoadSystem; // Saves and loads game data

    // PRIVATE VARIABLES
    private float timeSinceLastDay = 0f; // Internal timer for day progression
    private bool isPaused = false; // Whether game is paused

    // GAME STATE ENUM
    /// Possible game states
    public enum GameState
    {
        Playing, // Game is active
        Won,     // Player reduced carbon to target
        Lost     // Carbon rose too high
    }

    // TIME SPEED ENUM
    /// Available game speeds
    public enum TimeSpeed
    {
        Paused,   // Time frozen (0x)
        Normal,   // Normal speed (1x)
        Speed2x,  // Double speed (2x)
        Speed5x,  // Five times speed (5x)
        Speed10x  // Ten times speed (10x)
    }

    // UNITY LIFECYCLE METHODS
    
    /// Called when object is created - sets up singleton
    private void Awake()
    {
        // Singleton pattern: ensure only one GameManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object when loading new scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    /// Called on first frame - initializes the game
    private void Start()
    {
        InitializeGame();
    }

    /// Called every frame - updates game systems
    private void Update()
    {
        // Don't update if game is over
        if (currentGameState != GameState.Playing) return;

        UpdateTime();
        UpdateEnergy();
        UpdateCarbonLevel();
        CheckWinLoseConditions();
    }

    // INITIALIZATION
    
    /// Sets up the game at start
    private void InitializeGame()
    {
        // Set starting values
        carbonLevel = STARTING_CARBON;
        currentDay = 0;
        timeOfDay = 0.25f; // Start at 6 AM
        currentEnergy = 50f; // Give player some starting energy
        
        // Initialize all game systems
        if (mapGenerator != null) mapGenerator.GenerateMap();
        if (researchManager != null) researchManager.Initialize();
        if (generatorManager != null) generatorManager.Initialize();
        if (uiManager != null) uiManager.Initialize();
    }

    // TIME SYSTEM
    
    /// Updates the game time and day/night cycle
    private void UpdateTime()
    {
        // Don't advance time if paused
        if (isPaused || currentTimeSpeed == TimeSpeed.Paused) return;

        // Get time multiplier based on current speed
        float speedMultiplier = GetSpeedMultiplier();
        float deltaTime = Time.deltaTime * speedMultiplier;
        
        // Track time
        timeSinceLastDay += deltaTime;
        timeOfDay += deltaTime / dayDuration;

        // Check if a full day has passed
        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f; // Reset to midnight
            currentDay++; // Increment day counter
            OnNewDay(); // Trigger day change events
        }

        // Update UI with current time
        if (uiManager != null)
        {
            uiManager.UpdateTimeDisplay(currentDay, timeOfDay);
        }
    }

    /// Updates energy production and storage
    private void UpdateEnergy()
    {
        // Calculate current energy production from all generators
        energyProductionRate = generatorManager != null ? 
            generatorManager.GetTotalEnergyProduction(timeOfDay) : 0f;
        
        // Add energy based on production rate
        float speedMultiplier = GetSpeedMultiplier();
        currentEnergy += energyProductionRate * Time.deltaTime * speedMultiplier;
        
        // Cap energy at maximum storage
        currentEnergy = Mathf.Min(currentEnergy, maxEnergyStorage);

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateEnergyDisplay(currentEnergy, maxEnergyStorage);
        }
    }

    /// Updates carbon levels based on clean energy production
    private void UpdateCarbonLevel()
    {
        // Calculate how much carbon we're reducing
        carbonReductionRate = CalculateCarbonReduction();
        
        float speedMultiplier = GetSpeedMultiplier();
        carbonLevel -= carbonReductionRate * Time.deltaTime * speedMultiplier;
        
        // If not producing enough clean energy, carbon rises
        if (carbonReductionRate < 1f) // Minimum threshold
        {
            carbonLevel += 0.5f * Time.deltaTime * speedMultiplier;
        }

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateCarbonDisplay(carbonLevel, TARGET_CARBON);
        }
    }

    /// Calculates carbon reduction rate based on energy production
    /// <returns>Carbon reduction in ppm per second</returns>
    private float CalculateCarbonReduction()
    {
        // More clean energy = faster carbon reduction
        float totalProduction = generatorManager != null ? 
            generatorManager.GetTotalEnergyProduction(0.5f) : 0f; // Use noon production as baseline
        
        return totalProduction * 0.01f; // Tunable multiplier for game balance
    }

    /// Checks if player has won or lost
    private void CheckWinLoseConditions()
    {
        // Win: Carbon reduced to pre-industrial levels
        if (carbonLevel <= TARGET_CARBON)
        {
            WinGame();
        }

        // Lose: Carbon rose too high (100 ppm above starting)
        if (carbonLevel > STARTING_CARBON + 100f)
        {
            LoseGame();
        }
    }

    /// Called when a new day starts
    private void OnNewDay()
    {
        Debug.Log($"New Day: {currentDay}");
        // Can add daily events here (random weather, etc.)
    }

    // PUBLIC METHODS
    
    /// Triggers win state
    public void WinGame()
    {
        currentGameState = GameState.Won;
        Debug.Log("You won! Carbon levels restored to pre-industrial levels!");
        if (uiManager != null) uiManager.ShowWinScreen();
    }

    /// Triggers lose state
    public void LoseGame()
    {
        currentGameState = GameState.Lost;
        Debug.Log("Game Over! Carbon levels too high!");
        if (uiManager != null) uiManager.ShowLoseScreen();
    }

    /// Changes the game speed
    /// <param name="speed">New speed setting</param>
    public void SetTimeSpeed(TimeSpeed speed)
    {
        currentTimeSpeed = speed;
        isPaused = (speed == TimeSpeed.Paused);
    }

    /// Converts TimeSpeed enum to actual multiplier
    /// <returns>Speed multiplier (0 for paused, 1 for normal, etc.)</returns>
    private float GetSpeedMultiplier()
    {
        switch (currentTimeSpeed)
        {
            case TimeSpeed.Paused: return 0f;
            case TimeSpeed.Normal: return 1f;
            case TimeSpeed.Speed2x: return 2f;
            case TimeSpeed.Speed5x: return 5f;
            case TimeSpeed.Speed10x: return 10f;
            default: return 1f;
        }
    }

    /// Attempts to spend energy - returns false if not enough
    /// <param name="amount">Amount of energy to spend</param>
    /// <returns>True if successful, false if insufficient energy</returns>
    public bool TryConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }

    /// Increases maximum energy storage (called by battery research)
    /// <param name="amount">Amount to add to max storage</param>
    public void AddEnergyStorage(float amount)
    {
        maxEnergyStorage += amount;
    }

    /// Saves the current game state to file
    public void SaveGame()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveGame();
        }
    }

    /// Loads a saved game from file
    public void LoadGame()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.LoadGame();
        }
    }
}
