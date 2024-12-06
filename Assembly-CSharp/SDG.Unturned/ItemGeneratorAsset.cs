using UnityEngine;

namespace SDG.Unturned;

public class ItemGeneratorAsset : ItemBarricadeAsset
{
    protected ushort _capacity;

    protected float _wirerange;

    protected float _burn;

    public ushort capacity => _capacity;

    public float wirerange => _wirerange;

    /// <summary>
    /// Seconds to wait between burning one unit of fuel.
    /// </summary>
    public float burn => _burn;

    public override byte[] getState(EItemOrigin origin)
    {
        return new byte[3];
    }

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FuelCapacity", capacity), 2000);
        if (burn > 0f)
        {
            int num = Mathf.RoundToInt(3600f / burn);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FuelBurnRate", num), 2000);
            float num2 = burn * (float)(int)capacity / 3600f;
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FuelMaxRuntime", num2.ToString("0.00")), 2000);
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _capacity = data.ParseUInt16("Capacity", 0);
        _wirerange = data.ParseFloat("Wirerange");
        if (wirerange > PowerTool.MAX_POWER_RANGE + 0.1f)
        {
            float mAX_POWER_RANGE = PowerTool.MAX_POWER_RANGE;
            Assets.ReportError(this, "Wirerange is further than the max supported power range of " + mAX_POWER_RANGE);
        }
        _burn = data.ParseFloat("Burn");
    }
}
