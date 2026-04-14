using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abstract base for research node UI buttons — covers both generator tiers and battery tiers.
/// Subclasses bind a specific node type and implement the abstract state queries.
/// Common title/cost/progress/locked-overlay logic is handled here.
/// </summary>
public abstract class ResearchNodeUIBase : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text costText;
    public TMP_Text progressText;
    public Button researchButton;
    public Image lockedOverlay;

    protected virtual void Update()
    {
        if (HasNode())
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Wires up shared UI references, sets title/cost labels, and does the first UpdateUI() pass.
    /// Call this from your subclass's Setup() method after binding the node.
    /// </summary>
    protected void SetupCommon(string title, float cost)
    {
        ResolveSharedReferences();

        if (lockedOverlay != null)
        {
            lockedOverlay.raycastTarget = false;
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (costText != null)
        {
            costText.text = $"Cost: {(int)cost}";
        }

        if (researchButton != null)
        {
            researchButton.onClick.RemoveListener(OnResearchClicked);
            researchButton.onClick.AddListener(OnResearchClicked);
        }

        UpdateUI();
    }

    /// <summary>
    /// Refreshes button interactability, progress text, and locked overlay based on current node state.
    /// Called every frame from Update() while the node is bound.
    /// </summary>
    protected void UpdateUI()
    {
        if (!HasNode())
        {
            return;
        }

        if (researchButton != null)
        {
            researchButton.interactable = !IsUnlocked() && !IsResearching();
        }

        if (progressText != null)
        {
            if (IsUnlocked())
            {
                progressText.text = "Unlocked";
            }
            else if (IsResearching())
            {
                progressText.text = $"Researching {Mathf.FloorToInt(GetProgress() * 100)}%";
            }
            else
            {
                progressText.text = "Available";
            }
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.gameObject.SetActive(!IsUnlocked());
        }
    }

    protected virtual void OnDestroy()
    {
        if (researchButton != null)
        {
            researchButton.onClick.RemoveListener(OnResearchClicked);
        }
    }

    /// <summary>
    /// Populates any null serialized references by borrowing them from a sibling
    /// <see cref="ResearchNodeUIBase"/> on the same GameObject. This lets two node-UI
    /// components (e.g. ResearchNodeUI + BatteryNodeUI) share a single set of UI widgets
    /// without duplicate inspector assignments.
    /// </summary>
    private void ResolveSharedReferences()
    {
        if (titleText != null && costText != null && progressText != null && researchButton != null && lockedOverlay != null)
        {
            return;
        }

        ResearchNodeUIBase[] siblings = GetComponents<ResearchNodeUIBase>();
        foreach (ResearchNodeUIBase sibling in siblings)
        {
            if (sibling == this)
            {
                continue;
            }

            if (titleText == null)
            {
                titleText = sibling.titleText;
            }

            if (costText == null)
            {
                costText = sibling.costText;
            }

            if (progressText == null)
            {
                progressText = sibling.progressText;
            }

            if (researchButton == null)
            {
                researchButton = sibling.researchButton;
            }

            if (lockedOverlay == null)
            {
                lockedOverlay = sibling.lockedOverlay;
            }
        }
    }

    /// <summary>Returns true if the subclass has bound a non-null node.</summary>
    protected abstract bool HasNode();
    /// <summary>Returns true if the bound node has been fully researched.</summary>
    protected abstract bool IsUnlocked();
    /// <summary>Returns true if research is currently in progress on the bound node.</summary>
    protected abstract bool IsResearching();
    /// <summary>Returns the current research progress as a 0–1 fraction.</summary>
    protected abstract float GetProgress();
    /// <summary>Called when the player clicks the research button; should call the appropriate ResearchManager method.</summary>
    protected abstract void OnResearchClicked();
}
