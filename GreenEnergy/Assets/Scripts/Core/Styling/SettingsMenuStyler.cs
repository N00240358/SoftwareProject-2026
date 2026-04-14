using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies theme styling to the Settings Menu panel, its sliders, toggles, and buttons.
/// Button variants are selected by name: "Delete"/"Destructive" → destructive red,
/// "Save" → accent green, "Cancel"/"Close" → secondary, all others → primary.
/// Called from <c>UIManager.ApplyMenuStyling()</c>.
/// </summary>
public class SettingsMenuStyler : MonoBehaviour
{
    /// <summary>
    /// Styles the settings menu panel, sliders, toggles, and buttons.
    /// Buttons are categorized by name: Delete → destructive, Save → accent, Cancel/Close → secondary, others → primary.
    /// </summary>
    public void ApplySettingsMenuStyling(SettingsManager settingsManager)
    {
        if (settingsManager == null) return;
        
        // Style menu panel
        Image panelImage = settingsManager.GetComponent<Image>();
        if (panelImage != null)
        {
            PanelStyle.ApplyDarkPanelStyle(panelImage);
        }
        
        // Style layout group
        VerticalLayoutGroup layout = settingsManager.GetComponentInChildren<VerticalLayoutGroup>();
        if (layout != null)
        {
            PanelStyle.ConfigureVerticalLayout(layout);
        }
        
        // Style all sliders
        Slider[] sliders = settingsManager.GetComponentsInChildren<Slider>();
        foreach (Slider slider in sliders)
        {
            ApplySliderStyling(slider);
        }
        
        // Style all toggles
        Toggle[] toggles = settingsManager.GetComponentsInChildren<Toggle>();
        foreach (Toggle toggle in toggles)
        {
            ApplyToggleStyling(toggle);
        }
        
        // Style all buttons
        Button[] buttons = settingsManager.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            
            // Determine button type
            if (btn.name.Contains("Delete") || btn.name.Contains("Destructive"))
                ButtonStyle.ApplyDestructiveStyle(btn);
            else if (btn.name.Contains("Save"))
                ButtonStyle.ApplyAccentStyle(btn);
            else if (btn.name.Contains("Cancel") || btn.name.Contains("Close"))
                ButtonStyle.ApplySecondaryStyle(btn);
            else
                ButtonStyle.ApplyPrimaryStyle(btn);
        }
    }
    
    /// <summary>
    /// Styles a slider's background (dark), fill and handle (accent cyan), and sets a fixed
    /// preferred height of 36px via a <see cref="LayoutElement"/>.
    /// </summary>
    private void ApplySliderStyling(Slider slider)
    {
        if (slider == null) return;
        
        // Background
        Image background = slider.GetComponent<Image>();
        if (background != null)
            background.color = UITheme.ColorBackgroundDark;
        
        // Fill
        Image fillImage = slider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
            fillImage.color = UITheme.ColorAccentCyan;
        
        // Handle thumb
        Image handleImage = slider.handleRect?.GetComponent<Image>();
        if (handleImage != null)
            handleImage.color = UITheme.ColorAccentCyan;
        
        // Set slider size
        LayoutElement layoutElement = slider.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = slider.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 36f;
    }
    
    /// <summary>
    /// Styles a toggle's background (button-normal), checkmark graphic (accent cyan),
    /// color block states, and label text (primary white at 26px).
    /// </summary>
    private void ApplyToggleStyling(Toggle toggle)
    {
        if (toggle == null) return;
        
        // Background
        Image background = toggle.GetComponent<Image>();
        if (background != null)
            background.color = UITheme.ColorButtonNormal;
        
        // Checkmark (the graphic component)
        Image checkmark = toggle.graphic as Image;
        if (checkmark != null)
            checkmark.color = UITheme.ColorAccentCyan;
        
        // Colors
        ColorBlock colors = toggle.colors;
        colors.normalColor = UITheme.ColorButtonNormal;
        colors.highlightedColor = UITheme.ColorButtonHover;
        colors.pressedColor = UITheme.ColorAccentCyan;
        colors.disabledColor = UITheme.ColorTextSecondary;
        toggle.colors = colors;
        
        // Label text
        TextMeshProUGUI labelText = toggle.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null)
        {
            labelText.color = UITheme.ColorTextPrimary;
            labelText.fontSize = 26;
        }
    }
}
