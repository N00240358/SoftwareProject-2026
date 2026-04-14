using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for EnergyManager — covers TryConsumeEnergy, AddEnergy, and GetEnergyPercentage.
/// Uses the AddComponent pattern: a bare GameObject hosts EnergyManager so Awake() runs,
/// but we set fields directly to avoid triggering ResetEnergy() (which calls singletons).
/// </summary>
[TestFixture]
public class EnergyManagerTests
{
    private GameObject _go;
    private EnergyManager _em;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestEnergyManager");
        _em = _go.AddComponent<EnergyManager>();
        // Set a known state directly — do NOT call ResetEnergy() here because
        // that method tries to wire GeneratorManager.Instance and UIManager.Instance.
        _em.currentEnergy    = 500f;
        _em.maxEnergyStorage = 1000f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // ===== TryConsumeEnergy =====

    [Test]
    public void TryConsumeEnergy_SufficientEnergy_ReturnsTrue()
    {
        bool result = _em.TryConsumeEnergy(200f);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryConsumeEnergy_SufficientEnergy_DeductsCorrectAmount()
    {
        _em.TryConsumeEnergy(200f);
        Assert.AreEqual(300f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void TryConsumeEnergy_ExactAmount_ReturnsTrueAndZeroesEnergy()
    {
        bool result = _em.TryConsumeEnergy(500f);
        Assert.IsTrue(result);
        Assert.AreEqual(0f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void TryConsumeEnergy_InsufficientEnergy_ReturnsFalse()
    {
        bool result = _em.TryConsumeEnergy(600f);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryConsumeEnergy_InsufficientEnergy_DoesNotChangeCurrentEnergy()
    {
        _em.TryConsumeEnergy(600f);
        Assert.AreEqual(500f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void TryConsumeEnergy_ZeroAmount_ReturnsTrue()
    {
        bool result = _em.TryConsumeEnergy(0f);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryConsumeEnergy_ZeroAmount_DoesNotChangeEnergy()
    {
        _em.TryConsumeEnergy(0f);
        Assert.AreEqual(500f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void TryConsumeEnergy_NegativeAmount_ReturnsFalse()
    {
        // Negative consumption is not allowed — the method guards against it
        bool result = _em.TryConsumeEnergy(-50f);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryConsumeEnergy_NegativeAmount_DoesNotChangeEnergy()
    {
        _em.TryConsumeEnergy(-50f);
        Assert.AreEqual(500f, _em.currentEnergy, 0.001f);
    }

    // ===== AddEnergy =====

    [Test]
    public void AddEnergy_BelowCap_AddsCorrectAmount()
    {
        _em.AddEnergy(200f);
        Assert.AreEqual(700f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void AddEnergy_ExceedsCap_ClampsToMaxStorage()
    {
        _em.currentEnergy = 900f;
        _em.AddEnergy(200f); // Would be 1100, capped at 1000
        Assert.AreEqual(1000f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void AddEnergy_ExactlyFillsStorage_DoesNotExceedMax()
    {
        _em.currentEnergy = 500f;
        _em.AddEnergy(500f);
        Assert.AreEqual(1000f, _em.currentEnergy, 0.001f);
    }

    [Test]
    public void AddEnergy_WhenAlreadyFull_StaysAtMax()
    {
        _em.currentEnergy = 1000f;
        _em.AddEnergy(100f);
        Assert.AreEqual(1000f, _em.currentEnergy, 0.001f);
    }

    // ===== GetEnergyPercentage =====

    [Test]
    public void GetEnergyPercentage_HalfFull_Returns0Point5()
    {
        _em.currentEnergy    = 500f;
        _em.maxEnergyStorage = 1000f;
        Assert.AreEqual(0.5f, _em.GetEnergyPercentage(), 0.001f);
    }

    [Test]
    public void GetEnergyPercentage_Full_Returns1()
    {
        _em.currentEnergy    = 1000f;
        _em.maxEnergyStorage = 1000f;
        Assert.AreEqual(1f, _em.GetEnergyPercentage(), 0.001f);
    }

    [Test]
    public void GetEnergyPercentage_Empty_Returns0()
    {
        _em.currentEnergy    = 0f;
        _em.maxEnergyStorage = 1000f;
        Assert.AreEqual(0f, _em.GetEnergyPercentage(), 0.001f);
    }

    [Test]
    public void GetEnergyPercentage_ZeroMaxStorage_Returns0NotNaN()
    {
        // Guard: avoid divide-by-zero — should return 0, not NaN or infinity
        _em.currentEnergy    = 100f;
        _em.maxEnergyStorage = 0f;
        float result = _em.GetEnergyPercentage();
        Assert.AreEqual(0f, result, 0.001f);
        Assert.IsFalse(float.IsNaN(result));
    }
}
