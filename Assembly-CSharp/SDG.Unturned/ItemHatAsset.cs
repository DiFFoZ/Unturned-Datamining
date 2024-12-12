using UnityEngine;

namespace SDG.Unturned;

public class ItemHatAsset : ItemGearAsset
{
    protected GameObject _hat;

    public GameObject hat => _hat;

    internal override GameObject ClothingPrefab => hat;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (!Dedicator.IsDedicatedServer)
        {
            _hat = loadRequiredAsset<GameObject>(bundle, "Hat");
            if ((bool)Assets.shouldValidateAssets)
            {
                AssetValidation.ValidateLayersEqual(this, _hat, 10);
                AssetValidation.ValidateClothComponents(this, _hat);
            }
        }
    }
}
