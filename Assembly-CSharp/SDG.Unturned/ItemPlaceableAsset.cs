using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Common base for barricades and structures.
/// 2023-01-16: not ideal to be adding this so late in development, but at least it is a step in the right direction.
/// </summary>
public class ItemPlaceableAsset : ItemAsset
{
    /// <summary>
    /// If true, this item is eligible for zombies to detect and attack when stuck.
    /// Defaults to true.
    /// </summary>
    public bool CanZombiesTarget { get; protected set; }

    /// <summary>
    /// Item recovered when picked up below 100% health.
    /// </summary>
    public AssetReference<ItemAsset> salvageItemRef { get; protected set; }

    /// <summary>
    /// Minimum number of items to drop when destroyed.
    /// </summary>
    public int minItemsDroppedOnDestroy { get; protected set; }

    /// <summary>
    /// Maximum number of items to drop when destroyed.
    /// </summary>
    public int maxItemsDroppedOnDestroy { get; protected set; }

    /// <summary>
    /// Spawn table for items dropped when destroyed.
    /// </summary>
    public AssetReference<SpawnAsset> itemDroppedOnDestroy { get; protected set; }

    public ItemAsset FindSalvageItemAsset()
    {
        if (salvageItemRef.isValid)
        {
            return salvageItemRef.Find();
        }
        return FindDefaultSalvageItemAsset();
    }

    /// <summary>
    /// By default a crafting ingredient is salvaged.
    /// </summary>
    internal ItemAsset FindDefaultSalvageItemAsset()
    {
        foreach (Blueprint blueprint in base.blueprints)
        {
            if (blueprint.outputs.Length == 1 && blueprint.outputs[0].id == id)
            {
                BlueprintSupply blueprintSupply = blueprint.supplies[Random.Range(0, blueprint.supplies.Length)];
                return Assets.find(EAssetType.ITEM, blueprintSupply.id) as ItemAsset;
            }
        }
        return null;
    }

    internal void SpawnItemDropsOnDestroy(Vector3 position)
    {
        int value = Random.Range(minItemsDroppedOnDestroy, maxItemsDroppedOnDestroy + 1);
        value = Mathf.Clamp(value, 0, 100);
        if (value < 1)
        {
            return;
        }
        SpawnAsset spawnAsset = itemDroppedOnDestroy.Find();
        if (spawnAsset == null)
        {
            return;
        }
        for (int i = 0; i < value; i++)
        {
            ushort num = SpawnTableTool.ResolveLegacyId(spawnAsset, EAssetType.ITEM, OnGetItemDroppedOnDestroySpawnTableErrorContext);
            if (num > 0)
            {
                ItemManager.dropItem(new Item(num, EItemOrigin.NATURE), position + new Vector3(Random.Range(-2f, 2f), 2f, Random.Range(-2f, 2f)), playEffect: false, Dedicator.IsDedicatedServer, wideSpread: true);
            }
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        CanZombiesTarget = data.ParseBool("Can_Zombies_Target", defaultValue: true);
        salvageItemRef = data.readAssetReference<ItemAsset>("SalvageItem");
        minItemsDroppedOnDestroy = data.ParseInt32("Min_Items_Dropped_On_Destroy");
        maxItemsDroppedOnDestroy = data.ParseInt32("Max_Items_Dropped_On_Destroy");
        itemDroppedOnDestroy = data.readAssetReference<SpawnAsset>("Item_Dropped_On_Destroy");
    }

    private string OnGetItemDroppedOnDestroySpawnTableErrorContext()
    {
        return FriendlyName + " items dropped on destroy";
    }
}
