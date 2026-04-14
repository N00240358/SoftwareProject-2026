using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies theme styling to the Build Menu panel and its generator buttons.
/// Attach to the same GameObject as <see cref="BuildMenuController"/> and call
/// <see cref="ApplyBuildMenuStyling"/> from <c>UIManager.ApplyMenuStyling()</c>.
/// </summary>
public class BuildMenuStyler : MonoBehaviour
{
    /// <summary>
    /// Styles the build menu panel and all generator buttons.
    /// Buttons are color-coded green (affordable) or red (too expensive) based on current energy.
    /// </summary>
    public void ApplyBuildMenuStyling(BuildMenuController buildMenu)
    {
        if (buildMenu == null) return;
        
        // Style menu panel
        Image panelImage = buildMenu.GetComponent<Image>();
        if (panelImage != null)
        {
            PanelStyle.ApplyDarkPanelStyle(panelImage);
        }
        
        // Style layout group
        VerticalLayoutGroup layout = buildMenu.GetComponentInChildren<VerticalLayoutGroup>();
        if (layout != null)
        {
            PanelStyle.ConfigureVerticalLayout(layout);
        }
        
        // Style all buttons and their costs
        Button[] buildButtons = buildMenu.GetComponentsInChildren<Button>();
        foreach (Button btn in buildButtons)
        {
            if (btn == null) continue;
            
            // Check if this is a generator button by looking for cost text
            TextMeshProUGUI costText = FindCostTextForButton(btn);
            
            ButtonStyle.ApplyPrimaryStyle(btn);
            
            // Color-code button: green if affordable, red if not
            if (costText != null)
            {
                UpdateButtonAffordabilityColor(btn, costText);
            }
        }
    }
    
    /// <summary>
    /// Searches the button's sibling hierarchy for a TextMeshPro element whose text
    /// contains "Cost" or "Energy". Returns null if none is found.
    /// </summary>
    private TextMeshProUGUI FindCostTextForButton(Button button)
    {
        // Look for text component in button's siblings or parent
        Transform parent = button.transform.parent;
        if (parent == null) return null;
        
        // Search for a text element near the button that contains "Cost"
        foreach (TextMeshProUGUI text in parent.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (text.text.Contains("Cost") || text.text.Contains("Energy"))
                return text;
        }
        
        return null;
    }
    
    /// <summary>
    /// Colors <paramref name="costText"/> green and enables the button when the player can afford
    /// the cost, or colors it red and disables the button when they cannot.
    /// Parses the cost from the first space-delimited token in <paramref name="costText"/>.
    /// No-ops if parsing fails or <see cref="GameManager.Instance"/> is null.
    /// </summary>
    private void UpdateButtonAffordabilityColor(Button button, TextMeshProUGUI costText)
    {
        // Parse cost from text if possible
        string costStr = costText.text;
        
        // If we can't parse, just use default style
        if (!int.TryParse(costStr.Split(' ')[0], out int cost))
        {
            return;
        }
        
        // Check if player can afford (would need access to GameManager)
        if (GameManager.Instance != null && GameManager.Instance.currentEnergy < cost)
        {
            // Too expensive - mute the button
            costText.color = UITheme.ColorAccentRed;
            button.interactable = false;
        }
        else
        {
            // Affordable - highlight in green
            costText.color = UITheme.ColorAccentGreen;
            button.interactable = true;
        }
    }
}
