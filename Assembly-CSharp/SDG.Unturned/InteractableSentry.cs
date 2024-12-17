using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class InteractableSentry : InteractableStorage
{
    private static List<Player> playersInRadius = new List<Player>();

    private static List<Zombie> zombiesInRadius = new List<Zombie>();

    private static List<Animal> animalsInRadius = new List<Animal>();

    private static List<InteractableVehicle> vehiclesInRadius = new List<InteractableVehicle>();

    public InteractablePower power;

    private bool hasWeapon;

    private bool interact;

    private Attachments attachments;

    private AudioSource gunshotAudioSource;

    private ParticleSystem shellEmitter;

    private ParticleSystem muzzleEmitter;

    private Light muzzleLight;

    private ParticleSystem tracerEmitter;

    private Transform yawTransform;

    private Transform pitchTransform;

    private Transform aimTransform;

    private GameObject onGameObject;

    private GameObject onModelGameObject;

    private Material onMaterial;

    private GameObject offGameObject;

    private GameObject offModelGameObject;

    private Material offMaterial;

    private GameObject spotGameObject;

    private Player targetPlayer;

    private Zombie targetZombie;

    private Animal targetAnimal;

    private InteractableVehicle targetVehicle;

    private float targetYaw;

    private float yaw;

    private float targetPitch;

    private float pitch;

    private bool isAlert;

    private double lastAlert;

    private bool isFiring;

    private double lastFire;

    private float fireTime;

    private bool isAiming;

    private double lastAim;

    private double lastScan;

    private double lastDrift;

    private double lastShot;

    internal static readonly ClientInstanceMethod SendShoot = ClientInstanceMethod.Get(typeof(InteractableSentry), "ReceiveShoot");

    internal static readonly ClientInstanceMethod<byte, byte> SendAlert = ClientInstanceMethod<byte, byte>.Get(typeof(InteractableSentry), "ReceiveAlert");

    private bool hasInitializedSentryComponents;

    public ItemSentryAsset sentryAsset { get; private set; }

    public ESentryMode sentryMode => sentryAsset.sentryMode;

    public bool isPowered
    {
        get
        {
            if (power == null)
            {
                return false;
            }
            if (sentryAsset.requiresPower)
            {
                return power.isWired;
            }
            return true;
        }
    }

    private void trace(Vector3 pos, Vector3 dir)
    {
        if (!(tracerEmitter == null) && (!(attachments.barrelModel != null) || !attachments.barrelAsset.isBraked || displayItem.state[16] <= 0))
        {
            tracerEmitter.transform.position = pos;
            tracerEmitter.transform.rotation = Quaternion.LookRotation(dir);
            tracerEmitter.Emit(1);
        }
    }

    public void shoot()
    {
        lastAlert = Time.timeAsDouble;
        if (!Dedicator.IsDedicatedServer)
        {
            if (gunshotAudioSource != null)
            {
                AudioClip clip = ((ItemGunAsset)displayAsset).shoot;
                float num = 1f;
                float num2 = ((ItemGunAsset)displayAsset).gunshotRolloffDistance;
                if (attachments.barrelAsset != null && displayItem.state[16] > 0)
                {
                    if (attachments.barrelAsset.shoot != null)
                    {
                        clip = attachments.barrelAsset.shoot;
                    }
                    num *= attachments.barrelAsset.volume;
                    num2 *= attachments.barrelAsset.gunshotRolloffDistanceMultiplier;
                }
                gunshotAudioSource.clip = clip;
                gunshotAudioSource.volume = num;
                gunshotAudioSource.maxDistance = num2;
                gunshotAudioSource.pitch = UnityEngine.Random.Range(0.975f, 1.025f);
                gunshotAudioSource.PlayOneShot(gunshotAudioSource.clip);
            }
            if (((ItemGunAsset)displayAsset).action == EAction.Trigger && shellEmitter != null)
            {
                shellEmitter.Emit(1);
            }
            if (attachments.barrelModel == null || !attachments.barrelAsset.isBraked || displayItem.state[16] == 0)
            {
                if (muzzleEmitter != null)
                {
                    muzzleEmitter.Emit(1);
                }
                if (muzzleLight != null)
                {
                    muzzleLight.enabled = true;
                }
            }
            if (aimTransform != null)
            {
                if (((ItemGunAsset)displayAsset).range < 32f)
                {
                    trace(aimTransform.position + aimTransform.forward * 32f, aimTransform.forward);
                }
                else
                {
                    trace(aimTransform.position + aimTransform.forward * UnityEngine.Random.Range(32f, Mathf.Min(64f, ((ItemGunAsset)displayAsset).range)), aimTransform.forward);
                }
            }
        }
        lastShot = Time.timeAsDouble;
        if (attachments.barrelAsset != null && attachments.barrelAsset.durability > 0)
        {
            if (attachments.barrelAsset.durability > displayItem.state[16])
            {
                displayItem.state[16] = 0;
            }
            else
            {
                displayItem.state[16] -= attachments.barrelAsset.durability;
            }
        }
    }

    public void alert(float newYaw, float newPitch)
    {
        targetYaw = newYaw;
        targetPitch = newPitch;
        lastAlert = Time.timeAsDouble;
    }

    public override void updateState(Asset asset, byte[] state)
    {
        sentryAsset = asset as ItemSentryAsset;
        if (!hasInitializedSentryComponents)
        {
            hasInitializedSentryComponents = true;
            yawTransform = base.transform.Find("Yaw");
            if (yawTransform != null)
            {
                pitchTransform = yawTransform.Find("Pitch");
                if (pitchTransform != null)
                {
                    aimTransform = pitchTransform.Find("Aim");
                    Transform transform = aimTransform.Find("Spot");
                    if (transform != null)
                    {
                        spotGameObject = transform.gameObject;
                    }
                }
            }
            Transform transform2 = base.transform.FindChildRecursive("On");
            if (transform2 != null)
            {
                onGameObject = transform2.gameObject;
            }
            Transform transform3 = base.transform.FindChildRecursive("On_Model");
            if (transform3 != null)
            {
                onModelGameObject = transform3.gameObject;
                onMaterial = onModelGameObject.GetComponent<Renderer>()?.material;
            }
            Transform transform4 = base.transform.FindChildRecursive("Off");
            if (transform4 != null)
            {
                offGameObject = transform4.gameObject;
            }
            Transform transform5 = base.transform.FindChildRecursive("Off_Model");
            if (transform5 != null)
            {
                offModelGameObject = transform5.gameObject;
                offMaterial = offModelGameObject.GetComponent<Renderer>()?.material;
            }
        }
        isAlert = false;
        lastAlert = 0.0;
        targetYaw = HousingConnections.GetModelYaw(base.transform);
        yaw = targetYaw;
        targetPitch = 0f;
        pitch = targetPitch;
        targetPlayer = null;
        targetAnimal = null;
        targetZombie = null;
        targetVehicle = null;
        base.updateState(asset, state);
    }

    public override void refreshDisplay()
    {
        base.refreshDisplay();
        hasWeapon = false;
        attachments = null;
        gunshotAudioSource = null;
        destroyEffects();
        if (spotGameObject != null)
        {
            spotGameObject.SetActive(value: false);
        }
        if (displayAsset == null || displayAsset.type != EItemType.GUN || ((ItemGunAsset)displayAsset).action == EAction.String || ((ItemGunAsset)displayAsset).action == EAction.Rocket)
        {
            return;
        }
        hasWeapon = true;
        attachments = displayModel.gameObject.GetComponent<Attachments>();
        interact = displayItem.state[12] == 1;
        if (!Dedicator.IsDedicatedServer)
        {
            gunshotAudioSource = displayModel.gameObject.AddComponent<AudioSource>();
            gunshotAudioSource.clip = null;
            gunshotAudioSource.spatialBlend = 1f;
            gunshotAudioSource.rolloffMode = AudioRolloffMode.Linear;
            gunshotAudioSource.volume = 1f;
            gunshotAudioSource.minDistance = 8f;
            gunshotAudioSource.maxDistance = 256f;
            gunshotAudioSource.playOnAwake = false;
            gunshotAudioSource.outputAudioMixerGroup = UnturnedAudioMixer.GetDefaultGroup();
        }
        if (attachments.ejectHook != null && ((ItemGunAsset)displayAsset).action != EAction.String && ((ItemGunAsset)displayAsset).action != EAction.Rocket)
        {
            EffectAsset effectAsset = ((ItemGunAsset)displayAsset).FindShellEffectAsset();
            if (effectAsset != null)
            {
                Transform transform = EffectManager.InstantiateFromPool(effectAsset).transform;
                transform.name = "Emitter";
                transform.parent = attachments.ejectHook;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                shellEmitter = transform.GetComponent<ParticleSystem>();
            }
        }
        if (attachments.barrelHook != null)
        {
            EffectAsset effectAsset2 = ((ItemGunAsset)displayAsset).FindMuzzleEffectAsset();
            if (effectAsset2 != null)
            {
                Transform transform2 = EffectManager.InstantiateFromPool(effectAsset2).transform;
                transform2.name = "Emitter";
                transform2.parent = attachments.barrelHook;
                transform2.localPosition = Vector3.zero;
                transform2.localRotation = Quaternion.identity;
                muzzleEmitter = transform2.GetComponent<ParticleSystem>();
                muzzleLight = transform2.GetComponent<Light>();
                if (muzzleLight != null)
                {
                    muzzleLight.enabled = false;
                    muzzleLight.cullingMask = -2049;
                }
            }
        }
        if (muzzleEmitter != null)
        {
            if (attachments.barrelModel != null)
            {
                muzzleEmitter.transform.localPosition = Vector3.up * 0.25f;
            }
            else
            {
                muzzleEmitter.transform.localPosition = Vector3.zero;
            }
        }
        if (attachments.magazineAsset != null)
        {
            EffectAsset effectAsset3 = attachments.magazineAsset.FindTracerEffectAsset();
            if (effectAsset3 != null)
            {
                Transform transform3 = EffectManager.InstantiateFromPool(effectAsset3).transform;
                transform3.name = "Tracer";
                transform3.localPosition = Vector3.zero;
                transform3.localRotation = Quaternion.identity;
                tracerEmitter = transform3.GetComponent<ParticleSystem>();
            }
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (attachments.tacticalAsset != null && (attachments.tacticalAsset.isLight || attachments.tacticalAsset.isLaser) && attachments.lightHook != null)
            {
                attachments.lightHook.gameObject.SetActive(interact);
            }
            if (spotGameObject != null)
            {
                spotGameObject.SetActive(attachments.tacticalAsset != null && attachments.tacticalAsset.isLight && interact);
            }
        }
        int num = ((ItemGunAsset)displayAsset).firerate;
        if (attachments.sightAsset != null)
        {
            num -= attachments.sightAsset.FirerateOffset;
        }
        if (attachments.tacticalAsset != null)
        {
            num -= attachments.tacticalAsset.FirerateOffset;
        }
        if (attachments.gripAsset != null)
        {
            num -= attachments.gripAsset.FirerateOffset;
        }
        if (attachments.barrelAsset != null)
        {
            num -= attachments.barrelAsset.FirerateOffset;
        }
        if (attachments.magazineAsset != null)
        {
            num -= attachments.magazineAsset.FirerateOffset;
        }
        num = Mathf.Max(num, 1);
        fireTime = num;
        fireTime /= 50f;
        fireTime *= 3.33f;
    }

    private void Update()
    {
        if (Provider.isServer && isPowered)
        {
            Vector3 vector = base.transform.position + new Vector3(0f, 0.65f, 0f);
            if (Time.timeAsDouble - lastScan > 0.10000000149011612)
            {
                lastScan = Time.timeAsDouble;
                ScanForTargets(vector);
            }
            if (targetPlayer != null)
            {
                switch (sentryMode)
                {
                case ESentryMode.NEUTRAL:
                case ESentryMode.FRIENDLY:
                    isFiring = targetPlayer.animator.gesture != EPlayerGesture.SURRENDER_START;
                    break;
                case ESentryMode.HOSTILE:
                    isFiring = true;
                    break;
                }
                isAiming = true;
            }
            else if (targetZombie != null)
            {
                isFiring = true;
                isAiming = true;
            }
            else if (targetAnimal != null)
            {
                switch (sentryMode)
                {
                case ESentryMode.NEUTRAL:
                case ESentryMode.FRIENDLY:
                    isFiring = targetAnimal.isHunting;
                    break;
                case ESentryMode.HOSTILE:
                    isFiring = true;
                    break;
                }
                isAiming = true;
            }
            else if (targetVehicle != null)
            {
                isFiring = true;
                isAiming = true;
            }
            else
            {
                isFiring = false;
                isAiming = false;
            }
            if (isAiming && Time.timeAsDouble - lastAim > (double)Provider.UPDATE_TIME)
            {
                lastAim = Time.timeAsDouble;
                Transform transform = null;
                Vector3 vector2 = Vector3.zero;
                if (targetPlayer != null)
                {
                    transform = targetPlayer.transform;
                    vector2 = targetPlayer.look.aim.position;
                }
                else if (targetZombie != null)
                {
                    transform = targetZombie.transform;
                    vector2 = targetZombie.transform.position;
                    switch (targetZombie.speciality)
                    {
                    case EZombieSpeciality.CRAWLER:
                        vector2 += new Vector3(0f, 0.25f, 0f);
                        break;
                    case EZombieSpeciality.MEGA:
                        vector2 += new Vector3(0f, 2.625f, 0f);
                        break;
                    case EZombieSpeciality.NORMAL:
                        vector2 += new Vector3(0f, 1.75f, 0f);
                        break;
                    case EZombieSpeciality.SPRINTER:
                        vector2 += new Vector3(0f, 1f, 0f);
                        break;
                    }
                }
                else if (targetAnimal != null)
                {
                    transform = targetAnimal.transform;
                    vector2 = targetAnimal.transform.position + Vector3.up;
                }
                else if (targetVehicle != null)
                {
                    transform = targetVehicle.transform;
                    vector2 = targetVehicle.GetSentryTargetingPoint();
                }
                if (transform != null)
                {
                    float num = Mathf.Atan2(vector2.x - vector.x, vector2.z - vector.z) * 57.29578f;
                    float num2 = Mathf.Sin((vector2.y - vector.y) / (vector2 - vector).magnitude) * 57.29578f;
                    BarricadeManager.sendAlertSentry(base.transform, num, num2);
                }
            }
            if (isFiring && hasWeapon && !isOpen)
            {
                bool flag = sentryAsset.infiniteAmmo || ((ItemGunAsset)displayAsset).infiniteAmmo;
                bool flag2 = flag || displayItem.state[10] >= ((ItemGunAsset)displayAsset).ammoPerShot;
                if (flag2 && Time.timeAsDouble - lastFire > (double)fireTime)
                {
                    lastFire += fireTime;
                    if (Time.timeAsDouble - lastFire > (double)fireTime)
                    {
                        lastFire = Time.timeAsDouble;
                    }
                    float quality = (float)(int)displayItem.quality / 100f;
                    if (attachments.magazineAsset == null)
                    {
                        return;
                    }
                    if (!flag && UnityEngine.Random.value <= sentryAsset.AmmoConsumptionProbability)
                    {
                        displayItem.state[10] -= ((ItemGunAsset)displayAsset).ammoPerShot;
                    }
                    if (attachments.barrelAsset == null || !attachments.barrelAsset.isSilenced || displayItem.state[16] == 0)
                    {
                        AlertTool.alert(base.transform.position, 48f);
                    }
                    if (!sentryAsset.infiniteQuality && Provider.modeConfigData.Items.ShouldWeaponTakeDamage && displayItem.quality > 0 && UnityEngine.Random.value < ((ItemWeaponAsset)displayAsset).durability && UnityEngine.Random.value <= sentryAsset.QualityConsumptionProbability)
                    {
                        if (displayItem.quality > ((ItemWeaponAsset)displayAsset).wear)
                        {
                            displayItem.quality -= ((ItemWeaponAsset)displayAsset).wear;
                        }
                        else
                        {
                            displayItem.quality = 0;
                        }
                    }
                    if (((ItemGunAsset)displayAsset).projectile == null)
                    {
                        float num3 = CalculateSpreadAngleRadians(quality);
                        BarricadeManager.sendShootSentry(base.transform);
                        float bulletDamageMultiplier = GetBulletDamageMultiplier(quality);
                        byte pellets = attachments.magazineAsset.pellets;
                        for (byte b = 0; b < pellets; b++)
                        {
                            EPlayerKill kill = EPlayerKill.NONE;
                            uint xp = 0u;
                            Transform transform2 = null;
                            float num4 = 0f;
                            if (targetPlayer != null)
                            {
                                transform2 = targetPlayer.transform;
                            }
                            else if (targetZombie != null)
                            {
                                transform2 = targetZombie.transform;
                            }
                            else if (targetAnimal != null)
                            {
                                transform2 = targetAnimal.transform;
                            }
                            if (transform2 != null)
                            {
                                num4 = (transform2.position - base.transform.position).magnitude;
                            }
                            float num5 = Mathf.Clamp01(num4 / ((ItemWeaponAsset)displayAsset).range);
                            float num6 = 1f - num5;
                            num6 *= CalculateChanceToHitSpreadMultiplier(num3);
                            num6 *= 0.75f;
                            if (transform2 == null || UnityEngine.Random.value > num6)
                            {
                                Vector3 randomForwardVectorInCone = RandomEx.GetRandomForwardVectorInCone(num3);
                                Vector3 direction = aimTransform.TransformDirection(randomForwardVectorInCone);
                                RaycastInfo raycastInfo = DamageTool.raycast(new Ray(aimTransform.position, direction), mask: RayMasks.DAMAGE_SERVER | 0x4000000, range: ((ItemWeaponAsset)displayAsset).range);
                                if (!(raycastInfo.transform == null))
                                {
                                    DamageTool.ServerSpawnBulletImpact(raycastInfo.point, raycastInfo.normal, raycastInfo.materialName, raycastInfo.collider?.transform, null, Provider.GatherClientConnectionsWithinSphere(raycastInfo.point, EffectManager.SMALL));
                                    if (raycastInfo.vehicle != null)
                                    {
                                        DamageTool.damage(raycastInfo.vehicle, damageTires: false, Vector3.zero, isRepairing: false, ((ItemGunAsset)displayAsset).vehicleDamage, bulletDamageMultiplier, canRepair: true, out kill, default(CSteamID), EDamageOrigin.Sentry);
                                    }
                                    else if (raycastInfo.transform != null)
                                    {
                                        if (raycastInfo.transform.CompareTag("Barricade"))
                                        {
                                            BarricadeDrop barricadeDrop = BarricadeDrop.FindByRootFast(raycastInfo.transform);
                                            if (barricadeDrop != null)
                                            {
                                                ItemBarricadeAsset asset = barricadeDrop.asset;
                                                if (asset != null && asset.canBeDamaged && (asset.isVulnerable || ((ItemWeaponAsset)displayAsset).isInvulnerable))
                                                {
                                                    DamageTool.damage(raycastInfo.transform, isRepairing: false, ((ItemGunAsset)displayAsset).barricadeDamage, bulletDamageMultiplier, out kill, default(CSteamID), EDamageOrigin.Sentry);
                                                }
                                            }
                                        }
                                        else if (raycastInfo.transform.CompareTag("Structure"))
                                        {
                                            StructureDrop structureDrop = StructureDrop.FindByRootFast(raycastInfo.transform);
                                            if (structureDrop != null)
                                            {
                                                ItemStructureAsset asset2 = structureDrop.asset;
                                                if (asset2 != null && asset2.canBeDamaged && (asset2.isVulnerable || ((ItemWeaponAsset)displayAsset).isInvulnerable))
                                                {
                                                    DamageTool.damage(raycastInfo.transform, isRepairing: false, raycastInfo.direction * Mathf.Ceil((float)(int)attachments.magazineAsset.pellets / 2f), ((ItemGunAsset)displayAsset).structureDamage, bulletDamageMultiplier, out kill, default(CSteamID), EDamageOrigin.Sentry);
                                                }
                                            }
                                        }
                                        else if (raycastInfo.transform.CompareTag("Resource"))
                                        {
                                            if (ResourceManager.tryGetRegion(raycastInfo.transform, out var x, out var y, out var index))
                                            {
                                                ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(x, y, index);
                                                if (resourceSpawnpoint != null && !resourceSpawnpoint.isDead && ((ItemWeaponAsset)displayAsset).hasBladeID(resourceSpawnpoint.asset.bladeID))
                                                {
                                                    DamageTool.damage(raycastInfo.transform, raycastInfo.direction * Mathf.Ceil((float)(int)attachments.magazineAsset.pellets / 2f), ((ItemGunAsset)displayAsset).resourceDamage, bulletDamageMultiplier, 1f, out kill, out xp, default(CSteamID), EDamageOrigin.Sentry);
                                                }
                                            }
                                        }
                                        else if (raycastInfo.section < byte.MaxValue)
                                        {
                                            InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
                                            if (componentInParent != null && componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && ((ItemWeaponAsset)displayAsset).hasBladeID(componentInParent.asset.rubbleBladeID) && (componentInParent.asset.rubbleIsVulnerable || ((ItemWeaponAsset)displayAsset).isInvulnerable))
                                            {
                                                DamageTool.damage(componentInParent.transform, raycastInfo.direction, raycastInfo.section, ((ItemGunAsset)displayAsset).objectDamage, bulletDamageMultiplier, out kill, out xp, default(CSteamID), EDamageOrigin.Sentry);
                                            }
                                        }
                                    }
                                    if (attachments.magazineAsset != null && attachments.magazineAsset.isExplosive)
                                    {
                                        Vector3 position = raycastInfo.point + raycastInfo.normal * 0.25f;
                                        UseableGun.DetonateExplosiveMagazine(attachments.magazineAsset, position, null, ERagdollEffect.NONE);
                                    }
                                }
                            }
                            else
                            {
                                Vector3 vector3 = Vector3.zero;
                                if (targetPlayer != null)
                                {
                                    vector3 = targetPlayer.look.aim.position;
                                }
                                else if (targetZombie != null)
                                {
                                    vector3 = targetZombie.transform.position;
                                    switch (targetZombie.speciality)
                                    {
                                    case EZombieSpeciality.CRAWLER:
                                        vector3 += new Vector3(0f, 0.25f, 0f);
                                        break;
                                    case EZombieSpeciality.MEGA:
                                        vector3 += new Vector3(0f, 2.625f, 0f);
                                        break;
                                    case EZombieSpeciality.NORMAL:
                                        vector3 += new Vector3(0f, 1.75f, 0f);
                                        break;
                                    case EZombieSpeciality.SPRINTER:
                                        vector3 += new Vector3(0f, 1f, 0f);
                                        break;
                                    }
                                }
                                else if (targetAnimal != null)
                                {
                                    vector3 = targetAnimal.transform.position + Vector3.up;
                                }
                                DamageTool.ServerSpawnBulletImpact(vector3, -aimTransform.forward, "Flesh_Dynamic", null, null, Provider.GatherClientConnectionsWithinSphere(vector3, EffectManager.SMALL));
                                Vector3 direction2 = aimTransform.forward * Mathf.Ceil((float)(int)attachments.magazineAsset.pellets / 2f);
                                if (targetPlayer != null)
                                {
                                    DamageTool.damage(targetPlayer, EDeathCause.SENTRY, ELimb.SPINE, base.owner, direction2, ((ItemGunAsset)displayAsset).playerDamageMultiplier, bulletDamageMultiplier, armor: true, out kill, trackKill: true);
                                }
                                else if (targetZombie != null)
                                {
                                    IDamageMultiplier zombieOrPlayerDamageMultiplier = ((ItemGunAsset)displayAsset).zombieOrPlayerDamageMultiplier;
                                    DamageZombieParameters parameters = DamageZombieParameters.make(targetZombie, direction2, zombieOrPlayerDamageMultiplier, ELimb.SPINE);
                                    parameters.times = bulletDamageMultiplier;
                                    parameters.allowBackstab = false;
                                    parameters.respectArmor = true;
                                    parameters.instigator = this;
                                    DamageTool.damageZombie(parameters, out kill, out xp);
                                }
                                else if (targetAnimal != null)
                                {
                                    IDamageMultiplier animalOrPlayerDamageMultiplier = ((ItemGunAsset)displayAsset).animalOrPlayerDamageMultiplier;
                                    DamageAnimalParameters parameters2 = DamageAnimalParameters.make(targetAnimal, direction2, animalOrPlayerDamageMultiplier, ELimb.SPINE);
                                    parameters2.times = bulletDamageMultiplier;
                                    parameters2.instigator = this;
                                    DamageTool.damageAnimal(parameters2, out kill, out xp);
                                }
                                if (attachments.magazineAsset != null && attachments.magazineAsset.isExplosive)
                                {
                                    Vector3 position2 = vector3 + aimTransform.forward * -0.25f;
                                    UseableGun.DetonateExplosiveMagazine(attachments.magazineAsset, position2, null, ERagdollEffect.NONE);
                                }
                            }
                        }
                    }
                    rebuildState();
                }
            }
        }
        bool flag3 = Time.timeAsDouble - lastAlert < 1.0;
        if (flag3 != isAlert)
        {
            isAlert = flag3;
            if (!Dedicator.IsDedicatedServer)
            {
                if (isAlert)
                {
                    EffectManager.effect(sentryAsset.targetAcquiredEffect, base.transform.position);
                }
                else
                {
                    EffectManager.effect(sentryAsset.targetLostEffect, base.transform.position);
                }
            }
            if (!isAlert)
            {
                targetYaw = HousingConnections.GetModelYaw(base.transform);
            }
        }
        if (!(power != null))
        {
            return;
        }
        if (isPowered)
        {
            if (isAlert)
            {
                lastDrift = Time.timeAsDouble;
                yaw = Mathf.LerpAngle(yaw, targetYaw, 4f * Time.deltaTime);
            }
            else
            {
                yaw = Mathf.LerpAngle(yaw, targetYaw + Mathf.Sin((float)(Time.timeAsDouble - lastDrift)) * 60f, 4f * Time.deltaTime);
            }
            pitch = Mathf.LerpAngle(pitch, targetPitch, 4f * Time.deltaTime);
            if (yawTransform != null)
            {
                yawTransform.rotation = Quaternion.Euler(-90f, 0f, yaw);
            }
            if (pitchTransform != null)
            {
                pitchTransform.localRotation = Quaternion.Euler(0f, -90f, pitch);
            }
        }
        if (onGameObject != null)
        {
            onGameObject.SetActive(isAlert && isPowered);
        }
        if (onModelGameObject != null)
        {
            onModelGameObject.SetActive(isAlert);
        }
        if (offGameObject != null)
        {
            offGameObject.SetActive(!isAlert && isPowered);
        }
        if (offModelGameObject != null)
        {
            offModelGameObject.SetActive(!isAlert);
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (onMaterial != null)
            {
                onMaterial.SetColor("_EmissionColor", (isAlert && isPowered) ? (onMaterial.color * 2f) : Color.black);
            }
            if (offMaterial != null)
            {
                offMaterial.SetColor("_EmissionColor", (!isAlert && isPowered) ? (offMaterial.color * 2f) : Color.black);
            }
            if (Time.timeAsDouble - lastShot > 0.05 && muzzleLight != null)
            {
                muzzleLight.GetComponent<Light>().enabled = false;
            }
        }
    }

    private void destroyEffects()
    {
        if (tracerEmitter != null)
        {
            EffectManager.DestroyIntoPool(tracerEmitter.gameObject);
            tracerEmitter = null;
        }
        if (muzzleEmitter != null)
        {
            EffectManager.DestroyIntoPool(muzzleEmitter.gameObject);
            muzzleEmitter = null;
        }
        muzzleLight = null;
        if (shellEmitter != null)
        {
            EffectManager.DestroyIntoPool(shellEmitter.gameObject);
            shellEmitter = null;
        }
    }

    private void OnDestroy()
    {
        destroyEffects();
        if (onMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(onMaterial);
            onMaterial = null;
        }
        if (offMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(offMaterial);
            offMaterial = null;
        }
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public void ReceiveShoot()
    {
        shoot();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public void ReceiveAlert(byte yaw, byte pitch)
    {
        alert(MeasurementTool.byteToAngle(yaw), MeasurementTool.byteToAngle(pitch));
    }

    /// <summary>
    /// Calculate damage multiplier for individual bullet.
    /// </summary>
    private float GetBulletDamageMultiplier(float quality)
    {
        float num = ((quality < 0.5f) ? (0.5f + quality) : 1f);
        if (attachments.magazineAsset != null)
        {
            num *= attachments.magazineAsset.ballisticDamageMultiplier;
        }
        if (attachments.sightAsset != null)
        {
            num *= attachments.sightAsset.ballisticDamageMultiplier;
        }
        if (attachments.tacticalAsset != null)
        {
            num *= attachments.tacticalAsset.ballisticDamageMultiplier;
        }
        if (attachments.barrelAsset != null)
        {
            num *= attachments.barrelAsset.ballisticDamageMultiplier;
        }
        if (attachments.gripAsset != null)
        {
            num *= attachments.gripAsset.ballisticDamageMultiplier;
        }
        return num;
    }

    private float CalculateSpreadAngleRadians(float quality)
    {
        float baseSpreadAngleRadians = ((ItemGunAsset)displayAsset).baseSpreadAngleRadians;
        baseSpreadAngleRadians *= ((ItemGunAsset)displayAsset).spreadAim;
        baseSpreadAngleRadians *= ((quality < 0.5f) ? (1f + (1f - quality * 2f)) : 1f);
        if (attachments.tacticalAsset != null && interact)
        {
            baseSpreadAngleRadians *= attachments.tacticalAsset.spread;
        }
        if (attachments.gripAsset != null)
        {
            baseSpreadAngleRadians *= attachments.gripAsset.spread;
        }
        if (attachments.barrelAsset != null)
        {
            baseSpreadAngleRadians *= attachments.barrelAsset.spread;
        }
        if (attachments.magazineAsset != null)
        {
            baseSpreadAngleRadians *= attachments.magazineAsset.spread;
        }
        return baseSpreadAngleRadians;
    }

    /// <summary>
    /// Each shot has a percentage chance to hit the target. Higher values are more likely to hit. e.g., it
    /// decreases from 1.0 at point blank to 0.0 at the weapon's maximum range. This chance is affected by the
    /// gun's spread.
    /// </summary>
    private float CalculateChanceToHitSpreadMultiplier(float spreadAngleRadians)
    {
        if (spreadAngleRadians > MathF.PI / 2f)
        {
            return 0f;
        }
        return Mathf.Cos(spreadAngleRadians);
    }

    private void ScanForTargets(Vector3 fromPoint)
    {
        float num = sentryAsset.detectionRadius;
        float num2 = sentryAsset.targetLossRadius;
        if (hasWeapon)
        {
            float range = ((ItemWeaponAsset)displayAsset).range;
            num = Mathf.Min(num, range);
            num2 = Mathf.Min(num2, range);
        }
        float num3 = num * num;
        float num4 = num2 * num2;
        float num5 = num3;
        bool flag = false;
        Player player = null;
        Zombie zombie = null;
        Animal animal = null;
        InteractableVehicle interactableVehicle = null;
        if (Provider.isPvP)
        {
            float sqrRadius = ((targetPlayer != null) ? num4 : num5);
            playersInRadius.Clear();
            PlayerTool.getPlayersInRadius(fromPoint, sqrRadius, playersInRadius);
            for (int i = 0; i < playersInRadius.Count; i++)
            {
                Player player2 = playersInRadius[i];
                if (player2.channel.owner.playerID.steamID == base.owner || player2.quests.isMemberOfGroup(base.group) || player2.life.isDead || player2.animator.gesture == EPlayerGesture.ARREST_START || (player2.movement.isSafe && player2.movement.isSafeInfo.noWeapons) || !player2.movement.canAddSimulationResultsToUpdates || (player != null && player2.animator.gesture == EPlayerGesture.SURRENDER_START) || (sentryMode == ESentryMode.FRIENDLY && !(Time.realtimeSinceStartup - player2.equipment.lastPunching < 2f) && (!player2.equipment.HasValidUseable || player2.equipment.asset == null || !player2.equipment.asset.shouldFriendlySentryTargetUser)))
                {
                    continue;
                }
                float sqrMagnitude = (player2.look.aim.position - fromPoint).sqrMagnitude;
                if (player2 != targetPlayer && sqrMagnitude > num5)
                {
                    continue;
                }
                Vector3 vector = player2.look.aim.position - fromPoint;
                float magnitude = vector.magnitude;
                Vector3 vector2 = vector / magnitude;
                if (player2 != targetPlayer && Vector3.Dot(vector2, aimTransform.forward) < 0.5f)
                {
                    continue;
                }
                if (magnitude > 0.025f)
                {
                    Physics.Raycast(new Ray(fromPoint, vector2), out var hitInfo, magnitude - 0.025f, RayMasks.BLOCK_SENTRY);
                    if (hitInfo.transform != null && hitInfo.transform != base.transform)
                    {
                        continue;
                    }
                    Physics.Raycast(new Ray(fromPoint + vector2 * (magnitude - 0.025f), -vector2), out hitInfo, magnitude - 0.025f, RayMasks.DAMAGE_SERVER);
                    if (hitInfo.transform != null && hitInfo.transform != base.transform)
                    {
                        continue;
                    }
                }
                num5 = sqrMagnitude;
                player = player2;
                flag = true;
            }
        }
        float sqrRadius2 = ((!flag && targetZombie != null) ? num4 : num5);
        zombiesInRadius.Clear();
        ZombieManager.getZombiesInRadius(fromPoint, sqrRadius2, zombiesInRadius);
        for (int j = 0; j < zombiesInRadius.Count; j++)
        {
            Zombie zombie2 = zombiesInRadius[j];
            if (zombie2.isDead || !zombie2.isHunting)
            {
                continue;
            }
            Vector3 position = zombie2.transform.position;
            switch (zombie2.speciality)
            {
            case EZombieSpeciality.CRAWLER:
                position += new Vector3(0f, 0.25f, 0f);
                break;
            case EZombieSpeciality.MEGA:
                position += new Vector3(0f, 2.625f, 0f);
                break;
            case EZombieSpeciality.NORMAL:
                position += new Vector3(0f, 1.75f, 0f);
                break;
            case EZombieSpeciality.SPRINTER:
                position += new Vector3(0f, 1f, 0f);
                break;
            }
            float sqrMagnitude2 = (position - fromPoint).sqrMagnitude;
            if (zombie2 != targetZombie && sqrMagnitude2 > num5)
            {
                continue;
            }
            Vector3 vector3 = position - fromPoint;
            float magnitude2 = vector3.magnitude;
            Vector3 vector4 = vector3 / magnitude2;
            if (zombie2 != targetZombie && Vector3.Dot(vector4, aimTransform.forward) < 0.5f)
            {
                continue;
            }
            if (magnitude2 > 0.025f)
            {
                Physics.Raycast(new Ray(fromPoint, vector4), out var hitInfo2, magnitude2 - 0.025f, RayMasks.BLOCK_SENTRY);
                if (hitInfo2.transform != null && hitInfo2.transform != base.transform)
                {
                    continue;
                }
                Physics.Raycast(new Ray(fromPoint + vector4 * (magnitude2 - 0.025f), -vector4), out hitInfo2, magnitude2 - 0.025f, RayMasks.DAMAGE_SERVER);
                if (hitInfo2.transform != null && hitInfo2.transform != base.transform)
                {
                    continue;
                }
            }
            num5 = sqrMagnitude2;
            player = null;
            zombie = zombie2;
            flag = true;
        }
        float sqrRadius3 = ((!flag && targetAnimal != null) ? num4 : num5);
        animalsInRadius.Clear();
        AnimalManager.getAnimalsInRadius(fromPoint, sqrRadius3, animalsInRadius);
        for (int k = 0; k < animalsInRadius.Count; k++)
        {
            Animal animal2 = animalsInRadius[k];
            if (animal2.isDead)
            {
                continue;
            }
            Vector3 position2 = animal2.transform.position;
            float sqrMagnitude3 = (position2 - fromPoint).sqrMagnitude;
            if (animal2 != targetAnimal && sqrMagnitude3 > num5)
            {
                continue;
            }
            Vector3 vector5 = position2 - fromPoint;
            float magnitude3 = vector5.magnitude;
            Vector3 vector6 = vector5 / magnitude3;
            if (animal2 != targetAnimal && Vector3.Dot(vector6, aimTransform.forward) < 0.5f)
            {
                continue;
            }
            if (magnitude3 > 0.025f)
            {
                Physics.Raycast(new Ray(fromPoint, vector6), out var hitInfo3, magnitude3 - 0.025f, RayMasks.BLOCK_SENTRY);
                if (hitInfo3.transform != null && hitInfo3.transform != base.transform)
                {
                    continue;
                }
                Physics.Raycast(new Ray(fromPoint + vector6 * (magnitude3 - 0.025f), -vector6), out hitInfo3, magnitude3 - 0.025f, RayMasks.DAMAGE_SERVER);
                if (hitInfo3.transform != null && hitInfo3.transform != base.transform)
                {
                    continue;
                }
            }
            num5 = sqrMagnitude3;
            player = null;
            zombie = null;
            animal = animal2;
            flag = true;
        }
        if (Provider.isPvP && sentryMode == ESentryMode.HOSTILE)
        {
            float sqrRadius4 = ((!flag && targetVehicle != null) ? num4 : num5);
            vehiclesInRadius.Clear();
            VehicleManager.getVehiclesInRadius(fromPoint, sqrRadius4, vehiclesInRadius);
            for (int l = 0; l < vehiclesInRadius.Count; l++)
            {
                InteractableVehicle interactableVehicle2 = vehiclesInRadius[l];
                if (interactableVehicle2.isDead || interactableVehicle2.isInsideSafezone || interactableVehicle2.IsFriendlyToSentry(this) || !interactableVehicle2.anySeatsOccupied)
                {
                    continue;
                }
                Vector3 sentryTargetingPoint = interactableVehicle2.GetSentryTargetingPoint();
                float sqrMagnitude4 = (sentryTargetingPoint - fromPoint).sqrMagnitude;
                if (interactableVehicle2 != targetVehicle && sqrMagnitude4 > num5)
                {
                    continue;
                }
                Vector3 vector7 = sentryTargetingPoint - fromPoint;
                float magnitude4 = vector7.magnitude;
                Vector3 vector8 = vector7 / magnitude4;
                if (interactableVehicle2 != targetVehicle && Vector3.Dot(vector8, aimTransform.forward) < 0.5f)
                {
                    continue;
                }
                if (magnitude4 > 0.025f)
                {
                    Physics.Raycast(new Ray(fromPoint, vector8), out var hitInfo4, magnitude4 - 0.025f, RayMasks.BLOCK_SENTRY);
                    if (hitInfo4.transform != null && hitInfo4.transform != base.transform && !hitInfo4.transform.IsChildOf(interactableVehicle2.transform))
                    {
                        continue;
                    }
                    Physics.Raycast(new Ray(fromPoint + vector8 * (magnitude4 - 0.025f), -vector8), out hitInfo4, magnitude4 - 0.025f, RayMasks.DAMAGE_SERVER);
                    if (hitInfo4.transform != null && hitInfo4.transform != base.transform && !hitInfo4.transform.IsChildOf(interactableVehicle2.transform))
                    {
                        continue;
                    }
                }
                num5 = sqrMagnitude4;
                interactableVehicle = interactableVehicle2;
            }
        }
        if (player != targetPlayer || zombie != targetZombie || animal != targetAnimal || interactableVehicle != targetVehicle)
        {
            targetPlayer = player;
            targetZombie = zombie;
            targetAnimal = animal;
            targetVehicle = interactableVehicle;
            lastFire = Time.timeAsDouble + 0.1;
        }
    }
}
