using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for GeneratorManager cost formulas — GetPlacementCost and GetUpgradeCost.
/// These methods only use the type and tier arguments; no GameManager, no mapGenerator,
/// no scene is required. Uses AddComponent pattern to host the MonoBehaviour.
///
/// Placement formula: baseCost × (1 + (tier - 1) × 0.3)   (+30% per tier above 1)
/// Upgrade formula:   baseCost × targetTier × 0.5           (50% of base × tier number)
///
/// Base costs: Solar=80, Wind=120, Hydro=240, Tidal=180, Nuclear=900
/// </summary>
[TestFixture]
public class GeneratorManagerCostTests
{
    private GameObject _go;
    private GeneratorManager _gm;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestGeneratorManager");
        _gm = _go.AddComponent<GeneratorManager>();
        // GeneratorManager.Instance is now set. Initialize() is NOT called here
        // because we only need the cost methods, which have no state dependencies.
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // ===== GetPlacementCost — base costs at tier 1 =====

    [TestCase(MapGenerator.GeneratorType.Solar,          1,  80f)]
    [TestCase(MapGenerator.GeneratorType.Wind,           1, 120f)]
    [TestCase(MapGenerator.GeneratorType.Hydroelectric,  1, 240f)]
    [TestCase(MapGenerator.GeneratorType.Tidal,          1, 180f)]
    [TestCase(MapGenerator.GeneratorType.Nuclear,        1, 900f)]
    public void GetPlacementCost_Tier1_ReturnsBaseCost(
        MapGenerator.GeneratorType type, int tier, float expected)
    {
        // Tier 1: multiplier = 1 + (1-1)*0.3 = 1.0 → cost = baseCost × 1.0
        Assert.AreEqual(expected, _gm.GetPlacementCost(type, tier), 0.01f);
    }

    // ===== GetPlacementCost — tier scaling (+30% per tier) =====

    [Test]
    public void GetPlacementCost_Solar_Tier2_Is104()
    {
        // 80 × (1 + 1×0.3) = 80 × 1.3 = 104
        Assert.AreEqual(104f, _gm.GetPlacementCost(MapGenerator.GeneratorType.Solar, 2), 0.01f);
    }

    [Test]
    public void GetPlacementCost_Solar_Tier5_Is176()
    {
        // 80 × (1 + 4×0.3) = 80 × 2.2 = 176
        Assert.AreEqual(176f, _gm.GetPlacementCost(MapGenerator.GeneratorType.Solar, 5), 0.01f);
    }

    [Test]
    public void GetPlacementCost_Solar_Tier10_Is296()
    {
        // 80 × (1 + 9×0.3) = 80 × 3.7 = 296
        Assert.AreEqual(296f, _gm.GetPlacementCost(MapGenerator.GeneratorType.Solar, 10), 0.01f);
    }

    [Test]
    public void GetPlacementCost_Nuclear_Tier2_Is1170()
    {
        // 900 × (1 + 1×0.3) = 900 × 1.3 = 1170
        Assert.AreEqual(1170f, _gm.GetPlacementCost(MapGenerator.GeneratorType.Nuclear, 2), 0.01f);
    }

    [Test]
    public void GetPlacementCost_Nuclear_Tier10_Is3330()
    {
        // 900 × (1 + 9×0.3) = 900 × 3.7 = 3330
        Assert.AreEqual(3330f, _gm.GetPlacementCost(MapGenerator.GeneratorType.Nuclear, 10), 0.01f);
    }

    [Test]
    public void GetPlacementCost_HigherTierAlwaysMoreExpensive()
    {
        // Verify cost is strictly increasing across all types
        MapGenerator.GeneratorType[] types = {
            MapGenerator.GeneratorType.Solar,
            MapGenerator.GeneratorType.Wind,
            MapGenerator.GeneratorType.Hydroelectric,
            MapGenerator.GeneratorType.Tidal,
            MapGenerator.GeneratorType.Nuclear
        };

        foreach (var type in types)
        {
            float costTier1 = _gm.GetPlacementCost(type, 1);
            float costTier5 = _gm.GetPlacementCost(type, 5);
            float costTier10 = _gm.GetPlacementCost(type, 10);
            Assert.Greater(costTier5, costTier1, $"{type}: tier 5 should cost more than tier 1");
            Assert.Greater(costTier10, costTier5, $"{type}: tier 10 should cost more than tier 5");
        }
    }

    // ===== GetUpgradeCost — formula: baseCost × targetTier × 0.5 =====

    [TestCase(MapGenerator.GeneratorType.Solar,         2,   80f)]   // 80 × 2 × 0.5 = 80
    [TestCase(MapGenerator.GeneratorType.Solar,         5,  200f)]   // 80 × 5 × 0.5 = 200
    [TestCase(MapGenerator.GeneratorType.Solar,        10,  400f)]   // 80 × 10 × 0.5 = 400
    [TestCase(MapGenerator.GeneratorType.Wind,          2,  120f)]   // 120 × 2 × 0.5 = 120
    [TestCase(MapGenerator.GeneratorType.Hydroelectric, 2,  240f)]   // 240 × 2 × 0.5 = 240
    [TestCase(MapGenerator.GeneratorType.Tidal,         2,  180f)]   // 180 × 2 × 0.5 = 180
    [TestCase(MapGenerator.GeneratorType.Nuclear,       2,  900f)]   // 900 × 2 × 0.5 = 900
    [TestCase(MapGenerator.GeneratorType.Nuclear,      10, 4500f)]   // 900 × 10 × 0.5 = 4500
    public void GetUpgradeCost_ReturnsCorrectValue(
        MapGenerator.GeneratorType type, int targetTier, float expected)
    {
        Assert.AreEqual(expected, _gm.GetUpgradeCost(type, targetTier), 0.01f);
    }

    [Test]
    public void GetUpgradeCost_HigherTargetTierCostsMore()
    {
        float tier2 = _gm.GetUpgradeCost(MapGenerator.GeneratorType.Solar, 2);
        float tier5 = _gm.GetUpgradeCost(MapGenerator.GeneratorType.Solar, 5);
        float tier10 = _gm.GetUpgradeCost(MapGenerator.GeneratorType.Solar, 10);
        Assert.Greater(tier5, tier2);
        Assert.Greater(tier10, tier5);
    }

    [Test]
    public void GetUpgradeCost_NuclearTier10_IsHighestUpgradeCost()
    {
        float nuclearTier10 = _gm.GetUpgradeCost(MapGenerator.GeneratorType.Nuclear, 10);
        float solarTier10   = _gm.GetUpgradeCost(MapGenerator.GeneratorType.Solar, 10);
        Assert.Greater(nuclearTier10, solarTier10,
            "Nuclear upgrades should be more expensive than Solar at the same tier");
    }
}
