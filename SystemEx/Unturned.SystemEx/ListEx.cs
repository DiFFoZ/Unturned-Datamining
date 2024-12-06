using System;
using System.Collections.Generic;

namespace Unturned.SystemEx;

public static class ListEx
{
    public static void RemoveSwap<T>(this List<T> list, Predicate<T> predicate)
    {
        for (int num = list.Count - 1; num >= 0; num--)
        {
            if (predicate(list[num]))
            {
                list[num] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }
        }
    }

    public static void AddIfNotContained<T>(this List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }
}
