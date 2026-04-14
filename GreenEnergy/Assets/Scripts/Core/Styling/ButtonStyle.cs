using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Static helpers that apply fully configured button visual styles in a single call.
/// Covers five variants (Primary, Secondary, Minimal, Accent, Destructive) and a navigation helper.
/// Every method is safe to call on buttons that already have components — they are overwritten
/// rather than duplicated. Always use these instead of setting colours manually.
/// </summary>
public static class ButtonStyle
{
    /// <summary>
    /// Applies the primary style: dark background, bold white text, standard hover/press states,
    /// and preferred height from <see cref="UITheme.ButtonHeightStandard"/>.
    /// </summary>
    /// <param name="buttonText">If non-empty, sets the button's TextMeshPro label text.</param>
    public static void ApplyPrimaryStyle(Button button, string buttonText = "")
    {
        if (button == null) return;
        
        // Button image background
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = UITheme.ColorButtonNormal;
        }
        
        // Text styling
        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.color = UITheme.ColorTextPrimary;
            textComponent.fontSize = 32;
            textComponent.fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(buttonText))
                textComponent.text = buttonText;
        }
        
        // Interactive colors
        ColorBlock colors = button.colors;
        colors.normalColor = UITheme.ColorButtonNormal;
        colors.highlightedColor = UITheme.ColorButtonHover;
        colors.pressedColor = UITheme.ColorButtonPressed;
        colors.disabledColor = UITheme.ColorTextSecondary;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        
        // Layout
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
            layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = UITheme.ButtonHeightStandard;
        layout.preferredWidth = -1; // Auto width
    }
    
    /// <summary>
    /// Applies secondary style: same as Primary but with a slightly lighter background
    /// (<see cref="UITheme.ColorBackgroundLight"/>) to visually de-emphasise the button.
    /// </summary>
    public static void ApplySecondaryStyle(Button button, string buttonText = "")
    {
        ApplyPrimaryStyle(button, buttonText);
        
        // Slightly different appearance
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = UITheme.ColorBackgroundLight;
        }
    }
    
    /// <summary>
    /// Applies minimal style: 60% opacity background, smaller text (28px), no fixed height.
    /// Used for icon-like controls such as close buttons and toolbar icons.
    /// </summary>
    public static void ApplyMinimalStyle(Button button, string buttonText = "")
    {
        if (button == null) return;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = UITheme.WithAlpha(UITheme.ColorButtonNormal, UITheme.OpacityMedium);
        }
        
        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.color = UITheme.ColorTextPrimary;
            textComponent.fontSize = 28;
            if (!string.IsNullOrEmpty(buttonText))
                textComponent.text = buttonText;
        }
        
        ColorBlock colors = button.colors;
        colors.normalColor = UITheme.WithAlpha(UITheme.ColorButtonNormal, UITheme.OpacityMedium);
        colors.highlightedColor = UITheme.ColorButtonHover;
        colors.pressedColor = UITheme.ColorAccentCyan;
        colors.disabledColor = UITheme.ColorTextSecondary;
        button.colors = colors;
    }
    
    /// <summary>
    /// Applies accent style: dark background with coloured text to signal a positive action.
    /// </summary>
    /// <param name="accentColor">Text colour for the accent. Defaults to <see cref="UITheme.ColorAccentGreen"/>.</param>
    public static void ApplyAccentStyle(Button button, string buttonText = "", Color? accentColor = null)
    {
        if (button == null) return;
        
        Color accent = accentColor ?? UITheme.ColorAccentGreen;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = UITheme.ColorButtonNormal;
        }
        
        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.color = accent;
            textComponent.fontSize = 32;
            textComponent.fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(buttonText))
                textComponent.text = buttonText;
        }
        
        ColorBlock colors = button.colors;
        colors.normalColor = UITheme.ColorButtonNormal;
        colors.highlightedColor = UITheme.ColorButtonHover;
        colors.pressedColor = UITheme.WithAlpha(accent, 0.8f);
        colors.disabledColor = UITheme.ColorTextSecondary;
        button.colors = colors;
        
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
            layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = UITheme.ButtonHeightStandard;
    }
    
    /// <summary>
    /// Applies destructive style: Accent style with <see cref="UITheme.ColorAccentRed"/> text.
    /// Use for delete, reset, or any irreversible action to warn the player visually.
    /// </summary>
    public static void ApplyDestructiveStyle(Button button, string buttonText = "")
    {
        ApplyAccentStyle(button, buttonText, UITheme.ColorAccentRed);
    }
    
    /// <summary>
    /// Sets the button's navigation mode to Explicit and wires up directional focus targets
    /// for keyboard/gamepad navigation. Any parameter left null is simply unset.
    /// </summary>
    public static void ConfigureNavigation(Button button, Button upButton = null, Button downButton = null, Button leftButton = null, Button rightButton = null)
    {
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = upButton;
        nav.selectOnDown = downButton;
        nav.selectOnLeft = leftButton;
        nav.selectOnRight = rightButton;
        button.navigation = nav;
    }
}
