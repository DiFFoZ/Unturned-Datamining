using System;
using System.Collections.Generic;

namespace SDG.Unturned;

public class MainMenuWorkshopFeaturedLiveConfig
{
    public long id;

    public ulong[] fileIds;

    public EMapStatus status;

    public EFeaturedWorkshopType type;

    public bool autoExpandDescription;

    public string overrideDescription;

    public string linkText;

    public string linkURL;

    public int[] associatedStockpileItems;

    public bool useTimeWindow;

    public DateTime startTime;

    public DateTime endTime;

    public bool IsNowFeaturedTime
    {
        get
        {
            if (useTimeWindow)
            {
                DateTime utcNow = DateTime.UtcNow;
                if (utcNow >= startTime)
                {
                    return utcNow <= endTime;
                }
                return false;
            }
            return true;
        }
    }

    public bool IsFeatured(ulong fileId)
    {
        if (fileId == 0L || fileIds == null)
        {
            return false;
        }
        ulong[] array = fileIds;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == fileId)
            {
                return true;
            }
        }
        return false;
    }

    public void Parse(DatDictionary data)
    {
        id = data.ParseInt64("Id", 0L);
        if (data.TryGetList("FileIds", out var node))
        {
            List<ulong> list = new List<ulong>(node.Count);
            foreach (DatValue value3 in node.GetValues())
            {
                if (value3.TryParseUInt64(out var value))
                {
                    list.Add(value);
                }
            }
            fileIds = list.ToArray();
        }
        else
        {
            fileIds = new ulong[0];
        }
        status = data.ParseEnum("Status", EMapStatus.None);
        type = data.ParseEnum("Type", EFeaturedWorkshopType.Curated);
        autoExpandDescription = data.ParseBool("AutoExpandDescription");
        overrideDescription = data.GetString("OverrideDescription");
        linkText = data.GetString("LinkText");
        linkURL = data.GetString("LinkURL");
        if (data.TryGetList("AssociatedStockpileItems", out var node2))
        {
            List<int> list2 = new List<int>(node2.Count);
            foreach (DatValue value4 in node2.GetValues())
            {
                if (value4.TryParseInt32(out var value2))
                {
                    list2.Add(value2);
                }
            }
            associatedStockpileItems = list2.ToArray();
        }
        else
        {
            associatedStockpileItems = new int[0];
        }
        useTimeWindow = data.ParseBool("UseTimeWindow");
        startTime = data.ParseDateTimeUtc("StartTime");
        endTime = data.ParseDateTimeUtc("EndTime");
    }
}
