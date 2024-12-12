using UnityEngine;

namespace SDG.Unturned;

public class ItemMaskAsset : ItemGearAsset
{
    protected GameObject _mask;

    private bool _isEarpiece;

    public GameObject mask => _mask;

    public bool isEarpiece => _isEarpiece;

    /// <summary>
    /// Multiplier for how quickly deadzones deplete a gasmask's filter quality.
    /// e.g., 2 is faster (2x) and 0.5 is slower.
    /// </summary>
    public float FilterDegradationRateMultiplier { get; protected set; } = 1f;


    internal override GameObject ClothingPrefab => mask;

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        if (!builder.shouldRestrictToLegacyContent)
        {
            if (FilterDegradationRateMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FilterDegradationRateMultiplier", PlayerDashboardInventoryUI.FormatStatModifier(FilterDegradationRateMultiplier, higherIsPositive: true, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(FilterDegradationRateMultiplier));
            }
            if (isEarpiece)
            {
                builder.Append(PlayerDashboardInventoryUI.FormatStatColor(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_Earpiece"), isBeneficial: true), 9999);
            }
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        FilterDegradationRateMultiplier = data.ParseFloat("FilterDegradationRateMultiplier", 1f);
        if (!Dedicator.IsDedicatedServer)
        {
            _mask = loadRequiredAsset<GameObject>(bundle, "Mask");
            if ((bool)Assets.shouldValidateAssets)
            {
                AssetValidation.ValidateLayersEqual(this, _mask, 10);
                AssetValidation.ValidateClothComponents(this, _mask);
            }
        }
        if (!isPro)
        {
            _isEarpiece = data.ContainsKey("Earpiece");
        }
    }
}
