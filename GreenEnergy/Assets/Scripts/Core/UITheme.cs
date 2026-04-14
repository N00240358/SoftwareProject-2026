using UnityEngine;

/// <summary>
/// Single source of truth for all visual constants: colours, spacing, animation durations,
/// button sizing, and opacity levels. Every script that sets a colour or size must use
/// a constant from this class — never hardcode a hex value directly.
/// </summary>
public static class UITheme
{
    // ===== COLOR PALETTE =====
    
    // Primary background colors
    public static Color ColorBackgroundDark = new Color(0.1f, 0.1f, 0.1f, 1f);    // #1a1a1a - Main UI backgrounds
    public static Color ColorBackgroundMedium = new Color(0.15f, 0.15f, 0.15f, 1f); // #262626 - Panel backgrounds
    public static Color ColorBackgroundLight = new Color(0.2f, 0.2f, 0.2f, 1f);     // #333333 - Hover states
    
    // Text colors
    public static Color ColorTextPrimary = new Color(0.95f, 0.95f, 0.95f, 1f);     // #f2f2f2 - Main text
    public static Color ColorTextSecondary = new Color(0.7f, 0.7f, 0.7f, 1f);      // #b3b3b3 - Disabled/muted
    public static Color ColorTextAccent = new Color(0f, 0.85f, 1f, 1f);            // #00d9ff - Highlights
    
    // Accent colors
    public static Color ColorAccentCyan = new Color(0f, 0.85f, 1f, 1f);            // #00d9ff - Energy, active states
    public static Color ColorAccentGreen = new Color(0.1f, 0.9f, 0.4f, 1f);        // #1ae664 - Success, progress
    public static Color ColorAccentRed = new Color(0.95f, 0.3f, 0.3f, 1f);         // #f24c4c - Warnings, errors
    public static Color ColorAccentOrange = new Color(1f, 0.6f, 0.1f, 1f);         // #ff9919 - Alerts, secondary
    
    // UI element colors
    public static Color ColorBorderDark = new Color(0.3f, 0.3f, 0.3f, 1f);         // #4d4d4d - Subtle borders
    public static Color ColorBorderCyan = new Color(0f, 0.85f, 1f, 0.6f);          // #00d9ff (60% alpha) - Active borders
    public static Color ColorButtonNormal = new Color(0.15f, 0.15f, 0.15f, 1f);    // #262626 - Button default
    public static Color ColorButtonHover = new Color(0.25f, 0.25f, 0.25f, 1f);     // #404040 - Button hover
    public static Color ColorButtonPressed = new Color(0f, 0.85f, 1f, 0.3f);       // Cyan highlight on press
    
    // ===== SPACING =====
    
    public static float SpacingXSmall = 4f;   // Tiny gaps
    public static float SpacingSmall = 8f;    // Small padding
    public static float SpacingMedium = 16f;  // Standard padding/gaps
    public static float SpacingLarge = 24f;   // Large padding
    public static float SpacingXLarge = 32f;  // Extra large gaps
    
    // ===== ANIMATION SPEEDS =====
    
    public static float AnimationFastDuration = 0.15f;      // Quick feedback
    public static float AnimationNormalDuration = 0.3f;     // Standard transitions (menu pop-out)
    public static float AnimationSlowDuration = 0.5f;       // Slow reveals
    
    // Button animation properties
    public static float ButtonHoverScaleFactor = 1.05f;     // Subtle scale on hover
    public static float ButtonPressScaleFactor = 0.98f;     // Slight press feedback
    
    // ===== SIZING =====
    
    public static float ButtonHeightStandard = 44f;         // Standard button height
    public static float ButtonHeightSmall = 32f;            // Compact buttons
    public static float ButtonWidthMinimum = 80f;           // Minimum button width
    
    public static float PanelCornerRadius = 8f;             // Subtle rounded corners (if using Image with rounded sprite)
    
    // ===== OPACITY =====
    
    public static float OpacityFull = 1f;
    public static float OpacityHigh = 0.9f;
    public static float OpacityMedium = 0.6f;
    public static float OpacityLow = 0.3f;
    public static float OpacityVeryLow = 0.1f;
    
    // ===== HELPER METHODS =====
    
    /// <summary>
    /// Returns <paramref name="color"/> with its alpha channel set to <paramref name="alpha"/>.
    /// Useful for creating transparent variants of theme colours without defining extra constants.
    /// </summary>
    /// <param name="color">Base colour (copied by value — original is not modified).</param>
    /// <param name="alpha">Target alpha, 0 (transparent) to 1 (opaque).</param>
    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    /// <summary>
    /// Linear interpolation between two colours. Thin wrapper over <see cref="Color.Lerp"/>
    /// kept here so callers don't need to import UnityEngine just for a colour blend.
    /// </summary>
    /// <param name="from">Start colour at t=0.</param>
    /// <param name="to">End colour at t=1.</param>
    /// <param name="t">Blend factor, clamped to 0–1.</param>
    public static Color LerpColor(Color from, Color to, float t)
    {
        return Color.Lerp(from, to, t);
    }

    /// <summary>
    /// Returns the correct button background colour for the given interaction state.
    /// Pressed takes priority over hover; both false returns the normal background.
    /// </summary>
    /// <param name="isHover">True when the cursor is over the button.</param>
    /// <param name="isPressed">True while the button is held down.</param>
    public static Color GetButtonColor(bool isHover, bool isPressed)
    {
        if (isPressed)
            return ColorButtonPressed;
        if (isHover)
            return ColorButtonHover;
        return ColorButtonNormal;
    }
}
