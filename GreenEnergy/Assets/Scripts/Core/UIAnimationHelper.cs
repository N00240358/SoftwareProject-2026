using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static library of reusable UI animation coroutines: menu fades, directional slides,
/// button press/hover scale, pulse, colour flash, and colour transition.
/// All methods return <see cref="System.Collections.IEnumerator"/> and must be run via
/// <c>StartCoroutine()</c> from a MonoBehaviour. All durations are in seconds.
/// </summary>
public static class UIAnimationHelper
{
    // ===== MENU ANIMATIONS =====

    /// <summary>
    /// Fades a panel in from fully transparent to fully opaque, then enables interaction.
    /// Sets <c>interactable</c> and <c>blocksRaycasts</c> to false during the fade so
    /// the player can't click through the fading panel.
    /// </summary>
    /// <param name="canvasGroup">The panel's CanvasGroup component.</param>
    /// <param name="duration">Fade duration in seconds (default 0.3s).</param>
    public static IEnumerator AnimateMenuAppear(CanvasGroup canvasGroup, float duration = 0.3f)
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// Fades a panel out from opaque to transparent. Immediately disables interaction so the
    /// player cannot click the panel during the close animation. Does NOT deactivate the
    /// GameObject — the caller is responsible for calling <c>SetActive(false)</c> afterward.
    /// </summary>
    /// <param name="canvasGroup">The panel's CanvasGroup component.</param>
    /// <param name="duration">Fade duration in seconds (default 0.2s).</param>
    public static IEnumerator AnimateMenuDisappear(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    // ===== SLIDE ANIMATIONS =====

    /// <summary>
    /// Slides a panel in from the left edge. Uses SmoothStep easing for a natural feel.
    /// </summary>
    /// <param name="rectTransform">Panel's RectTransform.</param>
    /// <param name="panelWidth">Panel width in pixels — used as the off-screen start X position.</param>
    /// <param name="duration">Slide duration in seconds (default 0.3s).</param>
    public static IEnumerator AnimateSlideInFromLeft(RectTransform rectTransform, float panelWidth, float duration = 0.3f)
    {
        float startX = -panelWidth;
        float endX = 0f;
        float elapsed = 0f;
        
        Vector2 startPos = rectTransform.anchoredPosition;
        startPos.x = startX;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.x = Mathf.Lerp(startX, endX, progress);
            rectTransform.anchoredPosition = newPos;
            yield return null;
        }
        
        Vector2 finalPos = rectTransform.anchoredPosition;
        finalPos.x = endX;
        rectTransform.anchoredPosition = finalPos;
    }
    
    /// <summary>Slides a panel out to the left edge (reverse of <see cref="AnimateSlideInFromLeft"/>).</summary>
    /// <param name="rectTransform">Panel's RectTransform.</param>
    /// <param name="panelWidth">Panel width in pixels — used as the off-screen end X position.</param>
    /// <param name="duration">Slide duration in seconds (default 0.2s).</param>
    public static IEnumerator AnimateSlideOutToLeft(RectTransform rectTransform, float panelWidth, float duration = 0.2f)
    {
        float startX = 0f;
        float endX = -panelWidth;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.x = Mathf.Lerp(startX, endX, progress);
            rectTransform.anchoredPosition = newPos;
            yield return null;
        }
        
        Vector2 finalPos = rectTransform.anchoredPosition;
        finalPos.x = endX;
        rectTransform.anchoredPosition = finalPos;
    }
    
    /// <summary>Slides a panel in from the right by <paramref name="slideDistance"/> pixels. Used for notifications.</summary>
    /// <param name="slideDistance">How far off-screen (in pixels) the panel starts.</param>
    /// <param name="duration">Slide duration in seconds (default 0.2s).</param>
    public static IEnumerator AnimateSlideInFromRight(RectTransform rectTransform, float slideDistance, float duration = 0.2f)
    {
        float startX = slideDistance;
        float endX = 0f;
        float elapsed = 0f;
        
        Vector2 startPos = rectTransform.anchoredPosition;
        startPos.x = startX;
        rectTransform.anchoredPosition = startPos;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.x = Mathf.Lerp(startX, endX, progress);
            rectTransform.anchoredPosition = newPos;
            yield return null;
        }
        
        Vector2 finalPos = rectTransform.anchoredPosition;
        finalPos.x = endX;
        rectTransform.anchoredPosition = finalPos;
    }
    
    /// <summary>Slides a panel out to the right by <paramref name="slideDistance"/> pixels. Used for notification dismiss.</summary>
    public static IEnumerator AnimateSlideOutToRight(RectTransform rectTransform, float slideDistance, float duration = 0.2f)
    {
        float startX = 0f;
        float endX = slideDistance;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.x = Mathf.Lerp(startX, endX, progress);
            rectTransform.anchoredPosition = newPos;
            yield return null;
        }
        
        Vector2 finalPos = rectTransform.anchoredPosition;
        finalPos.x = endX;
        rectTransform.anchoredPosition = finalPos;
    }
    
    // ===== BUTTON ANIMATIONS =====

    /// <summary>
    /// Compress-then-release scale animation giving tactile press feedback.
    /// Scales down to 98% over the first half then back to original over the second half.
    /// </summary>
    /// <param name="duration">Total animation duration in seconds (default 0.15s).</param>
    public static IEnumerator AnimateButtonPress(RectTransform buttonRect, float duration = 0.15f)
    {
        Vector3 originalScale = buttonRect.localScale;
        Vector3 pressScale = originalScale * 0.98f;
        
        // Compress
        float elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            buttonRect.localScale = Vector3.Lerp(originalScale, pressScale, progress);
            yield return null;
        }
        
        // Release
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            buttonRect.localScale = Vector3.Lerp(pressScale, originalScale, progress);
            yield return null;
        }
        
        buttonRect.localScale = originalScale;
    }
    
    /// <summary>
    /// Scales a button to <see cref="UITheme.ButtonHoverScaleFactor"/> (1.05×) on hover-enter,
    /// or back to original scale on hover-exit.
    /// </summary>
    /// <param name="isHovering">True to scale up (entering hover), false to scale back down.</param>
    public static IEnumerator AnimateButtonHover(RectTransform buttonRect, float duration = 0.1f, bool isHovering = true)
    {
        Vector3 originalScale = buttonRect.localScale;
        Vector3 targetScale = isHovering ? originalScale * UITheme.ButtonHoverScaleFactor : originalScale;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            buttonRect.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        buttonRect.localScale = targetScale;
    }
    
    /// <summary>
    /// Repeating scale-up / scale-down pulse for drawing attention to an element.
    /// </summary>
    /// <param name="pulseScale">Peak scale multiplier relative to original (default 1.1 = 110%).</param>
    /// <param name="duration">Duration of one full pulse cycle in seconds.</param>
    /// <param name="pulseCount">Number of times to pulse before returning to original scale.</param>
    public static IEnumerator AnimatePulse(RectTransform target, float pulseScale = 1.1f, float duration = 0.3f, int pulseCount = 1)
    {
        Vector3 originalScale = target.localScale;
        Vector3 pulseScaleVec = originalScale * pulseScale;
        
        for (int i = 0; i < pulseCount; i++)
        {
            // Scale up
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2f);
                target.localScale = Vector3.Lerp(originalScale, pulseScaleVec, progress);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2f);
                target.localScale = Vector3.Lerp(pulseScaleVec, originalScale, progress);
                yield return null;
            }
        }
        
        target.localScale = originalScale;
    }
    
    // ===== COLOR ANIMATIONS =====

    /// <summary>
    /// Briefly transitions the image to <paramref name="flashColor"/> then back to its original colour.
    /// Used for button press feedback and status-change indicators.
    /// </summary>
    /// <param name="flashColor">The peak colour at the midpoint of the animation.</param>
    /// <param name="duration">Total animation duration in seconds (default 0.2s).</param>
    public static IEnumerator AnimateColorFlash(Image image, Color flashColor, float duration = 0.2f)
    {
        Color originalColor = image.color;
        
        // Flash to color
        float elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            image.color = Color.Lerp(originalColor, flashColor, progress);
            yield return null;
        }
        
        // Flash back
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            image.color = Color.Lerp(flashColor, originalColor, progress);
            yield return null;
        }
        
        image.color = originalColor;
    }
    
    /// <summary>
    /// Smoothly transitions an image from its current colour to <paramref name="targetColor"/>.
    /// Uses SmoothStep easing. Does not return to the original colour — use for permanent state changes.
    /// </summary>
    /// <param name="targetColor">The final colour after the transition.</param>
    /// <param name="duration">Transition duration in seconds (default 0.3s).</param>
    public static IEnumerator AnimateColorChange(Image image, Color targetColor, float duration = 0.3f)
    {
        Color startColor = image.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            image.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        image.color = targetColor;
    }
    
    /// <summary>
    /// Scale panel up smoothly (for panel open)
    /// </summary>
    public static IEnumerator AnimatePanelOpen(RectTransform panelRect, float duration = 0.3f)
    {
        panelRect.localScale = new Vector3(0.95f, 0.95f, 1f);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            panelRect.localScale = Vector3.Lerp(new Vector3(0.95f, 0.95f, 1f), Vector3.one, progress);
            yield return null;
        }
        
        panelRect.localScale = Vector3.one;
    }
}
