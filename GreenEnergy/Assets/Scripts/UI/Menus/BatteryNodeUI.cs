using UnityEngine;

/// <summary>
/// Concrete UI component for a single battery storage research node (e.g. "Battery Tier 2").
/// Attach to a button GameObject in the research menu hierarchy.
/// Call Setup() with the matching BatteryNode to bind the data.
/// </summary>
public class BatteryNodeUI : ResearchNodeUIBase
{
    private BatteryNode node;

    /// <summary>Binds this UI button to the given battery node and refreshes the display.</summary>
    public void Setup(BatteryNode batteryNode)
    {
        node = batteryNode;
        SetupCommon($"Battery Tier {node.tier}", node.energyCost);
    }

    /// <inheritdoc/>
    protected override bool HasNode() => node != null;

    /// <inheritdoc/>
    protected override bool IsUnlocked() => node != null && node.isUnlocked;

    /// <inheritdoc/>
    protected override bool IsResearching() => node != null && node.isResearching;

    /// <inheritdoc/>
    protected override float GetProgress() => node != null ? node.researchProgress : 0f;

    /// <inheritdoc/>
    protected override void OnResearchClicked()
    {
        if (ResearchManager.Instance != null)
            ResearchManager.Instance.StartBatteryResearch(node.tier);
    }
}
