using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component that ensures menus have CanvasGroup for animations.
/// Automatically added to menu panels to support fade in/out animations.
/// </summary>
public class MenuAnimationSetup : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // Ensure CanvasGroup exists for animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start invisible and non-interactive
        if (!gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// Returns this panel's CanvasGroup, creating one if it doesn't exist yet.
    /// Used by UIAnimationHelper coroutines to drive fade animations.
    /// </summary>
    public CanvasGroup GetCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        return canvasGroup;
    }
}
