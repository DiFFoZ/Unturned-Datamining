using UnityEngine;

namespace SDG.Unturned;

public class ItemBackpackAsset : ItemBagAsset
{
    protected GameObject _backpack;

    public GameObject backpack => _backpack;

    public ItemBackpackAsset(Bundle bundle, Data data, Local localization, ushort id)
        : base(bundle, data, localization, id)
    {
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
