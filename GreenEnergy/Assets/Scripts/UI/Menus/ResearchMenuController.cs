using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the research menu panel. Finds each generator-type and battery button by name
/// inside the panel hierarchy and refreshes their labels each frame to show the next
/// available tier, energy cost, and in-progress percentage. Clicking a button calls
/// <see cref="ResearchManager.StartResearch"/> or <see cref="ResearchManager.StartBatteryResearch"/>.
/// <see cref="RefreshResearchMenuButtons"/> is polled by UIManager every frame while the menu is open.
/// </summary>
public class ResearchMenuController : MonoBehaviour
{
    [Header("Research Menu Panel")]
    public GameObject researchMenuPanel;

    private void OnDestroy()
    {
        // Clean up listeners if needed
    }

    /// <summary>Reserved for future initialization logic. Called by UIManager.SetupButtons().</summary>
    public void Initialize()
    {
        // Any initialization logic
    }

    /// <summary>
    /// Updates all research and battery buttons to reflect the current research state.
    /// Shows next available tier, current cost, and in-progress percentage.
    /// Called every frame while the research menu is open.
    /// </summary>
    public void RefreshResearchMenuButtons()
    {
        if (researchMenuPanel == null || ResearchManager.Instance == null) return;

        UpdateResearchButton(MapGenerator.GeneratorType.Solar);
        UpdateResearchButton(MapGenerator.GeneratorType.Wind);
        UpdateResearchButton(MapGenerator.GeneratorType.Hydroelectric);
        UpdateResearchButton(MapGenerator.GeneratorType.Tidal);
        UpdateResearchButton(MapGenerator.GeneratorType.Nuclear);
        UpdateBatteryResearchButton();
    }

    // ===== PRIVATE HELPERS =====

    /// <summary>
    /// Finds the button named "<c>{type}ResearchButton</c>" in the panel hierarchy and
    /// updates its label and interactable state to reflect the next available tier.
    /// If all tiers are unlocked the button is disabled and shows "MAX".
    /// </summary>
    /// <param name="type">Generator type whose research button should be updated.</param>
    private void UpdateResearchButton(MapGenerator.GeneratorType type)
    {
        string buttonName = $"{type}ResearchButton";
        Button button = FindButtonRecursive(researchMenuPanel.transform, buttonName);
        if (button == null) return;

        int nextTier = ResearchManager.Instance.GetNextAvailableTier(type);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (nextTier > 10)
        {
            if (label != null) label.text = $"{type}\nMAX\nCost: --";
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            return;
        }

        ResearchNode node = ResearchManager.Instance.GetNode($"{type}_Tier{nextTier}");
        bool researching = node != null && node.isResearching;
        if (label != null)
        {
            int cost = node != null ? Mathf.FloorToInt(node.energyCost) : 0;
            if (researching)
            {
                label.text = $"{type}\nTier {nextTier} ({Mathf.FloorToInt(node.researchProgress * 100)}%)\nCost: {cost}";
            }
            else
            {
                label.text = $"{type}\nTier {nextTier}\nCost: {cost}";
            }
        }

        button.interactable = node != null && !node.isUnlocked && !node.isResearching;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => StartResearchForType(type));
    }

    /// <summary>
    /// Finds the "BatteryResearchButton" and updates it to show the next battery tier,
    /// cost, and progress. Mirrors <see cref="UpdateResearchButton"/> but uses
    /// <see cref="BatteryNode"/> data instead of <see cref="ResearchNode"/>.
    /// </summary>
    private void UpdateBatteryResearchButton()
    {
        Button button = FindButtonRecursive(researchMenuPanel.transform, "BatteryResearchButton");
        if (button == null) return;

        int nextTier = ResearchManager.Instance.GetNextAvailableBatteryTier();
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (nextTier > 10)
        {
            if (label != null) label.text = "Battery\nMAX\nCost: --";
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            return;
        }

        BatteryNode node = ResearchManager.Instance.GetBatteryNode(nextTier);
        bool researching = node != null && node.isResearching;
        if (label != null)
        {
            int cost = node != null ? Mathf.FloorToInt(node.energyCost) : 0;
            if (researching)
            {
                label.text = $"Battery\nTier {nextTier} ({Mathf.FloorToInt(node.researchProgress * 100)}%)\nCost: {cost}";
            }
            else
            {
                label.text = $"Battery\nTier {nextTier}\nCost: {cost}";
            }
        }

        button.interactable = node != null && !node.isUnlocked && !node.isResearching;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(StartBatteryResearchForNextTier);
    }

    /// <summary>
    /// Determines the next locked tier for <paramref name="type"/> and starts researching it.
    /// Refreshes button state immediately so cost/progress feedback is instant.
    /// </summary>
    private void StartResearchForType(MapGenerator.GeneratorType type)
    {
        if (ResearchManager.Instance == null) return;
        int nextTier = ResearchManager.Instance.GetNextAvailableTier(type);
        if (nextTier <= 10)
        {
            ResearchManager.Instance.StartResearch($"{type}_Tier{nextTier}");
            RefreshResearchMenuButtons();
        }
    }

    /// <summary>
    /// Starts research on the next locked battery tier and immediately refreshes the button.
    /// </summary>
    private void StartBatteryResearchForNextTier()
    {
        if (ResearchManager.Instance == null) return;
        int nextTier = ResearchManager.Instance.GetNextAvailableBatteryTier();
        if (nextTier <= 10)
        {
            ResearchManager.Instance.StartBatteryResearch(nextTier);
            RefreshResearchMenuButtons();
        }
    }

    /// <summary>
    /// Depth-first search through the transform hierarchy for a child named <paramref name="name"/>.
    /// Returns the <see cref="Button"/> component on that child, or null if not found.
    /// Used because button depth in the panel hierarchy may vary.
    /// </summary>
    private Button FindButtonRecursive(Transform parent, string name)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.GetComponent<Button>();
            }

            Button nested = FindButtonRecursive(child, name);
            if (nested != null) return nested;
        }

        return null;
    }
}
