using UnityEngine;

/// <summary>
/// Main game manager - controls the entire game state, time, energy, and carbon levels.
/// This is a singleton, meaning only one instance exists in the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance - access this from anywhere with GameManager.Instance
    public static GameManager Instance { get; private set; }

    // ===== GAME STATE =====
    [Header("Game State")]
    public GameState currentGameState = GameState.MainMenu; // Current state: MainMenu, Playing, Won, or Lost
    public bool startAtMainMenu = true;
    
    // ===== CARBON SYSTEM =====
    public float carbonLevel; // Current atmospheric CO2 in parts per million (ppm)
    public const float TARGET_CARBON = 300f; // Win condition for current balance pass
    public const float STARTING_CARBON = 420f; // Starting level: current real-world CO2
    public float carbonReductionRate = 0f; // How fast we're reducing carbon (ppm per second)

    [Header("Balance Tuning")]
    [SerializeField] private float carbonReductionMultiplier = 0.003f;
    [SerializeField] private float underproductionThreshold = 0.72f;
    [SerializeField] private float underproductionPenaltyRate = 0.05f;
    [Header("Difficulty")]
    public GameDifficulty currentDifficulty = GameDifficulty.Normal;
    
    // ===== TIME MANAGEMENT =====
    [Header("Time Management")]
    public TimeManager timeManager; // Manages the game clock and time progression

    // Convenient accessors for time state
    public int currentDay 
    { 
        get => timeManager != null ? timeManager.currentDay : 0;
        set { if (timeManager != null) timeManager.currentDay = value; }
    }

    public float timeOfDay 
    { 
        get => timeManager != null ? timeManager.timeOfDay : 0f;
        set { if (timeManager != null) timeManager.timeOfDay = value; }
    }
    
    public TimeSpeed currentTimeSpeed => timeManager != null ? timeManager.CurrentTimeSpeed : TimeSpeed.Normal;
    
    // ===== ENERGY SYSTEM =====
    [Header("Energy System")]
    public EnergyManager energyManager;          // Manages energy production and storage

    // Convenient accessors for energy state
    public float currentEnergy 
    { 
        get => energyManager != null ? energyManager.currentEnergy : 0f;
        set { if (energyManager != null) energyManager.currentEnergy = value; }
    }

    public float maxEnergyStorage 
    { 
        get => energyManager != null ? energyManager.maxEnergyStorage : 1000f;
        set { if (energyManager != null) energyManager.maxEnergyStorage = value; }
    }

    public float energyProductionRate 
    { 
        get => energyManager != null ? energyManager.energyProductionRate : 0f;
        set { if (energyManager != null) energyManager.energyProductionRate = value; }
    }
    
    // ===== REFERENCES TO OTHER MANAGERS =====
    [Header("References")]
    public UIManager uiManager; // Handles all UI updates and displays
    public MapGenerator mapGenerator; // Generates the tile-based map
    public ResearchManager researchManager; // Manages tech tree and research
    public GeneratorManager generatorManager; // Manages placing and upgrading generators
    public SaveLoadSystem saveLoadSystem; // Saves and loads game data

    // ===== PRIVATE VARIABLES =====
    private bool gameplayPaused = false; // True when time progression should stop

    // ===== GAME STATE ENUM =====
    /// <summary>
    /// Possible game states
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing, // Game is active
        Won,     // Player reduced carbon to target
        Lost     // Carbon rose too high
    }

    /// <summary>
    /// The active difficulty profile, with any inspector-level tuning overrides applied on top.
    /// Always use this instead of DifficultyBalanceLibrary.GetProfile() at runtime.
    /// </summary>
    public DifficultyBalanceProfile CurrentDifficultyProfile => GetEffectiveDifficultyProfile();

    /// <summary>
    /// The carbon level (ppm) the player must reach to win, from the active difficulty profile.
    /// </summary>
    public float CurrentTargetCarbon => CurrentDifficultyProfile.targetCarbon;

    /// <summary>
    /// Builds the effective difficulty profile by starting from the library preset
    /// and then replacing certain values with the GameManager's inspector-tuned overrides.
    /// </summary>
    private DifficultyBalanceProfile GetEffectiveDifficultyProfile()
    {
        DifficultyBalanceProfile profile = DifficultyBalanceLibrary.GetProfile(currentDifficulty);

        return new DifficultyBalanceProfile(
            profile.targetCarbon,
            carbonReductionMultiplier,
            underproductionThreshold,
            underproductionPenaltyRate,
            profile.loseCarbonOffset,
            profile.researchTimeMultiplier,
            profile.researchCostMultiplier,
            profile.generatorOutputMultiplier,
            profile.minimumRealTimeToWinSeconds);
    }

    // ===== TIME SPEED ENUM =====
    /// <summary>
    /// Available game speeds
    /// </summary>
    public enum TimeSpeed
    {
        Paused,   // Time frozen (0x)
        Normal,   // Normal speed (1x)
        Speed2x,  // Double speed (2x)
        Speed5x,  // Five times speed (5x)
        Speed10x  // Ten times speed (10x)
    }

    // ===== UNITY LIFECYCLE METHODS =====
    
    /// <summary>
    /// Called when object is created - sets up singleton
    /// </summary>
    private void Awake()
    {
        // Singleton pattern: ensure only one GameManager exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    /// <summary>
    /// Called on first frame - initializes the game
    /// </summary>
    private void Start()
    {
        CacheMissingReferences();
        if (startAtMainMenu)
        {
            EnterMainMenu();
        }
        else
        {
            StartNewGame();
        }
    }

    /// <summary>
    /// Fills in any manager references that weren't assigned in the Inspector by searching for them on this GameObject or via their singletons.
    /// </summary>
    private void CacheMissingReferences()
    {
        if (uiManager == null) uiManager = UIManager.Instance;
        if (researchManager == null) researchManager = ResearchManager.Instance;
        if (generatorManager == null) generatorManager = GeneratorManager.Instance;
        if (saveLoadSystem == null) saveLoadSystem = SaveLoadSystem.Instance;
        
        // Ensure TimeManager exists
        if (timeManager == null)
        {
            timeManager = GetComponent<TimeManager>();
            if (timeManager == null)
            {
                timeManager = gameObject.AddComponent<TimeManager>();
                Debug.Log("GameManager: Created TimeManager component");
            }
        }
        
        // Ensure EnergyManager exists
        if (energyManager == null)
        {
            energyManager = GetComponent<EnergyManager>();
            if (energyManager == null)
            {
                energyManager = gameObject.AddComponent<EnergyManager>();
                Debug.Log("GameManager: Created EnergyManager component");
            }
        }
    }

    /// <summary>
    /// Called every frame - updates game systems
    /// </summary>
    private void Update()
    {
        // Don't update if game is over
        if (currentGameState != GameState.Playing) return;

        if (timeManager != null)
        {
            timeManager.UpdateTime();
        }
        
        UpdateEnergy();
        UpdateCarbonLevel();
        CheckWinLoseConditions();
    }

    // ===== INITIALIZATION =====
    
    /// <summary>
    /// Sets up the game at start
    /// </summary>
    private void InitializeGame(bool generateFreshMap = true)
    {
        // Set starting values
        carbonLevel = STARTING_CARBON;
        // Initialize energy system
        if (energyManager != null)
        {
            energyManager.ResetEnergy();
        }
        currentGameState = GameState.Playing;
        
        // Initialize time system
        if (timeManager != null)
        {
            timeManager.ResetTime();
            timeManager.OnDayChanged -= OnNewDay;
            timeManager.OnDayChanged += OnNewDay;
        }
        else
        {
            gameplayPaused = false;
        }
        
        // Initialize all game systems
        if (mapGenerator != null)
        {
            if (generateFreshMap)
            {
                mapGenerator.seed = 0;
            }

            if (generateFreshMap)
            {
                mapGenerator.GenerateMap();
            }
        }
        if (researchManager != null) researchManager.Initialize();
        if (generatorManager != null) generatorManager.Initialize();
        ApplyDifficultyToSystems();
        if (uiManager != null) uiManager.Initialize();
    }

    /// <summary>
    /// Legacy energy update - used as fallback when EnergyManager not assigned
    /// </summary>
    private void UpdateEnergy()
    {
        // Calculate current energy production from all generators
        energyProductionRate = generatorManager != null ? 
            generatorManager.GetTotalEnergyProduction(timeOfDay) : 0f;
        
        // Add energy based on production rate
        float speedMultiplier = TimeSystemUtils.GetSpeedMultiplier(currentTimeSpeed);
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
    /// Updates carbon levels based on clean energy production
    /// </summary>
    private void UpdateCarbonLevel()
    {
        DifficultyBalanceProfile profile = CurrentDifficultyProfile;

        // Calculate how much carbon we're reducing
        carbonReductionRate = CalculateCarbonReduction();
        
        float speedMultiplier = TimeSystemUtils.GetSpeedMultiplier(currentTimeSpeed);
        carbonLevel -= carbonReductionRate * Time.deltaTime * speedMultiplier;
        
        // If not producing enough clean energy, carbon rises
        if (carbonReductionRate < profile.underproductionThreshold)
        {
            carbonLevel += profile.underproductionPenaltyRate * Time.deltaTime * speedMultiplier;
        }

        carbonLevel = Mathf.Max(carbonLevel, CurrentTargetCarbon - 20f);

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateCarbonDisplay(carbonLevel, CurrentTargetCarbon);
        }
    }

    /// <summary>
    /// Calculates carbon reduction rate based on energy production
    /// </summary>
    /// <returns>Carbon reduction in ppm per second</returns>
    private float CalculateCarbonReduction()
    {
        DifficultyBalanceProfile profile = CurrentDifficultyProfile;

        // More clean energy = faster carbon reduction
        float totalProduction = generatorManager != null ? 
            generatorManager.GetTotalEnergyProduction(0.5f) : 0f; // Use noon production as baseline
        
        return totalProduction * profile.carbonReductionMultiplier;
    }

    /// <summary>
    /// Checks if player has won or lost
    /// </summary>
    private void CheckWinLoseConditions()
    {
        if (carbonLevel > GetLoseCarbonThreshold())
        {
            LoseGame();
            return;
        }

        if (carbonLevel <= CurrentTargetCarbon)
        {
            WinGame();
        }
    }

    /// <summary>
    /// Returns the carbon level (ppm) at which the player loses. Derived from starting carbon plus the difficulty's lose offset.
    /// </summary>
    public float GetLoseCarbonThreshold()
    {
        return STARTING_CARBON + CurrentDifficultyProfile.loseCarbonOffset;
    }

    /// <summary>
    /// Changes the current difficulty setting. Pass applyToSystems = false when loading a save
    /// (the load process applies difficulty after restoring all state).
    /// </summary>
    public void SetDifficulty(GameDifficulty difficulty, bool applyToSystems = true)
    {
        currentDifficulty = difficulty;

        if (applyToSystems)
        {
            ApplyDifficultyToSystems();
        }
    }

    /// <summary>
    /// Pushes the current difficulty profile out to ResearchManager, GeneratorManager, and UIManager
    /// so they all use the same balance values.
    /// </summary>
    public void ApplyDifficultyToSystems()
    {
        if (researchManager != null)
        {
            researchManager.ApplyDifficultyProfile(CurrentDifficultyProfile);
        }

        if (generatorManager != null)
        {
            generatorManager.ApplyDifficultyProfile(CurrentDifficultyProfile);
        }

        if (uiManager != null)
        {
            uiManager.RefreshAllDisplays();
        }
    }

    /// <summary>
    /// Called when a new day starts
    /// </summary>
    private void OnNewDay()
    {
        Debug.Log($"New Day: {currentDay}");
        // Can add daily events here (random weather, etc.)
    }

    /// <summary>
    /// Transitions to the main menu state and pauses time.
    /// </summary>
    public void EnterMainMenu()
    {
        CacheMissingReferences();
        currentGameState = GameState.MainMenu;
        SetTimeSpeed(TimeSpeed.Paused);
        if (uiManager != null) uiManager.ShowMainMenu();
    }

    /// <summary>
    /// Starts a fresh game with a newly generated map.
    /// </summary>
    public void StartNewGame()
    {
        CacheMissingReferences();
        InitializeGame(true);
        if (uiManager != null) uiManager.HideMainMenu();
    }

    /// <summary>
    /// Initializes systems and then loads a saved game from disk.
    /// Returns false if no save file exists or loading fails.
    /// </summary>
    public bool LoadSavedGameFromMenu()
    {
        CacheMissingReferences();
        if (saveLoadSystem == null)
        {
            Debug.LogError("SaveLoadSystem missing - cannot load game.");
            return false;
        }

        InitializeGame(false);
        bool loaded = saveLoadSystem.LoadGame();
        if (!loaded)
        {
            EnterMainMenu();
            return false;
        }

        currentGameState = GameState.Playing;
        if (uiManager != null)
        {
            uiManager.HideMainMenu();
            uiManager.RefreshAllDisplays();
        }
        return true;
    }

    // ===== PUBLIC METHODS =====
    
    /// <summary>
    /// Triggers win state
    /// </summary>
    public void WinGame()
    {
        currentGameState = GameState.Won;
        SetTimeSpeed(TimeSpeed.Paused);
        Debug.Log("You won! Carbon levels restored to pre-industrial levels!");
        if (uiManager != null) uiManager.ShowWinScreen();
    }

    /// <summary>
    /// Triggers lose state
    /// </summary>
    public void LoseGame()
    {
        currentGameState = GameState.Lost;
        SetTimeSpeed(TimeSpeed.Paused);
        Debug.Log("Game Over! Carbon levels too high!");
        if (uiManager != null) uiManager.ShowLoseScreen();
    }

    /// <summary>
    /// Changes the game speed
    /// </summary>
    /// <param name="speed">New speed setting</param>
    public void SetTimeSpeed(TimeSpeed speed)
    {
        if (timeManager != null)
        {
            timeManager.SetTimeSpeed(speed);
        }
        else
        {
            gameplayPaused = (speed == TimeSpeed.Paused);
        }
    }

    /// <summary>
    /// Attempts to spend energy. Returns false if the current energy is insufficient.
    /// </summary>
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

    /// <summary>
    /// Increases maximum energy storage (called by battery research)
    /// </summary>
    /// <param name="amount">Amount to add to max storage</param>
    public void AddEnergyStorage(float amount)
    {
        maxEnergyStorage += amount;
        if (uiManager != null)
        {
            uiManager.UpdateEnergyDisplay(currentEnergy, maxEnergyStorage);
        }
    }

    /// <summary>
    /// Saves the current game state to file
    /// </summary>
    public void SaveGame()
    {
        CacheMissingReferences();
        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveGame();
        }
    }

    /// <summary>
    /// Loads a saved game from file
    /// </summary>
    public void LoadGame()
    {
        LoadSavedGameFromMenu();
    }
}
