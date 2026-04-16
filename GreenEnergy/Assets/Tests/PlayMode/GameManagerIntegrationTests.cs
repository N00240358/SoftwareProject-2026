using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode integration tests for GameManager — verifies game state, carbon thresholds,
/// difficulty application, and energy delegation all work correctly after Start() runs.
///
/// These are [UnityTest] coroutines that yield at least one frame so Start() executes
/// before assertions. A minimal hierarchy is built in SetUp:
///   GameManagerGO  → GameManager + TimeManager + EnergyManager
/// No UIManager, MapGenerator or ResearchManager are wired — GameManager handles
/// null-checks gracefully for all optional references.
/// </summary>
[TestFixture]
public class GameManagerIntegrationTests
{
    private GameObject _go;
    private GameManager _gm;
    private EnergyManager _em;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("IntegrationGameManager");

        // Add components that GameManager.CacheMissingReferences() will find via GetComponent
        _em = _go.AddComponent<EnergyManager>();
        _go.AddComponent<TimeManager>();
        _gm = _go.AddComponent<GameManager>();

        // Prevent GameManager.Start() from calling StartNewGame() which tries to generate
        // the map, initialise research, etc. — those systems aren't present here.
        _gm.startAtMainMenu = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
        // Clear the static Instance so the next test's Awake doesn't see a stale
        // reference and destroy the new GameObject via the singleton guard.
        typeof(GameManager)
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, null);
    }

    // ===== Carbon constants =====

    [UnityTest]
    public IEnumerator StartingCarbon_ConstantIs420()
    {
        yield return null; // wait one frame for Start() to run
        Assert.AreEqual(420f, GameManager.STARTING_CARBON, 0.001f);
    }

    // ===== GetLoseCarbonThreshold =====

    [UnityTest]
    public IEnumerator GetLoseCarbonThreshold_Normal_Is580()
    {
        yield return null;
        // Default difficulty is Normal: 420 + 160 = 580
        Assert.AreEqual(580f, _gm.GetLoseCarbonThreshold(), 0.001f);
    }

    [UnityTest]
    public IEnumerator GetLoseCarbonThreshold_Easy_Is600()
    {
        yield return null;
        _gm.SetDifficulty(GameDifficulty.Easy, applyToSystems: false);
        Assert.AreEqual(600f, _gm.GetLoseCarbonThreshold(), 0.001f);
    }

    [UnityTest]
    public IEnumerator GetLoseCarbonThreshold_Hard_Is560()
    {
        yield return null;
        _gm.SetDifficulty(GameDifficulty.Hard, applyToSystems: false);
        Assert.AreEqual(560f, _gm.GetLoseCarbonThreshold(), 0.001f);
    }

    // ===== SetDifficulty =====

    [UnityTest]
    public IEnumerator SetDifficulty_Hard_UpdatesCurrentDifficultyField()
    {
        yield return null;
        _gm.SetDifficulty(GameDifficulty.Hard, applyToSystems: false);
        Assert.AreEqual(GameDifficulty.Hard, _gm.currentDifficulty);
    }

    [UnityTest]
    public IEnumerator SetDifficulty_Hard_CurrentTargetCarbon_Is280()
    {
        yield return null;
        _gm.SetDifficulty(GameDifficulty.Hard, applyToSystems: false);
        Assert.AreEqual(280f, _gm.CurrentTargetCarbon, 0.001f);
    }

    [UnityTest]
    public IEnumerator SetDifficulty_Easy_CurrentTargetCarbon_Is320()
    {
        yield return null;
        _gm.SetDifficulty(GameDifficulty.Easy, applyToSystems: false);
        Assert.AreEqual(320f, _gm.CurrentTargetCarbon, 0.001f);
    }

    // ===== TryConsumeEnergy =====

    [UnityTest]
    public IEnumerator TryConsumeEnergy_DelegatesToEnergyManager()
    {
        yield return null;
        // Wire energyManager (Start/CacheMissingReferences finds it via GetComponent)
        _em.currentEnergy    = 500f;
        _em.maxEnergyStorage = 1000f;

        bool result = _gm.TryConsumeEnergy(200f);

        Assert.IsTrue(result);
        Assert.AreEqual(300f, _em.currentEnergy, 0.001f);
    }

    [UnityTest]
    public IEnumerator TryConsumeEnergy_InsufficientEnergy_ReturnsFalse()
    {
        yield return null;
        _em.currentEnergy    = 100f;
        _em.maxEnergyStorage = 1000f;

        bool result = _gm.TryConsumeEnergy(500f);
        Assert.IsFalse(result);
    }

    // ===== AddEnergyStorage =====

    [UnityTest]
    public IEnumerator AddEnergyStorage_IncreasesMaxStorageOnEnergyManager()
    {
        yield return null;
        float before = _em.maxEnergyStorage;
        _gm.AddEnergyStorage(500f);
        Assert.AreEqual(before + 500f, _em.maxEnergyStorage, 0.001f);
    }

    // ===== SetTimeSpeed =====

    [UnityTest]
    public IEnumerator SetTimeSpeed_Paused_PausesTimeManager()
    {
        yield return null;
        _gm.SetTimeSpeed(GameManager.TimeSpeed.Paused);
        // Access TimeManager via the GameManager's reference
        TimeManager tm = _go.GetComponent<TimeManager>();
        Assert.IsTrue(tm.IsGameplayPaused);
    }

    [UnityTest]
    public IEnumerator SetTimeSpeed_Speed5x_SetsCorrectSpeedOnTimeManager()
    {
        yield return null;
        _gm.SetTimeSpeed(GameManager.TimeSpeed.Speed5x);
        TimeManager tm = _go.GetComponent<TimeManager>();
        Assert.AreEqual(GameManager.TimeSpeed.Speed5x, tm.CurrentTimeSpeed);
    }

    // ===== WinGame / LoseGame state transitions =====

    [UnityTest]
    public IEnumerator WinGame_SetsStateToWon()
    {
        yield return null;
        _gm.WinGame();
        Assert.AreEqual(GameManager.GameState.Won, _gm.currentGameState);
    }

    [UnityTest]
    public IEnumerator LoseGame_SetsStateToLost()
    {
        yield return null;
        _gm.LoseGame();
        Assert.AreEqual(GameManager.GameState.Lost, _gm.currentGameState);
    }

    [UnityTest]
    public IEnumerator WinGame_PausesTime()
    {
        yield return null;
        _gm.WinGame();
        TimeManager tm = _go.GetComponent<TimeManager>();
        Assert.IsTrue(tm.IsGameplayPaused, "Time should be paused when the game is won");
    }

    [UnityTest]
    public IEnumerator LoseGame_PausesTime()
    {
        yield return null;
        _gm.LoseGame();
        TimeManager tm = _go.GetComponent<TimeManager>();
        Assert.IsTrue(tm.IsGameplayPaused, "Time should be paused when the game is lost");
    }
}
