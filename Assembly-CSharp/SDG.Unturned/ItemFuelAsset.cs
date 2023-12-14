using System;
using UnityEngine;

namespace SDG.Unturned;

public class ItemFuelAsset : ItemAsset
{
    protected AudioClip _use;

    protected ushort _fuel;

    private bool shouldAlwaysSpawnFull;

    private byte[] fuelState;

    public AudioClip use => _use;

    public ushort fuel => _fuel;

    public bool shouldDeleteAfterFillingTarget { get; protected set; }

    public override byte[] getState(EItemOrigin origin)
    {
        byte[] array = new byte[2];
        if (origin == EItemOrigin.ADMIN || shouldAlwaysSpawnFull)
        {
            array[0] = fuelState[0];
            array[1] = fuelState[1];
        }
        return array;
    }

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        ushort num = BitConverter.ToUInt16(itemInstance.state, 0);
        float num2 = (float)(int)num / (float)(int)fuel;
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FuelAmountWithCapacity", num, fuel, num2.ToString("P")), 2000);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _use = bundle.load<AudioClip>("Use");
        _fuel = data.ParseUInt16("Fuel", 0);
        fuelState = BitConverter.GetBytes(fuel);
        shouldDeleteAfterFillingTarget = data.ParseBool("Delete_After_Filling_Target");
        shouldAlwaysSpawnFull = data.ParseBool("Always_Spawn_Full");
    }
}
