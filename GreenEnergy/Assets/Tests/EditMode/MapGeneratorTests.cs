using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Tests for MapGenerator — covers GetBiomeAt (bounds), CanPlaceGeneratorType (placement rules),
/// and GetBiomeEfficiencyMultiplier (biome efficiency values).
///
/// GenerateMap() is NOT called in these tests because it calls RenderTilemap() which requires
/// a Tilemap reference. Instead, the private biomeMap field is injected via reflection so that
/// GetBiomeAt and CanPlaceGeneratorType work with a known 3×3 grid:
///
///   (0,0)=Desert  (1,0)=Plains   (2,0)=Mountain
///   (0,1)=Water   (1,1)=Coastal  (2,1)=Forest
///   (0,2)=Plains  (1,2)=Desert   (2,2)=Coastal
///
/// GetBiomeEfficiencyMultiplier takes a BiomeType directly, so no biomeMap injection is needed
/// for those tests.
/// </summary>
[TestFixture]
public class MapGeneratorTests
{
    private GameObject _go;
    private MapGenerator _mg;

    // The private field we inject via reflection
    private static readonly FieldInfo BiomeMapField =
        typeof(MapGenerator).GetField("biomeMap",
            BindingFlags.NonPublic | BindingFlags.Instance);

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestMapGenerator");
        _mg = _go.AddComponent<MapGenerator>();
        _mg.mapWidth  = 3;
        _mg.mapHeight = 3;

        // Build a known 3×3 biome map and inject it
        var map = new MapGenerator.BiomeType[3, 3];
        map[0, 0] = MapGenerator.BiomeType.Desert;
        map[1, 0] = MapGenerator.BiomeType.Plains;
        map[2, 0] = MapGenerator.BiomeType.Mountain;
        map[0, 1] = MapGenerator.BiomeType.Water;
        map[1, 1] = MapGenerator.BiomeType.Coastal;
        map[2, 1] = MapGenerator.BiomeType.Forest;
        map[0, 2] = MapGenerator.BiomeType.Plains;
        map[1, 2] = MapGenerator.BiomeType.Desert;
        map[2, 2] = MapGenerator.BiomeType.Coastal;
        BiomeMapField.SetValue(_mg, map);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // ===== GetBiomeAt — valid coordinates =====

    [Test]
    public void GetBiomeAt_ValidCoord_0_0_ReturnsDesert()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Desert, _mg.GetBiomeAt(0, 0));
    }

    [Test]
    public void GetBiomeAt_ValidCoord_2_0_ReturnsMountain()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Mountain, _mg.GetBiomeAt(2, 0));
    }

    [Test]
    public void GetBiomeAt_ValidCoord_1_1_ReturnsCoastal()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Coastal, _mg.GetBiomeAt(1, 1));
    }

    [Test]
    public void GetBiomeAt_ValidCoord_2_1_ReturnsForest()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Forest, _mg.GetBiomeAt(2, 1));
    }

    // ===== GetBiomeAt — out-of-bounds returns Plains default =====

    [Test]
    public void GetBiomeAt_NegativeX_ReturnsPlains()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Plains, _mg.GetBiomeAt(-1, 0));
    }

    [Test]
    public void GetBiomeAt_NegativeY_ReturnsPlains()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Plains, _mg.GetBiomeAt(0, -1));
    }

    [Test]
    public void GetBiomeAt_XEqualToWidth_ReturnsPlains()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Plains, _mg.GetBiomeAt(3, 0));
    }

    [Test]
    public void GetBiomeAt_YEqualToHeight_ReturnsPlains()
    {
        Assert.AreEqual(MapGenerator.BiomeType.Plains, _mg.GetBiomeAt(0, 3));
    }

    // ===== CanPlaceGeneratorType — Solar =====
    // Solar allowed: Desert, Plains, Coastal, Forest

    [Test]
    public void CanPlace_Solar_OnDesert_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 0, 0)); // Desert
    }

    [Test]
    public void CanPlace_Solar_OnPlains_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 1, 0)); // Plains
    }

    [Test]
    public void CanPlace_Solar_OnCoastal_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 1, 1)); // Coastal
    }

    [Test]
    public void CanPlace_Solar_OnForest_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 2, 1)); // Forest
    }

    [Test]
    public void CanPlace_Solar_OnMountain_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 2, 0)); // Mountain
    }

    [Test]
    public void CanPlace_Solar_OnWater_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Solar, 0, 1)); // Water
    }

    // ===== CanPlaceGeneratorType — Wind =====
    // Wind allowed: Plains, Coastal, Mountain

    [Test]
    public void CanPlace_Wind_OnPlains_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 1, 0)); // Plains
    }

    [Test]
    public void CanPlace_Wind_OnCoastal_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 1, 1)); // Coastal
    }

    [Test]
    public void CanPlace_Wind_OnMountain_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 2, 0)); // Mountain
    }

    [Test]
    public void CanPlace_Wind_OnDesert_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 0, 0)); // Desert
    }

    [Test]
    public void CanPlace_Wind_OnWater_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 0, 1)); // Water
    }

    [Test]
    public void CanPlace_Wind_OnForest_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Wind, 2, 1)); // Forest
    }

    // ===== CanPlaceGeneratorType — Hydroelectric =====
    // Hydro allowed: Mountain, Forest

    [Test]
    public void CanPlace_Hydro_OnMountain_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Hydroelectric, 2, 0)); // Mountain
    }

    [Test]
    public void CanPlace_Hydro_OnForest_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Hydroelectric, 2, 1)); // Forest
    }

    [Test]
    public void CanPlace_Hydro_OnPlains_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Hydroelectric, 1, 0)); // Plains
    }

    [Test]
    public void CanPlace_Hydro_OnDesert_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Hydroelectric, 0, 0)); // Desert
    }

    [Test]
    public void CanPlace_Hydro_OnWater_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Hydroelectric, 0, 1)); // Water
    }

    // ===== CanPlaceGeneratorType — Tidal =====
    // Tidal allowed: Coastal, Water

    [Test]
    public void CanPlace_Tidal_OnCoastal_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Tidal, 1, 1)); // Coastal
    }

    [Test]
    public void CanPlace_Tidal_OnWater_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Tidal, 0, 1)); // Water
    }

    [Test]
    public void CanPlace_Tidal_OnDesert_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Tidal, 0, 0)); // Desert
    }

    [Test]
    public void CanPlace_Tidal_OnPlains_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Tidal, 1, 0)); // Plains
    }

    [Test]
    public void CanPlace_Tidal_OnMountain_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Tidal, 2, 0)); // Mountain
    }

    // ===== CanPlaceGeneratorType — Nuclear =====
    // Nuclear allowed: Desert, Plains, Forest

    [Test]
    public void CanPlace_Nuclear_OnDesert_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 0, 0)); // Desert
    }

    [Test]
    public void CanPlace_Nuclear_OnPlains_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 1, 0)); // Plains
    }

    [Test]
    public void CanPlace_Nuclear_OnForest_ReturnsTrue()
    {
        Assert.IsTrue(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 2, 1)); // Forest
    }

    [Test]
    public void CanPlace_Nuclear_OnMountain_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 2, 0)); // Mountain
    }

    [Test]
    public void CanPlace_Nuclear_OnWater_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 0, 1)); // Water
    }

    [Test]
    public void CanPlace_Nuclear_OnCoastal_ReturnsFalse()
    {
        Assert.IsFalse(_mg.CanPlaceGeneratorType(MapGenerator.GeneratorType.Nuclear, 1, 1)); // Coastal
    }

    // ===== GetBiomeEfficiencyMultiplier — Solar =====

    [Test]
    public void Efficiency_Solar_Desert_Is1Point2()
    {
        Assert.AreEqual(1.2f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Desert), 0.001f);
    }

    [Test]
    public void Efficiency_Solar_Plains_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Plains), 0.001f);
    }

    [Test]
    public void Efficiency_Solar_Coastal_Is0Point9()
    {
        Assert.AreEqual(0.9f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Coastal), 0.001f);
    }

    [Test]
    public void Efficiency_Solar_Forest_Is0Point7()
    {
        Assert.AreEqual(0.7f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Forest), 0.001f);
    }

    [Test]
    public void Efficiency_Solar_Mountain_IsZero()
    {
        // Mountain is an invalid biome for Solar — efficiency returns 0
        Assert.AreEqual(0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Mountain), 0.001f);
    }

    [Test]
    public void Efficiency_Solar_Water_IsZero()
    {
        Assert.AreEqual(0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Solar, MapGenerator.BiomeType.Water), 0.001f);
    }

    // ===== GetBiomeEfficiencyMultiplier — Wind =====

    [Test]
    public void Efficiency_Wind_Plains_Is1Point2()
    {
        Assert.AreEqual(1.2f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Wind, MapGenerator.BiomeType.Plains), 0.001f);
    }

    [Test]
    public void Efficiency_Wind_Coastal_Is1Point1()
    {
        Assert.AreEqual(1.1f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Wind, MapGenerator.BiomeType.Coastal), 0.001f);
    }

    [Test]
    public void Efficiency_Wind_Mountain_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Wind, MapGenerator.BiomeType.Mountain), 0.001f);
    }

    [Test]
    public void Efficiency_Wind_Desert_IsZero()
    {
        Assert.AreEqual(0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Wind, MapGenerator.BiomeType.Desert), 0.001f);
    }

    // ===== GetBiomeEfficiencyMultiplier — Hydroelectric =====

    [Test]
    public void Efficiency_Hydro_Mountain_Is1Point3()
    {
        Assert.AreEqual(1.3f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Hydroelectric, MapGenerator.BiomeType.Mountain), 0.001f);
    }

    [Test]
    public void Efficiency_Hydro_Forest_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Hydroelectric, MapGenerator.BiomeType.Forest), 0.001f);
    }

    [Test]
    public void Efficiency_Hydro_Desert_IsZero()
    {
        Assert.AreEqual(0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Hydroelectric, MapGenerator.BiomeType.Desert), 0.001f);
    }

    // ===== GetBiomeEfficiencyMultiplier — Tidal =====

    [Test]
    public void Efficiency_Tidal_Coastal_Is1Point2()
    {
        Assert.AreEqual(1.2f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Tidal, MapGenerator.BiomeType.Coastal), 0.001f);
    }

    [Test]
    public void Efficiency_Tidal_Water_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Tidal, MapGenerator.BiomeType.Water), 0.001f);
    }

    [Test]
    public void Efficiency_Tidal_Desert_IsZero()
    {
        Assert.AreEqual(0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Tidal, MapGenerator.BiomeType.Desert), 0.001f);
    }

    // ===== GetBiomeEfficiencyMultiplier — Nuclear =====

    [Test]
    public void Efficiency_Nuclear_Desert_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Nuclear, MapGenerator.BiomeType.Desert), 0.001f);
    }

    [Test]
    public void Efficiency_Nuclear_Plains_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Nuclear, MapGenerator.BiomeType.Plains), 0.001f);
    }

    [Test]
    public void Efficiency_Nuclear_Mountain_Is1Point0()
    {
        // Nuclear returns 1.0 for ALL biomes regardless of placement validity
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Nuclear, MapGenerator.BiomeType.Mountain), 0.001f);
    }

    [Test]
    public void Efficiency_Nuclear_Water_Is1Point0()
    {
        Assert.AreEqual(1.0f, _mg.GetBiomeEfficiencyMultiplier(
            MapGenerator.GeneratorType.Nuclear, MapGenerator.BiomeType.Water), 0.001f);
    }
}
