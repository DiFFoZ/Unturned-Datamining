using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SDG.Unturned;

public static class RichTextUtil
{
    private static Regex richTextColorTagRegex = new Regex("</*color.*?>", RegexOptions.IgnoreCase);

    public static string replaceColorTags(string text)
    {
        return richTextColorTagRegex.Replace(text, string.Empty);
    }

    public static GUIContent makeShadowContent(GUIContent content)
    {
        return new GUIContent(replaceColorTags(content.text), replaceColorTags(content.tooltip));
    }

    public static string wrapWithColor(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    public static string wrapWithColor(string text, Color32 color)
    {
        return wrapWithColor(text, Palette.hex(color));
    }

    public static string wrapWithColor(string text, Color color)
    {
        return wrapWithColor(text, (Color32)color);
    }

    public static void replaceNewlineMarkup(ref string s)
    {
        s = s.Replace("<br>", "\n");
    }

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
}
