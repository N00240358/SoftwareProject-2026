using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static helpers for applying consistent panel visuals and layout configurations.
/// Covers three panel background variants (Dark, Nested, AccentBorder), three layout-group
/// presets (Vertical, Horizontal, Grid), divider/spacer creation, and a shadow effect.
/// Always use these instead of configuring layout components manually.
/// </summary>
public static class PanelStyle
{
    /// <summary>
    /// Standard panel: medium-dark background (#262626) with a 1px dark border outline.
    /// Use for top-level menus and content areas.
    /// </summary>
    public static void ApplyDarkPanelStyle(Image panelImage)
    {
        if (panelImage == null) return;
        
        panelImage.color = UITheme.ColorBackgroundMedium;
        
        // Add optional border outline
        Outline outline = panelImage.GetComponent<Outline>();
        if (outline == null)
            outline = panelImage.gameObject.AddComponent<Outline>();
        outline.effectColor = UITheme.ColorBorderDark;
        outline.effectDistance = new Vector2(1, 1);
    }
    
    /// <summary>
    /// Nested panel: darker background (#1a1a1a) with a 1px dark border.
    /// Use inside a standard dark panel to create visual depth for sub-sections.
    /// </summary>
    public static void ApplyNestedPanelStyle(Image panelImage)
    {
        if (panelImage == null) return;
        
        panelImage.color = UITheme.ColorBackgroundDark;
        
        Outline outline = panelImage.GetComponent<Outline>();
        if (outline == null)
            outline = panelImage.gameObject.AddComponent<Outline>();
        outline.effectColor = UITheme.ColorBorderDark;
        outline.effectDistance = new Vector2(1, 1);
    }
    
    /// <summary>
    /// Accent-border panel: standard dark background with a thicker 2px coloured outline.
    /// Use to highlight the currently selected entry or active menu.
    /// </summary>
    /// <param name="borderColor">Border colour; defaults to <see cref="UITheme.ColorBorderCyan"/>.</param>
    public static void ApplyAccentBorderPanelStyle(Image panelImage, Color? borderColor = null)
    {
        if (panelImage == null) return;
        
        panelImage.color = UITheme.ColorBackgroundMedium;
        
        Color border = borderColor ?? UITheme.ColorBorderCyan;
        
        Outline outline = panelImage.GetComponent<Outline>();
        if (outline == null)
            outline = panelImage.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2, 2);
    }
    
    /// <summary>
    /// Configures a vertical layout group with standard 16px padding on all sides,
    /// 8px child spacing, and children expanding horizontally but not vertically.
    /// </summary>
    public static void ConfigureVerticalLayout(VerticalLayoutGroup layout, bool childForceExpand = false)
    {
        if (layout == null) return;
        
        layout.padding = new RectOffset(
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium
        );
        layout.spacing = UITheme.SpacingSmall;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
    }
    
    /// <summary>
    /// Configures a horizontal layout group with 16px horizontal padding, 8px vertical padding,
    /// and 8px child spacing. Used for the top menu bar and stats bar.
    /// </summary>
    /// <param name="childForceExpand">When true, children stretch to fill all available width.</param>
    public static void ConfigureHorizontalLayout(HorizontalLayoutGroup layout, bool childForceExpand = false)
    {
        if (layout == null) return;
        
        layout.padding = new RectOffset(
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingSmall,
            (int)UITheme.SpacingSmall
        );
        layout.spacing = UITheme.SpacingSmall;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = childForceExpand;
    }
    
    /// <summary>
    /// Configures a grid layout group with standard padding/spacing, a fixed column count,
    /// and auto-width cells (width = -1, height = <paramref name="cellHeight"/>).
    /// </summary>
    /// <param name="columnCount">Number of columns in the grid.</param>
    /// <param name="cellHeight">Height of each cell in pixels.</param>
    public static void ConfigureGridLayout(GridLayoutGroup layout, int columnCount, float cellHeight)
    {
        if (layout == null) return;
        
        layout.padding = new RectOffset(
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium,
            (int)UITheme.SpacingMedium
        );
        layout.spacing = new Vector2(UITheme.SpacingSmall, UITheme.SpacingSmall);
        layout.cellSize = new Vector2(-1, cellHeight); // -1 width = auto
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columnCount;
    }
    
    /// <summary>
    /// Creates a 1px-height horizontal divider line and parents it to <paramref name="parent"/>.
    /// </summary>
    /// <returns>The Image component on the divider GameObject.</returns>
    public static Image AddDivider(Transform parent)
    {
        GameObject dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(parent, false);
        
        Image dividerImage = dividerGO.AddComponent<Image>();
        dividerImage.color = UITheme.ColorBorderDark;
        
        LayoutElement layout = dividerGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 1f;
        layout.preferredWidth = -1; // Full width
        
        RectTransform rect = dividerGO.GetComponent<RectTransform>();
        rect.offsetMin = new Vector2(0, rect.offsetMin.y);
        rect.offsetMax = new Vector2(0, rect.offsetMax.y);
        
        return dividerImage;
    }
    
    /// <summary>
    /// Creates an invisible spacer element and parents it to <paramref name="parent"/>.
    /// Use between sibling elements in a layout group when a divider line is too heavy.
    /// </summary>
    /// <param name="parent">The layout group transform to add the spacer to.</param>
    /// <param name="height">Height of the spacer in pixels (default 8px).</param>
    public static void AddSpacing(Transform parent, float height = 8f)
    {
        GameObject spacerGO = new GameObject("Spacer");
        spacerGO.transform.SetParent(parent, false);
        
        LayoutElement layout = spacerGO.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.preferredWidth = -1;
    }
    
    /// <summary>
    /// Adds (or reuses) a Unity <see cref="Shadow"/> component on the panel's Image to produce
    /// a subtle 2px drop-shadow, giving floating panels the appearance of depth.
    /// </summary>
    /// <param name="panelImage">The panel Image whose GameObject will receive the shadow.</param>
    public static void ApplyShadowEffect(Image panelImage)
    {
        if (panelImage == null) return;
        
        Shadow shadow = panelImage.GetComponent<Shadow>();
        if (shadow == null)
            shadow = panelImage.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);
    }
}
