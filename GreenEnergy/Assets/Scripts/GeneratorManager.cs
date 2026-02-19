using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Generator
{
    public string generatorId;
    public MapGenerator.GeneratorType type;
    public int tier = 1; // 1-10
    public Vector2Int position;
    public float baseEnergyOutput;
    public float currentOutput;
    public MapGenerator.BiomeType biome;
    
    // Wind and Tidal specific
    public float windCycleTimer = 0f;
    public float tidalCycleTimer = 0f;
    public bool isProducing = true;
    
    public Generator(MapGenerator.GeneratorType genType, Vector2Int pos, int genTier, MapGenerator.BiomeType genBiome)
    {
        generatorId = System.Guid.NewGuid().ToString();
        type = genType;
        position = pos;
        tier = genTier;
        biome = genBiome;
        baseEnergyOutput = CalculateBaseOutput();
        
        // Randomize initial timers for wind and tidal
        windCycleTimer = Random.Range(0f, 30f);
        tidalCycleTimer = Random.Range(0f, 60f);
    }

    public float CalculateBaseOutput()
    {
        // Base output increases with tier
        float tierMultiplier = 1f + (tier - 1) * 0.1f; // +10% per tier
        
        float baseValue = 0f;
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar:
                baseValue = 5f;
                break;
            case MapGenerator.GeneratorType.Wind:
                baseValue = 4f;
                break;
            case MapGenerator.GeneratorType.Hydroelectric:
                baseValue = 7.5f; // Higher but limited placement
                break;
            case MapGenerator.GeneratorType.Tidal:
                baseValue = 6f;
                break;
            case MapGenerator.GeneratorType.Nuclear:
                baseValue = 25f; // Expensive but powerful
                break;
        }
        
        return baseValue * tierMultiplier;
    }

    public float GetOutput(float timeOfDay)
    {
        float output = baseEnergyOutput;
        
        // Apply time-of-day modifiers
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar:
                // Solar only works during day (6 AM to 6 PM)
                if (timeOfDay < 0.25f || timeOfDay > 0.75f)
                {
                    output = 0f; // Night time
                }
                else
                {
                    // Peak at noon (0.5), reduced at dawn/dusk
                    float dayProgress = (timeOfDay - 0.25f) / 0.5f; // 0 to 1
                    float efficiency = Mathf.Sin(dayProgress * Mathf.PI); // Sine curve
                    output *= efficiency;
                }
                break;
            
            case MapGenerator.GeneratorType.Wind:
                // Wind has random fluctuations
                output *= isProducing ? 1f : 0.1f; // 10% when not producing
                break;
            
            case MapGenerator.GeneratorType.Tidal:
                // Tidal has cycles
                output *= isProducing ? 1f : 0.25f; // 25% at low tide
                break;
            
            case MapGenerator.GeneratorType.Hydroelectric:
                // Constant output
                break;
            
            case MapGenerator.GeneratorType.Nuclear:
                // Constant output
                break;
        }
        
        return output;
    }

    public void UpdateCycles(float deltaTime)
    {
        // Update wind cycle (random periods between 10-30 seconds)
        if (type == MapGenerator.GeneratorType.Wind)
        {
            windCycleTimer += deltaTime;
            if (windCycleTimer >= 30f)
            {
                windCycleTimer = 0f;
                isProducing = Random.value > 0.5f; // 50% chance to produce
            }
        }
        
        // Update tidal cycle (6 hour cycles = 1.5 minutes in game time)
        if (type == MapGenerator.GeneratorType.Tidal)
        {
            tidalCycleTimer += deltaTime;
            if (tidalCycleTimer >= 90f) // 1.5 minutes
            {
                tidalCycleTimer = 0f;
                isProducing = !isProducing; // Toggle
            }
        }
    }
}

public class GeneratorManager : MonoBehaviour
{
    public static GeneratorManager Instance { get; private set; }
    
    [Header("References")]
    public MapGenerator mapGenerator;
    public PDASystem pdaSystem;
    
    [Header("Generator Prefabs")]
    public GameObject solarPrefab;
    public GameObject windPrefab;
    public GameObject hydroPrefab;
    public GameObject tidalPrefab;
    public GameObject nuclearPrefab;
    
    private List<Generator> activeGenerators = new List<Generator>();
    private Dictionary<Vector2Int, GameObject> generatorVisuals = new Dictionary<Vector2Int, GameObject>();
    
    // Track milestones
    private Dictionary<MapGenerator.GeneratorType, int> generatorCounts = new Dictionary<MapGenerator.GeneratorType, int>();

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
    }

    public void Initialize()
    {
        activeGenerators.Clear();
        generatorVisuals.Clear();
        
        foreach (MapGenerator.GeneratorType type in System.Enum.GetValues(typeof(MapGenerator.GeneratorType)))
        {
            generatorCounts[type] = 0;
        }
    }

    private void Update()
    {
        // Update all generator cycles
        float deltaTime = Time.deltaTime;
        if (GameManager.Instance != null)
        {
            deltaTime *= GetSpeedMultiplier(GameManager.Instance.currentTimeSpeed);
        }
        
        foreach (var generator in activeGenerators)
        {
            generator.UpdateCycles(deltaTime);
        }
    }

    private float GetSpeedMultiplier(GameManager.TimeSpeed speed)
    {
        switch (speed)
        {
            case GameManager.TimeSpeed.Paused: return 0f;
            case GameManager.TimeSpeed.Normal: return 1f;
            case GameManager.TimeSpeed.Speed2x: return 2f;
            case GameManager.TimeSpeed.Speed5x: return 5f;
            case GameManager.TimeSpeed.Speed10x: return 10f;
            default: return 1f;
        }
    }

    public bool PlaceGenerator(MapGenerator.GeneratorType type, Vector2Int position, int tier)
    {
        // Check if tile is occupied
        if (generatorVisuals.ContainsKey(position))
        {
            Debug.Log("Tile already occupied!");
            return false;
        }
        
        // Check if generator type can be placed on this biome
        if (!mapGenerator.CanPlaceGeneratorType(type, position.x, position.y))
        {
            Debug.Log($"Cannot place {type} generator on this terrain!");
            return false;
        }
        
        // Check for hydro limit (only 10 total)
        if (type == MapGenerator.GeneratorType.Hydroelectric)
        {
            int hydroCount = activeGenerators.Count(g => g.type == MapGenerator.GeneratorType.Hydroelectric);
            if (hydroCount >= 10)
            {
                Debug.Log("Maximum hydroelectric dams reached (10)!");
                return false;
            }
        }
        
        // Get placement cost
        float cost = GetPlacementCost(type, tier);
        
        // Try to consume energy
        if (!GameManager.Instance.TryConsumeEnergy(cost))
        {
            Debug.Log($"Not enough energy! Need {cost}, have {GameManager.Instance.currentEnergy}");
            return false;
        }
        
        // Create generator
        MapGenerator.BiomeType biome = mapGenerator.GetBiomeAt(position.x, position.y);
        Generator newGenerator = new Generator(type, position, tier, biome);
        activeGenerators.Add(newGenerator);
        
        // Spawn visual
        SpawnGeneratorVisual(newGenerator);
        
        // Update count and check milestones
        generatorCounts[type]++;
        CheckGeneratorMilestones(type);
        
        Debug.Log($"Placed {type} generator at {position}, Tier {tier}");
        return true;
    }

    public bool UpgradeGenerator(Vector2Int position)
    {
        Generator generator = activeGenerators.FirstOrDefault(g => g.position == position);
        if (generator == null)
        {
            Debug.Log("No generator at this position!");
            return false;
        }
        
        if (generator.tier >= 10)
        {
            Debug.Log("Generator already at max tier!");
            return false;
        }
        
        float cost = GetUpgradeCost(generator.type, generator.tier + 1);
        
        if (!GameManager.Instance.TryConsumeEnergy(cost))
        {
            Debug.Log($"Not enough energy to upgrade! Need {cost}");
            return false;
        }
        
        generator.tier++;
        generator.baseEnergyOutput = generator.CalculateBaseOutput();
        
        // Trigger PDA entry for upgrade
        if (pdaSystem != null)
        {
            pdaSystem.TriggerEntry($"{generator.type}_Tier{generator.tier}");
        }
        
        Debug.Log($"Upgraded {generator.type} to Tier {generator.tier}");
        return true;
    }

    private void SpawnGeneratorVisual(Generator generator)
    {
        GameObject prefab = GetPrefabForType(generator.type);
        if (prefab == null) return;
        
        Vector3 worldPos = new Vector3(generator.position.x, generator.position.y, 0);
        GameObject visual = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        generatorVisuals[generator.position] = visual;
    }

    private GameObject GetPrefabForType(MapGenerator.GeneratorType type)
    {
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar: return solarPrefab;
            case MapGenerator.GeneratorType.Wind: return windPrefab;
            case MapGenerator.GeneratorType.Hydroelectric: return hydroPrefab;
            case MapGenerator.GeneratorType.Tidal: return tidalPrefab;
            case MapGenerator.GeneratorType.Nuclear: return nuclearPrefab;
            default: return null;
        }
    }

    public float GetTotalEnergyProduction(float timeOfDay)
    {
        float total = 0f;
        foreach (var generator in activeGenerators)
        {
            float output = generator.GetOutput(timeOfDay);
            
            // Apply biome efficiency multiplier
            float efficiency = mapGenerator.GetBiomeEfficiencyMultiplier(generator.type, generator.biome);
            total += output * efficiency;
        }
        return total;
    }

    public float GetPlacementCost(MapGenerator.GeneratorType type, int tier)
    {
        float baseCost = GetBasePlacementCost(type);
        float tierMultiplier = 1f + (tier - 1) * 0.3f; // +30% per tier
        return baseCost * tierMultiplier;
    }

    private float GetBasePlacementCost(MapGenerator.GeneratorType type)
    {
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar: return 50f;
            case MapGenerator.GeneratorType.Wind: return 75f;
            case MapGenerator.GeneratorType.Hydroelectric: return 150f;
            case MapGenerator.GeneratorType.Tidal: return 100f;
            case MapGenerator.GeneratorType.Nuclear: return 500f;
            default: return 50f;
        }
    }

    public float GetUpgradeCost(MapGenerator.GeneratorType type, int targetTier)
    {
        float baseCost = GetBasePlacementCost(type);
        float tierCost = baseCost * targetTier * 0.5f;
        return tierCost;
    }

    private void CheckGeneratorMilestones(MapGenerator.GeneratorType type)
    {
        int count = generatorCounts[type];
        
        // Check milestones: 25, 50, 75, 100
        if (count == 25 || count == 50 || count == 75 || count == 100)
        {
            if (pdaSystem != null)
            {
                pdaSystem.TriggerEntry($"{type}_Milestone{count}");
            }
        }
    }

    public List<Generator> GetAllGenerators()
    {
        return new List<Generator>(activeGenerators);
    }

    public Generator GetGeneratorAt(Vector2Int position)
    {
        return activeGenerators.FirstOrDefault(g => g.position == position);
    }
}
