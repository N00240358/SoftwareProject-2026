using UnityEngine;

/// <summary>
/// Concrete UI component for a single generator research node (e.g. "Wind Tier 3").
/// Attach to a button GameObject in the research menu hierarchy.
/// Call Setup() with the matching ResearchNode to bind the data.
/// </summary>
public class ResearchNodeUI : ResearchNodeUIBase
{
    private ResearchNode node;

    /// <summary>Binds this UI button to the given research node and refreshes the display.</summary>
    public void Setup(ResearchNode researchNode)
    {
        node = researchNode;
        SetupCommon($"{node.generatorType} Tier {node.tier}", node.energyCost);
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
            ResearchManager.Instance.StartResearch(node.nodeId);
    }
}
