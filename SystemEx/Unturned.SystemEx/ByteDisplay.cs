using System;

namespace Unturned.SystemEx;

public static class ByteDisplay
{
    private static string[] BASE_10_FORMAT_STRINGS = new string[5] { "{0} B", "{0} kB", "{0} MB", "{0} GB", "{0} TB" };

    private static string[] BASE_2_FORMAT_STRINGS = new string[5] { "{0} B", "{0} KiB", "{0} MiB", "{0} GiB", "{0} TiB" };

    public static string Base10ToString(long byteCount)
    {
        InternalBase10(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString("G3"));
    }

    public static string Base10ToString(long byteCount, IFormatProvider formatProvider)
    {
        InternalBase10(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(formatProvider));
    }

    public static string Base10ToString(long byteCount, string format)
    {
        InternalBase10(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(format));
    }

    public static string Base10ToString(long byteCount, string format, IFormatProvider formatProvider)
    {
        InternalBase10(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(format, formatProvider));
    }

    public static string Base2ToString(long byteCount)
    {
        InternalBase2(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString("G3"));
    }

    public static string Base2ToString(long byteCount, IFormatProvider formatProvider)
    {
        InternalBase2(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(formatProvider));
    }

    public static string Base2ToString(long byteCount, string format)
    {
        InternalBase2(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(format));
    }

    public static string Base2ToString(long byteCount, string format, IFormatProvider formatProvider)
    {
        InternalBase2(byteCount, out var value, out var formatString);
        return string.Format(formatString, value.ToString(format, formatProvider));
    }

    public static string FileSizeToString(long byteCount)
    {
        return Base10ToString(byteCount);
    }

    private static void InternalBase10(long byteCount, out double value, out string formatString)
    {
        if (byteCount == 0L)
        {
            value = 0.0;
            formatString = BASE_10_FORMAT_STRINGS[0];
            return;
        }
        long num = (long)Math.Log(Math.Abs(byteCount), 1000.0);
        formatString = ((num < BASE_10_FORMAT_STRINGS.Length) ? BASE_10_FORMAT_STRINGS[num] : BASE_10_FORMAT_STRINGS[0]);
        double num2 = Math.Pow(1000.0, num);
        value = (double)byteCount / num2;
    }

    private static void InternalBase2(long byteCount, out double value, out string formatString)
    {
        if (byteCount == 0L)
        {
            value = 0.0;
            formatString = BASE_2_FORMAT_STRINGS[0];
            return;
        }
        long num = (long)Math.Log(Math.Abs(byteCount), 1024.0);
        formatString = ((num < BASE_2_FORMAT_STRINGS.Length) ? BASE_2_FORMAT_STRINGS[num] : BASE_2_FORMAT_STRINGS[0]);
        double num2 = Math.Pow(1024.0, num);
        value = (double)byteCount / num2;
    }
}
