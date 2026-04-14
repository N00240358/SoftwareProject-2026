using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies modern minimal styling to the bottom stats bar.
/// Called from UIManager on initialization.
/// </summary>
public class StatsBarStyler : MonoBehaviour
{
    /// <summary>
    /// Applies theme colors to the stats bar text fields and fill bars.
    /// Energy fill bar starts cyan; carbon fill bar starts red. Both are updated dynamically by UIManager.
    /// </summary>
    public void ApplyStatsBarStyling(UIManager uiManager)
    {
        if (uiManager == null) return;
        
        // Style the stats bar container
        Image statsBarImage = uiManager.menuBarPanel?.GetComponent<Image>();
        if (statsBarImage != null)
        {
            statsBarImage.color = UITheme.ColorBackgroundMedium;
            
            Outline outline = statsBarImage.GetComponent<Outline>();
            if (outline == null)
                outline = statsBarImage.gameObject.AddComponent<Outline>();
            outline.effectColor = UITheme.ColorBorderDark;
            outline.effectDistance = new Vector2(1, 1);
        }
        
        // Style energy text
        if (uiManager.energyText != null)
        {
            uiManager.energyText.color = UITheme.ColorTextPrimary;
            uiManager.energyText.fontSize = 26;
        }
        
        // Style carbon text
        if (uiManager.carbonText != null)
        {
            uiManager.carbonText.color = UITheme.ColorTextPrimary;
            uiManager.carbonText.fontSize = 26;
        }
        
        // Style day/time text
        if (uiManager.dayText != null)
        {
            uiManager.dayText.color = UITheme.ColorTextPrimary;
            uiManager.dayText.fontSize = 26;
        }
        
        // Style time speed text
        if (uiManager.timeSpeedText != null)
        {
            uiManager.timeSpeedText.color = UITheme.ColorTextSecondary;
            uiManager.timeSpeedText.fontSize = 24;
        }
        
        // Style energy fill bar
        if (uiManager.energyFillBar != null)
        {
            uiManager.energyFillBar.color = UITheme.ColorAccentCyan;
            
            Image barBackground = uiManager.energyFillBar.GetComponent<Image>();
            if (barBackground != null)
                barBackground.color = UITheme.ColorBackgroundDark;
        }
        
        // Style carbon fill bar
        if (uiManager.carbonFillBar != null)
        {
            uiManager.carbonFillBar.color = UITheme.ColorAccentRed;
            
            Image barBackground = uiManager.carbonFillBar.GetComponent<Image>();
            if (barBackground != null)
                barBackground.color = UITheme.ColorBackgroundDark;
        }
    }
    
    /// <summary>
    /// Applies minimal button styling to the top-bar menu buttons (Build, Research, Settings, Time Control).
    /// </summary>
    public void ApplyMenuBarButtonStyling(UIManager uiManager)
    {
        if (uiManager == null) return;
        
        // Apply modern styling to all top menu buttons
        Button[] menuButtons = new Button[]
        {
            uiManager.buildMenuButton,
            uiManager.researchMenuButton,
            uiManager.settingsButton,
            uiManager.timeControlButton
        };
        
        foreach (Button btn in menuButtons)
        {
            if (btn == null) continue;
            
            ButtonStyle.ApplyMinimalStyle(btn);
            
            // Override to be more visible
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = UITheme.ColorButtonNormal;
            
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.color = UITheme.ColorTextPrimary;
                btnText.fontSize = 28;
            }
        }
    }
}
