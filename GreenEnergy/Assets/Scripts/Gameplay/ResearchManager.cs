using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents one node in the generator research tree (e.g. "Solar Tier 3").
/// Tracks unlock state, in-progress research, and the cost/time for that tier.
/// </summary>
[System.Serializable]
public class ResearchNode
{
    public string nodeId;
    public MapGenerator.GeneratorType generatorType;
    public int tier; // 1-10
    public float energyCost;
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress; // 0-1
    public float researchTime; // seconds to complete
    
    public ResearchNode(MapGenerator.GeneratorType type, int nodeTier)
    {
        nodeId = $"{type}_Tier{nodeTier}";
        generatorType = type;
        tier = nodeTier;
        ApplyDifficultyProfile(GetActiveDifficultyProfile());
        isUnlocked = false;
        isResearching = false;
        researchProgress = 0f;
    }

    /// <summary>
    /// Recalculates cost and research time from the given difficulty profile.
    /// Cost uses exponential scaling: baseCost × (costMultiplier ^ (tier-1)).
    /// </summary>
    public void ApplyDifficultyProfile(DifficultyBalanceProfile profile)
    {
        // Cost increases exponentially: tier 1 = base, tier 2 = base × mult, tier 3 = base × mult², etc.
        float baseCost = GetBaseCost(generatorType);
        float tierMultiplier = Mathf.Pow(profile.researchCostMultiplier, tier - 1);
        energyCost = baseCost * tierMultiplier;
        researchTime = CalculateResearchTime(profile);
    }

    private float GetBaseCost(MapGenerator.GeneratorType type)
    {
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar: return 100f;
            case MapGenerator.GeneratorType.Wind: return 180f;
            case MapGenerator.GeneratorType.Hydroelectric: return 340f;
            case MapGenerator.GeneratorType.Tidal: return 280f;
            case MapGenerator.GeneratorType.Nuclear: return 1200f;
            case MapGenerator.GeneratorType.Battery: return 220f;
            default: return 100f;
        }
    }

    private float CalculateResearchTime(DifficultyBalanceProfile profile)
    {
        // Research time increases with tier
        return (15f + (tier * 6f)) * profile.researchTimeMultiplier;
    }

    private DifficultyBalanceProfile GetActiveDifficultyProfile()
    {
        return GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficultyProfile
            : DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
    }
}

/// <summary>
/// Represents one tier of the battery storage research tree.
/// Unlocking a battery tier permanently increases the maximum energy storage.
/// </summary>
[System.Serializable]
public class BatteryNode
{
    public int tier; // 1-10
    public float energyCost;
    public float storageIncrease; // How much storage this adds
    public bool isUnlocked;
    public bool isResearching;
    public float researchProgress;
    public float researchTime;
    
    public BatteryNode(int batteryTier)
    {
        tier = batteryTier;
        ApplyDifficultyProfile(GetActiveDifficultyProfile());
        storageIncrease = 1000f * tier; // +1000 per tier
        isUnlocked = false;
        isResearching = false;
        researchProgress = 0f;
    }

    /// <summary>
    /// Recalculates cost and research time from the given difficulty profile.
    /// Base cost is 220 scaled exponentially per tier; time starts at 24s and grows 6s per tier.
    /// </summary>
    public void ApplyDifficultyProfile(DifficultyBalanceProfile profile)
    {
        energyCost = 220f * Mathf.Pow(profile.researchCostMultiplier, tier - 1); // 220 = battery base cost
        researchTime = (18f + (tier * 6f)) * profile.researchTimeMultiplier;      // 24s at tier 1, +6s per tier
    }

    private DifficultyBalanceProfile GetActiveDifficultyProfile()
    {
        return GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficultyProfile
            : DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
    }
}

/// <summary>
/// Manages the full research tree: 50 generator nodes (5 types × 10 tiers) plus 10 battery tiers.
/// Handles starting research, advancing progress each frame, and completing nodes.
/// Singleton accessed via ResearchManager.Instance.
/// </summary>
public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }
    
    [Header("References")]
    [Header("Research Trees")]
    public Dictionary<string, ResearchNode> researchNodes = new Dictionary<string, ResearchNode>();
    public List<BatteryNode> batteryNodes = new List<BatteryNode>();
    
    private readonly List<ResearchNode> activeResearchNodes = new List<ResearchNode>();
    private readonly List<BatteryNode> activeBatteryResearchNodes = new List<BatteryNode>();

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
    /// Builds the full research tree from scratch and unlocks Solar Tier 1 as the starting node.
    /// Called at the start of a new game.
    /// </summary>
    public void Initialize()
    {
        CreateResearchTrees();
        activeResearchNodes.Clear();
        activeBatteryResearchNodes.Clear();
        ApplyDifficultyProfile(GetActiveDifficultyProfile());

        // Solar tier 1 starts unlocked
        string solarT1 = "Solar_Tier1";
        if (researchNodes.ContainsKey(solarT1))
        {
            researchNodes[solarT1].isUnlocked = true;
        }
    }

    /// <summary>
    /// After loading save data, call this to re-populate the active research lists
    /// from any nodes that have isResearching == true.
    /// </summary>
    public void ResumeAllActiveResearch()
    {
        activeResearchNodes.Clear();
        activeBatteryResearchNodes.Clear();

        foreach (var node in researchNodes.Values)
        {
            if (node.isResearching)
                activeResearchNodes.Add(node);
        }

        foreach (var node in batteryNodes)
        {
            if (node.isResearching)
                activeBatteryResearchNodes.Add(node);
        }
    }

    /// <summary>
    /// Builds all 60 research nodes from scratch: 10 tiers for each of the 5 generator types
    /// (50 nodes) plus 10 battery tiers. Clears any existing nodes first.
    /// </summary>
    private void CreateResearchTrees()
    {
        researchNodes.Clear();
        batteryNodes.Clear();
        
        // Create 10 tiers for each generator type
        MapGenerator.GeneratorType[] types = new MapGenerator.GeneratorType[]
        {
            MapGenerator.GeneratorType.Solar,
            MapGenerator.GeneratorType.Wind,
            MapGenerator.GeneratorType.Hydroelectric,
            MapGenerator.GeneratorType.Tidal,
            MapGenerator.GeneratorType.Nuclear
        };
        
        foreach (var type in types)
        {
            for (int tier = 1; tier <= 10; tier++)
            {
                ResearchNode node = new ResearchNode(type, tier);
                researchNodes[node.nodeId] = node;
            }
        }
        
        // Create 10 battery tiers
        for (int tier = 1; tier <= 10; tier++)
        {
            batteryNodes.Add(new BatteryNode(tier));
        }
    }

    /// <summary>
    /// Pushes the given difficulty profile to every research and battery node so costs and times update.
    /// </summary>
    public void ApplyDifficultyProfile(DifficultyBalanceProfile profile)
    {
        foreach (var node in researchNodes.Values)
        {
            node.ApplyDifficultyProfile(profile);
        }

        foreach (var node in batteryNodes)
        {
            node.ApplyDifficultyProfile(profile);
        }
    }

    private DifficultyBalanceProfile GetActiveDifficultyProfile()
    {
        return GameManager.Instance != null
            ? GameManager.Instance.CurrentDifficultyProfile
            : DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
    }

    private void Update()
    {
        // Advance generator research and battery research independently each frame.
        // Both lists use the same generic helper to avoid duplicating progress logic.
        float deltaTime = GetResearchDeltaTime();
        if (deltaTime <= 0f)
        {
            return;
        }

        UpdateResearchBatch(
            activeResearchNodes,
            node => node.researchProgress,
            (node, progress) => node.researchProgress = progress,
            node => node.researchTime,
            CompleteResearch);

        UpdateResearchBatch(
            activeBatteryResearchNodes,
            node => node.researchProgress,
            (node, progress) => node.researchProgress = progress,
            node => node.researchTime,
            CompleteBatteryResearch);
    }

    /// <summary>
    /// Returns the speed-adjusted delta time to apply to research progress this frame.
    /// Returns 0 when the game is paused or no GameManager exists, so research
    /// freezes correctly without needing extra guards throughout Update().
    /// </summary>
    private float GetResearchDeltaTime()
    {
        if (GameManager.Instance == null)
        {
            return 0f;
        }

        float speedMultiplier = TimeSystemUtils.GetSpeedMultiplier(GameManager.Instance.currentTimeSpeed);
        if (speedMultiplier <= 0f)
        {
            // Time is paused — freeze all research progress
            return 0f;
        }

        return Time.deltaTime * speedMultiplier;
    }

    /// <summary>
    /// Generic helper that advances a list of in-progress nodes each frame.
    /// Uses lambdas to read/write progress and research time so the same logic
    /// works for both ResearchNode and BatteryNode without duplication.
    /// </summary>
    private void UpdateResearchBatch<TNode>(
        List<TNode> activeNodes,
        System.Func<TNode, float> getProgress,
        System.Action<TNode, float> setProgress,
        System.Func<TNode, float> getResearchTime,
        System.Action<TNode> onCompleted)
    {
        if (activeNodes.Count == 0)
        {
            return;
        }

        float deltaTime = GetResearchDeltaTime();
        if (deltaTime <= 0f)
        {
            return;
        }

        for (int i = activeNodes.Count - 1; i >= 0; i--)
        {
            TNode node = activeNodes[i];
            float progress = getProgress(node) + (deltaTime / getResearchTime(node));
            setProgress(node, progress);

            if (progress >= 1f)
            {
                onCompleted(node);
                activeNodes.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Starts researching the given node (e.g. "Wind_Tier2").
    /// Fails if the node is already unlocked, already in progress, the previous tier isn't unlocked,
    /// or there isn't enough energy.
    /// </summary>
    public bool StartResearch(string nodeId)
    {
        EnsureResearchTreesInitialized();

        if (!researchNodes.ContainsKey(nodeId))
        {
            Debug.LogError($"Research node {nodeId} not found!");
            return false;
        }
        
        ResearchNode node = researchNodes[nodeId];
        
        if (node.isUnlocked)
        {
            Debug.Log("Already unlocked!");
            return false;
        }
        
        if (node.isResearching)
        {
            Debug.Log("Already researching!");
            return false;
        }
        
        // Check if previous tier is unlocked (except tier 1)
        if (node.tier > 1)
        {
            string prevNodeId = $"{node.generatorType}_Tier{node.tier - 1}";
            if (!researchNodes[prevNodeId].isUnlocked)
            {
                Debug.Log("Must unlock previous tier first!");
                return false;
            }
        }
        
        // Try to consume energy
        if (!GameManager.Instance.TryConsumeEnergy(node.energyCost))
        {
            Debug.Log($"Not enough energy! Need {node.energyCost}");
            return false;
        }
        
        node.isResearching = true;
        node.researchProgress = 0f;
        activeResearchNodes.Add(node);
        
        Debug.Log($"Started researching {nodeId}");
        return true;
    }

    /// <summary>
    /// Starts researching the given battery tier (1–10).
    /// Same preconditions as StartResearch: previous tier must be unlocked and energy must be available.
    /// </summary>
    public bool StartBatteryResearch(int tier)
    {
        EnsureResearchTreesInitialized();

        if (tier < 1 || tier > 10)
        {
            Debug.LogError($"Invalid battery tier: {tier}");
            return false;
        }

        if (tier - 1 >= batteryNodes.Count)
        {
            Debug.LogError($"Battery tier {tier} is unavailable. Current battery node count: {batteryNodes.Count}.");
            return false;
        }
        
        BatteryNode node = batteryNodes[tier - 1];
        
        if (node.isUnlocked)
        {
            Debug.Log("Battery tier already unlocked!");
            return false;
        }
        
        if (node.isResearching)
        {
            Debug.Log("Already researching this battery tier!");
            return false;
        }
        
        // Check previous tier unlocked (except tier 1)
        if (tier > 1 && !batteryNodes[tier - 2].isUnlocked)
        {
            Debug.Log("Must unlock previous battery tier first!");
            return false;
        }
        
        if (!GameManager.Instance.TryConsumeEnergy(node.energyCost))
        {
            Debug.Log($"Not enough energy! Need {node.energyCost}");
            return false;
        }
        
        node.isResearching = true;
        node.researchProgress = 0f;
        activeBatteryResearchNodes.Add(node);
        
        Debug.Log($"Started researching Battery Tier {tier}");
        return true;
    }

    private void CompleteResearch(ResearchNode node)
    {
        node.isUnlocked = true;
        node.isResearching = false;
        node.researchProgress = 1f;
        
        Debug.Log($"Research complete: {node.nodeId}");

        if (PDAProgressionSystem.Instance != null)
        {
            PDAProgressionSystem.Instance.HandleResearchUnlocked(node.generatorType, node.tier);
        }
        
    }

    private void CompleteBatteryResearch(BatteryNode node)
    {
        node.isUnlocked = true;
        node.isResearching = false;
        node.researchProgress = 1f;
        
        // Add storage capacity
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddEnergyStorage(node.storageIncrease);
        }
        
        Debug.Log($"Battery Tier {node.tier} research complete! +{node.storageIncrease} storage");

        if (PDAProgressionSystem.Instance != null)
        {
            PDAProgressionSystem.Instance.HandleBatteryUnlocked(node.tier);
        }
        
    }

    /// <summary>Returns true if the given generator type and tier has been fully researched.</summary>
    public bool IsUnlocked(MapGenerator.GeneratorType type, int tier)
    {
        string nodeId = $"{type}_Tier{tier}";
        return researchNodes.ContainsKey(nodeId) && researchNodes[nodeId].isUnlocked;
    }

    /// <summary>Returns a snapshot of all currently in-progress generator research nodes.</summary>
    public List<ResearchNode> GetActiveResearch()
    {
        return new List<ResearchNode>(activeResearchNodes);
    }

    /// <summary>Returns a snapshot of all currently in-progress battery research nodes.</summary>
    public List<BatteryNode> GetActiveBatteryResearch()
    {
        return new List<BatteryNode>(activeBatteryResearchNodes);
    }

    /// <summary>Returns the research node for the given ID (e.g. "Solar_Tier2"), or null if not found.</summary>
    public ResearchNode GetNode(string nodeId)
    {
        EnsureResearchTreesInitialized();
        return researchNodes.ContainsKey(nodeId) ? researchNodes[nodeId] : null;
    }

    /// <summary>Returns the battery node for the given tier (1–10), or null if the tier is out of range.</summary>
    public BatteryNode GetBatteryNode(int tier)
    {
        EnsureResearchTreesInitialized();

        if (tier < 1 || tier > 10)
        {
            return null;
        }

        if (tier - 1 < 0 || tier - 1 >= batteryNodes.Count)
        {
            return null;
        }

        return batteryNodes[tier - 1];
    }

    /// <summary>
    /// Returns the lowest locked tier for the given generator type (1–10).
    /// Returns 11 if all tiers are already unlocked.
    /// </summary>
    public int GetNextAvailableTier(MapGenerator.GeneratorType type)
    {
        EnsureResearchTreesInitialized();

        for (int tier = 1; tier <= 10; tier++)
        {
            string nodeId = $"{type}_Tier{tier}";
            if (!researchNodes.ContainsKey(nodeId)) continue;
            if (!researchNodes[nodeId].isUnlocked)
            {
                return tier;
            }
        }

        return 11;
    }

    /// <summary>
    /// Returns the lowest locked battery tier (1–10). Returns 11 if all tiers are unlocked.
    /// </summary>
    public int GetNextAvailableBatteryTier()
    {
        EnsureResearchTreesInitialized();

        for (int tier = 1; tier <= batteryNodes.Count; tier++)
        {
            BatteryNode node = GetBatteryNode(tier);
            if (node != null && !node.isUnlocked)
            {
                return tier;
            }
        }

        return 11;
    }

    /// <summary>Returns true if research is currently in progress for the given generator type and tier.</summary>
    public bool IsResearching(MapGenerator.GeneratorType type, int tier)
    {
        EnsureResearchTreesInitialized();
        ResearchNode node = GetNode($"{type}_Tier{tier}");
        return node != null && node.isResearching;
    }

    /// <summary>Returns true if battery research is currently in progress for the given tier.</summary>
    public bool IsBatteryResearching(int tier)
    {
        EnsureResearchTreesInitialized();
        BatteryNode node = GetBatteryNode(tier);
        return node != null && node.isResearching;
    }

    /// <summary>Returns a snapshot copy of all research nodes keyed by node ID.</summary>
    public Dictionary<string, ResearchNode> GetAllNodes()
    {
        EnsureResearchTreesInitialized();
        return new Dictionary<string, ResearchNode>(researchNodes);
    }

    /// <summary>Returns a snapshot copy of all battery research nodes.</summary>
    public List<BatteryNode> GetAllBatteryNodes()
    {
        EnsureResearchTreesInitialized();
        return new List<BatteryNode>(batteryNodes);
    }

    /// <summary>
    /// Lazy-init guard: builds the research trees if they are empty.
    /// Called at the top of every public getter so tests and editor code can
    /// query the manager safely without an explicit Initialize() call first.
    /// Solar Tier 1 is unlocked by default to match new-game behaviour.
    /// </summary>
    private void EnsureResearchTreesInitialized()
    {
        if (researchNodes.Count == 0 || batteryNodes.Count == 0)
        {
            CreateResearchTrees();

            // Preserve existing behavior: Solar tier 1 starts unlocked.
            string solarT1 = "Solar_Tier1";
            if (researchNodes.ContainsKey(solarT1))
            {
                researchNodes[solarT1].isUnlocked = true;
            }
        }
    }
}
