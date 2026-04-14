using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single placed energy generator on the map.
/// Tracks its type, tier, position, biome, and time-varying production state.
/// </summary>
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

    /// <summary>
    /// Computes the base energy output for this generator's type and tier.
    /// Called on construction and again whenever the difficulty profile changes.
    /// </summary>
    public float CalculateBaseOutput()
    {
        // Base output increases with tier
        float tierMultiplier = 1f + (tier - 1) * 0.08f; // +8% per tier
        DifficultyBalanceProfile profile = GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficultyProfile
            : DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        
        float baseValue = 0f;
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar:
                baseValue = 4.5f;
                break;
            case MapGenerator.GeneratorType.Wind:
                baseValue = 4.5f;
                break;
            case MapGenerator.GeneratorType.Hydroelectric:
                baseValue = 8.5f; // Higher but limited placement
                break;
            case MapGenerator.GeneratorType.Tidal:
                baseValue = 6.5f;
                break;
            case MapGenerator.GeneratorType.Nuclear:
                baseValue = 20f; // Expensive but powerful
                break;
        }
        
        return baseValue * tierMultiplier * profile.generatorOutputMultiplier;
    }

    /// <summary>
    /// Returns the actual energy output this frame based on time of day and production cycle state.
    /// Solar follows a sine curve peaking at noon; wind and tidal use on/off cycles.
    /// </summary>
    /// <param name="timeOfDay">Current time as a 0–1 fraction (0=midnight, 0.5=noon)</param>
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

    /// <summary>
    /// Advances wind and tidal production cycles.
    /// Wind toggles randomly every 30 real-seconds; tidal flips every 90 real-seconds (≈ 6 in-game hours).
    /// Should be called from GeneratorManager.Update() with a speed-adjusted deltaTime.
    /// </summary>
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

/// <summary>
/// Manages all placed energy generators: placement, upgrades, production calculation,
/// visual spawning, and milestone tracking. Singleton accessed via GeneratorManager.Instance.
/// </summary>
public class GeneratorManager : MonoBehaviour
{
    public static GeneratorManager Instance { get; private set; }
    
    [Header("References")]
    public MapGenerator mapGenerator;
    
    [Header("Generator Prefabs")]
    public GameObject solarPrefab;
    public GameObject windPrefab;
    public GameObject hydroPrefab;
    public GameObject tidalPrefab;
    public GameObject nuclearPrefab;
    
    private readonly List<Generator> generators = new List<Generator>();
    private readonly Dictionary<Vector2Int, GameObject> visualsByPosition = new Dictionary<Vector2Int, GameObject>();
    
    // Track milestones
    private readonly Dictionary<MapGenerator.GeneratorType, int> generatorCountByType = new Dictionary<MapGenerator.GeneratorType, int>();

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

    /// <summary>
    /// Clears all placed generators and their visuals, resets milestone counters.
    /// Called at the start of a new game or before loading a save.
    /// </summary>
    public void Initialize()
    {
        foreach (var visualPair in visualsByPosition)
        {
            if (visualPair.Value != null)
            {
                Destroy(visualPair.Value);
            }
        }

        // Safety clear if visuals were instantiated under this manager but not tracked.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        generators.Clear();
        visualsByPosition.Clear();
        
        foreach (MapGenerator.GeneratorType type in System.Enum.GetValues(typeof(MapGenerator.GeneratorType)))
        {
            generatorCountByType[type] = 0;
        }
    }

    /// <summary>
    /// Recalculates base output for every placed generator using the new difficulty profile.
    /// Called whenever difficulty changes so existing generators reflect the updated multiplier.
    /// </summary>
    public void ApplyDifficultyProfile(DifficultyBalanceProfile profile)
    {
        for (int i = 0; i < generators.Count; i++)
        {
            generators[i].baseEnergyOutput = generators[i].CalculateBaseOutput();
        }
    }

    private void Update()
    {
        // Update all generator cycles
        float deltaTime = Time.deltaTime;
        if (GameManager.Instance != null)
        {
            deltaTime *= TimeSystemUtils.GetSpeedMultiplier(GameManager.Instance.currentTimeSpeed);
        }
        
        foreach (var generator in generators)
        {
            generator.UpdateCycles(deltaTime);
        }
    }

    /// <summary>
    /// Adds a generator directly from save data without consuming energy or checking placement rules.
    /// </summary>
    public void LoadGenerator(MapGenerator.GeneratorType type, Vector2Int position, int tier,
                              MapGenerator.BiomeType biome, string generatorId)
    {
        if (visualsByPosition.ContainsKey(position)) return; // already loaded
        Generator generatorData = new Generator(type, position, tier, biome);
        generatorData.generatorId = generatorId;
        generators.Add(generatorData);
        SpawnGeneratorVisual(generatorData);
        generatorCountByType[type]++;
        CheckGeneratorMilestones(type);
    }

    /// <summary>
    /// Attempts to place a generator at the given grid position.
    /// Checks biome compatibility, tile vacancy, Hydroelectric cap, and energy cost.
    /// Returns false and logs the reason if placement is blocked.
    /// </summary>
    public bool PlaceGenerator(MapGenerator.GeneratorType type, Vector2Int position, int tier)
    {
        // Check if tile is occupied
        if (visualsByPosition.ContainsKey(position))
        {
            Debug.Log("Tile already occupied!");
            return false;
        }
        
        // Check if generator type can be placed on this biome
        if (mapGenerator == null)
        {
            Debug.LogError("GeneratorManager: mapGenerator is not assigned!");
            return false;
        }
        if (!mapGenerator.CanPlaceGeneratorType(type, position.x, position.y))
        {
            MapGenerator.BiomeType targetBiome = mapGenerator.GetBiomeAt(position.x, position.y);
            Debug.Log($"Cannot place {type} generator on biome {targetBiome}. Placement blocked.");
            return false;
        }
        
        // Check for hydro limit (only 10 total)
        if (type == MapGenerator.GeneratorType.Hydroelectric)
        {
            int hydroCount = generators.Count(g => g.type == MapGenerator.GeneratorType.Hydroelectric);
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
        generators.Add(newGenerator);
        
        // Spawn visual
        SpawnGeneratorVisual(newGenerator);
        
        // Update count and check milestones
        generatorCountByType[type]++;
        CheckGeneratorMilestones(type);
        
        Debug.Log($"Placed {type} generator at {position}, Tier {tier}");
        return true;
    }

    /// <summary>
    /// Upgrades the generator at the given grid position by one tier (up to tier 10).
    /// Deducts energy cost and recalculates base output.
    /// </summary>
    public bool UpgradeGenerator(Vector2Int position)
    {
        Generator generator = generators.FirstOrDefault(g => g.position == position);
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
        
        Debug.Log($"Upgraded {generator.type} to Tier {generator.tier}");
        return true;
    }

    /// <summary>Instantiates the prefab for the generator's type at its grid position, parented to this transform.</summary>
    private void SpawnGeneratorVisual(Generator generator)
    {
        GameObject prefab = GetPrefabForType(generator.type);
        if (prefab == null) return;
        
        Vector3 worldPos = new Vector3(generator.position.x, generator.position.y, 0);
        GameObject visual = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        visualsByPosition[generator.position] = visual;
    }

    /// <summary>Returns the inspector-assigned prefab for the given generator type, or null if not set.</summary>
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

    /// <summary>
    /// Returns the combined energy output of all placed generators at the given time of day,
    /// with each generator's output multiplied by its biome efficiency.
    /// </summary>
    public float GetTotalEnergyProduction(float timeOfDay)
    {
        if (mapGenerator == null) return 0f;
        float total = 0f;
        foreach (var generator in generators)
        {
            float output = generator.GetOutput(timeOfDay);
            float efficiency = mapGenerator.GetBiomeEfficiencyMultiplier(generator.type, generator.biome);
            total += output * efficiency;
        }
        return total;
    }

    /// <summary>
    /// Returns the energy cost to place a new generator of the given type and tier.
    /// Cost scales by +30% per tier above tier 1.
    /// </summary>
    public float GetPlacementCost(MapGenerator.GeneratorType type, int tier)
    {
        float baseCost = GetBasePlacementCost(type);
        float tierMultiplier = 1f + (tier - 1) * 0.3f; // +30% per tier
        return baseCost * tierMultiplier;
    }

    /// <summary>Returns the flat tier-1 placement cost for each generator type (used as the base for tier scaling).</summary>
    private float GetBasePlacementCost(MapGenerator.GeneratorType type)
    {
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar: return 80f;
            case MapGenerator.GeneratorType.Wind: return 120f;
            case MapGenerator.GeneratorType.Hydroelectric: return 240f;
            case MapGenerator.GeneratorType.Tidal: return 180f;
            case MapGenerator.GeneratorType.Nuclear: return 900f;
            default: return 50f;
        }
    }

    /// <summary>
    /// Returns the energy cost to upgrade a generator to targetTier.
    /// Upgrade cost = 50% of the base placement cost × target tier number.
    /// </summary>
    public float GetUpgradeCost(MapGenerator.GeneratorType type, int targetTier)
    {
        float baseCost = GetBasePlacementCost(type);
        float tierCost = baseCost * targetTier * 0.5f;
        return tierCost;
    }

    /// <summary>
    /// Fires a PDA milestone event if the generator count for <paramref name="type"/>
    /// has just reached 25, 50, 75, or 100.
    /// </summary>
    private void CheckGeneratorMilestones(MapGenerator.GeneratorType type)
    {
        int count = generatorCountByType[type];
        
        // Check milestones: 25, 50, 75, 100
        if (count == 25 || count == 50 || count == 75 || count == 100)
        {
            if (PDAProgressionSystem.Instance != null)
            {
                PDAProgressionSystem.Instance.HandleGeneratorMilestone(type, count);
            }

        }
    }

    /// <summary>Returns the total number of placed generators of the given type.</summary>
    public int GetGeneratorCount(MapGenerator.GeneratorType type)
    {
        return generatorCountByType.TryGetValue(type, out int count) ? count : 0;
    }

    /// <summary>Returns a snapshot copy of all placed generators.</summary>
    public List<Generator> GetAllGenerators()
    {
        return new List<Generator>(generators);
    }

    /// <summary>Returns the generator at the given grid position, or null if none exists there.</summary>
    public Generator GetGeneratorAt(Vector2Int position)
    {
        return generators.FirstOrDefault(g => g.position == position);
    }
}
