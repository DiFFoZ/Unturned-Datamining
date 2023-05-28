using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SDG.Framework.Devkit;
using SDG.Framework.Modules;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unturned.SystemEx;
using Unturned.UnityEx;

namespace SDG.Unturned;

public class Assets : MonoBehaviour
{
    internal class AssetMapping
    {
        public Dictionary<EAssetType, Dictionary<ushort, Asset>> legacyAssetsTable;

        public Dictionary<Guid, Asset> assetDictionary;

        public List<Asset> assetList;

        public AssetMapping()
        {
            legacyAssetsTable = new Dictionary<EAssetType, Dictionary<ushort, Asset>>();
            legacyAssetsTable.Add(EAssetType.ITEM, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.EFFECT, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.OBJECT, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.RESOURCE, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.VEHICLE, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.ANIMAL, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.MYTHIC, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.SKIN, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.SPAWN, new Dictionary<ushort, Asset>());
            legacyAssetsTable.Add(EAssetType.NPC, new Dictionary<ushort, Asset>());
            assetDictionary = new Dictionary<Guid, Asset>();
            assetList = new List<Asset>();
        }
    }

    private static TypeRegistryDictionary _assetTypes = new TypeRegistryDictionary(typeof(Asset));

    private static TypeRegistryDictionary _useableTypes = new TypeRegistryDictionary(typeof(Useable));

    private static Assets instance;

    private static bool hasFinishedInitialStartupLoading;

    private static bool isLoadingAllAssets;

    private static bool isLoadingFromUpdate;

    public static AssetsRefreshed onAssetsRefreshed;

    internal static System.Action OnNewAssetsFinishedLoading;

    internal static AssetMapping defaultAssetMapping;

    private static AssetMapping currentAssetMapping;

    public static CommandLineFlag shouldLoadAnyAssets = new CommandLineFlag(defaultValue: true, "-SkipAssets");

    public static CommandLineFlag wantsDeferLoadingAssets = new CommandLineFlag(defaultValue: true, "-NoDeferAssets");

    public static CommandLineFlag shouldValidateAssets = new CommandLineFlag(defaultValue: false, "-ValidateAssets");

    public static CommandLineFlag shouldLogWorkshopAssets = new CommandLineFlag(defaultValue: false, "-LogWorkshopAssets");

    private static CommandLineFlag shouldCollectGarbageAggressively = new CommandLineFlag(defaultValue: false, "-AggressiveGC");

    private static CommandLineFlag shouldLogSpawnInsertions = new CommandLineFlag(defaultValue: false, "-LogSpawnInsertions");

    private static List<MasterBundleConfig> allMasterBundles;

    private static List<MasterBundleConfig> pendingMasterBundles;

    internal static MasterBundleConfig coreMasterBundle;

    internal static List<AssetOrigin> assetOrigins;

    internal static AssetOrigin coreOrigin;

    private static AssetOrigin reloadOrigin;

    private static AssetOrigin legacyServerSharedOrigin;

    private static AssetOrigin legacyPerServerOrigin;

    private static List<string> errors;

    private static bool hasUnlinkedSpawns;

    internal static readonly ClientStaticMethod<Guid> SendKickForInvalidGuid = ClientStaticMethod<Guid>.Get(ReceiveKickForInvalidGuid);

    internal static readonly ClientStaticMethod<Guid, string, string, byte[], string, string> SendKickForHashMismatch = ClientStaticMethod<Guid, string, string, byte[], string, string>.Get(ReceiveKickForHashMismatch);

    internal static AssetLoadingStats loadingStats = new AssetLoadingStats();

    private AssetsWorker worker;

    internal static AssetOrigin legacyOfficialOrigin;

    internal static AssetOrigin legacyMiscOrigin;

    internal static AssetOrigin legacyWorkshopOrigin;

    public static TypeRegistryDictionary assetTypes => _assetTypes;

    public static TypeRegistryDictionary useableTypes => _useableTypes;

    public static bool hasLoadedUgc { get; protected set; }

    public static bool hasLoadedMaps { get; protected set; }

    public static bool isLoading
    {
        get
        {
            if (!isLoadingAllAssets)
            {
                return isLoadingFromUpdate;
            }
            return true;
        }
    }

    internal static bool ShouldWaitForNewAssetsToFinishLoading
    {
        get
        {
            if (!isLoading)
            {
                return instance.worker.IsWorking;
            }
            return true;
        }
    }

    public static bool shouldDeferLoadingAssets
    {
        get
        {
            if ((bool)wantsDeferLoadingAssets)
            {
                return !shouldValidateAssets;
            }
            return false;
        }
    }

    public static MasterBundleConfig currentMasterBundle { get; private set; }

    private static string getExceptionMessage(Exception e)
    {
        if (e != null)
        {
            if (e.InnerException != null)
            {
                return e.InnerException.Message;
            }
            return e.Message;
        }
        return "Exception = Null";
    }

    public static void reportError(string error)
    {
        errors.Add(error);
        UnturnedLog.warn(error);
    }

    public static void reportError(Asset offendingAsset, string error)
    {
        error = offendingAsset.getTypeNameAndIdDisplayString() + ": " + error;
        reportError(error);
    }

    public static void reportError(Asset offendingAsset, string format, params object[] args)
    {
        string error = string.Format(format, args);
        reportError(offendingAsset, error);
    }

    public static void reportError(Asset offendingAsset, string format, object arg0)
    {
        string error = string.Format(format, arg0);
        reportError(offendingAsset, error);
    }

    public static void reportError(Asset offendingAsset, string format, object arg0, object arg1)
    {
        string error = string.Format(format, arg0, arg1);
        reportError(offendingAsset, error);
    }

    public static void reportError(Asset offendingAsset, string format, object arg0, object arg1, object arg2)
    {
        string error = string.Format(format, arg0, arg1, arg2);
        reportError(offendingAsset, error);
    }

    public static List<string> getReportedErrorsList()
    {
        return errors;
    }

    internal static AssetOrigin FindWorkshopFileOrigin(ulong workshopFileId)
    {
        foreach (AssetOrigin assetOrigin in assetOrigins)
        {
            if (assetOrigin.workshopFileId == workshopFileId)
            {
                return assetOrigin;
            }
        }
        return null;
    }

    private static AssetOrigin FindLevelOrigin(LevelInfo level)
    {
        if (level.publishedFileId != 0L)
        {
            return FindWorkshopFileOrigin(level.publishedFileId);
        }
        string b = "Map \"" + level.name + "\"";
        foreach (AssetOrigin assetOrigin in assetOrigins)
        {
            if (string.Equals(assetOrigin.name, b))
            {
                return assetOrigin;
            }
        }
        return null;
    }

    internal static AssetOrigin FindOrAddWorkshopFileOrigin(ulong workshopFileId, bool shouldOverrideIds)
    {
        AssetOrigin assetOrigin = FindWorkshopFileOrigin(workshopFileId);
        if (assetOrigin != null)
        {
            return assetOrigin;
        }
        AssetOrigin assetOrigin2 = new AssetOrigin();
        assetOrigin2.name = $"Workshop File ({workshopFileId})";
        assetOrigin2.workshopFileId = workshopFileId;
        assetOrigin2.shouldAssetsOverrideExistingIds = shouldOverrideIds;
        assetOrigins.Add(assetOrigin2);
        return assetOrigin2;
    }

    private static AssetOrigin FindOrAddLevelOrigin(LevelInfo level)
    {
        if (level.publishedFileId != 0L)
        {
            return FindOrAddWorkshopFileOrigin(level.publishedFileId, shouldOverrideIds: false);
        }
        string b = "Map \"" + level.name + "\"";
        foreach (AssetOrigin assetOrigin2 in assetOrigins)
        {
            if (string.Equals(assetOrigin2.name, b))
            {
                return assetOrigin2;
            }
        }
        AssetOrigin assetOrigin = new AssetOrigin();
        assetOrigin.name = b;
        assetOrigins.Add(assetOrigin);
        return assetOrigin;
    }

    public static Asset find(EAssetType type, ushort id)
    {
        if (type == EAssetType.NONE || id == 0)
        {
            return null;
        }
        currentAssetMapping.legacyAssetsTable[type].TryGetValue(id, out var value);
        return value;
    }

    public static T find<T>(AssetReference<T> reference) where T : Asset
    {
        if (!reference.isValid)
        {
            return null;
        }
        return find(reference.GUID) as T;
    }

    public static T Find_UseDefaultAssetMapping<T>(AssetReference<T> reference) where T : Asset
    {
        if (reference.isNull)
        {
            return null;
        }
        defaultAssetMapping.assetDictionary.TryGetValue(reference.GUID, out var value);
        return value as T;
    }

    public static T load<T>(ContentReference<T> reference) where T : UnityEngine.Object
    {
        if (!reference.isValid)
        {
            return null;
        }
        MasterBundleConfig masterBundleConfig = findMasterBundleByName(reference.name);
        if (masterBundleConfig != null && masterBundleConfig.assetBundle != null)
        {
            string text = masterBundleConfig.formatAssetPath(reference.path);
            T val = masterBundleConfig.assetBundle.LoadAsset<T>(text);
            if ((UnityEngine.Object)val == (UnityEngine.Object)null)
            {
                UnturnedLog.warn("Failed to load content reference '{0}' from master bundle '{1}' as {2}", text, reference.name, typeof(T).Name);
            }
            return val;
        }
        return null;
    }

    public static Asset find(Guid GUID)
    {
        currentAssetMapping.assetDictionary.TryGetValue(GUID, out var value);
        return value;
    }

    public static T find<T>(Guid guid) where T : Asset
    {
        return find(guid) as T;
    }

    public static EffectAsset FindEffectAssetByGuidOrLegacyId(Guid guid, ushort legacyId)
    {
        if (guid.IsEmpty())
        {
            return find(EAssetType.EFFECT, legacyId) as EffectAsset;
        }
        return find<EffectAsset>(guid);
    }

    public static T FindNpcAssetByGuidOrLegacyId<T>(Guid guid, ushort legacyId) where T : Asset
    {
        if (guid.IsEmpty())
        {
            return find(EAssetType.NPC, legacyId) as T;
        }
        return find<T>(guid);
    }

    public static VehicleAsset FindVehicleAssetByGuidOrLegacyId(Guid guid, ushort legacyId)
    {
        if (guid.IsEmpty())
        {
            return find(EAssetType.VEHICLE, legacyId) as VehicleAsset;
        }
        return find<VehicleAsset>(guid);
    }

    internal static T FindItemByGuidOrLegacyId<T>(Guid guid, ushort legacyId) where T : ItemAsset
    {
        if (guid.IsEmpty())
        {
            return find(EAssetType.ITEM, legacyId) as T;
        }
        return find<T>(guid);
    }

    public static void find<T>(List<T> results) where T : Asset
    {
        FindAssetsInListByType(currentAssetMapping.assetList, results);
    }

    internal static void FindAssetsByType_UseDefaultAssetMapping<T>(List<T> results) where T : Asset
    {
        FindAssetsInListByType(defaultAssetMapping.assetList, results);
    }

    private static void FindAssetsInListByType<T>(List<Asset> assetList, List<T> results) where T : Asset
    {
        foreach (Asset asset in assetList)
        {
            if (asset is T item)
            {
                results.Add(item);
            }
        }
    }

    public static Asset findByAbsolutePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        path = Path.GetFullPath(path);
        foreach (Asset asset in currentAssetMapping.assetList)
        {
            if (path.Equals(asset.absoluteOriginFilePath))
            {
                return asset;
            }
        }
        return null;
    }

    public static void runtimeCreate(Type type)
    {
        try
        {
            if (Activator.CreateInstance(type) is Asset asset)
            {
                asset.GUID = Guid.NewGuid();
                AddToMapping(asset, overrideExistingID: false, defaultAssetMapping);
                if (asset is IDirtyable)
                {
                    (asset as IDirtyable).isDirty = true;
                }
            }
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e);
        }
    }

    internal static void AddToMapping(Asset asset, bool overrideExistingID, AssetMapping assetMapping)
    {
        if (asset == null)
        {
            return;
        }
        EAssetType assetCategory = asset.assetCategory;
        if (assetCategory == EAssetType.SPAWN)
        {
            hasUnlinkedSpawns = true;
        }
        if (assetCategory == EAssetType.OBJECT)
        {
            if (overrideExistingID)
            {
                assetMapping.legacyAssetsTable[assetCategory].Remove(asset.id);
                assetMapping.legacyAssetsTable[assetCategory].Add(asset.id, asset);
            }
            else if (!assetMapping.legacyAssetsTable[assetCategory].ContainsKey(asset.id))
            {
                assetMapping.legacyAssetsTable[assetCategory].Add(asset.id, asset);
            }
        }
        else if (assetCategory != 0 && (assetCategory != EAssetType.ITEM || !(asset is ItemAsset itemAsset) || !itemAsset.isPro || itemAsset.id != 0))
        {
            if (overrideExistingID)
            {
                assetMapping.legacyAssetsTable[assetCategory].Remove(asset.id);
            }
            else if (assetMapping.legacyAssetsTable[assetCategory].ContainsKey(asset.id))
            {
                assetMapping.legacyAssetsTable[assetCategory].TryGetValue(asset.id, out var value);
                reportError(asset, "short ID is already taken by " + value.getTypeNameAndIdDisplayString() + "!");
                return;
            }
            assetMapping.legacyAssetsTable[assetCategory].Add(asset.id, asset);
        }
        if (asset.GUID != Guid.Empty)
        {
            if (overrideExistingID)
            {
                if (assetMapping.assetDictionary.TryGetValue(asset.GUID, out var value2))
                {
                    assetMapping.assetDictionary.Remove(value2.GUID);
                    assetMapping.assetList.Remove(value2);
                }
            }
            else if (assetMapping.assetDictionary.ContainsKey(asset.GUID))
            {
                assetMapping.assetDictionary.TryGetValue(asset.GUID, out var value3);
                reportError(asset, "long GUID " + asset.GUID.ToString("N") + " is already taken by " + value3.getTypeNameAndIdDisplayString() + "!");
                return;
            }
            assetMapping.assetDictionary.Add(asset.GUID, asset);
            assetMapping.assetList.Add(asset);
        }
        if (asset.origin != null && asset.origin.workshopFileId != 0L && (bool)shouldLogWorkshopAssets)
        {
            UnturnedLog.info(asset.getTypeNameAndIdDisplayString());
        }
    }

    private static void AddAssetsFromOriginToCurrentMapping(AssetOrigin origin)
    {
        UnturnedLog.info($"Adding {origin.assets.Count} asset(s) from origin {origin.name} to server mapping");
        foreach (Asset asset in origin.assets)
        {
            AddToMapping(asset, overrideExistingID: true, currentAssetMapping);
        }
    }

    internal static void ApplyServerAssetMapping(LevelInfo pendingLevel, List<PublishedFileId_t> serverWorkshopFileIds)
    {
        currentAssetMapping = new AssetMapping();
        List<AssetOrigin> list = new List<AssetOrigin>();
        list.Add(coreOrigin);
        AssetOrigin assetOrigin = null;
        if (pendingLevel != null)
        {
            assetOrigin = FindLevelOrigin(pendingLevel);
            if (assetOrigin != null)
            {
                list.Add(assetOrigin);
            }
        }
        if (serverWorkshopFileIds != null)
        {
            foreach (PublishedFileId_t serverWorkshopFileId in serverWorkshopFileIds)
            {
                AssetOrigin assetOrigin2 = FindWorkshopFileOrigin(serverWorkshopFileId.m_PublishedFileId);
                if (assetOrigin2 != null)
                {
                    if (assetOrigin2 != assetOrigin)
                    {
                        list.Add(assetOrigin2);
                    }
                }
                else
                {
                    UnturnedLog.info($"Unable to find assets for server mapping (file ID {serverWorkshopFileId})");
                }
            }
        }
        if (Dedicator.IsDedicatedServer)
        {
            foreach (AssetOrigin assetOrigin3 in assetOrigins)
            {
                if (assetOrigin3 != reloadOrigin && assetOrigin3.assets.Count >= 1 && !list.Contains(assetOrigin3))
                {
                    UnturnedLog.info("Inserting asset origin " + assetOrigin3.name + " before other assets to reduce chances of ID conflicts because otherwise it was not included");
                    list.Insert(0, assetOrigin3);
                }
            }
        }
        foreach (AssetOrigin item in list)
        {
            AddAssetsFromOriginToCurrentMapping(item);
        }
    }

    internal static void ClearServerAssetMapping()
    {
        currentAssetMapping = defaultAssetMapping;
    }

    public static void RequestReloadAllAssets()
    {
        if (hasFinishedInitialStartupLoading && !isLoading)
        {
            instance.StartCoroutine(instance.LoadAllAssets());
        }
    }

    private static MasterBundleConfig LoadMasterBundleConfig(AssetsWorker.MasterBundle mb)
    {
        try
        {
            MasterBundleConfig masterBundleConfig = findMasterBundleByName(mb.config.assetBundleName);
            if (masterBundleConfig == null)
            {
                masterBundleConfig = findMasterBundleInListByName(pendingMasterBundles, mb.config.assetBundleName);
            }
            if (masterBundleConfig != null)
            {
                UnturnedLog.info("Found duplicate of master bundle '{0}' originally in '{1}' at '{2}'", mb.config.assetBundleName, masterBundleConfig.directoryPath, mb.config.directoryPath);
                return null;
            }
            return mb.config;
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e);
        }
        return null;
    }

    public static MasterBundleConfig findMasterBundleByPath(string path)
    {
        int num = 0;
        MasterBundleConfig result = null;
        foreach (MasterBundleConfig allMasterBundle in allMasterBundles)
        {
            if (allMasterBundle.directoryPath.Length < num || !path.StartsWith(allMasterBundle.directoryPath))
            {
                continue;
            }
            if (path.Length > allMasterBundle.directoryPath.Length)
            {
                char c = path[allMasterBundle.directoryPath.Length];
                if (c != '/' && c != '\\')
                {
                    continue;
                }
            }
            num = allMasterBundle.directoryPath.Length;
            result = allMasterBundle;
        }
        return result;
    }

    public static MasterBundleConfig findMasterBundleInListByName(List<MasterBundleConfig> list, string name, bool matchExtension = true)
    {
        foreach (MasterBundleConfig item in list)
        {
            if ((matchExtension ? item.assetBundleName : item.assetBundleNameWithoutExtension).Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }

    public static MasterBundleConfig findMasterBundleByName(string name, bool matchExtension = true)
    {
        return findMasterBundleInListByName(allMasterBundles, name, matchExtension);
    }

    private static void UnloadAllMasterBundles()
    {
        foreach (MasterBundleConfig allMasterBundle in allMasterBundles)
        {
            allMasterBundle.unload();
        }
        allMasterBundles.Clear();
    }

    private static void TryLoadFile(AssetsWorker.AssetDefinition file)
    {
        try
        {
            loadingStats.totalFilesLoaded++;
            LoadFile(file);
        }
        catch (Exception e)
        {
            UnturnedLog.error("Exception loading file {0}:", file.path);
            UnturnedLog.exception(e);
        }
    }

    private static void LoadFile(AssetsWorker.AssetDefinition file)
    {
        string path = file.path;
        DatDictionary assetData = file.assetData;
        byte[] hash = file.hash;
        if (!string.IsNullOrEmpty(file.assetError))
        {
            reportError("Error parsing \"" + path + "\": \"" + file.assetError + "\"");
        }
        string directoryName = Path.GetDirectoryName(path);
        string text = (path.EndsWith("Asset.dat", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(directoryName) : Path.GetFileNameWithoutExtension(path));
        Guid value = default(Guid);
        Type type = null;
        if (assetData.TryGetDictionary("Metadata", out var node))
        {
            if (!node.TryParseGuid("GUID", out value))
            {
                reportError("Unable to parse Metadata.GUID in \"" + path + "\"");
                return;
            }
            type = node.ParseType("Type");
            if (type == null)
            {
                reportError("Unable to parse Metadata.Type in \"" + path + "\"");
                return;
            }
        }
        else if (!assetData.TryParseGuid("GUID", out value))
        {
            value = Guid.NewGuid();
            try
            {
                string text2 = File.ReadAllText(path);
                text2 = "GUID " + value.ToString("N") + "\n" + text2;
                File.WriteAllText(path, text2);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught IO exception adding GUID to \"" + path + "\":");
            }
        }
        if (value.IsEmpty())
        {
            reportError("Cannot use empty GUID in \"" + path + "\"");
            return;
        }
        DatDictionary datDictionary = assetData;
        if (assetData.TryGetDictionary("Asset", out var node2))
        {
            datDictionary = node2;
        }
        if (type == null)
        {
            string @string = datDictionary.GetString("Type");
            if (string.IsNullOrEmpty(@string))
            {
                reportError("Missing asset Type in \"" + path + "\"");
                return;
            }
            type = assetTypes.getType(@string);
            if (type == null)
            {
                type = datDictionary.ParseType("Type");
                if (type == null)
                {
                    reportError("Unhandled asset type \"" + @string + "\" in \"" + path + "\"");
                    return;
                }
            }
        }
        if (!typeof(Asset).IsAssignableFrom(type))
        {
            reportError($"Type \"{type}\" is not a valid asset type in \"{path}\"");
            return;
        }
        MasterBundleConfig masterBundleConfig = findMasterBundleByPath(path);
        string string2 = datDictionary.GetString("Master_Bundle_Override");
        if (string2 != null)
        {
            masterBundleConfig = findMasterBundleByName(string2);
            if (masterBundleConfig == null)
            {
                UnturnedLog.warn("Unable to find master bundle override '{0}' for '{1}'", string2, path);
            }
        }
        else if (datDictionary.ContainsKey("Exclude_From_Master_Bundle"))
        {
            masterBundleConfig = null;
        }
        if (masterBundleConfig != null && masterBundleConfig.assetBundle == null)
        {
            UnturnedLog.warn("Skipping master bundle '{0}' for '{1}' because asset bundle is null", masterBundleConfig.assetBundleName, path);
            masterBundleConfig = null;
        }
        currentMasterBundle = masterBundleConfig;
        int a = -1;
        Bundle bundle;
        if (masterBundleConfig != null)
        {
            if (!datDictionary.TryGetString("Bundle_Override_Path", out var value2))
            {
                value2 = directoryName.Substring(masterBundleConfig.directoryPath.Length);
                value2 = value2.Replace('\\', '/');
            }
            bundle = new MasterBundle(masterBundleConfig, value2, text);
            a = masterBundleConfig.version;
        }
        else if (datDictionary.ContainsKey("Bundle_Override_Path"))
        {
            string string3 = datDictionary.GetString("Bundle_Override_Path");
            int num = string3.LastIndexOf('/');
            string text3 = ((num != -1) ? string3.Substring(num + 1) : string3);
            string3 = string3 + "/" + text3 + ".unity3d";
            bundle = new Bundle(string3, usePath: false, text);
        }
        else
        {
            bundle = new Bundle(directoryName + "/" + text + ".unity3d", usePath: false);
        }
        bundle.isCoreAsset = file.origin == coreOrigin;
        int num2 = datDictionary.ParseInt32("Asset_Bundle_Version", 1);
        if (num2 < 1)
        {
            reportError(text + " Lowest individual asset bundle version is 1 (default), associated with 5.5.");
            num2 = 1;
        }
        else if (num2 > 4)
        {
            reportError(text + " Highest individual asset bundle version is 4, associated with 2020 LTS.");
            num2 = 4;
        }
        int num3 = Mathf.Max(a, num2);
        bundle.convertShadersToStandard = num3 < 2;
        bundle.consolidateShaders = num3 < 3 || (datDictionary.ContainsKey("Enable_Shader_Consolidation") && !datDictionary.ContainsKey("Disable_Shader_Consolidation"));
        Local localization = new Local(file.translationData, file.fallbackTranslationData);
        ushort id = datDictionary.ParseUInt16("ID", 0);
        Asset asset;
        try
        {
            asset = Activator.CreateInstance(type) as Asset;
        }
        catch (Exception e2)
        {
            reportError($"Caught exception while constructing {type} in \"{path}\": {getExceptionMessage(e2)}");
            UnturnedLog.exception(e2);
            bundle.unload();
            currentMasterBundle = null;
            return;
        }
        if (asset == null)
        {
            reportError($"Failed to construct {type} in \"{path}\"");
            bundle.unload();
            currentMasterBundle = null;
            return;
        }
        try
        {
            asset.id = id;
            asset.GUID = value;
            asset.hash = hash;
            asset.requiredShaderUpgrade = bundle.convertShadersToStandard || bundle.consolidateShaders;
            asset.absoluteOriginFilePath = path;
            asset.origin = file.origin;
            asset.PopulateAsset(bundle, datDictionary, localization);
            asset.origin.assets.Add(asset);
            AddToMapping(asset, file.origin.shouldAssetsOverrideExistingIds, defaultAssetMapping);
            bundle.unload();
        }
        catch (Exception e3)
        {
            reportError("Caught exception while populating \"" + path + "\": " + getExceptionMessage(e3));
            UnturnedLog.exception(e3);
            bundle.unload();
        }
        currentMasterBundle = null;
    }

    public static void RequestAddSearchLocation(string absoluteDirectoryPath, AssetOrigin origin)
    {
        instance.AddSearchLocation(absoluteDirectoryPath, origin);
    }

    public static void reload(string absolutePath)
    {
        if (hasFinishedInitialStartupLoading && !isLoading)
        {
            loadingStats.Reset();
            RequestAddSearchLocation(absolutePath, reloadOrigin);
        }
    }

    public static void linkSpawnsIfDirty()
    {
        if (hasUnlinkedSpawns)
        {
            UnturnedLog.info("Linking spawns because changes were detected");
            linkSpawns();
        }
        else
        {
            UnturnedLog.info("Skipping link spawns because no changes were detected");
        }
    }

    public static void linkSpawns()
    {
        if (!hasUnlinkedSpawns)
        {
            return;
        }
        hasUnlinkedSpawns = false;
        Dictionary<ushort, Asset>.ValueCollection values = defaultAssetMapping.legacyAssetsTable[EAssetType.SPAWN].Values;
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        foreach (SpawnAsset item in values)
        {
            if (item.insertRoots.Count < 1)
            {
                continue;
            }
            foreach (SpawnTable insertRoot in item.insertRoots)
            {
                if (insertRoot.spawnID == 0)
                {
                    continue;
                }
                if (!(find(EAssetType.SPAWN, insertRoot.spawnID) is SpawnAsset spawnAsset2))
                {
                    reportError(item, "unable to find root {0} during link", insertRoot.spawnID);
                    continue;
                }
                insertRoot.spawnID = item.id;
                insertRoot.isLink = true;
                spawnAsset2.tables.Add(insertRoot);
                if (insertRoot.isOverride)
                {
                    spawnAsset2.markOverridden();
                }
                spawnAsset2.markTablesDirty();
                num++;
                if ((bool)shouldLogSpawnInsertions)
                {
                    if (insertRoot.isOverride)
                    {
                        UnturnedLog.info("Spawn {0} overriding {1}", item.name, spawnAsset2.name);
                    }
                    else
                    {
                        UnturnedLog.info("Spawn {0} inserted into {1}", item.name, spawnAsset2.name);
                    }
                }
            }
            item.insertRoots.Clear();
        }
        foreach (SpawnAsset item2 in values)
        {
            if (item2.areTablesDirty)
            {
                item2.sortAndNormalizeWeights();
                num2++;
            }
        }
        foreach (SpawnAsset item3 in values)
        {
            foreach (SpawnTable table in item3.tables)
            {
                if (table.spawnID != 0 && !table.hasNotifiedChild)
                {
                    table.hasNotifiedChild = true;
                    if (!(find(EAssetType.SPAWN, table.spawnID) is SpawnAsset spawnAsset5))
                    {
                        reportError(item3, "unable to find child table {0} during link", table.spawnID);
                        continue;
                    }
                    SpawnTable spawnTable = new SpawnTable();
                    spawnTable.assetID = 0;
                    spawnTable.spawnID = item3.id;
                    spawnTable.weight = table.weight;
                    spawnTable.chance = table.chance;
                    spawnTable.isLink = table.isLink;
                    spawnAsset5.roots.Add(spawnTable);
                    num3++;
                }
            }
        }
        UnturnedLog.info("Link spawns: {0} children, {1} sorted/normalized and {2} parents", num, num2, num3);
    }

    public static void initializeMasterBundleValidation()
    {
        MasterBundleValidation.initialize(allMasterBundles);
    }

    private void CheckForBlueprintErrors()
    {
        Func<Blueprint, Blueprint, bool> func = delegate(Blueprint myBlueprint, Blueprint yourBlueprint)
        {
            if (myBlueprint.type != yourBlueprint.type)
            {
                return false;
            }
            if (myBlueprint.outputs.Length != yourBlueprint.outputs.Length)
            {
                return false;
            }
            if (myBlueprint.supplies.Length != yourBlueprint.supplies.Length)
            {
                return false;
            }
            if (myBlueprint.questConditions.Length != yourBlueprint.questConditions.Length)
            {
                return false;
            }
            if (myBlueprint.questRewards.Length != yourBlueprint.questRewards.Length)
            {
                return false;
            }
            if (myBlueprint.tool != yourBlueprint.tool)
            {
                return false;
            }
            for (byte b5 = 0; b5 < myBlueprint.outputs.Length; b5 = (byte)(b5 + 1))
            {
                if (myBlueprint.outputs[b5].id != yourBlueprint.outputs[b5].id)
                {
                    return false;
                }
            }
            for (byte b6 = 0; b6 < myBlueprint.supplies.Length; b6 = (byte)(b6 + 1))
            {
                if (myBlueprint.supplies[b6].id != yourBlueprint.supplies[b6].id)
                {
                    return false;
                }
            }
            for (int k = 0; k < myBlueprint.questConditions.Length; k++)
            {
                if (!myBlueprint.questConditions[k].Equals(yourBlueprint.questConditions[k]))
                {
                    return false;
                }
            }
            for (int l = 0; l < myBlueprint.questRewards.Length; l++)
            {
                if (!myBlueprint.questRewards[l].Equals(yourBlueprint.questRewards[l]))
                {
                    return false;
                }
            }
            return true;
        };
        List<ItemAsset> list = new List<ItemAsset>();
        find(list);
        if (list.Count <= 0)
        {
            return;
        }
        for (int i = 0; i < list.Count; i++)
        {
            ItemAsset itemAsset = list[i];
            for (byte b = 0; b < itemAsset.blueprints.Count; b = (byte)(b + 1))
            {
                Blueprint blueprint = itemAsset.blueprints[b];
                for (byte b2 = 0; b2 < itemAsset.blueprints.Count; b2 = (byte)(b2 + 1))
                {
                    if (b2 != b)
                    {
                        Blueprint arg = itemAsset.blueprints[b2];
                        if (func(blueprint, arg))
                        {
                            reportError(itemAsset, "has an identical blueprint: " + blueprint);
                        }
                    }
                }
            }
            for (int j = 0; j < list.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }
                ItemAsset itemAsset2 = list[j];
                for (byte b3 = 0; b3 < itemAsset.blueprints.Count; b3 = (byte)(b3 + 1))
                {
                    Blueprint blueprint2 = itemAsset.blueprints[b3];
                    for (byte b4 = 0; b4 < itemAsset2.blueprints.Count; b4 = (byte)(b4 + 1))
                    {
                        Blueprint arg2 = itemAsset2.blueprints[b4];
                        if (func(blueprint2, arg2))
                        {
                            reportError(itemAsset, "shares an identical blueprint with " + itemAsset2.itemName + ": " + blueprint2);
                        }
                    }
                }
            }
        }
    }

    private void CheckForNpcErrors()
    {
        List<DialogueAsset> list = new List<DialogueAsset>();
        find(list);
        foreach (DialogueAsset item in list)
        {
            int num = item.responses.Length;
            for (int i = 0; i < num; i++)
            {
                DialogueResponse dialogueResponse = item.responses[i];
                if (!dialogueResponse.IsDialogueRefNull() && dialogueResponse.FindDialogueAsset() == null)
                {
                    reportError(item, "unable to find dialogue asset for response " + i);
                }
                if (!dialogueResponse.IsVendorRefNull() && dialogueResponse.FindVendorAsset() == null)
                {
                    reportError(item, "unable to find vendor asset for response " + i);
                }
            }
        }
    }

    private void CleanupMemory()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    private void AddDedicatedServerUgcSearchLocations()
    {
        string path = Path.Combine(ReadWrite.PATH, "Bundles", "Workshop", "Content");
        if (ReadWrite.folderExists(path, usePath: false))
        {
            AddSearchLocation(path, legacyServerSharedOrigin);
        }
        string path2 = Path.Combine(ReadWrite.PATH, ServerSavedata.directoryName, Provider.serverID, "Workshop", "Content");
        if (ReadWrite.folderExists(path2, usePath: false))
        {
            AddSearchLocation(path2, legacyPerServerOrigin);
        }
        string path3 = Path.Combine(ReadWrite.PATH, ServerSavedata.directoryName, Provider.serverID, "Bundles");
        if (ReadWrite.folderExists(path3, usePath: false))
        {
            AddSearchLocation(path3, legacyPerServerOrigin);
        }
    }

    private void AddClientUgcSearchLocations()
    {
        if (Provider.provider.workshopService.ugc == null)
        {
            return;
        }
        SteamContent[] array = Provider.provider.workshopService.ugc.ToArray();
        hasLoadedUgc = true;
        SteamContent[] array2 = array;
        foreach (SteamContent steamContent in array2)
        {
            if (LocalWorkshopSettings.get().getEnabled(steamContent.publishedFileID) && (steamContent.type == ESteamUGCType.OBJECT || steamContent.type == ESteamUGCType.ITEM || steamContent.type == ESteamUGCType.VEHICLE))
            {
                AssetOrigin origin = FindOrAddWorkshopFileOrigin(steamContent.publishedFileID.m_PublishedFileId, shouldOverrideIds: false);
                AddSearchLocation(steamContent.path, origin);
            }
        }
    }

    private void AddSandboxSearchLocations()
    {
        string path = Path.Combine(ReadWrite.PATH, "Sandbox");
        if (Directory.Exists(path))
        {
            string[] folders = ReadWrite.getFolders(path, usePath: false);
            foreach (string text in folders)
            {
                UnturnedLog.info("Sandbox: {0}", text);
                AssetOrigin assetOrigin = new AssetOrigin();
                assetOrigin.name = "Sandbox Folder \"" + text + "\"";
                assetOrigin.shouldAssetsOverrideExistingIds = true;
                assetOrigins.Add(assetOrigin);
                AddSearchLocation(text, assetOrigin);
            }
        }
        else
        {
            Directory.CreateDirectory(path);
        }
    }

    private void AddMapSearchLocations()
    {
        LevelInfo[] levels = Level.getLevels(ESingleplayerMapCategory.ALL);
        hasLoadedMaps = true;
        foreach (LevelInfo levelInfo in levels)
        {
            if (levelInfo != null)
            {
                string path = Path.Combine(levelInfo.path, "Bundles");
                if (ReadWrite.folderExists(path, usePath: false))
                {
                    AssetOrigin origin = FindOrAddLevelOrigin(levelInfo);
                    AddSearchLocation(path, origin);
                }
            }
        }
    }

    private void AddSearchLocation(string path, AssetOrigin origin)
    {
        path = Path.GetFullPath(path);
        UnturnedLog.info(origin.name + " added asset search location \"" + path + "\"");
        worker.RequestSearch(path, origin);
    }

    private IEnumerator LoadAssetsFromWorkerThread()
    {
        double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
        while (worker.IsWorking || pendingMasterBundles.Count > 0)
        {
            if (worker.TryDequeueMasterBundle(out var result))
            {
                MasterBundleConfig masterBundleConfig = LoadMasterBundleConfig(result);
                if (masterBundleConfig != null)
                {
                    pendingMasterBundles.Add(masterBundleConfig);
                    masterBundleConfig.StartLoad(result.assetBundleData, result.hash);
                    loadingStats.isLoadingAssetBundles = true;
                }
                continue;
            }
            AssetsWorker.AssetDefinition result2;
            if (pendingMasterBundles.Count > 0)
            {
                for (int num = pendingMasterBundles.Count - 1; num >= 0; num--)
                {
                    MasterBundleConfig masterBundleConfig2 = pendingMasterBundles[num];
                    if (masterBundleConfig2.assetBundleCreateRequest.isDone)
                    {
                        pendingMasterBundles.RemoveAtFast(num);
                        masterBundleConfig2.FinishLoad();
                        loadingStats.totalMasterBundlesLoaded++;
                        if (masterBundleConfig2.assetBundle != null)
                        {
                            if (masterBundleConfig2.origin == coreOrigin)
                            {
                                coreMasterBundle = masterBundleConfig2;
                            }
                            allMasterBundles.Add(masterBundleConfig2);
                        }
                    }
                }
                if (pendingMasterBundles.Count < 1)
                {
                    loadingStats.isLoadingAssetBundles = false;
                }
            }
            else if (coreMasterBundle != null && worker.TryDequeueAssetDefinition(out result2))
            {
                TryLoadFile(result2);
            }
            if (Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble > 0.05)
            {
                SyncAssetDefinitionLoadingProgress();
                if ((bool)shouldCollectGarbageAggressively)
                {
                    CleanupMemory();
                }
                yield return null;
                realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
            }
        }
    }

    internal static void SyncAssetDefinitionLoadingProgress()
    {
        loadingStats.totalRegisteredSearchLocations = instance.worker.totalSearchLocationRequests;
        loadingStats.totalSearchLocationsFinishedSearching = instance.worker.totalSearchLocationsFinishedSearching;
        loadingStats.totalMasterBundlesFound = instance.worker.totalMasterBundlesFound;
        loadingStats.totalFilesFound = instance.worker.totalAssetDefinitionsFound;
        loadingStats.totalFilesRead = instance.worker.totalAssetDefinitionsRead;
        LoadingUI.NotifyAssetDefinitionLoadingProgress();
    }

    private IEnumerator LoadAllAssets()
    {
        isLoadingAllAssets = true;
        double startTime = Time.realtimeSinceStartupAsDouble;
        if (errors == null)
        {
            errors = new List<string>();
        }
        else
        {
            errors.Clear();
        }
        defaultAssetMapping = new AssetMapping();
        currentAssetMapping = defaultAssetMapping;
        coreMasterBundle = null;
        if (allMasterBundles == null)
        {
            allMasterBundles = new List<MasterBundleConfig>();
            pendingMasterBundles = new List<MasterBundleConfig>();
        }
        else
        {
            UnloadAllMasterBundles();
        }
        assetOrigins = new List<AssetOrigin>();
        loadingStats.Reset();
        coreOrigin = new AssetOrigin();
        coreOrigin.name = "Vanilla Built-in Assets";
        assetOrigins.Add(coreOrigin);
        reloadOrigin = new AssetOrigin();
        reloadOrigin.name = "Reloaded Assets (Debug)";
        reloadOrigin.shouldAssetsOverrideExistingIds = true;
        assetOrigins.Add(reloadOrigin);
        legacyServerSharedOrigin = new AssetOrigin();
        legacyServerSharedOrigin.name = "Server Common (Legacy)";
        assetOrigins.Add(legacyServerSharedOrigin);
        legacyPerServerOrigin = new AssetOrigin();
        legacyPerServerOrigin.name = "Per-Server (Legacy)";
        assetOrigins.Add(legacyPerServerOrigin);
        yield return null;
        if ((bool)shouldLoadAnyAssets)
        {
            AddSearchLocation(Path.Combine(ReadWrite.PATH, "Bundles"), coreOrigin);
            if (Dedicator.IsDedicatedServer)
            {
                AddDedicatedServerUgcSearchLocations();
            }
            else
            {
                AddClientUgcSearchLocations();
            }
            AddSandboxSearchLocations();
            AddMapSearchLocations();
            yield return null;
            if (!Dedicator.IsDedicatedServer)
            {
                Provider.initAutoSubscribeMaps();
            }
            yield return LoadAssetsFromWorkerThread();
        }
        LoadingUI.SetLoadingText("Loading_Blueprints");
        yield return null;
        if ((bool)shouldValidateAssets)
        {
            CheckForBlueprintErrors();
        }
        LoadingUI.SetLoadingText("Loading_Spawns");
        yield return null;
        if (!Dedicator.IsDedicatedServer)
        {
            linkSpawns();
        }
        if ((bool)shouldValidateAssets)
        {
            CheckForNpcErrors();
        }
        LoadingUI.SetLoadingText("Loading_Misc");
        yield return null;
        onAssetsRefreshed?.Invoke();
        yield return null;
        UnturnedLog.info($"Loading all assets took {Time.realtimeSinceStartupAsDouble - startTime}s");
        isLoadingAllAssets = false;
    }

    private IEnumerator StartupAssetLoading()
    {
        yield return LoadAllAssets();
        hasFinishedInitialStartupLoading = true;
        if (Dedicator.IsDedicatedServer)
        {
            Provider.host();
            yield break;
        }
        LoadingUI.SetLoadingText("Loading_MainMenu");
        yield return null;
        UnturnedLog.info("Launching main menu");
        SceneManager.LoadScene("Menu");
    }

    private IEnumerator LoadNewAssetsFromUpdate()
    {
        isLoadingFromUpdate = true;
        double startTime = Time.realtimeSinceStartupAsDouble;
        yield return LoadAssetsFromWorkerThread();
        linkSpawnsIfDirty();
        UnturnedLog.info($"Loading new assets took {Time.realtimeSinceStartupAsDouble - startTime}s");
        isLoadingFromUpdate = false;
        OnNewAssetsFinishedLoading?.Invoke();
    }

    private bool TestDedicatedServerSteamRedist()
    {
        string text = PathEx.Join(UnityPaths.GameDirectory, "linux64", "steamclient.so");
        if (!File.Exists(text))
        {
            CommandWindow.LogError("Missing steamclient redist file at: " + text);
            return false;
        }
        try
        {
            FileInfo fileInfo = new FileInfo(text);
            DateTime dateTime = new DateTime(2021, 9, 14, 21, 30, 0, DateTimeKind.Utc);
            if (fileInfo.LastWriteTimeUtc >= dateTime)
            {
                return true;
            }
            CommandWindow.LogError($"Out-of-date steamclient redist file (expected: {dateTime} actual: {fileInfo.LastWriteTimeUtc})");
            return false;
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Unable to get steamclient redist file info");
            return false;
        }
    }

    private void Start()
    {
        if (Dedicator.IsDedicatedServer)
        {
            Module moduleByName = ModuleHook.getModuleByName("Rocket.Unturned");
            if (moduleByName != null)
            {
                uint uInt32FromIP = Parser.getUInt32FromIP("4.9.3.1");
                if (moduleByName.config.Version_Internal < uInt32FromIP)
                {
                    CommandWindow.LogError("Upgrading to the officially maintained version of Rocket, or a custom fork of it, is required.");
                    CommandWindow.LogErrorFormat("Installed version: {0} Maintained version: 4.9.3.3+", moduleByName.config.Version);
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("--- Overview ---");
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("SDG maintains a fork of Rocket called the Legally Distinct Missile (or LDM) after the resignation of its original community team. Using this fork is important because it preserves compatibility, and has fixes for important legacy Rocket issues like multithreading exceptions and teleportation exploits.");
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("--- Installation ---");
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("The dedicated server includes the latest version, so an external download is not necessary:");
                    CommandWindow.Log("1. Copy the Rocket.Unturned module from the game's Extras directory.");
                    CommandWindow.Log("2. Paste it into the game's Modules directory.");
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("--- Resources ---");
                    CommandWindow.Log(string.Empty);
                    CommandWindow.Log("https://github.com/SmartlyDressedGames/Legally-Distinct-Missile");
                    CommandWindow.Log("https://www.reddit.com/r/UnturnedLDM/");
                    CommandWindow.Log("https://forum.smartlydressedgames.com/c/modding/ldm");
                    CommandWindow.Log("https://steamcommunity.com/app/304930/discussions/17/");
                    return;
                }
            }
            CommandWindow.LogError("Hosting dedicated servers using client files has been deprecated since June 2019.");
            CommandWindow.Log("Please use the standalone dedicated server app ID 1110390 available through SteamCMD instead.");
            CommandWindow.Log("For more information and an installation guide read more at:");
            CommandWindow.Log("https://docs.smartlydressedgames.com/en/stable/servers/server-hosting.html");
        }
        else
        {
            worker = new AssetsWorker();
            worker.Initialize();
            StartCoroutine(StartupAssetLoading());
        }
    }

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        worker.Update();
        if (!isLoading && worker.IsWorking)
        {
            StartCoroutine(LoadNewAssetsFromUpdate());
        }
    }

    private void OnDestroy()
    {
        worker.Shutdown();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveKickForInvalidGuid(Guid guid)
    {
        Provider._connectionFailureInfo = ESteamConnectionFailureInfo.CUSTOM;
        Asset asset = find(guid);
        if (asset != null)
        {
            Provider._connectionFailureReason = $"Server missing asset: \"{asset.FriendlyName}\" File: \"{asset.name}\" Id: {guid:N}";
        }
        else
        {
            Provider._connectionFailureReason = string.Concat(string.Concat(string.Concat("Client and server are both missing unknown asset! ID: " + guid.ToString("N"), "\nThis probably means either an invalid ID was sent by the server,"), "\nthe ID got corrupted for example by plugins modifying network traffic,"), "\nor a required level asset like materials/foliage/trees/objects is missing.");
        }
        Provider.RequestDisconnect($"Kicked for sending invalid asset guid: {guid:N}");
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveKickForHashMismatch(Guid guid, string serverName, string serverFriendlyName, byte[] serverHash, string serverAssetBundleNameWithoutExtension, string serverAssetOrigin)
    {
        Provider._connectionFailureInfo = ESteamConnectionFailureInfo.CUSTOM;
        Asset asset = find(guid);
        if (asset != null)
        {
            string text = asset.origin?.name;
            if (string.IsNullOrEmpty(text))
            {
                text = "Unknown";
            }
            string text2;
            if (string.Equals(asset.name, serverName) && string.Equals(asset.FriendlyName, serverFriendlyName))
            {
                if (!string.IsNullOrEmpty(serverAssetBundleNameWithoutExtension) && asset.originMasterBundle != null && !string.Equals(asset.originMasterBundle.assetBundleNameWithoutExtension, serverAssetBundleNameWithoutExtension))
                {
                    text2 = $"Client and server loaded \"{serverFriendlyName}\" from different asset bundles! (File: \"{asset.name}\" ID: {guid:N})";
                    text2 = text2 + "\nClient asset bundle is \"" + asset.originMasterBundle.assetBundleNameWithoutExtension + "\", whereas server asset bundle is \"" + serverAssetBundleNameWithoutExtension + "\".";
                }
                else if (!string.IsNullOrEmpty(serverAssetBundleNameWithoutExtension) && asset.originMasterBundle == null)
                {
                    text2 = $"Client loaded \"{serverFriendlyName}\" from legacy asset bundle but server did not! (File: \"{asset.name}\" ID: {guid:N})";
                    text2 = text2 + "\nServer asset bundle name: \"" + serverAssetBundleNameWithoutExtension + "\".";
                }
                else if (string.IsNullOrEmpty(serverAssetBundleNameWithoutExtension) && asset.originMasterBundle != null)
                {
                    text2 = $"Server loaded \"{serverFriendlyName}\" from legacy asset bundle but client did not! (File: \"{asset.name}\" ID: {guid:N})";
                    text2 = text2 + "\nClient asset bundle name: \"" + asset.originMasterBundle.assetBundleNameWithoutExtension + "\"";
                }
                else if (Hash.verifyHash(asset.hash, serverHash))
                {
                    text2 = $"Server asset bundle hash out of date for \"{serverFriendlyName}\"! (File: \"{asset.name}\" ID: {guid:N})";
                    text2 = text2 + "\nThis probably means the mod creator should re-export the \"" + serverAssetBundleNameWithoutExtension + "\" asset bundle.";
                }
                else
                {
                    text2 = $"Client and server disagree on asset \"{asset.FriendlyName}\" configuration. (File: \"{asset.name}\" ID: {guid:N})";
                    text2 += "\nUsually this means the files are different versions in which case updating the client and server might fix it.";
                    text2 += "\nAlternatively the file may have been corrupted, locally modified, or modified on the server.";
                    text2 = text2 + "\nClient hash is " + Hash.toString(asset.hash) + ", whereas server hash is " + Hash.toString(serverHash) + ".";
                }
            }
            else
            {
                text2 = $"Client and server have different assets with the same ID! ({guid:N})";
                text2 += "\nThis probably means an existing file was copied, but the mod creator can fix it by changing the ID.";
                text2 = ((!string.Equals(asset.FriendlyName, serverFriendlyName)) ? (text2 + "\nClient display name is \"" + asset.FriendlyName + "\", whereas server display name is \"" + serverFriendlyName + "\".") : (text2 + "\nDisplay name \"" + serverFriendlyName + "\" matches between client and server."));
                text2 = ((!string.Equals(asset.name, serverName)) ? (text2 + "\nClient file name is \"" + asset.name + "\", whereas server file name is \"" + serverName + "\".") : (text2 + "\nFile name \"" + asset.name + "\" matches between client and server."));
            }
            text2 = (Provider._connectionFailureReason = ((!string.Equals(text, serverAssetOrigin)) ? (text2 + "\nClient asset is from " + text + ", whereas server asset is from " + serverAssetOrigin + ".") : (text2 + "\nClient and server agree this asset is from " + text + ".")));
        }
        else
        {
            Provider._connectionFailureReason = $"Unknown asset hash mismatch? (should never happen) Name: \"{serverFriendlyName}\" File: \"{serverName}\" Id: {guid:N}";
        }
        Provider.RequestDisconnect($"Kicked for asset hash mismatch guid: {guid:N} serverName: \"{serverName}\" serverFriendlyName: \"{serverFriendlyName}\" serverHash: {Hash.toString(serverHash)} serverAssetBundleName: \"{serverAssetBundleNameWithoutExtension}\" serverAssetOrigin: \"{serverAssetOrigin}\"");
    }

    [Obsolete("Renamed to RequestAddSearchLocation")]
    public static void load(string absoluteDirectoryPath, AssetOrigin origin, bool overrideExistingIDs)
    {
        RequestAddSearchLocation(absoluteDirectoryPath, origin);
    }

    [Obsolete("Renamed to RequestReloadAllAssets")]
    public static void refresh()
    {
        RequestReloadAllAssets();
    }

    [Obsolete]
    public static void rename(Asset asset, string newName)
    {
    }

    [Obsolete]
    public static AssetOrigin ConvertLegacyOrigin(EAssetOrigin legacyOrigin)
    {
        switch (legacyOrigin)
        {
        case EAssetOrigin.OFFICIAL:
            if (legacyOfficialOrigin == null)
            {
                legacyOfficialOrigin = new AssetOrigin();
                legacyOfficialOrigin.name = "Official (Legacy)";
                assetOrigins.Add(legacyOfficialOrigin);
            }
            return legacyOfficialOrigin;
        case EAssetOrigin.MISC:
            if (legacyMiscOrigin == null)
            {
                legacyMiscOrigin = new AssetOrigin();
                legacyMiscOrigin.name = "Misc (Legacy)";
                assetOrigins.Add(legacyMiscOrigin);
            }
            return legacyMiscOrigin;
        default:
            if (legacyWorkshopOrigin == null)
            {
                legacyWorkshopOrigin = new AssetOrigin();
                legacyWorkshopOrigin.name = "Workshop File (Legacy)";
                assetOrigins.Add(legacyWorkshopOrigin);
            }
            return legacyWorkshopOrigin;
        }
    }

    [Obsolete]
    public static Asset find(EAssetType type, string name)
    {
        return null;
    }

    [Obsolete]
    public static void add(Asset asset, bool overrideExistingID)
    {
        AddToMapping(asset, overrideExistingID, defaultAssetMapping);
    }

    [Obsolete]
    public static void load(string path, bool usePath, bool loadFromResources, bool canUse, EAssetOrigin origin, bool overrideExistingIDs)
    {
        load(path, usePath, loadFromResources, canUse, origin, overrideExistingIDs, 0uL);
    }

    [Obsolete("Remove unused loadFromResources which was used by vanilla assets before masterbundles, and canUse which was for timed curated maps.")]
    public static void load(string path, bool usePath, bool loadFromResources, bool canUse, EAssetOrigin origin, bool overrideExistingIDs, ulong workshopFileId)
    {
        load(path, usePath, origin, overrideExistingIDs, workshopFileId);
    }

    [Obsolete("Replaced origin enum with class")]
    public static void load(string path, bool usePath, EAssetOrigin legacyOrigin, bool overrideExistingIDs, ulong workshopFileId)
    {
        if (usePath)
        {
            path = ReadWrite.PATH + path;
        }
        AssetOrigin origin = ConvertLegacyOrigin(legacyOrigin);
        load(path, origin, overrideExistingIDs);
    }

    [Obsolete("Please use the method which takes a List instead.")]
    public static Asset[] find(EAssetType type)
    {
        switch (type)
        {
        case EAssetType.NONE:
            return null;
        case EAssetType.OBJECT:
            throw new NotSupportedException();
        default:
        {
            Asset[] array = new Asset[currentAssetMapping.legacyAssetsTable[type].Values.Count];
            int num = 0;
            {
                foreach (KeyValuePair<ushort, Asset> item in currentAssetMapping.legacyAssetsTable[type])
                {
                    array[num] = item.Value;
                    num++;
                }
                return array;
            }
        }
        }
    }
}
