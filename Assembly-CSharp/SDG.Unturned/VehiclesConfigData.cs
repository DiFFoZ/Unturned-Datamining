namespace SDG.Unturned;

public class VehiclesConfigData
{
    /// <summary>
    /// Seconds vehicle can be neglected before it begins taking damage.
    /// </summary>
    public float Decay_Time;

    /// <summary>
    /// After vehicle has been neglected for more than Decay_Time seconds it will begin taking this much damage per second.
    /// </summary>
    public float Decay_Damage_Per_Second;

    public float Has_Battery_Chance;

    public float Min_Battery_Charge;

    public float Max_Battery_Charge;

    public float Has_Tire_Chance;

    public float Respawn_Time;

    public float Unlocked_After_Seconds_In_Safezone;

    public float Armor_Multiplier;

    public float Child_Explosion_Armor_Multiplier;

    public float Gun_Lowcal_Damage_Multiplier;

    public float Gun_Highcal_Damage_Multiplier;

    public float Melee_Damage_Multiplier;

    public float Melee_Repair_Multiplier;

    public uint Max_Instances_Tiny;

    public uint Max_Instances_Small;

    public uint Max_Instances_Medium;

    public uint Max_Instances_Large;

    public uint Max_Instances_Insane;

    /// <summary>
    /// Vehicles are considered "natural" if they were spawned by the level as opposed to players or vendors.
    /// If less than this many natural vehicles exist in the level, more will be spawned. The minimum of this or
    /// Max_Instances is used. (i.e., if this value is higher than max instances the max instances value is used
    /// instead.)
    /// </summary>
    public uint Min_Natural_Vehicles;

    public VehiclesConfigData(EGameMode mode)
    {
        Decay_Time = 604800f;
        Decay_Damage_Per_Second = 0.1f;
        Has_Battery_Chance = 0.8f;
        Min_Battery_Charge = 0.5f;
        Max_Battery_Charge = 0.75f;
        switch (mode)
        {
        case EGameMode.EASY:
            Has_Battery_Chance = 1f;
            Min_Battery_Charge = 0.8f;
            Max_Battery_Charge = 1f;
            Has_Tire_Chance = 1f;
            break;
        case EGameMode.NORMAL:
            Has_Battery_Chance = 0.8f;
            Min_Battery_Charge = 0.5f;
            Max_Battery_Charge = 0.75f;
            Has_Tire_Chance = 0.85f;
            break;
        case EGameMode.HARD:
            Has_Battery_Chance = 0.25f;
            Min_Battery_Charge = 0.1f;
            Max_Battery_Charge = 0.3f;
            Has_Tire_Chance = 0.7f;
            break;
        default:
            Has_Battery_Chance = 1f;
            Min_Battery_Charge = 1f;
            Max_Battery_Charge = 1f;
            Has_Tire_Chance = 1f;
            break;
        }
        Respawn_Time = 300f;
        Unlocked_After_Seconds_In_Safezone = 3600f;
        Armor_Multiplier = 1f;
        Child_Explosion_Armor_Multiplier = 1f;
        Gun_Lowcal_Damage_Multiplier = 1f;
        Gun_Highcal_Damage_Multiplier = 1f;
        Melee_Damage_Multiplier = 1f;
        Melee_Repair_Multiplier = 1f;
        Max_Instances_Tiny = 4u;
        Max_Instances_Small = 8u;
        Max_Instances_Medium = 16u;
        Max_Instances_Large = 32u;
        Max_Instances_Insane = 64u;
        Min_Natural_Vehicles = 16u;
    }
}
