using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for TimeManager — covers SetTimeSpeed, SetGameplayPaused, and GetDayNightMultiplier.
/// UpdateTime() uses Time.deltaTime so it cannot be called from EditMode tests;
/// everything here exercises state-setting methods and the pure math of GetDayNightMultiplier.
/// </summary>
[TestFixture]
public class TimeManagerTests
{
    private GameObject _go;
    private TimeManager _tm;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestTimeManager");
        _tm = _go.AddComponent<TimeManager>();
        // Default state after Awake: Normal speed, not paused
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // ===== SetTimeSpeed =====

    [Test]
    public void SetTimeSpeed_Normal_SetsCurrentTimeSpeedToNormal()
    {
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Normal);
        Assert.AreEqual(GameManager.TimeSpeed.Normal, _tm.CurrentTimeSpeed);
    }

    [Test]
    public void SetTimeSpeed_Paused_SetsCurrentTimeSpeedToPaused()
    {
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Paused);
        Assert.AreEqual(GameManager.TimeSpeed.Paused, _tm.CurrentTimeSpeed);
    }

    [Test]
    public void SetTimeSpeed_Paused_SetsIsGameplayPausedTrue()
    {
        // Setting speed to Paused also raises the IsGameplayPaused flag
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Paused);
        Assert.IsTrue(_tm.IsGameplayPaused);
    }

    [Test]
    public void SetTimeSpeed_Normal_ClearsIsGameplayPausedFlag()
    {
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Paused); // pause first
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Normal); // then resume
        Assert.IsFalse(_tm.IsGameplayPaused);
    }

    [Test]
    public void SetTimeSpeed_Speed10x_SetsCorrectSpeed()
    {
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Speed10x);
        Assert.AreEqual(GameManager.TimeSpeed.Speed10x, _tm.CurrentTimeSpeed);
    }

    [Test]
    public void SetTimeSpeed_Speed10x_DoesNotPauseGame()
    {
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Speed10x);
        Assert.IsFalse(_tm.IsGameplayPaused);
    }

    // ===== SetGameplayPaused =====

    [Test]
    public void SetGameplayPaused_True_SetsIsGameplayPausedTrue()
    {
        _tm.SetGameplayPaused(true);
        Assert.IsTrue(_tm.IsGameplayPaused);
    }

    [Test]
    public void SetGameplayPaused_False_ClearsIsGameplayPaused()
    {
        _tm.SetGameplayPaused(true);
        _tm.SetGameplayPaused(false);
        Assert.IsFalse(_tm.IsGameplayPaused);
    }

    [Test]
    public void SetGameplayPaused_DoesNotChangeCurrentTimeSpeed()
    {
        // SetGameplayPaused only affects the pause flag, not the speed setting
        _tm.SetTimeSpeed(GameManager.TimeSpeed.Speed2x);
        _tm.SetGameplayPaused(true);
        Assert.AreEqual(GameManager.TimeSpeed.Speed2x, _tm.CurrentTimeSpeed);
    }

    // ===== GetDayNightMultiplier =====

    [Test]
    public void GetDayNightMultiplier_AtMidnight_IsZeroOrNearZero()
    {
        // timeOfDay=0 → Abs(Sin(0 * PI)) = Abs(Sin(0)) = 0
        _tm.timeOfDay = 0f;
        Assert.AreEqual(0f, _tm.GetDayNightMultiplier(), 0.001f);
    }

    [Test]
    public void GetDayNightMultiplier_AtNoon_IsOne()
    {
        // timeOfDay=0.5 → Abs(Sin(0.5 * PI)) = Abs(Sin(PI/2)) = 1
        _tm.timeOfDay = 0.5f;
        Assert.AreEqual(1f, _tm.GetDayNightMultiplier(), 0.001f);
    }

    [Test]
    public void GetDayNightMultiplier_AtEndOfDay_IsZeroOrNearZero()
    {
        // timeOfDay=1 → Abs(Sin(PI)) ≈ 0
        _tm.timeOfDay = 1f;
        Assert.AreEqual(0f, _tm.GetDayNightMultiplier(), 0.001f);
    }

    [Test]
    public void GetDayNightMultiplier_At6AM_IsPositive()
    {
        // timeOfDay=0.25 (6 AM) → Abs(Sin(PI/4)) ≈ 0.707
        _tm.timeOfDay = 0.25f;
        float result = _tm.GetDayNightMultiplier();
        Assert.Greater(result, 0f, "Morning sun should give positive output");
        Assert.LessOrEqual(result, 1f, "Morning sun should not exceed noon maximum");
    }

    [Test]
    public void GetDayNightMultiplier_IsAlwaysNonNegative()
    {
        // Abs() ensures no negative values regardless of timeOfDay
        for (float t = 0f; t <= 1f; t += 0.1f)
        {
            _tm.timeOfDay = t;
            float result = _tm.GetDayNightMultiplier();
            Assert.GreaterOrEqual(result, 0f, $"Multiplier should be >= 0 at timeOfDay={t}");
        }
    }

    [Test]
    public void GetDayNightMultiplier_NoonIsHigherThanMorning()
    {
        _tm.timeOfDay = 0.5f;  // noon
        float noon = _tm.GetDayNightMultiplier();

        _tm.timeOfDay = 0.25f; // 6 AM
        float morning = _tm.GetDayNightMultiplier();

        Assert.Greater(noon, morning, "Noon multiplier should be higher than morning");
    }
}
