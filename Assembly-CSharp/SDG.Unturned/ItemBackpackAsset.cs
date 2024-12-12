using UnityEngine;

namespace SDG.Unturned;

public class ItemBackpackAsset : ItemBagAsset
{
    protected GameObject _backpack;

    public GameObject backpack => _backpack;

    internal override GameObject ClothingPrefab => backpack;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (!Dedicator.IsDedicatedServer)
        {
            _backpack = loadRequiredAsset<GameObject>(bundle, "Backpack");
            if ((bool)Assets.shouldValidateAssets)
            {
                AssetValidation.ValidateLayersEqual(this, _backpack, 10);
                AssetValidation.ValidateClothComponents(this, _backpack);
            }
        }
    }

    protected override AudioReference GetDefaultInventoryAudio()
    {
        if (base.width <= 3 || base.height <= 3)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/LightMetalEquipment.asset");
        }
        if (base.width <= 6 || base.height <= 6)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/MediumMetalEquipment.asset");
        }
        return new AudioReference("core.masterbundle", "Sounds/Inventory/HeavyMetalEquipment.asset");
    }
}
