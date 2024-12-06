using UnityEngine;

namespace SDG.Unturned;

public class ItemWeaponAsset : ItemAsset
{
    public float range;

    public PlayerDamageMultiplier playerDamageMultiplier;

    public ZombieDamageMultiplier zombieDamageMultiplier;

    public AnimalDamageMultiplier animalDamageMultiplier;

    public float barricadeDamage;

    public float structureDamage;

    public float vehicleDamage;

    public float resourceDamage;

    public float objectDamage;

    public float durability;

    public byte wear;

    public bool isInvulnerable;

    public byte[] bladeIDs { get; protected set; }

    public DamagePlayerParameters.Bleeding playerDamageBleeding { get; protected set; }

    public DamagePlayerParameters.Bones playerDamageBones { get; protected set; }

    /// <summary>
    /// Added to player's food value.
    /// </summary>
    public float playerDamageFood { get; protected set; }

    /// <summary>
    /// Added to player's water value.
    /// </summary>
    public float playerDamageWater { get; protected set; }

    /// <summary>
    /// Added to player's virus value.
    /// </summary>
    public float playerDamageVirus { get; protected set; }

    /// <summary>
    /// Added to player's hallucination value.
    /// </summary>
    public float playerDamageHallucination { get; protected set; }

    public EZombieStunOverride zombieStunOverride { get; protected set; }

    /// <summary>
    /// Get animal or player damage based on game mode config.
    /// </summary>
    public IDamageMultiplier animalOrPlayerDamageMultiplier
    {
        get
        {
            if (!Provider.modeConfigData.Animals.Weapons_Use_Player_Damage)
            {
                return animalDamageMultiplier;
            }
            return playerDamageMultiplier;
        }
    }

    /// <summary>
    /// Get zombie or player damage based on game mode config.
    /// </summary>
    public IDamageMultiplier zombieOrPlayerDamageMultiplier
    {
        get
        {
            if (!Provider.modeConfigData.Zombies.Weapons_Use_Player_Damage)
            {
                return zombieDamageMultiplier;
            }
            return playerDamageMultiplier;
        }
    }

    /// <summary>
    /// Should player/animal/zombie surface be nulled on hit?
    /// Requested by spyjack for a chainsaw-style shield that was overboard with the blood.
    /// </summary>
    public bool allowFleshFx { get; protected set; }

    /// <summary>
    /// Should this weapon bypass the DamageTool.allowedToDamagePlayer test?
    /// Used by weapons that heal players in PvE.
    /// </summary>
    public bool bypassAllowedToDamagePlayer { get; protected set; }

    public bool hasBladeID(byte bladeID)
    {
        if (bladeIDs != null)
        {
            for (int i = 0; i < bladeIDs.Length; i++)
            {
                if (bladeIDs[i] == bladeID)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void initPlayerDamageParameters(ref DamagePlayerParameters parameters)
    {
        parameters.bleedingModifier = playerDamageBleeding;
        parameters.bonesModifier = playerDamageBones;
        parameters.foodModifier = playerDamageFood;
        parameters.waterModifier = playerDamageWater;
        parameters.virusModifier = playerDamageVirus;
        parameters.hallucinationModifier = playerDamageHallucination;
    }

    /// <summary>
    /// Please refer to ItemWeaponAsset.BuildDescription for an explanation of why this is necessary.
    /// </summary>
    protected void BuildExplosiveDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        int sortOrder = 30000;
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionBlastRadius", MeasurementTool.FormatLengthString(range)), sortOrder++);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionPlayerDamage", Mathf.RoundToInt(playerDamageMultiplier.damage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionZombieDamage", Mathf.RoundToInt(zombieDamageMultiplier.damage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionAnimalDamage", Mathf.RoundToInt(animalDamageMultiplier.damage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionBarricadeDamage", Mathf.RoundToInt(barricadeDamage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionStructureDamage", Mathf.RoundToInt(structureDamage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionVehicleDamage", Mathf.RoundToInt(vehicleDamage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionResourceDamage", Mathf.RoundToInt(resourceDamage)), sortOrder);
        builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ExplosionObjectDamage", Mathf.RoundToInt(objectDamage)), sortOrder);
    }

    /// <summary>
    /// Please refer to ItemWeaponAsset.BuildDescription for an explanation of why this is necessary.
    /// </summary>
    protected void BuildNonExplosiveDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        if (range > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponRange", MeasurementTool.FormatLengthString(range)), 10000);
        }
        int sortOrder = 30000;
        if (playerDamageMultiplier.damage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_Head", Mathf.FloorToInt(playerDamageMultiplier.damage * playerDamageMultiplier.skull)), sortOrder++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_Body", Mathf.FloorToInt(playerDamageMultiplier.damage * playerDamageMultiplier.spine)), sortOrder++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_Arm", Mathf.FloorToInt(playerDamageMultiplier.damage * playerDamageMultiplier.arm)), sortOrder++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_Leg", Mathf.FloorToInt(playerDamageMultiplier.damage * playerDamageMultiplier.leg)), sortOrder++);
        }
        int num = Mathf.RoundToInt(playerDamageFood);
        if (num > 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_FoodPositive", num.ToString()), sortOrder);
        }
        else if (num < 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_FoodNegative", (-num).ToString()), sortOrder);
        }
        int num2 = Mathf.RoundToInt(playerDamageWater);
        if (num2 > 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_WaterPositive", num2.ToString()), sortOrder);
        }
        else if (num2 < 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_WaterNegative", (-num2).ToString()), sortOrder);
        }
        int num3 = Mathf.RoundToInt(playerDamageVirus);
        if (num3 > 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_VirusPositive", num3.ToString()), sortOrder);
        }
        else if (num3 < 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_VirusNegative", (-num3).ToString()), sortOrder);
        }
        int num4 = Mathf.RoundToInt(playerDamageHallucination);
        if (num4 > 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_HallucinationPositive", $"{num4} s"), sortOrder);
        }
        else if (num4 < 0)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Player_HallucinationNegative", $"{-num4} s"), sortOrder);
        }
        switch (playerDamageBleeding)
        {
        case DamagePlayerParameters.Bleeding.Always:
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponBleeding_Always"), sortOrder);
            break;
        case DamagePlayerParameters.Bleeding.Heal:
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponBleeding_Heal"), sortOrder);
            break;
        }
        switch (playerDamageBones)
        {
        case DamagePlayerParameters.Bones.Always:
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponBones_Always"), sortOrder);
            break;
        case DamagePlayerParameters.Bones.Heal:
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponBones_Heal"), sortOrder);
            break;
        }
        if (isInvulnerable)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Invulnerable"), 10000);
        }
        if (zombieDamageMultiplier.damage > 0f)
        {
            int sortOrder2 = 31000;
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Zombie_Head", Mathf.FloorToInt(zombieDamageMultiplier.damage * zombieDamageMultiplier.skull)), sortOrder2++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Zombie_Body", Mathf.FloorToInt(zombieDamageMultiplier.damage * zombieDamageMultiplier.spine)), sortOrder2++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Zombie_Arm", Mathf.FloorToInt(zombieDamageMultiplier.damage * zombieDamageMultiplier.arm)), sortOrder2++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Zombie_Leg", Mathf.FloorToInt(zombieDamageMultiplier.damage * zombieDamageMultiplier.leg)), sortOrder2);
        }
        if (animalDamageMultiplier.damage > 0f)
        {
            int sortOrder3 = 32000;
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Animal_Head", Mathf.FloorToInt(animalDamageMultiplier.damage * animalDamageMultiplier.skull)), sortOrder3++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Animal_Body", Mathf.FloorToInt(animalDamageMultiplier.damage * animalDamageMultiplier.spine)), sortOrder3++);
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Animal_Limb", Mathf.FloorToInt(animalDamageMultiplier.damage * animalDamageMultiplier.leg)), sortOrder3);
        }
        if (barricadeDamage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Barricade", Mathf.FloorToInt(barricadeDamage)), 33000);
        }
        if (structureDamage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Structure", Mathf.FloorToInt(structureDamage)), 33000);
        }
        if (vehicleDamage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Vehicle", Mathf.FloorToInt(vehicleDamage)), 33000);
        }
        if (resourceDamage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Resource", Mathf.FloorToInt(resourceDamage)), 33000);
        }
        if (objectDamage > 0f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_WeaponDamage_Object", Mathf.FloorToInt(objectDamage)), 33000);
        }
    }

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        _ = builder.shouldRestrictToLegacyContent;
    }

    public ItemWeaponAsset()
    {
        playerDamageMultiplier = new PlayerDamageMultiplier(0f, 0f, 0f, 0f, 0f);
        zombieDamageMultiplier = new ZombieDamageMultiplier(0f, 0f, 0f, 0f, 0f);
        animalDamageMultiplier = new AnimalDamageMultiplier(0f, 0f, 0f, 0f);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        int num = data.ParseInt32("BladeIDs");
        if (num > 0)
        {
            bladeIDs = new byte[num];
            for (int i = 0; i < num; i++)
            {
                bladeIDs[i] = data.ParseUInt8("BladeID_" + i, 0);
            }
        }
        else
        {
            bladeIDs = new byte[1];
            bladeIDs[0] = data.ParseUInt8("BladeID", 0);
        }
        range = data.ParseFloat("Range");
        playerDamageMultiplier = new PlayerDamageMultiplier(data.ParseFloat("Player_Damage"), data.ParseFloat("Player_Leg_Multiplier"), data.ParseFloat("Player_Arm_Multiplier"), data.ParseFloat("Player_Spine_Multiplier"), data.ParseFloat("Player_Skull_Multiplier"));
        playerDamageBleeding = data.ParseEnum("Player_Damage_Bleeding", DamagePlayerParameters.Bleeding.Default);
        playerDamageBones = data.ParseEnum("Player_Damage_Bones", DamagePlayerParameters.Bones.None);
        playerDamageFood = data.ParseFloat("Player_Damage_Food");
        playerDamageWater = data.ParseFloat("Player_Damage_Water");
        playerDamageVirus = data.ParseFloat("Player_Damage_Virus");
        playerDamageHallucination = data.ParseFloat("Player_Damage_Hallucination");
        zombieDamageMultiplier = new ZombieDamageMultiplier(data.ParseFloat("Zombie_Damage"), data.ParseFloat("Zombie_Leg_Multiplier"), data.ParseFloat("Zombie_Arm_Multiplier"), data.ParseFloat("Zombie_Spine_Multiplier"), data.ParseFloat("Zombie_Skull_Multiplier"));
        animalDamageMultiplier = new AnimalDamageMultiplier(data.ParseFloat("Animal_Damage"), data.ParseFloat("Animal_Leg_Multiplier"), data.ParseFloat("Animal_Spine_Multiplier"), data.ParseFloat("Animal_Skull_Multiplier"));
        barricadeDamage = data.ParseFloat("Barricade_Damage");
        structureDamage = data.ParseFloat("Structure_Damage");
        vehicleDamage = data.ParseFloat("Vehicle_Damage");
        resourceDamage = data.ParseFloat("Resource_Damage");
        if (data.ContainsKey("Object_Damage"))
        {
            objectDamage = data.ParseFloat("Object_Damage");
        }
        else
        {
            objectDamage = resourceDamage;
        }
        durability = data.ParseFloat("Durability");
        wear = data.ParseUInt8("Wear", 0);
        if (wear < 1)
        {
            wear = 1;
        }
        isInvulnerable = data.ContainsKey("Invulnerable");
        if (data.ContainsKey("Allow_Flesh_Fx"))
        {
            allowFleshFx = data.ParseBool("Allow_Flesh_Fx");
        }
        else
        {
            allowFleshFx = true;
        }
        if (data.ContainsKey("Stun_Zombie_Always"))
        {
            zombieStunOverride = EZombieStunOverride.Always;
        }
        else if (data.ContainsKey("Stun_Zombie_Never"))
        {
            zombieStunOverride = EZombieStunOverride.Never;
        }
        else
        {
            zombieStunOverride = EZombieStunOverride.None;
        }
        bypassAllowedToDamagePlayer = data.ParseBool("Bypass_Allowed_To_Damage_Player");
    }

    internal override void BuildCargoData(CargoBuilder builder)
    {
        base.BuildCargoData(builder);
        CargoDeclaration orAddDeclaration = builder.GetOrAddDeclaration("Weapon");
        orAddDeclaration.AppendGuid("GUID", GUID);
        orAddDeclaration.AppendFloat("Durability", durability);
        orAddDeclaration.AppendFloat("Range", range);
        orAddDeclaration.AppendByte("Wear", wear);
        orAddDeclaration.AppendFloat("Player_Damage", playerDamageMultiplier.damage);
        orAddDeclaration.AppendFloat("Player_Leg_Multiplier", playerDamageMultiplier.leg);
        orAddDeclaration.AppendFloat("Player_Arm_Multiplier", playerDamageMultiplier.arm);
        orAddDeclaration.AppendFloat("Player_Spine_Multiplier", playerDamageMultiplier.spine);
        orAddDeclaration.AppendFloat("Player_Skull_Multiplier", playerDamageMultiplier.skull);
        orAddDeclaration.AppendToString("Player_Damage_Bleeding", playerDamageBleeding);
        orAddDeclaration.AppendToString("Player_Damage_Bones", playerDamageBones);
        orAddDeclaration.AppendFloat("Player_Damage_Food", playerDamageFood);
        orAddDeclaration.AppendFloat("Player_Damage_Water", playerDamageWater);
        orAddDeclaration.AppendFloat("Player_Damage_Virus", playerDamageVirus);
        orAddDeclaration.AppendFloat("Player_Damage_Hallucination", playerDamageHallucination);
        orAddDeclaration.AppendFloat("Zombie_Damage", zombieDamageMultiplier.damage);
        orAddDeclaration.AppendFloat("Zombie_Leg_Multiplier", zombieDamageMultiplier.leg);
        orAddDeclaration.AppendFloat("Zombie_Arm_Multiplier", zombieDamageMultiplier.arm);
        orAddDeclaration.AppendFloat("Zombie_Spine_Multiplier", zombieDamageMultiplier.spine);
        orAddDeclaration.AppendFloat("Zombie_Skull_Multiplier", zombieDamageMultiplier.skull);
        orAddDeclaration.AppendToString("zombieStunOverride", zombieStunOverride);
        orAddDeclaration.AppendFloat("Animal_Damage", animalDamageMultiplier.damage);
        orAddDeclaration.AppendFloat("Animal_Leg_Multiplier", animalDamageMultiplier.leg);
        orAddDeclaration.AppendFloat("Animal_Spine_Multiplier", animalDamageMultiplier.spine);
        orAddDeclaration.AppendFloat("Animal_Skull_Multiplier", animalDamageMultiplier.skull);
        orAddDeclaration.AppendInt("BladeIDs", bladeIDs.Length);
        orAddDeclaration.AppendFloat("Barricade_Damage", barricadeDamage);
        orAddDeclaration.AppendFloat("Structure_Damage", structureDamage);
        orAddDeclaration.AppendFloat("Vehicle_Damage", vehicleDamage);
        orAddDeclaration.AppendFloat("Resource_Damage", resourceDamage);
        orAddDeclaration.AppendFloat("Object_Damage", objectDamage);
        orAddDeclaration.AppendBool("Invulnerable", isInvulnerable);
        for (byte b = 0; b < bladeIDs.Length; b++)
        {
            CargoDeclaration cargoDeclaration = builder.AddDeclaration("Weapon_BladeID");
            cargoDeclaration.AppendGuid("GUID", GUID);
            cargoDeclaration.AppendToString("BladeID", bladeIDs[b]);
        }
    }
}
