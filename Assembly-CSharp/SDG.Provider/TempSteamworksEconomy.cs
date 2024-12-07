using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SDG.Framework.IO.Deserialization;
using SDG.SteamworksProvider;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;
using Unturned.UnityEx;

namespace SDG.Provider;

public class TempSteamworksEconomy
{
    public delegate void InventoryRefreshed();

    public delegate void InventoryDropped(int item, ushort quantity, ulong instance);

    public delegate void InventoryExchanged(List<SteamItemDetails_t> grantedItems);

    public delegate void InventoryExchangeFailed();

    /// <summary>
    /// For purchasable box and bundle itemdefs this maps their itemdefid to the list of itemdefids in their desc.
    /// </summary>
    private static Dictionary<int, List<int>> bundleContents;

    public InventoryRefreshed onInventoryRefreshed;

    public InventoryDropped onInventoryDropped;

    /// <summary>
    /// Invoked after a successful exchange with the newly granted items.
    /// </summary>
    public InventoryExchanged onInventoryExchanged;

    /// <summary>
    /// Invoke after a succesful purchase from the item store.
    /// </summary>
    public InventoryExchanged onInventoryPurchased;

    public InventoryExchangeFailed onInventoryExchangeFailed;

    private SteamInventoryResult_t promoResult = SteamInventoryResult_t.Invalid;

    public SteamInventoryResult_t exchangeResult = SteamInventoryResult_t.Invalid;

    public SteamInventoryResult_t dropResult = SteamInventoryResult_t.Invalid;

    public SteamInventoryResult_t wearingResult = SteamInventoryResult_t.Invalid;

    public SteamInventoryResult_t inventoryResult = SteamInventoryResult_t.Invalid;

    public SteamInventoryResult_t commitResult = SteamInventoryResult_t.Invalid;

    public List<SteamItemDetails_t> inventoryDetails = new List<SteamItemDetails_t>(0);

    public Dictionary<ulong, DynamicEconDetails> dynamicInventoryDetails = new Dictionary<ulong, DynamicEconDetails>();

    public bool isInventoryAvailable;

    /// <summary>
    /// Purchase result does not have a handle, so we guess based on when it arrives.
    /// </summary>
    public bool isExpectingPurchaseResult;

    private SteamworksAppInfo appInfo;

    private float lastHeartbeat;

    private Callback<SteamInventoryResultReady_t> inventoryResultReady;

    public bool canOpenInventory => true;

    internal static Dictionary<int, UnturnedEconInfo> econInfo { get; private set; }

    public static byte[] econInfoHash { get; private set; }

    public List<SteamItemDetails_t> inventory => inventoryDetails;

    /// <summary>
    /// Do we know the player's region?
    /// If not, default to not allowing random items.
    /// </summary>
    public bool hasCountryDetails { get; protected set; }

    /// <summary>
    /// Does the player's region allow crates and keys to be used?
    /// Similar to TF2 and other Valve games we disable unboxing in certain regions.
    /// </summary>
    public bool doesCountryAllowRandomItems { get; protected set; }

    public void open(ulong id)
    {
        SDG.Unturned.Provider.openURL("http://steamcommunity.com/profiles/" + SteamUser.GetSteamID().ToString() + "/inventory/?sellOnLoad=1#" + SteamUtils.GetAppID().ToString() + "_2_" + id);
    }

    /// <summary>
    /// Find the first instanceId of a given itemDefId.
    /// </summary>
    public ulong getInventoryPackage(int item)
    {
        if (inventoryDetails != null)
        {
            for (int i = 0; i < inventoryDetails.Count; i++)
            {
                if (inventoryDetails[i].m_iDefinition.m_SteamItemDef == item)
                {
                    return inventoryDetails[i].m_itemId.m_SteamItemInstanceID;
                }
            }
        }
        return 0uL;
    }

    /// <summary>
    /// Count quantity of a given itemDefId.
    /// </summary>
    public int countInventoryPackages(int item)
    {
        int num = 0;
        if (inventoryDetails != null)
        {
            for (int i = 0; i < inventoryDetails.Count; i++)
            {
                if (inventoryDetails[i].m_iDefinition.m_SteamItemDef == item)
                {
                    num += inventoryDetails[i].m_unQuantity;
                }
            }
        }
        return num;
    }

    /// <summary>
    /// Find certain quantity of given itemDefId.
    /// </summary>
    public bool getInventoryPackages(int item, ushort requiredQuantity, out List<EconExchangePair> pairs)
    {
        ushort num = 0;
        pairs = new List<EconExchangePair>();
        if (inventoryDetails != null)
        {
            for (int i = 0; i < inventoryDetails.Count; i++)
            {
                if (inventoryDetails[i].m_iDefinition.m_SteamItemDef != item)
                {
                    continue;
                }
                ushort num2 = inventoryDetails[i].m_unQuantity;
                if (num2 >= 1)
                {
                    ushort num3 = (ushort)(requiredQuantity - num);
                    if (num2 >= num3)
                    {
                        num2 = num3;
                    }
                    EconExchangePair item2 = new EconExchangePair(inventoryDetails[i].m_itemId.m_SteamItemInstanceID, num2);
                    pairs.Add(item2);
                    num += num2;
                    if (num == requiredQuantity)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public int getInventoryItem(ulong package)
    {
        if (inventoryDetails != null)
        {
            for (int i = 0; i < inventoryDetails.Count; i++)
            {
                if (inventoryDetails[i].m_itemId.m_SteamItemInstanceID == package)
                {
                    return inventoryDetails[i].m_iDefinition.m_SteamItemDef;
                }
            }
        }
        return 0;
    }

    public string getInventoryTags(ulong instance)
    {
        if (!dynamicInventoryDetails.TryGetValue(instance, out var value))
        {
            return string.Empty;
        }
        return value.tags;
    }

    public string getInventoryDynamicProps(ulong instance)
    {
        if (!dynamicInventoryDetails.TryGetValue(instance, out var value))
        {
            return string.Empty;
        }
        return value.dynamic_props;
    }

    public bool getInventoryStatTrackerValue(ulong instance, out EStatTrackerType type, out int kills)
    {
        if (!dynamicInventoryDetails.TryGetValue(instance, out var value))
        {
            type = EStatTrackerType.NONE;
            kills = -1;
            return false;
        }
        return value.getStatTrackerValue(out type, out kills);
    }

    public bool getInventoryRagdollEffect(ulong instance, out ERagdollEffect effect)
    {
        if (!dynamicInventoryDetails.TryGetValue(instance, out var value))
        {
            effect = ERagdollEffect.NONE;
            return false;
        }
        return value.getRagdollEffect(out effect);
    }

    public ushort getInventoryParticleEffect(ulong instance)
    {
        if (dynamicInventoryDetails.TryGetValue(instance, out var value))
        {
            return value.getParticleEffect();
        }
        return 0;
    }

    private UnturnedEconInfo FindEconInfo(int itemdefid)
    {
        if (econInfo.TryGetValue(itemdefid, out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Does itemdefid exist in the EconInfo.json file?
    /// </summary>
    public bool IsItemKnown(int item)
    {
        return FindEconInfo(item) != null;
    }

    public string getInventoryName(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return "";
        }
        return unturnedEconInfo.name;
    }

    internal DateTime GetCreationTime(int itemdefid)
    {
        return FindEconInfo(itemdefid)?.creationTimeUtc ?? DateTime.MinValue;
    }

    public string getInventoryType(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return "";
        }
        return unturnedEconInfo.display_type;
    }

    public bool IsItemBundle(int itemdefid)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(itemdefid);
        if (unturnedEconInfo != null)
        {
            return unturnedEconInfo.econ_type == 13;
        }
        return false;
    }

    public string getInventoryDescription(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return "";
        }
        return unturnedEconInfo.description;
    }

    public bool getInventoryMarketable(int item)
    {
        return FindEconInfo(item)?.marketable ?? false;
    }

    public int getInventoryScraps(int item)
    {
        return FindEconInfo(item)?.scraps ?? 0;
    }

    /// <summary>
    /// Get item with an exchange recipe for the appropriate number of scraps.
    /// </summary>
    public int getScrapExchangeItem(int item)
    {
        return getInventoryScraps(item) switch
        {
            1 => 19006, 
            2 => 19007, 
            3 => 19008, 
            4 => 19009, 
            5 => 19010, 
            10 => 19011, 
            _ => 0, 
        };
    }

    public Color getInventoryColor(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return Color.white;
        }
        if (unturnedEconInfo.name_color != null && unturnedEconInfo.name_color.Length > 0 && uint.TryParse(unturnedEconInfo.name_color, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result))
        {
            uint num = (result >> 16) & 0xFF;
            uint num2 = (result >> 8) & 0xFFu;
            uint num3 = result & 0xFFu;
            return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f);
        }
        return Color.white;
    }

    public UnturnedEconInfo.EQuality getInventoryQuality(int item)
    {
        return FindEconInfo(item)?.quality ?? UnturnedEconInfo.EQuality.None;
    }

    public UnturnedEconInfo.ERarity getInventoryRarity(int item)
    {
        return getInventoryQuality(item) switch
        {
            UnturnedEconInfo.EQuality.Common => UnturnedEconInfo.ERarity.Common, 
            UnturnedEconInfo.EQuality.Uncommon => UnturnedEconInfo.ERarity.Uncommon, 
            UnturnedEconInfo.EQuality.Gold => UnturnedEconInfo.ERarity.Gold, 
            UnturnedEconInfo.EQuality.Rare => UnturnedEconInfo.ERarity.Rare, 
            UnturnedEconInfo.EQuality.Epic => UnturnedEconInfo.ERarity.Epic, 
            UnturnedEconInfo.EQuality.Legendary => UnturnedEconInfo.ERarity.Legendary, 
            UnturnedEconInfo.EQuality.Mythical => UnturnedEconInfo.ERarity.Mythical, 
            UnturnedEconInfo.EQuality.Premium => UnturnedEconInfo.ERarity.Premium, 
            UnturnedEconInfo.EQuality.Achievement => UnturnedEconInfo.ERarity.Achievement, 
            _ => UnturnedEconInfo.ERarity.Unknown, 
        };
    }

    public EItemRarity getGameRarity(int item)
    {
        return getInventoryQuality(item) switch
        {
            UnturnedEconInfo.EQuality.Uncommon => EItemRarity.UNCOMMON, 
            UnturnedEconInfo.EQuality.Rare => EItemRarity.RARE, 
            UnturnedEconInfo.EQuality.Epic => EItemRarity.EPIC, 
            UnturnedEconInfo.EQuality.Legendary => EItemRarity.LEGENDARY, 
            UnturnedEconInfo.EQuality.Mythical => EItemRarity.MYTHICAL, 
            _ => EItemRarity.COMMON, 
        };
    }

    public Color getStatTrackerColor(EStatTrackerType type)
    {
        return type switch
        {
            EStatTrackerType.NONE => Color.white, 
            EStatTrackerType.TOTAL => new Color(1f, 0.5f, 0f), 
            EStatTrackerType.PLAYER => new Color(1f, 0f, 0f), 
            _ => Color.black, 
        };
    }

    public string getStatTrackerPropertyName(EStatTrackerType type)
    {
        return type switch
        {
            EStatTrackerType.TOTAL => "total_kills", 
            EStatTrackerType.PLAYER => "player_kills", 
            _ => null, 
        };
    }

    public ushort getInventoryMythicID(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return 0;
        }
        return (ushort)unturnedEconInfo.item_effect;
    }

    public void getInventoryTargetID(int item, out Guid item_guid, out Guid vehicle_guid)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            item_guid = default(Guid);
            vehicle_guid = default(Guid);
        }
        else
        {
            item_guid = unturnedEconInfo.target_game_asset_guid;
            vehicle_guid = unturnedEconInfo.target_game_asset_guid;
        }
    }

    public Guid getInventoryItemGuid(int item)
    {
        getInventoryTargetID(item, out var item_guid, out var _);
        return item_guid;
    }

    public ushort getInventorySkinID(int item)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(item);
        if (unturnedEconInfo == null)
        {
            return 0;
        }
        return (ushort)unturnedEconInfo.item_skin;
    }

    public Texture2D LoadItemIcon(int itemdefid)
    {
        UnturnedEconInfo unturnedEconInfo = FindEconInfo(itemdefid);
        if (unturnedEconInfo == null)
        {
            return null;
        }
        if (unturnedEconInfo.target_game_asset_guid != default(Guid))
        {
            ItemAsset itemAsset = Assets.find<ItemAsset>(unturnedEconInfo.target_game_asset_guid);
            if (itemAsset == null)
            {
                return null;
            }
            if (itemAsset.econIconUseId)
            {
                return Resources.Load<Texture2D>("Economy/Item/" + itemdefid + "/Icon_Large");
            }
            if (itemAsset.type == EItemType.SHIRT || itemAsset.type == EItemType.PANTS || itemAsset.type == EItemType.HAT || itemAsset.type == EItemType.BACKPACK || itemAsset.type == EItemType.VEST || itemAsset.type == EItemType.GLASSES || itemAsset.type == EItemType.MASK)
            {
                return Resources.Load<Texture2D>("Economy/CosmeticPreviews/" + itemAsset.GUID.ToString("N"));
            }
            if (itemAsset.proPath == null || itemAsset.proPath.Length == 0)
            {
                return null;
            }
            return Resources.Load<Texture2D>("Economy" + itemAsset.proPath + "/Icon_Large");
        }
        return Resources.Load<Texture2D>("Economy/Item/" + itemdefid + "/Icon_Large");
    }

    /// <summary>
    /// Get list of itemdefids mentioned in purchasable box or bundle item description.
    /// </summary>
    internal List<int> GetBundleContents(int itemdefid)
    {
        if (bundleContents.TryGetValue(itemdefid, out var value))
        {
            return value;
        }
        return null;
    }

    internal HashSet<int> GatherOwnedItemDefIds()
    {
        HashSet<int> hashSet = new HashSet<int>(inventoryDetails.Count);
        foreach (SteamItemDetails_t inventoryDetail in inventoryDetails)
        {
            hashSet.Add(inventoryDetail.m_iDefinition.m_SteamItemDef);
        }
        return hashSet;
    }

    public void consumeItem(ulong instance, uint quantity)
    {
        UnturnedLog.info("Consume item: {0} x{1}", instance, quantity);
        SteamInventory.ConsumeItem(out var _, (SteamItemInstanceID_t)instance, quantity);
    }

    public void exchangeInventory(int generate, List<EconExchangePair> destroy)
    {
        UnturnedLog.info("Exchange these item instances for " + generate);
        for (int i = 0; i < destroy.Count; i++)
        {
            ulong instanceId = destroy[i].instanceId;
            int num = -1;
            for (int j = 0; j < inventoryDetails.Count; j++)
            {
                if (inventoryDetails[j].m_itemId.m_SteamItemInstanceID == instanceId)
                {
                    num = j;
                    break;
                }
            }
            if (num == -1)
            {
                UnturnedLog.error("Unable to find item for exchange: {0}", instanceId);
                return;
            }
            SteamItemDetails_t steamItemDetails_t = inventoryDetails[num];
            UnturnedLog.info(steamItemDetails_t.m_iDefinition.m_SteamItemDef + " [" + instanceId + "] x" + destroy[i].quantity);
            if (destroy[i].quantity >= steamItemDetails_t.m_unQuantity)
            {
                UnturnedLog.info("Locally removed item - Instance: {0} Definition: {1}", instanceId, steamItemDetails_t.m_iDefinition.m_SteamItemDef);
                inventoryDetails.RemoveAtFast(num);
                dynamicInventoryDetails.Remove(instanceId);
            }
        }
        SteamItemDef_t[] array = new SteamItemDef_t[1];
        uint[] array2 = new uint[1];
        array[0] = (SteamItemDef_t)generate;
        array2[0] = 1u;
        SteamItemInstanceID_t[] array3 = new SteamItemInstanceID_t[destroy.Count];
        uint[] array4 = new uint[destroy.Count];
        for (int k = 0; k < destroy.Count; k++)
        {
            array3[k] = (SteamItemInstanceID_t)destroy[k].instanceId;
            array4[k] = destroy[k].quantity;
        }
        SteamInventory.ExchangeItems(out exchangeResult, array, array2, (uint)array.Length, array3, array4, (uint)array3.Length);
    }

    public void updateInventory()
    {
        if (!(Time.realtimeSinceStartup - lastHeartbeat < 30f))
        {
            lastHeartbeat = Time.realtimeSinceStartup;
            SteamInventory.SendItemDropHeartbeat();
        }
    }

    public void dropInventory()
    {
        SteamItemDef_t steamItemDef_t = new SteamItemDef_t(LiveConfig.Get().playtimeGeneratorItemDefId);
        if (steamItemDef_t.m_SteamItemDef > 0)
        {
            UnturnedLog.info($"Requesting playtime drop ({steamItemDef_t})");
            SteamInventory.TriggerItemDrop(out dropResult, steamItemDef_t);
        }
        GrantPromoItems();
    }

    public void GrantPromoItems()
    {
        if (promoResult == SteamInventoryResult_t.Invalid)
        {
            UnturnedLog.info("Requesting promo item grant");
            SteamInventory.GrantPromoItems(out promoResult);
        }
    }

    /// <summary>
    /// One player's inventory became so large that the Steam client's built-in GetInventory fails,
    /// so as temporary fix we can send them a json file with their inventory.
    /// </summary>
    private void loadInventoryFromResponseFile(string filePath)
    {
        UnturnedLog.info("Loading Steam inventory from GetInventory response file: {0}", filePath);
        List<SteamGetInventoryResponse.Item> list = SteamGetInventoryResponse.parse(filePath);
        dynamicInventoryDetails.Clear();
        inventoryDetails = new List<SteamItemDetails_t>(list.Count);
        foreach (SteamGetInventoryResponse.Item item2 in list)
        {
            SteamItemDetails_t item = default(SteamItemDetails_t);
            item.m_iDefinition = new SteamItemDef_t(item2.itemdefid);
            item.m_itemId = new SteamItemInstanceID_t(item2.itemid);
            item.m_unFlags = 0;
            item.m_unQuantity = item2.quantity;
            inventoryDetails.Add(item);
        }
        onInventoryRefreshed?.Invoke();
        isInventoryAvailable = true;
        SDG.Unturned.Provider.isLoadingInventory = false;
    }

    public void refreshInventory()
    {
        UnturnedLog.info("Refreshing Steam inventory");
        string text = Path.Combine(ReadWrite.PATH, "SteamInventory.json");
        if (File.Exists(text))
        {
            try
            {
                loadInventoryFromResponseFile(text);
                return;
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e);
                return;
            }
        }
        if (!SteamInventory.GetAllItems(out inventoryResult))
        {
            SDG.Unturned.Provider.isLoadingInventory = false;
        }
    }

    /// <summary>
    /// Add an item locally that we know exists in the online inventory, but is just a matter of waiting for it.
    /// </summary>
    private void addLocalItem(SteamItemDetails_t item, string tags, string dynamic_props)
    {
        inventoryDetails.Add(item);
        dynamicInventoryDetails.Remove(item.m_itemId.m_SteamItemInstanceID);
        DynamicEconDetails value = default(DynamicEconDetails);
        value.tags = (string.IsNullOrEmpty(tags) ? string.Empty : tags);
        value.dynamic_props = (string.IsNullOrEmpty(dynamic_props) ? string.Empty : dynamic_props);
        dynamicInventoryDetails.Add(item.m_itemId.m_SteamItemInstanceID, value);
    }

    /// <summary>
    /// Remove an item locally that we know no longer exists in the online inventory.
    /// </summary>
    private void removeLocalItem(SteamItemDetails_t item)
    {
        for (int i = 0; i < inventoryDetails.Count; i++)
        {
            if (inventoryDetails[i].m_itemId == item.m_itemId)
            {
                inventoryDetails.RemoveAtFast(i);
                break;
            }
        }
        dynamicInventoryDetails.Remove(item.m_itemId.m_SteamItemInstanceID);
    }

    /// <summary>
    /// Update our local version of an item that we know has changed, but we are waiting for a full refresh.
    /// </summary>
    private bool updateLocalItem(SteamItemDetails_t item, SteamInventoryResult_t resultHandle, uint resultIndex)
    {
        removeLocalItem(item);
        bool num = (item.m_unFlags & 0x100) != 0;
        bool flag = item.m_unQuantity < 1;
        if (num || flag)
        {
            UnturnedLog.info("Locally removed item - Instance: {0} Definition: {1}", item.m_itemId, item.m_iDefinition);
            return false;
        }
        uint punValueBufferSizeOut = 1024u;
        SteamInventory.GetResultItemProperty(resultHandle, resultIndex, "tags", out var pchValueBuffer, ref punValueBufferSizeOut);
        uint punValueBufferSizeOut2 = 1024u;
        SteamInventory.GetResultItemProperty(resultHandle, resultIndex, "dynamic_props", out var pchValueBuffer2, ref punValueBufferSizeOut2);
        addLocalItem(item, pchValueBuffer, pchValueBuffer2);
        UnturnedLog.info("Locally added or updated item - Instance: {0} Definition: {1} Tags: {2} Props: {3}", item.m_itemId, item.m_iDefinition, pchValueBuffer, pchValueBuffer2);
        return true;
    }

    private void handleServerResultReady(SteamInventoryResultReady_t callback)
    {
        SteamPending steamPending = null;
        for (int i = 0; i < SDG.Unturned.Provider.pending.Count; i++)
        {
            if (SDG.Unturned.Provider.pending[i].inventoryResult == callback.m_handle)
            {
                steamPending = SDG.Unturned.Provider.pending[i];
                break;
            }
        }
        if (steamPending == null)
        {
            return;
        }
        if (callback.m_result != EResult.k_EResultOK || !SteamGameServerInventory.CheckResultSteamID(callback.m_handle, steamPending.playerID.steamID))
        {
            UnturnedLog.info("inventory auth: " + callback.m_result.ToString() + " " + SteamGameServerInventory.CheckResultSteamID(callback.m_handle, steamPending.playerID.steamID));
            SDG.Unturned.Provider.reject(steamPending.playerID.steamID, ESteamRejection.AUTH_ECON_VERIFY);
            return;
        }
        uint punOutItemsArraySize = 0u;
        if (SteamGameServerInventory.GetResultItems(callback.m_handle, null, ref punOutItemsArraySize) && punOutItemsArraySize != 0)
        {
            steamPending.inventoryDetails = new SteamItemDetails_t[punOutItemsArraySize];
            SteamGameServerInventory.GetResultItems(callback.m_handle, steamPending.inventoryDetails, ref punOutItemsArraySize);
            for (uint num = 0u; num < punOutItemsArraySize; num++)
            {
                uint punValueBufferSizeOut = 1024u;
                SteamGameServerInventory.GetResultItemProperty(callback.m_handle, num, "tags", out var pchValueBuffer, ref punValueBufferSizeOut);
                uint punValueBufferSizeOut2 = 1024u;
                SteamGameServerInventory.GetResultItemProperty(callback.m_handle, num, "dynamic_props", out var pchValueBuffer2, ref punValueBufferSizeOut2);
                DynamicEconDetails value = default(DynamicEconDetails);
                value.tags = (string.IsNullOrEmpty(pchValueBuffer) ? string.Empty : pchValueBuffer);
                value.dynamic_props = (string.IsNullOrEmpty(pchValueBuffer2) ? string.Empty : pchValueBuffer2);
                steamPending.dynamicInventoryDetails.Add(steamPending.inventoryDetails[num].m_itemId.m_SteamItemInstanceID, value);
            }
        }
        steamPending.inventoryDetailsReady();
    }

    /// <summary>
    /// Callback when client knows which items were crafted or exchanged.
    /// </summary>
    private void handleClientExchangeResultReady(SteamInventoryResultReady_t callback)
    {
        SteamInventoryResult_t handle = callback.m_handle;
        List<SteamItemDetails_t> list = new List<SteamItemDetails_t>();
        uint punOutItemsArraySize = 0u;
        if (SteamInventory.GetResultItems(handle, null, ref punOutItemsArraySize) && punOutItemsArraySize != 0)
        {
            UnturnedLog.info("Exchange result items: {0}", punOutItemsArraySize);
            SteamItemDetails_t[] array = new SteamItemDetails_t[punOutItemsArraySize];
            if (SteamInventory.GetResultItems(handle, array, ref punOutItemsArraySize))
            {
                for (uint num = 0u; num < punOutItemsArraySize; num++)
                {
                    SteamItemDetails_t item = array[num];
                    if (updateLocalItem(item, handle, num))
                    {
                        list.Add(item);
                    }
                }
            }
        }
        if (list.Count > 0)
        {
            onInventoryExchanged?.Invoke(list);
            onInventoryRefreshed?.Invoke();
        }
        else
        {
            onInventoryExchangeFailed?.Invoke();
        }
    }

    /// <summary>
    /// Callback when client thinks result was from purchase.
    /// </summary>
    private void handleClientPurchaseResultReady(SteamInventoryResultReady_t callback)
    {
        SteamInventoryResult_t handle = callback.m_handle;
        List<SteamItemDetails_t> list = new List<SteamItemDetails_t>();
        uint punOutItemsArraySize = 0u;
        if (SteamInventory.GetResultItems(handle, null, ref punOutItemsArraySize) && punOutItemsArraySize != 0)
        {
            UnturnedLog.info("Purchase result items: {0}", punOutItemsArraySize);
            SteamItemDetails_t[] array = new SteamItemDetails_t[punOutItemsArraySize];
            if (SteamInventory.GetResultItems(handle, array, ref punOutItemsArraySize))
            {
                for (uint num = 0u; num < punOutItemsArraySize; num++)
                {
                    SteamItemDetails_t item = array[num];
                    if (updateLocalItem(item, handle, num))
                    {
                        list.Add(item);
                    }
                }
            }
        }
        if (list.Count > 0)
        {
            onInventoryPurchased(list);
        }
        onInventoryRefreshed?.Invoke();
    }

    /// <summary>
    /// 2022-01-01 it does not seem to be documented by Steam, but we get SteamInventoryResultReady callbacks
    /// for external events like AddItem calls, so we may as well handle them.
    /// </summary>
    private void UpdateLocalItemsFromUnknownResult(SteamInventoryResult_t resultHandle)
    {
        uint punOutItemsArraySize = 0u;
        if (SteamInventory.GetResultItems(resultHandle, null, ref punOutItemsArraySize) && punOutItemsArraySize != 0)
        {
            SteamItemDetails_t[] array = new SteamItemDetails_t[punOutItemsArraySize];
            if (SteamInventory.GetResultItems(resultHandle, array, ref punOutItemsArraySize))
            {
                for (uint num = 0u; num < punOutItemsArraySize; num++)
                {
                    updateLocalItem(array[num], resultHandle, num);
                }
            }
        }
        onInventoryRefreshed?.Invoke();
    }

    private void DumpInventoryResult(SteamInventoryResult_t handle)
    {
        uint punOutItemsArraySize = 0u;
        if (!SteamInventory.GetResultItems(handle, null, ref punOutItemsArraySize))
        {
            UnturnedLog.warn("Unable to get result items length from handle {0}", handle);
            return;
        }
        if (punOutItemsArraySize < 1)
        {
            UnturnedLog.info("Handle {0} result items empty", handle);
            return;
        }
        UnturnedLog.info("Handle {0} contains {1} result item(s)", handle, punOutItemsArraySize);
        SteamItemDetails_t[] array = new SteamItemDetails_t[punOutItemsArraySize];
        if (!SteamInventory.GetResultItems(handle, array, ref punOutItemsArraySize))
        {
            UnturnedLog.warn("Unable to get result items from handle {0}", handle);
            return;
        }
        for (uint num = 0u; num < punOutItemsArraySize; num++)
        {
            SteamItemDetails_t steamItemDetails_t = array[num];
            UnturnedLog.info("[{0}] Instance: {1} Def: {2} Quantity: {3} Flags: {4}", num, steamItemDetails_t.m_itemId, steamItemDetails_t.m_iDefinition, steamItemDetails_t.m_unQuantity, (ESteamItemFlags)steamItemDetails_t.m_unFlags);
        }
    }

    private void handleClientResultReady(SteamInventoryResultReady_t callback)
    {
        if (promoResult != SteamInventoryResult_t.Invalid && callback.m_handle == promoResult)
        {
            SteamInventory.DestroyResult(promoResult);
            promoResult = SteamInventoryResult_t.Invalid;
        }
        else if (exchangeResult != SteamInventoryResult_t.Invalid && callback.m_handle == exchangeResult)
        {
            handleClientExchangeResultReady(callback);
            SteamInventory.DestroyResult(exchangeResult);
            exchangeResult = SteamInventoryResult_t.Invalid;
        }
        else if (dropResult != SteamInventoryResult_t.Invalid && callback.m_handle == dropResult)
        {
            SteamItemDetails_t[] array = null;
            string pchValueBuffer = null;
            string pchValueBuffer2 = null;
            uint punOutItemsArraySize = 0u;
            if (SteamInventory.GetResultItems(dropResult, null, ref punOutItemsArraySize) && punOutItemsArraySize != 0)
            {
                array = new SteamItemDetails_t[punOutItemsArraySize];
                SteamInventory.GetResultItems(dropResult, array, ref punOutItemsArraySize);
                uint punValueBufferSizeOut = 1024u;
                SteamInventory.GetResultItemProperty(dropResult, 0u, "tags", out pchValueBuffer, ref punValueBufferSizeOut);
                uint punValueBufferSizeOut2 = 1024u;
                SteamInventory.GetResultItemProperty(dropResult, 0u, "dynamic_props", out pchValueBuffer2, ref punValueBufferSizeOut2);
            }
            UnturnedLog.info("onInventoryResultReady: Drop {0}", punOutItemsArraySize);
            if (array != null && punOutItemsArraySize != 0)
            {
                SteamItemDetails_t item = array[0];
                addLocalItem(item, pchValueBuffer, pchValueBuffer2);
                onInventoryDropped?.Invoke(item.m_iDefinition.m_SteamItemDef, item.m_unQuantity, item.m_itemId.m_SteamItemInstanceID);
                onInventoryRefreshed?.Invoke();
            }
            SteamInventory.DestroyResult(dropResult);
            dropResult = SteamInventoryResult_t.Invalid;
        }
        else
        {
            if (wearingResult != SteamInventoryResult_t.Invalid && callback.m_handle == wearingResult)
            {
                return;
            }
            if (inventoryResult != SteamInventoryResult_t.Invalid && callback.m_handle == inventoryResult)
            {
                dynamicInventoryDetails.Clear();
                uint punOutItemsArraySize2 = 0u;
                if (SteamInventory.GetResultItems(inventoryResult, null, ref punOutItemsArraySize2) && punOutItemsArraySize2 != 0)
                {
                    SteamItemDetails_t[] array2 = new SteamItemDetails_t[punOutItemsArraySize2];
                    SteamInventory.GetResultItems(inventoryResult, array2, ref punOutItemsArraySize2);
                    for (uint num = 0u; num < punOutItemsArraySize2; num++)
                    {
                        uint punValueBufferSizeOut3 = 1024u;
                        SteamInventory.GetResultItemProperty(inventoryResult, num, "tags", out var pchValueBuffer3, ref punValueBufferSizeOut3);
                        uint punValueBufferSizeOut4 = 1024u;
                        SteamInventory.GetResultItemProperty(inventoryResult, num, "dynamic_props", out var pchValueBuffer4, ref punValueBufferSizeOut4);
                        DynamicEconDetails value = default(DynamicEconDetails);
                        value.tags = (string.IsNullOrEmpty(pchValueBuffer3) ? string.Empty : pchValueBuffer3);
                        value.dynamic_props = (string.IsNullOrEmpty(pchValueBuffer4) ? string.Empty : pchValueBuffer4);
                        dynamicInventoryDetails.Add(array2[num].m_itemId.m_SteamItemInstanceID, value);
                        if (array2[num].m_unQuantity >= 1)
                        {
                            continue;
                        }
                        uint punValueBufferSizeOut5 = 64u;
                        if (SteamInventory.GetResultItemProperty(inventoryResult, num, "quantity", out var pchValueBuffer5, ref punValueBufferSizeOut5))
                        {
                            if (ulong.TryParse(pchValueBuffer5, out var result))
                            {
                                UnturnedLog.info(string.Format(arg2: (result <= 65535) ? ((ushort)result) : ushort.MaxValue, format: "Used quantity string fallback for itemdefid {0} (actual: {1} clamped: {2})", arg0: array2[num].m_iDefinition, arg1: result));
                            }
                            else
                            {
                                UnturnedLog.warn($"Tried using quantity string fallback for itemdefid {array2[num].m_iDefinition} but unable to parse \"{pchValueBuffer5}\"");
                            }
                        }
                        else
                        {
                            UnturnedLog.warn($"Tried using quantity string fallback for itemdefid {array2[num].m_iDefinition} but GetResultItemProperty returned false");
                        }
                    }
                    inventoryDetails = new List<SteamItemDetails_t>(array2);
                }
                onInventoryRefreshed?.Invoke();
                isInventoryAvailable = true;
                SDG.Unturned.Provider.isLoadingInventory = false;
                SteamInventory.DestroyResult(inventoryResult);
                inventoryResult = SteamInventoryResult_t.Invalid;
            }
            else if (commitResult != SteamInventoryResult_t.Invalid && callback.m_handle == commitResult)
            {
                UnturnedLog.info("Commit dynamic properties result: " + callback.m_result);
                SteamInventory.DestroyResult(commitResult);
                commitResult = SteamInventoryResult_t.Invalid;
            }
            else if (isExpectingPurchaseResult)
            {
                isExpectingPurchaseResult = false;
                handleClientPurchaseResultReady(callback);
                SteamInventory.DestroyResult(callback.m_handle);
            }
            else
            {
                UnturnedLog.info("Unexpected client inventory result ready callback  - Handle: {0} Result: {1}", callback.m_handle, callback.m_result);
                UpdateLocalItemsFromUnknownResult(callback.m_handle);
                DumpInventoryResult(callback.m_handle);
                SteamInventory.DestroyResult(callback.m_handle);
            }
        }
    }

    private void onInventoryResultReady(SteamInventoryResultReady_t callback)
    {
        if (appInfo.isDedicated)
        {
            handleServerResultReady(callback);
        }
        else
        {
            handleClientResultReady(callback);
        }
    }

    public void loadTranslationEconInfo()
    {
        if (SDG.Unturned.Provider.language == "English")
        {
            return;
        }
        string text = SDG.Unturned.Provider.localizationRoot + "/EconInfo.json";
        if (!ReadWrite.fileExists(text, useCloud: false, usePath: false))
        {
            UnturnedLog.warn("Looked for economy translation at: {0}", text);
            return;
        }
        JSONDeserializer jSONDeserializer = new JSONDeserializer();
        new List<UnturnedEconInfo>();
        foreach (UnturnedEconInfo item in ((IDeserializer)jSONDeserializer).deserialize<List<UnturnedEconInfo>>(text))
        {
            UnturnedEconInfo unturnedEconInfo = FindEconInfo(item.itemdefid);
            if (unturnedEconInfo != null)
            {
                unturnedEconInfo.name = item.name;
                unturnedEconInfo.display_type = item.display_type;
                unturnedEconInfo.description = item.description;
            }
        }
    }

    /// <summary>
    /// If player's region does not allow crates and keys to be used, return the country code.
    /// </summary>
    public string getCountryWarningId()
    {
        return SteamUtils.GetIPCountry();
    }

    /// <summary>
    /// Similar to TF2 and other Valve games we disable unboxing in certain regions, so hide those items.
    /// </summary>
    public bool isItemHiddenByCountryRestrictions(int itemdefid)
    {
        if (doesCountryAllowRandomItems)
        {
            return false;
        }
        ItemAsset itemAsset = Assets.find<ItemAsset>(getInventoryItemGuid(itemdefid));
        if (itemAsset != null)
        {
            if (itemAsset.type != EItemType.KEY)
            {
                return itemAsset.type == EItemType.BOX;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Similar to TF2 and other Valve games we disable unboxing in certain regions.
    /// </summary>
    private void initCountryRestrictions()
    {
        string iPCountry = SteamUtils.GetIPCountry();
        if (string.IsNullOrWhiteSpace(iPCountry))
        {
            hasCountryDetails = false;
            doesCountryAllowRandomItems = false;
            UnturnedLog.warn("Unable to determine country/region");
        }
        else
        {
            hasCountryDetails = true;
            doesCountryAllowRandomItems = true;
            doesCountryAllowRandomItems &= !string.Equals(iPCountry, "BE");
            doesCountryAllowRandomItems &= !string.Equals(iPCountry, "NL");
        }
    }

    /// <summary>
    /// Not called on dedicated server.
    /// </summary>
    public void initializeClient()
    {
        initCountryRestrictions();
    }

    public TempSteamworksEconomy(SteamworksAppInfo newAppInfo)
    {
        appInfo = newAppInfo;
        string path = ((UnityPaths.ProjectDirectory == null) ? PathEx.Join(UnturnedPaths.RootDirectory, "EconInfo.bin") : PathEx.Join(UnityPaths.ProjectDirectory, "Builds", "Shared_Release", "EconInfo.bin"));
        econInfo = new Dictionary<int, UnturnedEconInfo>();
        bundleContents = new Dictionary<int, List<int>>();
        try
        {
            using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using SHA1Stream sHA1Stream = new SHA1Stream(fileStream);
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                int num = binaryReader.ReadInt32();
                if (num <= 2)
                {
                    int num2 = binaryReader.ReadInt32();
                    for (int i = 0; i < num2; i++)
                    {
                        UnturnedEconInfo unturnedEconInfo = new UnturnedEconInfo
                        {
                            name = binaryReader.ReadString(),
                            display_type = binaryReader.ReadString(),
                            description = binaryReader.ReadString(),
                            name_color = binaryReader.ReadString(),
                            itemdefid = binaryReader.ReadInt32(),
                            marketable = binaryReader.ReadBoolean(),
                            scraps = binaryReader.ReadInt32(),
                            target_game_asset_guid = new Guid(binaryReader.ReadBytes(16)),
                            item_skin = binaryReader.ReadInt32(),
                            item_effect = binaryReader.ReadInt32(),
                            quality = (UnturnedEconInfo.EQuality)binaryReader.ReadInt32(),
                            econ_type = binaryReader.ReadInt32()
                        };
                        if (num >= 2)
                        {
                            unturnedEconInfo.creationTimeUtc = DateTime.FromBinary(binaryReader.ReadInt64());
                        }
                        if (!econInfo.TryAdd(unturnedEconInfo.itemdefid, unturnedEconInfo))
                        {
                            UnturnedLog.error($"Duplicate itemdefid {unturnedEconInfo.itemdefid} name: \"{unturnedEconInfo.name}\"");
                        }
                    }
                    int num3 = binaryReader.ReadInt32();
                    for (int j = 0; j < num3; j++)
                    {
                        int num4 = binaryReader.ReadInt32();
                        int num5 = binaryReader.ReadInt32();
                        List<int> list = new List<int>(num5);
                        for (int k = 0; k < num5; k++)
                        {
                            int item = binaryReader.ReadInt32();
                            list.Add(item);
                        }
                        if (!bundleContents.TryAdd(num4, list))
                        {
                            UnturnedLog.error($"Duplicate bundle contents itemdefid {num4}");
                        }
                    }
                }
                else
                {
                    UnturnedLog.warn($"Unable to load future EconInfo.bin version ({num})");
                }
            }
            econInfoHash = sHA1Stream.Hash;
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Caught exception loading EconInfo.bin:");
        }
        if (appInfo.isDedicated)
        {
            inventoryResultReady = Callback<SteamInventoryResultReady_t>.CreateGameServer(onInventoryResultReady);
        }
        else
        {
            inventoryResultReady = Callback<SteamInventoryResultReady_t>.Create(onInventoryResultReady);
        }
    }
}
