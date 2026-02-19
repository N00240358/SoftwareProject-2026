using UnityEngine;
using System.Collections.Generic;

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
        energyCost = CalculateEnergyCost();
        researchTime = CalculateResearchTime();
        isUnlocked = false;
        isResearching = false;
        researchProgress = 0f;
    }

    private float CalculateEnergyCost()
    {
        // Cost increases exponentially with tier
        float baseCost = GetBaseCost(generatorType);
        float tierMultiplier = Mathf.Pow(2, tier - 1); // 1, 2, 4, 8, 16, 32, 64, 128, 256, 512
        return baseCost * tierMultiplier;
    }

    private float GetBaseCost(MapGenerator.GeneratorType type)
    {
        switch (type)
        {
            case MapGenerator.GeneratorType.Solar: return 100f;
            case MapGenerator.GeneratorType.Wind: return 150f;
            case MapGenerator.GeneratorType.Hydroelectric: return 300f;
            case MapGenerator.GeneratorType.Tidal: return 250f;
            case MapGenerator.GeneratorType.Nuclear: return 1000f;
            case MapGenerator.GeneratorType.Battery: return 200f;
            default: return 100f;
        }
    }

    private float CalculateResearchTime()
    {
        // Research time increases with tier (10-60 seconds)
        return 10f + (tier * 5f);
    }
}

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
        energyCost = 200f * Mathf.Pow(2, tier - 1);
        storageIncrease = 1000f * tier; // +1000 per tier
        researchTime = 15f + (tier * 5f);
        isUnlocked = false;
        isResearching = false;
        researchProgress = 0f;
    }
}

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }
    
    [Header("References")]
    public PDASystem pdaSystem;
    
    [Header("Research Trees")]
    public Dictionary<string, ResearchNode> researchNodes = new Dictionary<string, ResearchNode>();
    public List<BatteryNode> batteryNodes = new List<BatteryNode>();
    
    private List<ResearchNode> activeResearch = new List<ResearchNode>();
    private List<BatteryNode> activeBatteryResearch = new List<BatteryNode>();

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
        CreateResearchTrees();
        
        // Solar tier 1 starts unlocked
        string solarT1 = "Solar_Tier1";
        if (researchNodes.ContainsKey(solarT1))
        {
            researchNodes[solarT1].isUnlocked = true;
        }
    }

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

    private void Update()
    {
        UpdateActiveResearch();
        UpdateActiveBatteryResearch();
    }

    private void UpdateActiveResearch()
    {
        if (activeResearch.Count == 0) return;
        
        float deltaTime = Time.deltaTime;
        if (GameManager.Instance != null && GameManager.Instance.currentTimeSpeed == GameManager.TimeSpeed.Paused)
        {
            return;
        }
        
        for (int i = activeResearch.Count - 1; i >= 0; i--)
        {
            ResearchNode node = activeResearch[i];
            node.researchProgress += deltaTime / node.researchTime;
            
            if (node.researchProgress >= 1f)
            {
                CompleteResearch(node);
                activeResearch.RemoveAt(i);
            }
        }
    }

    private void UpdateActiveBatteryResearch()
    {
        if (activeBatteryResearch.Count == 0) return;
        
        float deltaTime = Time.deltaTime;
        if (GameManager.Instance != null && GameManager.Instance.currentTimeSpeed == GameManager.TimeSpeed.Paused)
        {
            return;
        }
        
        for (int i = activeBatteryResearch.Count - 1; i >= 0; i--)
        {
            BatteryNode node = activeBatteryResearch[i];
            node.researchProgress += deltaTime / node.researchTime;
            
            if (node.researchProgress >= 1f)
            {
                CompleteBatteryResearch(node);
                activeBatteryResearch.RemoveAt(i);
            }
        }
    }

    public bool StartResearch(string nodeId)
    {
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
        activeResearch.Add(node);
        
        Debug.Log($"Started researching {nodeId}");
        return true;
    }

    public bool StartBatteryResearch(int tier)
    {
        if (tier < 1 || tier > 10)
        {
            Debug.LogError($"Invalid battery tier: {tier}");
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
        activeBatteryResearch.Add(node);
        
        Debug.Log($"Started researching Battery Tier {tier}");
        return true;
    }

    private void CompleteResearch(ResearchNode node)
    {
        node.isUnlocked = true;
        node.isResearching = false;
        node.researchProgress = 1f;
        
        Debug.Log($"Research complete: {node.nodeId}");
        
        // Trigger PDA entry
        if (pdaSystem != null)
        {
            pdaSystem.TriggerEntry(node.nodeId);
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
        
        // Trigger PDA entry
        if (pdaSystem != null)
        {
            pdaSystem.TriggerEntry($"Battery_Tier{node.tier}");
        }
    }

    public bool IsUnlocked(MapGenerator.GeneratorType type, int tier)
    {
        string nodeId = $"{type}_Tier{tier}";
        return researchNodes.ContainsKey(nodeId) && researchNodes[nodeId].isUnlocked;
    }

    public List<ResearchNode> GetActiveResearch()
    {
        return new List<ResearchNode>(activeResearch);
    }

    public List<BatteryNode> GetActiveBatteryResearch()
    {
        return new List<BatteryNode>(activeBatteryResearch);
    }

    public ResearchNode GetNode(string nodeId)
    {
        return researchNodes.ContainsKey(nodeId) ? researchNodes[nodeId] : null;
    }

    public BatteryNode GetBatteryNode(int tier)
    {
        if (tier < 1 || tier > 10) return null;
        return batteryNodes[tier - 1];
    }

    public Dictionary<string, ResearchNode> GetAllNodes()
    {
        return new Dictionary<string, ResearchNode>(researchNodes);
    }

    public List<BatteryNode> GetAllBatteryNodes()
    {
        return new List<BatteryNode>(batteryNodes);
    }
}
