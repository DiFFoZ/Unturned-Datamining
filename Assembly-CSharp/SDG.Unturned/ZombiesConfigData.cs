namespace SDG.Unturned;

public class ZombiesConfigData
{
    public float Spawn_Chance;

    public float Loot_Chance;

    public float Crawler_Chance;

    public float Sprinter_Chance;

    public float Flanker_Chance;

    public float Burner_Chance;

    public float Acid_Chance;

    public float Boss_Electric_Chance;

    public float Boss_Wind_Chance;

    public float Boss_Fire_Chance;

    public float Spirit_Chance;

    public float DL_Red_Volatile_Chance;

    public float DL_Blue_Volatile_Chance;

    public float Boss_Elver_Stomper_Chance;

    public float Boss_Kuwait_Chance;

    public float Respawn_Day_Time;

    public float Respawn_Night_Time;

    public float Respawn_Beacon_Time;

    /// <summary>
    /// Minimum seconds between boss zombie spawns for players doing quests.
    /// Players were abusing the spawns to farm boss tier loot.
    /// </summary>
    public float Quest_Boss_Respawn_Interval;

    public float Damage_Multiplier;

    public float Armor_Multiplier;

    public float Backstab_Multiplier;

    /// <summary>
    /// Weapon damage multiplier against body, arms, legs. Useful for headshot-only mode.
    /// </summary>
    public float NonHeadshot_Armor_Multiplier;

    public float Beacon_Experience_Multiplier;

    public float Full_Moon_Experience_Multiplier;

    public uint Min_Drops;

    public uint Max_Drops;

    public uint Min_Mega_Drops;

    public uint Max_Mega_Drops;

    public uint Min_Boss_Drops;

    public uint Max_Boss_Drops;

    public bool Slow_Movement;

    public bool Can_Stun;

    public bool Only_Critical_Stuns;

    public bool Weapons_Use_Player_Damage;

    public bool Can_Target_Barricades;

    public bool Can_Target_Structures;

    public bool Can_Target_Vehicles;

    public bool Can_Target_Objects;

    public uint Beacon_Max_Rewards;

    public uint Beacon_Max_Participants;

    public float Beacon_Rewards_Multiplier;

    public ZombiesConfigData(EGameMode mode)
    {
        Respawn_Day_Time = 360f;
        Respawn_Night_Time = 30f;
        Respawn_Beacon_Time = 0f;
        Quest_Boss_Respawn_Interval = 600f;
        switch (mode)
        {
        case EGameMode.EASY:
            Spawn_Chance = 0.2f;
            Loot_Chance = 0.55f;
            Crawler_Chance = 0f;
            Sprinter_Chance = 0f;
            Flanker_Chance = 0f;
            Burner_Chance = 0f;
            Acid_Chance = 0f;
            break;
        case EGameMode.NORMAL:
            Spawn_Chance = 0.25f;
            Loot_Chance = 0.5f;
            Crawler_Chance = 0.15f;
            Sprinter_Chance = 0.15f;
            Flanker_Chance = 0.025f;
            Burner_Chance = 0.025f;
            Acid_Chance = 0.025f;
            break;
        case EGameMode.HARD:
            Spawn_Chance = 0.3f;
            Loot_Chance = 0.3f;
            Crawler_Chance = 0.125f;
            Sprinter_Chance = 0.175f;
            Flanker_Chance = 0.05f;
            Burner_Chance = 0.05f;
            Acid_Chance = 0.05f;
            break;
        default:
            Spawn_Chance = 1f;
            Loot_Chance = 0f;
            Crawler_Chance = 0f;
            Sprinter_Chance = 0f;
            Flanker_Chance = 0f;
            Burner_Chance = 0f;
            Acid_Chance = 0f;
            break;
        }
        Boss_Electric_Chance = 0f;
        Boss_Wind_Chance = 0f;
        Boss_Fire_Chance = 0f;
        Spirit_Chance = 0f;
        DL_Red_Volatile_Chance = 0f;
        DL_Blue_Volatile_Chance = 0f;
        Boss_Elver_Stomper_Chance = 0f;
        Boss_Kuwait_Chance = 0f;
        switch (mode)
        {
        case EGameMode.EASY:
            Damage_Multiplier = 0.75f;
            Armor_Multiplier = 1.25f;
            break;
        case EGameMode.HARD:
            Damage_Multiplier = 1.5f;
            Armor_Multiplier = 0.75f;
            break;
        default:
            Damage_Multiplier = 1f;
            Armor_Multiplier = 1f;
            break;
        }
        Backstab_Multiplier = 1.25f;
        NonHeadshot_Armor_Multiplier = 1f;
        Beacon_Experience_Multiplier = 1f;
        Full_Moon_Experience_Multiplier = 2f;
        Min_Drops = 1u;
        Max_Drops = 1u;
        Min_Mega_Drops = 5u;
        Max_Mega_Drops = 5u;
        Min_Boss_Drops = 8u;
        Max_Boss_Drops = 10u;
        Slow_Movement = mode == EGameMode.EASY;
        Can_Stun = mode != EGameMode.HARD;
        Only_Critical_Stuns = mode == EGameMode.HARD;
        Weapons_Use_Player_Damage = mode == EGameMode.HARD;
        Can_Target_Barricades = true;
        Can_Target_Structures = true;
        Can_Target_Vehicles = true;
        Can_Target_Objects = true;
        Beacon_Max_Rewards = 0u;
        Beacon_Max_Participants = 0u;
        Beacon_Rewards_Multiplier = 1f;
    }
}
