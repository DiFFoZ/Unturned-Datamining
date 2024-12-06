using System.Collections.Generic;

namespace SDG.Unturned;

public class MainMenuWorkshopPopularLiveConfig
{
    public uint trendDays;

    public int carouselItems;

    public ulong[] hiddenFileIds;

    public bool IsHidden(ulong fileId)
    {
        if (fileId == 0L || hiddenFileIds == null)
        {
            return false;
        }
        ulong[] array = hiddenFileIds;
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
        trendDays = data.ParseUInt32("TrendDays");
        carouselItems = data.ParseInt32("CarouselItems");
        if (data.TryGetList("HiddenFileIds", out var node))
        {
            List<ulong> list = new List<ulong>(node.Count);
            foreach (DatValue value2 in node.GetValues())
            {
                if (value2.TryParseUInt64(out var value))
                {
                    list.Add(value);
                }
            }
            hiddenFileIds = list.ToArray();
        }
        else
        {
            hiddenFileIds = new ulong[0];
        }
    }
}
