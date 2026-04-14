using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for ResearchManager — covers unlock state, research guards (chain, energy, duplicates),
/// battery research, next-available-tier queries, and node cost scaling by difficulty.
///
/// Setup order is critical:
///   1. Create GameManager + EnergyManager on the same GameObject (matching production layout)
///   2. Wire GameManager.energyManager manually (Start() doesn't run in EditMode)
///   3. Create ResearchManager, then call Initialize()
///
/// This order ensures ResearchNode constructors can call GameManager.Instance.CurrentDifficultyProfile
/// without a null-ref, and that TryConsumeEnergy works through the energyManager reference.
/// </summary>
[TestFixture]
public class ResearchManagerTests
{
    private GameObject _managerGo;   // hosts GameManager + EnergyManager
    private GameObject _researchGo;  // hosts ResearchManager

    private GameManager     _gm;
    private EnergyManager   _em;
    private ResearchManager _rm;

    [SetUp]
    public void SetUp()
    {
        // 1. Create GameManager (needs EnergyManager on same GO for TryConsumeEnergy)
        _managerGo = new GameObject("GameManager");
        _em = _managerGo.AddComponent<EnergyManager>();
        _gm = _managerGo.AddComponent<GameManager>();

        // 2. Wire energyManager manually — Start() doesn't run in EditMode
        _gm.energyManager       = _em;
        _em.currentEnergy       = 100000f;
        _em.maxEnergyStorage    = 100000f;

        // Force-set GameManager.Instance via reflection. In Unity EditMode the Awake
        // singleton pattern is unreliable across test iterations (the static field can
        // survive as a fake-null from a previous TearDown's DestroyImmediate, causing
        // GameManager.Instance.TryConsumeEnergy to throw NullReferenceException).
        typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, _gm);

        // 3. Create ResearchManager and initialise the tree
        _researchGo = new GameObject("ResearchManager");
        _rm = _researchGo.AddComponent<ResearchManager>();
        _rm.Initialize(); // builds trees, unlocks Solar_Tier1
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_researchGo);
        Object.DestroyImmediate(_managerGo);
        // Clear statics so a stale fake-null never leaks into the next test's SetUp
        typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, null);
    }

    // ===== Initial state after Initialize() =====

    [Test]
    public void IsUnlocked_SolarTier1_TrueAfterInitialize()
    {
        Assert.IsTrue(_rm.IsUnlocked(MapGenerator.GeneratorType.Solar, 1));
    }

    [Test]
    public void IsUnlocked_SolarTier2_FalseAfterInitialize()
    {
        Assert.IsFalse(_rm.IsUnlocked(MapGenerator.GeneratorType.Solar, 2));
    }

    [Test]
    public void IsUnlocked_WindTier1_FalseAfterInitialize()
    {
        Assert.IsFalse(_rm.IsUnlocked(MapGenerator.GeneratorType.Wind, 1));
    }

    [Test]
    public void IsUnlocked_NuclearTier1_FalseAfterInitialize()
    {
        Assert.IsFalse(_rm.IsUnlocked(MapGenerator.GeneratorType.Nuclear, 1));
    }

    [Test]
    public void GetAllNodes_Returns50NodesAfterInitialize()
    {
        // 5 generator types × 10 tiers = 50 generator research nodes
        Assert.AreEqual(50, _rm.GetAllNodes().Count);
    }

    [Test]
    public void GetAllBatteryNodes_Returns10NodesAfterInitialize()
    {
        Assert.AreEqual(10, _rm.GetAllBatteryNodes().Count);
    }

    // ===== GetNextAvailableTier =====

    [Test]
    public void GetNextAvailableTier_Solar_ReturnsTier2AfterInitialize()
    {
        // Solar_Tier1 is already unlocked, so the next tier to research is 2
        Assert.AreEqual(2, _rm.GetNextAvailableTier(MapGenerator.GeneratorType.Solar));
    }

    [Test]
    public void GetNextAvailableTier_Wind_ReturnsTier1AfterInitialize()
    {
        // No wind tiers are unlocked yet
        Assert.AreEqual(1, _rm.GetNextAvailableTier(MapGenerator.GeneratorType.Wind));
    }

    [Test]
    public void GetNextAvailableBatteryTier_InitialState_ReturnsTier1()
    {
        Assert.AreEqual(1, _rm.GetNextAvailableBatteryTier());
    }

    // ===== StartResearch — success path =====

    [Test]
    public void StartResearch_SolarTier2_WithPreviousTierUnlocked_ReturnsTrue()
    {
        bool result = _rm.StartResearch("Solar_Tier2");
        Assert.IsTrue(result);
    }

    [Test]
    public void StartResearch_SolarTier2_SetsIsResearchingTrue()
    {
        _rm.StartResearch("Solar_Tier2");
        Assert.IsTrue(_rm.IsResearching(MapGenerator.GeneratorType.Solar, 2));
    }

    [Test]
    public void StartResearch_SolarTier2_DeductsEnergyFromGameManager()
    {
        float before = _em.currentEnergy;
        float cost   = _rm.GetNode("Solar_Tier2").energyCost;
        _rm.StartResearch("Solar_Tier2");
        Assert.AreEqual(before - cost, _em.currentEnergy, 0.01f);
    }

    [Test]
    public void StartResearch_SolarTier2_AppearsInActiveResearch()
    {
        _rm.StartResearch("Solar_Tier2");
        var active = _rm.GetActiveResearch();
        Assert.AreEqual(1, active.Count);
        Assert.AreEqual("Solar_Tier2", active[0].nodeId);
    }

    // ===== StartResearch — guard: already unlocked =====

    [Test]
    public void StartResearch_SolarTier1_AlreadyUnlocked_ReturnsFalse()
    {
        bool result = _rm.StartResearch("Solar_Tier1");
        Assert.IsFalse(result, "Cannot re-research an already unlocked node");
    }

    // ===== StartResearch — guard: tier chain =====

    [Test]
    public void StartResearch_SolarTier3_WithoutTier2_ReturnsFalse()
    {
        // Tier 2 is not unlocked yet, so tier 3 cannot be started
        bool result = _rm.StartResearch("Solar_Tier3");
        Assert.IsFalse(result);
    }

    [Test]
    public void StartResearch_WindTier2_WithoutWindTier1_ReturnsFalse()
    {
        bool result = _rm.StartResearch("Wind_Tier2");
        Assert.IsFalse(result);
    }

    // ===== StartResearch — guard: already researching =====

    [Test]
    public void StartResearch_SolarTier2_Twice_SecondCallReturnsFalse()
    {
        _rm.StartResearch("Solar_Tier2");
        bool secondResult = _rm.StartResearch("Solar_Tier2");
        Assert.IsFalse(secondResult, "Cannot start research on a node that is already in progress");
    }

    // ===== StartResearch — guard: insufficient energy =====

    [Test]
    public void StartResearch_WithInsufficientEnergy_ReturnsFalse()
    {
        _em.currentEnergy = 0f;
        bool result = _rm.StartResearch("Solar_Tier2");
        Assert.IsFalse(result);
    }

    [Test]
    public void StartResearch_WithInsufficientEnergy_DoesNotSetResearching()
    {
        _em.currentEnergy = 0f;
        _rm.StartResearch("Solar_Tier2");
        Assert.IsFalse(_rm.IsResearching(MapGenerator.GeneratorType.Solar, 2));
    }

    // ===== StartResearch — guard: invalid node ID =====

    [Test]
    public void StartResearch_InvalidNodeId_ReturnsFalse()
    {
        LogAssert.Expect(LogType.Error, "Research node NotARealNode_Tier99 not found!");
        bool result = _rm.StartResearch("NotARealNode_Tier99");
        Assert.IsFalse(result);
    }

    // ===== StartBatteryResearch — success path =====

    [Test]
    public void StartBatteryResearch_Tier1_ReturnsTrue()
    {
        bool result = _rm.StartBatteryResearch(1);
        Assert.IsTrue(result);
    }

    [Test]
    public void StartBatteryResearch_Tier1_SetsIsResearchingTrue()
    {
        _rm.StartBatteryResearch(1);
        Assert.IsTrue(_rm.IsBatteryResearching(1));
    }

    [Test]
    public void StartBatteryResearch_Tier1_AppearsInActiveBatteryResearch()
    {
        _rm.StartBatteryResearch(1);
        var active = _rm.GetActiveBatteryResearch();
        Assert.AreEqual(1, active.Count);
        Assert.AreEqual(1, active[0].tier);
    }

    // ===== StartBatteryResearch — guards =====

    [Test]
    public void StartBatteryResearch_Tier0_ReturnsFalse()
    {
        LogAssert.Expect(LogType.Error, "Invalid battery tier: 0");
        bool result = _rm.StartBatteryResearch(0);
        Assert.IsFalse(result);
    }

    [Test]
    public void StartBatteryResearch_Tier11_ReturnsFalse()
    {
        LogAssert.Expect(LogType.Error, "Invalid battery tier: 11");
        bool result = _rm.StartBatteryResearch(11);
        Assert.IsFalse(result);
    }

    [Test]
    public void StartBatteryResearch_Tier2_WithoutTier1Unlocked_ReturnsFalse()
    {
        bool result = _rm.StartBatteryResearch(2);
        Assert.IsFalse(result);
    }

    // ===== GetNode =====

    [Test]
    public void GetNode_ValidId_ReturnsCorrectNode()
    {
        ResearchNode node = _rm.GetNode("Wind_Tier3");
        Assert.IsNotNull(node);
        Assert.AreEqual(MapGenerator.GeneratorType.Wind, node.generatorType);
        Assert.AreEqual(3, node.tier);
    }

    [Test]
    public void GetNode_InvalidId_ReturnsNull()
    {
        ResearchNode node = _rm.GetNode("Nonexistent_Tier99");
        Assert.IsNull(node);
    }

    // ===== GetBatteryNode =====

    [Test]
    public void GetBatteryNode_Tier1_ReturnsCorrectNode()
    {
        BatteryNode node = _rm.GetBatteryNode(1);
        Assert.IsNotNull(node);
        Assert.AreEqual(1, node.tier);
    }

    [Test]
    public void GetBatteryNode_Tier0_ReturnsNull()
    {
        Assert.IsNull(_rm.GetBatteryNode(0));
    }

    [Test]
    public void GetBatteryNode_Tier11_ReturnsNull()
    {
        Assert.IsNull(_rm.GetBatteryNode(11));
    }

    // ===== Research node cost scaling on Normal difficulty =====

    [Test]
    public void ResearchNode_SolarTier1_CostIs100_OnNormalDifficulty()
    {
        // baseCost=100, tier=1 → 100 × 1.75^0 = 100
        ResearchNode node = _rm.GetNode("Solar_Tier1");
        Assert.AreEqual(100f, node.energyCost, 0.01f);
    }

    [Test]
    public void ResearchNode_SolarTier2_CostIs175_OnNormalDifficulty()
    {
        // 100 × 1.75^1 = 175
        ResearchNode node = _rm.GetNode("Solar_Tier2");
        Assert.AreEqual(175f, node.energyCost, 0.01f);
    }

    [Test]
    public void ResearchNode_NuclearTier1_CostIs1200_OnNormalDifficulty()
    {
        // baseCost=1200, tier=1 → 1200 × 1.75^0 = 1200
        ResearchNode node = _rm.GetNode("Nuclear_Tier1");
        Assert.AreEqual(1200f, node.energyCost, 0.01f);
    }

    [Test]
    public void BatteryNode_Tier1_CostIs220_OnNormalDifficulty()
    {
        // 220 × 1.75^0 = 220
        BatteryNode node = _rm.GetBatteryNode(1);
        Assert.AreEqual(220f, node.energyCost, 0.01f);
    }

    [Test]
    public void BatteryNode_Tier2_CostIs385_OnNormalDifficulty()
    {
        // 220 × 1.75^1 = 385
        BatteryNode node = _rm.GetBatteryNode(2);
        Assert.AreEqual(385f, node.energyCost, 0.01f);
    }

    // ===== ApplyDifficultyProfile changes costs =====

    [Test]
    public void ApplyDifficultyProfile_Easy_LowersSolarTier2Cost()
    {
        // Normal: 100 × 1.75 = 175 ; Easy: 100 × 1.5 = 150
        DifficultyBalanceProfile easyProfile = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy);
        _rm.ApplyDifficultyProfile(easyProfile);
        ResearchNode node = _rm.GetNode("Solar_Tier2");
        Assert.AreEqual(150f, node.energyCost, 0.01f);
    }

    [Test]
    public void ApplyDifficultyProfile_Hard_RaisesSolarTier2Cost()
    {
        // Hard: 100 × 2.0 = 200 (vs Normal 175)
        DifficultyBalanceProfile hardProfile = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard);
        _rm.ApplyDifficultyProfile(hardProfile);
        ResearchNode node = _rm.GetNode("Solar_Tier2");
        Assert.AreEqual(200f, node.energyCost, 0.01f);
    }
}
