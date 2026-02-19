using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int currentDay;
    public float timeOfDay;
    public float carbonLevel;
    public float currentEnergy;
    public float maxEnergyStorage;
    public int mapSeed;
    public string timeSpeed;
    
    public List<GeneratorSaveData> generators;
    public List<ResearchSaveData> researchNodes;
    public List<BatterySaveData> batteryNodes;
    public List<PDAEntrySaveData> pdaEntries;
}

[System.Serializable]
public class GeneratorSaveData
{
    public string generatorId;
    public string type;
    public int tier;
    public int posX;
    public int posY;
    public string biome;
}

[System.Serializable]
public class ResearchSaveData
{
    public string nodeId;
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress;
}

[System.Serializable]
public class BatterySaveData
{
    public int tier;
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress;
}

[System.Serializable]
public class PDAEntrySaveData
{
    public string entryId;
    public bool isUnlocked;
    public bool hasBeenViewed;
}

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
        }
        
        // Save map seed
        if (GameManager.Instance.mapGenerator != null)
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
        
        // Save PDA entries
        saveData.pdaEntries = new List<PDAEntrySaveData>();
        if (PDASystem.Instance != null)
        {
            foreach (var entry in PDASystem.Instance.GetAllEntries())
            {
                PDAEntrySaveData pdaData = new PDAEntrySaveData
                {
                    entryId = entry.entryId,
                    isUnlocked = entry.isUnlocked,
                    hasBeenViewed = entry.hasBeenViewed
                };
                saveData.pdaEntries.Add(pdaData);
            }
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

    private void ApplyLoadedData(GameSaveData saveData)
    {
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
        if (GameManager.Instance.mapGenerator != null)
        {
            GameManager.Instance.mapGenerator.seed = saveData.mapSeed;
            GameManager.Instance.mapGenerator.GenerateMap();
        }
        
        // Load generators
        if (GeneratorManager.Instance != null && saveData.generators != null)
        {
            GeneratorManager.Instance.Initialize(); // Clear existing
            
            foreach (var genData in saveData.generators)
            {
                if (System.Enum.TryParse(genData.type, out MapGenerator.GeneratorType type))
                {
                    Vector2Int pos = new Vector2Int(genData.posX, genData.posY);
                    // Recreate generator (simplified - doesn't deduct energy)
                    MapGenerator.BiomeType biome = MapGenerator.BiomeType.Plains;
                    System.Enum.TryParse(genData.biome, out biome);
                    
                    Generator gen = new Generator(type, pos, genData.tier, biome);
                    gen.generatorId = genData.generatorId;
                    // Add to manager's list manually (you'll need to expose this)
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
        }
        
        // Load PDA entries
        if (PDASystem.Instance != null && saveData.pdaEntries != null)
        {
            foreach (var pdaData in saveData.pdaEntries)
            {
                // You'll need to expose a way to set PDA entry states
                // For now, we'll trigger unlocked ones
                if (pdaData.isUnlocked)
                {
                    PDASystem.Instance.TriggerEntry(pdaData.entryId);
                }
            }
        }
    }

    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }

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

    public string GetSaveFilePath()
    {
        return saveFilePath;
    }
}
