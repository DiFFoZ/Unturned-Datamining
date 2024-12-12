using UnityEngine;

namespace SDG.Unturned;

public class ItemVestAsset : ItemBagAsset
{
    protected GameObject _vest;

    public GameObject vest => _vest;

    internal override GameObject ClothingPrefab => vest;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (!Dedicator.IsDedicatedServer)
        {
            _vest = loadRequiredAsset<GameObject>(bundle, "Vest");
            if ((bool)Assets.shouldValidateAssets)
            {
                AssetValidation.ValidateLayersEqual(this, _vest, 10);
                AssetValidation.ValidateClothComponents(this, _vest);
            }
        }
    }
}
