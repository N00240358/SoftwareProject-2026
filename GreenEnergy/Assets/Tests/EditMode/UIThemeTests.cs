using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for UITheme helper methods — WithAlpha, LerpColor, and GetButtonColor.
/// UITheme is a static class with only value-type (Color/float) fields, so tests
/// are pure and require no scene or MonoBehaviour setup.
/// </summary>
[TestFixture]
public class UIThemeTests
{
    // ===== WithAlpha =====

    [Test]
    public void WithAlpha_SetsAlphaToProvidedValue()
    {
        Color result = UITheme.WithAlpha(Color.red, 0.5f);
        Assert.AreEqual(0.5f, result.a, 0.001f);
    }

    [Test]
    public void WithAlpha_PreservesRGBChannels()
    {
        Color original = new Color(0.2f, 0.4f, 0.8f, 1f);
        Color result = UITheme.WithAlpha(original, 0f);
        Assert.AreEqual(original.r, result.r, 0.001f);
        Assert.AreEqual(original.g, result.g, 0.001f);
        Assert.AreEqual(original.b, result.b, 0.001f);
    }

    [Test]
    public void WithAlpha_ZeroAlpha_IsFullyTransparent()
    {
        Color result = UITheme.WithAlpha(Color.white, 0f);
        Assert.AreEqual(0f, result.a, 0.001f);
    }

    [Test]
    public void WithAlpha_OneAlpha_IsFullyOpaque()
    {
        Color result = UITheme.WithAlpha(Color.black, 1f);
        Assert.AreEqual(1f, result.a, 0.001f);
    }

    [Test]
    public void WithAlpha_DoesNotMutateColorConstant()
    {
        // Color is a struct — passing it by value should not modify the original field.
        Color beforeCyan = UITheme.ColorAccentCyan;
        UITheme.WithAlpha(UITheme.ColorAccentCyan, 0f);
        Assert.AreEqual(beforeCyan.a, UITheme.ColorAccentCyan.a, 0.001f,
            "WithAlpha should not mutate the original static field");
    }

    // ===== LerpColor =====

    [Test]
    public void LerpColor_ZeroT_ReturnsFromColor()
    {
        Color from = Color.red;
        Color to   = Color.blue;
        Color result = UITheme.LerpColor(from, to, 0f);
        Assert.AreEqual(from.r, result.r, 0.001f);
        Assert.AreEqual(from.g, result.g, 0.001f);
        Assert.AreEqual(from.b, result.b, 0.001f);
    }

    [Test]
    public void LerpColor_OneT_ReturnsToColor()
    {
        Color from = Color.red;
        Color to   = Color.blue;
        Color result = UITheme.LerpColor(from, to, 1f);
        Assert.AreEqual(to.r, result.r, 0.001f);
        Assert.AreEqual(to.g, result.g, 0.001f);
        Assert.AreEqual(to.b, result.b, 0.001f);
    }

    [Test]
    public void LerpColor_HalfT_ReturnsMidpoint()
    {
        Color from = Color.black; // (0, 0, 0, 1)
        Color to   = Color.white; // (1, 1, 1, 1)
        Color result = UITheme.LerpColor(from, to, 0.5f);
        Assert.AreEqual(0.5f, result.r, 0.001f);
        Assert.AreEqual(0.5f, result.g, 0.001f);
        Assert.AreEqual(0.5f, result.b, 0.001f);
    }

    // ===== GetButtonColor =====

    [Test]
    public void GetButtonColor_NeitherHoverNorPressed_ReturnsNormalColor()
    {
        Color result = UITheme.GetButtonColor(isHover: false, isPressed: false);
        Assert.AreEqual(UITheme.ColorButtonNormal, result);
    }

    [Test]
    public void GetButtonColor_HoverOnly_ReturnsHoverColor()
    {
        Color result = UITheme.GetButtonColor(isHover: true, isPressed: false);
        Assert.AreEqual(UITheme.ColorButtonHover, result);
    }

    [Test]
    public void GetButtonColor_PressedOnly_ReturnsPressedColor()
    {
        Color result = UITheme.GetButtonColor(isHover: false, isPressed: true);
        Assert.AreEqual(UITheme.ColorButtonPressed, result);
    }

    [Test]
    public void GetButtonColor_BothHoverAndPressed_PressedTakesPriority()
    {
        // Pressed state takes priority over hover (isPressed checked first in the method)
        Color result = UITheme.GetButtonColor(isHover: true, isPressed: true);
        Assert.AreEqual(UITheme.ColorButtonPressed, result);
    }

    // ===== Constants sanity =====

    [Test]
    public void OpacityFull_IsOne()
    {
        Assert.AreEqual(1f, UITheme.OpacityFull, 0.001f);
    }

    [Test]
    public void OpacityVeryLow_IsLessThanOpacityLow()
    {
        Assert.Less(UITheme.OpacityVeryLow, UITheme.OpacityLow);
    }

    [Test]
    public void AnimationFastDuration_IsLessThanNormal()
    {
        Assert.Less(UITheme.AnimationFastDuration, UITheme.AnimationNormalDuration);
    }

    [Test]
    public void AnimationNormalDuration_IsLessThanSlow()
    {
        Assert.Less(UITheme.AnimationNormalDuration, UITheme.AnimationSlowDuration);
    }
}
