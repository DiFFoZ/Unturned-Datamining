using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SDG.Unturned;

public static class RichTextUtil
{
    private static Regex richTextColorTagRegex = new Regex("</*color.*?>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Remove all color rich formatting so that shadow text displays correctly.
    /// </summary>
    public static string replaceColorTags(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            return richTextColorTagRegex.Replace(text, string.Empty);
        }
        return text;
    }

    /// <summary>
    /// Shadow text needs the color tags removed, otherwise the shadow uses those colors.
    /// </summary>
    public static GUIContent makeShadowContent(GUIContent content)
    {
        return new GUIContent(replaceColorTags(content.text), replaceColorTags(content.tooltip));
    }

    /// <summary>
    /// Wrap text with color tags.
    /// </summary>
    public static string wrapWithColor(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    /// <summary>
    /// Wrap text with color tags.
    /// </summary>
    public static string wrapWithColor(string text, Color32 color)
    {
        return wrapWithColor(text, Palette.hex(color));
    }

    /// <summary>
    /// Wrap text with color tags.
    /// </summary>
    public static string wrapWithColor(string text, Color color)
    {
        return wrapWithColor(text, (Color32)color);
    }

    /// <summary>
    /// Replace br tags with newlines.
    /// </summary>
    public static void replaceNewlineMarkup(ref string s)
    {
        s = s.Replace("<br>", "\n");
    }

    /// <summary>
    /// Should player be allowed to write given text on a sign?
    /// Keep in mind that newer signs use TMP, whereas older signs use uGUI.
    /// </summary>
    public static bool isTextValidForSign(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }
        if (text.IndexOf("<size", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<voffset", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Disable style, align, and space because they make server list unfair.
    /// </summary>
    internal static bool IsTextValidForServerListShortDescription(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }
        if (!isTextValidForSign(text))
        {
            return false;
        }
        if (text.IndexOf("<style", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<align", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<space", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<scale", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        if (text.IndexOf("<pos", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return false;
        }
        return true;
    }
}
