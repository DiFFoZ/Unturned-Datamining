using System;
using System.Collections.Generic;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class UseableMelee : Useable
{
    private uint startedUse;

    private float startedSwing;

    private float weakAttackAnimLengthSeconds;

    private float strongAttackAnimLengthSeconds;

    private uint weakAttackAnimLengthFrames;

    private uint strongAttackAnimLengthFrames;

    /// <summary>
    /// For non-repeat weapons the "Use" audio clip is played once time reaches this point.
    /// </summary>
    private double playUseSoundTime;

    private bool isUsing;

    private bool isSwinging;

    private ESwingMode swingMode;

    private ParticleSystem firstEmitter;

    private ParticleSystem thirdEmitter;

    private Transform firstLightHook;

    private Transform thirdLightHook;

    private Transform firstFakeLight;

    private bool interact;

    private static ClientInstanceMethod<Vector3, Vector3, string, Transform> SendSpawnMeleeImpact = ClientInstanceMethod<Vector3, Vector3, string, Transform>.Get(typeof(UseableMelee), "ReceiveSpawnMeleeImpact");

    private static readonly ServerInstanceMethod SendInteractMelee = ServerInstanceMethod.Get(typeof(UseableMelee), "ReceiveInteractMelee");

    private static readonly ClientInstanceMethod SendPlaySwingStart = ClientInstanceMethod.Get(typeof(UseableMelee), "ReceivePlaySwingStart");

    private static readonly ClientInstanceMethod SendPlaySwingStop = ClientInstanceMethod.Get(typeof(UseableMelee), "ReceivePlaySwingStop");

    private static readonly ClientInstanceMethod<ESwingMode> SendPlaySwing = ClientInstanceMethod<ESwingMode>.Get(typeof(UseableMelee), "ReceivePlaySwing");

    public ItemMeleeAsset equippedMeleeAsset => base.player.equipment.asset as ItemMeleeAsset;

    private bool isUseable
    {
        get
        {
            if (swingMode == ESwingMode.WEAK)
            {
                return base.player.input.simulation - startedUse > weakAttackAnimLengthFrames;
            }
            if (swingMode == ESwingMode.STRONG)
            {
                return base.player.input.simulation - startedUse > strongAttackAnimLengthFrames;
            }
            return false;
        }
    }

    private bool isDamageable
    {
        get
        {
            if (swingMode == ESwingMode.WEAK)
            {
                return (float)(base.player.input.simulation - startedUse) > (float)weakAttackAnimLengthFrames * equippedMeleeAsset.weak;
            }
            if (swingMode == ESwingMode.STRONG)
            {
                return (float)(base.player.input.simulation - startedUse) > (float)strongAttackAnimLengthFrames * equippedMeleeAsset.strong;
            }
            return false;
        }
    }

    public override bool canInspect
    {
        get
        {
            if (!isUsing)
            {
                return !isSwinging;
            }
            return false;
        }
    }

    private void swing()
    {
        startedUse = base.player.input.simulation;
        startedSwing = Time.realtimeSinceStartup;
        isUsing = true;
        isSwinging = true;
        if (swingMode == ESwingMode.WEAK)
        {
            base.player.animator.play("Weak", smooth: false);
            playUseSoundTime = Time.timeAsDouble + (double)(weakAttackAnimLengthSeconds * equippedMeleeAsset.weak);
        }
        else if (swingMode == ESwingMode.STRONG)
        {
            base.player.animator.play("Strong", smooth: false);
            playUseSoundTime = Time.timeAsDouble + (double)(strongAttackAnimLengthSeconds * equippedMeleeAsset.strong);
        }
    }

    private void startSwing()
    {
        startedUse = base.player.input.simulation;
        startedSwing = Time.realtimeSinceStartup;
        isUsing = true;
        isSwinging = true;
        base.player.animator.play("Start_Swing", smooth: false);
    }

    private void stopSwing()
    {
        isUsing = false;
        isSwinging = false;
        base.player.animator.play("Stop_Swing", smooth: false);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public void ReceiveSpawnMeleeImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform)
    {
        DamageTool.LocalSpawnBulletImpactEffect(position, normal, materialName, colliderTransform);
        DamageTool.PlayMeleeImpactAudio(position, materialName);
        if (equippedMeleeAsset != null)
        {
            AudioReference audioReference = base.player.equipment.GetUseableSpecialAudioOverride();
            if (audioReference.IsNullOrEmpty)
            {
                audioReference = equippedMeleeAsset.impactAudio;
            }
            float volumeMultiplier;
            float pitchMultiplier;
            AudioClip audioClip = audioReference.LoadAudioClip(out volumeMultiplier, out pitchMultiplier);
            if (!(audioClip == null))
            {
                OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(position, audioClip);
                oneShotAudioParameters.volume = volumeMultiplier;
                oneShotAudioParameters.pitch = pitchMultiplier;
                oneShotAudioParameters.SetLinearRolloff(1f, 16f);
                oneShotAudioParameters.Play();
            }
        }
    }

    internal void ServerSpawnMeleeImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform, List<ITransportConnection> transportConnections)
    {
        position += normal * UnityEngine.Random.Range(0.04f, 0.06f);
        SendSpawnMeleeImpact.Invoke(GetNetId(), ENetReliability.Unreliable, transportConnections, position, normal, materialName, colliderTransform);
    }

    [Obsolete]
    public void askInteractMelee(CSteamID steamID)
    {
        ReceiveInteractMelee();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 10, legacyName = "askInteractMelee")]
    public void ReceiveInteractMelee()
    {
        if (!base.player.equipment.isBusy && base.player.equipment.asset != null && equippedMeleeAsset.isLight)
        {
            interact = !interact;
            base.player.equipment.state[0] = (byte)(interact ? 1u : 0u);
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    [Obsolete]
    public void askSwingStart(CSteamID steamID)
    {
        ReceivePlaySwingStart();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askSwingStart")]
    public void ReceivePlaySwingStart()
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            startSwing();
        }
    }

    [Obsolete]
    public void askSwingStop(CSteamID steamID)
    {
        ReceivePlaySwingStop();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askSwingStop")]
    public void ReceivePlaySwingStop()
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            stopSwing();
        }
    }

    [Obsolete]
    public void askSwing(CSteamID steamID, byte mode)
    {
        ReceivePlaySwing((ESwingMode)mode);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askSwing")]
    public void ReceivePlaySwing(ESwingMode mode)
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            swingMode = mode;
            swing();
        }
    }

    private void fire()
    {
        float num = (float)(int)base.player.equipment.quality / 100f;
        if (Provider.isServer)
        {
            AlertTool.alert(base.transform.position, equippedMeleeAsset.alertRadius);
        }
        if (base.channel.IsLocalPlayer)
        {
            if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out int data))
            {
                Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", data + 1);
            }
            RaycastInfo raycastInfo = DamageTool.raycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), ((ItemWeaponAsset)base.player.equipment.asset).range, RayMasks.DAMAGE_CLIENT, base.player);
            if (raycastInfo.player != null && equippedMeleeAsset.playerDamageMultiplier.damage > 1f && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player) || equippedMeleeAsset.bypassAllowedToDamagePlayer))
            {
                if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                {
                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                }
                if (raycastInfo.limb == ELimb.SKULL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out data))
                {
                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", data + 1);
                }
                PlayerUI.hitmark(raycastInfo.point, worldspace: false, (raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
            }
            else if ((raycastInfo.zombie != null && equippedMeleeAsset.zombieDamageMultiplier.damage > 1f) || (raycastInfo.animal != null && equippedMeleeAsset.animalDamageMultiplier.damage > 1f))
            {
                if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                {
                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                }
                if (raycastInfo.limb == ELimb.SKULL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out data))
                {
                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", data + 1);
                }
                PlayerUI.hitmark(raycastInfo.point, worldspace: false, (raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
            }
            else if (raycastInfo.vehicle != null && equippedMeleeAsset.vehicleDamage > 1f)
            {
                if (equippedMeleeAsset.isRepair)
                {
                    if (!raycastInfo.vehicle.isExploded && !raycastInfo.vehicle.isRepaired && raycastInfo.vehicle.canPlayerRepair(base.player))
                    {
                        if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                        {
                            Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                        }
                        PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                    }
                }
                else if (!raycastInfo.vehicle.isDead && raycastInfo.vehicle.asset != null && raycastInfo.vehicle.canBeDamaged && (raycastInfo.vehicle.asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                {
                    if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                    {
                        Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                    }
                    PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                }
            }
            else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Barricade") && equippedMeleeAsset.barricadeDamage > 1f)
            {
                BarricadeDrop barricadeDrop = BarricadeDrop.FindByRootFast(raycastInfo.transform);
                if (barricadeDrop != null)
                {
                    ItemBarricadeAsset asset = barricadeDrop.asset;
                    if (asset != null)
                    {
                        if (equippedMeleeAsset.isRepair)
                        {
                            Interactable2HP component = raycastInfo.transform.GetComponent<Interactable2HP>();
                            if (component != null && asset.isRepairable && component.hp < 100)
                            {
                                if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                                {
                                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                                }
                                PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                            }
                        }
                        else if (asset.canBeDamaged && (asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                        {
                            if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                            {
                                Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                            }
                            PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                        }
                    }
                }
            }
            else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Structure") && equippedMeleeAsset.structureDamage > 1f)
            {
                StructureDrop structureDrop = StructureDrop.FindByRootFast(raycastInfo.transform);
                if (structureDrop != null)
                {
                    ItemStructureAsset asset2 = structureDrop.asset;
                    if (asset2 != null)
                    {
                        if (equippedMeleeAsset.isRepair)
                        {
                            Interactable2HP component2 = raycastInfo.transform.GetComponent<Interactable2HP>();
                            if (component2 != null && asset2.isRepairable && component2.hp < 100)
                            {
                                if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                                {
                                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                                }
                                PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                            }
                        }
                        else if (asset2.canBeDamaged && (asset2.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                        {
                            if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                            {
                                Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                            }
                            PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                        }
                    }
                }
            }
            else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Resource") && equippedMeleeAsset.resourceDamage > 1f)
            {
                if (ResourceManager.tryGetRegion(raycastInfo.transform, out var x, out var y, out var index))
                {
                    ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(x, y, index);
                    bool flag = resourceSpawnpoint.asset.vulnerableToAllMeleeWeapons || equippedMeleeAsset.hasBladeID(resourceSpawnpoint.asset.bladeID);
                    if (resourceSpawnpoint != null && !resourceSpawnpoint.isDead && flag)
                    {
                        if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                        {
                            Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                        }
                        PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                    }
                }
            }
            else if (raycastInfo.transform != null && equippedMeleeAsset.objectDamage > 1f)
            {
                InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
                if (componentInParent != null)
                {
                    raycastInfo.transform = componentInParent.transform;
                    raycastInfo.section = componentInParent.getSection(raycastInfo.collider.transform);
                    if (componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && equippedMeleeAsset.hasBladeID(componentInParent.asset.rubbleBladeID) && (componentInParent.asset.rubbleIsVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                    {
                        if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out data))
                        {
                            Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                        }
                        PlayerUI.hitmark(raycastInfo.point, worldspace: false, EPlayerHit.BUILD);
                    }
                }
            }
            if (!equippedMeleeAsset.allowFleshFx && (raycastInfo.player != null || raycastInfo.animal != null || raycastInfo.zombie != null))
            {
                raycastInfo.material = EPhysicsMaterial.NONE;
                raycastInfo.materialName = string.Empty;
            }
            base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Melee);
        }
        if (!Provider.isServer)
        {
            return;
        }
        switch (swingMode)
        {
        case ESwingMode.WEAK:
            equippedMeleeAsset.weakAttackQuestRewards.Grant(base.player);
            break;
        case ESwingMode.STRONG:
            equippedMeleeAsset.strongAttackQuestRewards.Grant(base.player);
            break;
        }
        if (!base.player.input.hasInputs())
        {
            return;
        }
        InputInfo input = base.player.input.getInput(doOcclusionCheck: true, ERaycastInfoUsage.Melee);
        if (input == null || (input.point - base.player.look.aim.position).sqrMagnitude > MathfEx.Square(equippedMeleeAsset.range + 4f))
        {
            return;
        }
        if ((!equippedMeleeAsset.isRepair || !equippedMeleeAsset.isRepeated) && !string.IsNullOrEmpty(input.materialName))
        {
            ServerSpawnMeleeImpact(input.point, input.normal, input.materialName, input.colliderTransform, base.channel.GatherOwnerAndClientConnectionsWithinSphere(input.point, EffectManager.SMALL));
        }
        EPlayerKill kill = EPlayerKill.NONE;
        uint xp = 0u;
        float num2 = 1f;
        num2 *= 1f + base.channel.owner.player.skills.mastery(0, 0) * 0.5f;
        num2 *= ((swingMode == ESwingMode.STRONG) ? equippedMeleeAsset.strength : 1f);
        num2 *= ((num < 0.5f) ? (0.5f + num) : 1f);
        ERagdollEffect useableRagdollEffect = base.player.equipment.getUseableRagdollEffect();
        if (input.type != 0 && input.type != ERaycastInfoType.SKIP && Provider.modeConfigData.Items.ShouldWeaponTakeDamage && base.player.equipment.quality > 0 && UnityEngine.Random.value < ((ItemWeaponAsset)base.player.equipment.asset).durability)
        {
            if (base.player.equipment.quality > ((ItemWeaponAsset)base.player.equipment.asset).wear)
            {
                base.player.equipment.quality -= ((ItemWeaponAsset)base.player.equipment.asset).wear;
            }
            else
            {
                base.player.equipment.quality = 0;
            }
            base.player.equipment.sendUpdateQuality();
        }
        if (input.type == ERaycastInfoType.PLAYER)
        {
            if (input.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, input.player) || equippedMeleeAsset.bypassAllowedToDamagePlayer))
            {
                IDamageMultiplier playerDamageMultiplier = equippedMeleeAsset.playerDamageMultiplier;
                DamagePlayerParameters parameters = DamagePlayerParameters.make(input.player, EDeathCause.MELEE, input.direction, playerDamageMultiplier, input.limb);
                parameters.killer = base.channel.owner.playerID.steamID;
                parameters.times = num2;
                parameters.respectArmor = true;
                parameters.trackKill = true;
                parameters.ragdollEffect = useableRagdollEffect;
                equippedMeleeAsset.initPlayerDamageParameters(ref parameters);
                if (base.player.input.IsUnderFakeLagPenalty)
                {
                    parameters.times *= Provider.configData.Server.Fake_Lag_Damage_Penalty_Multiplier;
                }
                DamageTool.damagePlayer(parameters, out kill);
            }
        }
        else if (input.type == ERaycastInfoType.ZOMBIE)
        {
            if (input.zombie != null)
            {
                EZombieStunOverride eZombieStunOverride = equippedMeleeAsset.zombieStunOverride;
                if (Provider.modeConfigData.Zombies.Only_Critical_Stuns && eZombieStunOverride == EZombieStunOverride.None && swingMode == ESwingMode.STRONG)
                {
                    eZombieStunOverride = EZombieStunOverride.Always;
                }
                IDamageMultiplier zombieOrPlayerDamageMultiplier = equippedMeleeAsset.zombieOrPlayerDamageMultiplier;
                DamageZombieParameters parameters2 = DamageZombieParameters.make(input.zombie, input.direction, zombieOrPlayerDamageMultiplier, input.limb);
                parameters2.times = num2;
                parameters2.allowBackstab = true;
                parameters2.respectArmor = true;
                parameters2.instigator = base.player;
                parameters2.zombieStunOverride = eZombieStunOverride;
                parameters2.ragdollEffect = useableRagdollEffect;
                if (base.player.movement.nav != byte.MaxValue)
                {
                    parameters2.AlertPosition = base.transform.position;
                }
                DamageTool.damageZombie(parameters2, out kill, out xp);
            }
        }
        else if (input.type == ERaycastInfoType.ANIMAL)
        {
            if (input.animal != null)
            {
                IDamageMultiplier animalOrPlayerDamageMultiplier = equippedMeleeAsset.animalOrPlayerDamageMultiplier;
                DamageAnimalParameters parameters3 = DamageAnimalParameters.make(input.animal, input.direction, animalOrPlayerDamageMultiplier, input.limb);
                parameters3.times = num2;
                parameters3.instigator = base.player;
                parameters3.ragdollEffect = useableRagdollEffect;
                parameters3.AlertPosition = base.transform.position;
                DamageTool.damageAnimal(parameters3, out kill, out xp);
            }
        }
        else if (input.type == ERaycastInfoType.VEHICLE)
        {
            if (input.vehicle != null && input.vehicle.asset != null)
            {
                if (equippedMeleeAsset.isRepair)
                {
                    if (!input.vehicle.isExploded && !input.vehicle.isRepaired && input.vehicle.canPlayerRepair(base.player))
                    {
                        num2 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
                        DamageTool.damage(input.vehicle, damageTires: true, input.point, equippedMeleeAsset.isRepair, equippedMeleeAsset.vehicleDamage, num2 * Provider.modeConfigData.Vehicles.Melee_Repair_Multiplier, canRepair: true, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                    }
                }
                else if (input.vehicle.canBeDamaged && (input.vehicle.asset.isVulnerable || equippedMeleeAsset.isInvulnerable))
                {
                    DamageTool.damage(input.vehicle, damageTires: true, input.point, equippedMeleeAsset.isRepair, equippedMeleeAsset.vehicleDamage, num2 * Provider.modeConfigData.Vehicles.Melee_Damage_Multiplier, canRepair: true, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                }
            }
        }
        else if (input.type == ERaycastInfoType.BARRICADE)
        {
            if (input.transform != null && input.transform.CompareTag("Barricade"))
            {
                BarricadeDrop barricadeDrop2 = BarricadeDrop.FindByRootFast(input.transform);
                if (barricadeDrop2 != null)
                {
                    ItemBarricadeAsset asset3 = barricadeDrop2.asset;
                    if (asset3 != null)
                    {
                        if (equippedMeleeAsset.isRepair)
                        {
                            if (asset3.isRepairable)
                            {
                                num2 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
                                DamageTool.damage(input.transform, isRepairing: true, equippedMeleeAsset.barricadeDamage, num2 * Provider.modeConfigData.Barricades.Melee_Repair_Multiplier, out kill, base.channel.owner.playerID.steamID);
                            }
                        }
                        else if (asset3.canBeDamaged && (asset3.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                        {
                            DamageTool.damage(input.transform, isRepairing: false, equippedMeleeAsset.barricadeDamage, num2 * Provider.modeConfigData.Barricades.Melee_Damage_Multiplier, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                        }
                    }
                }
            }
        }
        else if (input.type == ERaycastInfoType.STRUCTURE)
        {
            if (input.transform != null && input.transform.CompareTag("Structure"))
            {
                StructureDrop structureDrop2 = StructureDrop.FindByRootFast(input.transform);
                if (structureDrop2 != null)
                {
                    ItemStructureAsset asset4 = structureDrop2.asset;
                    if (asset4 != null)
                    {
                        if (equippedMeleeAsset.isRepair)
                        {
                            if (asset4.isRepairable)
                            {
                                num2 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
                                DamageTool.damage(input.transform, isRepairing: true, input.direction, equippedMeleeAsset.structureDamage, num2 * Provider.modeConfigData.Structures.Melee_Repair_Multiplier, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                            }
                        }
                        else if (asset4.canBeDamaged && (asset4.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
                        {
                            DamageTool.damage(input.transform, isRepairing: false, input.direction, equippedMeleeAsset.structureDamage, num2 * Provider.modeConfigData.Structures.Melee_Damage_Multiplier, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                        }
                    }
                }
            }
        }
        else if (input.type == ERaycastInfoType.RESOURCE)
        {
            if (input.transform != null && input.transform.CompareTag("Resource"))
            {
                num2 *= 1f + base.channel.owner.player.skills.mastery(2, 2) * 0.5f;
                if (ResourceManager.tryGetRegion(input.transform, out var x2, out var y2, out var index2))
                {
                    ResourceSpawnpoint resourceSpawnpoint2 = ResourceManager.getResourceSpawnpoint(x2, y2, index2);
                    bool flag2 = resourceSpawnpoint2.asset.vulnerableToAllMeleeWeapons || equippedMeleeAsset.hasBladeID(resourceSpawnpoint2.asset.bladeID);
                    if (resourceSpawnpoint2 != null && !resourceSpawnpoint2.isDead && flag2)
                    {
                        DamageTool.damage(input.transform, input.direction, equippedMeleeAsset.resourceDamage, num2, 1f + base.channel.owner.player.skills.mastery(2, 2) * 0.5f, out kill, out xp, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
                    }
                }
            }
        }
        else if (input.type == ERaycastInfoType.OBJECT && input.transform != null && input.section < byte.MaxValue)
        {
            InteractableObjectRubble componentInParent2 = input.transform.GetComponentInParent<InteractableObjectRubble>();
            if (componentInParent2 != null && componentInParent2.IsSectionIndexValid(input.section) && !componentInParent2.isSectionDead(input.section) && equippedMeleeAsset.hasBladeID(componentInParent2.asset.rubbleBladeID) && (componentInParent2.asset.rubbleIsVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable))
            {
                DamageTool.damage(componentInParent2.transform, input.direction, input.section, equippedMeleeAsset.objectDamage, num2, out kill, out xp, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Melee);
            }
        }
        if (input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !base.player.life.isAggressor)
        {
            float num3 = equippedMeleeAsset.range + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
            num3 *= num3;
            float ray_Aggressor_Distance = Provider.modeConfigData.Players.Ray_Aggressor_Distance;
            ray_Aggressor_Distance *= ray_Aggressor_Distance;
            Vector3 forward = base.player.look.aim.forward;
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                if (Provider.clients[i] == base.channel.owner)
                {
                    continue;
                }
                Player player = Provider.clients[i].player;
                if (!(player == null))
                {
                    Vector3 vector = player.look.aim.position - base.player.look.aim.position;
                    Vector3 vector2 = Vector3.Project(vector, forward);
                    if (vector2.sqrMagnitude < num3 && (vector2 - vector).sqrMagnitude < ray_Aggressor_Distance)
                    {
                        base.player.life.markAggressive(force: false);
                    }
                }
            }
        }
        if (Level.info.type == ELevelType.HORDE)
        {
            if (input.zombie != null)
            {
                if (input.limb == ELimb.SKULL)
                {
                    base.player.skills.askPay(10u);
                }
                else
                {
                    base.player.skills.askPay(5u);
                }
            }
            if (kill == EPlayerKill.ZOMBIE)
            {
                if (input.limb == ELimb.SKULL)
                {
                    base.player.skills.askPay(50u);
                }
                else
                {
                    base.player.skills.askPay(25u);
                }
            }
        }
        else
        {
            if (kill == EPlayerKill.PLAYER && Level.info.type == ELevelType.ARENA)
            {
                base.player.skills.askPay(100u);
            }
            base.player.sendStat(kill);
            if (xp != 0)
            {
                base.player.skills.askPay(xp);
            }
        }
    }

    public override bool startPrimary()
    {
        if (base.player.equipment.isBusy || base.player.quests.IsCutsceneModeActive())
        {
            return false;
        }
        if (equippedMeleeAsset.isRepeated)
        {
            if (!isSwinging)
            {
                swingMode = ESwingMode.WEAK;
                startSwing();
                if (Provider.isServer)
                {
                    SendPlaySwingStart.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner());
                }
                return true;
            }
        }
        else if (isUseable)
        {
            base.player.equipment.isBusy = true;
            startedUse = base.player.input.simulation;
            startedSwing = Time.realtimeSinceStartup;
            isUsing = true;
            swingMode = ESwingMode.WEAK;
            swing();
            if (Provider.isServer)
            {
                SendPlaySwing.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner(), swingMode);
            }
            return true;
        }
        return false;
    }

    public override void stopPrimary()
    {
        if (!base.player.equipment.isBusy && !base.player.quests.IsCutsceneModeActive() && equippedMeleeAsset.isRepeated && isSwinging)
        {
            stopSwing();
            if (Provider.isServer)
            {
                SendPlaySwingStop.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner());
            }
        }
    }

    public override bool startSecondary()
    {
        if (base.player.equipment.isBusy)
        {
            return false;
        }
        if (!equippedMeleeAsset.isRepeated && isUseable && (float)(int)base.player.life.stamina >= (float)(int)equippedMeleeAsset.stamina * (1f - base.player.skills.mastery(0, 4) * 0.75f))
        {
            base.player.life.askTire((byte)((float)(int)equippedMeleeAsset.stamina * (1f - base.player.skills.mastery(0, 4) * 0.5f)));
            base.player.equipment.isBusy = true;
            swingMode = ESwingMode.STRONG;
            swing();
            if (Provider.isServer)
            {
                SendPlaySwing.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner(), swingMode);
            }
            return true;
        }
        return false;
    }

    public override void equip()
    {
        base.player.animator.play("Equip", smooth: true);
        if (equippedMeleeAsset.isLight)
        {
            interact = base.player.equipment.state[0] == 1;
            if (base.channel.IsLocalPlayer)
            {
                firstLightHook = base.player.equipment.firstModel.Find("Model_0").Find("Light");
                firstLightHook.tag = "Viewmodel";
                firstLightHook.gameObject.layer = 11;
                Transform transform = firstLightHook.Find("Light");
                if (transform != null)
                {
                    transform.tag = "Viewmodel";
                    transform.gameObject.layer = 11;
                }
                PlayerUI.message(EPlayerMessage.LIGHT, "");
            }
            thirdLightHook = base.player.equipment.thirdModel.Find("Model_0").Find("Light");
            LightLODTool.applyLightLOD(thirdLightHook);
            if (base.channel.IsLocalPlayer && thirdLightHook != null)
            {
                Transform transform2 = thirdLightHook.Find("Light");
                if (transform2 != null)
                {
                    firstFakeLight = UnityEngine.Object.Instantiate(transform2.gameObject).transform;
                    firstFakeLight.name = "Emitter";
                }
            }
        }
        else
        {
            firstLightHook = null;
            thirdLightHook = null;
        }
        updateAttachments();
        if (equippedMeleeAsset.isRepeated)
        {
            if (base.channel.IsLocalPlayer && base.player.equipment.firstModel.Find("Hit") != null)
            {
                firstEmitter = base.player.equipment.firstModel.Find("Hit").GetComponent<ParticleSystem>();
                firstEmitter.tag = "Viewmodel";
                firstEmitter.gameObject.layer = 11;
            }
            if (base.player.equipment.thirdModel.Find("Hit") != null)
            {
                thirdEmitter = base.player.equipment.thirdModel.Find("Hit").GetComponent<ParticleSystem>();
            }
            weakAttackAnimLengthSeconds = base.player.animator.GetAnimationLength("Start_Swing");
            strongAttackAnimLengthSeconds = base.player.animator.GetAnimationLength("Stop_Swing");
        }
        else
        {
            weakAttackAnimLengthSeconds = base.player.animator.GetAnimationLength("Weak");
            strongAttackAnimLengthSeconds = base.player.animator.GetAnimationLength("Strong");
        }
        weakAttackAnimLengthFrames = (uint)(weakAttackAnimLengthSeconds / PlayerInput.RATE);
        strongAttackAnimLengthFrames = (uint)(strongAttackAnimLengthSeconds / PlayerInput.RATE);
    }

    public override void dequip()
    {
        base.player.disableItemSpotLight();
        if (base.channel.IsLocalPlayer)
        {
            base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
            if (firstFakeLight != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight.gameObject);
                firstFakeLight = null;
            }
        }
    }

    public override void updateState(byte[] newState)
    {
        if (equippedMeleeAsset.isLight)
        {
            interact = newState[0] == 1;
        }
        updateAttachments();
    }

    public override void tick()
    {
        if (!base.player.equipment.IsEquipAnimationFinished)
        {
            return;
        }
        if (!Dedicator.IsDedicatedServer && isSwinging)
        {
            if (equippedMeleeAsset.isRepeated)
            {
                if ((double)(Time.realtimeSinceStartup - startedSwing) > 0.1)
                {
                    startedSwing = Time.realtimeSinceStartup;
                    if (firstEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST)
                    {
                        firstEmitter.Emit(4);
                    }
                    if (thirdEmitter != null && (!base.channel.IsLocalPlayer || base.player.look.perspective == EPlayerPerspective.THIRD))
                    {
                        thirdEmitter.Emit(4);
                    }
                    if (equippedMeleeAsset.isRepair)
                    {
                        base.player.playSound(((ItemMeleeAsset)base.player.equipment.asset).use, 0.1f);
                    }
                    else
                    {
                        base.player.playSound(((ItemMeleeAsset)base.player.equipment.asset).use, 0.5f);
                    }
                }
            }
            else if (Time.timeAsDouble >= playUseSoundTime)
            {
                if (swingMode == ESwingMode.WEAK)
                {
                    base.player.playSound(((ItemMeleeAsset)base.player.equipment.asset).use, 0.5f);
                }
                else if (swingMode == ESwingMode.STRONG)
                {
                    base.player.playSound(((ItemMeleeAsset)base.player.equipment.asset).use, 0.5f, 0.7f, 0.1f);
                }
                isSwinging = false;
            }
        }
        if (!base.channel.IsLocalPlayer)
        {
            return;
        }
        if (isSwinging)
        {
            if (equippedMeleeAsset.isRepeated && !equippedMeleeAsset.isRepair)
            {
                base.player.animator.viewmodelCameraLocalPositionOffset = new Vector3(UnityEngine.Random.Range(-0.05f, 0.05f), UnityEngine.Random.Range(-0.05f, 0.05f), UnityEngine.Random.Range(-0.05f, 0.05f));
            }
            else
            {
                base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
            }
        }
        if (InputEx.GetKeyDown(ControlsSettings.tactical) && equippedMeleeAsset.isLight)
        {
            SendInteractMelee.Invoke(GetNetId(), ENetReliability.Unreliable);
        }
    }

    public override void simulate(uint simulation, bool inputSteady)
    {
        if (isUsing && isDamageable)
        {
            if (equippedMeleeAsset.isRepeated)
            {
                startedUse = base.player.input.simulation;
            }
            else
            {
                base.player.equipment.isBusy = false;
                isUsing = false;
            }
            fire();
        }
    }

    private void updateAttachments()
    {
        if (!equippedMeleeAsset.isLight)
        {
            return;
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (base.channel.IsLocalPlayer && firstLightHook != null)
            {
                firstLightHook.gameObject.SetActive(interact);
            }
            if (thirdLightHook != null)
            {
                thirdLightHook.gameObject.SetActive(interact);
            }
        }
        if (interact && equippedMeleeAsset != null)
        {
            base.player.enableItemSpotLight(equippedMeleeAsset.lightConfig);
        }
        else
        {
            base.player.disableItemSpotLight();
        }
    }

    private void Update()
    {
        if (base.channel.IsLocalPlayer && firstFakeLight != null && thirdLightHook != null)
        {
            firstFakeLight.position = thirdLightHook.position;
            if (firstFakeLight.gameObject.activeSelf != (base.player.look.perspective == EPlayerPerspective.FIRST && thirdLightHook.gameObject.activeSelf))
            {
                firstFakeLight.gameObject.SetActive(base.player.look.perspective == EPlayerPerspective.FIRST && thirdLightHook.gameObject.activeSelf);
            }
        }
    }
}
