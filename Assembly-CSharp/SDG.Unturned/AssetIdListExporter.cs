using System;
using System.Collections.Generic;
using System.IO;
using Unturned.SystemEx;

namespace SDG.Unturned;

public static class AssetIdListExporter
{
    public static void Export()
    {
        List<Asset> list = new List<Asset>();
        Assets.FindAssetsByType_UseDefaultAssetMapping(list);
        string text = Path.Join(ReadWrite.PATH, "Extras", "AssetIDs");
        ReadWrite.createFolder(text, usePath: false);
        string text2 = Path.Join(text, "All Assets");
        ReadWrite.createFolder(text2, usePath: false);
        ExportAssetsToCsv(list, Path.Join(text2, "All Assets.csv"));
        ExportAssetsToCsvGroupedByLegacyCategory(text2, list);
        ExportAssetsToCsvGroupedByType(text2, list);
        foreach (AssetOrigin assetOrigin in Assets.assetOrigins)
        {
            if (!assetOrigin.assets.IsEmpty())
            {
                string text3 = PathEx.ReplaceInvalidFileNameChars(assetOrigin.name, '_');
                if (string.IsNullOrEmpty(text3))
                {
                    UnturnedLog.error("Unable to export origin " + assetOrigin.name + " Asset IDs because file name would be empty");
                    continue;
                }
                string text4 = Path.Join(text, text3);
                ReadWrite.createFolder(text4, usePath: false);
                ExportAssetsToCsv(csvPath: Path.Join(text4, text3 + ".csv"), assets: assetOrigin.assets);
                ExportAssetsToCsvGroupedByLegacyCategory(text4, assetOrigin.assets);
                ExportAssetsToCsvGroupedByType(text4, assetOrigin.assets);
            }
        }
    }

    private static void ExportAssetsToCsvGroupedByLegacyCategory(string basePath, List<Asset> assets)
    {
        basePath = Path.Join(basePath, "Grouped by Legacy Category");
        ReadWrite.createFolder(basePath, usePath: false);
        foreach (KeyValuePair<EAssetType, List<Asset>> item in GroupAssetsByLegacyCategory(assets))
        {
            string text = item.Key.ToString();
            string csvPath = Path.Combine(basePath, text + ".csv");
            ExportAssetsToCsv(item.Value, csvPath);
        }
    }

    private static void ExportAssetsToCsvGroupedByType(string basePath, List<Asset> assets)
    {
        basePath = Path.Join(basePath, "Grouped by Type");
        ReadWrite.createFolder(basePath, usePath: false);
        foreach (KeyValuePair<Type, List<Asset>> item in GroupAssetsByType(assets))
        {
            string typeFriendlyName = item.Value[0].GetTypeFriendlyName();
            string csvPath = Path.Combine(basePath, typeFriendlyName + ".csv");
            ExportAssetsToCsv(item.Value, csvPath);
        }
    }

    private static Dictionary<EAssetType, List<Asset>> GroupAssetsByLegacyCategory(List<Asset> assets)
    {
        Dictionary<EAssetType, List<Asset>> dictionary = new Dictionary<EAssetType, List<Asset>>();
        foreach (Asset asset in assets)
        {
            EAssetType assetCategory = asset.assetCategory;
            if (!dictionary.TryGetValue(assetCategory, out var value))
            {
                value = (dictionary[assetCategory] = new List<Asset>());
            }
            value.Add(asset);
        }
        return dictionary;
    }

    private static Dictionary<Type, List<Asset>> GroupAssetsByType(List<Asset> assets)
    {
        Dictionary<Type, List<Asset>> dictionary = new Dictionary<Type, List<Asset>>();
        foreach (Asset asset in assets)
        {
            Type type = asset.GetType();
            if (!dictionary.TryGetValue(type, out var value))
            {
                value = (dictionary[type] = new List<Asset>());
            }
            value.Add(asset);
        }
        return dictionary;
    }

    private static void ExportAssetsToCsv(IEnumerable<Asset> assets, string csvPath)
    {
        using FileStream stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write);
        using StreamWriter streamWriter = new StreamWriter(stream);
        streamWriter.WriteLine("Name,GUID,Type,Origin,Legacy ID,Legacy Category");
        foreach (Asset asset in assets)
        {
            WriteEscapedString(streamWriter, asset.FriendlyName);
            streamWriter.Write(',');
            streamWriter.Write(asset.GUID.ToString("N"));
            streamWriter.Write(',');
            streamWriter.Write(asset.GetTypeFriendlyName());
            streamWriter.Write(',');
            WriteEscapedString(streamWriter, asset.GetOriginName());
            streamWriter.Write(',');
            streamWriter.Write(asset.id);
            streamWriter.Write(',');
            streamWriter.WriteLine(asset.assetCategory.ToString());
        }
    }

    private static void WriteEscapedString(StreamWriter sw, string value)
    {
        sw.Write('"');
        foreach (char c in value)
        {
            if (c == '"')
            {
                sw.Write('"');
            }
            sw.Write(c);
        }
        sw.Write('"');
    }
}
