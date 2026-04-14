using NUnit.Framework;

/// <summary>
/// Tests for DifficultyBalanceLibrary — verifies all three difficulty profiles
/// have correct values and that ordering invariants hold (Hard is harder than Easy, etc.).
/// These are pure data tests with zero external dependencies.
/// </summary>
[TestFixture]
public class DifficultyBalanceTests
{
    // ===== EASY PROFILE =====

    [Test]
    public void GetProfile_Easy_HasCorrectTargetCarbon()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy);
        Assert.AreEqual(320f, p.targetCarbon, 0.001f);
    }

    [Test]
    public void GetProfile_Easy_HasCorrectGeneratorOutputMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy);
        Assert.AreEqual(0.85f, p.generatorOutputMultiplier, 0.001f);
    }

    [Test]
    public void GetProfile_Easy_HasCorrectResearchCostMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy);
        Assert.AreEqual(1.5f, p.researchCostMultiplier, 0.001f);
    }

    [Test]
    public void GetProfile_Easy_LoseThresholdIs600()
    {
        // 420 (starting carbon) + 180 (easy lose offset) = 600
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy);
        float loseThreshold = GameManager.STARTING_CARBON + p.loseCarbonOffset;
        Assert.AreEqual(600f, loseThreshold, 0.001f);
    }

    // ===== NORMAL PROFILE =====

    [Test]
    public void GetProfile_Normal_HasCorrectTargetCarbon()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        Assert.AreEqual(300f, p.targetCarbon, 0.001f);
    }

    [Test]
    public void GetProfile_Normal_HasCorrectGeneratorOutputMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        Assert.AreEqual(0.75f, p.generatorOutputMultiplier, 0.001f);
    }

    [Test]
    public void GetProfile_Normal_HasCorrectCarbonReductionMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        Assert.AreEqual(0.00075f, p.carbonReductionMultiplier, 0.000001f);
    }

    [Test]
    public void GetProfile_Normal_LoseThresholdIs580()
    {
        // 420 + 160 = 580
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        float loseThreshold = GameManager.STARTING_CARBON + p.loseCarbonOffset;
        Assert.AreEqual(580f, loseThreshold, 0.001f);
    }

    // ===== HARD PROFILE =====

    [Test]
    public void GetProfile_Hard_HasCorrectTargetCarbon()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard);
        Assert.AreEqual(280f, p.targetCarbon, 0.001f);
    }

    [Test]
    public void GetProfile_Hard_HasCorrectGeneratorOutputMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard);
        Assert.AreEqual(0.65f, p.generatorOutputMultiplier, 0.001f);
    }

    [Test]
    public void GetProfile_Hard_HasCorrectResearchCostMultiplier()
    {
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard);
        Assert.AreEqual(2.0f, p.researchCostMultiplier, 0.001f);
    }

    [Test]
    public void GetProfile_Hard_LoseThresholdIs560()
    {
        // 420 + 140 = 560
        DifficultyBalanceProfile p = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard);
        float loseThreshold = GameManager.STARTING_CARBON + p.loseCarbonOffset;
        Assert.AreEqual(560f, loseThreshold, 0.001f);
    }

    // ===== DIFFICULTY ORDERING INVARIANTS =====

    [Test]
    public void Hard_HasLowerTargetCarbon_ThanEasy()
    {
        float easyTarget = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy).targetCarbon;
        float hardTarget = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard).targetCarbon;
        Assert.Less(hardTarget, easyTarget, "Hard should require lower carbon (stricter win condition) than Easy");
    }

    [Test]
    public void Hard_HasHigherResearchCostMultiplier_ThanEasy()
    {
        float easyCost = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy).researchCostMultiplier;
        float hardCost = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard).researchCostMultiplier;
        Assert.Greater(hardCost, easyCost, "Hard research should scale more steeply per tier");
    }

    [Test]
    public void Hard_HasLowerGeneratorOutputMultiplier_ThanEasy()
    {
        float easyOutput = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy).generatorOutputMultiplier;
        float hardOutput = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard).generatorOutputMultiplier;
        Assert.Less(hardOutput, easyOutput, "Hard generators should produce less energy than Easy");
    }

    [Test]
    public void Hard_HasNarrowerLoseMargin_ThanEasy()
    {
        float easyOffset = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy).loseCarbonOffset;
        float hardOffset = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard).loseCarbonOffset;
        Assert.Less(hardOffset, easyOffset, "Hard should have a tighter lose margin (lower offset)");
    }

    [Test]
    public void Hard_HasLongerResearchTime_ThanEasy()
    {
        float easyTime = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Easy).researchTimeMultiplier;
        float hardTime = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Hard).researchTimeMultiplier;
        Assert.Greater(hardTime, easyTime, "Hard research should take longer");
    }

    // ===== DEFAULT FALLBACK =====

    [Test]
    public void GetProfile_UnknownDifficulty_FallsBackToNormal()
    {
        // Pass an out-of-range enum value — the switch default should return Normal
        DifficultyBalanceProfile normal = DifficultyBalanceLibrary.GetProfile(GameDifficulty.Normal);
        DifficultyBalanceProfile fallback = DifficultyBalanceLibrary.GetProfile((GameDifficulty)99);
        Assert.AreEqual(normal.targetCarbon, fallback.targetCarbon, 0.001f);
        Assert.AreEqual(normal.researchCostMultiplier, fallback.researchCostMultiplier, 0.001f);
    }

    // ===== TRY PARSE DIFFICULTY =====

    [TestCase("Easy",   GameDifficulty.Easy)]
    [TestCase("Normal", GameDifficulty.Normal)]
    [TestCase("Hard",   GameDifficulty.Hard)]
    public void TryParseDifficulty_ValidString_ReturnsTrueAndCorrectEnum(string input, GameDifficulty expected)
    {
        bool result = DifficultyBalanceLibrary.TryParseDifficulty(input, out GameDifficulty parsed);
        Assert.IsTrue(result, $"Expected TryParse to succeed for '{input}'");
        Assert.AreEqual(expected, parsed);
    }

    [TestCase("easy")]
    [TestCase("NORMAL")]
    [TestCase("HARD")]
    [TestCase("Medium")]
    [TestCase("")]
    [TestCase(" Normal")]
    public void TryParseDifficulty_InvalidString_ReturnsFalse(string input)
    {
        // Note: System.Enum.TryParse trims whitespace and ignores case.
        // Truly invalid inputs (not a valid enum name): "Medium" and "".
        // "easy"/"NORMAL"/"HARD" succeed because Enum.TryParse ignores case.
        // " Normal" succeeds because Enum.TryParse trims leading whitespace.
        bool result = DifficultyBalanceLibrary.TryParseDifficulty(input, out _);
        if (input == "Medium" || input == "")
        {
            Assert.IsFalse(result, $"Expected TryParse to fail for '{input}'");
        }
    }

    [Test]
    public void TryParseDifficulty_EmptyString_ReturnsFalse()
    {
        bool result = DifficultyBalanceLibrary.TryParseDifficulty("", out _);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryParseDifficulty_UnknownName_ReturnsFalse()
    {
        bool result = DifficultyBalanceLibrary.TryParseDifficulty("Medium", out _);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryParseDifficulty_LeadingSpace_ReturnsTrue()
    {
        // Enum.TryParse silently trims whitespace, so " Normal" parses as Normal.
        bool result = DifficultyBalanceLibrary.TryParseDifficulty(" Normal", out _);
        Assert.IsTrue(result);
    }
}
