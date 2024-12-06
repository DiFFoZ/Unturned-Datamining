using System;
using System.Collections.Generic;
using SDG.Framework.Water;
using SDG.NetTransport;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unturned.UnityEx;

namespace SDG.Unturned;

public class UseableGun : Useable
{
    public delegate void ChangeAttachmentRequestHandler(PlayerEquipment equipment, UseableGun gun, Item oldItem, ItemJar newItem, ref bool shouldAllow);

    public delegate void BulletSpawnedHandler(UseableGun gun, BulletInfo bullet);

    public delegate void BulletHitHandler(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow);

    public delegate void ProjectileSpawnedHandler(UseableGun sender, GameObject projectile);

    private class DistanceMarker
    {
        public bool isActive;

        public float distance;

        public Transform transform;

        public LineRenderer lineComponent;

        public TextMeshPro textComponent;
    }

    private static readonly PlayerDamageMultiplier DAMAGE_PLAYER_MULTIPLIER = new PlayerDamageMultiplier(40f, 0.6f, 0.6f, 0.8f, 1.1f);

    private static readonly ZombieDamageMultiplier DAMAGE_ZOMBIE_MULTIPLIER = new ZombieDamageMultiplier(40f, 0.3f, 0.3f, 0.6f, 1.1f);

    private static readonly AnimalDamageMultiplier DAMAGE_ANIMAL_MULTIPLIER = new AnimalDamageMultiplier(40f, 0.3f, 0.6f, 1.1f);

    private static readonly float SHAKE_CROUCH = 0.85f;

    private static readonly float SHAKE_PRONE = 0.7f;

    private static readonly float SWAY_CROUCH = 0.85f;

    private static readonly float SWAY_PRONE = 0.7f;

    private Local localization;

    private Bundle icons;

    private SleekButtonIcon sightButton;

    private SleekJars sightJars;

    private SleekButtonIcon tacticalButton;

    private SleekJars tacticalJars;

    private SleekButtonIcon gripButton;

    private SleekJars gripJars;

    private SleekButtonIcon barrelButton;

    private ISleekLabel barrelQualityLabel;

    private ISleekImage barrelQualityImage;

    private SleekJars barrelJars;

    private SleekButtonIcon magazineButton;

    private ISleekLabel magazineQualityLabel;

    private ISleekImage magazineQualityImage;

    private SleekJars magazineJars;

    private ISleekLabel rangeLabel;

    private ISleekBox infoBox;

    private ISleekLabel ammoLabel;

    private ISleekLabel firemodeLabel;

    private ISleekLabel attachLabel;

    internal Attachments firstAttachments;

    private ParticleSystem firstShellEmitter;

    private ParticleSystem firstMuzzleEmitter;

    private Transform firstFakeLight;

    private Transform firstFakeLight_0;

    private Transform firstFakeLight_1;

    private Attachments thirdAttachments;

    private ParticleSystem thirdShellEmitter;

    private ParticleSystemRenderer thirdShellRenderer;

    private ParticleSystem thirdMuzzleEmitter;

    private float minigunSpeed;

    private float minigunDistance;

    private Transform firstMinigunBarrel;

    private Transform thirdMinigunBarrel;

    private Text firstAmmoCounter;

    private Text thirdAmmoCounter;

    private EffectAsset currentTracerEffectAsset;

    private ParticleSystem tracerEmitter;

    private AudioSource gunshotAudioSource;

    private AudioSource whir;

    /// <summary>
    /// reticuleHook.localPosition after instantiation, or zero if null.
    /// </summary>
    private Vector3 originalReticuleHookLocalPosition;

    private bool isShooting;

    /// <summary>
    /// True if startPrimary was called this simulation frame.
    /// Allows gun to shoot even if stopPrimary is called immediately afterwards.
    /// </summary>
    private bool wasTriggerJustPulled;

    private bool isJabbing;

    private bool isMinigunSpinning;

    private bool isSprinting;

    private bool isReloading;

    private bool isHammering;

    private bool isAttaching;

    private bool isUnjamming;

    private float lastShot;

    private float lastRechamber;

    private uint lastFire;

    private uint lastJab;

    private bool isFired;

    private int bursts;

    /// <summary>
    /// Remaining calls to tock before firing.
    /// </summary>
    private int fireDelayCounter;

    private int aimAccuracy;

    private uint steadyAccuracy;

    private bool canSteady;

    private float swayTime;

    private List<BulletInfo> bullets;

    private float startedReload;

    private float startedHammer;

    private float startedUnjammingChamber;

    private float reloadTime;

    private float hammerTime;

    private float unjamChamberDuration;

    private bool needsHammer;

    private bool needsRechamber;

    private bool needsEject;

    private bool needsUnload;

    private bool needsUnplace;

    private bool needsReplace;

    /// <summary>
    /// Is the tactical attachment toggle on?
    /// e.g. True when the laser is enabled.
    /// </summary>
    private bool interact;

    private byte ammo;

    private EFiremode firemode;

    private List<InventorySearch> sightSearch;

    private List<InventorySearch> tacticalSearch;

    private List<InventorySearch> gripSearch;

    private List<InventorySearch> barrelSearch;

    private List<InventorySearch> magazineSearch;

    /// <summary>
    /// Factor e.g. 2 is a 2x multiplier.
    /// Prior to 2022-04-11 this was the target field of view. (90/fov)
    /// </summary>
    private float firstPersonZoomFactor;

    /// <summary>
    /// Zoom multiplier in third-person.
    /// </summary>
    private float thirdPersonZoomFactor = 1.25f;

    /// <summary>
    /// Whether main camera field of view should zoom without scope camera / scope overlay.
    /// </summary>
    private bool shouldZoomUsingEyes;

    private float crosshair;

    private GameObject laserGameObject;

    private Transform laserTransform;

    private Material laserMaterial;

    private bool wasLaser;

    private bool wasLight;

    private bool wasRange;

    private bool wasBayonet;

    private bool inRange;

    private bool fireTacticalInput;

    private RaycastHit contact;

    private UseableGunEventHook firstEventComponent;

    private UseableGunEventHook thirdEventComponent;

    private UseableGunEventHook characterEventComponent;

    private static readonly ServerInstanceMethod<EFiremode> SendChangeFiremode = ServerInstanceMethod<EFiremode>.Get(typeof(UseableGun), "ReceiveChangeFiremode");

    private static readonly ClientInstanceMethod<Vector3, Vector3, ushort, ushort> SendPlayProject = ClientInstanceMethod<Vector3, Vector3, ushort, ushort>.Get(typeof(UseableGun), "ReceivePlayProject");

    private static MasterBundleReference<OneShotAudioDefinition> bulletFlybyAudioRef = new MasterBundleReference<OneShotAudioDefinition>("core.masterbundle", "Effects/Guns/BulletFlyby.asset");

    private static readonly ClientInstanceMethod SendPlayShoot = ClientInstanceMethod.Get(typeof(UseableGun), "ReceivePlayShoot");

    private static MasterBundleReference<AudioClip> jabClipRef = new MasterBundleReference<AudioClip>("core.masterbundle", "Sounds/MeleeAttack_01.mp3");

    internal const float BALLISTICS_DELTA_TIME = 0.02f;

    private static readonly ServerInstanceMethod<byte, byte, byte, byte[]> SendAttachSight = ServerInstanceMethod<byte, byte, byte, byte[]>.Get(typeof(UseableGun), "ReceiveAttachSight");

    private static readonly ServerInstanceMethod<byte, byte, byte, byte[]> SendAttachTactical = ServerInstanceMethod<byte, byte, byte, byte[]>.Get(typeof(UseableGun), "ReceiveAttachTactical");

    private static readonly ServerInstanceMethod<byte, byte, byte, byte[]> SendAttachGrip = ServerInstanceMethod<byte, byte, byte, byte[]>.Get(typeof(UseableGun), "ReceiveAttachGrip");

    private static readonly ServerInstanceMethod<byte, byte, byte, byte[]> SendAttachBarrel = ServerInstanceMethod<byte, byte, byte, byte[]>.Get(typeof(UseableGun), "ReceiveAttachBarrel");

    private static readonly ServerInstanceMethod<byte, byte, byte, byte[]> SendAttachMagazine = ServerInstanceMethod<byte, byte, byte, byte[]>.Get(typeof(UseableGun), "ReceiveAttachMagazine");

    private static readonly ClientInstanceMethod<bool> SendPlayReload = ClientInstanceMethod<bool>.Get(typeof(UseableGun), "ReceivePlayReload");

    private static readonly ClientInstanceMethod<byte> SendPlayChamberJammed = ClientInstanceMethod<byte>.Get(typeof(UseableGun), "ReceivePlayChamberJammed");

    private static readonly ClientInstanceMethod SendPlayAimStart = ClientInstanceMethod.Get(typeof(UseableGun), "ReceivePlayAimStart");

    private static readonly ClientInstanceMethod SendPlayAimStop = ClientInstanceMethod.Get(typeof(UseableGun), "ReceivePlayAimStop");

    private int maxAimingAccuracy;

    private float maxAimingAccuracyReciprocal;

    private List<DistanceMarker> scopeDistanceMarkers;

    private static Material scopeDistanceMarkerMaterial;

    internal const float DEFAULT_THIRD_PERSON_ZOOM_FACTOR = 1.25f;

    public bool isAiming { get; protected set; }

    /// <summary>
    /// Should stat modifiers from the current tactical attachment be used?
    /// </summary>
    private bool shouldEnableTacticalStats
    {
        get
        {
            ItemTacticalAsset tacticalAsset = thirdAttachments.tacticalAsset;
            if (tacticalAsset != null)
            {
                if (tacticalAsset.isLaser || tacticalAsset.isLight || tacticalAsset.isRangefinder)
                {
                    return interact;
                }
                return true;
            }
            return false;
        }
    }

    public ItemGunAsset equippedGunAsset => base.player.equipment.asset as ItemGunAsset;

    public override bool canInspect
    {
        get
        {
            if (!isShooting && !isReloading && !isHammering && !isUnjamming && !isSprinting && !isAttaching && !isAiming)
            {
                return !needsRechamber;
            }
            return false;
        }
    }

    private bool CanDamageInvulnerableEntities
    {
        get
        {
            if (((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable)
            {
                return true;
            }
            if ((thirdAttachments?.barrelAsset?.CanDamageInvulernableEntities).GetValueOrDefault())
            {
                return true;
            }
            if ((thirdAttachments?.tacticalAsset?.CanDamageInvulernableEntities).GetValueOrDefault())
            {
                return true;
            }
            if ((thirdAttachments?.gripAsset?.CanDamageInvulernableEntities).GetValueOrDefault())
            {
                return true;
            }
            if ((thirdAttachments?.sightAsset?.CanDamageInvulernableEntities).GetValueOrDefault())
            {
                return true;
            }
            if ((thirdAttachments?.magazineAsset?.CanDamageInvulernableEntities).GetValueOrDefault())
            {
                return true;
            }
            return false;
        }
    }

    public static event ChangeAttachmentRequestHandler onChangeSightRequested;

    public static event ChangeAttachmentRequestHandler onChangeTacticalRequested;

    public static event ChangeAttachmentRequestHandler onChangeGripRequested;

    public static event ChangeAttachmentRequestHandler onChangeBarrelRequested;

    public static event ChangeAttachmentRequestHandler onChangeMagazineRequested;

    /// <summary>
    /// Plugin-only event when bullet is fired on server.
    /// </summary>
    public static event BulletSpawnedHandler onBulletSpawned;

    /// <summary>
    /// Plugin-only event when bullet hit is received from client.
    /// </summary>
    public static event BulletHitHandler onBulletHit;

    /// <summary>
    /// Plugin-only event when projectile is spawned on server.
    /// </summary>
    public static event ProjectileSpawnedHandler onProjectileSpawned;

    public static event Action<UseableGun> OnReloading_Global;

    public static event Action<UseableGun> OnAimingChanged_Global;

    /// <returns>Whether plugin allowed attachment.</returns>
    private bool changeAttachmentRequested(ChangeAttachmentRequestHandler handler, Item oldItem, ItemJar newItem)
    {
        if (handler != null)
        {
            bool shouldAllow = true;
            handler(base.player.equipment, this, oldItem, newItem, ref shouldAllow);
            return shouldAllow;
        }
        return true;
    }

    private bool changeSightRequested(Item oldItem, ItemJar newItem)
    {
        return changeAttachmentRequested(UseableGun.onChangeSightRequested, oldItem, newItem);
    }

    private bool changeTacticalRequested(Item oldItem, ItemJar newItem)
    {
        return changeAttachmentRequested(UseableGun.onChangeTacticalRequested, oldItem, newItem);
    }

    private bool changeGripRequested(Item oldItem, ItemJar newItem)
    {
        return changeAttachmentRequested(UseableGun.onChangeGripRequested, oldItem, newItem);
    }

    private bool changeBarrelRequested(Item oldItem, ItemJar newItem)
    {
        return changeAttachmentRequested(UseableGun.onChangeBarrelRequested, oldItem, newItem);
    }

    private bool changeMagazineRequested(Item oldItem, ItemJar newItem)
    {
        return changeAttachmentRequested(UseableGun.onChangeMagazineRequested, oldItem, newItem);
    }

    protected VehicleTurretEventHook GetVehicleTurretEventHook()
    {
        if (base.player.equipment.isTurret)
        {
            return base.player.movement.getVehicleSeat()?.turretEventHook;
        }
        return null;
    }

    [Obsolete]
    public void askFiremode(CSteamID steamID, byte id)
    {
        ReceiveChangeFiremode((EFiremode)id);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askFiremode")]
    public void ReceiveChangeFiremode(EFiremode newFiremode)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null)
        {
            return;
        }
        switch (newFiremode)
        {
        case EFiremode.SAFETY:
            if (equippedGunAsset.hasSafety)
            {
                firemode = newFiremode;
            }
            break;
        case EFiremode.SEMI:
            if (equippedGunAsset.hasSemi)
            {
                firemode = newFiremode;
            }
            break;
        case EFiremode.AUTO:
            if (equippedGunAsset.hasAuto)
            {
                firemode = newFiremode;
            }
            break;
        case EFiremode.BURST:
            if (equippedGunAsset.hasBurst)
            {
                firemode = newFiremode;
            }
            break;
        }
        base.player.equipment.state[11] = (byte)firemode;
        base.player.equipment.sendUpdateState();
        EffectManager.TriggerFiremodeEffect(base.transform.position);
    }

    public void askInteractGun()
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || thirdAttachments.tacticalAsset == null)
        {
            return;
        }
        if (thirdAttachments.tacticalAsset.isMelee)
        {
            if (!isSprinting && (!base.player.movement.isSafe || !base.player.movement.isSafeInfo.noWeapons) && firemode != 0)
            {
                isJabbing = true;
            }
        }
        else
        {
            interact = !interact;
            base.player.equipment.state[12] = (byte)(interact ? 1u : 0u);
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    /// <summary>
    /// Original barrel and magazine assets are supplied because they may have already been deleted. Barrel is only
    /// valid if quality was greater than zero.
    /// </summary>
    private void project(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset)
    {
        if (gunshotAudioSource != null)
        {
            playGunshot();
        }
        if (barrelAsset == null || !barrelAsset.isBraked)
        {
            if (firstMuzzleEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
            {
                firstMuzzleEmitter.Emit(1);
                Light component = firstMuzzleEmitter.GetComponent<Light>();
                if (component != null)
                {
                    component.enabled = true;
                }
                if (firstFakeLight != null)
                {
                    component = firstFakeLight.GetComponent<Light>();
                    if (component != null)
                    {
                        component.enabled = true;
                    }
                }
            }
            if (thirdMuzzleEmitter != null && (!base.channel.IsLocalPlayer || base.player.look.perspective == EPlayerPerspective.THIRD || equippedGunAsset.isTurret))
            {
                thirdMuzzleEmitter.Emit(1);
                Light component2 = thirdMuzzleEmitter.GetComponent<Light>();
                if (component2 != null)
                {
                    component2.enabled = true;
                }
            }
        }
        float num = 1f;
        float num2 = 1f;
        float num3 = 1f;
        if (magazineAsset != null)
        {
            num *= magazineAsset.projectileDamageMultiplier;
            num2 *= magazineAsset.projectileBlastRadiusMultiplier;
            num3 *= magazineAsset.projectileLaunchForceMultiplier;
        }
        Transform transform = UnityEngine.Object.Instantiate(equippedGunAsset.projectile).transform;
        transform.name = "Projectile";
        EffectManager.RegisterDebris(transform.gameObject);
        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        Rigidbody component3 = transform.GetComponent<Rigidbody>();
        if (component3 != null)
        {
            component3.AddForce(direction * equippedGunAsset.ballisticForce * num3);
            component3.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        if (base.channel.IsLocalPlayer && transform.GetComponent<AudioSource>() != null)
        {
            transform.GetComponent<AudioSource>().maxDistance = 512f;
        }
        if (equippedGunAsset.action == EAction.Bolt || equippedGunAsset.action == EAction.Pump)
        {
            needsRechamber = true;
        }
        Rocket rocket = transform.gameObject.AddComponent<Rocket>();
        rocket.ignoreTransform = base.transform;
        if (Provider.isServer)
        {
            rocket.killer = base.channel.owner.playerID.steamID;
            rocket.range = equippedGunAsset.range * num2;
            rocket.playerDamage = equippedGunAsset.playerDamageMultiplier.damage * num;
            rocket.zombieDamage = equippedGunAsset.zombieDamageMultiplier.damage * num;
            rocket.animalDamage = equippedGunAsset.animalDamageMultiplier.damage * num;
            rocket.barricadeDamage = equippedGunAsset.barricadeDamage * num;
            rocket.structureDamage = equippedGunAsset.structureDamage * num;
            rocket.vehicleDamage = equippedGunAsset.vehicleDamage * num;
            rocket.resourceDamage = equippedGunAsset.resourceDamage * num;
            rocket.objectDamage = equippedGunAsset.objectDamage * num;
            if (magazineAsset != null && !magazineAsset.IsExplosionEffectRefNull())
            {
                rocket.explosionEffectGuid = magazineAsset.explosionEffectGuid;
                rocket.explosion = magazineAsset.explosion;
            }
            else
            {
                rocket.explosionEffectGuid = equippedGunAsset.projectileExplosionEffectGuid;
                rocket.explosion = equippedGunAsset.explosion;
            }
            rocket.penetrateBuildables = equippedGunAsset.projectilePenetrateBuildables;
            rocket.explosionLaunchSpeed = equippedGunAsset.projectileExplosionLaunchSpeed;
            rocket.ragdollEffect = base.player.equipment.getUseableRagdollEffect();
        }
        UnityEngine.Object.Destroy(transform.gameObject, equippedGunAsset.projectileLifespan);
        lastShot = Time.realtimeSinceStartup;
        UseableGun.onProjectileSpawned?.Invoke(this, transform.gameObject);
        InvokeModHookShotFiredEvents();
    }

    [Obsolete]
    public void askProject(CSteamID steamID, Vector3 origin, Vector3 direction, ushort barrelId, ushort magazineId)
    {
        ReceivePlayProject(origin, direction, barrelId, magazineId);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askProject")]
    public void ReceivePlayProject(Vector3 origin, Vector3 direction, ushort barrelId, ushort magazineId)
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            ItemBarrelAsset barrelAsset = Assets.find(EAssetType.ITEM, barrelId) as ItemBarrelAsset;
            ItemMagazineAsset magazineAsset = Assets.find(EAssetType.ITEM, magazineId) as ItemMagazineAsset;
            project(origin, direction, barrelAsset, magazineAsset);
        }
    }

    private void trace(Vector3 pos, Vector3 dir)
    {
        if (!(tracerEmitter == null) && (!(thirdAttachments.barrelModel != null) || !thirdAttachments.barrelAsset.isBraked || base.player.equipment.state[16] <= 0))
        {
            tracerEmitter.transform.position = pos;
            tracerEmitter.transform.rotation = Quaternion.LookRotation(dir);
            tracerEmitter.Emit(1);
        }
    }

    private void PlayFlybyAudio(Vector3 origin, Vector3 direction, float range)
    {
        if (MainCamera.instance == null || (base.channel.IsLocalPlayer && !base.player.look.IsLocallyUsingFreecam))
        {
            return;
        }
        Vector3 position = MainCamera.instance.transform.position;
        float num = Vector3.Dot(position - origin, direction);
        if (!(num > 0f) || !(num < range))
        {
            return;
        }
        Vector3 vector = origin + direction * num;
        if ((vector - position).sqrMagnitude < 25f)
        {
            OneShotAudioDefinition oneShotAudioDefinition = bulletFlybyAudioRef.loadAsset();
            if (oneShotAudioDefinition == null)
            {
                UnturnedLog.warn("Missing built-in bullet flyby audio");
                return;
            }
            AudioClip randomClip = oneShotAudioDefinition.GetRandomClip();
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(vector, randomClip);
            oneShotAudioParameters.minDistance = 0f;
            oneShotAudioParameters.maxDistance = 5f;
            oneShotAudioParameters.RandomizePitch(oneShotAudioDefinition.minPitch, oneShotAudioDefinition.maxPitch);
            oneShotAudioParameters.Play();
        }
    }

    private void playGunshot()
    {
        AudioClip clip = equippedGunAsset.shoot;
        float num = 1f;
        float num2 = equippedGunAsset.gunshotRolloffDistance;
        if (thirdAttachments.barrelAsset != null && base.player.equipment.state[16] > 0)
        {
            if (thirdAttachments.barrelAsset.shoot != null)
            {
                clip = thirdAttachments.barrelAsset.shoot;
            }
            num *= thirdAttachments.barrelAsset.volume;
            num2 *= thirdAttachments.barrelAsset.gunshotRolloffDistanceMultiplier;
        }
        gunshotAudioSource.clip = clip;
        gunshotAudioSource.volume = num;
        gunshotAudioSource.maxDistance = num2;
        gunshotAudioSource.pitch = UnityEngine.Random.Range(0.975f, 1.025f);
        if (gunshotAudioSource.clip != null)
        {
            gunshotAudioSource.PlayOneShot(gunshotAudioSource.clip);
        }
    }

    private void shoot()
    {
        if (gunshotAudioSource != null)
        {
            playGunshot();
        }
        if (equippedGunAsset.action == EAction.Trigger || equippedGunAsset.action == EAction.Minigun)
        {
            if (firstShellEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
            {
                firstShellEmitter.Emit(1);
            }
            if (thirdShellEmitter != null)
            {
                thirdShellEmitter.Emit(1);
            }
        }
        if (thirdAttachments.barrelModel == null || !thirdAttachments.barrelAsset.isBraked || base.player.equipment.state[16] == 0)
        {
            if (firstMuzzleEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
            {
                firstMuzzleEmitter.Emit(1);
                firstMuzzleEmitter.GetComponent<Light>().enabled = true;
                if (firstFakeLight != null)
                {
                    firstFakeLight.GetComponent<Light>().enabled = true;
                }
            }
            if (thirdMuzzleEmitter != null && (!base.channel.IsLocalPlayer || base.player.look.perspective == EPlayerPerspective.THIRD || equippedGunAsset.isTurret))
            {
                thirdMuzzleEmitter.Emit(1);
                thirdMuzzleEmitter.GetComponent<Light>().enabled = true;
            }
        }
        if (!base.channel.IsLocalPlayer)
        {
            if (equippedGunAsset.range < 32f)
            {
                trace(base.player.look.aim.position + base.player.look.aim.forward * 32f, base.player.look.aim.forward);
            }
            else
            {
                trace(base.player.look.aim.position + base.player.look.aim.forward * UnityEngine.Random.Range(32f, Mathf.Min(64f, equippedGunAsset.range)), base.player.look.aim.forward);
            }
            PlayFlybyAudio(base.player.look.aim.position, base.player.look.aim.forward, equippedGunAsset.range);
        }
        lastShot = Time.realtimeSinceStartup;
        if (equippedGunAsset.action == EAction.Bolt || equippedGunAsset.action == EAction.Pump)
        {
            needsRechamber = true;
        }
        if (thirdAttachments.barrelAsset != null && thirdAttachments.barrelAsset.durability > 0)
        {
            if (thirdAttachments.barrelAsset.durability > base.player.equipment.state[16])
            {
                base.player.equipment.state[16] = 0;
            }
            else
            {
                base.player.equipment.state[16] -= thirdAttachments.barrelAsset.durability;
            }
            if (base.channel.IsLocalPlayer || Provider.isServer)
            {
                base.player.equipment.updateState();
            }
        }
        InvokeModHookShotFiredEvents();
    }

    [Obsolete]
    public void askShoot(CSteamID steamID)
    {
        ReceivePlayShoot();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askShoot")]
    public void ReceivePlayShoot()
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            shoot();
        }
    }

    /// <summary>
    /// Called on server and owning client.
    /// </summary>
    private void fire()
    {
        float num = (float)(int)base.player.equipment.quality / 100f;
        if (!equippedGunAsset.infiniteAmmo)
        {
            if (ammo < equippedGunAsset.ammoPerShot)
            {
                throw new Exception("Insufficient ammo");
            }
            ammo -= equippedGunAsset.ammoPerShot;
            if (equippedGunAsset.action != EAction.String)
            {
                base.player.equipment.state[10] = ammo;
                base.player.equipment.updateState();
            }
        }
        if (base.channel.IsLocalPlayer && ammo < equippedGunAsset.ammoPerShot)
        {
            PlayerUI.message(EPlayerMessage.RELOAD, "");
        }
        if (!isAiming)
        {
            base.player.equipment.uninspect();
        }
        if (Provider.isServer)
        {
            SendPlayShoot.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsWithinSphereExcludingOwner(base.transform.position, EffectManager.INSANE));
            lastShot = Time.realtimeSinceStartup;
            if (!base.channel.IsLocalPlayer)
            {
                InvokeModHookShotFiredEvents();
            }
            if (equippedGunAsset.action == EAction.Bolt || equippedGunAsset.action == EAction.Pump)
            {
                needsRechamber = true;
            }
            if (thirdAttachments.barrelAsset == null || !thirdAttachments.barrelAsset.isSilenced || base.player.equipment.state[16] == 0)
            {
                AlertTool.alert(base.transform.position, equippedGunAsset.alertRadius);
            }
            if (Provider.modeConfigData.Items.ShouldWeaponTakeDamage && base.player.equipment.quality > 0 && UnityEngine.Random.value < ((ItemWeaponAsset)base.player.equipment.asset).durability)
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
        }
        if (base.channel.IsLocalPlayer)
        {
            if (!base.player.look.IsLocallyUsingFreecam && base.player.look.perspective == EPlayerPerspective.THIRD)
            {
                Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out var hitInfo, 512f, RayMasks.DAMAGE_CLIENT);
                if (hitInfo.transform != null)
                {
                    if (Vector3.Dot(hitInfo.point - base.player.look.aim.position, MainCamera.instance.transform.forward) > 0f)
                    {
                        base.player.look.aim.rotation = Quaternion.LookRotation(hitInfo.point - base.player.look.aim.position);
                    }
                }
                else
                {
                    base.player.look.aim.rotation = Quaternion.LookRotation(MainCamera.instance.transform.position + MainCamera.instance.transform.forward * 512f - base.player.look.aim.position);
                }
            }
            if (equippedGunAsset.projectile == null)
            {
                Quaternion rotation = base.player.look.aim.rotation;
                if (base.player.look.perspective == EPlayerPerspective.FIRST)
                {
                    Quaternion quaternion = Quaternion.Euler(base.player.animator.recoilViewmodelCameraRotation.currentPosition);
                    rotation *= quaternion;
                }
                float halfAngleRadians = CalculateSpreadAngleRadians(num, GetSimulationAimAlpha());
                byte b = (byte)((thirdAttachments.magazineAsset == null) ? 1 : thirdAttachments.magazineAsset.pellets);
                float gravityMultiplier = CalculateBulletGravityMultiplier();
                for (byte b2 = 0; b2 < b; b2++)
                {
                    BulletInfo bulletInfo = new BulletInfo();
                    bulletInfo.origin = base.player.look.aim.position;
                    bulletInfo.position = bulletInfo.origin;
                    Vector3 vector = rotation * RandomEx.GetRandomForwardVectorInCone(halfAngleRadians);
                    bulletInfo.velocity = vector * equippedGunAsset.muzzleVelocity;
                    bulletInfo.pellet = b2;
                    bulletInfo.quality = num;
                    bulletInfo.barrelAsset = thirdAttachments.barrelAsset;
                    bulletInfo.magazineAsset = thirdAttachments.magazineAsset;
                    bulletInfo.gravityMultiplier = gravityMultiplier;
                    bullets.Add(bulletInfo);
                    if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out int data))
                    {
                        Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", data + 1);
                    }
                }
            }
            else
            {
                Vector3 forward = base.player.look.aim.forward;
                RaycastInfo raycastInfo = DamageTool.raycast(new Ray(base.player.look.aim.position, forward), 512f, RayMasks.DAMAGE_CLIENT, base.player);
                if (raycastInfo.transform != null)
                {
                    base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Gun);
                }
                Vector3 position = base.player.look.aim.position;
                if (!Physics.Raycast(new Ray(position, forward), out var _, 1f, RayMasks.DAMAGE_SERVER))
                {
                    position += forward;
                }
                project(position, forward, thirdAttachments.barrelAsset, thirdAttachments.magazineAsset);
                if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out int data2))
                {
                    Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", data2 + 1);
                }
            }
            float num2 = UnityEngine.Random.Range(equippedGunAsset.recoilMin_x, equippedGunAsset.recoilMax_x) * ((num < 0.5f) ? (1f + (1f - num * 2f)) : 1f);
            float num3 = UnityEngine.Random.Range(equippedGunAsset.recoilMin_y, equippedGunAsset.recoilMax_y) * ((num < 0.5f) ? (1f + (1f - num * 2f)) : 1f);
            float num4 = UnityEngine.Random.Range(equippedGunAsset.shakeMin_x, equippedGunAsset.shakeMax_x);
            float num5 = UnityEngine.Random.Range(equippedGunAsset.shakeMin_y, equippedGunAsset.shakeMax_y);
            float num6 = UnityEngine.Random.Range(equippedGunAsset.shakeMin_z, equippedGunAsset.shakeMax_z);
            num2 *= 1f - base.player.skills.mastery(0, 1) * 0.5f;
            num3 *= 1f - base.player.skills.mastery(0, 1) * 0.5f;
            if (isAiming)
            {
                num2 *= equippedGunAsset.aimingRecoilMultiplier;
                num3 *= equippedGunAsset.aimingRecoilMultiplier;
            }
            if (thirdAttachments.sightAsset != null)
            {
                if (isAiming)
                {
                    num2 *= thirdAttachments.sightAsset.aimingRecoilMultiplier;
                    num3 *= thirdAttachments.sightAsset.aimingRecoilMultiplier;
                }
                num2 *= thirdAttachments.sightAsset.recoil_x;
                num3 *= thirdAttachments.sightAsset.recoil_y;
                num4 *= thirdAttachments.sightAsset.shake;
                num5 *= thirdAttachments.sightAsset.shake;
                num6 *= thirdAttachments.sightAsset.shake;
            }
            if (thirdAttachments.tacticalAsset != null && shouldEnableTacticalStats)
            {
                if (isAiming)
                {
                    num2 *= thirdAttachments.tacticalAsset.aimingRecoilMultiplier;
                    num3 *= thirdAttachments.tacticalAsset.aimingRecoilMultiplier;
                }
                num2 *= thirdAttachments.tacticalAsset.recoil_x;
                num3 *= thirdAttachments.tacticalAsset.recoil_y;
                num4 *= thirdAttachments.tacticalAsset.shake;
                num5 *= thirdAttachments.tacticalAsset.shake;
                num6 *= thirdAttachments.tacticalAsset.shake;
            }
            if (thirdAttachments.gripAsset != null && (!thirdAttachments.gripAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
            {
                if (isAiming)
                {
                    num2 *= thirdAttachments.gripAsset.aimingRecoilMultiplier;
                    num3 *= thirdAttachments.gripAsset.aimingRecoilMultiplier;
                }
                num2 *= thirdAttachments.gripAsset.recoil_x;
                num3 *= thirdAttachments.gripAsset.recoil_y;
                num4 *= thirdAttachments.gripAsset.shake;
                num5 *= thirdAttachments.gripAsset.shake;
                num6 *= thirdAttachments.gripAsset.shake;
            }
            if (thirdAttachments.barrelAsset != null)
            {
                if (isAiming)
                {
                    num2 *= thirdAttachments.barrelAsset.aimingRecoilMultiplier;
                    num3 *= thirdAttachments.barrelAsset.aimingRecoilMultiplier;
                }
                num2 *= thirdAttachments.barrelAsset.recoil_x;
                num3 *= thirdAttachments.barrelAsset.recoil_y;
                num4 *= thirdAttachments.barrelAsset.shake;
                num5 *= thirdAttachments.barrelAsset.shake;
                num6 *= thirdAttachments.barrelAsset.shake;
            }
            if (thirdAttachments.magazineAsset != null)
            {
                if (isAiming)
                {
                    num2 *= thirdAttachments.magazineAsset.aimingRecoilMultiplier;
                    num3 *= thirdAttachments.magazineAsset.aimingRecoilMultiplier;
                }
                num2 *= thirdAttachments.magazineAsset.recoil_x;
                num3 *= thirdAttachments.magazineAsset.recoil_y;
                num4 *= thirdAttachments.magazineAsset.shake;
                num5 *= thirdAttachments.magazineAsset.shake;
                num6 *= thirdAttachments.magazineAsset.shake;
            }
            applyRecoilMagnitudeModifiers(ref num2);
            applyRecoilMagnitudeModifiers(ref num3);
            if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                num4 *= SHAKE_CROUCH;
                num5 *= SHAKE_CROUCH;
                num6 *= SHAKE_CROUCH;
            }
            else if (base.player.stance.stance == EPlayerStance.PRONE)
            {
                num4 *= SHAKE_PRONE;
                num5 *= SHAKE_PRONE;
                num6 *= SHAKE_PRONE;
            }
            if (base.player.look.perspective == EPlayerPerspective.THIRD)
            {
                num2 *= Provider.modeConfigData.Gameplay.ThirdPerson_RecoilMultiplier;
                num3 *= Provider.modeConfigData.Gameplay.ThirdPerson_RecoilMultiplier;
            }
            base.player.look.recoil(num2, num3, equippedGunAsset.recover_x, equippedGunAsset.recover_y);
            base.player.animator.AddRecoilViewmodelCameraOffset(num4, num5, num6);
            base.player.animator.AddRecoilViewmodelCameraRotation(num2, num3);
            updateInfo();
            if (equippedGunAsset.projectile == null)
            {
                shoot();
            }
        }
        if (Provider.isServer)
        {
            if (!base.channel.IsLocalPlayer && thirdAttachments.barrelAsset != null && thirdAttachments.barrelAsset.durability > 0)
            {
                if (thirdAttachments.barrelAsset.durability > base.player.equipment.state[16])
                {
                    base.player.equipment.state[16] = 0;
                }
                else
                {
                    base.player.equipment.state[16] -= thirdAttachments.barrelAsset.durability;
                }
                base.player.equipment.updateState();
            }
            equippedGunAsset.GrantShootQuestRewards(base.player);
            if (equippedGunAsset.projectile == null)
            {
                byte b3 = (byte)((thirdAttachments.magazineAsset == null) ? 1 : thirdAttachments.magazineAsset.pellets);
                float gravityMultiplier2 = CalculateBulletGravityMultiplier();
                for (byte b4 = 0; b4 < b3; b4++)
                {
                    BulletInfo bulletInfo2;
                    if (base.channel.IsLocalPlayer)
                    {
                        bulletInfo2 = bullets[bullets.Count - b3 + b4];
                    }
                    else
                    {
                        bulletInfo2 = new BulletInfo();
                        bulletInfo2.origin = base.player.look.aim.position;
                        bulletInfo2.ApproximatePlayerAimDirection = base.player.look.aim.forward;
                        bulletInfo2.position = bulletInfo2.origin;
                        bulletInfo2.pellet = b4;
                        bulletInfo2.quality = num;
                        bulletInfo2.barrelAsset = thirdAttachments.barrelAsset;
                        bulletInfo2.magazineAsset = thirdAttachments.magazineAsset;
                        bulletInfo2.gravityMultiplier = gravityMultiplier2;
                        bullets.Add(bulletInfo2);
                        UseableGun.onBulletSpawned?.Invoke(this, bulletInfo2);
                    }
                    if (thirdAttachments.magazineAsset != null && thirdAttachments.magazineAsset.isExplosive)
                    {
                        if (equippedGunAsset.action == EAction.String)
                        {
                            base.player.equipment.state[8] = 0;
                            base.player.equipment.state[9] = 0;
                            base.player.equipment.state[10] = 0;
                            base.player.equipment.state[17] = 0;
                            base.player.equipment.sendUpdateState();
                        }
                    }
                    else if (equippedGunAsset.action == EAction.String)
                    {
                        if (base.player.equipment.state[17] > 0)
                        {
                            byte b5 = (byte)((thirdAttachments.magazineAsset == null) ? 1 : thirdAttachments.magazineAsset.stuck);
                            if (base.player.equipment.state[17] > b5)
                            {
                                base.player.equipment.state[17] -= b5;
                            }
                            else
                            {
                                base.player.equipment.state[17] = 0;
                            }
                            bulletInfo2.dropID = thirdAttachments.magazineID;
                            bulletInfo2.dropAmount = base.player.equipment.state[10];
                            bulletInfo2.dropQuality = base.player.equipment.state[17];
                        }
                        base.player.equipment.state[8] = 0;
                        base.player.equipment.state[9] = 0;
                        base.player.equipment.state[10] = 0;
                        base.player.equipment.sendUpdateState();
                    }
                }
            }
            else
            {
                ItemBarrelAsset itemBarrelAsset = ((base.player.equipment.state[16] > 0) ? thirdAttachments.barrelAsset : null);
                ItemMagazineAsset magazineAsset = thirdAttachments.magazineAsset;
                if (base.player.input.hasInputs())
                {
                    InputInfo input = base.player.input.getInput(doOcclusionCheck: false, ERaycastInfoUsage.Gun);
                    if (input != null && input.transform != null)
                    {
                        base.player.look.aim.LookAt(input.point);
                    }
                }
                if (ammo == 0 && equippedGunAsset.shouldDeleteEmptyMagazines)
                {
                    base.player.equipment.state[8] = 0;
                    base.player.equipment.state[9] = 0;
                    base.player.equipment.state[10] = 0;
                    base.player.equipment.sendUpdateState();
                }
                if (!base.channel.IsLocalPlayer)
                {
                    Vector3 position2 = base.player.look.aim.position;
                    Vector3 forward2 = base.player.look.aim.forward;
                    if (!Physics.Raycast(new Ray(position2, forward2), out var _, 1f, RayMasks.DAMAGE_SERVER))
                    {
                        position2 += forward2;
                    }
                    project(position2, forward2, itemBarrelAsset, magazineAsset);
                    SendPlayProject.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner(), position2, forward2, itemBarrelAsset?.id ?? 0, magazineAsset?.id ?? 0);
                }
                base.player.life.markAggressive(force: false);
            }
        }
        if (equippedGunAsset.canEverJam && Provider.isServer && num < equippedGunAsset.jamQualityThreshold)
        {
            float t = 1f - num / equippedGunAsset.jamQualityThreshold;
            float num7 = Mathf.Lerp(0f, equippedGunAsset.jamMaxChance, t);
            if (UnityEngine.Random.value < num7)
            {
                SendPlayChamberJammed.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), ammo);
            }
        }
    }

    private void jab()
    {
        if (Provider.isServer)
        {
            AlertTool.alert(base.transform.position, 8f);
        }
        if (base.channel.IsLocalPlayer)
        {
            AudioClip audioClip = jabClipRef.loadAsset();
            if (audioClip == null)
            {
                UnturnedLog.warn("Missing built-in bayonet audio");
            }
            base.player.animator.AddBayonetViewmodelCameraOffset(0f, 0f, 0.8f);
            base.player.playSound(audioClip, 0.5f);
            if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out int data))
            {
                Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", data + 1);
            }
            RaycastInfo raycastInfo = DamageTool.raycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), 2f, RayMasks.DAMAGE_CLIENT, base.player);
            if (raycastInfo.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player) || equippedGunAsset.bypassAllowedToDamagePlayer))
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
            else if (raycastInfo.zombie != null || raycastInfo.animal != null)
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
            base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Bayonet);
        }
        if (!Provider.isServer || !base.player.input.hasInputs())
        {
            return;
        }
        InputInfo input = base.player.input.getInput(doOcclusionCheck: true, ERaycastInfoUsage.Bayonet);
        if (input == null || (input.point - base.player.look.aim.position).sqrMagnitude > 36f)
        {
            return;
        }
        if (!string.IsNullOrEmpty(input.materialName))
        {
            DamageTool.ServerSpawnLegacyImpact(input.point, input.normal, input.materialName, input.colliderTransform, base.channel.GatherOwnerAndClientConnectionsWithinSphere(input.point, EffectManager.SMALL));
        }
        EPlayerKill kill = EPlayerKill.NONE;
        uint xp = 0u;
        float num = 1f;
        num *= 1f + base.channel.owner.player.skills.mastery(0, 0) * 0.5f;
        ERagdollEffect useableRagdollEffect = base.player.equipment.getUseableRagdollEffect();
        if (input.type == ERaycastInfoType.PLAYER)
        {
            if (input.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, input.player) || equippedGunAsset.bypassAllowedToDamagePlayer))
            {
                IDamageMultiplier dAMAGE_PLAYER_MULTIPLIER = DAMAGE_PLAYER_MULTIPLIER;
                DamagePlayerParameters parameters = DamagePlayerParameters.make(input.player, EDeathCause.MELEE, input.direction, dAMAGE_PLAYER_MULTIPLIER, input.limb);
                parameters.killer = base.channel.owner.playerID.steamID;
                parameters.times = num;
                parameters.respectArmor = true;
                parameters.trackKill = true;
                parameters.ragdollEffect = useableRagdollEffect;
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
                IDamageMultiplier dAMAGE_ZOMBIE_MULTIPLIER = DAMAGE_ZOMBIE_MULTIPLIER;
                DamageZombieParameters parameters2 = DamageZombieParameters.make(input.zombie, input.direction, dAMAGE_ZOMBIE_MULTIPLIER, input.limb);
                parameters2.times = num;
                parameters2.allowBackstab = true;
                parameters2.respectArmor = true;
                parameters2.instigator = base.player;
                parameters2.zombieStunOverride = equippedGunAsset.zombieStunOverride;
                parameters2.ragdollEffect = useableRagdollEffect;
                if (base.player.movement.nav != byte.MaxValue)
                {
                    parameters2.AlertPosition = base.transform.position;
                }
                DamageTool.damageZombie(parameters2, out kill, out xp);
            }
        }
        else if (input.type == ERaycastInfoType.ANIMAL && input.animal != null)
        {
            IDamageMultiplier dAMAGE_ANIMAL_MULTIPLIER = DAMAGE_ANIMAL_MULTIPLIER;
            DamageAnimalParameters parameters3 = DamageAnimalParameters.make(input.animal, input.direction, dAMAGE_ANIMAL_MULTIPLIER, input.limb);
            parameters3.times = num;
            parameters3.instigator = base.player;
            parameters3.ragdollEffect = useableRagdollEffect;
            parameters3.AlertPosition = base.transform.position;
            DamageTool.damageAnimal(parameters3, out kill, out xp);
        }
        if (input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !base.player.life.isAggressor)
        {
            float num2 = 2f + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
            num2 *= num2;
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
                    if (vector2.sqrMagnitude < num2 && (vector2 - vector).sqrMagnitude < ray_Aggressor_Distance)
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

    /// <summary>
    /// Calculate damage multiplier for individual bullet.
    /// </summary>
    private float getBulletDamageMultiplier(ref BulletInfo bullet)
    {
        float num = ((bullet.quality < 0.5f) ? (0.5f + bullet.quality) : 1f);
        if (bullet.magazineAsset != null)
        {
            num *= bullet.magazineAsset.ballisticDamageMultiplier;
        }
        if (thirdAttachments.sightAsset != null)
        {
            num *= thirdAttachments.sightAsset.ballisticDamageMultiplier;
        }
        if (thirdAttachments.tacticalAsset != null && shouldEnableTacticalStats)
        {
            num *= thirdAttachments.tacticalAsset.ballisticDamageMultiplier;
        }
        if (thirdAttachments.barrelAsset != null)
        {
            num *= thirdAttachments.barrelAsset.ballisticDamageMultiplier;
        }
        if (thirdAttachments.gripAsset != null)
        {
            num *= thirdAttachments.gripAsset.ballisticDamageMultiplier;
        }
        return num;
    }

    private void ballistics()
    {
        if (equippedGunAsset.projectile != null || bullets == null)
        {
            return;
        }
        if (base.channel.IsLocalPlayer)
        {
            for (int i = 0; i < bullets.Count; i++)
            {
                BulletInfo bulletInfo = bullets[i];
                byte b = (byte)((bulletInfo.magazineAsset == null) ? 1 : bulletInfo.magazineAsset.pellets);
                if (!base.channel.IsLocalPlayer)
                {
                    continue;
                }
                EPlayerHit ePlayerHit = EPlayerHit.NONE;
                _ = bulletInfo.pellet;
                Ray ray = new Ray(bulletInfo.position, bulletInfo.velocity);
                float range = (Provider.modeConfigData.Gameplay.Ballistics ? (bulletInfo.velocity.magnitude * 0.02f) : equippedGunAsset.range);
                RaycastInfo raycastInfo = DamageTool.raycast(ray, range, RayMasks.DAMAGE_CLIENT, base.player);
                if (raycastInfo.player != null && equippedGunAsset.playerDamageMultiplier.damage > 1f && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player) || equippedGunAsset.bypassAllowedToDamagePlayer))
                {
                    if (ePlayerHit != EPlayerHit.CRITICAL)
                    {
                        ePlayerHit = ((raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
                    }
                    PlayerUI.hitmark(raycastInfo.point, b > 1, (raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
                }
                else if (raycastInfo.zombie != null && equippedGunAsset.zombieDamageMultiplier.damage > 1f)
                {
                    EPlayerHit ePlayerHit2 = ((raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
                    if (raycastInfo.zombie.getBulletResistance() < 0.2f)
                    {
                        ePlayerHit2 = EPlayerHit.GHOST;
                    }
                    if (ePlayerHit != EPlayerHit.CRITICAL)
                    {
                        ePlayerHit = ePlayerHit2;
                    }
                    PlayerUI.hitmark(raycastInfo.point, b > 1, ePlayerHit2);
                }
                else if (raycastInfo.animal != null && equippedGunAsset.animalDamageMultiplier.damage > 1f)
                {
                    if (ePlayerHit != EPlayerHit.CRITICAL)
                    {
                        ePlayerHit = ((raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
                    }
                    PlayerUI.hitmark(raycastInfo.point, b > 1, (raycastInfo.limb != ELimb.SKULL) ? EPlayerHit.ENTITIY : EPlayerHit.CRITICAL);
                }
                else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Barricade") && equippedGunAsset.barricadeDamage > 1f)
                {
                    BarricadeDrop barricadeDrop = BarricadeDrop.FindByRootFast(raycastInfo.transform);
                    if (barricadeDrop != null)
                    {
                        ItemBarricadeAsset asset = barricadeDrop.asset;
                        if (asset != null && asset.canBeDamaged && (asset.isVulnerable || CanDamageInvulnerableEntities))
                        {
                            if (ePlayerHit == EPlayerHit.NONE)
                            {
                                ePlayerHit = EPlayerHit.BUILD;
                            }
                            PlayerUI.hitmark(raycastInfo.point, b > 1, EPlayerHit.BUILD);
                        }
                    }
                }
                else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Structure") && equippedGunAsset.structureDamage > 1f)
                {
                    StructureDrop structureDrop = StructureDrop.FindByRootFast(raycastInfo.transform);
                    if (structureDrop != null)
                    {
                        ItemStructureAsset asset2 = structureDrop.asset;
                        if (asset2 != null && asset2.canBeDamaged && (asset2.isVulnerable || CanDamageInvulnerableEntities))
                        {
                            if (ePlayerHit == EPlayerHit.NONE)
                            {
                                ePlayerHit = EPlayerHit.BUILD;
                            }
                            PlayerUI.hitmark(raycastInfo.point, b > 1, EPlayerHit.BUILD);
                        }
                    }
                }
                else if (raycastInfo.vehicle != null && !raycastInfo.vehicle.isDead && equippedGunAsset.vehicleDamage > 1f)
                {
                    if (raycastInfo.vehicle.asset != null && raycastInfo.vehicle.canBeDamaged && (raycastInfo.vehicle.asset.isVulnerable || CanDamageInvulnerableEntities))
                    {
                        if (ePlayerHit == EPlayerHit.NONE)
                        {
                            ePlayerHit = EPlayerHit.BUILD;
                        }
                        PlayerUI.hitmark(raycastInfo.point, b > 1, EPlayerHit.BUILD);
                    }
                }
                else if (raycastInfo.transform != null && raycastInfo.transform.CompareTag("Resource") && equippedGunAsset.resourceDamage > 1f)
                {
                    if (ResourceManager.tryGetRegion(raycastInfo.transform, out var x, out var y, out var index))
                    {
                        ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(x, y, index);
                        if (resourceSpawnpoint != null && !resourceSpawnpoint.isDead && equippedGunAsset.hasBladeID(resourceSpawnpoint.asset.bladeID))
                        {
                            if (ePlayerHit == EPlayerHit.NONE)
                            {
                                ePlayerHit = EPlayerHit.BUILD;
                            }
                            PlayerUI.hitmark(raycastInfo.point, b > 1, EPlayerHit.BUILD);
                        }
                    }
                }
                else if (raycastInfo.transform != null && equippedGunAsset.objectDamage > 1f)
                {
                    InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
                    if (componentInParent != null)
                    {
                        raycastInfo.transform = componentInParent.transform;
                        raycastInfo.section = componentInParent.getSection(raycastInfo.collider.transform);
                        if (componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && equippedGunAsset.hasBladeID(componentInParent.asset.rubbleBladeID) && (componentInParent.asset.rubbleIsVulnerable || CanDamageInvulnerableEntities))
                        {
                            if (ePlayerHit == EPlayerHit.NONE)
                            {
                                ePlayerHit = EPlayerHit.BUILD;
                            }
                            PlayerUI.hitmark(raycastInfo.point, b > 1, EPlayerHit.BUILD);
                        }
                    }
                }
                if (Provider.modeConfigData.Gameplay.Ballistics)
                {
                    if (bulletInfo.steps > 0 || equippedGunAsset.ballisticSteps <= 1)
                    {
                        Vector3 direction = bulletInfo.GetDirection();
                        if (equippedGunAsset.ballisticTravel < 32f)
                        {
                            trace(bulletInfo.position + direction * 32f, direction);
                        }
                        else
                        {
                            trace(bulletInfo.position + direction * UnityEngine.Random.Range(32f, equippedGunAsset.ballisticTravel), direction);
                        }
                        if (b < 2)
                        {
                            PlayFlybyAudio(ray.origin, ray.direction, equippedGunAsset.ballisticTravel);
                        }
                    }
                }
                else
                {
                    if (equippedGunAsset.range < 32f)
                    {
                        trace(ray.origin + ray.direction * 32f, ray.direction);
                    }
                    else
                    {
                        trace(ray.origin + ray.direction * UnityEngine.Random.Range(32f, Mathf.Min(64f, equippedGunAsset.range)), ray.direction);
                    }
                    if (b < 2)
                    {
                        PlayFlybyAudio(ray.origin, ray.direction, equippedGunAsset.range);
                    }
                }
                if (base.player.input.isRaycastInvalid(raycastInfo))
                {
                    float y2 = Physics.gravity.y;
                    y2 *= bulletInfo.gravityMultiplier;
                    bulletInfo.position += bulletInfo.velocity * 0.02f;
                    bulletInfo.velocity = new Vector3(bulletInfo.velocity.x, bulletInfo.velocity.y + y2 * 0.02f, bulletInfo.velocity.z);
                    continue;
                }
                if (ePlayerHit != 0)
                {
                    if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out int data))
                    {
                        Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", data + 1);
                    }
                    if (ePlayerHit == EPlayerHit.CRITICAL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out data))
                    {
                        Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", data + 1);
                    }
                }
                base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Gun);
                bulletInfo.steps = 254;
            }
        }
        if (Provider.isServer)
        {
            while (bullets.Count > 0)
            {
                BulletInfo bullet = bullets[0];
                byte b2 = (byte)((bullet.magazineAsset == null) ? 1 : bullet.magazineAsset.pellets);
                if (!base.player.input.hasInputs())
                {
                    break;
                }
                InputInfo input = base.player.input.getInput(doOcclusionCheck: true, ERaycastInfoUsage.Gun);
                if (input == null || equippedGunAsset == null)
                {
                    break;
                }
                if (!base.channel.IsLocalPlayer)
                {
                    if (Provider.modeConfigData.Gameplay.Ballistics)
                    {
                        if ((input.point - bullet.position).magnitude > equippedGunAsset.ballisticTravel * (float)(bullet.steps + 1 + PlayerInput.SAMPLES) + 4f)
                        {
                            bullets.RemoveAt(0);
                            continue;
                        }
                    }
                    else if ((input.point - base.player.look.aim.position).sqrMagnitude > MathfEx.Square(equippedGunAsset.range + 4f))
                    {
                        break;
                    }
                }
                if (UseableGun.onBulletHit != null)
                {
                    bool shouldAllow = true;
                    UseableGun.onBulletHit(this, bullet, input, ref shouldAllow);
                    if (!shouldAllow)
                    {
                        bullets.RemoveAt(0);
                        continue;
                    }
                }
                if (!string.IsNullOrEmpty(input.materialName))
                {
                    if (bullet.magazineAsset != null && !bullet.magazineAsset.IsImpactEffectRefNull())
                    {
                        DamageTool.ServerTriggerImpactEffectForMagazinesV2(bullet.magazineAsset.FindImpactEffectAsset(), input.point, input.normal, base.channel.owner);
                    }
                    else
                    {
                        DamageTool.ServerSpawnBulletImpact(input.point, input.normal, input.materialName, input.colliderTransform, base.channel.owner, base.channel.GatherOwnerAndClientConnectionsWithinSphere(input.point, EffectManager.SMALL));
                    }
                }
                EPlayerKill kill = EPlayerKill.NONE;
                uint xp = 0u;
                float bulletDamageMultiplier = getBulletDamageMultiplier(ref bullet);
                float value = Vector3.Distance(bullet.origin, input.point);
                float a = equippedGunAsset.range * equippedGunAsset.damageFalloffRange;
                float b3 = equippedGunAsset.range * equippedGunAsset.damageFalloffMaxRange;
                float t = Mathf.InverseLerp(a, b3, value);
                bulletDamageMultiplier *= Mathf.Lerp(1f, equippedGunAsset.damageFalloffMultiplier, t);
                ERagdollEffect useableRagdollEffect = base.player.equipment.getUseableRagdollEffect();
                if (input.type == ERaycastInfoType.PLAYER)
                {
                    if (input.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, input.player) || equippedGunAsset.bypassAllowedToDamagePlayer))
                    {
                        bool flag = input.limb == ELimb.SKULL && equippedGunAsset.instakillHeadshots && Provider.modeConfigData.Players.Allow_Instakill_Headshots;
                        IDamageMultiplier playerDamageMultiplier = equippedGunAsset.playerDamageMultiplier;
                        DamagePlayerParameters parameters = DamagePlayerParameters.make(input.player, EDeathCause.GUN, input.direction * Mathf.Ceil((float)(int)b2 / 2f), playerDamageMultiplier, input.limb);
                        parameters.killer = base.channel.owner.playerID.steamID;
                        parameters.times = bulletDamageMultiplier;
                        parameters.respectArmor = !flag;
                        parameters.trackKill = true;
                        parameters.ragdollEffect = useableRagdollEffect;
                        equippedGunAsset.initPlayerDamageParameters(ref parameters);
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
                        bool flag2 = input.limb == ELimb.SKULL && equippedGunAsset.instakillHeadshots && Provider.modeConfigData.Zombies.Weapons_Use_Player_Damage && Provider.modeConfigData.Players.Allow_Instakill_Headshots;
                        Vector3 direction2 = input.direction * Mathf.Ceil((float)(int)b2 / 2f);
                        IDamageMultiplier zombieOrPlayerDamageMultiplier = equippedGunAsset.zombieOrPlayerDamageMultiplier;
                        DamageZombieParameters parameters2 = DamageZombieParameters.make(input.zombie, direction2, zombieOrPlayerDamageMultiplier, input.limb);
                        parameters2.times = bulletDamageMultiplier * input.zombie.getBulletResistance();
                        parameters2.allowBackstab = false;
                        parameters2.respectArmor = !flag2;
                        parameters2.instigator = base.player;
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
                        Vector3 direction3 = input.direction * Mathf.Ceil((float)(int)b2 / 2f);
                        IDamageMultiplier animalOrPlayerDamageMultiplier = equippedGunAsset.animalOrPlayerDamageMultiplier;
                        DamageAnimalParameters parameters3 = DamageAnimalParameters.make(input.animal, direction3, animalOrPlayerDamageMultiplier, input.limb);
                        parameters3.times = bulletDamageMultiplier;
                        parameters3.instigator = base.player;
                        parameters3.ragdollEffect = useableRagdollEffect;
                        parameters3.AlertPosition = base.transform.position;
                        DamageTool.damageAnimal(parameters3, out kill, out xp);
                    }
                }
                else if (input.type == ERaycastInfoType.VEHICLE)
                {
                    if (input.vehicle != null && input.vehicle.asset != null && input.vehicle.canBeDamaged && (input.vehicle.asset.isVulnerable || CanDamageInvulnerableEntities))
                    {
                        float num = (CanDamageInvulnerableEntities ? Provider.modeConfigData.Vehicles.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Vehicles.Gun_Lowcal_Damage_Multiplier);
                        DamageTool.damage(input.vehicle, damageTires: true, input.point, isRepairing: false, equippedGunAsset.vehicleDamage, bulletDamageMultiplier * num, canRepair: true, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
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
                            if (asset3 != null && asset3.canBeDamaged && (asset3.isVulnerable || CanDamageInvulnerableEntities))
                            {
                                float num2 = (CanDamageInvulnerableEntities ? Provider.modeConfigData.Barricades.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Barricades.Gun_Lowcal_Damage_Multiplier);
                                DamageTool.damage(input.transform, isRepairing: false, equippedGunAsset.barricadeDamage, bulletDamageMultiplier * num2, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
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
                            if (asset4 != null && asset4.canBeDamaged && (asset4.isVulnerable || CanDamageInvulnerableEntities))
                            {
                                float num3 = (CanDamageInvulnerableEntities ? Provider.modeConfigData.Structures.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Structures.Gun_Lowcal_Damage_Multiplier);
                                DamageTool.damage(input.transform, isRepairing: false, input.direction * Mathf.Ceil((float)(int)b2 / 2f), equippedGunAsset.structureDamage, bulletDamageMultiplier * num3, out kill, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                            }
                        }
                    }
                }
                else if (input.type == ERaycastInfoType.RESOURCE)
                {
                    if (input.transform != null && input.transform.CompareTag("Resource") && ResourceManager.tryGetRegion(input.transform, out var x2, out var y3, out var index2))
                    {
                        ResourceSpawnpoint resourceSpawnpoint2 = ResourceManager.getResourceSpawnpoint(x2, y3, index2);
                        if (resourceSpawnpoint2 != null && !resourceSpawnpoint2.isDead && equippedGunAsset.hasBladeID(resourceSpawnpoint2.asset.bladeID))
                        {
                            DamageTool.damage(input.transform, input.direction * Mathf.Ceil((float)(int)b2 / 2f), equippedGunAsset.resourceDamage, bulletDamageMultiplier, 1f, out kill, out xp, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                        }
                    }
                }
                else if (input.type == ERaycastInfoType.OBJECT && input.transform != null && input.section < byte.MaxValue)
                {
                    InteractableObjectRubble componentInParent2 = input.transform.GetComponentInParent<InteractableObjectRubble>();
                    if (componentInParent2 != null && componentInParent2.IsSectionIndexValid(input.section) && !componentInParent2.isSectionDead(input.section) && equippedGunAsset.hasBladeID(componentInParent2.asset.rubbleBladeID) && (componentInParent2.asset.rubbleIsVulnerable || CanDamageInvulnerableEntities))
                    {
                        DamageTool.damage(componentInParent2.transform, input.direction, input.section, equippedGunAsset.objectDamage, bulletDamageMultiplier, out kill, out xp, base.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                    }
                }
                if (input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !base.player.life.isAggressor)
                {
                    float num4 = equippedGunAsset.range + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
                    num4 *= num4;
                    float ray_Aggressor_Distance = Provider.modeConfigData.Players.Ray_Aggressor_Distance;
                    ray_Aggressor_Distance *= ray_Aggressor_Distance;
                    Vector3 normalized = (bullet.position - base.player.look.aim.position).normalized;
                    for (int j = 0; j < Provider.clients.Count; j++)
                    {
                        if (Provider.clients[j] == base.channel.owner)
                        {
                            continue;
                        }
                        Player player = Provider.clients[j].player;
                        if (!(player == null))
                        {
                            Vector3 vector = player.look.aim.position - base.player.look.aim.position;
                            Vector3 vector2 = Vector3.Project(vector, normalized);
                            if (vector2.sqrMagnitude < num4 && (vector2 - vector).sqrMagnitude < ray_Aggressor_Distance)
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
                Vector3 vector3 = input.point + input.normal * 0.25f;
                if (bullet.magazineAsset != null && bullet.magazineAsset.isExplosive)
                {
                    DetonateExplosiveMagazine(bullet.magazineAsset, vector3, base.player, useableRagdollEffect);
                }
                if (bullet.dropID != 0)
                {
                    ItemManager.dropItem(new Item(bullet.dropID, bullet.dropAmount, bullet.dropQuality), vector3, playEffect: false, Dedicator.IsDedicatedServer, wideSpread: false);
                }
                bullets.RemoveAt(0);
            }
        }
        if (base.player.equipment.asset == null)
        {
            return;
        }
        if (Provider.modeConfigData.Gameplay.Ballistics)
        {
            for (int num5 = bullets.Count - 1; num5 >= 0; num5--)
            {
                BulletInfo bulletInfo2 = bullets[num5];
                bulletInfo2.steps++;
                if (bulletInfo2.steps >= equippedGunAsset.ballisticSteps)
                {
                    bullets.RemoveAt(num5);
                }
            }
        }
        else
        {
            bullets.Clear();
        }
    }

    [Obsolete]
    public void askAttachSight(CSteamID steamID, byte page, byte x, byte y, byte[] hash)
    {
        ReceiveAttachSight(page, x, y, hash);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askAttachSight")]
    public void ReceiveAttachSight(byte page, byte x, byte y, byte[] hash)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null || !equippedGunAsset.hasSight)
        {
            return;
        }
        Item item = null;
        if (thirdAttachments.sightAsset != null)
        {
            item = new Item(thirdAttachments.sightID, full: false, base.player.equipment.state[13]);
        }
        if (page != byte.MaxValue)
        {
            byte index = base.player.inventory.getIndex(page, x, y);
            if (index != byte.MaxValue)
            {
                ItemJar item2 = base.player.inventory.getItem(page, index);
                ItemCaliberAsset asset = item2.GetAsset<ItemCaliberAsset>();
                if (asset == null || (asset.shouldVerifyHash && !Hash.verifyHash(hash, asset.hash)))
                {
                    return;
                }
                if (asset.calibers.Length != 0)
                {
                    bool flag = false;
                    for (byte b = 0; b < asset.calibers.Length; b++)
                    {
                        for (byte b2 = 0; b2 < equippedGunAsset.attachmentCalibers.Length; b2++)
                        {
                            if (asset.calibers[b] == equippedGunAsset.attachmentCalibers[b2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return;
                    }
                }
                else if (equippedGunAsset.requiresNonZeroAttachmentCaliber)
                {
                    return;
                }
                if (changeSightRequested(item, item2))
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(item2.item.id), 0, base.player.equipment.state, 0, 2);
                    base.player.equipment.state[13] = item2.item.quality;
                    base.player.inventory.removeItem(page, index);
                    if (item != null)
                    {
                        base.player.inventory.forceAddItem(item, auto: true);
                    }
                    base.player.equipment.sendUpdateState();
                    EffectManager.TriggerFiremodeEffect(base.transform.position);
                }
                return;
            }
        }
        if (changeSightRequested(item, null))
        {
            if (item != null)
            {
                base.player.inventory.forceAddItem(item, auto: true);
            }
            base.player.equipment.state[0] = 0;
            base.player.equipment.state[1] = 0;
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    [Obsolete]
    public void askAttachTactical(CSteamID steamID, byte page, byte x, byte y, byte[] hash)
    {
        ReceiveAttachTactical(page, x, y, hash);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askAttachTactical")]
    public void ReceiveAttachTactical(byte page, byte x, byte y, byte[] hash)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null || !equippedGunAsset.hasTactical)
        {
            return;
        }
        Item item = null;
        if (thirdAttachments.tacticalAsset != null)
        {
            item = new Item(thirdAttachments.tacticalID, full: false, base.player.equipment.state[14]);
        }
        if (page != byte.MaxValue)
        {
            byte index = base.player.inventory.getIndex(page, x, y);
            if (index != byte.MaxValue)
            {
                ItemJar item2 = base.player.inventory.getItem(page, index);
                ItemCaliberAsset asset = item2.GetAsset<ItemCaliberAsset>();
                if (asset == null || (asset.shouldVerifyHash && !Hash.verifyHash(hash, asset.hash)))
                {
                    return;
                }
                if (asset.calibers.Length != 0)
                {
                    bool flag = false;
                    for (byte b = 0; b < asset.calibers.Length; b++)
                    {
                        for (byte b2 = 0; b2 < equippedGunAsset.attachmentCalibers.Length; b2++)
                        {
                            if (asset.calibers[b] == equippedGunAsset.attachmentCalibers[b2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return;
                    }
                }
                else if (equippedGunAsset.requiresNonZeroAttachmentCaliber)
                {
                    return;
                }
                if (changeTacticalRequested(item, item2))
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(item2.item.id), 0, base.player.equipment.state, 2, 2);
                    base.player.equipment.state[14] = item2.item.quality;
                    base.player.inventory.removeItem(page, index);
                    if (item != null)
                    {
                        base.player.inventory.forceAddItem(item, auto: true);
                    }
                    base.player.equipment.sendUpdateState();
                    EffectManager.TriggerFiremodeEffect(base.transform.position);
                }
                return;
            }
        }
        if (changeTacticalRequested(item, null))
        {
            if (item != null)
            {
                base.player.inventory.forceAddItem(item, auto: true);
            }
            base.player.equipment.state[2] = 0;
            base.player.equipment.state[3] = 0;
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    [Obsolete]
    public void askAttachGrip(CSteamID steamID, byte page, byte x, byte y, byte[] hash)
    {
        ReceiveAttachGrip(page, x, y, hash);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askAttachGrip")]
    public void ReceiveAttachGrip(byte page, byte x, byte y, byte[] hash)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null || !equippedGunAsset.hasGrip)
        {
            return;
        }
        Item item = null;
        if (thirdAttachments.gripAsset != null)
        {
            item = new Item(thirdAttachments.gripID, full: false, base.player.equipment.state[15]);
        }
        if (page != byte.MaxValue)
        {
            byte index = base.player.inventory.getIndex(page, x, y);
            if (index != byte.MaxValue)
            {
                ItemJar item2 = base.player.inventory.getItem(page, index);
                ItemCaliberAsset asset = item2.GetAsset<ItemCaliberAsset>();
                if (asset == null || (asset.shouldVerifyHash && !Hash.verifyHash(hash, asset.hash)))
                {
                    return;
                }
                if (asset.calibers.Length != 0)
                {
                    bool flag = false;
                    for (byte b = 0; b < asset.calibers.Length; b++)
                    {
                        for (byte b2 = 0; b2 < equippedGunAsset.attachmentCalibers.Length; b2++)
                        {
                            if (asset.calibers[b] == equippedGunAsset.attachmentCalibers[b2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return;
                    }
                }
                else if (equippedGunAsset.requiresNonZeroAttachmentCaliber)
                {
                    return;
                }
                if (changeGripRequested(item, item2))
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(item2.item.id), 0, base.player.equipment.state, 4, 2);
                    base.player.equipment.state[15] = item2.item.quality;
                    base.player.inventory.removeItem(page, index);
                    if (item != null)
                    {
                        base.player.inventory.forceAddItem(item, auto: true);
                    }
                    base.player.equipment.sendUpdateState();
                    EffectManager.TriggerFiremodeEffect(base.transform.position);
                }
                return;
            }
        }
        if (changeGripRequested(item, null))
        {
            if (item != null)
            {
                base.player.inventory.forceAddItem(item, auto: true);
            }
            base.player.equipment.state[4] = 0;
            base.player.equipment.state[5] = 0;
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    [Obsolete]
    public void askAttachBarrel(CSteamID steamID, byte page, byte x, byte y, byte[] hash)
    {
        ReceiveAttachBarrel(page, x, y, hash);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askAttachBarrel")]
    public void ReceiveAttachBarrel(byte page, byte x, byte y, byte[] hash)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null || !equippedGunAsset.hasBarrel)
        {
            return;
        }
        Item item = null;
        if (thirdAttachments.barrelAsset != null)
        {
            item = new Item(thirdAttachments.barrelID, full: false, base.player.equipment.state[16]);
        }
        if (page != byte.MaxValue)
        {
            byte index = base.player.inventory.getIndex(page, x, y);
            if (index != byte.MaxValue)
            {
                ItemJar item2 = base.player.inventory.getItem(page, index);
                ItemCaliberAsset asset = item2.GetAsset<ItemCaliberAsset>();
                if (asset == null || (asset.shouldVerifyHash && !Hash.verifyHash(hash, asset.hash)))
                {
                    return;
                }
                if (asset.calibers.Length != 0)
                {
                    bool flag = false;
                    for (byte b = 0; b < asset.calibers.Length; b++)
                    {
                        for (byte b2 = 0; b2 < equippedGunAsset.attachmentCalibers.Length; b2++)
                        {
                            if (asset.calibers[b] == equippedGunAsset.attachmentCalibers[b2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return;
                    }
                }
                else if (equippedGunAsset.requiresNonZeroAttachmentCaliber)
                {
                    return;
                }
                if (changeBarrelRequested(item, item2))
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(item2.item.id), 0, base.player.equipment.state, 6, 2);
                    base.player.equipment.state[16] = item2.item.quality;
                    base.player.inventory.removeItem(page, index);
                    if (item != null)
                    {
                        base.player.inventory.forceAddItem(item, auto: true);
                    }
                    base.player.equipment.sendUpdateState();
                    EffectManager.TriggerFiremodeEffect(base.transform.position);
                }
                return;
            }
        }
        if (changeBarrelRequested(item, null))
        {
            if (item != null)
            {
                base.player.inventory.forceAddItem(item, auto: true);
            }
            base.player.equipment.state[6] = 0;
            base.player.equipment.state[7] = 0;
            base.player.equipment.sendUpdateState();
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    [Obsolete]
    public void askAttachMagazine(CSteamID steamID, byte page, byte x, byte y, byte[] hash)
    {
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 2, legacyName = "askAttachMagazine")]
    public void ReceiveAttachMagazine(in ServerInvocationContext context, byte page, byte x, byte y, byte[] hash)
    {
        if (base.player.equipment.isBusy || isFired || isReloading || isHammering || isUnjamming || needsRechamber || base.player.equipment.asset == null || !equippedGunAsset.allowMagazineChange)
        {
            return;
        }
        Item item = null;
        if (thirdAttachments.magazineAsset != null && (ammo > 0 || (!equippedGunAsset.shouldDeleteEmptyMagazines && !thirdAttachments.magazineAsset.deleteEmpty)))
        {
            byte newAmount = base.player.equipment.state[10];
            if (thirdAttachments.magazineAsset.shouldFillAfterDetach)
            {
                newAmount = thirdAttachments.magazineAsset.amount;
            }
            item = new Item(thirdAttachments.magazineID, newAmount, base.player.equipment.state[17]);
        }
        if (page != byte.MaxValue)
        {
            byte index = base.player.inventory.getIndex(page, x, y);
            if (index != byte.MaxValue)
            {
                ItemJar item2 = base.player.inventory.getItem(page, index);
                ItemCaliberAsset asset = item2.GetAsset<ItemCaliberAsset>();
                if (asset == null || (asset.shouldVerifyHash && !Hash.verifyHash(hash, asset.hash)))
                {
                    return;
                }
                if (asset.calibers.Length != 0)
                {
                    bool flag = false;
                    for (byte b = 0; b < asset.calibers.Length; b++)
                    {
                        for (byte b2 = 0; b2 < equippedGunAsset.magazineCalibers.Length; b2++)
                        {
                            if (asset.calibers[b] == equippedGunAsset.magazineCalibers[b2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return;
                    }
                }
                else if (equippedGunAsset.requiresNonZeroAttachmentCaliber)
                {
                    return;
                }
                if (changeMagazineRequested(item, item2))
                {
                    bool flag2 = ammo == 0;
                    ammo = item2.item.amount;
                    Buffer.BlockCopy(BitConverter.GetBytes(item2.item.id), 0, base.player.equipment.state, 8, 2);
                    base.player.equipment.state[10] = item2.item.amount;
                    base.player.equipment.state[17] = item2.item.quality;
                    base.player.inventory.removeItem(page, index);
                    if (item != null)
                    {
                        base.player.inventory.forceAddItem(item, auto: true);
                    }
                    base.player.equipment.sendUpdateState();
                    SendPlayReload.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), flag2 && equippedGunAsset.hammer != null);
                    EffectManager.TriggerFiremodeEffect(base.transform.position);
                }
                return;
            }
        }
        if (changeMagazineRequested(item, null))
        {
            if (item != null)
            {
                base.player.inventory.forceAddItem(item, auto: true);
            }
            base.player.equipment.state[8] = 0;
            base.player.equipment.state[9] = 0;
            base.player.equipment.state[10] = 0;
            base.player.equipment.sendUpdateState();
            SendPlayReload.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), equippedGunAsset.hammer != null);
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
    }

    private void hammer()
    {
        base.player.equipment.isBusy = true;
        isHammering = true;
        startedHammer = Time.realtimeSinceStartup;
        float num = 1f;
        num += base.player.skills.mastery(0, 2) * 0.5f;
        if (thirdAttachments.magazineAsset != null)
        {
            num *= thirdAttachments.magazineAsset.speed;
        }
        base.player.playSound(equippedGunAsset.hammer, num, 0f);
        updateAnimationSpeeds(num);
        base.player.animator.play("Hammer", smooth: false);
        GetVehicleTurretEventHook()?.OnChamberingStarted?.TryInvoke(this);
        foreach (UseableGunEventHook item in EnumerateEventComponents())
        {
            item.OnChamberingStarted?.TryInvoke(this);
        }
    }

    [Obsolete]
    public void askReload(CSteamID steamID, bool newHammer)
    {
        ReceivePlayReload(newHammer);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askReload")]
    public void ReceivePlayReload(bool newHammer)
    {
        if (!base.player.equipment.IsEquipAnimationFinished)
        {
            return;
        }
        if (isAiming)
        {
            isAiming = false;
            stopAim();
        }
        if (isAttaching)
        {
            isAttaching = false;
            stopAttach();
        }
        isShooting = false;
        isSprinting = false;
        base.player.equipment.isBusy = true;
        needsHammer = newHammer;
        isReloading = true;
        startedReload = Time.realtimeSinceStartup;
        float num = 1f;
        num += base.player.skills.mastery(0, 2) * 0.5f;
        if (thirdAttachments.magazineAsset != null)
        {
            num *= thirdAttachments.magazineAsset.speed;
        }
        base.player.playSound(equippedGunAsset.reload, num, 0f);
        updateAnimationSpeeds(num);
        base.player.animator.play("Reload", smooth: false);
        needsUnplace = true;
        needsReplace = true;
        if (equippedGunAsset.action == EAction.Break)
        {
            needsUnload = true;
        }
        UseableGun.OnReloading_Global.TryInvoke("OnReloading_Global", this);
        GetVehicleTurretEventHook()?.OnReloadingStarted?.TryInvoke(this);
        foreach (UseableGunEventHook item in EnumerateEventComponents())
        {
            item.OnReloadingStarted?.TryInvoke(this);
        }
    }

    /// <summary>
    /// Requested for plugin use.
    /// </summary>
    public void ServerPlayReload(bool shouldHammer)
    {
        shouldHammer &= equippedGunAsset.hammer != null;
        SendPlayReload.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), shouldHammer);
    }

    [Obsolete]
    public void askPlayChamberJammed(CSteamID steamID, byte correctedAmmo)
    {
        ReceivePlayChamberJammed(correctedAmmo);
    }

    /// <summary>
    /// Request from the server to play a gun jammed animation.
    /// Since client can't predict chamber jams we fixup the predicted ammo count.
    /// </summary>
    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askPlayChamberJammed")]
    public void ReceivePlayChamberJammed(byte correctedAmmo)
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            if (isAiming)
            {
                isAiming = false;
                stopAim();
            }
            if (isAttaching)
            {
                isAttaching = false;
                stopAttach();
            }
            isShooting = false;
            isSprinting = false;
            base.player.equipment.isBusy = true;
            isUnjamming = true;
            startedUnjammingChamber = Time.realtimeSinceStartup;
            float num = 1f;
            base.player.playSound(equippedGunAsset.chamberJammedSound, num, 0f);
            updateAnimationSpeeds(num);
            base.player.animator.play(equippedGunAsset.unjamChamberAnimName, smooth: false);
            ammo = correctedAmmo;
        }
    }

    [Obsolete]
    public void askAimStart(CSteamID steamID)
    {
        ReceivePlayAimStart();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askAimStart")]
    public void ReceivePlayAimStart()
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            startAim();
        }
    }

    [Obsolete]
    public void askAimStop(CSteamID steamID)
    {
        ReceivePlayAimStop();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "askAimStop")]
    public void ReceivePlayAimStop()
    {
        if (base.player.equipment.IsEquipAnimationFinished)
        {
            stopAim();
        }
    }

    public override bool startPrimary()
    {
        if ((!isShooting && !isReloading && !isHammering && !isUnjamming && !isAttaching && !needsRechamber && firemode != 0 && !base.player.equipment.isBusy && !base.player.quests.IsCutsceneModeActive()) & (!isSprinting || equippedGunAsset.canAimDuringSprint))
        {
            if (equippedGunAsset.action == EAction.String)
            {
                if (thirdAttachments.nockHook != null || isAiming)
                {
                    isShooting = true;
                }
            }
            else if (equippedGunAsset.action == EAction.Minigun)
            {
                if (isAiming)
                {
                    isShooting = true;
                }
            }
            else
            {
                isShooting = true;
            }
        }
        if (isShooting)
        {
            wasTriggerJustPulled = true;
            if (fireDelayCounter < 1)
            {
                fireDelayCounter = equippedGunAsset.fireDelay;
                if (fireDelayCounter > 0 && base.channel.IsLocalPlayer && equippedGunAsset.fireDelaySound != null)
                {
                    base.player.playSound(equippedGunAsset.fireDelaySound, 1f, 0f);
                }
            }
        }
        return isShooting;
    }

    public override void stopPrimary()
    {
        isShooting = false;
    }

    public override bool startSecondary()
    {
        if ((!isAiming && !isReloading && !isHammering && !isUnjamming && !isAttaching && !needsRechamber && firemode != EFiremode.SAFETY) & (!isSprinting || equippedGunAsset.canAimDuringSprint))
        {
            isAiming = true;
            startAim();
            if (Provider.isServer)
            {
                SendPlayAimStart.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner());
            }
        }
        return isAiming;
    }

    public override void stopSecondary()
    {
        if (isAiming)
        {
            if (equippedGunAsset.action == EAction.Minigun && isShooting)
            {
                isShooting = false;
            }
            isAiming = false;
            stopAim();
            if (Provider.isServer)
            {
                SendPlayAimStop.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner());
            }
        }
    }

    public override void equip()
    {
        lastShot = float.MaxValue;
        firstEventComponent = base.player.equipment.firstModel?.GetComponent<UseableGunEventHook>();
        thirdEventComponent = base.player.equipment.thirdModel?.GetComponent<UseableGunEventHook>();
        characterEventComponent = base.player.equipment.characterModel?.GetComponent<UseableGunEventHook>();
        if (!Dedicator.IsDedicatedServer)
        {
            if (base.channel.IsLocalPlayer)
            {
                gunshotAudioSource = base.player.gameObject.AddComponent<AudioSource>();
                gunshotAudioSource.priority = 63;
            }
            else
            {
                gunshotAudioSource = base.player.equipment.thirdModel.gameObject.AddComponent<AudioSource>();
            }
            gunshotAudioSource.clip = null;
            gunshotAudioSource.spatialBlend = 1f;
            gunshotAudioSource.rolloffMode = AudioRolloffMode.Custom;
            gunshotAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, (Resources.Load("Guns/Rolloff") as GameObject).GetComponent<AudioSource>().GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            gunshotAudioSource.volume = 1f;
            gunshotAudioSource.playOnAwake = false;
            gunshotAudioSource.outputAudioMixerGroup = UnturnedAudioMixer.GetDefaultGroup();
        }
        if (base.channel.IsLocalPlayer)
        {
            firstAttachments = base.player.equipment.firstModel.gameObject.GetComponent<Attachments>();
            firstMinigunBarrel = firstAttachments.transform.Find("Model_1");
            Transform transform = firstAttachments.transform.FindChildRecursive("Ammo_Counter");
            if (transform != null)
            {
                firstAmmoCounter = transform.GetComponent<Text>();
                transform.parent.gameObject.SetActive(value: true);
                transform.parent.gameObject.layer = 11;
                transform.gameObject.layer = 11;
            }
            if (firstAttachments.rope != null)
            {
                firstAttachments.rope.gameObject.SetActive(value: true);
            }
            if (firstAttachments.ejectHook != null && equippedGunAsset.action != EAction.String && equippedGunAsset.action != EAction.Rocket)
            {
                EffectAsset effectAsset = equippedGunAsset.FindShellEffectAsset();
                if (effectAsset != null)
                {
                    Transform transform2 = EffectManager.InstantiateFromPool(effectAsset).transform;
                    transform2.name = "Emitter";
                    transform2.parent = firstAttachments.ejectHook;
                    transform2.localPosition = Vector3.zero;
                    transform2.localRotation = Quaternion.identity;
                    transform2.tag = "Viewmodel";
                    transform2.gameObject.layer = 11;
                    firstShellEmitter = transform2.GetComponent<ParticleSystem>();
                }
            }
            if (firstAttachments.barrelHook != null)
            {
                EffectAsset effectAsset2 = equippedGunAsset.FindMuzzleEffectAsset();
                if (effectAsset2 != null)
                {
                    Transform transform3 = EffectManager.InstantiateFromPool(effectAsset2).transform;
                    transform3.name = "Emitter";
                    transform3.parent = firstAttachments.barrelHook;
                    transform3.localPosition = Vector3.zero;
                    transform3.localRotation = Quaternion.identity;
                    transform3.tag = "Viewmodel";
                    transform3.gameObject.layer = 11;
                    firstMuzzleEmitter = transform3.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = firstMuzzleEmitter.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    Light component = transform3.GetComponent<Light>();
                    if (component != null)
                    {
                        component.enabled = false;
                        component.cullingMask = 2048;
                    }
                }
            }
            if (equippedGunAsset.isTurret)
            {
                base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.up;
            }
            if (equippedGunAsset.driverTurretViewmodelMode == EDriverTurretViewmodelMode.AlwaysOffscreen)
            {
                base.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.up;
            }
            else
            {
                base.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.zero;
            }
        }
        thirdAttachments = base.player.equipment.thirdModel.gameObject.GetComponent<Attachments>();
        if (base.channel.IsLocalPlayer)
        {
            Transform transform4 = thirdAttachments.transform.FindChildRecursive("Ammo_Counter");
            if (transform4 != null)
            {
                thirdAmmoCounter = transform4.GetComponent<Text>();
                transform4.parent.gameObject.SetActive(value: true);
                transform4.parent.gameObject.layer = 10;
                transform4.gameObject.layer = 10;
            }
        }
        thirdMinigunBarrel = thirdAttachments.transform.Find("Model_1");
        if (!Dedicator.IsDedicatedServer && thirdMinigunBarrel != null && equippedGunAsset.action == EAction.Minigun)
        {
            if (base.channel.IsLocalPlayer)
            {
                whir = base.player.gameObject.AddComponent<AudioSource>();
            }
            else
            {
                whir = base.player.equipment.thirdModel.gameObject.AddComponent<AudioSource>();
            }
            whir.clip = equippedGunAsset.minigun;
            whir.spatialBlend = 1f;
            whir.rolloffMode = AudioRolloffMode.Linear;
            whir.minDistance = 1f;
            whir.maxDistance = 16f;
            whir.volume = 0f;
            whir.playOnAwake = false;
            whir.loop = true;
            whir.outputAudioMixerGroup = UnturnedAudioMixer.GetDefaultGroup();
            whir.Play();
        }
        if (thirdAttachments.ejectHook != null && equippedGunAsset.action != EAction.String && equippedGunAsset.action != EAction.Rocket)
        {
            EffectAsset effectAsset3 = equippedGunAsset.FindShellEffectAsset();
            if (effectAsset3 != null)
            {
                Transform transform5 = EffectManager.InstantiateFromPool(effectAsset3).transform;
                transform5.name = "Emitter";
                transform5.localPosition = Vector3.zero;
                thirdShellEmitter = transform5.GetComponent<ParticleSystem>();
                thirdShellRenderer = transform5.GetComponent<ParticleSystemRenderer>();
                if (base.channel.IsLocalPlayer)
                {
                    ParticleSystem.CollisionModule collision = thirdShellEmitter.collision;
                    collision.enabled = true;
                    ParticleSystem.TriggerModule trigger = thirdShellEmitter.trigger;
                    trigger.enabled = true;
                    trigger.inside = ParticleSystemOverlapAction.Ignore;
                    trigger.outside = ParticleSystemOverlapAction.Ignore;
                    trigger.enter = ParticleSystemOverlapAction.Callback;
                    trigger.exit = ParticleSystemOverlapAction.Ignore;
                    List<WaterVolume> list = VolumeManager<WaterVolume, WaterVolumeManager>.Get().InternalGetAllVolumes();
                    for (int i = 0; i < list.Count; i++)
                    {
                        trigger.SetCollider(i, list[i].volumeCollider);
                    }
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        thirdShellRenderer.forceRenderingOff = true;
                    }
                }
            }
        }
        if (thirdAttachments.barrelHook != null)
        {
            EffectAsset effectAsset4 = equippedGunAsset.FindMuzzleEffectAsset();
            if (effectAsset4 != null)
            {
                Transform transform6 = EffectManager.InstantiateFromPool(effectAsset4).transform;
                transform6.name = "Emitter";
                transform6.parent = (equippedGunAsset.isTurret ? null : thirdAttachments.barrelHook);
                transform6.localPosition = Vector3.zero;
                transform6.localRotation = Quaternion.identity;
                thirdMuzzleEmitter = transform6.GetComponent<ParticleSystem>();
                Light component2 = transform6.GetComponent<Light>();
                if (component2 != null)
                {
                    component2.enabled = false;
                    component2.cullingMask = -2049;
                }
            }
            if (base.channel.IsLocalPlayer && effectAsset4 != null)
            {
                firstFakeLight = UnityEngine.Object.Instantiate(effectAsset4.effect).transform;
                firstFakeLight.name = "Emitter";
                Light component3 = firstFakeLight.GetComponent<Light>();
                if (component3 != null)
                {
                    component3.enabled = false;
                    component3.cullingMask = -2049;
                }
            }
        }
        ammo = base.player.equipment.state[10];
        firemode = (EFiremode)base.player.equipment.state[11];
        interact = base.player.equipment.state[12] == 1;
        updateAttachments(wasMagazineModelVisible: true);
        startedReload = float.MaxValue;
        startedHammer = float.MaxValue;
        if (base.channel.IsLocalPlayer)
        {
            if (firemode == EFiremode.SAFETY)
            {
                PlayerUI.message(EPlayerMessage.SAFETY, "");
            }
            else if (ammo < equippedGunAsset.ammoPerShot)
            {
                PlayerUI.message(EPlayerMessage.RELOAD, "");
            }
            if (firstAttachments.reticuleHook != null)
            {
                originalReticuleHookLocalPosition = firstAttachments.reticuleHook.localPosition;
            }
            else
            {
                originalReticuleHookLocalPosition = Vector3.zero;
            }
            localization = Localization.read("/Player/Useable/PlayerUseableGun.dat");
            if (icons != null)
            {
                icons.unload();
            }
            icons = Bundles.getBundle("/Bundles/Textures/Player/Icons/Useable/PlayerUseableGun/PlayerUseableGun.unity3d");
            if (equippedGunAsset.hasSight)
            {
                sightButton = new SleekButtonIcon(icons.load<Texture2D>("Sight"));
                sightButton.PositionOffset_X = -25f;
                sightButton.PositionOffset_Y = -25f;
                sightButton.SizeOffset_X = 50f;
                sightButton.SizeOffset_Y = 50f;
                sightButton.onClickedButton += onClickedSightHookButton;
                PlayerUI.container.AddChild(sightButton);
                sightButton.IsVisible = false;
            }
            if (equippedGunAsset.hasTactical)
            {
                tacticalButton = new SleekButtonIcon(icons.load<Texture2D>("Tactical"));
                tacticalButton.PositionOffset_X = -25f;
                tacticalButton.PositionOffset_Y = -25f;
                tacticalButton.SizeOffset_X = 50f;
                tacticalButton.SizeOffset_Y = 50f;
                tacticalButton.onClickedButton += onClickedTacticalHookButton;
                PlayerUI.container.AddChild(tacticalButton);
                tacticalButton.IsVisible = false;
            }
            if (equippedGunAsset.hasGrip)
            {
                gripButton = new SleekButtonIcon(icons.load<Texture2D>("Grip"));
                gripButton.PositionOffset_X = -25f;
                gripButton.PositionOffset_Y = -25f;
                gripButton.SizeOffset_X = 50f;
                gripButton.SizeOffset_Y = 50f;
                gripButton.onClickedButton += onClickedGripHookButton;
                PlayerUI.container.AddChild(gripButton);
                gripButton.IsVisible = false;
            }
            if (equippedGunAsset.hasBarrel)
            {
                barrelButton = new SleekButtonIcon(icons.load<Texture2D>("Barrel"));
                barrelButton.PositionOffset_X = -25f;
                barrelButton.PositionOffset_Y = -25f;
                barrelButton.SizeOffset_X = 50f;
                barrelButton.SizeOffset_Y = 50f;
                barrelButton.onClickedButton += onClickedBarrelHookButton;
                PlayerUI.container.AddChild(barrelButton);
                barrelButton.IsVisible = false;
                barrelQualityLabel = Glazier.Get().CreateLabel();
                barrelQualityLabel.PositionOffset_Y = -30f;
                barrelQualityLabel.PositionScale_Y = 1f;
                barrelQualityLabel.SizeOffset_Y = 30f;
                barrelQualityLabel.SizeScale_X = 1f;
                barrelQualityLabel.TextAlignment = TextAnchor.LowerLeft;
                barrelQualityLabel.FontSize = ESleekFontSize.Small;
                barrelButton.AddChild(barrelQualityLabel);
                barrelQualityLabel.IsVisible = false;
                barrelQualityImage = Glazier.Get().CreateImage();
                barrelQualityImage.PositionOffset_X = -15f;
                barrelQualityImage.PositionOffset_Y = -15f;
                barrelQualityImage.PositionScale_X = 1f;
                barrelQualityImage.PositionScale_Y = 1f;
                barrelQualityImage.SizeOffset_X = 10f;
                barrelQualityImage.SizeOffset_Y = 10f;
                barrelQualityImage.Texture = PlayerDashboardInventoryUI.icons.load<Texture2D>("Quality_1");
                barrelButton.AddChild(barrelQualityImage);
                barrelQualityImage.IsVisible = false;
            }
            if (equippedGunAsset.allowMagazineChange)
            {
                magazineButton = new SleekButtonIcon(icons.load<Texture2D>("Magazine"));
                magazineButton.PositionOffset_X = -25f;
                magazineButton.PositionOffset_Y = -25f;
                magazineButton.SizeOffset_X = 50f;
                magazineButton.SizeOffset_Y = 50f;
                magazineButton.onClickedButton += onClickedMagazineHookButton;
                PlayerUI.container.AddChild(magazineButton);
                magazineButton.IsVisible = false;
                magazineQualityLabel = Glazier.Get().CreateLabel();
                magazineQualityLabel.PositionOffset_Y = -30f;
                magazineQualityLabel.PositionScale_Y = 1f;
                magazineQualityLabel.SizeOffset_Y = 30f;
                magazineQualityLabel.SizeScale_X = 1f;
                magazineQualityLabel.TextAlignment = TextAnchor.LowerLeft;
                magazineQualityLabel.FontSize = ESleekFontSize.Small;
                magazineQualityLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
                magazineButton.AddChild(magazineQualityLabel);
                magazineQualityLabel.IsVisible = false;
                magazineQualityImage = Glazier.Get().CreateImage();
                magazineQualityImage.PositionOffset_X = -15f;
                magazineQualityImage.PositionOffset_Y = -15f;
                magazineQualityImage.PositionScale_X = 1f;
                magazineQualityImage.PositionScale_Y = 1f;
                magazineQualityImage.SizeOffset_X = 10f;
                magazineQualityImage.SizeOffset_Y = 10f;
                magazineQualityImage.Texture = PlayerDashboardInventoryUI.icons.load<Texture2D>("Quality_1");
                magazineButton.AddChild(magazineQualityImage);
                magazineQualityImage.IsVisible = false;
            }
            icons.unload();
            infoBox = Glazier.Get().CreateBox();
            infoBox.PositionOffset_Y = -70f;
            infoBox.PositionScale_X = 0.7f;
            infoBox.PositionScale_Y = 1f;
            infoBox.SizeOffset_Y = 70f;
            infoBox.SizeScale_X = 0.3f;
            PlayerLifeUI.container.AddChild(infoBox);
            ammoLabel = Glazier.Get().CreateLabel();
            ammoLabel.SizeScale_X = 0.35f;
            ammoLabel.SizeScale_Y = 1f;
            ammoLabel.FontSize = ESleekFontSize.Large;
            infoBox.AddChild(ammoLabel);
            firemodeLabel = Glazier.Get().CreateLabel();
            firemodeLabel.PositionOffset_Y = 5f;
            firemodeLabel.PositionScale_X = 0.35f;
            firemodeLabel.SizeScale_X = 0.65f;
            firemodeLabel.SizeScale_Y = 0.5f;
            infoBox.AddChild(firemodeLabel);
            attachLabel = Glazier.Get().CreateLabel();
            attachLabel.PositionOffset_Y = -5f;
            attachLabel.PositionScale_X = 0.35f;
            attachLabel.PositionScale_Y = 0.5f;
            attachLabel.SizeScale_X = 0.65f;
            attachLabel.SizeScale_Y = 0.5f;
            attachLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            infoBox.AddChild(attachLabel);
            base.player.onLocalPluginWidgetFlagsChanged += OnLocalPluginWidgetFlagsChanged;
            UpdateInfoBoxVisibility();
            updateInfo();
        }
        base.player.animator.play("Equip", smooth: false);
        if (base.player.channel.IsLocalPlayer)
        {
            PlayerUI.disableDot();
            PlayerStance stance = base.player.stance;
            stance.onStanceUpdated = (StanceUpdated)Delegate.Combine(stance.onStanceUpdated, new StanceUpdated(UpdateCrosshairEnabled));
            PlayerLook look = base.player.look;
            look.onPerspectiveUpdated = (PerspectiveUpdated)Delegate.Combine(look.onPerspectiveUpdated, new PerspectiveUpdated(onPerspectiveUpdated));
            OptionsSettings.OnUnitSystemChanged += SyncScopeDistanceMarkerText;
        }
        if ((base.channel.IsLocalPlayer || Provider.isServer) && equippedGunAsset.projectile == null)
        {
            bullets = new List<BulletInfo>();
        }
        aimAccuracy = 0;
        steadyAccuracy = 0u;
        canSteady = true;
        swayTime = Time.time;
    }

    public override void dequip()
    {
        if (infoBox != null)
        {
            if (sightButton != null)
            {
                PlayerUI.container.RemoveChild(sightButton);
            }
            if (tacticalButton != null)
            {
                PlayerUI.container.RemoveChild(tacticalButton);
            }
            if (gripButton != null)
            {
                PlayerUI.container.RemoveChild(gripButton);
            }
            if (barrelButton != null)
            {
                PlayerUI.container.RemoveChild(barrelButton);
            }
            if (magazineButton != null)
            {
                PlayerUI.container.RemoveChild(magazineButton);
            }
            if (rangeLabel != null)
            {
                rangeLabel.Parent.RemoveChild(rangeLabel);
            }
            PlayerLifeUI.container.RemoveChild(infoBox);
            base.player.onLocalPluginWidgetFlagsChanged -= OnLocalPluginWidgetFlagsChanged;
        }
        base.player.disableItemSpotLight();
        if (base.channel.IsLocalPlayer)
        {
            base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
            base.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.zero;
            if (gunshotAudioSource != null)
            {
                UnityEngine.Object.Destroy(gunshotAudioSource);
            }
            if (whir != null)
            {
                UnityEngine.Object.Destroy(whir);
            }
            DestroyLaser();
            if (isAiming)
            {
                stopAim();
            }
            if (isAttaching)
            {
                stopAttach();
            }
            PlayerUI.isLocked = false;
            if (isAttaching)
            {
                PlayerLifeUI.open();
            }
            if (base.player.movement.getVehicle() == null)
            {
                PlayerUI.enableDot();
            }
            PlayerUI.disableCrosshair();
            base.player.look.disableScope();
            PlayerStance stance = base.player.stance;
            stance.onStanceUpdated = (StanceUpdated)Delegate.Remove(stance.onStanceUpdated, new StanceUpdated(UpdateCrosshairEnabled));
            PlayerLook look = base.player.look;
            look.onPerspectiveUpdated = (PerspectiveUpdated)Delegate.Remove(look.onPerspectiveUpdated, new PerspectiveUpdated(onPerspectiveUpdated));
            OptionsSettings.OnUnitSystemChanged -= SyncScopeDistanceMarkerText;
            if (firstFakeLight != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight.gameObject);
            }
            if (firstFakeLight_0 != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight_0.gameObject);
            }
            if (firstFakeLight_1 != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight_1.gameObject);
            }
        }
        if (tracerEmitter != null)
        {
            EffectManager.DestroyIntoPool(tracerEmitter.gameObject);
            tracerEmitter = null;
        }
        if (firstMuzzleEmitter != null)
        {
            EffectManager.DestroyIntoPool(firstMuzzleEmitter.gameObject);
            firstMuzzleEmitter = null;
        }
        if (firstShellEmitter != null)
        {
            EffectManager.DestroyIntoPool(firstShellEmitter.gameObject);
            firstShellEmitter = null;
        }
        if (thirdMuzzleEmitter != null)
        {
            EffectManager.DestroyIntoPool(thirdMuzzleEmitter.gameObject);
            thirdMuzzleEmitter = null;
        }
        if (thirdShellEmitter != null)
        {
            if (base.channel.IsLocalPlayer)
            {
                ParticleSystem.CollisionModule collision = thirdShellEmitter.collision;
                collision.enabled = false;
                ParticleSystem.TriggerModule trigger = thirdShellEmitter.trigger;
                trigger.enabled = false;
            }
            if (thirdShellRenderer != null)
            {
                thirdShellRenderer.forceRenderingOff = false;
            }
            EffectManager.DestroyIntoPool(thirdShellEmitter.gameObject);
            thirdShellEmitter = null;
        }
    }

    public override void tick()
    {
        if (base.channel.IsLocalPlayer && firstAttachments.rope != null)
        {
            if (firstAttachments.leftHook != null)
            {
                firstAttachments.rope.SetPosition(0, firstAttachments.leftHook.position);
            }
            if (firstAttachments.nockHook != null)
            {
                if (firstAttachments.magazineModel != null && firstAttachments.magazineModel.gameObject.activeSelf)
                {
                    firstAttachments.rope.SetPosition(1, firstAttachments.nockHook.position);
                }
                else if (firstAttachments.restHook != null)
                {
                    firstAttachments.rope.SetPosition(1, firstAttachments.restHook.position);
                }
            }
            else if (isAiming)
            {
                firstAttachments.rope.SetPosition(1, base.player.equipment.firstRightHook.position);
            }
            else if ((isAttaching || isSprinting || base.player.equipment.isInspecting) && firstAttachments.magazineModel != null && firstAttachments.magazineModel.gameObject.activeSelf && firstAttachments.restHook != null)
            {
                firstAttachments.rope.SetPosition(1, firstAttachments.restHook.position);
            }
            else if (firstAttachments.leftHook != null)
            {
                firstAttachments.rope.SetPosition(1, firstAttachments.leftHook.position);
            }
            if (firstAttachments.rightHook != null)
            {
                firstAttachments.rope.SetPosition(2, firstAttachments.rightHook.position);
            }
        }
        if (!base.player.equipment.IsEquipAnimationFinished)
        {
            return;
        }
        if ((double)(Time.realtimeSinceStartup - lastShot) > 0.05)
        {
            if (firstMuzzleEmitter != null)
            {
                Light component = firstMuzzleEmitter.GetComponent<Light>();
                if ((bool)component)
                {
                    component.enabled = false;
                }
            }
            if (thirdMuzzleEmitter != null)
            {
                Light component2 = thirdMuzzleEmitter.GetComponent<Light>();
                if ((bool)component2)
                {
                    component2.enabled = false;
                }
            }
            if (firstFakeLight != null)
            {
                Light component3 = firstFakeLight.GetComponent<Light>();
                if ((bool)component3)
                {
                    component3.enabled = false;
                }
            }
        }
        if ((base.player.stance.stance == EPlayerStance.SPRINT && base.player.movement.isMoving) || firemode == EFiremode.SAFETY)
        {
            if (!isShooting && !isSprinting && !isReloading && !isHammering && !isUnjamming && !isAttaching && !isAiming && !needsRechamber)
            {
                isSprinting = true;
                base.player.animator.play("Sprint_Start", smooth: false);
            }
        }
        else if (isSprinting)
        {
            isSprinting = false;
            if (!isAiming)
            {
                base.player.animator.play("Sprint_Stop", smooth: false);
            }
        }
        if (base.channel.IsLocalPlayer)
        {
            if (InputEx.GetKeyUp(ControlsSettings.attach) && isAttaching)
            {
                isAttaching = false;
                base.player.animator.play("Attach_Stop", smooth: false);
                stopAttach();
            }
            if (InputEx.GetKeyDown(ControlsSettings.tactical))
            {
                fireTacticalInput = true;
            }
            if (!PlayerUI.window.showCursor)
            {
                if (InputEx.ConsumeKeyDown(ControlsSettings.attach) && !isShooting && !isAttaching && !isSprinting && !isReloading && !isHammering && !isUnjamming && !isAiming && !needsRechamber)
                {
                    isAttaching = true;
                    base.player.animator.play("Attach_Start", smooth: false);
                    updateAttach();
                    startAttach();
                }
                if (InputEx.GetKeyDown(ControlsSettings.reload) && !isShooting && !isReloading && !isHammering && !isUnjamming && !isSprinting && !isAttaching && !isAiming && !needsRechamber)
                {
                    bool allowZeroCaliber = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
                    magazineSearch = base.player.inventory.search(EItemType.MAGAZINE, equippedGunAsset.magazineCalibers, allowZeroCaliber);
                    if (magazineSearch.Count > 0)
                    {
                        byte b = 0;
                        byte b2 = byte.MaxValue;
                        for (byte b3 = 0; b3 < magazineSearch.Count; b3++)
                        {
                            if (magazineSearch[b3].jar.item.amount > b)
                            {
                                b = magazineSearch[b3].jar.item.amount;
                                b2 = b3;
                            }
                        }
                        if (b2 != byte.MaxValue)
                        {
                            ItemAsset asset = magazineSearch[b2].GetAsset();
                            if (asset != null)
                            {
                                SendAttachMagazine.Invoke(GetNetId(), ENetReliability.Unreliable, magazineSearch[b2].page, magazineSearch[b2].jar.x, magazineSearch[b2].jar.y, asset.hash);
                            }
                        }
                    }
                }
                if (InputEx.GetKeyDown(ControlsSettings.firemode) && !isAiming)
                {
                    if (firemode == EFiremode.SAFETY)
                    {
                        if (equippedGunAsset.hasSemi)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                        else if (equippedGunAsset.hasAuto)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                        else if (equippedGunAsset.hasBurst)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        PlayerUI.message(EPlayerMessage.NONE, "");
                    }
                    else if (firemode == EFiremode.SEMI)
                    {
                        if (equippedGunAsset.hasAuto)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                        else if (equippedGunAsset.hasBurst)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        else if (equippedGunAsset.hasSafety)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                    }
                    else if (firemode == EFiremode.AUTO)
                    {
                        if (equippedGunAsset.hasBurst)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        else if (equippedGunAsset.hasSafety)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                        else if (equippedGunAsset.hasSemi)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                    }
                    else if (firemode == EFiremode.BURST)
                    {
                        if (equippedGunAsset.hasSafety)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                        else if (equippedGunAsset.hasSemi)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                        else if (equippedGunAsset.hasAuto)
                        {
                            SendChangeFiremode.Invoke(GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                    }
                }
            }
            if (isAttaching)
            {
                if (sightButton != null)
                {
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        Vector3 vector = base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.sightHook.position + firstAttachments.sightHook.up * 0.05f + firstAttachments.sightHook.forward * 0.05f);
                        Vector2 vector2 = PlayerUI.container.ViewportToNormalizedPosition(vector);
                        sightButton.PositionScale_X = vector2.x;
                        sightButton.PositionScale_Y = vector2.y;
                    }
                    else
                    {
                        sightButton.PositionScale_X = 0.667f;
                        sightButton.PositionScale_Y = 0.75f;
                    }
                }
                if (tacticalButton != null)
                {
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        Vector3 vector3 = base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.tacticalHook.position);
                        Vector2 vector4 = PlayerUI.container.ViewportToNormalizedPosition(vector3);
                        tacticalButton.PositionScale_X = vector4.x;
                        tacticalButton.PositionScale_Y = vector4.y;
                    }
                    else
                    {
                        tacticalButton.PositionScale_X = 0.5f;
                        tacticalButton.PositionScale_Y = 0.25f;
                    }
                }
                if (gripButton != null)
                {
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        Vector3 vector5 = base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.gripHook.position + firstAttachments.gripHook.forward * -0.05f);
                        Vector2 vector6 = PlayerUI.container.ViewportToNormalizedPosition(vector5);
                        gripButton.PositionScale_X = vector6.x;
                        gripButton.PositionScale_Y = vector6.y;
                    }
                    else
                    {
                        gripButton.PositionScale_X = 0.75f;
                        gripButton.PositionScale_Y = 0.25f;
                    }
                }
                if (barrelButton != null)
                {
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        Vector3 vector7 = base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.barrelHook.position + firstAttachments.barrelHook.up * 0.05f);
                        Vector2 vector8 = PlayerUI.container.ViewportToNormalizedPosition(vector7);
                        barrelButton.PositionScale_X = vector8.x;
                        barrelButton.PositionScale_Y = vector8.y;
                    }
                    else
                    {
                        barrelButton.PositionScale_X = 0.25f;
                        barrelButton.PositionScale_Y = 0.25f;
                    }
                }
                if (magazineButton != null)
                {
                    if (base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
                    {
                        Vector2 viewportPosition = base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.magazineHook.position + firstAttachments.magazineHook.forward * -0.1f);
                        Vector2 vector9 = PlayerUI.container.ViewportToNormalizedPosition(viewportPosition);
                        magazineButton.PositionScale_X = vector9.x;
                        magazineButton.PositionScale_Y = vector9.y;
                    }
                    else
                    {
                        magazineButton.PositionScale_X = 0.334f;
                        magazineButton.PositionScale_Y = 0.75f;
                    }
                }
            }
            if (rangeLabel != null)
            {
                if (PlayerLifeUI.scopeOverlay.IsVisible)
                {
                    rangeLabel.PositionOffset_X = -300f;
                    rangeLabel.PositionOffset_Y = 100f;
                    rangeLabel.PositionScale_X = 0.5f;
                    rangeLabel.PositionScale_Y = 0.5f;
                    rangeLabel.TextAlignment = TextAnchor.UpperRight;
                }
                else
                {
                    Vector3 vector10 = ((base.player.look.perspective == EPlayerPerspective.FIRST && firstAttachments.lightHook != null) ? base.player.animator.viewmodelCamera.WorldToViewportPoint(firstAttachments.lightHook.position) : ((!(thirdAttachments.lightHook != null)) ? Vector3.zero : MainCamera.instance.WorldToViewportPoint(thirdAttachments.lightHook.position)));
                    Vector2 vector11 = PlayerLifeUI.container.ViewportToNormalizedPosition(vector10);
                    rangeLabel.PositionOffset_X = -100f;
                    rangeLabel.PositionOffset_Y = -15f;
                    rangeLabel.PositionScale_X = vector11.x;
                    rangeLabel.PositionScale_Y = vector11.y;
                    rangeLabel.TextAlignment = TextAnchor.MiddleCenter;
                }
                rangeLabel.IsVisible = true;
            }
        }
        if (needsRechamber && Time.realtimeSinceStartup - lastShot > 0.25f && !isAiming)
        {
            needsRechamber = false;
            base.player.equipment.isBusy = false;
            lastRechamber = Time.realtimeSinceStartup;
            needsEject = true;
            hammer();
        }
        if (needsEject && Time.realtimeSinceStartup - lastRechamber > 0.45f)
        {
            needsEject = false;
            if (firstShellEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
            {
                firstShellEmitter.Emit(1);
            }
            if (thirdShellEmitter != null)
            {
                thirdShellEmitter.Emit(1);
            }
        }
        if (needsUnload && Time.realtimeSinceStartup - startedReload > 0.5f)
        {
            needsUnload = false;
            if (firstShellEmitter != null && base.player.look.perspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret)
            {
                firstShellEmitter.Emit(equippedGunAsset.ammoMax);
            }
            if (thirdShellEmitter != null)
            {
                thirdShellEmitter.Emit(equippedGunAsset.ammoMax);
            }
        }
        if (needsUnplace && Time.realtimeSinceStartup - startedReload > reloadTime * equippedGunAsset.unplace)
        {
            needsUnplace = false;
            if (base.channel.IsLocalPlayer && firstAttachments.magazineModel != null)
            {
                firstAttachments.magazineModel.gameObject.SetActive(value: false);
            }
            if (thirdAttachments.magazineModel != null)
            {
                thirdAttachments.magazineModel.gameObject.SetActive(value: false);
            }
        }
        if (needsReplace && Time.realtimeSinceStartup - startedReload > reloadTime * equippedGunAsset.replace)
        {
            needsReplace = false;
            if (base.channel.IsLocalPlayer && firstAttachments.magazineModel != null)
            {
                firstAttachments.magazineModel.gameObject.SetActive(value: true);
            }
            if (thirdAttachments.magazineModel != null)
            {
                thirdAttachments.magazineModel.gameObject.SetActive(value: true);
            }
        }
        if (isReloading && Time.realtimeSinceStartup - startedReload > reloadTime)
        {
            isReloading = false;
            if (needsHammer)
            {
                hammer();
            }
            else
            {
                base.player.equipment.isBusy = false;
            }
        }
        if (isHammering && Time.realtimeSinceStartup - startedHammer > hammerTime)
        {
            isHammering = false;
            base.player.equipment.isBusy = false;
        }
        if (isUnjamming && Time.realtimeSinceStartup - startedUnjammingChamber > unjamChamberDuration)
        {
            isUnjamming = false;
            base.player.equipment.isBusy = false;
        }
    }

    public override void simulate(uint simulation, bool inputSteady)
    {
        if (isFired && Time.realtimeSinceStartup - lastShot > 0.15f)
        {
            isFired = false;
            if (!needsRechamber)
            {
                base.player.equipment.isBusy = false;
            }
        }
        if (!canSteady && !inputSteady && base.player.life.oxygen > 10)
        {
            canSteady = true;
        }
        if (isAiming && thirdAttachments.sightAsset != null && thirdAttachments.sightAsset.zoom > 2f && base.player.life.oxygen > 0 && canSteady && inputSteady)
        {
            if (steadyAccuracy < 4)
            {
                steadyAccuracy++;
            }
            base.player.life.askSuffocate((byte)(5 - base.player.skills.skills[0][5].level / 2));
            if (base.player.life.oxygen == 0)
            {
                canSteady = false;
            }
        }
        else if (steadyAccuracy != 0)
        {
            steadyAccuracy--;
        }
        if (base.channel.IsLocalPlayer && base.player.equipment.IsEquipAnimationFinished && fireTacticalInput)
        {
            if (!isReloading && !isHammering && !isUnjamming && !needsRechamber && thirdAttachments.tacticalAsset != null)
            {
                if (thirdAttachments.tacticalAsset.isMelee)
                {
                    if (!isSprinting && (!base.player.movement.isSafe || !base.player.movement.isSafeInfo.noWeapons) && firemode != 0)
                    {
                        if (!Provider.isServer)
                        {
                            isJabbing = true;
                        }
                        base.player.input.keys[8] = true;
                    }
                }
                else if (thirdAttachments.tacticalAsset.isLight || thirdAttachments.tacticalAsset.isLaser || thirdAttachments.tacticalAsset.isRangefinder)
                {
                    base.player.input.keys[8] = true;
                }
            }
            fireTacticalInput = false;
        }
        if (Provider.isServer && base.player.input.keys[8])
        {
            askInteractGun();
        }
    }

    private void tockShoot(uint clock)
    {
        if ((firemode == EFiremode.SAFETY) | isReloading | isHammering | isUnjamming | (base.player.stance.stance == EPlayerStance.SPRINT && !equippedGunAsset.canAimDuringSprint) | isAttaching | (!base.player.equipment.asset.canUseUnderwater && (base.player.stance.isSubmerged || base.player.stance.stance == EPlayerStance.SWIM)))
        {
            bursts = 0;
            fireDelayCounter = 0;
            isShooting = false;
            wasTriggerJustPulled = false;
            return;
        }
        bool flag = isShooting || wasTriggerJustPulled;
        wasTriggerJustPulled = false;
        if (fireDelayCounter > 1)
        {
            fireDelayCounter--;
            return;
        }
        if (fireDelayCounter > 0)
        {
            fireDelayCounter = 0;
            flag = true;
        }
        if (firemode == EFiremode.SEMI)
        {
            isShooting = false;
        }
        if (firemode == EFiremode.BURST)
        {
            isShooting = false;
            if (flag)
            {
                bursts += equippedGunAsset.bursts;
            }
        }
        int num = equippedGunAsset.firerate;
        if (thirdAttachments.sightAsset != null)
        {
            num -= thirdAttachments.sightAsset.FirerateOffset;
        }
        if (thirdAttachments.tacticalAsset != null && shouldEnableTacticalStats)
        {
            num -= thirdAttachments.tacticalAsset.FirerateOffset;
        }
        if (thirdAttachments.gripAsset != null)
        {
            num -= thirdAttachments.gripAsset.FirerateOffset;
        }
        if (thirdAttachments.barrelAsset != null)
        {
            num -= thirdAttachments.barrelAsset.FirerateOffset;
        }
        if (thirdAttachments.magazineAsset != null)
        {
            num -= thirdAttachments.magazineAsset.FirerateOffset;
        }
        num = Mathf.Max(num, 0);
        if (clock - lastFire <= num)
        {
            return;
        }
        if (bursts > 0)
        {
            bursts--;
        }
        if (ammo >= equippedGunAsset.ammoPerShot)
        {
            isFired = true;
            lastFire = clock;
            base.player.equipment.isBusy = true;
            fire();
            return;
        }
        if (Provider.isServer)
        {
            EffectManager.TriggerFiremodeEffect(base.transform.position);
        }
        bursts = 0;
        isShooting = false;
    }

    private void tockJab(uint clock)
    {
        isJabbing = false;
        if (clock - lastJab > 25)
        {
            lastJab = clock;
            jab();
        }
    }

    public override void tock(uint clock)
    {
        if (isShooting || wasTriggerJustPulled || bursts > 0 || fireDelayCounter > 0)
        {
            tockShoot(clock);
        }
        if (isJabbing)
        {
            tockJab(clock);
        }
        ballistics();
        if (isAiming)
        {
            if (aimAccuracy < maxAimingAccuracy)
            {
                aimAccuracy++;
            }
        }
        else if (aimAccuracy > 0)
        {
            aimAccuracy--;
        }
    }

    public override void updateState(byte[] newState)
    {
        ammo = newState[10];
        firemode = (EFiremode)newState[11];
        interact = newState[12] == 1;
        bool wasMagazineModelVisible = thirdAttachments.magazineModel != null && thirdAttachments.magazineModel.gameObject.activeSelf;
        if (base.channel.IsLocalPlayer)
        {
            firstAttachments.updateAttachments(newState, viewmodel: true);
        }
        thirdAttachments.updateAttachments(newState, viewmodel: false);
        updateAttachments(wasMagazineModelVisible);
        if (base.channel.IsLocalPlayer)
        {
            if (firstAttachments.reticuleHook != null)
            {
                originalReticuleHookLocalPosition = firstAttachments.reticuleHook.localPosition;
            }
            else
            {
                originalReticuleHookLocalPosition = Vector3.zero;
            }
        }
        if (infoBox != null)
        {
            if (isAttaching)
            {
                updateAttach();
            }
            updateInfo();
        }
    }

    private void updateAnimationSpeeds(float speed)
    {
        base.player.animator.setAnimationSpeed("Reload", speed);
        reloadTime = base.player.animator.GetAnimationLength("Reload");
        reloadTime = Mathf.Max(reloadTime, equippedGunAsset.reloadTime / speed);
        base.player.animator.setAnimationSpeed("Hammer", speed);
        hammerTime = base.player.animator.GetAnimationLength("Hammer");
        base.player.animator.setAnimationSpeed("Scope", speed);
        hammerTime = Mathf.Max(hammerTime, equippedGunAsset.hammerTime / speed);
        unjamChamberDuration = base.player.animator.GetAnimationLength(equippedGunAsset.unjamChamberAnimName);
    }

    private void updateAttachments(bool wasMagazineModelVisible)
    {
        if (base.channel.IsLocalPlayer)
        {
            ClientAssetIntegrity.QueueRequest(firstAttachments.sightAsset);
            ClientAssetIntegrity.QueueRequest(firstAttachments.tacticalAsset);
            ClientAssetIntegrity.QueueRequest(firstAttachments.gripAsset);
            ClientAssetIntegrity.QueueRequest(firstAttachments.barrelAsset);
            ClientAssetIntegrity.QueueRequest(firstAttachments.magazineAsset);
            if (firstAttachments.tacticalAsset != null)
            {
                if (firstAttachments.tacticalAsset.isLaser)
                {
                    if (!wasLaser)
                    {
                        PlayerUI.message(EPlayerMessage.LASER, "");
                    }
                    wasLaser = true;
                }
                else
                {
                    wasLaser = false;
                }
                if (firstAttachments.tacticalAsset.isLight)
                {
                    if (!wasLight)
                    {
                        PlayerUI.message(EPlayerMessage.LIGHT, "");
                    }
                    wasLight = true;
                }
                else
                {
                    wasLight = false;
                }
                if (firstAttachments.tacticalAsset.isRangefinder)
                {
                    if (!wasRange)
                    {
                        PlayerUI.message(EPlayerMessage.RANGEFINDER, "");
                    }
                    wasRange = true;
                }
                else
                {
                    wasRange = false;
                }
                if (firstAttachments.tacticalAsset.isMelee)
                {
                    if (!wasBayonet)
                    {
                        PlayerUI.message(EPlayerMessage.BAYONET, "");
                    }
                    wasBayonet = true;
                }
                else
                {
                    wasBayonet = false;
                }
            }
            else
            {
                wasLaser = false;
                wasLight = false;
                wasRange = false;
                wasBayonet = false;
            }
            ClearScopeDistanceMarkers();
            if (equippedGunAsset.projectile == null && firstAttachments.sightAsset != null && firstAttachments.sightAsset.distanceMarkers != null && firstAttachments.sightAsset.distanceMarkers.Count > 0)
            {
                InstantiateScopeDistanceMarkers();
            }
            if (firstAttachments.tacticalAsset != null && firstAttachments.tacticalAsset.isLaser && interact)
            {
                if (laserGameObject == null)
                {
                    laserGameObject = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Guns/Laser"));
                    laserTransform = laserGameObject.transform;
                    laserTransform.name = "Laser";
                    laserTransform.position = Vector3.zero;
                    laserTransform.rotation = Quaternion.identity;
                    laserMaterial = laserGameObject.GetComponent<Renderer>().material;
                }
                laserMaterial.SetColor("_Color", firstAttachments.tacticalAsset.laserColor);
                laserMaterial.SetColor("_EmissionColor", firstAttachments.tacticalAsset.laserColor * 2f);
            }
            else if (laserGameObject != null)
            {
                DestroyLaser();
            }
            if (firstAttachments.tacticalAsset != null && firstAttachments.tacticalAsset.isRangefinder && interact)
            {
                if (rangeLabel == null)
                {
                    rangeLabel = Glazier.Get().CreateLabel();
                    rangeLabel.SizeOffset_X = 200f;
                    rangeLabel.SizeOffset_Y = 30f;
                    rangeLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
                    PlayerUI.window.AddChild(rangeLabel);
                    rangeLabel.IsVisible = false;
                }
            }
            else if (rangeLabel != null)
            {
                rangeLabel.Parent.RemoveChild(rangeLabel);
                rangeLabel = null;
            }
            if (firstFakeLight_0 != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight_0.gameObject);
                firstFakeLight_0 = null;
            }
            if (thirdAttachments.lightHook != null)
            {
                Transform transform = thirdAttachments.lightHook.Find("Light");
                if (transform != null)
                {
                    firstFakeLight_0 = UnityEngine.Object.Instantiate(transform.gameObject).transform;
                    firstFakeLight_0.name = "Emitter";
                }
            }
            if (firstFakeLight_1 != null)
            {
                UnityEngine.Object.Destroy(firstFakeLight_1.gameObject);
                firstFakeLight_1 = null;
            }
            if (thirdAttachments.light2Hook != null)
            {
                Transform transform2 = thirdAttachments.light2Hook.Find("Light");
                if (transform2 != null)
                {
                    firstFakeLight_1 = UnityEngine.Object.Instantiate(transform2.gameObject).transform;
                    firstFakeLight_1.name = "Emitter";
                }
            }
        }
        if (firstMuzzleEmitter != null)
        {
            if (firstAttachments.barrelModel != null)
            {
                Transform transform3 = firstAttachments.barrelModel.Find("Muzzle");
                if (transform3 != null)
                {
                    firstMuzzleEmitter.transform.position = transform3.position;
                }
                else
                {
                    firstMuzzleEmitter.transform.localPosition = Vector3.up * 0.25f;
                }
            }
            else
            {
                firstMuzzleEmitter.transform.localPosition = Vector3.zero;
            }
        }
        if (thirdMuzzleEmitter != null)
        {
            if (thirdAttachments.barrelModel != null)
            {
                Transform transform4 = thirdAttachments.barrelModel.Find("Muzzle");
                if (transform4 != null)
                {
                    thirdMuzzleEmitter.transform.position = transform4.position;
                }
                else
                {
                    thirdMuzzleEmitter.transform.localPosition = Vector3.up * 0.25f;
                }
            }
            else
            {
                thirdMuzzleEmitter.transform.localPosition = Vector3.zero;
            }
        }
        if (thirdAttachments?.magazineAsset != null)
        {
            EffectAsset effectAsset = thirdAttachments.magazineAsset.FindTracerEffectAsset();
            if (currentTracerEffectAsset != effectAsset)
            {
                if (tracerEmitter != null)
                {
                    EffectManager.DestroyIntoPool(tracerEmitter.gameObject);
                    tracerEmitter = null;
                }
                currentTracerEffectAsset = effectAsset;
                if (effectAsset != null)
                {
                    Transform transform5 = EffectManager.InstantiateFromPool(effectAsset).transform;
                    transform5.name = "Tracer";
                    transform5.localPosition = Vector3.zero;
                    transform5.localRotation = Quaternion.identity;
                    tracerEmitter = transform5.GetComponent<ParticleSystem>();
                }
            }
        }
        if (base.channel.IsLocalPlayer && firstAttachments.magazineModel != null)
        {
            firstAttachments.magazineModel.gameObject.SetActive(wasMagazineModelVisible);
        }
        if (thirdAttachments.magazineModel != null)
        {
            thirdAttachments.magazineModel.gameObject.SetActive(wasMagazineModelVisible);
        }
        if (!Dedicator.IsDedicatedServer && thirdAttachments.tacticalAsset != null)
        {
            if (thirdAttachments.tacticalAsset.isLight || thirdAttachments.tacticalAsset.isLaser)
            {
                if (base.channel.IsLocalPlayer && firstAttachments.lightHook != null)
                {
                    firstAttachments.lightHook.gameObject.SetActive(interact);
                }
                if (thirdAttachments.lightHook != null)
                {
                    thirdAttachments.lightHook.gameObject.SetActive(interact);
                }
                if (firstFakeLight_0 != null)
                {
                    firstFakeLight_0.gameObject.SetActive(interact);
                }
            }
            else if (thirdAttachments.tacticalAsset.isRangefinder)
            {
                if (base.channel.IsLocalPlayer)
                {
                    if (firstAttachments.lightHook != null)
                    {
                        firstAttachments.lightHook.gameObject.SetActive(inRange && interact);
                    }
                    if (firstAttachments.light2Hook != null)
                    {
                        firstAttachments.light2Hook.gameObject.SetActive(!inRange && interact);
                    }
                    if (thirdAttachments.lightHook != null)
                    {
                        thirdAttachments.lightHook.gameObject.SetActive(inRange && interact);
                    }
                    if (thirdAttachments.light2Hook != null)
                    {
                        thirdAttachments.light2Hook.gameObject.SetActive(!inRange && interact);
                    }
                }
                if (firstFakeLight_0 != null)
                {
                    firstFakeLight_0.gameObject.SetActive(inRange && interact);
                }
                if (firstFakeLight_1 != null)
                {
                    firstFakeLight_1.gameObject.SetActive(!inRange && interact);
                }
            }
        }
        if (thirdAttachments.tacticalAsset != null && thirdAttachments.tacticalAsset.isLight && interact)
        {
            base.player.enableItemSpotLight(thirdAttachments.tacticalAsset.lightConfig);
        }
        else
        {
            base.player.disableItemSpotLight();
        }
        if (base.channel.IsLocalPlayer)
        {
            if (firstAttachments.sightAsset != null)
            {
                firstPersonZoomFactor = firstAttachments.sightAsset.zoom;
                thirdPersonZoomFactor = thirdAttachments.sightAsset.thirdPersonZoomFactor;
                shouldZoomUsingEyes = firstAttachments.sightAsset.shouldZoomUsingEyes;
                if (firstAttachments.scopeHook != null)
                {
                    base.player.look.enableScope(firstPersonZoomFactor, firstAttachments.sightAsset);
                    Renderer component = firstAttachments.scopeHook.GetComponent<Renderer>();
                    if (component != null)
                    {
                        component.enabled = GraphicsSettings.scopeQuality != EGraphicQuality.OFF;
                        component.sharedMaterial = base.player.look.scopeMaterial;
                    }
                    firstAttachments.scopeHook.gameObject.SetActive(value: true);
                    if (base.channel.owner.IsLeftHanded)
                    {
                        Vector3 localScale = firstAttachments.scopeHook.localScale;
                        localScale.x *= -1f;
                        firstAttachments.scopeHook.localScale = localScale;
                    }
                }
                else
                {
                    base.player.look.disableScope();
                }
            }
            else
            {
                firstPersonZoomFactor = 1f;
                thirdPersonZoomFactor = 1.25f;
                shouldZoomUsingEyes = false;
                base.player.look.disableScope();
            }
            UpdateCrosshairEnabled();
        }
        UpdateMovementSpeedMultiplier();
        UpdateAimInDuration();
    }

    private void applyRecoilMagnitudeModifiers(ref float value)
    {
        if (base.player.stance.stance == EPlayerStance.SPRINT)
        {
            value *= equippedGunAsset.recoilSprint;
        }
        else if (base.player.stance.stance == EPlayerStance.CROUCH)
        {
            value *= equippedGunAsset.recoilCrouch;
        }
        else if (base.player.stance.stance == EPlayerStance.PRONE)
        {
            value *= equippedGunAsset.recoilProne;
        }
        else if (base.player.stance.stance == EPlayerStance.SWIM)
        {
            value *= equippedGunAsset.recoilSwimming;
        }
        if (!base.player.movement.isGrounded)
        {
            value *= equippedGunAsset.recoilMidair;
        }
    }

    /// <summary>
    /// Note: This is the m/s² acceleration, not the multiplier.
    /// </summary>
    internal float CalculateBulletGravity()
    {
        return Physics.gravity.y * CalculateBulletGravityMultiplier();
    }

    internal float CalculateSpreadAngleRadians()
    {
        float quality = (float)(int)base.player.equipment.quality / 100f;
        float interpolatedAimAlpha = GetInterpolatedAimAlpha();
        return CalculateSpreadAngleRadians(quality, interpolatedAimAlpha);
    }

    internal float CalculateSpreadAngleRadians(float quality, float aimAlpha)
    {
        float baseSpreadAngleRadians = equippedGunAsset.baseSpreadAngleRadians;
        baseSpreadAngleRadians *= ((quality < 0.5f) ? (1f + (1f - quality * 2f)) : 1f);
        baseSpreadAngleRadians *= Mathf.Lerp(1f, equippedGunAsset.spreadAim, aimAlpha);
        baseSpreadAngleRadians *= 1f - base.player.skills.mastery(0, 1) * 0.5f;
        if (thirdAttachments.sightAsset != null && (!thirdAttachments.sightAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
        {
            baseSpreadAngleRadians *= Mathf.Lerp(1f, thirdAttachments.sightAsset.spread, aimAlpha);
        }
        if (thirdAttachments.tacticalAsset != null && shouldEnableTacticalStats && (!thirdAttachments.tacticalAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
        {
            baseSpreadAngleRadians *= thirdAttachments.tacticalAsset.spread;
        }
        if (thirdAttachments.gripAsset != null && (!thirdAttachments.gripAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
        {
            baseSpreadAngleRadians *= thirdAttachments.gripAsset.spread;
        }
        if (thirdAttachments.barrelAsset != null && (!thirdAttachments.barrelAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
        {
            baseSpreadAngleRadians *= thirdAttachments.barrelAsset.spread;
        }
        if (thirdAttachments.magazineAsset != null && (!thirdAttachments.magazineAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
        {
            baseSpreadAngleRadians *= thirdAttachments.magazineAsset.spread;
        }
        if (base.player.stance.stance == EPlayerStance.SPRINT)
        {
            baseSpreadAngleRadians *= equippedGunAsset.spreadSprint;
        }
        else if (base.player.stance.stance == EPlayerStance.CROUCH)
        {
            baseSpreadAngleRadians *= equippedGunAsset.spreadCrouch;
        }
        else if (base.player.stance.stance == EPlayerStance.PRONE)
        {
            baseSpreadAngleRadians *= equippedGunAsset.spreadProne;
        }
        else if (base.player.stance.stance == EPlayerStance.SWIM)
        {
            baseSpreadAngleRadians *= equippedGunAsset.spreadSwimming;
        }
        if (base.player.look.perspective == EPlayerPerspective.THIRD)
        {
            baseSpreadAngleRadians *= Provider.modeConfigData.Gameplay.ThirdPerson_SpreadMultiplier;
        }
        if (!base.player.movement.isGrounded)
        {
            baseSpreadAngleRadians *= equippedGunAsset.spreadMidair;
        }
        return baseSpreadAngleRadians;
    }

    internal void UpdateCrosshairEnabled()
    {
        if ((!equippedGunAsset.isTurret && equippedGunAsset.action != EAction.Minigun && ((isAiming && base.player.look.perspective == EPlayerPerspective.FIRST && (equippedGunAsset.action != EAction.String || thirdAttachments.sightHook != null)) || isAttaching)) || (base.player.movement.getVehicle() != null && base.player.look.perspective != 0))
        {
            PlayerUI.disableCrosshair();
        }
        else
        {
            PlayerUI.enableCrosshair();
        }
    }

    private void updateAttach()
    {
        if (sightButton != null)
        {
            bool allowZeroCaliber = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
            sightSearch = base.player.inventory.search(EItemType.SIGHT, equippedGunAsset.attachmentCalibers, allowZeroCaliber);
            if (sightJars != null)
            {
                sightButton.RemoveChild(sightJars);
            }
            sightJars = new SleekJars(100f, sightSearch);
            sightJars.SizeScale_X = 1f;
            sightJars.SizeScale_Y = 1f;
            sightJars.onClickedJar = onClickedSightJar;
            sightButton.AddChild(sightJars);
            if (thirdAttachments.sightAsset != null)
            {
                Color rarityColorUI = ItemTool.getRarityColorUI(thirdAttachments.sightAsset.rarity);
                sightButton.backgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
                sightButton.textColor = rarityColorUI;
                sightButton.tooltip = thirdAttachments.sightAsset.itemName;
                sightButton.iconColor = rarityColorUI;
            }
            else
            {
                sightButton.backgroundColor = ESleekTint.BACKGROUND;
                sightButton.textColor = ESleekTint.FOREGROUND;
                sightButton.tooltip = localization.format("Sight_Hook_Tooltip");
                sightButton.iconColor = ESleekTint.FOREGROUND;
            }
        }
        if (tacticalButton != null)
        {
            bool allowZeroCaliber2 = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
            tacticalSearch = base.player.inventory.search(EItemType.TACTICAL, equippedGunAsset.attachmentCalibers, allowZeroCaliber2);
            if (tacticalJars != null)
            {
                tacticalButton.RemoveChild(tacticalJars);
            }
            tacticalJars = new SleekJars(100f, tacticalSearch);
            tacticalJars.SizeScale_X = 1f;
            tacticalJars.SizeScale_Y = 1f;
            tacticalJars.onClickedJar = onClickedTacticalJar;
            tacticalButton.AddChild(tacticalJars);
            if (thirdAttachments.tacticalAsset != null)
            {
                Color rarityColorUI2 = ItemTool.getRarityColorUI(thirdAttachments.tacticalAsset.rarity);
                tacticalButton.backgroundColor = SleekColor.BackgroundIfLight(rarityColorUI2);
                tacticalButton.textColor = rarityColorUI2;
                tacticalButton.tooltip = thirdAttachments.tacticalAsset.itemName;
                tacticalButton.iconColor = rarityColorUI2;
            }
            else
            {
                tacticalButton.backgroundColor = ESleekTint.BACKGROUND;
                tacticalButton.textColor = ESleekTint.FOREGROUND;
                tacticalButton.tooltip = localization.format("Tactical_Hook_Tooltip");
                tacticalButton.iconColor = ESleekTint.FOREGROUND;
            }
        }
        if (gripButton != null)
        {
            bool allowZeroCaliber3 = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
            gripSearch = base.player.inventory.search(EItemType.GRIP, equippedGunAsset.attachmentCalibers, allowZeroCaliber3);
            if (gripJars != null)
            {
                gripButton.RemoveChild(gripJars);
            }
            gripJars = new SleekJars(100f, gripSearch);
            gripJars.SizeScale_X = 1f;
            gripJars.SizeScale_Y = 1f;
            gripJars.onClickedJar = onClickedGripJar;
            gripButton.AddChild(gripJars);
            if (thirdAttachments.gripAsset != null)
            {
                Color rarityColorUI3 = ItemTool.getRarityColorUI(thirdAttachments.gripAsset.rarity);
                gripButton.backgroundColor = SleekColor.BackgroundIfLight(rarityColorUI3);
                gripButton.textColor = rarityColorUI3;
                gripButton.tooltip = thirdAttachments.gripAsset.itemName;
                gripButton.iconColor = rarityColorUI3;
            }
            else
            {
                gripButton.backgroundColor = ESleekTint.BACKGROUND;
                gripButton.textColor = ESleekTint.FOREGROUND;
                gripButton.tooltip = localization.format("Grip_Hook_Tooltip");
                gripButton.iconColor = ESleekTint.FOREGROUND;
            }
        }
        if (barrelButton != null)
        {
            bool allowZeroCaliber4 = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
            barrelSearch = base.player.inventory.search(EItemType.BARREL, equippedGunAsset.attachmentCalibers, allowZeroCaliber4);
            if (barrelJars != null)
            {
                barrelButton.RemoveChild(barrelJars);
            }
            barrelJars = new SleekJars(100f, barrelSearch);
            barrelJars.SizeScale_X = 1f;
            barrelJars.SizeScale_Y = 1f;
            barrelJars.onClickedJar = onClickedBarrelJar;
            barrelButton.AddChild(barrelJars);
            if (thirdAttachments.barrelAsset != null)
            {
                Color rarityColorUI4 = ItemTool.getRarityColorUI(thirdAttachments.barrelAsset.rarity);
                barrelButton.backgroundColor = SleekColor.BackgroundIfLight(rarityColorUI4);
                barrelButton.textColor = rarityColorUI4;
                barrelButton.tooltip = thirdAttachments.barrelAsset.itemName;
                barrelButton.iconColor = rarityColorUI4;
            }
            else
            {
                barrelButton.backgroundColor = ESleekTint.BACKGROUND;
                barrelButton.textColor = ESleekTint.FOREGROUND;
                barrelButton.tooltip = localization.format("Barrel_Hook_Tooltip");
                barrelButton.iconColor = ESleekTint.FOREGROUND;
            }
            if (thirdAttachments.barrelAsset != null && thirdAttachments.barrelAsset.showQuality)
            {
                barrelQualityImage.TintColor = ItemTool.getQualityColor((float)(int)base.player.equipment.state[16] / 100f);
                barrelQualityLabel.Text = base.player.equipment.state[16] + "%";
                barrelQualityLabel.TextColor = barrelQualityImage.TintColor;
                barrelQualityLabel.IsVisible = true;
                barrelQualityImage.IsVisible = true;
            }
            else
            {
                barrelQualityLabel.IsVisible = false;
                barrelQualityImage.IsVisible = false;
            }
        }
        if (magazineButton != null)
        {
            bool allowZeroCaliber5 = !equippedGunAsset.requiresNonZeroAttachmentCaliber;
            magazineSearch = base.player.inventory.search(EItemType.MAGAZINE, equippedGunAsset.magazineCalibers, allowZeroCaliber5);
            if (magazineJars != null)
            {
                magazineButton.RemoveChild(magazineJars);
            }
            magazineJars = new SleekJars(100f, magazineSearch);
            magazineJars.SizeScale_X = 1f;
            magazineJars.SizeScale_Y = 1f;
            magazineJars.onClickedJar = onClickedMagazineJar;
            magazineButton.AddChild(magazineJars);
            if (thirdAttachments.magazineAsset != null)
            {
                Color rarityColorUI5 = ItemTool.getRarityColorUI(thirdAttachments.magazineAsset.rarity);
                magazineButton.backgroundColor = SleekColor.BackgroundIfLight(rarityColorUI5);
                magazineButton.textColor = rarityColorUI5;
                magazineButton.tooltip = thirdAttachments.magazineAsset.itemName;
                magazineButton.iconColor = rarityColorUI5;
            }
            else
            {
                magazineButton.backgroundColor = ESleekTint.BACKGROUND;
                magazineButton.textColor = ESleekTint.FOREGROUND;
                magazineButton.tooltip = localization.format("Magazine_Hook_Tooltip");
                magazineButton.iconColor = ESleekTint.FOREGROUND;
            }
            if (thirdAttachments.magazineAsset != null && thirdAttachments.magazineAsset.showQuality)
            {
                magazineQualityImage.TintColor = ItemTool.getQualityColor((float)(int)base.player.equipment.state[17] / 100f);
                magazineQualityLabel.Text = base.player.equipment.state[17] + "%";
                magazineQualityLabel.TextColor = magazineQualityImage.TintColor;
                magazineQualityLabel.IsVisible = true;
                magazineQualityImage.IsVisible = true;
            }
            else
            {
                magazineQualityLabel.IsVisible = false;
                magazineQualityImage.IsVisible = false;
            }
        }
    }

    private void updateInfo()
    {
        ammoLabel.TextColor = ((ammo < equippedGunAsset.ammoPerShot) ? ESleekTint.BAD : ESleekTint.FONT);
        ammoLabel.Text = localization.format("Ammo", ammo, (thirdAttachments.magazineAsset != null) ? thirdAttachments.magazineAsset.amount : 0);
        if (firstAmmoCounter != null)
        {
            firstAmmoCounter.text = ammo.ToString();
        }
        if (thirdAmmoCounter != null)
        {
            thirdAmmoCounter.text = ammo.ToString();
        }
        if (firemode == EFiremode.SAFETY)
        {
            firemodeLabel.Text = localization.format("Firemode", localization.format("Safety"), ControlsSettings.firemode);
        }
        else if (firemode == EFiremode.SEMI)
        {
            firemodeLabel.Text = localization.format("Firemode", localization.format("Semi"), ControlsSettings.firemode);
        }
        else if (firemode == EFiremode.AUTO)
        {
            firemodeLabel.Text = localization.format("Firemode", localization.format("Auto"), ControlsSettings.firemode);
        }
        else if (firemode == EFiremode.BURST)
        {
            firemodeLabel.Text = localization.format("Firemode", localization.format("Burst"), ControlsSettings.firemode);
        }
        attachLabel.Text = localization.format("Attach", (thirdAttachments.magazineAsset != null) ? thirdAttachments.magazineAsset.itemName : localization.format("None"), ControlsSettings.attach);
        if (thirdAttachments.magazineAsset != null)
        {
            attachLabel.TextColor = ItemTool.getRarityColorUI(thirdAttachments.magazineAsset.rarity);
        }
        else
        {
            attachLabel.TextColor = ESleekTint.FONT;
        }
    }

    private void onPerspectiveUpdated(EPlayerPerspective newPerspective)
    {
        UpdateCrosshairEnabled();
        if (newPerspective == EPlayerPerspective.THIRD)
        {
            if (isAiming)
            {
                PlayerUI.updateScope(isScoped: false);
                base.player.look.enableZoom(thirdPersonZoomFactor);
                base.player.look.disableOverlay();
            }
            else
            {
                base.player.look.disableZoom();
            }
        }
        else if (isAiming)
        {
            if (shouldZoomUsingEyes)
            {
                base.player.look.enableZoom(firstPersonZoomFactor);
            }
            else if (GraphicsSettings.scopeQuality == EGraphicQuality.OFF && PlayerLifeUI.scopeOverlay.scopeImage.Texture != null)
            {
                PlayerUI.updateScope(isScoped: true);
                base.player.look.enableZoom(firstPersonZoomFactor);
                base.player.look.enableOverlay();
            }
            else
            {
                base.player.look.disableZoom();
            }
        }
        else
        {
            base.player.look.disableZoom();
        }
        if (thirdShellRenderer != null)
        {
            thirdShellRenderer.forceRenderingOff = newPerspective == EPlayerPerspective.FIRST && !equippedGunAsset.isTurret;
        }
    }

    private void SyncScopeDistanceMarkerText()
    {
        foreach (DistanceMarker scopeDistanceMarker in scopeDistanceMarkers)
        {
            if (!(scopeDistanceMarker.textComponent == null))
            {
                if (OptionsSettings.metric)
                {
                    scopeDistanceMarker.textComponent.text = $"{scopeDistanceMarker.distance} m";
                }
                else
                {
                    scopeDistanceMarker.textComponent.text = $"{Mathf.RoundToInt(MeasurementTool.MtoYd(scopeDistanceMarker.distance))} yd";
                }
            }
        }
    }

    private void onClickedSightHookButton(ISleekElement button)
    {
        SendAttachSight.Invoke(GetNetId(), ENetReliability.Unreliable, byte.MaxValue, byte.MaxValue, byte.MaxValue, new byte[0]);
    }

    private void onClickedTacticalHookButton(ISleekElement button)
    {
        SendAttachTactical.Invoke(GetNetId(), ENetReliability.Unreliable, byte.MaxValue, byte.MaxValue, byte.MaxValue, new byte[0]);
    }

    private void onClickedGripHookButton(ISleekElement button)
    {
        SendAttachGrip.Invoke(GetNetId(), ENetReliability.Unreliable, byte.MaxValue, byte.MaxValue, byte.MaxValue, new byte[0]);
    }

    private void onClickedBarrelHookButton(ISleekElement button)
    {
        SendAttachBarrel.Invoke(GetNetId(), ENetReliability.Unreliable, byte.MaxValue, byte.MaxValue, byte.MaxValue, new byte[0]);
    }

    private void onClickedMagazineHookButton(ISleekElement button)
    {
        SendAttachMagazine.Invoke(GetNetId(), ENetReliability.Unreliable, byte.MaxValue, byte.MaxValue, byte.MaxValue, new byte[0]);
    }

    private void onClickedSightJar(SleekJars jars, int index)
    {
        ItemAsset asset = sightSearch[index].GetAsset();
        if (asset != null)
        {
            SendAttachSight.Invoke(GetNetId(), ENetReliability.Unreliable, sightSearch[index].page, sightSearch[index].jar.x, sightSearch[index].jar.y, asset.hash);
        }
    }

    private void onClickedTacticalJar(SleekJars jars, int index)
    {
        ItemAsset asset = tacticalSearch[index].GetAsset();
        if (asset != null)
        {
            SendAttachTactical.Invoke(GetNetId(), ENetReliability.Unreliable, tacticalSearch[index].page, tacticalSearch[index].jar.x, tacticalSearch[index].jar.y, asset.hash);
        }
    }

    private void onClickedGripJar(SleekJars jars, int index)
    {
        ItemAsset asset = gripSearch[index].GetAsset();
        if (asset != null)
        {
            SendAttachGrip.Invoke(GetNetId(), ENetReliability.Unreliable, gripSearch[index].page, gripSearch[index].jar.x, gripSearch[index].jar.y, asset.hash);
        }
    }

    private void onClickedBarrelJar(SleekJars jars, int index)
    {
        ItemAsset asset = barrelSearch[index].GetAsset();
        if (asset != null)
        {
            SendAttachBarrel.Invoke(GetNetId(), ENetReliability.Unreliable, barrelSearch[index].page, barrelSearch[index].jar.x, barrelSearch[index].jar.y, asset.hash);
        }
    }

    private void onClickedMagazineJar(SleekJars jars, int index)
    {
        ItemAsset asset = magazineSearch[index].GetAsset();
        if (asset != null)
        {
            SendAttachMagazine.Invoke(GetNetId(), ENetReliability.Unreliable, magazineSearch[index].page, magazineSearch[index].jar.x, magazineSearch[index].jar.y, asset.hash);
        }
    }

    private void startAim()
    {
        UpdateMovementSpeedMultiplier();
        if (base.channel.IsLocalPlayer)
        {
            base.player.animator.viewmodelSwayMultiplier = 0.1f;
            base.player.animator.viewmodelOffsetPreferenceMultiplier = 0f;
            if (!equippedGunAsset.isTurret && equippedGunAsset.action != EAction.Minigun)
            {
                if (GraphicsSettings.scopeQuality == EGraphicQuality.OFF && firstAttachments.sightModel != null && firstAttachments.scopeHook != null && firstAttachments.scopeHook.Find("Reticule") != null)
                {
                    Texture mainTexture = firstAttachments.scopeHook.Find("Reticule").GetComponent<Renderer>().sharedMaterial.mainTexture;
                    if (mainTexture.width <= 64)
                    {
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionOffset_X = -mainTexture.width / 2;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionOffset_Y = -mainTexture.height / 2;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionScale_X = 0.5f;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionScale_Y = 0.5f;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeOffset_X = mainTexture.width;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeOffset_Y = mainTexture.height;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_X = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_Y = 0f;
                    }
                    else
                    {
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionOffset_X = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionOffset_Y = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionScale_X = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.PositionScale_Y = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeOffset_X = 0f;
                        PlayerLifeUI.scopeOverlay.scopeImage.SizeOffset_Y = 0f;
                        if (firstAttachments.sightAsset.shouldOffsetScopeOverlayByOneTexel)
                        {
                            PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_X = 1f + 1f / (float)mainTexture.width;
                            PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_Y = 1f + 1f / (float)mainTexture.height;
                        }
                        else
                        {
                            PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_X = 1f;
                            PlayerLifeUI.scopeOverlay.scopeImage.SizeScale_Y = 1f;
                        }
                    }
                    PlayerLifeUI.scopeOverlay.scopeImage.Texture = mainTexture;
                    if (firstAttachments.aimHook.parent.Find("Reticule") != null)
                    {
                        Color criticalHitmarkerColor = OptionsSettings.criticalHitmarkerColor;
                        criticalHitmarkerColor.a = 1f;
                        PlayerLifeUI.scopeOverlay.scopeImage.TintColor = criticalHitmarkerColor;
                    }
                    else
                    {
                        PlayerLifeUI.scopeOverlay.scopeImage.TintColor = ESleekTint.NONE;
                    }
                    base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.up;
                }
                else
                {
                    PlayerLifeUI.scopeOverlay.scopeImage.Texture = null;
                    base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
                }
            }
            else
            {
                PlayerLifeUI.scopeOverlay.scopeImage.Texture = null;
            }
            if (equippedGunAsset.driverTurretViewmodelMode == EDriverTurretViewmodelMode.OffscreenWhileAiming)
            {
                base.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.up;
            }
            base.player.look.shouldUseZoomFactorForSensitivity = true;
            if (equippedGunAsset.isTurret || equippedGunAsset.action == EAction.Minigun || shouldZoomUsingEyes)
            {
                if (base.player.look.perspective == EPlayerPerspective.FIRST)
                {
                    base.player.look.enableZoom(firstPersonZoomFactor);
                }
                else if (base.player.look.perspective == EPlayerPerspective.THIRD)
                {
                    base.player.look.enableZoom(thirdPersonZoomFactor);
                }
            }
            else if (base.player.look.perspective == EPlayerPerspective.FIRST)
            {
                if (GraphicsSettings.scopeQuality == EGraphicQuality.OFF && PlayerLifeUI.scopeOverlay.scopeImage.Texture != null)
                {
                    PlayerUI.updateScope(isScoped: true);
                    base.player.look.enableZoom(firstPersonZoomFactor);
                    base.player.look.enableOverlay();
                }
            }
            else if (base.player.look.perspective == EPlayerPerspective.THIRD)
            {
                base.player.look.enableZoom(thirdPersonZoomFactor);
            }
            UpdateCrosshairEnabled();
            PlayerUI.instance.groupUI.IsVisible = false;
        }
        base.player.playSound(equippedGunAsset.aim, 1f, 0f);
        isMinigunSpinning = true;
        base.player.animator.play("Aim_Start", smooth: false);
        UseableGun.OnAimingChanged_Global.TryInvoke("OnAimingChanged_Global", this);
        GetVehicleTurretEventHook()?.OnAimingStarted?.TryInvoke(this);
        if (base.channel.IsLocalPlayer)
        {
            GetVehicleTurretEventHook()?.OnAimingStarted_Local?.TryInvoke(this);
        }
        foreach (UseableGunEventHook item in EnumerateEventComponents())
        {
            item.OnAimingStarted?.TryInvoke(this);
        }
    }

    private void stopAim()
    {
        UpdateMovementSpeedMultiplier();
        if (base.channel.IsLocalPlayer)
        {
            if (!equippedGunAsset.isTurret)
            {
                base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
            }
            if (equippedGunAsset.driverTurretViewmodelMode == EDriverTurretViewmodelMode.OffscreenWhileAiming)
            {
                base.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.zero;
            }
            base.player.animator.scopeSway = Vector3.zero;
            base.player.animator.viewmodelSwayMultiplier = 1f;
            base.player.animator.viewmodelOffsetPreferenceMultiplier = 1f;
            PlayerUI.updateScope(isScoped: false);
            base.player.look.shouldUseZoomFactorForSensitivity = false;
            base.player.look.disableZoom();
            base.player.look.disableOverlay();
            UpdateCrosshairEnabled();
            PlayerUI.instance.groupUI.IsVisible = true;
        }
        isMinigunSpinning = false;
        base.player.animator.play("Aim_Stop", smooth: false);
        UseableGun.OnAimingChanged_Global.TryInvoke("OnAimingChanged_Global", this);
        GetVehicleTurretEventHook()?.OnAimingStopped?.TryInvoke(this);
        if (base.channel.IsLocalPlayer)
        {
            GetVehicleTurretEventHook()?.OnAimingStopped_Local?.TryInvoke(this);
        }
        foreach (UseableGunEventHook item in EnumerateEventComponents())
        {
            item.OnAimingStopped?.TryInvoke(this);
        }
    }

    private void startAttach()
    {
        PlayerUI.isLocked = true;
        PlayerLifeUI.close();
        if (sightButton != null)
        {
            sightButton.IsVisible = true;
        }
        if (tacticalButton != null)
        {
            tacticalButton.IsVisible = true;
        }
        if (gripButton != null)
        {
            gripButton.IsVisible = true;
        }
        if (barrelButton != null)
        {
            barrelButton.IsVisible = true;
        }
        if (magazineButton != null)
        {
            magazineButton.IsVisible = true;
        }
        UpdateCrosshairEnabled();
        if (base.channel.IsLocalPlayer)
        {
            GetVehicleTurretEventHook()?.OnInspectingAttachmentsStarted_Local?.TryInvoke(this);
        }
    }

    private void stopAttach()
    {
        PlayerUI.isLocked = false;
        PlayerLifeUI.open();
        if (sightButton != null)
        {
            sightButton.IsVisible = false;
        }
        if (tacticalButton != null)
        {
            tacticalButton.IsVisible = false;
        }
        if (gripButton != null)
        {
            gripButton.IsVisible = false;
        }
        if (barrelButton != null)
        {
            barrelButton.IsVisible = false;
        }
        if (magazineButton != null)
        {
            magazineButton.IsVisible = false;
        }
        UpdateCrosshairEnabled();
        if (base.channel.IsLocalPlayer)
        {
            GetVehicleTurretEventHook()?.OnInspectingAttachmentsStopped_Local?.TryInvoke(this);
        }
    }

    private void Update()
    {
        if (!Dedicator.IsDedicatedServer && base.player.equipment.asset is ItemGunAsset { action: EAction.Minigun })
        {
            if (isMinigunSpinning)
            {
                minigunSpeed = Mathf.Lerp(minigunSpeed, 1f, 8f * Time.deltaTime);
            }
            else
            {
                minigunSpeed = Mathf.Lerp(minigunSpeed, 0f, 2f * Time.deltaTime);
            }
            minigunDistance += minigunSpeed * 720f * Time.deltaTime;
            if (firstMinigunBarrel != null)
            {
                firstMinigunBarrel.localRotation = Quaternion.Euler(0f, minigunDistance, 0f);
            }
            if (thirdMinigunBarrel != null)
            {
                thirdMinigunBarrel.localRotation = Quaternion.Euler(0f, minigunDistance, 0f);
            }
            if (whir != null)
            {
                whir.volume = minigunSpeed;
                whir.pitch = Mathf.Lerp(0.75f, 1f, minigunSpeed);
            }
        }
        if (base.player.movement.getVehicle() != null && base.player.movement.getVehicle().passengers[base.player.movement.getSeat()].turret != null)
        {
            Transform turretAim = base.player.movement.getVehicle().passengers[base.player.movement.getSeat()].turretAim;
            if (turretAim != null)
            {
                Transform transform = turretAim.Find("Barrel");
                Transform transform2 = turretAim.Find("Eject");
                if (thirdMuzzleEmitter != null && transform != null)
                {
                    thirdMuzzleEmitter.transform.position = transform.position;
                    thirdMuzzleEmitter.transform.rotation = transform.rotation;
                }
                if (thirdShellEmitter != null && transform2 != null)
                {
                    thirdShellEmitter.transform.position = transform2.position;
                    thirdShellEmitter.transform.rotation = transform2.rotation;
                }
            }
        }
        else if (thirdShellEmitter != null)
        {
            thirdShellEmitter.transform.SetPositionAndRotation(thirdAttachments.ejectHook.position, thirdAttachments.ejectHook.rotation);
        }
        if (!base.channel.IsLocalPlayer)
        {
            return;
        }
        if (laserTransform != null)
        {
            if (base.player.look.perspective == EPlayerPerspective.FIRST)
            {
                Quaternion quaternion = Quaternion.Euler(base.player.animator.recoilViewmodelCameraRotation.currentPosition);
                Vector3 vector = base.player.look.aim.rotation * quaternion * Vector3.forward;
                if (!base.player.look.IsLocallyUsingFreecam && Physics.Raycast(new Ray(base.player.look.aim.position, vector), out contact, 2048f, RayMasks.BLOCK_LASER))
                {
                    laserTransform.position = contact.point + vector * -0.05f;
                    laserGameObject.SetActive(value: true);
                }
                else
                {
                    laserGameObject.SetActive(value: false);
                }
            }
            else if (base.player.look.perspective == EPlayerPerspective.THIRD)
            {
                if (!base.player.look.IsLocallyUsingFreecam && Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out var hitInfo, 512f, RayMasks.DAMAGE_CLIENT))
                {
                    if (Physics.Raycast(new Ray(base.player.look.aim.position, (hitInfo.point - base.player.look.aim.position).normalized), out contact, 2048f, RayMasks.BLOCK_LASER))
                    {
                        laserTransform.position = contact.point + base.player.look.aim.forward * -0.05f;
                        laserGameObject.SetActive(value: true);
                    }
                    else
                    {
                        laserGameObject.SetActive(value: false);
                    }
                }
                else
                {
                    laserGameObject.SetActive(value: false);
                }
            }
        }
        else if (firstAttachments != null && firstAttachments.tacticalAsset != null && firstAttachments.tacticalAsset.isRangefinder)
        {
            bool flag = false;
            if (base.player.look.perspective == EPlayerPerspective.FIRST)
            {
                flag = Physics.Raycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), out contact, equippedGunAsset.rangeRangefinder, RayMasks.BLOCK_LASER);
            }
            else if (base.player.look.perspective == EPlayerPerspective.THIRD)
            {
                flag = Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out var hitInfo2, 512f, RayMasks.DAMAGE_CLIENT) && Physics.Raycast(new Ray(base.player.look.aim.position, (hitInfo2.point - base.player.look.aim.position).normalized), out contact, equippedGunAsset.rangeRangefinder, RayMasks.BLOCK_LASER);
            }
            if (rangeLabel != null)
            {
                if (inRange)
                {
                    if (OptionsSettings.metric)
                    {
                        rangeLabel.Text = (int)contact.distance + " m";
                    }
                    else
                    {
                        rangeLabel.Text = (int)MeasurementTool.MtoYd(contact.distance) + " yd";
                    }
                }
                else if (OptionsSettings.metric)
                {
                    rangeLabel.Text = "? m";
                }
                else
                {
                    rangeLabel.Text = "? yd";
                }
                rangeLabel.TextColor = (inRange ? Palette.COLOR_G : Palette.COLOR_R);
            }
            if (flag != inRange)
            {
                inRange = flag;
                if (firstAttachments.lightHook != null)
                {
                    firstAttachments.lightHook.gameObject.SetActive(inRange && interact);
                }
                if (firstAttachments.light2Hook != null)
                {
                    firstAttachments.light2Hook.gameObject.SetActive(!inRange && interact);
                }
                if (thirdAttachments.lightHook != null)
                {
                    thirdAttachments.lightHook.gameObject.SetActive(inRange && interact);
                }
                if (thirdAttachments.light2Hook != null)
                {
                    thirdAttachments.light2Hook.gameObject.SetActive(!inRange && interact);
                }
            }
        }
        if (firstFakeLight != null && thirdMuzzleEmitter != null)
        {
            firstFakeLight.position = thirdMuzzleEmitter.transform.position;
        }
        if (firstFakeLight_0 != null && thirdAttachments.lightHook != null)
        {
            firstFakeLight_0.position = thirdAttachments.lightHook.position;
            if (firstFakeLight_0.gameObject.activeSelf != (base.player.look.perspective == EPlayerPerspective.FIRST && thirdAttachments.lightHook.gameObject.activeSelf))
            {
                firstFakeLight_0.gameObject.SetActive(base.player.look.perspective == EPlayerPerspective.FIRST && thirdAttachments.lightHook.gameObject.activeSelf);
            }
        }
        if (firstFakeLight_1 != null && thirdAttachments.light2Hook != null)
        {
            firstFakeLight_1.position = thirdAttachments.light2Hook.position;
            if (firstFakeLight_1.gameObject.activeSelf != (base.player.look.perspective == EPlayerPerspective.FIRST && thirdAttachments.light2Hook.gameObject.activeSelf))
            {
                firstFakeLight_1.gameObject.SetActive(base.player.look.perspective == EPlayerPerspective.FIRST && thirdAttachments.light2Hook.gameObject.activeSelf);
            }
        }
        swayTime += Time.deltaTime * (1f - (float)steadyAccuracy / 4f);
        if (isAiming && firstAttachments.sightAsset != null)
        {
            float num = (1f - 1f / firstAttachments.sightAsset.zoom) * 1.25f;
            num *= 1f - base.player.skills.mastery(0, 5) * 0.5f;
            if (thirdAttachments != null && thirdAttachments.tacticalAsset != null && shouldEnableTacticalStats && (!thirdAttachments.tacticalAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
            {
                num *= thirdAttachments.tacticalAsset.sway;
            }
            if (thirdAttachments != null && thirdAttachments.gripAsset != null && (!thirdAttachments.gripAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
            {
                num *= thirdAttachments.gripAsset.sway;
            }
            if (thirdAttachments != null && thirdAttachments.barrelAsset != null && (!thirdAttachments.barrelAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
            {
                num *= thirdAttachments.barrelAsset.sway;
            }
            if (thirdAttachments != null && thirdAttachments.magazineAsset != null && (!thirdAttachments.magazineAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE))
            {
                num *= thirdAttachments.magazineAsset.sway;
            }
            if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                num *= SWAY_CROUCH;
            }
            else if (base.player.stance.stance == EPlayerStance.PRONE)
            {
                num *= SWAY_PRONE;
            }
            base.player.animator.scopeSway = Vector3.Lerp(base.player.animator.scopeSway, new Vector3(Mathf.Sin(0.75f * swayTime) * num, Mathf.Sin(1f * swayTime) * num, 0f), Time.deltaTime * 4f);
        }
        else
        {
            base.player.animator.scopeSway = Vector3.Lerp(base.player.animator.scopeSway, Vector3.zero, Time.deltaTime * 4f);
        }
        if (firstAttachments.reticuleHook != null && firstAttachments.sightAsset != null && firstAttachments.sightAsset.isHolographic)
        {
            UpdateHolographicReticulePosition();
        }
        if (scopeDistanceMarkers != null && scopeDistanceMarkers.Count > 0)
        {
            UpdateScopeDistanceMarkers();
        }
    }

    internal void GetAimingViewmodelAlignment(out Transform alignmentTransform, out Vector3 alignmentOffset, out float alpha)
    {
        alignmentTransform = null;
        alignmentOffset = Vector3.zero;
        alpha = GetInterpolatedAimAlpha();
        if (equippedGunAsset.isTurret || equippedGunAsset.action == EAction.Minigun || !(firstAttachments != null))
        {
            return;
        }
        if (firstAttachments.aimHook != null)
        {
            alignmentTransform = firstAttachments.aimHook;
            return;
        }
        if (firstAttachments.viewHook != null)
        {
            alignmentTransform = firstAttachments.viewHook;
            return;
        }
        alignmentTransform = firstAttachments.sightHook;
        if (equippedGunAsset.hasSight)
        {
            alignmentOffset = new Vector3(0f, -0.04f, 0.01f);
        }
    }

    /// <summary>
    /// This is a bit of a hack... aimAccuracy is [0, maxAimingAccuracy] and changed during each FixedUpdate call,
    /// but was used in some gameplay display features like holo sight, laser, ADS, etc. (yes, should
    /// be de-coupled from FixedUpdate but that is its own issue) To smooth this out we interpolate
    /// slightly behind the aimAccuracy value depending on the time since FixedUpdate.
    /// </summary>
    private float GetInterpolatedAimAlpha()
    {
        float num = (float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / (double)Time.fixedDeltaTime);
        if (isAiming)
        {
            if (aimAccuracy < maxAimingAccuracy)
            {
                return MathfEx.SmootherStep01((float)aimAccuracy * maxAimingAccuracyReciprocal + num * maxAimingAccuracyReciprocal);
            }
            return 1f;
        }
        if (aimAccuracy > 0)
        {
            return MathfEx.SmootherStep01((float)aimAccuracy * maxAimingAccuracyReciprocal - num * maxAimingAccuracyReciprocal);
        }
        return 0f;
    }

    private float GetSimulationAimAlpha()
    {
        return (float)aimAccuracy * maxAimingAccuracyReciprocal;
    }

    private void UpdateInfoBoxVisibility()
    {
        bool flag = base.player.isPluginWidgetFlagActive(EPluginWidgetFlags.ShowUseableGunStatus);
        if (Level.info != null && Level.info.configData != null)
        {
            flag &= Level.info.configData.PlayerUI_GunVisible;
        }
        infoBox.IsVisible = flag;
    }

    private void OnLocalPluginWidgetFlagsChanged(Player player, EPluginWidgetFlags oldFlags)
    {
        EPluginWidgetFlags pluginWidgetFlags = player.pluginWidgetFlags;
        if ((oldFlags & EPluginWidgetFlags.ShowUseableGunStatus) != (pluginWidgetFlags & EPluginWidgetFlags.ShowUseableGunStatus))
        {
            UpdateInfoBoxVisibility();
        }
    }

    private IEnumerable<UseableGunEventHook> EnumerateEventComponents()
    {
        if ((bool)firstEventComponent)
        {
            yield return firstEventComponent;
        }
        if ((bool)thirdEventComponent)
        {
            yield return thirdEventComponent;
        }
        if ((bool)characterEventComponent)
        {
            yield return characterEventComponent;
        }
    }

    private void InvokeModHookShotFiredEvents()
    {
        GetVehicleTurretEventHook()?.OnShotFired?.TryInvoke(this);
        foreach (UseableGunEventHook item in EnumerateEventComponents())
        {
            item.OnShotFired?.TryInvoke(this);
        }
    }

    private void ClearScopeDistanceMarkers()
    {
        if (scopeDistanceMarkers != null)
        {
            scopeDistanceMarkers.Clear();
        }
    }

    private void InstantiateScopeDistanceMarkers()
    {
        if (scopeDistanceMarkers == null)
        {
            scopeDistanceMarkers = new List<DistanceMarker>();
        }
        if (firstAttachments.scopeHook == null)
        {
            return;
        }
        Transform transform = firstAttachments.scopeHook.Find("Reticule");
        if (transform == null)
        {
            return;
        }
        if (scopeDistanceMarkerMaterial == null)
        {
            scopeDistanceMarkerMaterial = new Material(Shader.Find("Sprites/Default"));
            scopeDistanceMarkerMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        foreach (ItemSightAsset.DistanceMarker distanceMarker2 in firstAttachments.sightAsset.distanceMarkers)
        {
            DistanceMarker distanceMarker = new DistanceMarker();
            distanceMarker.isActive = true;
            distanceMarker.distance = distanceMarker2.distance;
            GameObject gameObject = new GameObject($"DistanceMarker_{distanceMarker2.distance}m");
            gameObject.layer = 11;
            distanceMarker.transform = gameObject.transform;
            distanceMarker.transform.SetParent(transform, worldPositionStays: false);
            distanceMarker.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject gameObject2 = new GameObject("Line");
            gameObject2.layer = 11;
            gameObject2.transform.SetParent(distanceMarker.transform, worldPositionStays: false);
            distanceMarker.lineComponent = gameObject2.AddComponent<LineRenderer>();
            distanceMarker.lineComponent.alignment = LineAlignment.Local;
            distanceMarker.lineComponent.endColor = distanceMarker2.color;
            distanceMarker.lineComponent.startColor = distanceMarker2.color;
            distanceMarker.lineComponent.useWorldSpace = false;
            distanceMarker.lineComponent.shadowCastingMode = ShadowCastingMode.Off;
            distanceMarker.lineComponent.widthMultiplier = 0.005f;
            distanceMarker.lineComponent.sharedMaterial = scopeDistanceMarkerMaterial;
            if (distanceMarker2.side == ItemSightAsset.DistanceMarker.ESide.Right)
            {
                distanceMarker.lineComponent.SetPositions(new Vector3[2]
                {
                    new Vector3(distanceMarker2.lineOffset * 2f, 0f, 0f),
                    new Vector3((distanceMarker2.lineOffset + distanceMarker2.lineWidth) * 2f, 0f, 0f)
                });
            }
            else
            {
                distanceMarker.lineComponent.SetPositions(new Vector3[2]
                {
                    new Vector3(distanceMarker2.lineOffset * -2f, 0f, 0f),
                    new Vector3((distanceMarker2.lineOffset + distanceMarker2.lineWidth) * -2f, 0f, 0f)
                });
            }
            if (distanceMarker2.hasLabel)
            {
                GameObject gameObject3 = new GameObject("Text");
                gameObject3.layer = 11;
                gameObject3.transform.SetParent(distanceMarker.transform, worldPositionStays: false);
                distanceMarker.textComponent = gameObject3.AddComponent<TextMeshPro>();
                distanceMarker.textComponent.color = distanceMarker2.color;
                distanceMarker.textComponent.fontSize = 0.35f;
                distanceMarker.textComponent.fontStyle = FontStyles.Bold;
                RectTransform rectTransform = gameObject3.GetRectTransform();
                if (distanceMarker2.side == ItemSightAsset.DistanceMarker.ESide.Right)
                {
                    rectTransform.localPosition = new Vector3((distanceMarker2.lineOffset + distanceMarker2.lineWidth) * 2f + 0.01f, 0f, 0f);
                    distanceMarker.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                    rectTransform.pivot = new Vector3(0f, 0.5f);
                }
                else
                {
                    rectTransform.localPosition = new Vector3((distanceMarker2.lineOffset + distanceMarker2.lineWidth) * -2f - 0.01f, 0f, 0f);
                    distanceMarker.textComponent.alignment = TextAlignmentOptions.MidlineRight;
                    rectTransform.pivot = new Vector3(1f, 0.5f);
                }
            }
            scopeDistanceMarkers.Add(distanceMarker);
        }
        SyncScopeDistanceMarkerText();
    }

    private void UpdateScopeDistanceMarkers()
    {
        float fieldOfView = base.player.look.scopeCamera.fieldOfView;
        float num = MathF.PI / 180f * fieldOfView;
        float muzzleVelocity = equippedGunAsset.muzzleVelocity;
        float gravity = CalculateBulletGravity();
        foreach (DistanceMarker scopeDistanceMarker in scopeDistanceMarkers)
        {
            float num2 = Mathf.Abs(SleekScopeOverlay.CalcAngle(muzzleVelocity, scopeDistanceMarker.distance, gravity)) / num * -2f;
            scopeDistanceMarker.transform.localPosition = new Vector3(0f, num2, 0f);
            bool flag = num2 < -0.01f && num2 > -0.9f;
            if (scopeDistanceMarker.isActive != flag)
            {
                scopeDistanceMarker.isActive = flag;
                scopeDistanceMarker.transform.gameObject.SetActive(flag);
            }
        }
    }

    /// <summary>
    /// Holographic sights follow the true aiming direction regardless of viewmodel animation.
    /// </summary>
    private void UpdateHolographicReticulePosition()
    {
        firstAttachments.reticuleHook.localPosition = originalReticuleHookLocalPosition;
        Plane plane = new Plane(firstAttachments.reticuleHook.forward, firstAttachments.reticuleHook.position);
        Quaternion quaternion = Quaternion.Euler(base.player.animator.recoilViewmodelCameraRotation.currentPosition);
        Vector3 vector = base.player.animator.viewmodelCameraTransform.rotation * quaternion * Vector3.forward;
        Vector3 position = base.player.animator.viewmodelCameraTransform.position;
        if (plane.Raycast(new Ray(position, vector), out var enter))
        {
            Vector3 position2 = position + vector * enter;
            Vector3 b = firstAttachments.reticuleHook.parent.InverseTransformPoint(position2);
            firstAttachments.reticuleHook.localPosition = Vector3.Lerp(originalReticuleHookLocalPosition, b, GetInterpolatedAimAlpha());
        }
    }

    private void UpdateMovementSpeedMultiplier()
    {
        movementSpeedMultiplier = 1f;
        if (isAiming)
        {
            movementSpeedMultiplier *= equippedGunAsset.aimingMovementSpeedMultiplier;
        }
        if (thirdAttachments.barrelAsset != null)
        {
            movementSpeedMultiplier *= thirdAttachments.barrelAsset.equipableMovementSpeedMultiplier;
            if (isAiming)
            {
                movementSpeedMultiplier *= thirdAttachments.barrelAsset.aimingMovementSpeedMultiplier;
            }
        }
        if (thirdAttachments.tacticalAsset != null)
        {
            movementSpeedMultiplier *= thirdAttachments.tacticalAsset.equipableMovementSpeedMultiplier;
            if (isAiming)
            {
                movementSpeedMultiplier *= thirdAttachments.tacticalAsset.aimingMovementSpeedMultiplier;
            }
        }
        if (thirdAttachments.sightAsset != null)
        {
            movementSpeedMultiplier *= thirdAttachments.sightAsset.equipableMovementSpeedMultiplier;
            if (isAiming)
            {
                movementSpeedMultiplier *= thirdAttachments.sightAsset.aimingMovementSpeedMultiplier;
            }
        }
        if (thirdAttachments.magazineAsset != null)
        {
            movementSpeedMultiplier *= thirdAttachments.magazineAsset.equipableMovementSpeedMultiplier;
            if (isAiming)
            {
                movementSpeedMultiplier *= thirdAttachments.magazineAsset.aimingMovementSpeedMultiplier;
            }
        }
        if (thirdAttachments.gripAsset != null)
        {
            movementSpeedMultiplier *= thirdAttachments.gripAsset.equipableMovementSpeedMultiplier;
            if (isAiming)
            {
                movementSpeedMultiplier *= thirdAttachments.gripAsset.aimingMovementSpeedMultiplier;
            }
        }
    }

    private void UpdateAimInDuration()
    {
        float num = equippedGunAsset.aimInDuration;
        if (thirdAttachments.barrelAsset != null)
        {
            num *= thirdAttachments.barrelAsset.aimDurationMultiplier;
        }
        if (thirdAttachments.tacticalAsset != null)
        {
            num *= thirdAttachments.tacticalAsset.aimDurationMultiplier;
        }
        if (thirdAttachments.sightAsset != null)
        {
            num *= thirdAttachments.sightAsset.aimDurationMultiplier;
        }
        if (thirdAttachments.magazineAsset != null)
        {
            num *= thirdAttachments.magazineAsset.aimDurationMultiplier;
        }
        if (thirdAttachments.gripAsset != null)
        {
            num *= thirdAttachments.gripAsset.aimDurationMultiplier;
        }
        maxAimingAccuracy = Mathf.Clamp(Mathf.RoundToInt(num * 50f), 1, 200);
        maxAimingAccuracyReciprocal = 1f / (float)maxAimingAccuracy;
        if (aimAccuracy > maxAimingAccuracy)
        {
            aimAccuracy = maxAimingAccuracy;
        }
        if (equippedGunAsset.shouldScaleAimAnimations)
        {
            float num2 = (float)maxAimingAccuracy / 50f;
            float animationLength = base.player.animator.GetAnimationLength("Aim_Start", scaled: false);
            base.player.animator.setAnimationSpeed("Aim_Start", animationLength / num2);
            float animationLength2 = base.player.animator.GetAnimationLength("Aim_Stop", scaled: false);
            base.player.animator.setAnimationSpeed("Aim_Stop", animationLength2 / num2);
        }
    }

    /// <summary>
    /// Note: This is the multiplier, not the m/s² acceleration.
    /// </summary>
    private float CalculateBulletGravityMultiplier()
    {
        float num = equippedGunAsset.bulletGravityMultiplier;
        if (thirdAttachments.barrelAsset != null)
        {
            num *= thirdAttachments.barrelAsset.BallisticGravityMultiplier;
        }
        if (thirdAttachments.tacticalAsset != null)
        {
            num *= thirdAttachments.tacticalAsset.BallisticGravityMultiplier;
        }
        if (thirdAttachments.sightAsset != null)
        {
            num *= thirdAttachments.sightAsset.BallisticGravityMultiplier;
        }
        if (thirdAttachments.magazineAsset != null)
        {
            num *= thirdAttachments.magazineAsset.BallisticGravityMultiplier;
        }
        if (thirdAttachments.gripAsset != null)
        {
            num *= thirdAttachments.gripAsset.BallisticGravityMultiplier;
        }
        return num;
    }

    private void DestroyLaser()
    {
        if (laserGameObject != null)
        {
            UnityEngine.Object.Destroy(laserGameObject);
            laserGameObject = null;
        }
        laserTransform = null;
        if (laserMaterial != null)
        {
            UnityEngine.Object.Destroy(laserMaterial);
            laserMaterial = null;
        }
    }

    /// <summary>
    /// Code common for regular gun and sentry gun.
    /// </summary>
    internal static void DetonateExplosiveMagazine(ItemMagazineAsset magazineAsset, Vector3 position, Player instigatingPlayer, ERagdollEffect ragdollEffect)
    {
        EffectAsset effectAsset = magazineAsset.FindExplosionEffect();
        if (effectAsset != null)
        {
            TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
            parameters.position = position;
            parameters.relevantDistance = EffectManager.MEDIUM;
            parameters.wasInstigatedByPlayer = true;
            EffectManager.triggerEffect(parameters);
        }
        CSteamID killer = ((instigatingPlayer != null) ? instigatingPlayer.channel.owner.playerID.steamID : CSteamID.Nil);
        ExplosionParameters parameters2 = new ExplosionParameters(position, magazineAsset.range, EDeathCause.SPLASH, killer);
        parameters2.playerDamage = magazineAsset.playerDamage;
        parameters2.zombieDamage = magazineAsset.zombieDamage;
        parameters2.animalDamage = magazineAsset.animalDamage;
        parameters2.barricadeDamage = magazineAsset.barricadeDamage;
        parameters2.structureDamage = magazineAsset.structureDamage;
        parameters2.vehicleDamage = magazineAsset.vehicleDamage;
        parameters2.resourceDamage = magazineAsset.resourceDamage;
        parameters2.objectDamage = magazineAsset.objectDamage;
        parameters2.damageOrigin = EDamageOrigin.Bullet_Explosion;
        parameters2.ragdollEffect = ragdollEffect;
        parameters2.launchSpeed = magazineAsset.explosionLaunchSpeed;
        DamageTool.explode(parameters2, out var kills);
        if (!(instigatingPlayer != null))
        {
            return;
        }
        foreach (EPlayerKill item in kills)
        {
            instigatingPlayer.sendStat(item);
        }
    }
}
