using UnityEngine;

/// <summary>
/// The three difficulty levels available to the player.
/// </summary>
public enum GameDifficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// A read-only snapshot of all balance values for one difficulty level.
/// GameManager reads this through CurrentDifficultyProfile; some fields can be
/// overridden by GameManager's inspector-exposed tuning fields.
/// </summary>
public struct DifficultyBalanceProfile
{
    public readonly float targetCarbon;               // Carbon level (ppm) the player must reach to win
    public readonly float carbonReductionMultiplier;  // How fast generators reduce carbon (ppm per unit of production per second)
    public readonly float underproductionThreshold;   // Minimum carbon-reduction rate before the penalty kicks in (0–1 scale)
    public readonly float underproductionPenaltyRate; // How fast carbon rises when production is below threshold (ppm per second)
    public readonly float loseCarbonOffset;           // Added to STARTING_CARBON (420) to set the lose threshold
    public readonly float researchTimeMultiplier;     // Multiplied onto every research node's base time
    public readonly float researchCostMultiplier;     // Scales the exponential cost increase per tier
    public readonly float generatorOutputMultiplier;  // Flat multiplier on every generator's base energy output
    public readonly float minimumRealTimeToWinSeconds;// Reserved for future use — minimum real seconds before a win is allowed

    public DifficultyBalanceProfile(
        float targetCarbon,
        float carbonReductionMultiplier,
        float underproductionThreshold,
        float underproductionPenaltyRate,
        float loseCarbonOffset,
        float researchTimeMultiplier,
        float researchCostMultiplier,
        float generatorOutputMultiplier,
        float minimumRealTimeToWinSeconds)
    {
        this.targetCarbon = targetCarbon;
        this.carbonReductionMultiplier = carbonReductionMultiplier;
        this.underproductionThreshold = underproductionThreshold;
        this.underproductionPenaltyRate = underproductionPenaltyRate;
        this.loseCarbonOffset = loseCarbonOffset;
        this.researchTimeMultiplier = researchTimeMultiplier;
        this.researchCostMultiplier = researchCostMultiplier;
        this.generatorOutputMultiplier = generatorOutputMultiplier;
        this.minimumRealTimeToWinSeconds = minimumRealTimeToWinSeconds;
    }
}

/// <summary>
/// Stores the pre-built balance profiles for each difficulty and provides lookup helpers.
/// All tuning values live here so balance changes are made in one place.
/// </summary>
public static class DifficultyBalanceLibrary
{
    // ===== DIFFICULTY PROFILES =====
    // Arguments match DifficultyBalanceProfile constructor order:
    // targetCarbon, carbonReductionMult, underproductionThreshold, underproductionPenalty,
    // loseCarbonOffset, researchTimeMult, researchCostMult, generatorOutputMult, minRealTimeToWin

    private static readonly DifficultyBalanceProfile easy = new DifficultyBalanceProfile(
        320f,      // win at 320 ppm (vs 300 on Normal) — easier target
        0.00095f,  // faster carbon reduction per unit of production
        0.55f,     // penalty only kicks in below 55% production threshold
        0.02f,     // slow carbon rise when underproducing
        180f,      // lose at 420 + 180 = 600 ppm — very forgiving
        2.2f,      // research completes faster (lower multiplier = shorter time)
        1.5f,      // research costs scale slower per tier
        0.85f,     // generators output 85% (slightly weaker to keep game paced)
        0f);       // no minimum real-time-to-win enforced

    private static readonly DifficultyBalanceProfile normal = new DifficultyBalanceProfile(
        300f,      // win at 300 ppm (pre-industrial baseline)
        0.00075f,  // standard carbon reduction rate
        0.50f,     // penalty kicks in below 50% production threshold
        0.03f,     // moderate carbon rise when underproducing
        160f,      // lose at 420 + 160 = 580 ppm
        2.9f,      // standard research time multiplier
        1.75f,     // standard research cost scaling
        0.75f,     // generators at 75% output
        0f);

    private static readonly DifficultyBalanceProfile hard = new DifficultyBalanceProfile(
        280f,      // win at 280 ppm — stricter target
        0.00062f,  // slower carbon reduction, harder to make progress
        0.45f,     // penalty kicks in below 45% production — less margin
        0.04f,     // faster carbon rise when underproducing
        140f,      // lose at 420 + 140 = 560 ppm — tighter loss margin
        3.7f,      // research takes longer
        2.0f,      // research costs scale steeply per tier
        0.65f,     // generators at 65% output — significantly weaker
        0f);

    // ===== LOOKUP METHODS =====

    /// <summary>
    /// Returns the balance profile for the given difficulty level.
    /// </summary>
    public static DifficultyBalanceProfile GetProfile(GameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case GameDifficulty.Easy:
                return easy;
            case GameDifficulty.Hard:
                return hard;
            case GameDifficulty.Normal:
            default:
                return normal;
        }
    }

    /// <summary>
    /// Tries to parse a difficulty from a saved string (e.g. "Normal").
    /// Returns false if the string doesn't match any known difficulty.
    /// </summary>
    public static bool TryParseDifficulty(string value, out GameDifficulty difficulty)
    {
        return System.Enum.TryParse(value, out difficulty) &&
               System.Enum.IsDefined(typeof(GameDifficulty), difficulty);
    }
}