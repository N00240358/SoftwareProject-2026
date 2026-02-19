using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Simplified PDA system - Educational content delivery
/// Shows entries as they unlock, player can re-read them
/// </summary>
[System.Serializable]
public class PDAEntry
{
    public string entryId;
    public string title;
    public string content;
    public AudioClip audioClip;
    public bool hasBeenViewed;
    public bool isUnlocked;
    
    public PDAEntry(string id, string entryTitle, string entryContent)
    {
        entryId = id;
        title = entryTitle;
        content = entryContent;
        hasBeenViewed = false;
        isUnlocked = false;
        audioClip = null;
    }
}

public class PDASystem : MonoBehaviour
{
    public static PDASystem Instance { get; private set; }
    
    // ===== UI REFERENCES =====
    [Header("Main PDA UI")]
    public GameObject pdaPanel; // Main PDA window
    public TMP_Text titleText; // Current entry title
    public TMP_Text contentText; // Current entry content
    public UnityEngine.UI.Button closeButton; // Close PDA button
    public UnityEngine.UI.Button audioButton; // Play audio button
    public UnityEngine.UI.Button nextButton; // Next entry button (optional)
    public UnityEngine.UI.Button previousButton; // Previous entry button (optional)
    
    [Header("Audio")]
    public AudioSource audioSource;
    
    // ===== INTERNAL DATA =====
    private Dictionary<string, PDAEntry> allEntries = new Dictionary<string, PDAEntry>();
    private List<string> unlockedEntryIds = new List<string>(); // Ordered list of unlocked entries
    private Queue<string> pendingEntries = new Queue<string>();
    private bool isShowingEntry = false;
    private int currentEntryIndex = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeEntries();
        SetupUI();
        
        if (pdaPanel != null)
        {
            pdaPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Show next pending entry if not currently showing one
        if (!isShowingEntry && pendingEntries.Count > 0)
        {
            string entryId = pendingEntries.Dequeue();
            ShowEntry(entryId);
        }
    }

    /// <summary>
    /// Sets up UI button listeners
    /// </summary>
    private void SetupUI()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePDA);
        }
        
        if (audioButton != null)
        {
            audioButton.onClick.AddListener(PlayAudio);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextEntry);
        }
        
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(ShowPreviousEntry);
        }
    }

    /// <summary>
    /// Initializes all 84 educational entries
    /// </summary>
    private void InitializeEntries()
    {
        CreateSolarEntries();
        CreateWindEntries();
        CreateHydroEntries();
        CreateTidalEntries();
        CreateNuclearEntries();
        CreateBatteryEntries();
    }

    private void CreateSolarEntries()
    {
        allEntries.Add("Solar_Tier1", new PDAEntry(
            "Solar_Tier1",
            "Solar Power: Getting Started",
            "Solar panels convert sunlight directly into electricity using photovoltaic cells. They're clean, renewable, and work best in areas with high sun exposure. Solar energy production varies with time of day and weather conditions, producing peak power at noon and nothing at night."
        ));
        
        for (int i = 2; i <= 10; i++)
        {
            allEntries.Add($"Solar_Tier{i}", new PDAEntry(
                $"Solar_Tier{i}",
                $"Solar Technology Tier {i}",
                $"Tier {i} solar panels feature improved efficiency with better photovoltaic cell technology, allowing for {i * 50}% more energy production compared to tier 1. Advanced materials and optimized panel angles maximize energy capture throughout the day."
            ));
        }
        
        CreateMilestoneEntries("Solar", "solar panels", "Solar farms");
    }

    private void CreateWindEntries()
    {
        allEntries.Add("Wind_Tier1", new PDAEntry(
            "Wind_Tier1",
            "Wind Power: Harnessing the Breeze",
            "Wind turbines convert kinetic energy from wind into electricity. They work best in open areas with consistent wind patterns. Wind energy is intermittent - production varies with wind speed and can fluctuate throughout the day."
        ));
        
        for (int i = 2; i <= 10; i++)
        {
            allEntries.Add($"Wind_Tier{i}", new PDAEntry(
                $"Wind_Tier{i}",
                $"Wind Technology Tier {i}",
                $"Tier {i} wind turbines feature larger rotor diameters and taller towers, capturing wind energy more efficiently. Advanced blade designs and smart controls optimize power generation across varying wind speeds."
            ));
        }
        
        CreateMilestoneEntries("Wind", "wind turbines", "Wind farms");
    }

    private void CreateHydroEntries()
    {
        allEntries.Add("Hydroelectric_Tier1", new PDAEntry(
            "Hydroelectric_Tier1",
            "Hydroelectric Power: Energy from Water",
            "Hydroelectric dams generate electricity by channeling flowing water through turbines. They provide consistent, reliable power and can only be built in suitable locations with water flow. Limited to 10 total installations due to environmental constraints."
        ));
        
        for (int i = 2; i <= 10; i++)
        {
            allEntries.Add($"Hydroelectric_Tier{i}", new PDAEntry(
                $"Hydroelectric_Tier{i}",
                $"Hydroelectric Technology Tier {i}",
                $"Tier {i} hydro turbines utilize advanced water flow optimization and more efficient generators. Modern fish-friendly designs minimize environmental impact while maximizing energy output."
            ));
        }
        
        CreateMilestoneEntries("Hydroelectric", "hydroelectric dams", "Hydroelectric installations");
    }

    private void CreateTidalEntries()
    {
        allEntries.Add("Tidal_Tier1", new PDAEntry(
            "Tidal_Tier1",
            "Tidal Power: Ocean Energy",
            "Tidal generators harness the power of ocean tides - one of the most predictable renewable energy sources. They work in coastal areas and produce power based on tidal cycles, with peak output during high and low tides."
        ));
        
        for (int i = 2; i <= 10; i++)
        {
            allEntries.Add($"Tidal_Tier{i}", new PDAEntry(
                $"Tidal_Tier{i}",
                $"Tidal Technology Tier {i}",
                $"Tier {i} tidal generators employ larger turbines and improved underwater durability. Advanced materials resist saltwater corrosion while capturing more energy from tidal flows."
            ));
        }
        
        CreateMilestoneEntries("Tidal", "tidal generators", "Tidal installations");
    }

    private void CreateNuclearEntries()
    {
        allEntries.Add("Nuclear_Tier1", new PDAEntry(
            "Nuclear_Tier1",
            "Nuclear Power: High-Density Energy",
            "Nuclear power plants generate massive amounts of electricity through controlled fission reactions. They produce consistent baseload power with zero carbon emissions, though they're expensive to build and require stable land. Modern designs emphasize safety and efficiency."
        ));
        
        for (int i = 2; i <= 10; i++)
        {
            allEntries.Add($"Nuclear_Tier{i}", new PDAEntry(
                $"Nuclear_Tier{i}",
                $"Nuclear Technology Tier {i}",
                $"Tier {i} nuclear reactors feature advanced safety systems and improved fuel efficiency. Next-generation designs reduce waste and enhance passive cooling, making nuclear power safer and more sustainable."
            ));
        }
        
        CreateMilestoneEntries("Nuclear", "nuclear reactors", "Nuclear plants");
    }

    private void CreateBatteryEntries()
    {
        for (int i = 1; i <= 10; i++)
        {
            allEntries.Add($"Battery_Tier{i}", new PDAEntry(
                $"Battery_Tier{i}",
                $"Energy Storage Tier {i}",
                $"Tier {i} battery technology provides {i * 1000} units of additional energy storage. Better storage allows you to save excess energy from solar and wind for use during low-production periods, making renewable energy more reliable."
            ));
        }
    }

    private void CreateMilestoneEntries(string type, string plural, string collective)
    {
        allEntries.Add($"{type}_Milestone25", new PDAEntry(
            $"{type}_Milestone25",
            $"{type} Achievement: 25 Installed",
            $"You've deployed 25 {plural}! {collective} at this scale demonstrate the viability of this renewable energy technology."
        ));
        
        allEntries.Add($"{type}_Milestone50", new PDAEntry(
            $"{type}_Milestone50",
            $"{type} Achievement: 50 Installed",
            $"50 {plural} deployed! This represents significant infrastructure investment in clean energy."
        ));
        
        allEntries.Add($"{type}_Milestone75", new PDAEntry(
            $"{type}_Milestone75",
            $"{type} Achievement: 75 Installed",
            $"75 {plural}! You're building the backbone of a renewable energy grid."
        ));
        
        allEntries.Add($"{type}_Milestone100", new PDAEntry(
            $"{type}_Milestone100",
            $"{type} Achievement: 100 Installed",
            $"100 {plural}! {collective} at this scale can power entire regions with clean, renewable energy."
        ));
    }

    /// <summary>
    /// Triggers an entry to be shown (queues it)
    /// </summary>
    public void TriggerEntry(string entryId)
    {
        if (!allEntries.ContainsKey(entryId))
        {
            Debug.LogWarning($"PDA entry {entryId} not found!");
            return;
        }
        
        PDAEntry entry = allEntries[entryId];
        if (!entry.isUnlocked)
        {
            entry.isUnlocked = true;
            unlockedEntryIds.Add(entryId); // Add to history
            pendingEntries.Enqueue(entryId); // Queue for popup
        }
    }

    /// <summary>
    /// Shows a specific entry
    /// </summary>
    public void ShowEntry(string entryId)
    {
        if (!allEntries.ContainsKey(entryId)) return;
        
        PDAEntry entry = allEntries[entryId];
        entry.hasBeenViewed = true;
        isShowingEntry = true;
        
        // Find index in unlocked list
        currentEntryIndex = unlockedEntryIds.IndexOf(entryId);
        
        // Open PDA panel
        if (pdaPanel != null) pdaPanel.SetActive(true);
        
        // Update content
        if (titleText != null) titleText.text = entry.title;
        if (contentText != null) contentText.text = entry.content;
        
        // Show/hide audio button
        if (audioButton != null)
        {
            audioButton.gameObject.SetActive(entry.audioClip != null);
        }
        
        // Update navigation buttons
        UpdateNavigationButtons();
        
        // Pause game
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Shows next entry in history
    /// </summary>
    public void ShowNextEntry()
    {
        if (currentEntryIndex < 0 || currentEntryIndex >= unlockedEntryIds.Count - 1) return;
        
        currentEntryIndex++;
        ShowEntry(unlockedEntryIds[currentEntryIndex]);
    }

    /// <summary>
    /// Shows previous entry in history
    /// </summary>
    public void ShowPreviousEntry()
    {
        if (currentEntryIndex <= 0) return;
        
        currentEntryIndex--;
        ShowEntry(unlockedEntryIds[currentEntryIndex]);
    }

    /// <summary>
    /// Updates next/previous button states
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (nextButton != null)
        {
            nextButton.interactable = (currentEntryIndex < unlockedEntryIds.Count - 1);
        }
        
        if (previousButton != null)
        {
            previousButton.interactable = (currentEntryIndex > 0);
        }
    }

    /// <summary>
    /// Closes the PDA
    /// </summary>
    public void ClosePDA()
    {
        isShowingEntry = false;
        if (pdaPanel != null) pdaPanel.SetActive(false);
        
        // Resume game
        Time.timeScale = 1f;
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Plays audio for current entry
    /// </summary>
    public void PlayAudio()
    {
        if (audioSource == null || currentEntryIndex < 0) return;
        
        string entryId = unlockedEntryIds[currentEntryIndex];
        if (allEntries.ContainsKey(entryId))
        {
            PDAEntry entry = allEntries[entryId];
            if (entry.audioClip != null)
            {
                audioSource.clip = entry.audioClip;
                audioSource.Play();
            }
        }
    }

    /// <summary>
    /// Assigns audio clip to an entry
    /// </summary>
    public void AssignAudioClip(string entryId, AudioClip clip)
    {
        if (allEntries.ContainsKey(entryId))
        {
            allEntries[entryId].audioClip = clip;
        }
    }

    /// <summary>
    /// Gets all unlocked entries
    /// </summary>
    public List<PDAEntry> GetUnlockedEntries()
    {
        return allEntries.Values.Where(e => e.isUnlocked).ToList();
    }

    /// <summary>
    /// Gets all entries
    /// </summary>
    public List<PDAEntry> GetAllEntries()
    {
        return allEntries.Values.ToList();
    }
}
