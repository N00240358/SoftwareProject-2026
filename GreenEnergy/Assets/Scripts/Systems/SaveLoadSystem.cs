using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Top-level save data container. Serialized to JSON and written to disk.
/// Contains all state needed to fully restore a game session.
/// </summary>
[System.Serializable]
public class GameSaveData
{
    public int currentDay;            // Number of in-game days elapsed
    public float timeOfDay;           // Time within the current day (0–1)
    public float carbonLevel;         // Current atmospheric CO2 in ppm
    public float currentEnergy;       // Energy in storage at time of save
    public float maxEnergyStorage;    // Maximum storage capacity at time of save
    public int mapSeed;               // Seed used to regenerate the same map on load
    public string timeSpeed;          // Saved as enum name, e.g. "Normal"
    public string difficulty;         // Saved as enum name, e.g. "Hard"

    public List<GeneratorSaveData> generators;      // All placed generators
    public List<ResearchSaveData> researchNodes;    // All generator research node states
    public List<BatterySaveData> batteryNodes;      // All battery research node states
    public List<PDAEntryUnlockSaveData> pdaEntries; // Which PDA entries have been unlocked
}

/// <summary>
/// Save data for a single placed generator. Enough information to reconstruct
/// a Generator instance via GeneratorManager.LoadGenerator().
/// </summary>
[System.Serializable]
public class GeneratorSaveData
{
    public string generatorId; // Unique GUID assigned when the generator was placed
    public string type;        // Generator type name (e.g. "Solar")
    public int tier;           // Tier level (1–10)
    public int posX;           // Grid X position on the map
    public int posY;           // Grid Y position on the map
    public string biome;       // Biome name at that position (e.g. "Desert")
}

/// <summary>
/// Save data for a single research node — records unlock and in-progress state.
/// </summary>
[System.Serializable]
public class ResearchSaveData
{
    public string nodeId;          // e.g. "Solar_Tier3"
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress; // 0–1 completion fraction
}

/// <summary>
/// Save data for a single battery research node.
/// </summary>
[System.Serializable]
public class BatterySaveData
{
    public int tier;               // Battery tier (1–10)
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress; // 0–1 completion fraction
}

/// <summary>
/// Handles saving and loading the game to a JSON file on disk.
/// Save file is written to Application.persistentDataPath/savegame.json.
/// </summary>
public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    private string saveFilePath;
    private const string SAVE_FILE_NAME = "savegame.json";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    /// <summary>
    /// Serializes the full game state to a JSON file on disk.
    /// Collects data from GameManager, GeneratorManager, ResearchManager, and PDAProgressionSystem.
    /// </summary>
    public void SaveGame()
    {
        GameSaveData saveData = new GameSaveData();
        
        // Save game state
        if (GameManager.Instance != null)
        {
            saveData.currentDay = GameManager.Instance.currentDay;
            saveData.timeOfDay = GameManager.Instance.timeOfDay;
            saveData.carbonLevel = GameManager.Instance.carbonLevel;
            saveData.currentEnergy = GameManager.Instance.currentEnergy;
            saveData.maxEnergyStorage = GameManager.Instance.maxEnergyStorage;
            saveData.timeSpeed = GameManager.Instance.currentTimeSpeed.ToString();
            saveData.difficulty = GameManager.Instance.currentDifficulty.ToString();
        }
        
        // Save map seed
        if (GameManager.Instance != null && GameManager.Instance.mapGenerator != null)
        {
            saveData.mapSeed = GameManager.Instance.mapGenerator.seed;
        }
        
        // Save generators
        saveData.generators = new List<GeneratorSaveData>();
        if (GeneratorManager.Instance != null)
        {
            foreach (var gen in GeneratorManager.Instance.GetAllGenerators())
            {
                GeneratorSaveData genData = new GeneratorSaveData
                {
                    generatorId = gen.generatorId,
                    type = gen.type.ToString(),
                    tier = gen.tier,
                    posX = gen.position.x,
                    posY = gen.position.y,
                    biome = gen.biome.ToString()
                };
                saveData.generators.Add(genData);
            }
        }
        
        // Save research
        saveData.researchNodes = new List<ResearchSaveData>();
        if (ResearchManager.Instance != null)
        {
            foreach (var kvp in ResearchManager.Instance.GetAllNodes())
            {
                ResearchSaveData resData = new ResearchSaveData
                {
                    nodeId = kvp.Key,
                    isUnlocked = kvp.Value.isUnlocked,
                    isResearching = kvp.Value.isResearching,
                    researchProgress = kvp.Value.researchProgress
                };
                saveData.researchNodes.Add(resData);
            }
            
            // Save battery research
            saveData.batteryNodes = new List<BatterySaveData>();
            foreach (var node in ResearchManager.Instance.GetAllBatteryNodes())
            {
                BatterySaveData batData = new BatterySaveData
                {
                    tier = node.tier,
                    isUnlocked = node.isUnlocked,
                    isResearching = node.isResearching,
                    researchProgress = node.researchProgress
                };
                saveData.batteryNodes.Add(batData);
            }
        }

        // Save PDA unlocks
        saveData.pdaEntries = new List<PDAEntryUnlockSaveData>();
        if (PDAProgressionSystem.Instance != null)
        {
            saveData.pdaEntries = PDAProgressionSystem.Instance.GetSaveData();
        }
        
        // Serialize to JSON
        string json = JsonUtility.ToJson(saveData, true);
        
        // Write to file
        try
        {
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Game saved to {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    /// <summary>
    /// Reads the JSON save file from disk and restores game state.
    /// Returns true on success, false if no save file exists or if parsing fails.
    /// </summary>
    public bool LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No save file found.");
            return false;
        }
        
        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            ApplyLoadedData(saveData);
            
            Debug.Log("Game loaded successfully!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pushes a loaded GameSaveData snapshot into all runtime systems.
    /// Order matters: difficulty is applied first so subsequent system initializations use the correct profile.
    /// </summary>
    private void ApplyLoadedData(GameSaveData saveData)
    {
        if (GameManager.Instance != null)
        {
            GameDifficulty difficulty = GameDifficulty.Normal;
            if (DifficultyBalanceLibrary.TryParseDifficulty(saveData.difficulty, out GameDifficulty parsedDifficulty))
            {
                difficulty = parsedDifficulty;
            }

            GameManager.Instance.SetDifficulty(difficulty, false);
        }

        // Load game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentDay = saveData.currentDay;
            GameManager.Instance.timeOfDay = saveData.timeOfDay;
            GameManager.Instance.carbonLevel = saveData.carbonLevel;
            GameManager.Instance.currentEnergy = saveData.currentEnergy;
            GameManager.Instance.maxEnergyStorage = saveData.maxEnergyStorage;
            
            if (System.Enum.TryParse(saveData.timeSpeed, out GameManager.TimeSpeed speed))
            {
                GameManager.Instance.SetTimeSpeed(speed);
            }
        }
        
        // Regenerate map with same seed
        if (GameManager.Instance != null && GameManager.Instance.mapGenerator != null)
        {
            if (saveData.mapSeed > 0)
            {
                GameManager.Instance.mapGenerator.seed = saveData.mapSeed;
                GameManager.Instance.mapGenerator.GenerateMap();
            }
            else
            {
                Debug.LogWarning("Loaded save has no valid map seed; keeping current map instead of generating a random new seed.");
            }
        }

        // Load generators
        if (GeneratorManager.Instance != null && saveData.generators != null)
        {
            GeneratorManager.Instance.Initialize(); // Clear existing generators

            foreach (var genData in saveData.generators)
            {
                if (System.Enum.TryParse(genData.type, out MapGenerator.GeneratorType type))
                {
                    Vector2Int pos = new Vector2Int(genData.posX, genData.posY);
                    MapGenerator.BiomeType biome = MapGenerator.BiomeType.Plains;
                    System.Enum.TryParse(genData.biome, out biome);
                    GeneratorManager.Instance.LoadGenerator(type, pos, genData.tier, biome, genData.generatorId);
                }
            }
        }
        
        // Load research
        if (ResearchManager.Instance != null)
        {
            if (saveData.researchNodes != null)
            {
                foreach (var resData in saveData.researchNodes)
                {
                    ResearchNode node = ResearchManager.Instance.GetNode(resData.nodeId);
                    if (node != null)
                    {
                        node.isUnlocked = resData.isUnlocked;
                        node.isResearching = resData.isResearching;
                        node.researchProgress = resData.researchProgress;
                    }
                }
            }

            if (saveData.batteryNodes != null)
            {
                foreach (var batData in saveData.batteryNodes)
                {
                    BatteryNode node = ResearchManager.Instance.GetBatteryNode(batData.tier);
                    if (node != null)
                    {
                        node.isUnlocked = batData.isUnlocked;
                        node.isResearching = batData.isResearching;
                        node.researchProgress = batData.researchProgress;
                    }
                }
            }

            // Re-populate the active research lists from restored node states
            ResearchManager.Instance.ResumeAllActiveResearch();
        }

        if (PDAProgressionSystem.Instance != null)
        {
            List<string> unlockedEntryIds = new List<string>();

            if (saveData.pdaEntries != null)
            {
                foreach (PDAEntryUnlockSaveData entry in saveData.pdaEntries)
                {
                    if (entry != null && entry.isUnlocked && !string.IsNullOrWhiteSpace(entry.entryId))
                    {
                        unlockedEntryIds.Add(entry.entryId);
                    }
                }
            }

            PDAProgressionSystem.Instance.SetUnlockedEntries(unlockedEntryIds);
            PDAProgressionSystem.Instance.SyncWithCurrentGameState();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyDifficultyToSystems();
        }

    }

    /// <summary>
    /// Returns true if a save file exists on disk.
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }

    /// <summary>
    /// Deletes the save file from disk if it exists.
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log("Save file deleted.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Returns the full path to the save file (useful for debug displays in the settings menu).
    /// </summary>
    public string GetSaveFilePath()
    {
        return saveFilePath;
    }
}
