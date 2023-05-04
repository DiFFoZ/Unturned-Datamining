using UnityEngine;

namespace SDG.Unturned;

public static class SleekCustomization
{
    public static bool darkTheme;

    public static Color backgroundColor = Color.white;

    public static Color foregroundColor = Color.white;

    public static Color fontColor = Color.white;

    public static Color badColor = new Color32(191, 31, 31, byte.MaxValue);

    public static ETextContrastPreference defaultTextContrast;

    public static ETextContrastPreference inconspicuousTextContrast;

    public static ETextContrastPreference colorfulTextContrast;
}
