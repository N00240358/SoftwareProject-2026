using NUnit.Framework;

/// <summary>
/// Tests for TimeSystemUtils.GetSpeedMultiplier — verifies every TimeSpeed enum value
/// maps to the correct numeric multiplier. Pure static utility, no dependencies.
/// </summary>
[TestFixture]
public class TimeSystemUtilsTests
{
    [TestCase(GameManager.TimeSpeed.Paused,   0f)]
    [TestCase(GameManager.TimeSpeed.Normal,   1f)]
    [TestCase(GameManager.TimeSpeed.Speed2x,  2f)]
    [TestCase(GameManager.TimeSpeed.Speed5x,  5f)]
    [TestCase(GameManager.TimeSpeed.Speed10x, 10f)]
    public void GetSpeedMultiplier_KnownSpeed_ReturnsCorrectValue(GameManager.TimeSpeed speed, float expected)
    {
        float result = TimeSystemUtils.GetSpeedMultiplier(speed);
        Assert.AreEqual(expected, result, 0.001f, $"Expected multiplier {expected} for {speed}");
    }

    [Test]
    public void GetSpeedMultiplier_Paused_ReturnsZero()
    {
        // Paused must return exactly 0 — any non-zero value would advance time
        float result = TimeSystemUtils.GetSpeedMultiplier(GameManager.TimeSpeed.Paused);
        Assert.AreEqual(0f, result, 0.0001f);
    }

    [Test]
    public void GetSpeedMultiplier_Normal_ReturnsOne()
    {
        float result = TimeSystemUtils.GetSpeedMultiplier(GameManager.TimeSpeed.Normal);
        Assert.AreEqual(1f, result, 0.0001f);
    }

    [Test]
    public void GetSpeedMultiplier_Speed10x_IsHigherThanSpeed5x()
    {
        float speed10 = TimeSystemUtils.GetSpeedMultiplier(GameManager.TimeSpeed.Speed10x);
        float speed5  = TimeSystemUtils.GetSpeedMultiplier(GameManager.TimeSpeed.Speed5x);
        Assert.Greater(speed10, speed5, "10x should be faster than 5x");
    }

    [Test]
    public void GetSpeedMultiplier_UnknownSpeed_DefaultsToOne()
    {
        // The switch default: returns 1f so unknown values don't freeze or warp time
        float result = TimeSystemUtils.GetSpeedMultiplier((GameManager.TimeSpeed)99);
        Assert.AreEqual(1f, result, 0.0001f);
    }

    [Test]
    public void GetSpeedMultiplier_AllKnownSpeeds_AreNonNegative()
    {
        foreach (GameManager.TimeSpeed speed in System.Enum.GetValues(typeof(GameManager.TimeSpeed)))
        {
            float result = TimeSystemUtils.GetSpeedMultiplier(speed);
            Assert.GreaterOrEqual(result, 0f, $"{speed} should not have a negative multiplier");
        }
    }
}
