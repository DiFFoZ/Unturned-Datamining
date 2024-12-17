using System;
using System.Collections.Generic;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class DamageTool
{
    public delegate void DamagePlayerHandler(ref DamagePlayerParameters parameters, ref bool shouldAllow);

    public delegate void DamageZombieHandler(ref DamageZombieParameters parameters, ref bool shouldAllow);

    public delegate void DamageAnimalHandler(ref DamageAnimalParameters parameters, ref bool shouldAllow);

    public delegate void PlayerAllowedToDamagePlayerHandler(Player instigator, Player victim, ref bool isAllowed);

    [Obsolete("Use damagePlayerRequested")]
    public static DamageToolPlayerDamagedHandler playerDamaged;

    [Obsolete("Use damageZombieRequested")]
    public static DamageToolZombieDamagedHandler zombieDamaged;

    [Obsolete("Use damageAnimalRequested")]
    public static DamageToolAnimalDamagedHandler animalDamaged;

    /// <summary>
    /// Refer to ExplosionPoolData for pooling explanation.
    /// </summary>
    private static List<ExplosionPoolData> explosionDataPool = new List<ExplosionPoolData>();

    private static ExplosionRangeComparator explosionRangeComparator = new ExplosionRangeComparator();

    internal const int EXPLOSION_CLOSEST_POINT_LAYER_MASK = -4194305;

    private static Collider[] explosionColliders = new Collider[256];

    private static HashSet<IExplosionDamageable> explosionOverlaps = new HashSet<IExplosionDamageable>();

    /// <summary>
    /// Used if explosion won't damage anything.
    /// </summary>
    private static List<EPlayerKill> emptyKillsList = new List<EPlayerKill>();

    private static ClientStaticMethod<Vector3, Vector3, string, Transform, NetId> SendSpawnBulletImpact = ClientStaticMethod<Vector3, Vector3, string, Transform, NetId>.Get(ReceiveSpawnBulletImpact);

    private static ClientStaticMethod<Vector3, Vector3, string, Transform> SendSpawnLegacyImpact = ClientStaticMethod<Vector3, Vector3, string, Transform>.Get(ReceiveSpawnLegacyImpact);

    internal static readonly AssetReference<EffectAsset> FleshDynamicRef = new AssetReference<EffectAsset>("cea791255ba74b43a20e511a52ebcbec");

    internal static readonly AssetReference<EffectAsset> AlienDynamicRef = new AssetReference<EffectAsset>("67a4addd45174d7e9ca5c8ec24f8010f");

    /// <summary>
    /// Replacement for playerDamaged.
    /// </summary>
    public static event DamagePlayerHandler damagePlayerRequested;

    /// <summary>
    /// Replacement for zombieDamaged.
    /// </summary>
    public static event DamageZombieHandler damageZombieRequested;

    /// <summary>
    /// Replacement for animalDamaged.
    /// </summary>
    public static event DamageAnimalHandler damageAnimalRequested;

    public static event PlayerAllowedToDamagePlayerHandler onPlayerAllowedToDamagePlayer;

    public static ELimb getLimb(Transform limb)
    {
        if (limb.CompareTag("Player") || limb.CompareTag("Enemy") || limb.CompareTag("Zombie") || limb.CompareTag("Animal"))
        {
            switch (limb.name)
            {
            case "Left_Foot":
                return ELimb.LEFT_FOOT;
            case "Left_Leg":
                return ELimb.LEFT_LEG;
            case "Right_Foot":
                return ELimb.RIGHT_FOOT;
            case "Right_Leg":
                return ELimb.RIGHT_LEG;
            case "Left_Hand":
                return ELimb.LEFT_HAND;
            case "Left_Arm":
                return ELimb.LEFT_ARM;
            case "Right_Hand":
                return ELimb.RIGHT_HAND;
            case "Right_Arm":
                return ELimb.RIGHT_ARM;
            case "Left_Back":
                return ELimb.LEFT_BACK;
            case "Right_Back":
                return ELimb.RIGHT_BACK;
            case "Left_Front":
                return ELimb.LEFT_FRONT;
            case "Right_Front":
                return ELimb.RIGHT_FRONT;
            case "Spine":
                return ELimb.SPINE;
            case "Skull":
                return ELimb.SKULL;
            }
        }
        return ELimb.SPINE;
    }

    public static Player getPlayer(Transform limb)
    {
        Player player = limb.GetComponentInParent<Player>();
        if (player != null && player.life.isDead)
        {
            player = null;
        }
        return player;
    }

    public static Zombie getZombie(Transform limb)
    {
        Zombie zombie = limb.GetComponentInParent<Zombie>();
        if (zombie != null && zombie.isDead)
        {
            zombie = null;
        }
        return zombie;
    }

    public static Animal getAnimal(Transform limb)
    {
        Animal animal = limb.GetComponentInParent<Animal>();
        if (animal != null && animal.isDead)
        {
            animal = null;
        }
        return animal;
    }

    public static InteractableVehicle getVehicle(Transform model)
    {
        if (model == null)
        {
            return null;
        }
        model = model.root;
        InteractableVehicle component = model.GetComponent<InteractableVehicle>();
        if (component != null)
        {
            return component;
        }
        VehicleRef component2 = model.GetComponent<VehicleRef>();
        if (component2 != null)
        {
            return component2.vehicle;
        }
        return null;
    }

    public static Transform getBarricadeRootTransform(Transform barricadeTransform)
    {
        Transform transform = barricadeTransform;
        while (true)
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return transform;
            }
            if (parent.CompareTag("Vehicle"))
            {
                break;
            }
            transform = parent;
        }
        return transform;
    }

    /// <summary>
    /// Was necessary when structures were children of level transform.
    /// </summary>
    public static Transform getStructureRootTransform(Transform structureTransform)
    {
        return structureTransform.root;
    }

    /// <summary>
    /// Was necessary when trees were children of ground transform.
    /// </summary>
    public static Transform getResourceRootTransform(Transform resourceTransform)
    {
        return resourceTransform.root;
    }

    public static void damagePlayer(DamagePlayerParameters parameters, out EPlayerKill kill)
    {
        if (parameters.player == null || parameters.player.life.isDead)
        {
            kill = EPlayerKill.NONE;
            return;
        }
        bool shouldAllow = true;
        DamageTool.damagePlayerRequested?.Invoke(ref parameters, ref shouldAllow);
        if (playerDamaged != null)
        {
            playerDamaged(parameters.player, ref parameters.cause, ref parameters.limb, ref parameters.killer, ref parameters.direction, ref parameters.damage, ref parameters.times, ref shouldAllow);
        }
        if (!shouldAllow)
        {
            kill = EPlayerKill.NONE;
            return;
        }
        if (parameters.respectArmor)
        {
            parameters.times *= getPlayerArmor(parameters.limb, parameters.player);
        }
        if (parameters.applyGlobalArmorMultiplier)
        {
            parameters.times *= Provider.modeConfigData.Players.Armor_Multiplier;
        }
        int num = Mathf.FloorToInt(parameters.damage * parameters.times);
        if (num == 0)
        {
            kill = EPlayerKill.NONE;
            return;
        }
        byte b = (byte)Mathf.Min(255, num);
        bool flag = parameters.player.life.InternalCanDamage();
        bool canCauseBleeding;
        switch (parameters.bleedingModifier)
        {
        default:
            canCauseBleeding = true;
            break;
        case DamagePlayerParameters.Bleeding.Always:
            canCauseBleeding = false;
            if (flag)
            {
                parameters.player.life.serverSetBleeding(newBleeding: true);
            }
            break;
        case DamagePlayerParameters.Bleeding.Never:
            canCauseBleeding = false;
            break;
        case DamagePlayerParameters.Bleeding.Heal:
            canCauseBleeding = false;
            parameters.player.life.serverSetBleeding(newBleeding: false);
            break;
        }
        parameters.player.life.askDamage(b, parameters.direction * (int)b, parameters.cause, parameters.limb, parameters.killer, out kill, parameters.trackKill, parameters.ragdollEffect, canCauseBleeding);
        switch (parameters.bonesModifier)
        {
        case DamagePlayerParameters.Bones.Always:
            if (flag)
            {
                parameters.player.life.serverSetLegsBroken(newLegsBroken: true);
            }
            break;
        case DamagePlayerParameters.Bones.Heal:
            parameters.player.life.serverSetLegsBroken(newLegsBroken: false);
            break;
        }
        if (parameters.foodModifier > 0f || flag)
        {
            parameters.player.life.serverModifyFood(parameters.foodModifier);
        }
        if (parameters.waterModifier > 0f || flag)
        {
            parameters.player.life.serverModifyWater(parameters.waterModifier);
        }
        if (parameters.virusModifier > 0f || flag)
        {
            parameters.player.life.serverModifyVirus(parameters.virusModifier);
        }
        if (parameters.hallucinationModifier < 0f || flag)
        {
            parameters.player.life.serverModifyHallucination(parameters.hallucinationModifier);
        }
    }

    public static void damage(Player player, EDeathCause cause, ELimb limb, CSteamID killer, Vector3 direction, float damage, float times, out EPlayerKill kill, bool applyGlobalArmorMultiplier = true, bool trackKill = false, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamagePlayerParameters parameters = new DamagePlayerParameters(player);
        parameters.cause = cause;
        parameters.limb = limb;
        parameters.killer = killer;
        parameters.direction = direction;
        parameters.damage = damage;
        parameters.times = times;
        parameters.applyGlobalArmorMultiplier = applyGlobalArmorMultiplier;
        parameters.trackKill = trackKill;
        parameters.ragdollEffect = ragdollEffect;
        damagePlayer(parameters, out kill);
    }

    /// <summary>
    /// Get average explosionArmor of player's equipped clothing.
    /// </summary>
    public static float getPlayerExplosionArmor(Player player)
    {
        if (player == null)
        {
            return 1f;
        }
        return (0f + (player.clothing.pantsAsset?.explosionArmor ?? 1f) + (player.clothing.shirtAsset?.explosionArmor ?? 1f) + (player.clothing.vestAsset?.explosionArmor ?? 1f) + (player.clothing.hatAsset?.explosionArmor ?? 1f)) / 4f;
    }

    public static float getPlayerArmor(ELimb limb, Player player)
    {
        switch (limb)
        {
        case ELimb.LEFT_FOOT:
        case ELimb.LEFT_LEG:
        case ELimb.RIGHT_FOOT:
        case ELimb.RIGHT_LEG:
        {
            ItemClothingAsset pantsAsset = player.clothing.pantsAsset;
            if (pantsAsset != null)
            {
                if (Provider.modeConfigData.Items.ShouldClothingTakeDamage && player.clothing.pantsQuality > 0)
                {
                    player.clothing.pantsQuality--;
                    player.clothing.sendUpdatePantsQuality();
                }
                return pantsAsset.armor + (1f - pantsAsset.armor) * (1f - (float)(int)player.clothing.pantsQuality / 100f);
            }
            break;
        }
        case ELimb.LEFT_HAND:
        case ELimb.LEFT_ARM:
        case ELimb.RIGHT_HAND:
        case ELimb.RIGHT_ARM:
        {
            ItemClothingAsset shirtAsset2 = player.clothing.shirtAsset;
            if (shirtAsset2 != null)
            {
                if (Provider.modeConfigData.Items.ShouldClothingTakeDamage && player.clothing.shirtQuality > 0)
                {
                    player.clothing.shirtQuality--;
                    player.clothing.sendUpdateShirtQuality();
                }
                return shirtAsset2.armor + (1f - shirtAsset2.armor) * (1f - (float)(int)player.clothing.shirtQuality / 100f);
            }
            break;
        }
        case ELimb.SPINE:
        {
            float num = 1f;
            if (player.clothing.vestAsset != null)
            {
                ItemClothingAsset vestAsset = player.clothing.vestAsset;
                if (Provider.modeConfigData.Items.ShouldClothingTakeDamage && player.clothing.vestQuality > 0)
                {
                    player.clothing.vestQuality--;
                    player.clothing.sendUpdateVestQuality();
                }
                num *= vestAsset.armor + (1f - vestAsset.armor) * (1f - (float)(int)player.clothing.vestQuality / 100f);
            }
            if (player.clothing.shirtAsset != null)
            {
                ItemClothingAsset shirtAsset = player.clothing.shirtAsset;
                if (Provider.modeConfigData.Items.ShouldClothingTakeDamage && player.clothing.shirtQuality > 0)
                {
                    player.clothing.shirtQuality--;
                    player.clothing.sendUpdateShirtQuality();
                }
                num *= shirtAsset.armor + (1f - shirtAsset.armor) * (1f - (float)(int)player.clothing.shirtQuality / 100f);
            }
            return num;
        }
        case ELimb.SKULL:
        {
            ItemClothingAsset hatAsset = player.clothing.hatAsset;
            if (hatAsset != null)
            {
                if (Provider.modeConfigData.Items.ShouldClothingTakeDamage && player.clothing.hatQuality > 0)
                {
                    player.clothing.hatQuality--;
                    player.clothing.sendUpdateHatQuality();
                }
                return hatAsset.armor + (1f - hatAsset.armor) * (1f - (float)(int)player.clothing.hatQuality / 100f);
            }
            break;
        }
        }
        return 1f;
    }

    /// <summary>
    /// Refer to getPlayerExplosionArmor for explanation of total/average.
    /// </summary>
    public static float GetZombieExplosionArmor(Zombie zombie)
    {
        if (zombie.type < LevelZombies.tables.Count)
        {
            float num = 0f;
            num = ((zombie.pants == byte.MaxValue || zombie.pants >= LevelZombies.tables[zombie.type].slots[1].table.Count) ? (num + 1f) : (num + ((Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[1].table[zombie.pants].item) as ItemClothingAsset)?.explosionArmor ?? 1f)));
            num = ((zombie.shirt == byte.MaxValue || zombie.shirt >= LevelZombies.tables[zombie.type].slots[0].table.Count) ? (num + 1f) : (num + ((Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[0].table[zombie.shirt].item) as ItemClothingAsset)?.explosionArmor ?? 1f)));
            num = ((zombie.gear == byte.MaxValue || zombie.gear >= LevelZombies.tables[zombie.type].slots[3].table.Count) ? (num + 1f) : (num + ((Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[3].table[zombie.gear].item) as ItemClothingAsset)?.explosionArmor ?? 1f)));
            num = ((zombie.hat == byte.MaxValue || zombie.hat >= LevelZombies.tables[zombie.type].slots[2].table.Count) ? (num + 1f) : (num + ((Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[2].table[zombie.hat].item) as ItemClothingAsset)?.explosionArmor ?? 1f)));
            return num / 4f;
        }
        return 1f;
    }

    public static float getZombieArmor(ELimb limb, Zombie zombie)
    {
        if (zombie.type < LevelZombies.tables.Count)
        {
            switch (limb)
            {
            case ELimb.LEFT_FOOT:
            case ELimb.LEFT_LEG:
            case ELimb.RIGHT_FOOT:
            case ELimb.RIGHT_LEG:
                if (zombie.pants != byte.MaxValue && zombie.pants < LevelZombies.tables[zombie.type].slots[1].table.Count && Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[1].table[zombie.pants].item) is ItemClothingAsset itemClothingAsset4)
                {
                    return itemClothingAsset4.armor;
                }
                break;
            case ELimb.LEFT_HAND:
            case ELimb.LEFT_ARM:
            case ELimb.RIGHT_HAND:
            case ELimb.RIGHT_ARM:
                if (zombie.shirt != byte.MaxValue && zombie.shirt < LevelZombies.tables[zombie.type].slots[0].table.Count && Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[0].table[zombie.shirt].item) is ItemClothingAsset itemClothingAsset3)
                {
                    return itemClothingAsset3.armor;
                }
                break;
            case ELimb.SPINE:
            {
                float num = 1f;
                if (zombie.gear != byte.MaxValue && zombie.gear < LevelZombies.tables[zombie.type].slots[3].table.Count && Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[3].table[zombie.gear].item) is ItemAsset { type: EItemType.VEST } itemAsset)
                {
                    num *= ((ItemClothingAsset)itemAsset).armor;
                }
                if (zombie.shirt != byte.MaxValue && zombie.shirt < LevelZombies.tables[zombie.type].slots[0].table.Count && Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[0].table[zombie.shirt].item) is ItemClothingAsset itemClothingAsset2)
                {
                    num *= itemClothingAsset2.armor;
                }
                return num;
            }
            case ELimb.SKULL:
                if (zombie.hat != byte.MaxValue && zombie.hat < LevelZombies.tables[zombie.type].slots[2].table.Count && Assets.find(EAssetType.ITEM, LevelZombies.tables[zombie.type].slots[2].table[zombie.hat].item) is ItemClothingAsset itemClothingAsset)
                {
                    return itemClothingAsset.armor;
                }
                break;
            }
        }
        return 1f;
    }

    public static void damage(Player player, EDeathCause cause, ELimb limb, CSteamID killer, Vector3 direction, IDamageMultiplier multiplier, float times, bool armor, out EPlayerKill kill, bool trackKill = false, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamagePlayerParameters parameters = DamagePlayerParameters.make(player, cause, direction, multiplier, limb);
        parameters.killer = killer;
        parameters.times = times;
        parameters.respectArmor = armor;
        parameters.trackKill = trackKill;
        parameters.ragdollEffect = ragdollEffect;
        damagePlayer(parameters, out kill);
    }

    /// <summary>
    /// Do damage to a zombie.
    /// </summary>
    public static void damageZombie(DamageZombieParameters parameters, out EPlayerKill kill, out uint xp)
    {
        if (parameters.zombie == null || parameters.zombie.isDead)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        if (parameters.respectArmor)
        {
            parameters.times *= getZombieArmor(parameters.limb, parameters.zombie);
        }
        if (parameters.allowBackstab && (double)Vector3.Dot(parameters.zombie.transform.forward, parameters.direction) > 0.5)
        {
            parameters.times *= Provider.modeConfigData.Zombies.Backstab_Multiplier;
            if (Provider.modeConfigData.Zombies.Only_Critical_Stuns && parameters.zombieStunOverride == EZombieStunOverride.None)
            {
                parameters.zombieStunOverride = EZombieStunOverride.Always;
            }
        }
        bool shouldAllow = true;
        DamageTool.damageZombieRequested?.Invoke(ref parameters, ref shouldAllow);
        if (zombieDamaged != null)
        {
            zombieDamaged(parameters.zombie, ref parameters.direction, ref parameters.damage, ref parameters.times, ref shouldAllow);
        }
        if (!shouldAllow)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        if (parameters.applyGlobalArmorMultiplier)
        {
            if (parameters.limb == ELimb.SKULL)
            {
                parameters.times *= Provider.modeConfigData.Zombies.Armor_Multiplier;
            }
            else
            {
                parameters.times *= Provider.modeConfigData.Zombies.NonHeadshot_Armor_Multiplier;
            }
        }
        int num = Mathf.FloorToInt(parameters.damage * parameters.times);
        if (num == 0)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        ushort num2 = (ushort)Mathf.Min(65535, num);
        parameters.zombie.askDamage(num2, parameters.direction * (int)num2, out kill, out xp, trackKill: true, dropLoot: true, parameters.zombieStunOverride, parameters.ragdollEffect);
        if (parameters.AlertPosition.HasValue)
        {
            parameters.zombie.alert(parameters.AlertPosition.Value, isStartling: true);
        }
    }

    /// <summary>
    /// Legacy function replaced by damageZombie.
    /// </summary>
    public static void damage(Zombie zombie, Vector3 direction, float damage, float times, out EPlayerKill kill, out uint xp, EZombieStunOverride zombieStunOverride = EZombieStunOverride.None, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamageZombieParameters parameters = new DamageZombieParameters(zombie, direction, damage);
        parameters.times = times;
        parameters.zombieStunOverride = zombieStunOverride;
        parameters.ragdollEffect = ragdollEffect;
        damageZombie(parameters, out kill, out xp);
    }

    /// <summary>
    /// Legacy function replaced by damageZombie.
    /// </summary>
    public static void damage(Zombie zombie, ELimb limb, Vector3 direction, IDamageMultiplier multiplier, float times, bool armor, out EPlayerKill kill, out uint xp, EZombieStunOverride zombieStunOverride = EZombieStunOverride.None, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamageZombieParameters parameters = DamageZombieParameters.make(zombie, direction, multiplier, limb);
        parameters.legacyArmor = armor;
        parameters.times = times;
        parameters.zombieStunOverride = zombieStunOverride;
        parameters.ragdollEffect = ragdollEffect;
        damageZombie(parameters, out kill, out xp);
    }

    /// <summary>
    /// Do damage to an animal.
    /// </summary>
    public static void damageAnimal(DamageAnimalParameters parameters, out EPlayerKill kill, out uint xp)
    {
        if (parameters.animal == null || parameters.animal.isDead)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        bool shouldAllow = true;
        DamageTool.damageAnimalRequested?.Invoke(ref parameters, ref shouldAllow);
        if (animalDamaged != null)
        {
            animalDamaged(parameters.animal, ref parameters.direction, ref parameters.damage, ref parameters.times, ref shouldAllow);
        }
        if (!shouldAllow)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        if (parameters.applyGlobalArmorMultiplier)
        {
            parameters.times *= Provider.modeConfigData.Animals.Armor_Multiplier;
        }
        int num = Mathf.FloorToInt(parameters.damage * parameters.times);
        if (num == 0)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
            return;
        }
        ushort num2 = (ushort)Mathf.Min(65535, num);
        parameters.animal.askDamage(num2, parameters.direction * (int)num2, out kill, out xp, trackKill: true, dropLoot: true, parameters.ragdollEffect);
        if (parameters.AlertPosition.HasValue)
        {
            parameters.animal.alertDamagedFromPoint(parameters.AlertPosition.Value);
        }
    }

    /// <summary>
    /// Legacy function replaced by damageAnimal.
    /// </summary>
    public static void damage(Animal animal, Vector3 direction, float damage, float times, out EPlayerKill kill, out uint xp, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamageAnimalParameters parameters = new DamageAnimalParameters(animal, direction, damage);
        parameters.times = times;
        parameters.ragdollEffect = ragdollEffect;
        damageAnimal(parameters, out kill, out xp);
    }

    /// <summary>
    /// Legacy function replaced by damageAnimal.
    /// </summary>
    public static void damage(Animal animal, ELimb limb, Vector3 direction, IDamageMultiplier multiplier, float times, out EPlayerKill kill, out uint xp, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        DamageAnimalParameters parameters = DamageAnimalParameters.make(animal, direction, multiplier, limb);
        parameters.times = times;
        parameters.ragdollEffect = ragdollEffect;
        damageAnimal(parameters, out kill, out xp);
    }

    public static void damage(InteractableVehicle vehicle, bool damageTires, Vector3 position, bool isRepairing, float vehicleDamage, float times, bool canRepair, out EPlayerKill kill, CSteamID instigatorSteamID = default(CSteamID), EDamageOrigin damageOrigin = EDamageOrigin.Unknown)
    {
        kill = EPlayerKill.NONE;
        if (vehicle == null)
        {
            return;
        }
        if (isRepairing)
        {
            if (!vehicle.isExploded && !vehicle.isRepaired)
            {
                VehicleManager.repair(vehicle, vehicleDamage, times, instigatorSteamID);
            }
            return;
        }
        if (!vehicle.isDead)
        {
            VehicleManager.damage(vehicle, vehicleDamage, times, canRepair, instigatorSteamID, damageOrigin);
        }
        if (damageTires && !vehicle.isExploded)
        {
            int hitTireIndex = vehicle.getHitTireIndex(position);
            if (hitTireIndex != -1)
            {
                VehicleManager.damageTire(vehicle, hitTireIndex, instigatorSteamID, damageOrigin);
            }
        }
    }

    public static void damage(Transform barricade, bool isRepairing, float barricadeDamage, float times, out EPlayerKill kill, CSteamID instigatorSteamID = default(CSteamID), EDamageOrigin damageOrigin = EDamageOrigin.Unknown)
    {
        kill = EPlayerKill.NONE;
        if (!(barricade == null))
        {
            if (isRepairing)
            {
                BarricadeManager.repair(barricade, barricadeDamage, times, instigatorSteamID);
            }
            else
            {
                BarricadeManager.damage(barricade, barricadeDamage, times, armor: true, instigatorSteamID, damageOrigin);
            }
        }
    }

    public static void damage(Transform structure, bool isRepairing, Vector3 direction, float structureDamage, float times, out EPlayerKill kill, CSteamID instigatorSteamID = default(CSteamID), EDamageOrigin damageOrigin = EDamageOrigin.Unknown)
    {
        kill = EPlayerKill.NONE;
        if (!(structure == null))
        {
            if (isRepairing)
            {
                StructureManager.repair(structure, structureDamage, times, instigatorSteamID);
            }
            else
            {
                StructureManager.damage(structure, direction, structureDamage, times, armor: true, instigatorSteamID, damageOrigin);
            }
        }
    }

    public static void damage(Transform resource, Vector3 direction, float resourceDamage, float times, float drops, out EPlayerKill kill, out uint xp, CSteamID instigatorSteamID = default(CSteamID), EDamageOrigin damageOrigin = EDamageOrigin.Unknown)
    {
        if (resource == null)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
        }
        else
        {
            ResourceManager.damage(resource, direction, resourceDamage, times, drops, out kill, out xp, instigatorSteamID, damageOrigin);
        }
    }

    public static void damage(Transform obj, Vector3 direction, byte section, float objectDamage, float times, out EPlayerKill kill, out uint xp, CSteamID instigatorSteamID = default(CSteamID), EDamageOrigin damageOrigin = EDamageOrigin.Unknown)
    {
        if (obj == null)
        {
            kill = EPlayerKill.NONE;
            xp = 0u;
        }
        else
        {
            ObjectManager.damage(obj, direction, section, objectDamage, times, out kill, out xp, instigatorSteamID, damageOrigin);
        }
    }

    /// <summary>
    /// This unwieldy mess is the original explode function, but should be maintained for backwards compatibility with plugins.
    /// </summary>
    public static void explode(Vector3 point, float damageRadius, EDeathCause cause, CSteamID killer, float playerDamage, float zombieDamage, float animalDamage, float barricadeDamage, float structureDamage, float vehicleDamage, float resourceDamage, float objectDamage, out List<EPlayerKill> kills, EExplosionDamageType damageType = EExplosionDamageType.CONVENTIONAL, float alertRadius = 32f, bool playImpactEffect = true, bool penetrateBuildables = false, EDamageOrigin damageOrigin = EDamageOrigin.Unknown, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        ExplosionParameters parameters = new ExplosionParameters(point, damageRadius, cause, killer);
        parameters.playerDamage = playerDamage;
        parameters.zombieDamage = zombieDamage;
        parameters.animalDamage = animalDamage;
        parameters.barricadeDamage = barricadeDamage;
        parameters.structureDamage = structureDamage;
        parameters.vehicleDamage = vehicleDamage;
        parameters.resourceDamage = resourceDamage;
        parameters.objectDamage = objectDamage;
        parameters.damageType = damageType;
        parameters.alertRadius = alertRadius;
        parameters.playImpactEffect = playImpactEffect;
        parameters.penetrateBuildables = penetrateBuildables;
        parameters.damageOrigin = damageOrigin;
        parameters.ragdollEffect = ragdollEffect;
        parameters.launchSpeed = playerDamage * 0.1f;
        explode(parameters, out kills);
    }

    /// <summary>
    /// Do radial damage.
    /// </summary>
    public static void explode(ExplosionParameters parameters, out List<EPlayerKill> kills)
    {
        ThreadUtil.assertIsGameThread();
        emptyKillsList.Clear();
        kills = emptyKillsList;
        bool flag = parameters.structureDamage > 0.5f;
        bool flag2 = parameters.resourceDamage > 0.5f;
        bool flag3 = parameters.objectDamage > 0.5f;
        bool flag4 = parameters.barricadeDamage > 0.5f;
        bool flag5 = (Provider.isPvP || parameters.damageType == EExplosionDamageType.ZOMBIE_ACID || parameters.damageType == EExplosionDamageType.ZOMBIE_FIRE || parameters.damageType == EExplosionDamageType.ZOMBIE_ELECTRIC) && parameters.playerDamage > 0.5f;
        bool flag6 = flag5 || parameters.launchSpeed > 0.01f;
        bool flag7 = parameters.damageType == EExplosionDamageType.ZOMBIE_FIRE || parameters.zombieDamage > 0.5f;
        bool flag8 = parameters.animalDamage > 0.5f;
        bool flag9 = parameters.vehicleDamage > 0.5f;
        int num = 0;
        num |= (flag ? 268435456 : 0);
        num |= (flag2 ? 16384 : 0);
        num |= (flag3 ? 229376 : 0);
        num |= (flag4 ? 134234112 : 0);
        num |= (flag6 ? 1536 : 0);
        num |= (flag7 ? 16777216 : 0);
        num |= (flag8 ? 16777216 : 0);
        num |= (flag9 ? 67108864 : 0);
        if (num == 0)
        {
            return;
        }
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;
        int num2 = Physics.OverlapSphereNonAlloc(parameters.point, parameters.damageRadius, explosionColliders, num, queryTriggerInteraction);
        if (num2 < 1)
        {
            return;
        }
        if (num2 == explosionColliders.Length)
        {
            UnturnedLog.warn($"Explosion overlap reached non-alloc collider limit ({num2})! (Radius: {parameters.damageRadius})");
            explosionColliders = Physics.OverlapSphere(parameters.point, parameters.damageRadius, num, queryTriggerInteraction);
            num2 = explosionColliders.Length;
            UnturnedLog.warn($"New explosion collider limit: {num2}");
        }
        ExplosionPoolData item;
        if (explosionDataPool.Count > 0)
        {
            item = explosionDataPool.GetAndRemoveTail();
        }
        else
        {
            ExplosionPoolData explosionPoolData = default(ExplosionPoolData);
            explosionPoolData.damageCandidates = new List<ExplosionDamageCandidate>();
            explosionPoolData.kills = new List<EPlayerKill>();
            item = explosionPoolData;
        }
        item.damageCandidates.Clear();
        item.kills.Clear();
        explosionOverlaps.Clear();
        try
        {
            for (int i = 0; i < num2; i++)
            {
                Collider collider = explosionColliders[i];
                if (collider == null)
                {
                    continue;
                }
                Transform transform = collider.transform;
                if (!(transform == null))
                {
                    IExplosionDamageable componentInParent = transform.GetComponentInParent<IExplosionDamageable>();
                    if (componentInParent != null && componentInParent.IsEligibleForExplosionDamage && explosionOverlaps.Add(componentInParent))
                    {
                        Vector3 closestPointToExplosion = componentInParent.GetClosestPointToExplosion(parameters.point);
                        ExplosionDamageCandidate explosionDamageCandidate = default(ExplosionDamageCandidate);
                        explosionDamageCandidate.target = componentInParent;
                        explosionDamageCandidate.closestPoint = closestPointToExplosion;
                        ExplosionDamageCandidate item2 = explosionDamageCandidate;
                        item.damageCandidates.Add(item2);
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Caught exception while evaluating explosion damage candidates:");
        }
        if (item.damageCandidates.IsEmpty())
        {
            explosionDataPool.Add(item);
            return;
        }
        explosionRangeComparator.explosionCenter = parameters.point;
        item.damageCandidates.Sort(explosionRangeComparator);
        int obstructionMask = ((!parameters.penetrateBuildables) ? RayMasks.BLOCK_EXPLOSION : RayMasks.BLOCK_EXPLOSION_PENETRATE_BUILDABLES);
        ExplosionDamageParameters explosionDamageParameters = default(ExplosionDamageParameters);
        explosionDamageParameters.kills = item.kills;
        explosionDamageParameters.xp = 0u;
        explosionDamageParameters.obstructionMask = obstructionMask;
        explosionDamageParameters.shouldAffectStructures = flag;
        explosionDamageParameters.shouldAffectTrees = flag2;
        explosionDamageParameters.shouldAffectObjects = flag3;
        explosionDamageParameters.shouldAffectBarricades = flag4;
        explosionDamageParameters.canDealPlayerDamage = flag5;
        explosionDamageParameters.shouldAffectPlayers = flag6;
        explosionDamageParameters.shouldAffectZombies = flag7;
        explosionDamageParameters.shouldAffectAnimals = flag8;
        explosionDamageParameters.shouldAffectVehicles = flag9;
        ExplosionDamageParameters damageParameters = explosionDamageParameters;
        try
        {
            foreach (ExplosionDamageCandidate damageCandidate in item.damageCandidates)
            {
                if (damageCandidate.target != null && damageCandidate.target.IsEligibleForExplosionDamage)
                {
                    damageParameters.closestPoint = damageCandidate.closestPoint;
                    damageCandidate.target.ApplyExplosionDamage(in parameters, ref damageParameters);
                }
            }
        }
        catch (Exception e2)
        {
            UnturnedLog.exception(e2, "Caught exception while applying explosion damage:");
        }
        AlertTool.alert(parameters.point, parameters.alertRadius);
        kills = item.kills;
        explosionDataPool.Add(item);
    }

    [Obsolete("Physics material enum replaced by string names")]
    public static EPhysicsMaterial getMaterial(Vector3 point, Transform transform, Collider collider)
    {
        return PhysicsTool.GetLegacyMaterialByName(PhysicsTool.GetMaterialName(point, transform, collider));
    }

    /// <summary>
    /// Server spawn impact effect for all players within range.
    /// </summary>
    [Obsolete("Replaced by separate melee and bullet impact methods")]
    public static void impact(Vector3 point, Vector3 normal, EPhysicsMaterial material, bool forceDynamic)
    {
        impact(point, normal, material, forceDynamic, CSteamID.Nil, point);
    }

    /// <summary>
    /// Server spawn impact effect for all players within range. Optional "spectator" receives effect regardless of distance.
    /// </summary>
    [Obsolete("Replaced by separate melee and bullet impact methods")]
    public static void impact(Vector3 point, Vector3 normal, EPhysicsMaterial material, bool forceDynamic, CSteamID spectatorID, Vector3 spectatorPoint)
    {
        if (material != 0)
        {
            ushort id = 0;
            switch (material)
            {
            case EPhysicsMaterial.CLOTH_DYNAMIC:
            case EPhysicsMaterial.TILE_DYNAMIC:
            case EPhysicsMaterial.CONCRETE_DYNAMIC:
                id = 38;
                break;
            case EPhysicsMaterial.CLOTH_STATIC:
            case EPhysicsMaterial.TILE_STATIC:
            case EPhysicsMaterial.CONCRETE_STATIC:
                id = (ushort)(forceDynamic ? 38 : 13);
                break;
            case EPhysicsMaterial.FLESH_DYNAMIC:
                id = 5;
                break;
            case EPhysicsMaterial.GRAVEL_DYNAMIC:
                id = 44;
                break;
            case EPhysicsMaterial.GRAVEL_STATIC:
            case EPhysicsMaterial.SAND_STATIC:
                id = (ushort)(forceDynamic ? 44 : 14);
                break;
            case EPhysicsMaterial.METAL_DYNAMIC:
                id = 18;
                break;
            case EPhysicsMaterial.METAL_STATIC:
            case EPhysicsMaterial.METAL_SLIP:
                id = (ushort)(forceDynamic ? 18 : 12);
                break;
            case EPhysicsMaterial.WOOD_DYNAMIC:
                id = 17;
                break;
            case EPhysicsMaterial.WOOD_STATIC:
                id = (ushort)(forceDynamic ? 17 : 2);
                break;
            case EPhysicsMaterial.FOLIAGE_STATIC:
            case EPhysicsMaterial.FOLIAGE_DYNAMIC:
                id = 15;
                break;
            case EPhysicsMaterial.SNOW_STATIC:
            case EPhysicsMaterial.ICE_STATIC:
                id = 41;
                break;
            case EPhysicsMaterial.WATER_STATIC:
                id = 16;
                break;
            case EPhysicsMaterial.ALIEN_DYNAMIC:
                id = 95;
                break;
            }
            impact(point, normal, id, spectatorID, spectatorPoint);
        }
    }

    /// <summary>
    /// Server spawn effect by ID for all players within range. Optional "spectator" receives effect regardless of distance.
    /// </summary>
    [Obsolete("Replaced by ServerTriggerImpactEffectForMagazinesV2")]
    public static void impact(Vector3 point, Vector3 normal, ushort id, CSteamID spectatorID, Vector3 spectatorPoint)
    {
        if (id != 0)
        {
            ServerTriggerImpactEffectForMagazinesV2(Assets.find(EAssetType.EFFECT, id) as EffectAsset, point, normal, PlayerTool.getSteamPlayer(spectatorID));
        }
    }

    /// <summary>
    /// Server spawn effect for all players within range and instigator receives effect regardless of distance.
    /// </summary>
    public static void ServerTriggerImpactEffectForMagazinesV2(EffectAsset asset, Vector3 position, Vector3 normal, SteamPlayer instigatingClient)
    {
        if (asset != null)
        {
            position += normal * UnityEngine.Random.Range(0.04f, 0.06f);
            TriggerEffectParameters parameters = new TriggerEffectParameters(asset);
            parameters.position = position;
            parameters.SetDirection(normal);
            parameters.relevantDistance = EffectManager.SMALL;
            if (instigatingClient != null && instigatingClient.player != null && instigatingClient.player.channel != null)
            {
                parameters.SetRelevantTransportConnections(instigatingClient.player.channel.GatherOwnerAndClientConnectionsWithinSphere(position, EffectManager.SMALL));
            }
            EffectManager.triggerEffect(parameters);
        }
    }

    /// <summary>
    /// parent should only be set if that system also calls ClearAttachments, otherwise attachedEffects will leak memory.
    /// </summary>
    internal static void LocalSpawnBulletImpactEffect(Vector3 position, Vector3 normal, string materialName, Transform parent)
    {
        EffectAsset effectAsset = PhysicMaterialCustomData.WipDoNotUseTemp_GetBulletImpactEffect(materialName).Find();
        if (effectAsset != null)
        {
            EffectManager.internalSpawnEffect(effectAsset, position, normal, Vector3.one, wasInstigatedByPlayer: false, parent);
        }
    }

    private static void PlayBulletImpactAudio(Vector3 position, string materialName, bool wasInstigatedByLocalPlayer)
    {
        OneShotAudioDefinition audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "BulletImpact");
        if (!(audioDef == null))
        {
            AudioClip randomClip = audioDef.GetRandomClip();
            if (!(randomClip == null))
            {
                OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(position, randomClip);
                oneShotAudioParameters.volume = 0.6f * audioDef.volumeMultiplier;
                oneShotAudioParameters.RandomizePitch(audioDef.minPitch, audioDef.maxPitch);
                oneShotAudioParameters.SetLinearRolloff(1f, 16f);
                oneShotAudioParameters.spatialBlend = (wasInstigatedByLocalPlayer ? 0.9f : 1f);
                oneShotAudioParameters.Play();
            }
        }
    }

    internal static void PlayMeleeImpactAudio(Vector3 position, string materialName)
    {
        OneShotAudioDefinition audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "MeleeImpact");
        if (audioDef == null)
        {
            audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "LegacyImpact");
            if (audioDef == null)
            {
                return;
            }
        }
        AudioClip randomClip = audioDef.GetRandomClip();
        if (!(randomClip == null))
        {
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(position, randomClip);
            oneShotAudioParameters.volume = 0.6f * audioDef.volumeMultiplier;
            oneShotAudioParameters.RandomizePitch(audioDef.minPitch, audioDef.maxPitch);
            oneShotAudioParameters.SetLinearRolloff(1f, 16f);
            oneShotAudioParameters.Play();
        }
    }

    private static void PlayLegacyImpactAudio(Vector3 position, string materialName)
    {
        OneShotAudioDefinition audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "LegacyImpact");
        if (audioDef == null)
        {
            audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "MeleeImpact");
            if (audioDef == null)
            {
                return;
            }
        }
        AudioClip randomClip = audioDef.GetRandomClip();
        if (!(randomClip == null))
        {
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(position, randomClip);
            oneShotAudioParameters.volume = 0.6f * audioDef.volumeMultiplier;
            oneShotAudioParameters.RandomizePitch(audioDef.minPitch, audioDef.maxPitch);
            oneShotAudioParameters.SetLinearRolloff(1f, 16f);
            oneShotAudioParameters.Play();
        }
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveSpawnBulletImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform, NetId instigatorNetId)
    {
        bool wasInstigatedByLocalPlayer = Player.player != null && instigatorNetId == Player.player.channel.owner.GetNetId();
        LocalSpawnBulletImpactEffect(position, normal, materialName, colliderTransform);
        PlayBulletImpactAudio(position, materialName, wasInstigatedByLocalPlayer);
    }

    internal static void ServerSpawnBulletImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform, SteamPlayer instigatingClient, List<ITransportConnection> transportConnections)
    {
        position += normal * UnityEngine.Random.Range(0.04f, 0.06f);
        NetId arg = instigatingClient?.GetNetId() ?? NetId.INVALID;
        SendSpawnBulletImpact.Invoke(ENetReliability.Unreliable, transportConnections, position, normal, materialName, colliderTransform, arg);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveSpawnLegacyImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform)
    {
        LocalSpawnBulletImpactEffect(position, normal, materialName, colliderTransform);
        PlayLegacyImpactAudio(position, materialName);
    }

    internal static void ServerSpawnLegacyImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform, List<ITransportConnection> transportConnections)
    {
        position += normal * UnityEngine.Random.Range(0.04f, 0.06f);
        SendSpawnLegacyImpact.Invoke(ENetReliability.Unreliable, transportConnections, position, normal, materialName, colliderTransform);
    }

    public static RaycastInfo raycast(Ray ray, float range, int mask)
    {
        return raycast(ray, range, mask, null);
    }

    public static RaycastInfo raycast(Ray ray, float range, int mask, Player ignorePlayer = null)
    {
        Physics.Raycast(ray, out var hitInfo, range, mask);
        RaycastInfo raycastInfo = new RaycastInfo(hitInfo);
        raycastInfo.direction = ray.direction;
        raycastInfo.limb = ELimb.SPINE;
        if (raycastInfo.transform != null)
        {
            if (raycastInfo.transform.CompareTag("Barricade"))
            {
                raycastInfo.transform = getBarricadeRootTransform(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Structure"))
            {
                raycastInfo.transform = getStructureRootTransform(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Resource"))
            {
                raycastInfo.transform = getResourceRootTransform(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Enemy"))
            {
                raycastInfo.player = getPlayer(raycastInfo.transform);
                if (raycastInfo.player == ignorePlayer)
                {
                    raycastInfo.player = null;
                }
                raycastInfo.limb = getLimb(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Zombie"))
            {
                raycastInfo.zombie = getZombie(raycastInfo.transform);
                raycastInfo.limb = getLimb(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Animal"))
            {
                raycastInfo.animal = getAnimal(raycastInfo.transform);
                raycastInfo.limb = getLimb(raycastInfo.transform);
            }
            else if (raycastInfo.transform.CompareTag("Vehicle"))
            {
                raycastInfo.vehicle = getVehicle(raycastInfo.transform);
            }
            if (raycastInfo.zombie != null && raycastInfo.zombie.isRadioactive)
            {
                raycastInfo.materialName = "Alien_Dynamic";
                raycastInfo.material = EPhysicsMaterial.ALIEN_DYNAMIC;
            }
            else
            {
                raycastInfo.materialName = PhysicsTool.GetMaterialName(hitInfo);
                raycastInfo.material = PhysicsTool.GetLegacyMaterialByName(raycastInfo.materialName);
            }
        }
        return raycastInfo;
    }

    public static bool isPlayerAllowedToDamagePlayer(Player instigator, Player victim)
    {
        bool isAllowed = Provider.isPvP && (Provider.modeConfigData.Gameplay.Friendly_Fire || !instigator.quests.isMemberOfSameGroupAs(victim));
        if (!instigator.movement.canAddSimulationResultsToUpdates)
        {
            isAllowed = false;
        }
        if (DamageTool.onPlayerAllowedToDamagePlayer != null)
        {
            try
            {
                DamageTool.onPlayerAllowedToDamagePlayer(instigator, victim, ref isAllowed);
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Plugin raised an exception from onPlayerAllowedToDamagePlayer:");
                UnturnedLog.exception(e);
            }
        }
        return isAllowed;
    }
}
