using System;
using System.IO;
using System.Text;

namespace Unturned.SystemEx;

public static class PathEx
{
    private static char[] invalidFileNameChars;

    public static string Join(DirectoryInfo path1, string path2)
    {
        return Path.Combine(path1.FullName, path2);
    }

    public static string Join(DirectoryInfo path1, string path2, string path3)
    {
        return Path.Combine(path1.FullName, path2, path3);
    }

    public static string Join(DirectoryInfo path1, string path2, string path3, string path4)
    {
        return Path.Combine(path1.FullName, path2, path3, path4);
    }

    public static string Join(DirectoryInfo path1, string path2, string path3, string path4, string path5)
    {
        return Path.Combine(path1.FullName, path2, path3, path4, path5);
    }

    public static string Join(DirectoryInfo path1, string path2, string path3, string path4, string path5, string path6)
    {
        return Path.Combine(path1.FullName, path2, path3, path4, path5, path6);
    }

    public static string ReplaceInvalidFileNameChars(string input, char replacement)
    {
        StringBuilder stringBuilder = null;
        int length = input.Length;
        for (int i = 0; i < length; i++)
        {
            char value = input[i];
            if (Array.IndexOf(invalidFileNameChars, value) >= 0)
            {
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder(length);
                    if (i > 0)
                    {
                        stringBuilder.Append(input, 0, i);
                    }
                }
                stringBuilder.Append(replacement);
            }
            else
            {
                stringBuilder?.Append(value);
            }
        }
        if (stringBuilder != null)
        {
            return stringBuilder.ToString();
        }
        return input;
    }

    static PathEx()
    {
        invalidFileNameChars = Path.GetInvalidFileNameChars();
    }
}
