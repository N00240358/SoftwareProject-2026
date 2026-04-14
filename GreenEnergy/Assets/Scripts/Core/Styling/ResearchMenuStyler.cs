using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies theme styling to the Research Menu panel, all <see cref="ResearchNodeUI"/> buttons,
/// and all <see cref="BatteryNodeUI"/> buttons. Locked nodes are visually dimmed (30% opacity).
/// Called from <c>UIManager.ApplyMenuStyling()</c>.
/// </summary>
public class ResearchMenuStyler : MonoBehaviour
{
    /// <summary>
    /// Styles the research menu panel plus all ResearchNodeUI and BatteryNodeUI buttons.
    /// Locked nodes are dimmed; buttons use color to reflect interactable/locked/researching states.
    /// </summary>
    public void ApplyResearchMenuStyling(ResearchMenuController researchMenu)
    {
        if (researchMenu == null) return;
        
        // Style menu panel
        Image panelImage = researchMenu.GetComponent<Image>();
        if (panelImage != null)
        {
            PanelStyle.ApplyDarkPanelStyle(panelImage);
        }
        
        // Style layout
        VerticalLayoutGroup layout = researchMenu.GetComponentInChildren<VerticalLayoutGroup>();
        if (layout != null)
        {
            PanelStyle.ConfigureVerticalLayout(layout);
        }
        
        // Style all research node buttons
        ResearchNodeUI[] nodes = researchMenu.GetComponentsInChildren<ResearchNodeUI>();
        foreach (ResearchNodeUI node in nodes)
        {
            ApplyNodeStyling(node);
        }
        
        // Style battery tier buttons
        BatteryNodeUI[] batteryNodes = researchMenu.GetComponentsInChildren<BatteryNodeUI>();
        foreach (BatteryNodeUI batteryNode in batteryNodes)
        {
            ApplyBatteryNodeStyling(batteryNode);
        }
    }
    
    /// <summary>
    /// Styles a single <see cref="ResearchNodeUI"/>: applies button colors, sets progress-text
    /// to primary white at 20px, and tints any child Image named "Progress" with accent green.
    /// </summary>
    private void ApplyNodeStyling(ResearchNodeUI node)
    {
        if (node == null) return;
        
        Button nodeButton = node.GetComponent<Button>();
        if (nodeButton == null) return;
        
        // Determine node state and apply appropriate styling
        ApplyNodeButtonStyle(nodeButton, node);
        
        // Style progress text if exists
        TextMeshProUGUI progressText = node.GetComponentInChildren<TextMeshProUGUI>();
        if (progressText != null)
        {
            progressText.color = UITheme.ColorTextPrimary;
            progressText.fontSize = 20;
        }
        
        // Style progress bar if exists
        Image progressBar = node.GetComponentInChildren<Image>();
        if (progressBar != null && progressBar.name.Contains("Progress"))
        {
            progressBar.color = UITheme.ColorAccentGreen;
        }
    }
    
    /// <summary>
    /// Styles a single <see cref="BatteryNodeUI"/>: applies button colors and sets the tier
    /// label to primary white at 20px. Delegates button-color logic to <see cref="ApplyNodeButtonStyle"/>.
    /// </summary>
    private void ApplyBatteryNodeStyling(BatteryNodeUI batteryNode)
    {
        if (batteryNode == null) return;
        
        Button nodeButton = batteryNode.GetComponent<Button>();
        if (nodeButton == null) return;
        
        ApplyNodeButtonStyle(nodeButton, batteryNode);
        
        // Style tier text
        TextMeshProUGUI tierText = batteryNode.GetComponentInChildren<TextMeshProUGUI>();
        if (tierText != null)
        {
            tierText.color = UITheme.ColorTextPrimary;
            tierText.fontSize = 20;
        }
    }
    
    /// <summary>
    /// Applies the standard research-node color block to <paramref name="button"/>.
    /// If the button is non-interactable (locked), the Image background is dimmed to 30% opacity.
    /// </summary>
    private void ApplyNodeButtonStyle(Button button, MonoBehaviour nodeComponent)
    {
        if (button == null) return;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) return;
        
        // Set colors based on node state (would need to check ResearchNodeUIBase for state logic)
        // For now, apply a standard style with color variations for interactivity
        
        ColorBlock colors = button.colors;
        colors.normalColor = UITheme.ColorButtonNormal;
        colors.highlightedColor = UITheme.ColorButtonHover;
        colors.pressedColor = UITheme.ColorAccentCyan;
        colors.selectedColor = UITheme.ColorAccentGreen;
        colors.disabledColor = UITheme.ColorTextSecondary;
        button.colors = colors;
        
        // If button is disabled, it's locked
        if (!button.interactable)
        {
            buttonImage.color = UITheme.WithAlpha(UITheme.ColorButtonNormal, UITheme.OpacityLow);
        }
    }
}
