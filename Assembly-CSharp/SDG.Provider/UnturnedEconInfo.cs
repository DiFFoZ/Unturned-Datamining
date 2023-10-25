using System;

namespace SDG.Provider;

public class UnturnedEconInfo
{
    public enum EQuality
    {
        None,
        Common,
        Uncommon,
        Gold,
        Rare,
        Epic,
        Legendary,
        Mythical,
        Premium,
        Achievement
    }

    /// <summary>
    /// This enum exists for sorting items based on rarity, and is derived from quality.
    /// Quality order cannot be changed due to loading from older files, but this one is ordered
    /// from lowest rarity to highest rarity and should match entries in quality.
    /// </summary>
    public enum ERarity
    {
        Common,
        Uncommon,
        Achievement,
        Unknown,
        Gold,
        Premium,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    public string name;

    public string type;

    public string description;

    public string name_color;

    public int itemdefid;

    public bool marketable;

    public int scraps;

    public Guid item_guid;

    public int item_skin;

    public int item_effect;

    public Guid vehicle_guid;

    public EQuality quality;

    public UnturnedEconInfo()
    {
        name = "";
        type = "";
        description = "";
        name_color = "";
        itemdefid = 0;
        scraps = 0;
        item_guid = Guid.Empty;
        item_skin = 0;
        item_effect = 0;
        vehicle_guid = Guid.Empty;
        quality = EQuality.None;
    }
}
