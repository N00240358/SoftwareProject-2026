using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// On <c>Start()</c> (or a manual <see cref="ApplyTheme"/> call), walks the full canvas hierarchy
/// and bulk-applies <see cref="UITheme"/> colours to every Image, Button, TextMeshProUGUI,
/// Slider, and Toggle it finds. Buttons that already have non-default colours are skipped so
/// hand-styled elements are not overwritten. Add this to the Canvas in the main game scene.
/// </summary>
[ExecuteInEditMode]
public class UIStyleBootstrapper : MonoBehaviour
{
    private CanvasScaler canvasScaler;
    
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            ApplyTheme();
        }
    }
    
    /// <summary>
    /// Walks the full UI hierarchy and applies UITheme colors to all panels, buttons, texts, sliders, and toggles.
    /// Skips elements that have already been styled (non-default colors). Safe to call multiple times.
    /// </summary>
    public void ApplyTheme()
    {
        if (Application.isEditor && !Application.isPlaying)
            return;
        
        // Get all UI elements
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        
        if (canvas == null)
        {
            Debug.LogWarning("UIStyleBootstrapper: No Canvas found");
            return;
        }
        
        // Style main canvas background
        Image canvasImage = canvas.GetComponent<Image>();
        if (canvasImage != null)
        {
            canvasImage.color = UITheme.ColorBackgroundDark;
        }
        
        // Find and style all major UI panels
        StyleAllPanels(canvas.transform);
        StyleAllButtons(canvas.transform);
        StyleAllTexts(canvas.transform);
        StyleAllSliders(canvas.transform);
        StyleAllToggles(canvas.transform);
    }
    
    /// <summary>
    /// Applies dark panel styling to every Image whose GameObject name contains "Panel",
    /// "Menu", or "Screen". Other images (e.g. button backgrounds, icons) are left untouched.
    /// </summary>
    private void StyleAllPanels(Transform parent)
    {
        foreach (Image image in parent.GetComponentsInChildren<Image>())
        {
            // Only style panels, not buttons or other elements
            if (image.gameObject.name.Contains("Panel") || 
                image.gameObject.name.Contains("Menu") ||
                image.gameObject.name.Contains("Screen"))
            {
                PanelStyle.ApplyDarkPanelStyle(image);
            }
        }
    }
    
    /// <summary>
    /// Applies a button style variant to every Button, choosing the variant based on the
    /// GameObject name: "Accent"/"Success" → Accent, "Delete"/"Destructive" → Destructive,
    /// "Minimal"/"Icon" → Minimal, anything else → Primary. Skips buttons with non-white
    /// normalColor since those have already been styled manually.
    /// </summary>
    private void StyleAllButtons(Transform parent)
    {
        foreach (Button button in parent.GetComponentsInChildren<Button>())
        {
            // Skip if already styled
            if (button.colors.normalColor != Color.white)
                continue;
            
            // Apply based on button name or type
            if (button.gameObject.name.Contains("Accent") || button.gameObject.name.Contains("Success"))
                ButtonStyle.ApplyAccentStyle(button);
            else if (button.gameObject.name.Contains("Delete") || button.gameObject.name.Contains("Destructive"))
                ButtonStyle.ApplyDestructiveStyle(button);
            else if (button.gameObject.name.Contains("Minimal") || button.gameObject.name.Contains("Icon"))
                ButtonStyle.ApplyMinimalStyle(button);
            else
                ButtonStyle.ApplyPrimaryStyle(button);
        }
    }
    
    /// <summary>
    /// Sets text colour and size by role: "Label"/"Caption" → secondary grey, small;
    /// "Title"/"Header" → primary white, bold large; all others → primary white.
    /// Skips text that is neither white nor black (already styled).
    /// </summary>
    private void StyleAllTexts(Transform parent)
    {
        foreach (TextMeshProUGUI text in parent.GetComponentsInChildren<TextMeshProUGUI>())
        {
            // Only style if not already colored
            if (text.color == Color.white || text.color == Color.black)
            {
                // Determine text role
                if (text.gameObject.name.Contains("Label") || text.gameObject.name.Contains("Caption"))
                {
                    text.color = UITheme.ColorTextSecondary;
                    text.fontSize = 18;
                }
                else if (text.gameObject.name.Contains("Title") || text.gameObject.name.Contains("Header"))
                {
                    text.color = UITheme.ColorTextPrimary;
                    text.fontSize = 36;
                    text.fontStyle = FontStyles.Bold;
                }
                else
                {
                    text.color = UITheme.ColorTextPrimary;
                }
            }
        }
    }
    
    /// <summary>Sets slider track to dark background, fill and handle to accent cyan.</summary>
    private void StyleAllSliders(Transform parent)
    {
        foreach (Slider slider in parent.GetComponentsInChildren<Slider>())
        {
            Image background = slider.GetComponent<Image>();
            if (background != null)
                background.color = UITheme.ColorBackgroundDark;
            
            Image fill = slider.fillRect?.GetComponent<Image>();
            if (fill != null)
                fill.color = UITheme.ColorAccentCyan;
            
            Image handle = slider.handleRect?.GetComponent<Image>();
            if (handle != null)
                handle.color = UITheme.ColorAccentCyan;
        }
    }
    
    /// <summary>Styles toggle background to button-normal, checkmark to accent cyan, and colour-block states to theme values.</summary>
    private void StyleAllToggles(Transform parent)
    {
        foreach (Toggle toggle in parent.GetComponentsInChildren<Toggle>())
        {
            // Background
            Image background = toggle.GetComponent<Image>();
            if (background != null)
                background.color = UITheme.ColorButtonNormal;
            
            // Checkmark
            Image checkmark = toggle.graphic as Image;
            if (checkmark != null)
                checkmark.color = UITheme.ColorAccentCyan;
            
            // Colors
            ColorBlock colors = toggle.colors;
            colors.normalColor = UITheme.ColorButtonNormal;
            colors.highlightedColor = UITheme.ColorButtonHover;
            colors.pressedColor = UITheme.ColorAccentCyan;
            toggle.colors = colors;
        }
    }
}
