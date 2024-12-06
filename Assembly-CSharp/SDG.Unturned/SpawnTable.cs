using System;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class SpawnTable
{
    /// <summary>
    /// If non-zero, legacy ID of final Asset to return.
    /// </summary>
    internal ushort legacyAssetId;

    /// <summary>
    /// If non-zero, legacy ID of SpawnAsset to resolve.
    /// </summary>
    internal ushort legacySpawnId;

    /// <summary>
    /// If both legacy IDs are zero this GUID will be used. If the target asset is
    /// a SpawnAsset it will be further resolved, otherwise the found asset is returned.
    /// </summary>
    internal Guid targetGuid;

    public int weight;

    internal float normalizedWeight;

    public bool isLink;

    /// <summary>
    /// Can be enabled by spawn tables that insert themselves into other spawn tables using the roots list.
    /// If true, zeros the weight of child tables in the parent spawn table.
    /// </summary>
    public bool isOverride;

    /// <summary>
    /// Has this spawn been added as a root of its child spawn table?
    /// Used for debugging spawn hierarchy in editor.
    /// </summary>
    public bool hasNotifiedChild;

    /// <summary>
    /// Helper method for plugins because IDs are internal.
    /// </summary>
    public Asset FindAsset(EAssetType legacyAssetType)
    {
        if (!targetGuid.IsEmpty())
        {
            return Assets.find(targetGuid);
        }
        if (legacyAssetId > 0)
        {
            return Assets.find(legacyAssetType, legacyAssetId);
        }
        if (legacySpawnId > 0)
        {
            return Assets.find(EAssetType.SPAWN, legacySpawnId) as SpawnAsset;
        }
        return null;
    }

    internal bool TryParse(Asset assetContext, DatDictionary datDictionary)
    {
        targetGuid = datDictionary.ParseGuid("Guid");
        legacyAssetId = datDictionary.ParseUInt16("LegacyAssetId", 0);
        legacySpawnId = datDictionary.ParseUInt16("LegacySpawnId", 0);
        isOverride = datDictionary.ParseBool("IsOverride");
        weight = datDictionary.ParseInt32("Weight", isOverride ? 1 : 0);
        if (legacySpawnId == 0 && legacyAssetId == 0 && targetGuid.IsEmpty())
        {
            Assets.ReportError(assetContext, "contains an entry with neither a LegacyAssetId, LegacySpawnId, or Guid set!");
            return false;
        }
        if (weight <= 0)
        {
            Assets.ReportError(assetContext, "contains an entry with no weight!");
            return false;
        }
        return true;
    }

    internal void Write(DatWriter writer, EAssetType legacyAssetType)
    {
        if (!targetGuid.IsEmpty())
        {
            Asset asset = Assets.find(targetGuid);
            string message = ((asset != null) ? (asset.FriendlyName + " (" + asset.GetTypeFriendlyName() + ")") : $"Unknown {legacyAssetType}");
            writer.WriteComment(message);
            writer.WriteKeyValue("Guid", targetGuid);
        }
        else if (legacyAssetId > 0)
        {
            Asset asset2 = Assets.find(legacyAssetType, legacyAssetId);
            string message2 = ((asset2 != null) ? (asset2.FriendlyName + " (" + asset2.GetTypeFriendlyName() + ")") : $"Unknown {legacyAssetType}");
            writer.WriteComment(message2);
            writer.WriteKeyValue("LegacyAssetId", legacyAssetId);
        }
        else if (legacySpawnId > 0)
        {
            string message3 = ((Assets.find(EAssetType.SPAWN, legacySpawnId) is SpawnAsset spawnAsset) ? (spawnAsset.FriendlyName + " (" + spawnAsset.GetTypeFriendlyName() + ")") : "Unknown");
            writer.WriteComment(message3);
            writer.WriteKeyValue("LegacySpawnId", legacySpawnId);
        }
        if (isOverride)
        {
            writer.WriteKeyValue("IsOverride", isOverride);
        }
        writer.WriteKeyValue("Weight", weight);
    }
}
