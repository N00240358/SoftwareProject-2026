using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Defines a single entry in the PDA encyclopedia (loaded from CSV).
/// Entries can be tier-based (unlocked by research), milestone-based (unlocked by generator count),
/// or battery-based. The entryId format encodes the unlock rule: e.g. "Solar_Tier3" or "Wind_Milestone25".
/// </summary>
[Serializable]
public class PDAEntryDefinition
{
    public string entryId;        // Unique identifier; format encodes unlock type (e.g. "Solar_Tier3")
    public string category;
    public string title;
    public string content;
    public int sortOrder;
    public bool enabled;
    public bool isTierEntry;
    public bool isMilestoneEntry;
    public bool isBatteryEntry;
    public MapGenerator.GeneratorType generatorType;
    public int tier;
    public int milestoneThreshold;
    public string unlockHint;
}

/// <summary>
/// Groups PDA entries under a shared category name (e.g. "Solar", "Wind") for sidebar display.
/// Built at runtime from the parsed CSV; not persisted.
/// </summary>
[Serializable]
public class PDAEntryCategory
{
    public string categoryName;
    public List<PDAEntryDefinition> entries = new List<PDAEntryDefinition>();
}

/// <summary>
/// Minimal save record for a single PDA entry. Only unlocked entries are stored;
/// locked entries are simply absent from the save data.
/// </summary>
[Serializable]
public class PDAEntryUnlockSaveData
{
    public string entryId;   // The entry's ID
    public bool isUnlocked;  // Always true when saved (we only save unlocked entries)
}

/// <summary>
/// Tracks which PDA encyclopedia entries have been unlocked and fires an event when the state changes.
/// Entry data is loaded from a CSV in Resources/ at startup. Unlock events are triggered by
/// ResearchManager (tier research) and GeneratorManager (generator count milestones).
/// Persists across scene loads via DontDestroyOnLoad. Singleton accessed via PDAProgressionSystem.Instance.
/// </summary>
public class PDAProgressionSystem : MonoBehaviour
{
    private static PDAProgressionSystem instance;
    private static bool isShuttingDown;
    private const string CSV_RESOURCE_NAME = "PDAEntries";

    [Header("CSV Data")]
    [SerializeField] [Tooltip("CSV file containing PDA entry data (as TextAsset from Resources folder)")] public TextAsset pdaEntriesCsv;

    private readonly List<PDAEntryDefinition> allEntries = new List<PDAEntryDefinition>();
    private readonly List<PDAEntryCategory> categories = new List<PDAEntryCategory>();
    private readonly Dictionary<string, PDAEntryDefinition> entriesById = new Dictionary<string, PDAEntryDefinition>();
    private readonly HashSet<string> unlockedEntryIds = new HashSet<string>();

    /// <summary>
    /// Main singleton accessor. Creates a new PDAProgressionSystem GameObject if none exists in the scene.
    /// Use ExistingInstance if you want a null return instead of auto-creation.
    /// </summary>
    public static PDAProgressionSystem Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            if (isShuttingDown)
            {
                return null;
            }

            instance = FindFirstObjectByType<PDAProgressionSystem>();
            if (instance != null)
            {
                return instance;
            }

            if (!Application.isPlaying)
            {
                return null;
            }

            GameObject systemGO = new GameObject("PDAProgressionSystem");
            instance = systemGO.AddComponent<PDAProgressionSystem>();
            return instance;
        }
    }

    /// <summary>
    /// Returns the existing instance without creating one. Returns null if no PDAProgressionSystem is in the scene.
    /// Use this when you want to avoid inadvertently spawning the system during teardown.
    /// </summary>
    public static PDAProgressionSystem ExistingInstance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            if (isShuttingDown)
            {
                return null;
            }

            instance = FindFirstObjectByType<PDAProgressionSystem>();
            return instance;
        }
    }

    public event Action OnProgressionChanged;

    private void Awake()
    {
        isShuttingDown = false;

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadEntries();
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Loads CSV data from Resources if not already loaded, then parses it and builds the category cache.
    /// Safe to call multiple times — exits early once data is present.
    /// </summary>
    private void LoadEntries()
    {
        if (allEntries.Count > 0)
        {
            return;
        }

        if (pdaEntriesCsv == null)
        {
            pdaEntriesCsv = Resources.Load<TextAsset>(CSV_RESOURCE_NAME);
        }

        if (pdaEntriesCsv == null)
        {
            Debug.LogWarning("PDAProgressionSystem: PDAEntries CSV not found in Resources.");
            return;
        }

        ParseCsv(pdaEntriesCsv.text);
        BuildCategoryCache();
    }

    /// <summary>
    /// Splits raw CSV text into rows, skips the header row, and calls
    /// <see cref="BuildEntryDefinition"/> for each data row with at least 3 columns.
    /// Populates <c>allEntries</c> and <c>entriesById</c>.
    /// </summary>
    private void ParseCsv(string csvText)
    {
        allEntries.Clear();
        entriesById.Clear();

        if (string.IsNullOrWhiteSpace(csvText))
        {
            return;
        }

        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool headerSkipped = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!headerSkipped)
            {
                headerSkipped = true;
                if (line.StartsWith("entryId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            List<string> columns = ParseCsvLine(line);
            if (columns.Count < 3)
            {
                continue;
            }

            PDAEntryDefinition entry = BuildEntryDefinition(columns);
            if (entry == null || string.IsNullOrWhiteSpace(entry.entryId))
            {
                continue;
            }

            allEntries.Add(entry);
            entriesById[entry.entryId] = entry;
        }
    }

    /// <summary>
    /// Converts one parsed CSV row (columns list) into a <see cref="PDAEntryDefinition"/>.
    /// Supports both the full 7-column format (entryId, category, title, content, _, sortOrder, enabled)
    /// and the compact 3-column format (entryId, title, content), inferring missing fields from the entryId.
    /// Returns null if the row is malformed.
    /// </summary>
    private PDAEntryDefinition BuildEntryDefinition(IReadOnlyList<string> columns)
    {
        PDAEntryDefinition entry = new PDAEntryDefinition();
        entry.entryId = columns[0].Trim();

        if (columns.Count >= 7)
        {
            entry.category = columns[1].Trim();
            entry.title = columns[2].Trim();
            entry.content = columns[3].Trim();
            entry.sortOrder = ParseInt(columns[5], ParseSortOrderFromEntryId(entry.entryId));
            entry.enabled = ParseBool(columns[6], true);
        }
        else
        {
            entry.category = InferCategoryFromEntryId(entry.entryId);
            entry.title = columns[1].Trim();
            entry.content = columns[2].Trim();
            entry.sortOrder = ParseSortOrderFromEntryId(entry.entryId);
            entry.enabled = true;
        }

        PopulateEntryMetadata(entry);
        return entry;
    }

    /// <summary>
    /// Derives <c>isTierEntry</c>, <c>isMilestoneEntry</c>, <c>isBatteryEntry</c>,
    /// <c>generatorType</c>, <c>tier</c>, <c>milestoneThreshold</c>, and <c>unlockHint</c>
    /// by parsing the <c>entryId</c> format: "TypeName_Tier#" or "TypeName_Milestone#".
    /// </summary>
    private void PopulateEntryMetadata(PDAEntryDefinition entry)
    {
        if (entry == null)
        {
            return;
        }

        string[] parts = entry.entryId.Split('_');
        if (parts.Length < 2)
        {
            entry.category = string.IsNullOrWhiteSpace(entry.category) ? "General" : entry.category;
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.category))
        {
            entry.category = parts[0];
        }

        string suffix = parts[1];
        if (suffix.StartsWith("Tier", StringComparison.OrdinalIgnoreCase))
        {
            entry.isTierEntry = true;
            entry.isMilestoneEntry = false;
            entry.isBatteryEntry = parts[0].Equals("Battery", StringComparison.OrdinalIgnoreCase);
            entry.generatorType = ParseGeneratorType(parts[0]);
            entry.tier = ParseInt(suffix.Substring(4), 1);
            entry.unlockHint = entry.isBatteryEntry
                ? $"Unlocks when Battery tier {entry.tier} is researched."
                : $"Unlocks when {parts[0]} tier {entry.tier} is researched.";
        }
        else if (suffix.StartsWith("Milestone", StringComparison.OrdinalIgnoreCase))
        {
            entry.isMilestoneEntry = true;
            entry.isTierEntry = false;
            entry.generatorType = ParseGeneratorType(parts[0]);
            entry.milestoneThreshold = ParseInt(suffix.Substring(9), 0);
            entry.unlockHint = $"Unlocks when {entry.milestoneThreshold} {parts[0]} generators are installed.";
        }
        else
        {
            entry.unlockHint = "Unlock requirement unavailable.";
        }
    }

    /// <summary>
    /// Groups all enabled entries into <see cref="PDAEntryCategory"/> objects in CSV row order,
    /// then sorts entries within each category by sortOrder then title.
    /// Rebuilds <c>categories</c> from scratch each call.
    /// </summary>
    private void BuildCategoryCache()
    {
        categories.Clear();

        Dictionary<string, PDAEntryCategory> categoryMap = new Dictionary<string, PDAEntryCategory>();
        List<string> categoryOrder = new List<string>();

        foreach (PDAEntryDefinition entry in allEntries.Where(e => e != null && e.enabled))
        {
            if (!categoryMap.TryGetValue(entry.category, out PDAEntryCategory category))
            {
                category = new PDAEntryCategory { categoryName = entry.category };
                categoryMap.Add(entry.category, category);
                categoryOrder.Add(entry.category);
            }

            category.entries.Add(entry);
        }

        foreach (PDAEntryCategory category in categoryMap.Values)
        {
            category.entries.Sort((left, right) =>
            {
                int sortCompare = left.sortOrder.CompareTo(right.sortOrder);
                if (sortCompare != 0)
                {
                    return sortCompare;
                }

                return string.Compare(left.title, right.title, StringComparison.OrdinalIgnoreCase);
            });
        }

        foreach (string categoryName in categoryOrder)
        {
            categories.Add(categoryMap[categoryName]);
        }
    }

    /// <summary>Returns all entry categories in display order (derived from CSV row order).</summary>
    public IReadOnlyList<PDAEntryCategory> GetCategories()
    {
        return categories;
    }

    /// <summary>Returns the definition for the given entry ID, or null if not found.</summary>
    public PDAEntryDefinition GetEntry(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return null;
        }

        return entriesById.TryGetValue(entryId, out PDAEntryDefinition entry) ? entry : null;
    }

    /// <summary>Returns true if the given entry has been unlocked.</summary>
    public bool IsUnlocked(string entryId)
    {
        return !string.IsNullOrWhiteSpace(entryId) && unlockedEntryIds.Contains(entryId);
    }

    /// <summary>
    /// Marks an entry as unlocked. Returns false if the entry doesn't exist, is disabled,
    /// or was already unlocked. Fires OnProgressionChanged unless notify is false.
    /// </summary>
    public bool TryUnlockEntry(string entryId, bool notify = true)
    {
        PDAEntryDefinition entry = GetEntry(entryId);
        if (entry == null || !entry.enabled)
        {
            return false;
        }

        if (!unlockedEntryIds.Add(entryId))
        {
            return false;
        }

        OnProgressionChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Replaces the full set of unlocked entries (used when loading a save).
    /// Only IDs that exist in the CSV are kept; unknown IDs are discarded.
    /// </summary>
    public void SetUnlockedEntries(IEnumerable<string> entryIds)
    {
        unlockedEntryIds.Clear();

        if (entryIds != null)
        {
            foreach (string entryId in entryIds)
            {
                if (!string.IsNullOrWhiteSpace(entryId) && entriesById.ContainsKey(entryId))
                {
                    unlockedEntryIds.Add(entryId);
                }
            }
        }

        OnProgressionChanged?.Invoke();
    }

    /// <summary>
    /// Returns a serializable list of all currently unlocked entry IDs, sorted for deterministic output.
    /// Used by SaveLoadSystem when writing the save file.
    /// </summary>
    public List<PDAEntryUnlockSaveData> GetSaveData()
    {
        List<PDAEntryUnlockSaveData> saveData = new List<PDAEntryUnlockSaveData>();

        foreach (string entryId in unlockedEntryIds.OrderBy(id => id))
        {
            saveData.Add(new PDAEntryUnlockSaveData
            {
                entryId = entryId,
                isUnlocked = true
            });
        }

        return saveData;
    }

    /// <summary>
    /// Called by ResearchManager when a generator research tier completes.
    /// Attempts to unlock the matching PDA entry (e.g. "Solar_Tier3").
    /// </summary>
    public void HandleResearchUnlocked(MapGenerator.GeneratorType type, int tier)
    {
        TryUnlockEntry($"{type}_Tier{tier}");
    }

    /// <summary>
    /// Called by ResearchManager when a battery research tier completes.
    /// Attempts to unlock the matching PDA entry (e.g. "Battery_Tier2").
    /// </summary>
    public void HandleBatteryUnlocked(int tier)
    {
        TryUnlockEntry($"Battery_Tier{tier}");
    }

    /// <summary>
    /// Called by GeneratorManager when a generator type reaches a count milestone (25/50/75/100).
    /// Unlocks all milestone entries whose threshold has been reached or exceeded.
    /// </summary>
    public void HandleGeneratorMilestone(MapGenerator.GeneratorType type, int count)
    {
        int[] milestoneThresholds = { 25, 50, 75, 100 };

        foreach (int threshold in milestoneThresholds)
        {
            if (count >= threshold)
            {
                TryUnlockEntry($"{type}_Milestone{threshold}");
            }
        }
    }

    /// <summary>
    /// Re-checks all entries against the current research and generator state and unlocks any
    /// that should already be unlocked (e.g. after loading a save).
    /// Pass emitEvent = false to batch multiple syncs without triggering multiple UI refreshes.
    /// </summary>
    public void SyncWithCurrentGameState(bool emitEvent = true)
    {
        if (ResearchManager.Instance != null)
        {
            foreach (PDAEntryDefinition entry in allEntries)
            {
                if (entry == null || !entry.enabled)
                {
                    continue;
                }

                if (entry.isTierEntry)
                {
                    if (entry.isBatteryEntry)
                    {
                        BatteryNode batteryNode = ResearchManager.Instance.GetBatteryNode(entry.tier);
                        if (batteryNode != null && batteryNode.isUnlocked)
                        {
                            TryUnlockEntry(entry.entryId, false);
                        }
                    }
                    else if (ResearchManager.Instance.IsUnlocked(entry.generatorType, entry.tier))
                    {
                        TryUnlockEntry(entry.entryId, false);
                    }
                }
            }
        }

        if (GeneratorManager.Instance != null)
        {
            foreach (PDAEntryDefinition entry in allEntries)
            {
                if (entry == null || !entry.enabled || !entry.isMilestoneEntry)
                {
                    continue;
                }

                if (GeneratorManager.Instance.GetGeneratorCount(entry.generatorType) >= entry.milestoneThreshold)
                {
                    TryUnlockEntry(entry.entryId, false);
                }
            }
        }

        if (emitEvent)
        {
            OnProgressionChanged?.Invoke();
        }
    }

    /// <summary>Returns the human-readable unlock hint for the given entry (e.g. "Unlocks when Solar tier 3 is researched.").</summary>
    public string GetUnlockHint(string entryId)
    {
        PDAEntryDefinition entry = GetEntry(entryId);
        return entry != null ? entry.unlockHint : "Unknown unlock requirement.";
    }

    /// <summary>Safe case-insensitive parse of a <see cref="MapGenerator.GeneratorType"/> string; defaults to Solar on failure.</summary>
    private static MapGenerator.GeneratorType ParseGeneratorType(string value)
    {
        if (Enum.TryParse(value, true, out MapGenerator.GeneratorType generatorType))
        {
            return generatorType;
        }

        return MapGenerator.GeneratorType.Solar;
    }

    /// <summary>Returns the prefix before the first underscore in <paramref name="entryId"/> as the category name, or "General" if none.</summary>
    private static string InferCategoryFromEntryId(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return "General";
        }

        int underscoreIndex = entryId.IndexOf('_');
        if (underscoreIndex <= 0)
        {
            return "General";
        }

        return entryId.Substring(0, underscoreIndex);
    }

    /// <summary>
    /// Derives a numeric sort order from the entryId:
    /// "…_Tier#" → the tier number; "…_Milestone#" → 1000 + threshold (sorts after tiers); all others → 0.
    /// </summary>
    private static int ParseSortOrderFromEntryId(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return 0;
        }

        if (entryId.IndexOf("Milestone", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int suffixIndex = entryId.IndexOf("Milestone", StringComparison.OrdinalIgnoreCase) + 9;
            if (suffixIndex < entryId.Length && int.TryParse(entryId.Substring(suffixIndex), out int milestoneOrder))
            {
                return 1000 + milestoneOrder;
            }
        }

        if (entryId.IndexOf("Tier", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int suffixIndex = entryId.IndexOf("Tier", StringComparison.OrdinalIgnoreCase) + 4;
            if (suffixIndex < entryId.Length && int.TryParse(entryId.Substring(suffixIndex), out int tierOrder))
            {
                return tierOrder;
            }
        }

        return 0;
    }

    /// <summary>Parses <paramref name="value"/> as an integer, returning <paramref name="fallback"/> on failure.</summary>
    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    /// <summary>Parses <paramref name="value"/> as a boolean, returning <paramref name="fallback"/> on failure.</summary>
    private static bool ParseBool(string value, bool fallback)
    {
        if (bool.TryParse(value, out bool parsed))
        {
            return parsed;
        }

        return fallback;
    }

    /// <summary>
    /// Splits a single CSV line into a list of field strings.
    /// Handles RFC-4180 quoted fields (commas inside quotes are preserved;
    /// doubled quotes inside a quoted field are unescaped to a single quote).
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        List<string> columns = new List<string>();
        StringBuilder current = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];

            if (character == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                columns.Add(current.ToString().Trim());
                current.Length = 0;
                continue;
            }

            current.Append(character);
        }

        columns.Add(current.ToString().Trim());
        return columns;
    }
}
