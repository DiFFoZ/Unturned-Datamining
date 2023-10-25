using UnityEngine;
using UnityEngine.UIElements;

namespace SDG.Unturned;

internal static class GlazierUtils_UIToolkit
{
    /// <summary>
    /// By default, clickable only responds to LeftMouse without the Control modifier.
    /// Unturned (currently) filters left/right mouse and modifiers outside Glazier,
    /// so add activators for left/right and control modifier to all clickables.
    /// </summary>
    public static void AddClickableActivators(Clickable clickable)
    {
        clickable.activators.Add(new ManipulatorActivationFilter
        {
            button = MouseButton.LeftMouse,
            modifiers = EventModifiers.Control
        });
        clickable.activators.Add(new ManipulatorActivationFilter
        {
            button = MouseButton.RightMouse
        });
        clickable.activators.Add(new ManipulatorActivationFilter
        {
            button = MouseButton.RightMouse,
            modifiers = EventModifiers.Control
        });
    }

    public static void ApplyTextContrast(IStyle style, ETextContrastContext contrastContext, float alpha)
    {
        switch (SleekShadowStyle.ContextToStyle(contrastContext))
        {
        default:
            style.textShadow = StyleKeyword.Null;
            style.unityTextOutlineColor = StyleKeyword.Null;
            style.unityTextOutlineWidth = StyleKeyword.Null;
            break;
        case ETextContrastStyle.Outline:
            style.textShadow = new TextShadow
            {
                color = SleekCustomization.shadowColor.WithAlpha(alpha),
                offset = new Vector2(0f, 0f),
                blurRadius = 1.5f
            };
            style.unityTextOutlineColor = StyleKeyword.Null;
            style.unityTextOutlineWidth = StyleKeyword.Null;
            break;
        case ETextContrastStyle.Shadow:
            style.textShadow = new TextShadow
            {
                color = SleekCustomization.shadowColor.WithAlpha(alpha),
                offset = new Vector2(0f, 1f),
                blurRadius = 1f
            };
            style.unityTextOutlineColor = StyleKeyword.Null;
            style.unityTextOutlineWidth = StyleKeyword.Null;
            break;
        case ETextContrastStyle.Tooltip:
            style.textShadow = new TextShadow
            {
                color = Color.black,
                offset = new Vector2(0f, 1f),
                blurRadius = 2f
            };
            style.unityTextOutlineColor = SleekCustomization.shadowColor.WithAlpha(alpha * 0.25f);
            style.unityTextOutlineWidth = 0.25f;
            break;
        }
    }

    public static StyleLength GetFontSize(ESleekFontSize fontSize)
    {
        return fontSize switch
        {
            ESleekFontSize.Tiny => 8f, 
            ESleekFontSize.Small => 10f, 
            ESleekFontSize.Medium => 14f, 
            ESleekFontSize.Large => 20f, 
            ESleekFontSize.Title => 50f, 
            _ => StyleKeyword.Null, 
        };
    }

    /// <summary>
    /// USS best practices mentions inline styles have a higher memory overhead, so we
    /// only apply an inline value if it doesn't match the default :root font style.
    /// </summary>
    public static StyleEnum<FontStyle> GetFontStyle(FontStyle fontStyle)
    {
        if (fontStyle == FontStyle.Normal)
        {
            return StyleKeyword.Null;
        }
        return fontStyle;
    }

    /// <summary>
    /// USS best practices mentions inline styles have a higher memory overhead, so we
    /// only apply an inline value if it doesn't match the default :root text alignment.
    /// </summary>
    public static StyleEnum<TextAnchor> GetTextAlignment(TextAnchor textAlignment)
    {
        if (textAlignment == TextAnchor.MiddleCenter)
        {
            return StyleKeyword.Null;
        }
        return textAlignment;
    }
}
