using System;
using System.Collections.Generic;
using SDG.Framework.Devkit;
using SDG.Framework.Water;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;
using Unturned.UnityEx;

namespace SDG.Unturned;

public class InteractableVehicle : Interactable, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    /// <summary>
    /// Temporary array for use with physics queries.
    /// </summary>
    private static Collider[] tempCollidersArray = new Collider[4];

    /// <summary>
    /// Temporary list for gathering materials.
    /// </summary>
    private static List<Material> tempMaterialsList = new List<Material>();

    private static List<Wheel> tempWheels = new List<Wheel>();

    private const float EXPLODE = 4f;

    private const ushort SMOKE_0_HEALTH_THRESHOLD = 100;

    private const ushort SMOKE_1_HEALTH_THRESHOLD = 200;

    /// <summary>
    /// Precursor to Net ID. Should eventually become obsolete.
    /// </summary>
    public uint instanceID;

    /// <summary>
    /// Asset ID. Essentially obsolete at this point.
    /// </summary>
    public ushort id;

    public Items trunkItems;

    public ushort skinID;

    public ushort mythicID;

    protected SkinAsset skinAsset;

    private List<Mesh> tempMesh;

    /// <summary>
    /// Used to restore vehicle materials when changing skin.
    /// </summary>
    private List<VehicleSkinMaterialChange> skinOriginalMaterials;

    protected Transform effectSlotsRoot;

    protected Transform[] effectSlots;

    protected MythicalEffectController[] effectSystems;

    /// <summary>
    /// Only used by trains. Constrains the train to this path.
    /// </summary>
    public ushort roadIndex;

    public float roadPosition;

    public ushort fuel;

    public ushort health;

    /// <summary>
    /// Nelson 2024-06-24: When first implementing batteries there was only the vanilla battery item, and it was
    /// fine to delete it when the charge reached zero. This may not be desirable, however, so zero now represents
    /// no battery item is present, and one represents the battery is completely drained but still there.
    /// </summary>
    public ushort batteryCharge;

    internal Guid batteryItemGuid;

    private float horned;

    internal bool _wasNaturallySpawned;

    protected VehicleEventHook eventHook;

    private bool _isDrowned;

    private float _lastDead;

    private float _lastUnderwater;

    private float _lastExploded;

    private float _slip;

    public bool isExploded;

    private float propellerRotationDegrees;

    private PropellerModel[] propellerModels;

    private GameObject exhaustGameObject;

    private bool isExhaustGameObjectActive;

    private bool isExhaustRateOverTimeZero;

    private ParticleSystem[] exhaustParticleSystems;

    private Transform steeringWheelModelTransform;

    private Transform overlapFront;

    private Transform overlapBack;

    private Transform pedalLeft;

    private Transform pedalRight;

    /// <summary>
    /// Front steering column of bicycles and motorcycles.
    /// </summary>
    private Transform frontModelTransform;

    private Quaternion steeringWheelRestLocalRotation;

    private Quaternion frontModelRestLocalRotation;

    private Transform waterCenterTransform;

    private Transform fire;

    private Transform smoke_0;

    private Transform smoke_1;

    [Obsolete("Replaced by MarkForReplicationUpdate. Will be removed in a future release.")]
    public List<VehicleStateUpdate> updates;

    /// <summary>
    /// If true, server should replicate latest state to clients.
    /// </summary>
    internal bool needsReplicationUpdate;

    private Material[] sirenMaterials;

    private bool sirenState;

    private List<GameObject> sirenGameObjects = new List<GameObject>();

    private List<GameObject> sirenGameObjects_0 = new List<GameObject>();

    private List<GameObject> sirenGameObjects_1 = new List<GameObject>();

    private bool _sirensOn;

    private Transform _headlights;

    private Material headlightsMaterial;

    private bool _headlightsOn;

    private Transform _taillights;

    private Material taillightsMaterial;

    private Material[] taillightMaterials;

    private bool _taillightsOn;

    private CSteamID _lockedOwner;

    private CSteamID _lockedGroup;

    private bool _isLocked;

    private VehicleAsset _asset;

    public float lastSeat;

    private Passenger[] _passengers;

    private Passenger[] _turrets;

    internal Wheel[] _wheels;

    public bool isHooked;

    private Transform buoyancy;

    private Transform hook;

    private List<HookInfo> hooked;

    private Vector3 lastUpdatedPos;

    private Vector3 interpTargetPosition;

    private Quaternion interpTargetRotation;

    private Vector3 real;

    private float lastTick;

    private float lastWeeoo;

    private AudioSource clipAudioSource;

    private WindZone windZone;

    private bool isRecovering;

    private float lastRecover;

    private bool isPhysical;

    private bool isFrozen;

    public bool isBlimpFloating;

    /// <summary>
    /// Used by several engine modes to represent an interpolated velocity target according to input.
    /// </summary>
    private float inputTargetVelocity;

    /// <summary>
    /// Set from inputTargetVelocity then multiplied by any factors which shouldn't affect the player's "target"
    /// speed ike boatTraction.
    /// </summary>
    private float inputEngineVelocity;

    /// <summary>
    /// Vehicles with buoyancy interpolate this value according to whether it's in the water, and multiply
    /// boat-related forces by it.
    /// </summary>
    private float boatTraction;

    private float batteryBuffer;

    private float fuelBurnBuffer;

    /// <summary>
    /// Rigidbody on the Vehicle prefab.
    /// (not called "rigidbody" because as of 2024-02-28 the deprecated "rigidbody" property still exists)
    /// </summary>
    private Rigidbody rootRigidbody;

    internal static readonly ClientInstanceMethod<Color32> SendPaintColor = ClientInstanceMethod<Color32>.Get(typeof(InteractableVehicle), "ReceivePaintColor");

    /// <summary>
    /// This check should really not be necessary, but somehow it is a recurring issue that servers get slowed down
    /// by something going wrong and the vehicle exploding a billion times leaving items everywhere.
    /// </summary>
    private bool hasDroppedScrapItemsAlready;

    private EngineCurvesComponent engineCurvesComponent;

    private float timeSinceLastGearChange;

    internal float latestGasInput;

    public bool hasDefaultCenterOfMass;

    public Vector3 defaultCenterOfMass;

    internal List<Collider> _vehicleColliders;

    private static List<Collider> _trainCarColliders = new List<Collider>(16);

    /// <summary>
    /// Transform used for exit physics queries.
    /// </summary>
    private Transform center;

    /// <summary>
    /// Skin material does not always need to be destroyed, so this is only valid if it should be destroyed.
    /// </summary>
    private Material skinMaterialToDestroy;

    /// <summary>
    /// Materials that should be destroyed when this vehicle is destroyed.
    /// </summary>
    private HashSet<Material> materialsToDestroy = new HashSet<Material>();

    /// <summary>
    /// Handles to unregister from DynamicWaterTransparentSort.
    /// </summary>
    private List<object> waterSortHandles = new List<object>();

    private static int PAINT_COLOR_ID = Shader.PropertyToID("_PaintColor");

    /// <summary>
    /// Materials to set _PaintColor on.
    /// </summary>
    private List<Material> paintableMaterials;

    /// <summary>
    /// Materials to move UVs in sync with wheels.
    /// </summary>
    private List<CrawlerTrackTilingMaterialInstance> crawlerTrackMaterials;

    /// <summary>
    /// Time.time decayTimer was last updated.
    /// </summary>
    internal float decayLastUpdateTime;

    /// <summary>
    /// Seconds since vehicle was interacted with.
    /// </summary>
    internal float decayTimer;

    /// <summary>
    /// Fractional damage counter.
    /// </summary>
    internal float decayPendingDamage;

    /// <summary>
    /// transform.position used to test whether vehicle is moving.
    /// </summary>
    internal Vector3 decayLastUpdatePosition;

    [Obsolete]
    public bool isUpdated;

    public Road road { get; protected set; }

    public Color32 PaintColor { get; internal set; }

    /// <summary>
    /// Is this vehicle inside a safezone?
    /// </summary>
    public bool isInsideSafezone { get; protected set; }

    public SafezoneNode insideSafezoneNode { get; protected set; }

    /// <summary>
    /// Duration in seconds since this vehicle entered a safezone,
    /// or -1 if it's not in a safezone.
    /// </summary>
    public float timeInsideSafezone { get; protected set; }

    /// <summary>
    /// Should askDamage requests currently be ignored because we are inside a safezone?
    /// </summary>
    public bool isInsideNoDamageZone
    {
        get
        {
            if (insideSafezoneNode != null)
            {
                return insideSafezoneNode.noWeapons;
            }
            return false;
        }
    }

    public bool usesFuel
    {
        get
        {
            if (!asset.isStaminaPowered)
            {
                return !asset.isBatteryPowered;
            }
            return false;
        }
    }

    public bool usesBattery
    {
        get
        {
            if (asset.isStaminaPowered)
            {
                return asset.isBatteryPowered;
            }
            return true;
        }
    }

    public bool usesHealth => asset.engine != EEngine.TRAIN;

    public bool isBoosting { get; protected set; }

    /// <summary>
    /// Nelson 2024-06-24: This property is confusing, especially with isEnginePowered, but essentially represents
    /// starting the engine ignition when a player enters the driver's seat. If true, there's a driver, there was
    /// sufficient battery to start (or battery not required), and the engine wasn't underwater.
    /// </summary>
    public bool isEngineOn { get; protected set; }

    public bool isEnginePowered
    {
        get
        {
            if (asset.isStaminaPowered)
            {
                return true;
            }
            if (asset.isBatteryPowered)
            {
                return HasBatteryWithCharge;
            }
            if (fuel > 0)
            {
                return isEngineOn;
            }
            return false;
        }
    }

    /// <summary>
    /// Doesn't imply the vehicle *uses* batteries, only that it contains a battery item with some charge left.
    /// </summary>
    public bool HasBatteryWithCharge => batteryCharge > 1;

    /// <summary>
    /// Doesn't imply the vehicle *uses* batteries, only that it contains a (potentially uncharged) battery item.
    /// </summary>
    public bool ContainsBatteryItem => batteryCharge > 0;

    public bool isBatteryFull
    {
        get
        {
            if (usesBattery)
            {
                return batteryCharge >= 10000;
            }
            return true;
        }
    }

    /// <summary>
    /// Nelson 2024-11-13: Adding this primarily to indicate whether a vehicle was spawned by the level versus
    /// placed by a player or bought from a vendor. This way if the number of "naturally"-spawned vehicles is below
    /// a certain threshold the level can spawn more. (e.g., a server where players have hoarded a bunch of
    /// vendor-purchased vehicles and no default vehicles are left for new players.)
    ///
    /// Only available on the server.
    /// Defaults to true for old saves to prevent suddenly spawning a bunch more vehicles.
    /// </summary>
    public bool WasNaturallySpawned
    {
        get
        {
            return _wasNaturallySpawned;
        }
        set
        {
            _wasNaturallySpawned = value;
        }
    }

    public bool canUseHorn
    {
        get
        {
            if (Time.realtimeSinceStartup - horned > 0.5f)
            {
                if (usesBattery)
                {
                    return HasBatteryWithCharge;
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Whether the player can shoot their equipped turret.
    /// </summary>
    public bool canUseTurret => !isDead;

    public bool canTurnOnLights
    {
        get
        {
            if (!usesBattery || HasBatteryWithCharge)
            {
                return !isUnderwater;
            }
            return false;
        }
    }

    public bool isRefillable
    {
        get
        {
            if (usesFuel && fuel < asset.fuel && !isDriven)
            {
                return !isExploded;
            }
            return false;
        }
    }

    public bool isSiphonable
    {
        get
        {
            if (usesFuel && fuel > 0 && !isDriven)
            {
                return !isExploded;
            }
            return false;
        }
    }

    public bool isRepaired => health == asset.health;

    public bool isDriven
    {
        get
        {
            if (passengers != null)
            {
                return passengers[0].player != null;
            }
            return false;
        }
    }

    /// <summary>
    /// Do any of the passenger seats have a player?
    /// </summary>
    public bool anySeatsOccupied
    {
        get
        {
            if (passengers != null)
            {
                Passenger[] array = passengers;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].player != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public bool isDriver
    {
        get
        {
            if (!Dedicator.IsDedicatedServer)
            {
                return checkDriver(Provider.client);
            }
            return false;
        }
    }

    public bool isEmpty
    {
        get
        {
            for (byte b = 0; b < passengers.Length; b++)
            {
                if (passengers[b].player != null)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public bool isDrowned => _isDrowned;

    public bool isUnderwater
    {
        get
        {
            if (waterCenterTransform != null)
            {
                return WaterUtility.isPointUnderwater(waterCenterTransform.position);
            }
            return WaterUtility.isPointUnderwater(base.transform.position + new Vector3(0f, 1f, 0f));
        }
    }

    public bool isBatteryReplaceable
    {
        get
        {
            if (usesBattery && !isBatteryFull && !isDriven)
            {
                return !isExploded;
            }
            return false;
        }
    }

    public bool isTireReplaceable
    {
        get
        {
            if (!isDriven && !isExploded)
            {
                return asset.canTiresBeDamaged;
            }
            return false;
        }
    }

    public bool canBeDamaged => asset.engine != EEngine.TRAIN;

    public bool isGoingToRespawn
    {
        get
        {
            if (!isExploded)
            {
                return isDrowned;
            }
            return true;
        }
    }

    /// <summary>
    /// When the server saves it doesn't include any cleared vehicles.
    /// </summary>
    public bool isAutoClearable
    {
        get
        {
            if (isExploded)
            {
                return true;
            }
            if (isUnderwater && buoyancy == null)
            {
                return true;
            }
            if (asset != null)
            {
                if (asset.engine == EEngine.BOAT && fuel == 0 && !asset.isBatteryPowered)
                {
                    return true;
                }
                _ = asset.engine;
                return false;
            }
            return false;
        }
    }

    public float lastDead => _lastDead;

    public float lastUnderwater => _lastUnderwater;

    public float lastExploded => _lastExploded;

    public float slip => _slip;

    public bool isDead => health == 0;

    /// <summary>
    /// Magnitude of rigidbody velocity, replicated by current simulation owner.
    /// </summary>
    public float ReplicatedSpeed { get; private set; }

    /// <summary>
    /// Rigidbody velocity along forward axis, replicated by current simulation owner.
    /// </summary>
    public float ReplicatedForwardVelocity { get; private set; }

    /// <summary>
    /// Replicated by current simulation owner. Target velocity used, e.g., for helicopter engine speed.
    /// </summary>
    public float ReplicatedVelocityInput { get; private set; }

    /// <summary>
    /// Animated toward ReplicatedForwardVelocity.
    /// </summary>
    public float AnimatedForwardVelocity { get; private set; }

    /// <summary>
    /// Animated toward ReplicatedVelocityInput.
    /// </summary>
    public float AnimatedVelocityInput { get; private set; }

    /// <summary>
    /// [-1.0, 1.0] Available on both client and server.
    /// </summary>
    public float ReplicatedSteeringInput { get; private set; }

    /// <summary>
    /// Animated towards replicated steering angle. Used for steering wheel and front steering column.
    /// Non-simulating wheels (e.g., car driven by remote client) use this as steering angle multiplied by their
    /// per-wheel <see cref="F:SDG.Unturned.VehicleWheelConfiguration.steeringAngleMultiplier" />.
    /// </summary>
    public float AnimatedSteeringAngle { get; private set; }

    public TrainCar[] trainCars { get; protected set; }

    public bool sirensOn => _sirensOn;

    public Transform headlights => _headlights;

    public bool headlightsOn => _headlightsOn;

    public Transform taillights => _taillights;

    public bool taillightsOn => _taillightsOn;

    public CSteamID lockedOwner => _lockedOwner;

    public CSteamID lockedGroup => _lockedGroup;

    public bool isLocked => _isLocked;

    public bool isSkinned => skinID != 0;

    public VehicleAsset asset => _asset;

    public Passenger[] passengers => _passengers;

    public Passenger[] turrets => _turrets;

    public Wheel[] tires => _wheels;

    private bool usesGravity => asset.engine != EEngine.TRAIN;

    private bool isKinematic => !usesGravity;

    public bool IsEligibleForExplosionDamage
    {
        get
        {
            if (isDead)
            {
                return false;
            }
            if (asset == null || !asset.isVulnerableToExplosions)
            {
                return false;
            }
            return true;
        }
    }

    public byte tireAliveMask
    {
        get
        {
            int num = 0;
            for (byte b = 0; b < Mathf.Min(8, _wheels.Length); b++)
            {
                if (_wheels[b].isAlive)
                {
                    int num2 = 1 << (int)b;
                    num |= num2;
                }
            }
            return (byte)num;
        }
        set
        {
            for (byte b = 0; b < Mathf.Min(8, _wheels.Length); b++)
            {
                if (!(_wheels[b].wheel == null))
                {
                    int num = 1 << (int)b;
                    _wheels[b].isAlive = (value & num) == num;
                }
            }
        }
    }

    /// <summary>
    /// Can a safe exit point currently be found?
    ///
    /// Called when considering to add a new passenger to prevent players from entering
    /// a vehicle that they wouldn't be able to exit properly.
    /// </summary>
    public bool isExitable
    {
        get
        {
            Vector3 point;
            byte angle;
            return tryGetExit(0, out point, out angle);
        }
    }

    /// <summary>
    /// -1 is reverse.
    /// 0 is neutral.
    /// +1 is index 0 in gear ratios list.
    /// </summary>
    public int GearNumber { get; internal set; }

    /// <summary>
    /// Engine RPM replicated by current simulation owner.
    /// </summary>
    public float ReplicatedEngineRpm { get; internal set; }

    /// <summary>
    /// Animated toward ReplicatedEngineRpm.
    /// </summary>
    public float AnimatedEngineRpm { get; private set; }

    public IEnumerable<Collider> vehicleColliders => _vehicleColliders;

    [Obsolete("Replaced by ReplicatedSteeringInput")]
    public int turn => Mathf.RoundToInt(ReplicatedSteeringInput);

    [Obsolete("Replaced by AnimatedSteeringAngle")]
    public float steer => AnimatedSteeringAngle;

    [Obsolete("Replaced by ReplicatedSpeed")]
    public float speed => ReplicatedSpeed * Mathf.Sign(ReplicatedForwardVelocity);

    [Obsolete("Replaced by ReplicatedForwardVelocity")]
    public float physicsSpeed => ReplicatedForwardVelocity;

    [Obsolete("Replaced by GetReplicatedForwardSpeedPercentageOfTargetSpeed")]
    public float factor => GetReplicatedForwardSpeedPercentageOfTargetSpeed();

    [Obsolete("Clarified with HasBatteryWithCharge and ContainsBatteryItem properties.")]
    public bool hasBattery
    {
        get
        {
            if (usesBattery)
            {
                return batteryCharge > 1;
            }
            return true;
        }
    }

    public event VehiclePassengersUpdated onPassengersUpdated;

    public event VehicleLockUpdated onLockUpdated;

    public event VehicleHeadlightsUpdated onHeadlightsUpdated;

    public event VehicleTaillightsUpdated onTaillightsUpdated;

    public event VehicleSirensUpdated onSirensUpdated;

    public event VehicleBlimpUpdated onBlimpUpdated;

    public event VehicleBatteryChangedHandler batteryChanged;

    public event VehicleSkinChangedHandler skinChanged;

    public static event Action<InteractableVehicle> OnHealthChanged_Global;

    public static event Action<InteractableVehicle> OnLockChanged_Global;

    public static event Action<InteractableVehicle> OnFuelChanged_Global;

    public static event Action<InteractableVehicle> OnBatteryLevelChanged_Global;

    public static event Action<InteractableVehicle, int> OnPassengerAdded_Global;

    public static event Action<InteractableVehicle, int, int> OnPassengerChangedSeats_Global;

    public static event Action<InteractableVehicle, int, Player> OnPassengerRemoved_Global;

    public event System.Action OnIsDrownedChanged;

    /// <summary>
    /// Unfortunately old netcode sends train position as a Vector3 using the X channel, but new code only supports
    /// [-4096, 4096) so we pack the train position into all three channels. Eventually this should be cleaned up.
    /// </summary>
    internal static Vector3 PackRoadPosition(float roadPosition)
    {
        if (roadPosition >= 16384f)
        {
            return new Vector3(4096f, 4096f, roadPosition - 20480f);
        }
        if (roadPosition >= 8192f)
        {
            return new Vector3(4096f, roadPosition - 12288f, -4096f);
        }
        return new Vector3(roadPosition - 4096f, -4096f, -4096f);
    }

    internal static float UnpackRoadPosition(Vector3 roadPosition)
    {
        return roadPosition.x + roadPosition.y + roadPosition.z + 12288f;
    }

    /// <summary>
    /// [0, 1] If forward velocity is greater than zero, get normalized by target forward speed. If less than zero,
    /// get normalized by target reverse speed. Result is always positive.
    /// </summary>
    public float GetReplicatedForwardSpeedPercentageOfTargetSpeed()
    {
        if (ReplicatedForwardVelocity > 0f)
        {
            return Mathf.Clamp01(ReplicatedForwardVelocity / asset.TargetForwardVelocity);
        }
        return Mathf.Clamp01(ReplicatedForwardVelocity / asset.TargetReverseVelocity);
    }

    public float GetAnimatedForwardSpeedPercentageOfTargetSpeed()
    {
        if (AnimatedForwardVelocity > 0f)
        {
            return Mathf.Clamp01(AnimatedForwardVelocity / asset.TargetForwardVelocity);
        }
        return Mathf.Clamp01(AnimatedForwardVelocity / asset.TargetReverseVelocity);
    }

    internal Wheel GetWheelAtIndex(int index)
    {
        if (_wheels != null && index >= 0 && index < _wheels.Length)
        {
            return _wheels[index];
        }
        return null;
    }

    public bool Equals(IExplosionDamageable obj)
    {
        return this == obj;
    }

    public Vector3 GetClosestPointToExplosion(Vector3 explosionCenter)
    {
        return getClosestPointOnHull(explosionCenter);
    }

    public void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        if (!damageParameters.shouldAffectVehicles)
        {
            return;
        }
        Vector3 vector = damageParameters.closestPoint - explosionParameters.point;
        float magnitude = vector.magnitude;
        if (magnitude > explosionParameters.damageRadius)
        {
            return;
        }
        float num = 1f - magnitude / explosionParameters.damageRadius;
        Vector3 direction = vector / magnitude;
        if (damageParameters.LineOfSightTest(explosionParameters.point, direction, magnitude, out var hit) && hit.transform != null)
        {
            if (!hit.transform.IsChildOf(base.transform))
            {
                return;
            }
            num *= asset.childExplosionArmorMultiplier;
            num *= Provider.modeConfigData.Vehicles.Child_Explosion_Armor_Multiplier;
        }
        VehicleManager.damage(this, explosionParameters.vehicleDamage, num, canRepair: false, explosionParameters.killer, explosionParameters.damageOrigin);
    }

    /// <summary>
    /// Primarily for backwards compatibility with plugins. Previously, multiple "updates" could be queued per
    /// vehicle and sent to clients. This list was public, unfortunately, so plugins may rely on submitting vehicle
    /// updates. After making it obsolete each vehicle can only be flagged as needing a replication update, and
    /// this is reset after each server replication update.
    /// </summary>
    public void MarkForReplicationUpdate()
    {
        if (!needsReplicationUpdate)
        {
            needsReplicationUpdate = true;
            VehicleManager.instance.vehiclesNeedingReplicationUpdate.Add(this);
        }
    }

    public void ResetDecayTimer()
    {
        decayTimer = 0f;
        decayPendingDamage = 0f;
        decayLastUpdatePosition = base.transform.position;
    }

    /// <summary>
    /// Is player currently allowed to repair this vehicle?
    /// </summary>
    public bool canPlayerRepair(Player player)
    {
        if (!asset.canRepairWhileSeated)
        {
            return player.movement.getVehicle() != this;
        }
        return true;
    }

    public void replaceBattery(Player player, byte quality, Guid newBatteryItemGuid)
    {
        if (ContainsBatteryItem)
        {
            GiveBatteryItem(player);
        }
        batteryItemGuid = newBatteryItemGuid;
        int num = Mathf.Clamp(Mathf.RoundToInt(quality * 100), 1, 10000);
        VehicleManager.sendVehicleBatteryCharge(this, (ushort)num);
        ResetDecayTimer();
    }

    /// <summary>
    /// Give battery item to player and set battery charge to zero.
    /// </summary>
    public void stealBattery(Player player)
    {
        if (ContainsBatteryItem)
        {
            GiveBatteryItem(player);
            VehicleManager.sendVehicleBatteryCharge(this, 0);
        }
    }

    /// <summary>
    /// Nelson 2024-06-24: Previously, this wouldn't give an item to the player if the quality was zero. Now it
    /// trusts the caller to validate we have a battery item to give, and respects <see cref="P:SDG.Unturned.ItemAsset.shouldDeleteAtZeroQuality" />.
    /// </summary>
    private void GiveBatteryItem(Player player)
    {
        byte b = (byte)Mathf.FloorToInt((float)(int)batteryCharge / 100f);
        if (batteryItemGuid == Guid.Empty)
        {
            batteryItemGuid = asset.defaultBatteryGuid;
        }
        if (Assets.find(batteryItemGuid) is ItemAsset itemAsset && (!itemAsset.shouldDeleteAtZeroQuality || b >= 1))
        {
            Item item = new Item(itemAsset.id, 1, b);
            player.inventory.forceAddItem(item, auto: false);
        }
    }

    public void sendTireAliveMaskUpdate()
    {
        VehicleManager.sendVehicleTireAliveMask(this, tireAliveMask);
    }

    /// <summary>
    /// Can a tire item be used with this vehicle?
    /// </summary>
    public bool isTireCompatible(ushort itemID)
    {
        if (asset != null)
        {
            return asset.tireID == itemID;
        }
        return false;
    }

    public void askRepairTire(int index)
    {
        if (index >= 0 && index < _wheels.Length)
        {
            _wheels[index].askRepair();
        }
    }

    public void askDamageTire(int index)
    {
        if (!isInsideNoDamageZone && index >= 0 && index < _wheels.Length && (asset == null || asset.canTiresBeDamaged))
        {
            _wheels[index].askDamage();
        }
    }

    /// <summary>
    /// Find the index of the wheel collider that contains this position.
    /// </summary>
    public int getHitTireIndex(Vector3 position)
    {
        for (int i = 0; i < _wheels.Length; i++)
        {
            WheelCollider wheel = _wheels[i].wheel;
            if (!(wheel == null) && (wheel.transform.position - position).sqrMagnitude < wheel.radius * wheel.radius)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find the index of the wheel collider closest to this position, or -1 if not near any.
    /// </summary>
    public int getClosestAliveTireIndex(Vector3 position, bool isAlive)
    {
        int result = -1;
        float num = 16f;
        for (int i = 0; i < _wheels.Length; i++)
        {
            if (_wheels[i].isAlive == isAlive && !(_wheels[i].wheel == null))
            {
                float sqrMagnitude = (_wheels[i].wheel.transform.position - position).sqrMagnitude;
                if (sqrMagnitude < num)
                {
                    result = i;
                    num = sqrMagnitude;
                }
            }
        }
        return result;
    }

    public void getDisplayFuel(out ushort currentFuel, out ushort maxFuel)
    {
        if (usesFuel)
        {
            currentFuel = fuel;
            maxFuel = asset.fuel;
            return;
        }
        if (passengers[0].player != null && passengers[0].player.player != null)
        {
            currentFuel = passengers[0].player.player.life.stamina;
        }
        else if (Player.player != null)
        {
            currentFuel = Player.player.life.stamina;
        }
        else
        {
            currentFuel = 0;
        }
        maxFuel = 100;
    }

    public void askBurnFuel(ushort amount)
    {
        if (amount != 0 && !isExploded)
        {
            if (amount >= fuel)
            {
                fuel = 0;
            }
            else
            {
                fuel -= amount;
            }
        }
    }

    public void askFillFuel(ushort amount)
    {
        if (amount != 0 && !isExploded)
        {
            if (amount >= asset.fuel - fuel)
            {
                fuel = asset.fuel;
            }
            else
            {
                fuel += amount;
            }
            VehicleManager.sendVehicleFuel(this, fuel);
            ResetDecayTimer();
        }
    }

    /// <summary>
    /// Called during simulate at fixed rate.
    /// </summary>
    protected void simulateBurnFuel()
    {
        if (usesFuel && isEngineOn)
        {
            float rATE = PlayerInput.RATE;
            fuelBurnBuffer += rATE * asset.fuelBurnRate;
            ushort num = (ushort)Mathf.FloorToInt(fuelBurnBuffer);
            if (num > 0)
            {
                fuelBurnBuffer -= (int)num;
                askBurnFuel(num);
            }
        }
    }

    public void askBurnBattery(ushort amount)
    {
        if (amount != 0 && !isExploded && batteryCharge >= 1)
        {
            if (amount >= batteryCharge - 1)
            {
                batteryCharge = 1;
            }
            else
            {
                batteryCharge -= amount;
            }
        }
    }

    public void askChargeBattery(ushort amount)
    {
        if (amount != 0 && !isExploded)
        {
            if (amount >= 10000 - batteryCharge)
            {
                batteryCharge = 10000;
            }
            else
            {
                batteryCharge += amount;
            }
        }
    }

    public void sendBatteryChargeUpdate()
    {
        VehicleManager.sendVehicleBatteryCharge(this, batteryCharge);
    }

    public void askDamage(ushort amount, bool canRepair)
    {
        if (isInsideNoDamageZone || amount == 0)
        {
            return;
        }
        if (isDead)
        {
            if (!canRepair)
            {
                explode();
            }
            return;
        }
        if (amount >= health)
        {
            health = 0;
        }
        else
        {
            health -= amount;
        }
        VehicleManager.sendVehicleHealth(this, health);
        if (isDead && !canRepair)
        {
            explode();
        }
    }

    public void askRepair(ushort amount)
    {
        if (amount != 0 && !isExploded)
        {
            if (amount >= asset.health - health)
            {
                health = asset.health;
            }
            else
            {
                health += amount;
            }
            VehicleManager.sendVehicleHealth(this, health);
        }
    }

    private void explode()
    {
        Vector3 force = new Vector3(UnityEngine.Random.Range(asset.minExplosionForce.x, asset.maxExplosionForce.x), UnityEngine.Random.Range(asset.minExplosionForce.y, asset.maxExplosionForce.y), UnityEngine.Random.Range(asset.minExplosionForce.z, asset.maxExplosionForce.z));
        rootRigidbody.AddForce(force);
        rootRigidbody.AddTorque(16f, 0f, 0f);
        dropTrunkItems();
        if (asset.ShouldExplosionCauseDamage)
        {
            DamageTool.explode(base.transform.position, 8f, EDeathCause.VEHICLE, CSteamID.Nil, 200f, 200f, 200f, 0f, 0f, 500f, 2000f, 500f, out var _, EExplosionDamageType.CONVENTIONAL, 32f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Vehicle_Explosion);
        }
        for (int i = 0; i < passengers.Length; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger == null)
            {
                continue;
            }
            SteamPlayer player = passenger.player;
            if (player == null)
            {
                continue;
            }
            Player player2 = player.player;
            if (!(player2 == null) && !player2.life.isDead)
            {
                if (asset.ShouldExplosionCauseDamage)
                {
                    player2.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SPINE, CSteamID.Nil, out var _);
                }
                else
                {
                    VehicleManager.forceRemovePlayer(this, player.playerID.steamID);
                }
            }
        }
        DropScrapItems();
        VehicleManager.sendVehicleExploded(this);
        EffectAsset effectAsset = asset.FindExplosionEffectAsset();
        if (effectAsset != null)
        {
            TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
            parameters.position = base.transform.position;
            parameters.relevantDistance = EffectManager.LARGE;
            EffectManager.triggerEffect(parameters);
        }
    }

    public bool checkEnter(CSteamID enemyPlayer, CSteamID enemyGroup)
    {
        if (isHooked)
        {
            return false;
        }
        if (Provider.isServer && !Dedicator.IsDedicatedServer)
        {
            return true;
        }
        if (isLocked && !(enemyPlayer == lockedOwner))
        {
            if (lockedGroup != CSteamID.Nil)
            {
                return enemyGroup == lockedGroup;
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Is a given player allowed access to this vehicle?
    /// </summary>
    public bool checkEnter(Player player)
    {
        if (player == null)
        {
            return false;
        }
        CSteamID steamID = player.channel.owner.playerID.steamID;
        CSteamID groupID = player.quests.groupID;
        return checkEnter(steamID, groupID);
    }

    /// <summary>
    /// If true, sentry ignores this vehicle early in target scanning.
    /// Friendly if locked by owner/group of sentry, or driven by owner/group of sentry.
    /// </summary>
    public bool IsFriendlyToSentry(InteractableSentry sentry)
    {
        if (checkEnter(sentry.owner, sentry.group))
        {
            return true;
        }
        Player driverPlayer = GetDriverPlayer();
        if (driverPlayer != null)
        {
            if (!(driverPlayer.channel.owner.playerID.steamID == sentry.owner))
            {
                return driverPlayer.quests.isMemberOfGroup(sentry.group);
            }
            return true;
        }
        return false;
    }

    public Vector3 GetSentryTargetingPoint()
    {
        if (!(center != null))
        {
            return base.transform.position;
        }
        return center.position;
    }

    public override bool checkUseable()
    {
        if (Player.player == null || (base.transform.position - Player.player.transform.position).sqrMagnitude > 100f)
        {
            return false;
        }
        if (!isExploded)
        {
            return checkEnter(Provider.client, Player.player.quests.groupID);
        }
        return false;
    }

    public override void use()
    {
        VehicleManager.enterVehicle(this);
    }

    public override bool checkHighlight(out Color color)
    {
        color = ItemTool.getRarityColorHighlight(asset.rarity);
        return true;
    }

    public override bool checkHint(out EPlayerMessage message, out string text, out Color color)
    {
        if (checkUseable())
        {
            message = EPlayerMessage.VEHICLE_ENTER;
            text = asset.vehicleName;
            color = ItemTool.getRarityColorUI(asset.rarity);
        }
        else
        {
            if (Player.player == null || (base.transform.position - Player.player.transform.position).sqrMagnitude > 100f)
            {
                message = EPlayerMessage.BLOCKED;
            }
            else
            {
                message = EPlayerMessage.LOCKED;
            }
            text = "";
            color = Color.white;
        }
        return !isExploded;
    }

    public void updateVehicle()
    {
        lastUpdatedPos = base.transform.position;
        interpTargetPosition = base.transform.position;
        interpTargetRotation = base.transform.rotation;
        real = base.transform.position;
        isRecovering = false;
        lastRecover = Time.realtimeSinceStartup;
        isFrozen = false;
    }

    /// <summary>
    /// Average vehicle-space position of wheel bases.
    /// </summary>
    private Vector3? calculateAverageLocalTireContactPosition()
    {
        if (_wheels == null)
        {
            return null;
        }
        Vector3 zero = Vector3.zero;
        int num = 0;
        Wheel[] wheels = _wheels;
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheel = wheels[i].wheel;
            if (!(wheel == null))
            {
                Vector3 position = wheel.transform.TransformPoint(wheel.center - new Vector3(0f, wheel.radius, 0f));
                Vector3 vector = base.transform.InverseTransformPoint(position);
                zero += vector;
                num++;
            }
        }
        if (num > 0)
        {
            return zero / num;
        }
        return null;
    }

    public void updatePhysics()
    {
        if (checkDriver(Provider.client) || (Provider.isServer && !isDriven))
        {
            rootRigidbody.useGravity = usesGravity;
            rootRigidbody.isKinematic = isKinematic;
            isPhysical = true;
            if (!isExploded)
            {
                if (_wheels != null)
                {
                    Wheel[] wheels = _wheels;
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        wheels[i].isPhysical = true;
                    }
                }
                if (buoyancy != null)
                {
                    buoyancy.gameObject.SetActive(value: true);
                }
            }
        }
        else
        {
            rootRigidbody.useGravity = false;
            rootRigidbody.isKinematic = true;
            isPhysical = false;
            if (_wheels != null)
            {
                Wheel[] wheels = _wheels;
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].isPhysical = false;
                }
            }
            if (buoyancy != null)
            {
                buoyancy.gameObject.SetActive(value: false);
            }
        }
        if (!hasDefaultCenterOfMass)
        {
            hasDefaultCenterOfMass = true;
            defaultCenterOfMass = rootRigidbody.centerOfMass;
        }
        Vector3 centerOfMass;
        if (asset.hasCenterOfMassOverride)
        {
            centerOfMass = asset.centerOfMass;
        }
        else
        {
            Transform transform = base.transform.Find("Cog");
            if ((bool)transform)
            {
                centerOfMass = transform.localPosition;
            }
            else
            {
                centerOfMass = new Vector3(0f, -0.25f, 0f);
                if (asset.engine == EEngine.CAR)
                {
                    Vector3? vector = calculateAverageLocalTireContactPosition();
                    if (vector.HasValue)
                    {
                        centerOfMass = vector.Value;
                    }
                }
            }
        }
        rootRigidbody.centerOfMass = centerOfMass;
    }

    public void updateEngine()
    {
        synchronizeTaillights();
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        foreach (GameObject sirenGameObject in sirenGameObjects)
        {
            AudioSource component = sirenGameObject.GetComponent<AudioSource>();
            if (component != null)
            {
                component.enabled = isDriven;
            }
        }
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public void ReceivePaintColor(Color32 newPaintColor)
    {
        PaintColor = newPaintColor;
        ApplyPaintColor();
    }

    public void ServerSetPaintColor(Color32 newPaintColor)
    {
        if (!PaintColor.Equals(newPaintColor))
        {
            SendPaintColor.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), newPaintColor);
        }
    }

    public void tellLocked(CSteamID owner, CSteamID group, bool locked)
    {
        _lockedOwner = owner;
        _lockedGroup = group;
        _isLocked = locked;
        this.onLockUpdated?.Invoke();
        if (eventHook != null)
        {
            if (locked)
            {
                eventHook.OnLocked.TryInvoke(this);
            }
            else
            {
                eventHook.OnUnlocked.TryInvoke(this);
            }
        }
        InteractableVehicle.OnLockChanged_Global.TryInvoke("OnLockChanged_Global", this);
    }

    public void tellSkin(ushort newSkinID, ushort newMythicID)
    {
        skinID = newSkinID;
        mythicID = newMythicID;
        updateSkin();
        this.skinChanged?.Invoke();
    }

    public void updateSkin()
    {
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        skinAsset = Assets.find(EAssetType.SKIN, skinID) as SkinAsset;
        if (tempMesh != null)
        {
            HighlighterTool.remesh(base.transform, tempMesh, null);
        }
        if (skinOriginalMaterials != null && skinOriginalMaterials.Count > 0)
        {
            foreach (VehicleSkinMaterialChange skinOriginalMaterial in skinOriginalMaterials)
            {
                if (skinOriginalMaterial.shared)
                {
                    skinOriginalMaterial.renderer.sharedMaterial = skinOriginalMaterial.originalMaterial;
                }
                else
                {
                    skinOriginalMaterial.renderer.material = skinOriginalMaterial.originalMaterial;
                }
            }
            skinOriginalMaterials.Clear();
        }
        if (skinMaterialToDestroy != null)
        {
            UnityEngine.Object.Destroy(skinMaterialToDestroy);
            skinMaterialToDestroy = null;
        }
        if (effectSystems != null)
        {
            for (int i = 0; i < effectSystems.Length; i++)
            {
                MythicalEffectController mythicalEffectController = effectSystems[i];
                if (mythicalEffectController != null)
                {
                    UnityEngine.Object.Destroy(mythicalEffectController);
                }
            }
        }
        if (skinAsset == null)
        {
            return;
        }
        VehicleAsset vehicleAsset = asset.FindSharedSkinVehicleAsset();
        if (mythicID != 0)
        {
            if (effectSlotsRoot == null)
            {
                effectSlotsRoot = base.transform.Find("Effect_Slots");
                if (effectSlotsRoot == null)
                {
                    effectSlotsRoot = UnityEngine.Object.Instantiate(vehicleAsset.GetOrLoadModel().transform.Find("Effect_Slots").gameObject).transform;
                    effectSlotsRoot.parent = base.transform;
                    effectSlotsRoot.name = "Effect_Slots";
                    effectSlotsRoot.localPosition = Vector3.zero;
                    effectSlotsRoot.localRotation = Quaternion.identity;
                    effectSlotsRoot.localScale = Vector3.one;
                }
                effectSlots = new Transform[effectSlotsRoot.childCount];
                for (int j = 0; j < effectSlots.Length; j++)
                {
                    effectSlots[j] = effectSlotsRoot.GetChild(j);
                }
                effectSystems = new MythicalEffectController[effectSlots.Length];
            }
            ItemTool.ApplyMythicalEffectToMultipleTransforms(effectSlots, effectSystems, mythicID, EEffectType.AREA);
        }
        if (skinAsset.overrideMeshes != null && skinAsset.overrideMeshes.Count > 0)
        {
            if (tempMesh == null)
            {
                tempMesh = new List<Mesh>();
                HighlighterTool.remesh(base.transform, skinAsset.overrideMeshes, tempMesh);
            }
            else
            {
                HighlighterTool.remesh(base.transform, skinAsset.overrideMeshes, null);
            }
        }
        if (!(skinAsset.primarySkin != null))
        {
            return;
        }
        Material skinMaterial;
        if (skinAsset.isPattern)
        {
            Material material = UnityEngine.Object.Instantiate(skinAsset.primarySkin);
            material.SetTexture("_AlbedoBase", vehicleAsset.albedoBase);
            material.SetTexture("_MetallicBase", vehicleAsset.metallicBase);
            material.SetTexture("_EmissionBase", vehicleAsset.emissionBase);
            skinMaterial = material;
            skinMaterialToDestroy = material;
        }
        else
        {
            skinMaterial = skinAsset.primarySkin;
            skinMaterialToDestroy = null;
        }
        if (skinOriginalMaterials == null)
        {
            skinOriginalMaterials = new List<VehicleSkinMaterialChange>();
        }
        else
        {
            skinOriginalMaterials.Clear();
        }
        bool shared = paintableMaterials == null || paintableMaterials.Count < 1;
        Renderer component = GetComponent<Renderer>();
        if (component != null)
        {
            ApplySkinToRenderer(component, skinMaterial, shared);
            return;
        }
        for (int k = 0; k < 4; k++)
        {
            Transform transform = base.transform.Find("Model_" + k);
            if (!(transform == null))
            {
                component = transform.GetComponent<Renderer>();
                if (component != null)
                {
                    ApplySkinToRenderer(component, skinMaterial, shared);
                }
            }
        }
    }

    public void tellSirens(bool on)
    {
        _sirensOn = on;
        if (!Dedicator.IsDedicatedServer)
        {
            foreach (GameObject sirenGameObject in sirenGameObjects)
            {
                sirenGameObject.SetActive(sirensOn);
            }
            if (sirenMaterials != null)
            {
                for (int i = 0; i < sirenMaterials.Length; i++)
                {
                    if (sirenMaterials[i] != null)
                    {
                        sirenMaterials[i].SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }
        this.onSirensUpdated?.Invoke();
    }

    public void tellBlimp(bool on)
    {
        isBlimpFloating = on;
        if (asset.engine == EEngine.BLIMP)
        {
            int childCount = buoyancy.childCount;
            for (int i = 0; i < childCount; i++)
            {
                buoyancy.GetChild(i).GetComponent<Buoyancy>().enabled = isBlimpFloating;
            }
            this.onBlimpUpdated?.Invoke();
        }
    }

    public void tellHeadlights(bool on)
    {
        _headlightsOn = on;
        if (!Dedicator.IsDedicatedServer)
        {
            if (headlights != null)
            {
                headlights.gameObject.SetActive(headlightsOn);
            }
            if (headlightsMaterial != null)
            {
                headlightsMaterial.SetColor("_EmissionColor", headlightsOn ? (headlightsMaterial.color * 2f) : Color.black);
            }
        }
        this.onHeadlightsUpdated?.Invoke();
    }

    public void tellTaillights(bool on)
    {
        _taillightsOn = on;
        if (!Dedicator.IsDedicatedServer)
        {
            if (taillights != null)
            {
                taillights.gameObject.SetActive(taillightsOn);
            }
            if (taillightsMaterial != null)
            {
                taillightsMaterial.SetColor("_EmissionColor", taillightsOn ? (taillightsMaterial.color * 2f) : Color.black);
            }
            else if (taillightMaterials != null)
            {
                for (int i = 0; i < taillightMaterials.Length; i++)
                {
                    if (taillightMaterials[i] != null)
                    {
                        taillightMaterials[i].SetColor("_EmissionColor", taillightsOn ? (taillightMaterials[i].color * 2f) : Color.black);
                    }
                }
            }
        }
        this.onTaillightsUpdated?.Invoke();
    }

    /// <summary>
    /// Turn taillights on/off depending on state.
    /// </summary>
    private void synchronizeTaillights()
    {
        bool flag = isDriven && canTurnOnLights;
        if (taillightsOn != flag)
        {
            tellTaillights(flag);
        }
    }

    public void tellHorn()
    {
        horned = Time.realtimeSinceStartup;
        if (!Dedicator.IsDedicatedServer && clipAudioSource != null && asset.horn != null)
        {
            clipAudioSource.pitch = 1f;
            clipAudioSource.PlayOneShot(asset.horn);
        }
        if (Provider.isServer)
        {
            AlertTool.alert(base.transform.position, 32f);
        }
        eventHook?.OnHornUsed.TryInvoke(this);
    }

    public void tellFuel(ushort newFuel)
    {
        fuel = newFuel;
        InteractableVehicle.OnFuelChanged_Global.TryInvoke("OnFuelChanged_Global", this);
    }

    public void tellBatteryCharge(ushort newBatteryCharge)
    {
        batteryCharge = newBatteryCharge;
        if (!HasBatteryWithCharge)
        {
            isEngineOn = false;
        }
        this.batteryChanged?.Invoke();
        InteractableVehicle.OnBatteryLevelChanged_Global.TryInvoke("OnBatteryLevelChanged_Global", this);
    }

    public void tellExploded()
    {
        clearHooked();
        isExploded = true;
        _lastExploded = Time.realtimeSinceStartup;
        if (sirensOn)
        {
            tellSirens(on: false);
        }
        if (isBlimpFloating)
        {
            tellBlimp(on: false);
        }
        if (headlightsOn)
        {
            tellHeadlights(on: false);
        }
        if (_wheels != null)
        {
            Wheel[] wheels = _wheels;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].isPhysical = false;
            }
        }
        if (buoyancy != null)
        {
            buoyancy.gameObject.SetActive(value: false);
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (asset.ShouldExplosionBurnMaterials)
            {
                ApplyExplosionBurnMaterials();
            }
            updateFires();
            if (_wheels != null)
            {
                Wheel[] wheels = _wheels;
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].Explode();
                }
            }
            if (propellerModels != null)
            {
                PropellerModel[] array = propellerModels;
                foreach (PropellerModel propellerModel in array)
                {
                    if (propellerModel.transform != null)
                    {
                        UnityEngine.Object.Destroy(propellerModel.transform.gameObject);
                        propellerModel.transform = null;
                    }
                }
            }
            if (exhaustParticleSystems != null && !isExhaustRateOverTimeZero)
            {
                SetExhaustParticleSystemsRateOverTimeToZero();
            }
        }
        if (eventHook != null)
        {
            eventHook.OnExploded.TryInvoke(this);
        }
    }

    public void updateFires()
    {
        if (!Dedicator.IsDedicatedServer)
        {
            if (fire != null)
            {
                fire.gameObject.SetActive((isExploded || isDead) && !isUnderwater);
            }
            if (smoke_0 != null)
            {
                smoke_0.gameObject.SetActive((isExploded || health < 100) && !isUnderwater);
            }
            if (smoke_1 != null)
            {
                smoke_1.gameObject.SetActive((isExploded || health < 200) && !isUnderwater);
            }
        }
    }

    public void tellHealth(ushort newHealth)
    {
        health = newHealth;
        if (isDead)
        {
            _lastDead = Time.realtimeSinceStartup;
        }
        updateFires();
        InteractableVehicle.OnHealthChanged_Global.TryInvoke("OnHealthChanged_Global", this);
    }

    public void tellRecov(Vector3 newPosition, int newRecov)
    {
        lastTick = Time.realtimeSinceStartup;
        rootRigidbody.MovePosition(newPosition);
        isFrozen = true;
        rootRigidbody.useGravity = false;
        rootRigidbody.isKinematic = true;
        if (passengers[0] != null && passengers[0].player != null && passengers[0].player.player != null && passengers[0].player.player.input != null)
        {
            passengers[0].player.player.input.recov = newRecov;
        }
    }

    public void tellState(Vector3 newPosition, Quaternion newRotation, float newSpeed, float newForwardVelocity, float newReplicatedSteeringInput, float newReplicatedVelocityInput)
    {
        if (!isDriver)
        {
            lastTick = Time.realtimeSinceStartup;
            lastUpdatedPos = newPosition;
            interpTargetPosition = newPosition;
            interpTargetRotation = newRotation;
            if (asset.engine == EEngine.TRAIN)
            {
                roadPosition = UnpackRoadPosition(newPosition);
            }
            ReplicatedSpeed = newSpeed;
            ReplicatedForwardVelocity = newForwardVelocity;
            ReplicatedSteeringInput = newReplicatedSteeringInput;
            ReplicatedVelocityInput = newReplicatedVelocityInput;
        }
    }

    public bool checkDriver(CSteamID steamID)
    {
        if (isDriven)
        {
            return passengers[0].player.playerID.steamID == steamID;
        }
        return false;
    }

    public SteamPlayer GetDriverClient()
    {
        if (passengers != null && passengers.Length != 0)
        {
            return passengers[0].player;
        }
        return null;
    }

    public Player GetDriverPlayer()
    {
        return GetDriverClient()?.player;
    }

    public void grantTrunkAccess(Player player)
    {
        if (Provider.isServer && trunkItems != null && trunkItems.height > 0)
        {
            player.inventory.openTrunk(trunkItems);
        }
    }

    public void revokeTrunkAccess(Player player)
    {
        if (Provider.isServer)
        {
            player.inventory.closeTrunk();
        }
    }

    public void dropTrunkItems()
    {
        if (Provider.isServer && trunkItems != null)
        {
            for (byte b = 0; b < trunkItems.getItemCount(); b++)
            {
                ItemManager.dropItem(trunkItems.getItem(b).item, base.transform.position, playEffect: false, isDropped: true, wideSpread: true);
            }
            trunkItems.clear();
            trunkItems = null;
            if (passengers[0].player != null && passengers[0].player.player != null)
            {
                revokeTrunkAccess(passengers[0].player.player);
            }
        }
    }

    private void DropScrapItems()
    {
        if (hasDroppedScrapItemsAlready || asset.dropsTableId <= 0)
        {
            return;
        }
        hasDroppedScrapItemsAlready = true;
        int value = UnityEngine.Random.Range(asset.dropsMin, asset.dropsMax);
        value = Mathf.Clamp(value, 0, 100);
        for (int i = 0; i < value; i++)
        {
            float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
            ushort num = SpawnTableTool.ResolveLegacyId(asset.dropsTableId, EAssetType.ITEM, OnGetDropsSpawnTableErrorContext);
            if (num != 0)
            {
                ItemManager.dropItem(new Item(num, EItemOrigin.NATURE), base.transform.position + new Vector3(Mathf.Sin(f) * 3f, 1f, Mathf.Cos(f) * 3f), playEffect: false, Dedicator.IsDedicatedServer, wideSpread: true);
            }
        }
    }

    private string OnGetDropsSpawnTableErrorContext()
    {
        return asset?.FriendlyName + " explosion drops";
    }

    public void addPlayer(byte seat, CSteamID steamID)
    {
        SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(steamID);
        if (steamPlayer != null)
        {
            passengers[seat].player = steamPlayer;
            if (steamPlayer.player != null)
            {
                steamPlayer.player.movement.setVehicle(this, seat, passengers[seat].seat, Vector3.zero, 0, forceUpdate: false);
                if (passengers[seat].turret != null)
                {
                    steamPlayer.player.equipment.turretEquipClient();
                    if (Provider.isServer)
                    {
                        steamPlayer.player.equipment.turretEquipServer(passengers[seat].turret.itemID, passengers[seat].state);
                    }
                }
            }
            if (passengers[seat].collider != null)
            {
                passengers[seat].collider.enabled = true;
            }
            updatePhysics();
            if (seat == 0)
            {
                grantTrunkAccess(steamPlayer.player);
            }
        }
        if (seat == 0)
        {
            isEngineOn = (!usesBattery || HasBatteryWithCharge) && !isUnderwater;
        }
        updateEngine();
        if (seat == 0 && isEnginePowered && isEngineOn && !Dedicator.IsDedicatedServer && !isUnderwater)
        {
            PlayIgnitionSound();
        }
        this.onPassengersUpdated?.Invoke();
        bool flag = !Dedicator.IsDedicatedServer && steamPlayer != null && Player.player != null && Player.player == steamPlayer.player;
        if (eventHook != null)
        {
            if (seat == 0)
            {
                eventHook.OnDriverAdded.TryInvoke(this);
                if (flag)
                {
                    eventHook.OnLocalDriverAdded.TryInvoke(this);
                }
            }
            if (flag)
            {
                eventHook.OnLocalPassengerAdded.TryInvoke(this);
            }
        }
        if (passengers[seat].turretEventHook != null)
        {
            passengers[seat].turretEventHook.OnPassengerAdded.TryInvoke(this);
            if (flag)
            {
                passengers[seat].turretEventHook.OnLocalPassengerAdded.TryInvoke(this);
            }
        }
        InteractableVehicle.OnPassengerAdded_Global.TryInvoke("OnPassengerAdded_Global", this, seat);
    }

    public void removePlayer(byte seatIndex, Vector3 point, byte angle, bool forceUpdate)
    {
        SteamPlayer steamPlayer = null;
        if (passengers != null && seatIndex < passengers.Length)
        {
            Passenger passenger = passengers[seatIndex];
            steamPlayer = passenger.player;
            if (steamPlayer != null && steamPlayer.player != null)
            {
                if (passenger.turret != null)
                {
                    steamPlayer.player.equipment.turretDequipClient();
                    if (Provider.isServer)
                    {
                        steamPlayer.player.equipment.turretDequipServer();
                    }
                }
                steamPlayer.player.movement.setVehicle(null, 0, null, point, angle, forceUpdate);
            }
            if (passengers[seatIndex].collider != null)
            {
                passengers[seatIndex].collider.enabled = false;
            }
            passenger.player = null;
            updatePhysics();
            if (Provider.isServer)
            {
                VehicleManager.sendVehicleFuel(this, fuel);
                VehicleManager.sendVehicleBatteryCharge(this, batteryCharge);
            }
            if (seatIndex == 0 && steamPlayer != null && steamPlayer.player != null)
            {
                revokeTrunkAccess(steamPlayer.player);
            }
        }
        if (seatIndex == 0)
        {
            isEngineOn = false;
        }
        updateEngine();
        if (seatIndex == 0)
        {
            inputTargetVelocity = 0f;
            inputEngineVelocity = 0f;
            ReplicatedSteeringInput = 0f;
            if (!Dedicator.IsDedicatedServer && windZone != null)
            {
                windZone.windMain = 0f;
            }
            if (_wheels != null)
            {
                Wheel[] wheels = _wheels;
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].Reset();
                }
            }
        }
        this.onPassengersUpdated?.Invoke();
        bool flag = !Dedicator.IsDedicatedServer && steamPlayer != null && Player.player != null && Player.player == steamPlayer.player;
        if (passengers[seatIndex].turretEventHook != null)
        {
            if (flag)
            {
                passengers[seatIndex].turretEventHook.OnLocalPassengerRemoved.TryInvoke(this);
            }
            passengers[seatIndex].turretEventHook.OnPassengerRemoved.TryInvoke(this);
        }
        if (eventHook != null)
        {
            if (flag)
            {
                eventHook.OnLocalPassengerRemoved.TryInvoke(this);
            }
            if (seatIndex == 0)
            {
                if (flag)
                {
                    eventHook.OnLocalDriverRemoved.TryInvoke(this);
                }
                eventHook.OnDriverRemoved.TryInvoke(this);
            }
        }
        InteractableVehicle.OnPassengerRemoved_Global.TryInvoke("OnPassengerRemoved_Global", this, seatIndex, steamPlayer?.player);
    }

    public void swapPlayer(byte fromSeatIndex, byte toSeatIndex)
    {
        SteamPlayer steamPlayer = null;
        if (passengers != null && fromSeatIndex < passengers.Length && toSeatIndex < passengers.Length)
        {
            Passenger passenger = passengers[fromSeatIndex];
            Passenger passenger2 = passengers[toSeatIndex];
            steamPlayer = passenger.player;
            if (steamPlayer != null && steamPlayer.player != null)
            {
                if (passenger.turret != null)
                {
                    steamPlayer.player.equipment.turretDequipClient();
                    if (Provider.isServer)
                    {
                        steamPlayer.player.equipment.turretDequipServer();
                    }
                }
                steamPlayer.player.movement.setVehicle(this, toSeatIndex, passengers[toSeatIndex].seat, Vector3.zero, 0, forceUpdate: false);
                if (passenger2.turret != null)
                {
                    steamPlayer.player.equipment.turretEquipClient();
                    if (Provider.isServer)
                    {
                        steamPlayer.player.equipment.turretEquipServer(passengers[toSeatIndex].turret.itemID, passengers[toSeatIndex].state);
                    }
                }
            }
            if (passenger.collider != null)
            {
                passenger.collider.enabled = false;
            }
            if (passenger2.collider != null)
            {
                passenger2.collider.enabled = true;
            }
            passenger.player = null;
            passenger2.player = steamPlayer;
            updatePhysics();
            if (Provider.isServer)
            {
                VehicleManager.sendVehicleFuel(this, fuel);
                VehicleManager.sendVehicleBatteryCharge(this, batteryCharge);
            }
            if (fromSeatIndex == 0 && steamPlayer != null && steamPlayer.player != null)
            {
                revokeTrunkAccess(steamPlayer.player);
            }
            if (toSeatIndex == 0 && steamPlayer != null && steamPlayer.player != null)
            {
                grantTrunkAccess(steamPlayer.player);
            }
        }
        if (toSeatIndex == 0)
        {
            isEngineOn = (!usesBattery || HasBatteryWithCharge) && !isUnderwater;
        }
        if (fromSeatIndex == 0)
        {
            isEngineOn = false;
        }
        updateEngine();
        if (toSeatIndex == 0 && isEnginePowered && isEngineOn && !Dedicator.IsDedicatedServer && !isUnderwater)
        {
            PlayIgnitionSound();
        }
        if (fromSeatIndex == 0)
        {
            inputTargetVelocity = 0f;
            inputEngineVelocity = 0f;
            ReplicatedSteeringInput = 0f;
            if (!Dedicator.IsDedicatedServer && windZone != null)
            {
                windZone.windMain = 0f;
            }
            if (_wheels != null)
            {
                Wheel[] wheels = _wheels;
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].Reset();
                }
            }
        }
        this.onPassengersUpdated?.Invoke();
        bool flag = !Dedicator.IsDedicatedServer && steamPlayer != null && Player.player != null && Player.player == steamPlayer.player;
        if (passengers[fromSeatIndex].turretEventHook != null)
        {
            if (flag)
            {
                passengers[fromSeatIndex].turretEventHook.OnLocalPassengerRemoved.TryInvoke(this);
            }
            passengers[fromSeatIndex].turretEventHook.OnPassengerRemoved.TryInvoke(this);
        }
        if (passengers[toSeatIndex].turretEventHook != null)
        {
            passengers[toSeatIndex].turretEventHook.OnPassengerAdded.TryInvoke(this);
            if (flag)
            {
                passengers[toSeatIndex].turretEventHook.OnLocalPassengerAdded.TryInvoke(this);
            }
        }
        if (eventHook != null)
        {
            if (fromSeatIndex == 0)
            {
                if (flag)
                {
                    eventHook.OnLocalDriverRemoved.TryInvoke(this);
                }
                eventHook.OnDriverRemoved.TryInvoke(this);
            }
            if (toSeatIndex == 0)
            {
                eventHook.OnDriverAdded.TryInvoke(this);
                if (flag)
                {
                    eventHook.OnLocalDriverAdded.TryInvoke(this);
                }
            }
        }
        InteractableVehicle.OnPassengerChangedSeats_Global.TryInvoke("OnPassengerChangedSeats_Global", this, fromSeatIndex, toSeatIndex);
    }

    /// <summary>
    /// VehicleManager expects this to only find the seat, not add the player,
    /// because it does a LoS check.
    /// </summary>
    public bool tryAddPlayer(out byte seat, Player player)
    {
        seat = byte.MaxValue;
        if (player == null)
        {
            return false;
        }
        if (isExploded)
        {
            return false;
        }
        if (!isExitable)
        {
            return false;
        }
        for (byte b = 0; b < passengers.Length; b++)
        {
            if (passengers[b] != null && passengers[b].player == player.channel.owner)
            {
                return false;
            }
        }
        bool flag = player.animator.gesture == EPlayerGesture.ARREST_START;
        for (byte b2 = (byte)(flag ? 1u : 0u); b2 < passengers.Length; b2++)
        {
            if (passengers[b2] != null && passengers[b2].player == null && (!flag || passengers[b2].turret == null))
            {
                seat = b2;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Call on the server to empty the vehicle of passengers.
    /// </summary>
    public void forceRemoveAllPlayers()
    {
        ThreadUtil.assertIsGameThread();
        for (int i = 0; i < passengers.Length; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger == null)
            {
                continue;
            }
            SteamPlayer player = passenger.player;
            if (player != null)
            {
                if (player.player == null)
                {
                    UnturnedLog.error($"Encountered client ({player}) without player game object when force-removing passengers from vehicle");
                }
                else
                {
                    VehicleManager.forceRemovePlayer(this, player.playerID.steamID);
                }
            }
        }
    }

    /// <summary>
    /// Kicks them out even if there isn't a good spot. Used when killing the occupant.
    /// </summary>
    /// <returns>True if player is seated, false otherwise.</returns>
    public bool forceRemovePlayer(out byte seat, CSteamID player, out Vector3 point, out byte angle)
    {
        seat = byte.MaxValue;
        point = Vector3.zero;
        angle = 0;
        if (findPlayerSeat(player, out seat))
        {
            forceGetExit(passengers[seat]?.player?.player, seat, out point, out angle);
            return true;
        }
        return false;
    }

    public bool findPlayerSeat(CSteamID player, out byte seat)
    {
        seat = byte.MaxValue;
        for (byte b = 0; b < passengers.Length; b++)
        {
            if (passengers[b] != null && passengers[b].player != null && passengers[b].player.playerID.steamID == player)
            {
                seat = b;
                return true;
            }
        }
        return false;
    }

    public bool findPlayerSeat(Player player, out byte seat)
    {
        return findPlayerSeat(player.channel.owner.playerID.steamID, out seat);
    }

    public bool trySwapPlayer(Player player, byte toSeat, out byte fromSeat)
    {
        fromSeat = byte.MaxValue;
        if (toSeat >= passengers.Length)
        {
            return false;
        }
        if (player.animator.gesture == EPlayerGesture.ARREST_START)
        {
            if (toSeat < 1)
            {
                return false;
            }
            if (passengers[toSeat].turret != null)
            {
                return false;
            }
        }
        for (byte b = 0; b < passengers.Length; b++)
        {
            if (passengers[b] != null && passengers[b].player != null && passengers[b].player.player == player)
            {
                if (toSeat != b)
                {
                    fromSeat = b;
                    if (passengers[toSeat].player == null)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Could a player capsule fit in a given exit position?
    /// </summary>
    protected bool isExitPositionEmpty(Vector3 position)
    {
        return PlayerStance.hasTeleportClearanceAtPosition(position);
    }

    /// <returns>True if anything was hit.</returns>
    protected bool raycastIgnoringVehicleAndChildren(Vector3 origin, Vector3 direction, float maxDistance, out float hitDistance)
    {
        hitDistance = maxDistance;
        bool result = false;
        RaycastHit[] array = Physics.RaycastAll(new Ray(origin, direction), maxDistance, RayMasks.BLOCK_EXIT);
        if (array != null && array.Length != 0)
        {
            RaycastHit[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                RaycastHit raycastHit = array2[i];
                if (raycastHit.transform != null && !raycastHit.transform.IsChildOf(base.transform))
                {
                    hitDistance = Mathf.Min(hitDistance, raycastHit.distance);
                    result = true;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Raycast along a given direction, penetrating through barricades attached to THIS vehicle.
    /// Returns point at the end of the ray if unblocked, or a safe (radius) distance away from hit.
    /// </summary>
    protected Vector3 getExitDistanceInDirection(Vector3 origin, Vector3 direction, float maxDistance, float extraPadding = 0.1f)
    {
        raycastIgnoringVehicleAndChildren(origin, direction, maxDistance, out var hitDistance);
        float num = PlayerStance.RADIUS + extraPadding;
        return origin + direction * (hitDistance - num);
    }

    protected void findGroundForExitPosition(ref Vector3 exitPosition)
    {
        Physics.Raycast(new Ray(exitPosition, Vector3.down), out var hitInfo, 3f, RayMasks.BLOCK_EXIT_FIND_GROUND);
        if (hitInfo.transform != null)
        {
            exitPosition = hitInfo.point + new Vector3(0f, 0.25f, 0f);
        }
    }

    protected bool getSafeExitInDirection(Vector3 origin, Vector3 direction, float maxDistance, out Vector3 exitPosition)
    {
        exitPosition = getExitDistanceInDirection(origin, direction, maxDistance);
        findGroundForExitPosition(ref exitPosition);
        return isExitPositionEmpty(exitPosition);
    }

    protected bool getExitSidePoint(Vector3 direction, out Vector3 exitPosition)
    {
        float num = PlayerStance.RADIUS + 0.1f;
        float maxDistance = asset.exit + Mathf.Abs(ReplicatedSpeed) * 0.1f + num;
        Vector3 position = center.position;
        return getSafeExitInDirection(position, direction, maxDistance, out exitPosition);
    }

    protected bool getExitUpwardPoint(out Vector3 exitPosition)
    {
        Vector3 position = center.position;
        Vector3 up = center.up;
        exitPosition = getExitDistanceInDirection(position, up, 6f, PlayerMovement.HEIGHT_STAND);
        findGroundForExitPosition(ref exitPosition);
        if (isExitPositionEmpty(exitPosition))
        {
            return true;
        }
        exitPosition = getExitDistanceInDirection(position, Vector3.up, 6f, PlayerMovement.HEIGHT_STAND);
        findGroundForExitPosition(ref exitPosition);
        return isExitPositionEmpty(exitPosition);
    }

    protected bool getExitDownwardPoint(out Vector3 exitPosition)
    {
        Vector3 position = center.position;
        Vector3 direction = -center.up;
        if (getSafeExitInDirection(position, direction, 6f, out exitPosition))
        {
            return true;
        }
        return getSafeExitInDirection(position, Vector3.down, 6f, out exitPosition);
    }

    protected bool getExitForwardPoint(Vector3 direction, out Vector3 exitPosition)
    {
        float maxDistance = 3f + asset.exit * 2f;
        Vector3 position = center.position;
        return getSafeExitInDirection(position, direction, maxDistance, out exitPosition);
    }

    /// <summary>
    /// Fallback if there are absolutely no good exit points.
    /// Sets point and angle with a normal player spawnpoint.
    ///
    /// Once vehicle is completely surrounded there is no nice way to pick an exit point. Finding
    /// a point upwards is abused to teleport upward into bases, finding an empty capsule nearby is
    /// abused to teleport through walls, so if we're sure there isn't a nice exit point we can
    /// fallback to teleporting them to a safe spawnpoint.
    /// </summary>
    protected void getExitSpawnPoint(Player player, ref Vector3 point, ref byte angle)
    {
        PlayerSpawnpoint spawn = LevelPlayers.getSpawn(Level.info != null && Level.info.type == ELevelType.ARENA && LevelManager.isPlayerInArena(player));
        if (spawn != null)
        {
            point = spawn.point;
            angle = MeasurementTool.angleToByte((int)angle);
        }
        else
        {
            point = new Vector3(0f, 256f, 0f);
            angle = 0;
        }
    }

    /// <returns>True if we can safely exit.</returns>
    internal bool tryGetExit(byte seat, out Vector3 point, out byte angle)
    {
        point = center.position;
        angle = MeasurementTool.angleToByte(center.rotation.eulerAngles.y);
        Vector3 vector = ((seat % 2 == 0) ? (-center.right) : center.right);
        if (getExitSidePoint(vector, out point))
        {
            return true;
        }
        vector = -vector;
        if (getExitSidePoint(vector, out point))
        {
            return true;
        }
        if (getExitUpwardPoint(out point))
        {
            return true;
        }
        if (getExitDownwardPoint(out point))
        {
            return true;
        }
        if (getExitForwardPoint(-center.forward, out point))
        {
            return true;
        }
        if (getExitForwardPoint(center.forward, out point))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Initially use tryGetExit to find a safe exit, but if one isn't available then fallback to getExitSpawnPoint.
    /// </summary>
    protected void forceGetExit(Player player, byte seat, out Vector3 point, out byte angle)
    {
        if (!tryGetExit(seat, out point, out angle))
        {
            getExitSpawnPoint(player, ref point, ref angle);
        }
    }

    /// <summary>
    /// Dedicated server simulate driving input.
    /// </summary>
    public void simulate(uint simulation, int recov, bool inputStamina, Vector3 point, Quaternion angle, float newSpeed, float newForwardVelocity, float newSteeringInput, float newVelocityInput, float delta)
    {
        if (asset.useStaminaBoost)
        {
            bool flag = passengers[0].player != null && passengers[0].player.player != null && passengers[0].player.player.life.stamina > 0;
            if (inputStamina && flag)
            {
                isBoosting = true;
            }
            else
            {
                isBoosting = false;
            }
        }
        else
        {
            isBoosting = false;
        }
        if (isRecovering)
        {
            if (recov < passengers[0].player.player.input.recov)
            {
                if (Time.realtimeSinceStartup - lastRecover > 5f)
                {
                    lastRecover = Time.realtimeSinceStartup;
                    VehicleManager.sendVehicleRecov(this, real, passengers[0].player.player.input.recov);
                }
                return;
            }
            isRecovering = false;
            isFrozen = false;
        }
        if (Dedicator.serverVisibility != ESteamServerVisibility.LAN && !PlayerMovement.forceTrustClient)
        {
            if (asset.engine == EEngine.CAR)
            {
                if (MathfEx.HorizontalDistanceSquared(point, real) > ((usesFuel && fuel == 0) ? 0.5f : asset.sqrDelta))
                {
                    isRecovering = true;
                    lastRecover = Time.realtimeSinceStartup;
                    passengers[0].player.player.input.recov++;
                    VehicleManager.sendVehicleRecov(this, real, passengers[0].player.player.input.recov);
                    return;
                }
            }
            else if (asset.engine == EEngine.BOAT)
            {
                if (MathfEx.HorizontalDistanceSquared(point, real) > (WaterUtility.isPointUnderwater(point + new Vector3(0f, -4f, 0f)) ? asset.sqrDelta : 0.5f))
                {
                    isRecovering = true;
                    lastRecover = Time.realtimeSinceStartup;
                    passengers[0].player.player.input.recov++;
                    VehicleManager.sendVehicleRecov(this, real, passengers[0].player.player.input.recov);
                    return;
                }
            }
            else if (asset.engine != EEngine.TRAIN && MathfEx.HorizontalDistanceSquared(point, real) > asset.sqrDelta)
            {
                isRecovering = true;
                lastRecover = Time.realtimeSinceStartup;
                passengers[0].player.player.input.recov++;
                VehicleManager.sendVehicleRecov(this, real, passengers[0].player.player.input.recov);
                return;
            }
            if (asset.engine != EEngine.TRAIN)
            {
                float num = ((point.y > real.y) ? asset.validSpeedUp : asset.validSpeedDown);
                if (Mathf.Abs(point.y - real.y) / delta > num)
                {
                    isRecovering = true;
                    lastRecover = Time.realtimeSinceStartup;
                    passengers[0].player.player.input.recov++;
                    VehicleManager.sendVehicleRecov(this, real, passengers[0].player.player.input.recov);
                    return;
                }
            }
        }
        if (asset.engine != EEngine.TRAIN)
        {
            UndergroundAllowlist.AdjustPosition(ref point, 10f, 2f);
        }
        simulateBurnFuel();
        bool num2 = !MathfEx.IsNearlyEqual(ReplicatedSteeringInput, newSteeringInput, 0.5f);
        ReplicatedSpeed = newSpeed;
        ReplicatedForwardVelocity = newForwardVelocity;
        ReplicatedSteeringInput = newSteeringInput;
        ReplicatedVelocityInput = newVelocityInput;
        real = point;
        if (asset.engine == EEngine.TRAIN)
        {
            roadPosition = clampRoadPosition(UnpackRoadPosition(point));
            teleportTrain();
        }
        else
        {
            rootRigidbody.MovePosition(point);
            rootRigidbody.MoveRotation(angle);
        }
        if (num2 || Mathf.Abs(lastUpdatedPos.x - real.x) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.y - real.y) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.z - real.z) > Provider.UPDATE_DISTANCE)
        {
            lastUpdatedPos = real;
            MarkForReplicationUpdate();
        }
    }

    public void clearHooked()
    {
        foreach (HookInfo item in hooked)
        {
            if (!(item.vehicle == null))
            {
                item.vehicle.isHooked = false;
                ignoreCollisionWithVehicle(item.vehicle, shouldIgnore: false);
            }
        }
        hooked.Clear();
    }

    public void useHook()
    {
        if (hooked.Count > 0)
        {
            clearHooked();
            return;
        }
        int num = Physics.OverlapSphereNonAlloc(hook.position, 3f, tempCollidersArray, 67108864);
        for (int i = 0; i < num; i++)
        {
            InteractableVehicle vehicle = DamageTool.getVehicle(tempCollidersArray[i].transform);
            if (!(vehicle == null) && !(vehicle == this) && vehicle.isEmpty && !vehicle.isHooked && !vehicle.isExploded && vehicle.asset.engine != EEngine.TRAIN)
            {
                VehicleBarricadeRegion vehicleBarricadeRegion = BarricadeManager.findRegionFromVehicle(vehicle);
                if (vehicleBarricadeRegion == null || vehicleBarricadeRegion.drops.Count <= 0)
                {
                    HookInfo hookInfo = new HookInfo();
                    hookInfo.target = vehicle.transform;
                    hookInfo.vehicle = vehicle;
                    hookInfo.deltaPosition = hook.InverseTransformPoint(vehicle.transform.position);
                    hookInfo.deltaRotation = Quaternion.FromToRotation(hook.forward, vehicle.transform.forward);
                    hooked.Add(hookInfo);
                    vehicle.isHooked = true;
                    ignoreCollisionWithVehicle(vehicle, shouldIgnore: true);
                }
            }
        }
    }

    /// <summary>
    /// Called when engine RPM exceeds threshold and there are more gears available.
    /// Purpose is to skip gear numbers that don't bring engine RPM within threshold (if possible).
    /// </summary>
    private int GetShiftUpGearNumber(float averagePoweredWheelRpm)
    {
        for (int i = GearNumber; i < asset.forwardGearRatios.Length; i++)
        {
            float num = averagePoweredWheelRpm * asset.forwardGearRatios[i];
            if (num > asset.GearShiftDownThresholdRpm && num < asset.GearShiftUpThresholdRpm)
            {
                return i + 1;
            }
        }
        return GearNumber + 1;
    }

    /// <summary>
    /// Called when engine RPM is below threshold and there are more lower gears available.
    /// Purpose is to skip gear numbers that don't bring engine RPM within threshold (if possible).
    /// </summary>
    private int GetShiftDownGearNumber(float averagePoweredWheelRpm)
    {
        for (int num = GearNumber - 2; num >= 0; num--)
        {
            float num2 = averagePoweredWheelRpm * asset.forwardGearRatios[num];
            if (num2 > asset.GearShiftDownThresholdRpm && num2 < asset.GearShiftUpThresholdRpm)
            {
                return num + 1;
            }
        }
        return GearNumber - 1;
    }

    private void ChangeGears(int newGearNumber)
    {
        if (GearNumber != newGearNumber)
        {
            timeSinceLastGearChange = 0f;
            GearNumber = newGearNumber;
        }
    }

    /// <summary>
    /// Client simulate driving input.
    /// </summary>
    public void simulate(uint simulation, int recov, int input_x, int input_y, float look_x, float look_y, bool inputBrake, bool inputStamina, float delta)
    {
        if (Provider.isServer && asset.engine != EEngine.TRAIN)
        {
            Vector3 worldspacePosition = base.transform.position;
            if (UndergroundAllowlist.AdjustPosition(ref worldspacePosition, 10f, 2f))
            {
                rootRigidbody.MovePosition(worldspacePosition);
            }
        }
        latestGasInput = input_y;
        float num = input_y;
        float num2 = 1f;
        if (asset.useStaminaBoost)
        {
            bool flag = passengers[0].player != null && passengers[0].player.player != null && passengers[0].player.player.life.stamina > 0;
            if (inputStamina && flag)
            {
                isBoosting = true;
            }
            else
            {
                isBoosting = false;
                num *= asset.staminaBoost;
                num2 *= asset.staminaBoost;
            }
        }
        else
        {
            isBoosting = false;
        }
        if (isFrozen)
        {
            isFrozen = false;
            rootRigidbody.useGravity = usesGravity;
            rootRigidbody.isKinematic = isKinematic;
            return;
        }
        if ((usesFuel && fuel == 0) || isUnderwater || isDead || !isEnginePowered)
        {
            num = 0f;
            num2 = 1f;
        }
        bool flag2 = false;
        float num3 = asset.steeringLeaningForceMultiplier;
        if (num3 > 0f)
        {
            if (asset.steeringLeaningForceShouldScaleWithSpeed)
            {
                float replicatedForwardSpeedPercentageOfTargetSpeed = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
                num3 *= Mathf.Pow(replicatedForwardSpeedPercentageOfTargetSpeed, asset.steeringLeaningForceSpeedExponent);
            }
            rootRigidbody.AddRelativeTorque(0f, 0f, (float)input_x * (0f - num3) * (float)PlayerInput.SAMPLES);
        }
        if (_wheels != null)
        {
            Wheel[] wheels = _wheels;
            foreach (Wheel wheel in wheels)
            {
                wheel.ClientSimulate(input_x, num, inputBrake, delta);
                flag2 |= wheel.isGrounded;
            }
            if (flag2 && asset.wheelBalancingForceMultiplier > 0f)
            {
                ApplyWheelBalancingForce(Time.fixedDeltaTime * (float)PlayerInput.SAMPLES);
            }
        }
        if (asset.rollAngularVelocityDamping > 0f)
        {
            ApplyAngularVelocityDamping(Time.fixedDeltaTime * (float)PlayerInput.SAMPLES);
        }
        switch (asset.engine)
        {
        case EEngine.CAR:
        {
            float replicatedForwardSpeedPercentageOfTargetSpeed2 = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
            if (flag2)
            {
                rootRigidbody.AddForce(-base.transform.up * replicatedForwardSpeedPercentageOfTargetSpeed2 * 40f);
            }
            if (!(buoyancy != null))
            {
                break;
            }
            float num4 = Mathf.Lerp(asset.steerMax, asset.steerMin, replicatedForwardSpeedPercentageOfTargetSpeed2);
            bool flag3 = WaterUtility.isPointUnderwater(base.transform.position + new Vector3(0f, -1f, 0f));
            boatTraction = Mathf.Lerp(boatTraction, flag3 ? 1 : 0, 4f * Time.deltaTime);
            if (!MathfEx.IsNearlyZero(boatTraction))
            {
                if (num > 0f)
                {
                    inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity, delta / 4f);
                }
                else if (num < 0f)
                {
                    inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetReverseVelocity, delta / 4f);
                }
                else
                {
                    inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
                }
                inputEngineVelocity = inputTargetVelocity * boatTraction;
                Vector3 forward = base.transform.forward;
                forward.y = 0f;
                rootRigidbody.AddForce(forward.normalized * inputEngineVelocity * 2f * boatTraction);
                rootRigidbody.AddRelativeTorque((float)input_y * -2.5f * boatTraction, (float)input_x * num4 / 8f * boatTraction, (float)input_x * -2.5f * boatTraction);
            }
            break;
        }
        case EEngine.PLANE:
        {
            float replicatedForwardSpeedPercentageOfTargetSpeed4 = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
            float num6 = Mathf.Lerp(asset.airSteerMax, asset.airSteerMin, replicatedForwardSpeedPercentageOfTargetSpeed4);
            if (num > 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity * num2, delta);
            }
            else if (num < 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
            }
            else
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 16f);
            }
            inputEngineVelocity = inputTargetVelocity;
            rootRigidbody.AddForce(base.transform.forward * inputEngineVelocity * 2f * asset.engineForceMultiplier);
            rootRigidbody.AddForce(Mathf.Lerp(0f, 1f, base.transform.InverseTransformDirection(rootRigidbody.velocity).z / asset.TargetForwardVelocity) * asset.lift * -Physics.gravity);
            if (_wheels == null || _wheels.Length == 0 || (!_wheels[0].isGrounded && !_wheels[1].isGrounded))
            {
                rootRigidbody.AddRelativeTorque(Mathf.Clamp(look_y, 0f - asset.airTurnResponsiveness, asset.airTurnResponsiveness) * num6, (float)input_x * asset.airTurnResponsiveness * num6 / 4f, Mathf.Clamp(look_x, 0f - asset.airTurnResponsiveness, asset.airTurnResponsiveness) * (0f - num6) / 2f);
            }
            if ((_wheels == null || _wheels.Length == 0) && num < 0f)
            {
                rootRigidbody.AddForce(base.transform.forward * asset.TargetReverseVelocity * 4f * asset.engineForceMultiplier);
            }
            break;
        }
        case EEngine.HELICOPTER:
        {
            float replicatedForwardSpeedPercentageOfTargetSpeed5 = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
            float num7 = Mathf.Lerp(asset.steerMax, asset.steerMin, replicatedForwardSpeedPercentageOfTargetSpeed5);
            if (num > 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity * num2, delta / 4f);
            }
            else if (num < 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
            }
            else
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 16f);
            }
            inputEngineVelocity = inputTargetVelocity;
            rootRigidbody.AddForce(base.transform.up * inputEngineVelocity * 3f);
            rootRigidbody.AddRelativeTorque(Mathf.Clamp(look_y, -2f, 2f) * num7, (float)input_x * num7 / 2f, Mathf.Clamp(look_x, -2f, 2f) * (0f - num7) / 4f);
            break;
        }
        case EEngine.BLIMP:
        {
            float replicatedForwardSpeedPercentageOfTargetSpeed3 = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
            float num5 = Mathf.Lerp(asset.steerMax, asset.steerMin, replicatedForwardSpeedPercentageOfTargetSpeed3);
            if (num > 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity * num2, delta / 4f);
            }
            else if (num < 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetReverseVelocity * num2, delta / 4f);
            }
            else
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
            }
            inputEngineVelocity = inputTargetVelocity;
            rootRigidbody.AddForce(base.transform.forward * inputEngineVelocity * 2f);
            if (!isBlimpFloating)
            {
                rootRigidbody.AddForce(-Physics.gravity * 0.5f);
            }
            rootRigidbody.AddRelativeTorque(Mathf.Clamp(look_y, 0f - asset.airTurnResponsiveness, asset.airTurnResponsiveness) * num5 / 4f, (float)input_x * asset.airTurnResponsiveness * num5 * 2f, Mathf.Clamp(look_x, 0f - asset.airTurnResponsiveness, asset.airTurnResponsiveness) * (0f - num5) / 4f);
            break;
        }
        case EEngine.BOAT:
        {
            float replicatedForwardSpeedPercentageOfTargetSpeed6 = GetReplicatedForwardSpeedPercentageOfTargetSpeed();
            float num8 = Mathf.Lerp(asset.steerMax, asset.steerMin, replicatedForwardSpeedPercentageOfTargetSpeed6);
            boatTraction = Mathf.Lerp(boatTraction, WaterUtility.isPointUnderwater(base.transform.position + new Vector3(0f, -1f, 0f)) ? 1 : 0, 4f * Time.deltaTime);
            if (num > 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity * num2, delta / 4f);
            }
            else if (num < 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetReverseVelocity * num2, delta / 4f);
            }
            else
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
            }
            inputEngineVelocity = inputTargetVelocity * boatTraction;
            Vector3 forward2 = base.transform.forward;
            forward2.y = 0f;
            rootRigidbody.AddForce(forward2.normalized * inputEngineVelocity * 4f * boatTraction);
            if (_wheels == null || _wheels.Length == 0 || (!_wheels[0].isGrounded && !_wheels[1].isGrounded))
            {
                rootRigidbody.AddRelativeTorque(num * -10f * boatTraction, (float)input_x * num8 / 2f * boatTraction, (float)input_x * -5f * boatTraction);
            }
            break;
        }
        case EEngine.TRAIN:
            if (num > 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetForwardVelocity * num2, delta / 8f);
            }
            else if (num < 0f)
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, asset.TargetReverseVelocity * num2, delta / 8f);
            }
            else
            {
                inputTargetVelocity = Mathf.Lerp(inputTargetVelocity, 0f, delta / 8f);
            }
            inputEngineVelocity = inputTargetVelocity;
            break;
        }
        if (asset.engine == EEngine.TRAIN)
        {
            ReplicatedSpeed = Mathf.Abs(inputEngineVelocity);
            ReplicatedForwardVelocity = inputEngineVelocity;
            ReplicatedVelocityInput = inputEngineVelocity;
        }
        else
        {
            Vector3 velocity = rootRigidbody.velocity;
            ReplicatedSpeed = velocity.magnitude;
            Vector3 vector = base.transform.InverseTransformDirection(velocity);
            if (asset.engine == EEngine.HELICOPTER)
            {
                ReplicatedForwardVelocity = vector.y;
            }
            else
            {
                ReplicatedForwardVelocity = vector.z;
            }
            ReplicatedVelocityInput = inputEngineVelocity;
        }
        ReplicatedSteeringInput = input_x;
        simulateBurnFuel();
        lastUpdatedPos = base.transform.position;
        interpTargetPosition = base.transform.position;
        interpTargetRotation = base.transform.rotation;
    }

    private void moveTrain(Vector3 frontPosition, Vector3 frontNormal, Vector3 frontDirection, Vector3 backPosition, Vector3 backNormal, Vector3 backDirection, TrainCar car)
    {
        Vector3 vector = (frontPosition + backPosition) / 2f;
        Vector3 vector2 = Vector3.Lerp(backNormal, frontNormal, 0.5f);
        Vector3 normalized = (frontPosition - backPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(frontDirection, frontNormal);
        Quaternion rotation2 = Quaternion.LookRotation(backDirection, backNormal);
        Quaternion quaternion = Quaternion.LookRotation(normalized, vector2);
        if (car.rootRigidbody != null)
        {
            car.rootRigidbody.MovePosition(vector + vector2 * asset.trainTrackOffset);
            car.rootRigidbody.MoveRotation(quaternion);
        }
        if (car.root != null)
        {
            car.root.position = vector + vector2 * asset.trainTrackOffset;
            car.root.rotation = quaternion;
        }
        if (car.trackFront != null)
        {
            car.trackFront.position = vector + normalized * asset.trainWheelOffset;
            car.trackFront.rotation = rotation;
        }
        if (car.trackBack != null)
        {
            car.trackBack.position = vector - normalized * asset.trainWheelOffset;
            car.trackBack.rotation = rotation2;
        }
    }

    private void teleportTrain()
    {
        TrainCar[] array = trainCars;
        foreach (TrainCar trainCar in array)
        {
            road.getTrackData(clampRoadPosition(roadPosition + trainCar.trackPositionOffset + asset.trainWheelOffset), out var position, out var normal, out var direction);
            road.getTrackData(clampRoadPosition(roadPosition + trainCar.trackPositionOffset - asset.trainWheelOffset), out var position2, out var normal2, out var direction2);
            moveTrain(position, normal, direction, position2, normal2, direction2, trainCar);
        }
    }

    private TrainCar getTrainCar(Transform root)
    {
        Transform trackFront = root.Find("Objects")?.Find("Track_Front");
        Transform trackBack = root.Find("Objects")?.Find("Track_Back");
        return new TrainCar
        {
            root = root,
            trackFront = trackFront,
            trackBack = trackBack,
            rootRigidbody = root.GetComponent<Rigidbody>()
        };
    }

    private float clampRoadPosition(float newRoadPosition)
    {
        if (road.isLoop)
        {
            if (newRoadPosition < 0f)
            {
                return road.trackSampledLength + newRoadPosition;
            }
            if (newRoadPosition > road.trackSampledLength)
            {
                return newRoadPosition - road.trackSampledLength;
            }
            return newRoadPosition;
        }
        return Mathf.Clamp(newRoadPosition, 0.5f + asset.trainWheelOffset, road.trackSampledLength - (float)(trainCars.Length - 1) * asset.trainCarLength - asset.trainWheelOffset - 0.5f);
    }

    /// <summary>
    /// 2020-11-26 experimented with dispatching all vehicle updates from C# in VehicleManager because they make up
    /// a significant portion of the MonoBehaviour Update, but the savings on my PC with 24 vehicles on PEI was
    /// minor. Not worth the potential troubles.
    /// </summary>
    private void Update()
    {
        if (asset == null)
        {
            return;
        }
        float deltaTime = Time.deltaTime;
        if (Provider.isServer && hooked != null)
        {
            for (int i = 0; i < hooked.Count; i++)
            {
                HookInfo hookInfo = hooked[i];
                if (hookInfo != null && !(hookInfo.target == null))
                {
                    hookInfo.target.position = hook.TransformPoint(hookInfo.deltaPosition);
                    hookInfo.target.rotation = hook.rotation * hookInfo.deltaRotation;
                }
            }
        }
        if (Provider.isServer && !needsReplicationUpdate && updates != null && updates.Count > 0)
        {
            updates.Clear();
            MarkForReplicationUpdate();
        }
        if (Dedicator.IsDedicatedServer)
        {
            if (isPhysical && !needsReplicationUpdate && (Mathf.Abs(lastUpdatedPos.x - base.transform.position.x) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.y - base.transform.position.y) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.z - base.transform.position.z) > Provider.UPDATE_DISTANCE))
            {
                lastUpdatedPos = base.transform.position;
                MarkForReplicationUpdate();
            }
        }
        else
        {
            float num = Mathf.Lerp(asset.steerMax, asset.steerMin, GetReplicatedForwardSpeedPercentageOfTargetSpeed());
            float target = ReplicatedSteeringInput * num;
            float maxDelta = asset.SteeringAngleTurnSpeed * deltaTime;
            AnimatedSteeringAngle = Mathf.MoveTowards(AnimatedSteeringAngle, target, maxDelta);
            float t = 1f - Mathf.Pow(2f, -13f * Time.deltaTime);
            AnimatedForwardVelocity = Mathf.Lerp(AnimatedForwardVelocity, ReplicatedForwardVelocity, t);
            AnimatedVelocityInput = Mathf.Lerp(AnimatedVelocityInput, ReplicatedVelocityInput, t);
            AnimatedEngineRpm = Mathf.Lerp(AnimatedEngineRpm, ReplicatedEngineRpm, t);
            if (!isExploded)
            {
                if (isDriven)
                {
                    propellerRotationDegrees += (AnimatedVelocityInput + (isEnginePowered ? 8f : 0f)) * 89f * Time.deltaTime;
                    propellerRotationDegrees %= 360f;
                }
                if (_wheels != null)
                {
                    if (crawlerTrackMaterials != null && crawlerTrackMaterials.Count > 0)
                    {
                        UpdateCrawlerTrackTilingMaterials();
                    }
                    Wheel[] wheels = _wheels;
                    foreach (Wheel wheel in wheels)
                    {
                        if (!(wheel.model == null))
                        {
                            wheel.UpdateModel(Time.deltaTime);
                        }
                    }
                }
                if (frontModelTransform != null)
                {
                    Vector3 axis = frontModelRestLocalRotation * new Vector3(0f, 0f, 1f);
                    frontModelTransform.localRotation = Quaternion.AngleAxis(AnimatedSteeringAngle, axis) * frontModelRestLocalRotation;
                }
                if (propellerModels != null && propellerModels.Length != 0)
                {
                    Quaternion quaternion = Quaternion.AngleAxis(propellerRotationDegrees, Vector3.up);
                    float a = ((!isDriven) ? 1f : ((asset.engine != EEngine.PLANE) ? Mathf.Lerp(1f, 0f, (AnimatedVelocityInput - 8f) / 8f) : Mathf.Lerp(1f, 0f, (AnimatedVelocityInput - 16f) / 8f)));
                    PropellerModel[] array = propellerModels;
                    foreach (PropellerModel propellerModel in array)
                    {
                        if (propellerModel == null || propellerModel.transform == null || propellerModel.bladeMaterial == null || propellerModel.motionBlurMaterial == null)
                        {
                            break;
                        }
                        propellerModel.transform.localRotation = propellerModel.baseLocationRotation * quaternion;
                        Color color = propellerModel.bladeMaterial.color;
                        color.a = a;
                        propellerModel.bladeMaterial.color = color;
                        color.a = (1f - color.a) * 0.25f;
                        propellerModel.motionBlurMaterial.color = color;
                    }
                }
                float num2 = (MathfEx.IsNearlyZero(AnimatedForwardVelocity, 0.04f) ? 0f : Mathf.Max(0f, Mathf.InverseLerp(0f, asset.TargetForwardVelocity, AnimatedForwardVelocity)));
                if (exhaustParticleSystems != null)
                {
                    if (num2 > 0f)
                    {
                        if (!isExhaustGameObjectActive)
                        {
                            exhaustGameObject.SetActive(value: true);
                            isExhaustGameObjectActive = true;
                        }
                        ParticleSystem[] array2 = exhaustParticleSystems;
                        foreach (ParticleSystem particleSystem in array2)
                        {
                            ParticleSystem.EmissionModule emission = particleSystem.emission;
                            emission.rateOverTime = (float)particleSystem.main.maxParticles * num2;
                        }
                        isExhaustRateOverTimeZero = false;
                    }
                    else if (isExhaustGameObjectActive)
                    {
                        if (!isExhaustRateOverTimeZero)
                        {
                            SetExhaustParticleSystemsRateOverTimeToZero();
                        }
                        bool flag = false;
                        ParticleSystem[] array2 = exhaustParticleSystems;
                        for (int j = 0; j < array2.Length; j++)
                        {
                            if (array2[j].particleCount > 0)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            exhaustGameObject.SetActive(value: false);
                            isExhaustGameObjectActive = false;
                        }
                    }
                }
                if (steeringWheelModelTransform != null)
                {
                    Vector3 axis2 = steeringWheelRestLocalRotation * new Vector3(0f, -1f, 0f);
                    steeringWheelModelTransform.localRotation = Quaternion.AngleAxis(AnimatedSteeringAngle, axis2) * steeringWheelRestLocalRotation;
                }
                if (pedalLeft != null && pedalRight != null && passengers[0].player != null && passengers[0].player.player != null)
                {
                    Transform thirdSkeleton = passengers[0].player.player.animator.thirdSkeleton;
                    Transform transform = thirdSkeleton.Find("Left_Hip").Find("Left_Leg").Find("Left_Foot");
                    Transform transform2 = thirdSkeleton.Find("Right_Hip").Find("Right_Leg").Find("Right_Foot");
                    if (passengers[0].player.IsLeftHanded)
                    {
                        pedalLeft.position = transform2.position + transform2.right * 0.325f;
                        pedalRight.position = transform.position + transform.right * 0.325f;
                    }
                    else
                    {
                        pedalLeft.position = transform.position + transform.right * -0.325f;
                        pedalRight.position = transform2.position + transform2.right * -0.325f;
                    }
                }
            }
            if (windZone != null && isDriven && !isUnderwater)
            {
                float num3 = ((asset.engine != 0 && asset.engine != EEngine.BOAT) ? Mathf.Abs(AnimatedVelocityInput) : Mathf.Abs(AnimatedForwardVelocity));
                if (asset.engine == EEngine.HELICOPTER)
                {
                    windZone.windMain = Mathf.Lerp(windZone.windMain, isEnginePowered ? (num3 * 0.1f) : 0f, 0.125f * deltaTime);
                }
                else if (asset.engine == EEngine.BLIMP)
                {
                    windZone.windMain = Mathf.Lerp(windZone.windMain, isEnginePowered ? (num3 * 0.5f) : 0f, 0.125f * deltaTime);
                }
            }
        }
        if (!Provider.isServer && !isPhysical && asset.engine != EEngine.TRAIN)
        {
            base.transform.GetPositionAndRotation(out var position, out var rotation);
            float t2 = 1f - Mathf.Pow(2f, -13f * Time.deltaTime);
            Vector3 position2 = Vector3.Lerp(position, interpTargetPosition, t2);
            Quaternion rot = Quaternion.Slerp(rotation, interpTargetRotation, t2);
            rootRigidbody.MovePosition(position2);
            rootRigidbody.MoveRotation(rot);
        }
        if (Provider.isServer && isPhysical && asset.engine != EEngine.TRAIN && !isDriven)
        {
            Vector3 worldspacePosition = base.transform.position;
            if (UndergroundAllowlist.AdjustPosition(ref worldspacePosition, 10f, 2f))
            {
                rootRigidbody.MovePosition(worldspacePosition);
            }
        }
        if (headlightsOn && !canTurnOnLights)
        {
            tellHeadlights(on: false);
        }
        if (sirensOn && !canTurnOnLights)
        {
            tellSirens(on: false);
        }
        if (isUnderwater)
        {
            if (!isDrowned)
            {
                _lastUnderwater = Time.realtimeSinceStartup;
                _isDrowned = true;
                this.OnIsDrownedChanged?.Invoke();
                tellSirens(on: false);
                tellBlimp(on: false);
                tellHeadlights(on: false);
                updateFires();
                if (!Dedicator.IsDedicatedServer && windZone != null)
                {
                    windZone.windMain = 0f;
                }
            }
        }
        else if (_isDrowned)
        {
            _isDrowned = false;
            this.OnIsDrownedChanged?.Invoke();
            updateFires();
        }
        synchronizeTaillights();
        if (isDriver)
        {
            if (!asset.hasTraction)
            {
                bool flag2 = LevelLighting.isPositionSnowy(base.transform.position);
                if (!flag2 && Level.info != null && Level.info.configData.Use_Snow_Volumes)
                {
                    AmbianceVolume firstOverlappingVolume = VolumeManager<AmbianceVolume, AmbianceVolumeManager>.Get().GetFirstOverlappingVolume(base.transform.position);
                    if (firstOverlappingVolume != null)
                    {
                        flag2 = (firstOverlappingVolume.weatherMask & 2) != 0;
                    }
                }
                flag2 &= LevelLighting.snowyness == ELightingSnow.BLIZZARD;
                _slip = Mathf.Lerp(_slip, flag2 ? 1 : 0, Time.deltaTime * 0.05f);
            }
            else
            {
                _slip = 0f;
            }
            if (_wheels != null)
            {
                float num4 = 0f;
                int num5 = 0;
                if (asset.poweredWheelIndices != null)
                {
                    float num6 = 0f;
                    int[] poweredWheelIndices = asset.poweredWheelIndices;
                    foreach (int index in poweredWheelIndices)
                    {
                        Wheel wheelAtIndex = GetWheelAtIndex(index);
                        if (wheelAtIndex != null && !(wheelAtIndex.wheel == null))
                        {
                            num6 += Mathf.Abs(wheelAtIndex.wheel.rpm);
                            num5++;
                        }
                    }
                    if (num5 > 0)
                    {
                        num4 = num6 / (float)num5;
                    }
                }
                float num7 = num4;
                if (asset.UsesEngineRpmAndGears)
                {
                    timeSinceLastGearChange += deltaTime;
                    if (timeSinceLastGearChange > asset.GearShiftInterval)
                    {
                        if (latestGasInput < -0.01f && ReplicatedForwardVelocity < 0.05f)
                        {
                            ChangeGears(-1);
                        }
                        else if (GearNumber < 1 && ReplicatedForwardVelocity > -0.05f)
                        {
                            ChangeGears(1);
                        }
                        else if (ReplicatedEngineRpm > asset.GearShiftUpThresholdRpm && GearNumber > 0 && GearNumber < asset.forwardGearRatios.Length)
                        {
                            ChangeGears(GetShiftUpGearNumber(num4));
                        }
                        else if (ReplicatedEngineRpm < asset.GearShiftDownThresholdRpm && GearNumber > 1)
                        {
                            ChangeGears(GetShiftDownGearNumber(num4));
                        }
                    }
                    if (GearNumber == -1)
                    {
                        num7 *= asset.reverseGearRatio;
                    }
                    else if (GearNumber >= 1 && GearNumber <= asset.forwardGearRatios.Length)
                    {
                        num7 *= asset.forwardGearRatios[GearNumber - 1];
                    }
                    num7 = Mathf.Max(num7, asset.EngineIdleRpm);
                }
                if (num7 > ReplicatedEngineRpm)
                {
                    ReplicatedEngineRpm = Mathf.MoveTowards(ReplicatedEngineRpm, num7, asset.EngineRpmIncreaseRate * deltaTime);
                }
                else if (num7 < ReplicatedEngineRpm)
                {
                    ReplicatedEngineRpm = Mathf.MoveTowards(ReplicatedEngineRpm, num7, asset.EngineRpmDecreaseRate * deltaTime);
                }
                ReplicatedEngineRpm = Mathf.Clamp(ReplicatedEngineRpm, asset.EngineIdleRpm, asset.EngineMaxRpm);
                float num8 = Mathf.InverseLerp(asset.EngineIdleRpm, asset.EngineMaxRpm, ReplicatedEngineRpm);
                float num9 = ((engineCurvesComponent != null) ? engineCurvesComponent.engineRpmToTorqueCurve.Evaluate(num8) : Mathf.Lerp(0.5f, 1f, num8)) * asset.EngineMaxTorque * Mathf.Abs(latestGasInput);
                if (timeSinceLastGearChange < asset.GearShiftDuration)
                {
                    num9 = 0f;
                }
                if (GearNumber == -1)
                {
                    num9 *= asset.reverseGearRatio;
                }
                else if (asset.UsesEngineRpmAndGears && GearNumber >= 1 && GearNumber <= asset.forwardGearRatios.Length)
                {
                    num9 *= asset.forwardGearRatios[GearNumber - 1];
                }
                if (asset.poweredWheelIndices != null && asset.poweredWheelIndices.Length != 0)
                {
                    num9 /= (float)asset.poweredWheelIndices.Length;
                }
                Wheel[] wheels = _wheels;
                foreach (Wheel wheel2 in wheels)
                {
                    if (wheel2 == null)
                    {
                        break;
                    }
                    wheel2.UpdateLocallyDriven(deltaTime, num9);
                }
            }
            if (asset.engine == EEngine.TRAIN && road != null)
            {
                TrainCar[] array3 = trainCars;
                foreach (TrainCar trainCar in array3)
                {
                    road.getTrackData(clampRoadPosition(roadPosition + trainCar.trackPositionOffset + asset.trainWheelOffset), out var position3, out var normal, out var direction);
                    road.getTrackData(clampRoadPosition(roadPosition + trainCar.trackPositionOffset - asset.trainWheelOffset), out var position4, out var normal2, out var direction2);
                    moveTrain(position3, normal, direction, position4, normal2, direction2, trainCar);
                }
                float num10 = inputEngineVelocity * deltaTime;
                Transform transform3 = ((!(inputEngineVelocity > 0f)) ? overlapBack : overlapFront);
                BoxCollider boxCollider = transform3?.GetComponent<BoxCollider>();
                bool flag3;
                if (boxCollider != null)
                {
                    flag3 = false;
                    Vector3 vector = transform3.position + transform3.forward * num10 / 2f;
                    Vector3 size = boxCollider.size;
                    size.z = num10;
                    int num11 = Physics.OverlapBoxNonAlloc(vector, size / 2f, tempCollidersArray, transform3.rotation, RayMasks.BLOCK_TRAIN, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < num11; k++)
                    {
                        bool flag4 = false;
                        for (int l = 0; l < trainCars.Length; l++)
                        {
                            if (tempCollidersArray[k].transform.IsChildOf(trainCars[l].root) || tempCollidersArray[k].transform == trainCars[l].root)
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (flag4)
                        {
                            continue;
                        }
                        if (tempCollidersArray[k].CompareTag("Vehicle"))
                        {
                            Rigidbody component = tempCollidersArray[k].GetComponent<Rigidbody>();
                            if (!component.isKinematic)
                            {
                                component.AddForce(base.transform.forward * inputEngineVelocity, ForceMode.VelocityChange);
                            }
                        }
                        flag3 = true;
                        break;
                    }
                }
                else
                {
                    flag3 = true;
                }
                if (flag3)
                {
                    if (inputEngineVelocity > 0f)
                    {
                        if (inputTargetVelocity > 0f)
                        {
                            inputTargetVelocity = 0f;
                        }
                    }
                    else if (inputTargetVelocity < 0f)
                    {
                        inputTargetVelocity = 0f;
                    }
                }
                else
                {
                    roadPosition += num10;
                    roadPosition = clampRoadPosition(roadPosition);
                }
            }
        }
        if (!Dedicator.IsDedicatedServer && road != null)
        {
            TrainCar[] array3 = trainCars;
            foreach (TrainCar trainCar2 in array3)
            {
                road.getTrackData(clampRoadPosition(roadPosition + trainCar2.trackPositionOffset + asset.trainWheelOffset), out var position5, out var normal3, out var direction3);
                trainCar2.currentFrontPosition = Vector3.Lerp(trainCar2.currentFrontPosition, position5, 8f * Time.deltaTime);
                trainCar2.currentFrontNormal = Vector3.Lerp(trainCar2.currentFrontNormal, normal3, 8f * Time.deltaTime);
                trainCar2.currentFrontDirection = Vector3.Lerp(trainCar2.currentFrontDirection, direction3, 8f * Time.deltaTime);
                road.getTrackData(clampRoadPosition(roadPosition + trainCar2.trackPositionOffset - asset.trainWheelOffset), out var position6, out var normal4, out var direction4);
                trainCar2.currentBackPosition = Vector3.Lerp(trainCar2.currentBackPosition, position6, 8f * Time.deltaTime);
                trainCar2.currentBackNormal = Vector3.Lerp(trainCar2.currentBackNormal, normal4, 8f * Time.deltaTime);
                trainCar2.currentBackDirection = Vector3.Lerp(trainCar2.currentBackDirection, direction4, 8f * Time.deltaTime);
                moveTrain(trainCar2.currentFrontPosition, trainCar2.currentFrontNormal, trainCar2.currentFrontDirection, trainCar2.currentBackPosition, trainCar2.currentBackNormal, trainCar2.currentBackDirection, trainCar2);
            }
        }
        if (Provider.isServer)
        {
            if (isDriven)
            {
                if (asset != null && asset.canTiresBeDamaged && _wheels != null)
                {
                    Wheel[] wheels = _wheels;
                    foreach (Wheel wheel3 in wheels)
                    {
                        if (!(wheel3.wheel == null) && !wheel3.IsDead)
                        {
                            wheel3.CheckForTraps();
                        }
                    }
                }
            }
            else
            {
                ReplicatedSpeed = rootRigidbody.velocity.magnitude;
                ReplicatedForwardVelocity = base.transform.InverseTransformDirection(rootRigidbody.velocity).z;
                ReplicatedSteeringInput = 0f;
                ReplicatedVelocityInput = 0f;
                real = base.transform.position;
            }
            if (isDead && !isExploded && !isUnderwater && Time.realtimeSinceStartup - lastDead > 4f)
            {
                explode();
            }
        }
        if (!Provider.isServer && !isPhysical && Time.realtimeSinceStartup - lastTick > Provider.UPDATE_TIME * 2f)
        {
            lastTick = Time.realtimeSinceStartup;
            ReplicatedSpeed = 0f;
            ReplicatedForwardVelocity = 0f;
            ReplicatedVelocityInput = 0f;
        }
        if (sirensOn && !Dedicator.IsDedicatedServer && Time.realtimeSinceStartup - lastWeeoo > 0.33f)
        {
            lastWeeoo = Time.realtimeSinceStartup;
            sirenState = !sirenState;
            foreach (GameObject item in sirenGameObjects_0)
            {
                item.SetActive(!sirenState);
            }
            foreach (GameObject item2 in sirenGameObjects_1)
            {
                item2.SetActive(sirenState);
            }
            if (sirenMaterials != null)
            {
                if (sirenMaterials[0] != null)
                {
                    sirenMaterials[0].SetColor("_EmissionColor", (!sirenState) ? (sirenMaterials[0].color * 2f) : Color.black);
                }
                if (sirenMaterials[1] != null)
                {
                    sirenMaterials[1].SetColor("_EmissionColor", sirenState ? (sirenMaterials[1].color * 2f) : Color.black);
                }
            }
        }
        if (usesBattery)
        {
            bool flag5 = false;
            bool flag6 = false;
            if (isDriven && isEnginePowered)
            {
                switch (asset.batteryDriving)
                {
                case EBatteryMode.Burn:
                    flag6 = true;
                    break;
                case EBatteryMode.Charge:
                    flag5 = true;
                    break;
                }
            }
            else
            {
                switch (asset.batteryEmpty)
                {
                case EBatteryMode.Burn:
                    flag6 = true;
                    break;
                case EBatteryMode.Charge:
                    flag5 = true;
                    break;
                }
            }
            if (headlightsOn)
            {
                switch (asset.batteryHeadlights)
                {
                case EBatteryMode.Burn:
                    flag6 = true;
                    break;
                case EBatteryMode.Charge:
                    flag5 = true;
                    break;
                }
            }
            if (sirensOn)
            {
                switch (asset.batterySirens)
                {
                case EBatteryMode.Burn:
                    flag6 = true;
                    break;
                case EBatteryMode.Charge:
                    flag5 = true;
                    break;
                }
            }
            flag5 &= ContainsBatteryItem;
            float num12 = 0f;
            if (flag5)
            {
                num12 = asset.batteryChargeRate;
            }
            else if (flag6)
            {
                num12 = asset.batteryBurnRate;
            }
            batteryBuffer += deltaTime * num12;
            ushort num13 = (ushort)Mathf.FloorToInt(batteryBuffer);
            if (num13 > 0)
            {
                batteryBuffer -= (int)num13;
                if (flag5)
                {
                    askChargeBattery(num13);
                }
                else if (flag6)
                {
                    askBurnBattery(num13);
                }
            }
        }
        if (Provider.isServer)
        {
            UpdateSafezoneStatus(deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!isPhysical || isDriven || !Provider.isServer)
        {
            return;
        }
        bool flag = false;
        if (Dedicator.IsDedicatedServer)
        {
            if (asset.replicatedWheelIndices != null)
            {
                int[] replicatedWheelIndices = asset.replicatedWheelIndices;
                foreach (int num in replicatedWheelIndices)
                {
                    Wheel wheelAtIndex = GetWheelAtIndex(num);
                    if (wheelAtIndex == null)
                    {
                        UnturnedLog.error($"\"{asset.FriendlyName}\" missing wheel for replicated index: {num}");
                        continue;
                    }
                    wheelAtIndex.UpdateServerSuspensionAndPhysicsMaterial();
                    flag |= wheelAtIndex.isGrounded;
                }
            }
        }
        else if (_wheels != null)
        {
            Wheel[] wheels = _wheels;
            foreach (Wheel wheel in wheels)
            {
                wheel.UpdateGrounded();
                flag |= wheel.isGrounded;
            }
        }
        if (_wheels != null && flag && asset.wheelBalancingForceMultiplier > 0f)
        {
            ApplyWheelBalancingForce(Time.fixedDeltaTime);
        }
        if (asset.rollAngularVelocityDamping > 0f)
        {
            ApplyAngularVelocityDamping(Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Update whether this vehicle is inside a safezone.
    /// If a certain option is enabled, unlock after time threshold is passed.
    /// </summary>
    private void UpdateSafezoneStatus(float deltaSeconds)
    {
        isInsideSafezone = LevelNodes.isPointInsideSafezone(base.transform.position, out var outSafezoneNode);
        insideSafezoneNode = outSafezoneNode;
        if (isInsideSafezone)
        {
            timeInsideSafezone += deltaSeconds;
            if (Provider.modeConfigData != null && Provider.modeConfigData.Vehicles.Unlocked_After_Seconds_In_Safezone > 0f && timeInsideSafezone > Provider.modeConfigData.Vehicles.Unlocked_After_Seconds_In_Safezone && isEmpty && isLocked)
            {
                VehicleManager.unlockVehicle(this, null);
            }
        }
        else
        {
            timeInsideSafezone = -1f;
        }
    }

    protected virtual void handleTireAliveChanged(Wheel wheel)
    {
        if (isPhysical)
        {
            rootRigidbody.WakeUp();
        }
    }

    /// <summary>
    /// Can be called without calling init.
    /// </summary>
    internal void safeInit(VehicleAsset asset)
    {
        _asset = asset;
        if (!Dedicator.IsDedicatedServer)
        {
            fire = base.transform.Find("Fire");
            LightLODTool.applyLightLOD(fire);
            smoke_0 = base.transform.Find("Smoke_0");
            smoke_1 = base.transform.Find("Smoke_1");
        }
        ApplyDepthMaskMaterial();
    }

    internal void init(VehicleAsset asset)
    {
        safeInit(asset);
        eventHook = base.gameObject.GetComponent<VehicleEventHook>();
        engineCurvesComponent = base.gameObject.GetComponentInChildren<EngineCurvesComponent>(includeInactive: true);
        if (Provider.isServer)
        {
            if (fuel == ushort.MaxValue)
            {
                if (Provider.mode == EGameMode.TUTORIAL)
                {
                    fuel = 0;
                }
                else
                {
                    fuel = (ushort)UnityEngine.Random.Range(asset.fuelMin, asset.fuelMax);
                }
            }
            if (health == ushort.MaxValue)
            {
                health = (ushort)UnityEngine.Random.Range(asset.healthMin, asset.healthMax);
            }
            if (batteryCharge == ushort.MaxValue)
            {
                if (usesBattery)
                {
                    if (asset.canSpawnWithBattery && UnityEngine.Random.value < Provider.modeConfigData.Vehicles.Has_Battery_Chance)
                    {
                        float num = UnityEngine.Random.Range(Provider.modeConfigData.Vehicles.Min_Battery_Charge, Provider.modeConfigData.Vehicles.Max_Battery_Charge);
                        num *= asset.batterySpawnChargeMultiplier;
                        batteryCharge = (ushort)Mathf.Max(1f, 10000f * num);
                    }
                    else
                    {
                        batteryCharge = 0;
                    }
                }
                else
                {
                    batteryCharge = 10000;
                }
            }
            if (PaintColor.a != byte.MaxValue)
            {
                Color32? randomDefaultPaintColor = asset.GetRandomDefaultPaintColor();
                if (randomDefaultPaintColor.HasValue)
                {
                    Color32 value = randomDefaultPaintColor.Value;
                    value.a = byte.MaxValue;
                    PaintColor = value;
                }
            }
        }
        if (!Dedicator.IsDedicatedServer)
        {
            base.transform.FindAllChildrenWithName("Sirens", sirenGameObjects);
            base.transform.FindAllChildrenWithName("Siren_0", sirenGameObjects_0);
            base.transform.FindAllChildrenWithName("Siren_1", sirenGameObjects_1);
            foreach (GameObject sirenGameObject in sirenGameObjects)
            {
                LightLODTool.applyLightLOD(sirenGameObject.transform);
            }
            sirenMaterials = new Material[2];
            List<GameObject> list = new List<GameObject>();
            base.transform.FindAllChildrenWithName("Siren_0_Model", list);
            foreach (GameObject item in list)
            {
                if (sirenMaterials[0] == null)
                {
                    Renderer component = item.GetComponent<Renderer>();
                    if (component != null)
                    {
                        sirenMaterials[0] = component.material;
                    }
                }
                else
                {
                    item.GetComponent<Renderer>().sharedMaterial = sirenMaterials[0];
                }
            }
            list.Clear();
            base.transform.FindAllChildrenWithName("Siren_1_Model", list);
            foreach (GameObject item2 in list)
            {
                if (sirenMaterials[1] == null)
                {
                    Renderer component2 = item2.GetComponent<Renderer>();
                    if (component2 != null)
                    {
                        sirenMaterials[1] = component2.material;
                    }
                }
                else
                {
                    item2.GetComponent<Renderer>().sharedMaterial = sirenMaterials[1];
                }
            }
            _headlights = base.transform.Find("Headlights");
            LightLODTool.applyLightLOD(headlights);
            Transform transform = base.transform.FindChildRecursive("Headlights_Model");
            if (transform != null)
            {
                Renderer component3 = transform.GetComponent<Renderer>();
                if ((bool)component3)
                {
                    headlightsMaterial = component3.material;
                }
            }
            _taillights = base.transform.Find("Taillights");
            LightLODTool.applyLightLOD(taillights);
            Transform transform2 = base.transform.FindChildRecursive("Taillights_Model");
            if (transform2 != null)
            {
                Renderer component4 = transform2.GetComponent<Renderer>();
                if ((bool)component4)
                {
                    taillightsMaterial = component4.material;
                }
            }
            else
            {
                tempMaterialsList.Clear();
                for (int i = 0; i < 4; i++)
                {
                    Transform transform3 = base.transform.Find("Taillight_" + i + "_Model");
                    if (transform3 == null)
                    {
                        break;
                    }
                    Renderer component5 = transform3.GetComponent<Renderer>();
                    if (component5 != null)
                    {
                        tempMaterialsList.Add(component5.material);
                    }
                }
                if (tempMaterialsList.Count > 0)
                {
                    taillightMaterials = tempMaterialsList.ToArray();
                }
            }
            if ((asset.engine == EEngine.HELICOPTER || asset.engine == EEngine.BLIMP) && clipAudioSource != null)
            {
                windZone = clipAudioSource.gameObject.AddComponent<WindZone>();
                windZone.mode = WindZoneMode.Spherical;
                windZone.radius = 64f;
                windZone.windMain = 0f;
                windZone.windTurbulence = 0f;
                windZone.windPulseFrequency = 0f;
                windZone.windPulseMagnitude = 0f;
            }
        }
        _sirensOn = false;
        _headlightsOn = false;
        _taillightsOn = false;
        waterCenterTransform = base.transform.Find("Water_Center");
        Transform transform4 = base.transform.Find("Seats");
        if (transform4 == null)
        {
            Assets.ReportError(asset, "missing 'Seats' Transform");
            transform4 = new GameObject("Seats").transform;
            transform4.parent = base.transform;
        }
        Transform transform5 = base.transform.Find("Objects");
        Transform transform6 = base.transform.Find("Turrets");
        Transform transform7 = base.transform.Find("Train_Cars");
        _passengers = new Passenger[transform4.childCount];
        for (int j = 0; j < passengers.Length; j++)
        {
            string text = "Seat_" + j;
            Transform transform8 = transform4.Find(text);
            if (transform8 == null)
            {
                Assets.ReportError(asset, "missing '{0}' Transform", text);
                transform8 = new GameObject(text).transform;
                transform8.parent = transform4;
            }
            Transform newObj = null;
            if (transform5 != null)
            {
                newObj = transform5.Find("Seat_" + j);
            }
            Transform transform9 = null;
            Transform transform10 = null;
            Transform newTurretPitch = null;
            Transform newTurretAim = null;
            if (transform6 != null)
            {
                transform9 = transform6.Find("Turret_" + j);
                if (transform9 != null)
                {
                    transform10 = transform9.Find("Yaw");
                    if (transform10 != null)
                    {
                        Transform transform11 = transform10.Find("Seats");
                        if (transform11 != null)
                        {
                            transform8 = transform11.Find("Seat_" + j);
                        }
                        Transform transform12 = transform10.Find("Objects");
                        if (transform12 != null)
                        {
                            newObj = transform12.Find("Seat_" + j);
                        }
                        newTurretPitch = transform10.Find("Pitch");
                    }
                    newTurretAim = transform9.FindChildRecursive("Aim");
                }
            }
            if (transform7 != null)
            {
                Transform transform13 = transform7.FindChildRecursive(text);
                if (transform13 != null)
                {
                    transform8 = transform13;
                }
            }
            passengers[j] = new Passenger(transform8, newObj, transform10, newTurretPitch, newTurretAim);
            if (transform9 != null)
            {
                passengers[j].turretEventHook = transform9.GetComponent<VehicleTurretEventHook>();
            }
            if (asset.shouldSpawnSeatCapsules)
            {
                Transform obj = new GameObject("Clip")
                {
                    layer = 21
                }.transform;
                obj.parent = transform8;
                obj.localPosition = Vector3.zero;
                obj.localRotation = Quaternion.identity;
                obj.localScale = Vector3.one;
                obj.parent = base.transform;
                CapsuleCollider orAddComponent = obj.GetOrAddComponent<CapsuleCollider>();
                orAddComponent.center = new Vector3(0f, PlayerMovement.HEIGHT_STAND * 0.5f, 0f);
                orAddComponent.height = PlayerMovement.HEIGHT_STAND;
                orAddComponent.radius = PlayerStance.RADIUS;
                orAddComponent.enabled = false;
                passengers[j].collider = orAddComponent;
            }
        }
        _turrets = new Passenger[asset.turrets.Length];
        for (int k = 0; k < turrets.Length; k++)
        {
            TurretInfo turretInfo = asset.turrets[k];
            if (turretInfo.seatIndex < passengers.Length)
            {
                passengers[turretInfo.seatIndex].turret = turretInfo;
                _turrets[k] = passengers[turretInfo.seatIndex];
            }
        }
        InitializeWheels();
        buoyancy = base.transform.Find("Buoyancy");
        if (buoyancy != null)
        {
            for (int l = 0; l < buoyancy.childCount; l++)
            {
                Transform child = buoyancy.GetChild(l);
                child.gameObject.AddComponent<Buoyancy>().density = buoyancy.childCount * 500;
                if (asset.engine == EEngine.BLIMP)
                {
                    child.GetComponent<Buoyancy>().overrideSurfaceElevation = Level.info.configData.Blimp_Altitude;
                }
            }
        }
        hook = base.transform.Find("Hook");
        hooked = new List<HookInfo>();
        if (!Dedicator.IsDedicatedServer)
        {
            steeringWheelModelTransform = base.transform.Find("Objects/Steer");
            if (steeringWheelModelTransform != null)
            {
                steeringWheelRestLocalRotation = steeringWheelModelTransform.localRotation;
            }
            pedalLeft = base.transform.Find("Objects").Find("Pedal_Left");
            pedalRight = base.transform.Find("Objects").Find("Pedal_Right");
            Transform transform14 = base.transform.Find("Rotors");
            if (transform14 != null)
            {
                propellerModels = new PropellerModel[transform14.childCount];
                int num2 = 0;
                foreach (Transform item3 in transform14)
                {
                    PropellerModel propellerModel = new PropellerModel();
                    propellerModel.transform = item3;
                    propellerModel.bladeMaterial = item3.Find("Model_0").GetComponent<Renderer>()?.material;
                    propellerModel.motionBlurMaterial = item3.Find("Model_1").GetComponent<Renderer>()?.material;
                    propellerModel.baseLocationRotation = item3.localRotation;
                    if (asset.requiredShaderUpgrade)
                    {
                        if (StandardShaderUtils.isMaterialUsingStandardShader(propellerModel.bladeMaterial))
                        {
                            StandardShaderUtils.setModeToTransparent(propellerModel.bladeMaterial);
                        }
                        if (StandardShaderUtils.isMaterialUsingStandardShader(propellerModel.motionBlurMaterial))
                        {
                            StandardShaderUtils.setModeToTransparent(propellerModel.motionBlurMaterial);
                        }
                    }
                    materialsToDestroy.Add(propellerModel.bladeMaterial);
                    materialsToDestroy.Add(propellerModel.motionBlurMaterial);
                    waterSortHandles.Add(DynamicWaterTransparentSort.Get().Register(item3, propellerModel.bladeMaterial));
                    waterSortHandles.Add(DynamicWaterTransparentSort.Get().Register(item3, propellerModel.motionBlurMaterial));
                    propellerModels[num2] = propellerModel;
                    num2++;
                }
            }
            Transform transform16 = base.transform.Find("Exhaust");
            if (transform16 != null)
            {
                exhaustGameObject = transform16.gameObject;
                isExhaustGameObjectActive = exhaustGameObject.activeSelf;
                exhaustParticleSystems = new ParticleSystem[transform16.childCount];
                for (int m = 0; m < transform16.childCount; m++)
                {
                    Transform child2 = transform16.GetChild(m);
                    exhaustParticleSystems[m] = child2.GetComponent<ParticleSystem>();
                    ParticleSystem.EmissionModule emission = exhaustParticleSystems[m].emission;
                    emission.rateOverTime = 0f;
                }
                isExhaustRateOverTimeZero = true;
            }
            frontModelTransform = base.transform.Find("Objects/Front");
            if (frontModelTransform != null)
            {
                frontModelRestLocalRotation = frontModelTransform.localRotation;
            }
            tellFuel(fuel);
            tellHealth(health);
            tellBatteryCharge(batteryCharge);
            InitializeAdditionalTransparentSections();
            InitializePaintableSections();
            InitializeCrawlerTrackTilingMaterials();
            updateSkin();
        }
        if (isExploded)
        {
            tellExploded();
        }
        if (asset.engine == EEngine.TRAIN)
        {
            int childCount = transform7.childCount;
            trainCars = new TrainCar[1 + childCount];
            trainCars[0] = getTrainCar(base.transform);
            for (int n = 1; n <= childCount; n++)
            {
                Transform transform17 = transform7.Find("Train_Car_" + n);
                transform17.parent = null;
                transform17.GetOrAddComponent<VehicleRef>().vehicle = this;
                TrainCar trainCar = getTrainCar(transform17);
                trainCar.trackPositionOffset = (float)n * (0f - asset.trainCarLength);
                trainCars[n] = trainCar;
            }
            TrainCar[] array = trainCars;
            foreach (TrainCar trainCar2 in array)
            {
                if (overlapFront == null)
                {
                    overlapFront = trainCar2.root.Find("Overlap_Front");
                }
                if (overlapBack == null)
                {
                    overlapBack = trainCar2.root.Find("Overlap_Back");
                }
                if (overlapFront != null && overlapBack != null)
                {
                    break;
                }
            }
            foreach (LevelTrainAssociation train in Level.info.configData.Trains)
            {
                if (train.VehicleID == id)
                {
                    roadIndex = train.RoadIndex;
                    break;
                }
            }
            road = LevelRoads.getRoad(roadIndex);
            roadPosition = clampRoadPosition(roadPosition);
            teleportTrain();
        }
        if (asset.physicsProfileRef.isValid)
        {
            asset.physicsProfileRef.Find()?.applyTo(this);
        }
        decayLastUpdateTime = Time.time;
        decayLastUpdatePosition = base.transform.position;
    }

    private void Awake()
    {
        rootRigidbody = GetComponent<Rigidbody>();
        if (!Dedicator.IsDedicatedServer)
        {
            Transform transform = base.transform.Find("Sound");
            if (transform != null)
            {
                clipAudioSource = transform.GetComponent<AudioSource>();
            }
        }
    }

    private void initBumper(Transform bumper, bool reverse, bool instakill)
    {
        if (!(bumper == null))
        {
            if (Provider.isServer)
            {
                Bumper bumper2 = bumper.gameObject.AddComponent<Bumper>();
                bumper2.reverse = reverse;
                bumper2.instakill = instakill;
                bumper2.init(this);
            }
            else
            {
                UnityEngine.Object.Destroy(bumper.gameObject);
            }
        }
    }

    private void initBumpers(Transform root)
    {
        Transform transform = root.FindChildRecursive("Nav");
        if (transform != null)
        {
            if (Provider.isServer)
            {
                transform.DestroyRigidbody();
            }
            else
            {
                UnityEngine.Object.Destroy(transform.gameObject);
            }
        }
        Transform bumper = root.FindChildRecursive("Bumper");
        initBumper(bumper, reverse: false, asset.engine == EEngine.TRAIN);
        Transform bumper2 = root.FindChildRecursive("Bumper_Front");
        initBumper(bumper2, reverse: false, asset.engine == EEngine.TRAIN);
        Transform bumper3 = root.FindChildRecursive("Bumper_Back");
        initBumper(bumper3, reverse: true, asset.engine == EEngine.TRAIN);
    }

    private void Start()
    {
        if (trainCars != null && trainCars.Length != 0)
        {
            TrainCar[] array = trainCars;
            foreach (TrainCar trainCar in array)
            {
                initBumpers(trainCar.root);
            }
        }
        else
        {
            initBumpers(base.transform);
        }
        updateVehicle();
        updatePhysics();
        updateEngine();
        updates = new List<VehicleStateUpdate>();
        switch (asset.engineSoundType)
        {
        case EVehicleEngineSoundType.Legacy:
            base.gameObject.AddComponent<DefaultEngineSoundController>().vehicle = this;
            break;
        case EVehicleEngineSoundType.EngineRPMSimple:
            base.gameObject.AddComponent<RpmEngineSoundController>().vehicle = this;
            break;
        }
    }

    private void OnDestroy()
    {
        dropTrunkItems();
        if (_wheels != null)
        {
            Wheel[] wheels = _wheels;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].OnVehicleDestroyed();
            }
        }
        if (skinMaterialToDestroy != null)
        {
            UnityEngine.Object.Destroy(skinMaterialToDestroy);
            skinMaterialToDestroy = null;
        }
        if (materialsToDestroy != null)
        {
            foreach (Material item in materialsToDestroy)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item);
                }
            }
            materialsToDestroy.Clear();
        }
        if (waterSortHandles != null)
        {
            DynamicWaterTransparentSort dynamicWaterTransparentSort = DynamicWaterTransparentSort.Get();
            foreach (object waterSortHandle in waterSortHandles)
            {
                dynamicWaterTransparentSort.Unregister(waterSortHandle);
            }
            waterSortHandles.Clear();
        }
        if (isExploded && !Dedicator.IsDedicatedServer && asset.ShouldExplosionBurnMaterials && asset.explosionBurnMaterialSections == null)
        {
            HighlighterTool.destroyMaterials(base.transform);
            if (turrets != null)
            {
                for (int j = 0; j < turrets.Length; j++)
                {
                    HighlighterTool.destroyMaterials(turrets[j].turretYaw);
                    HighlighterTool.destroyMaterials(turrets[j].turretPitch);
                }
            }
        }
        if (headlightsMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(headlightsMaterial);
        }
        if (taillightsMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(taillightsMaterial);
        }
        else if (taillightMaterials != null)
        {
            for (int k = 0; k < taillightMaterials.Length; k++)
            {
                if (taillightMaterials[k] != null)
                {
                    UnityEngine.Object.DestroyImmediate(taillightMaterials[k]);
                }
            }
        }
        if (sirenMaterials == null)
        {
            return;
        }
        for (int l = 0; l < sirenMaterials.Length; l++)
        {
            if (sirenMaterials[l] != null)
            {
                UnityEngine.Object.DestroyImmediate(sirenMaterials[l]);
            }
        }
    }

    /// <summary>
    /// Called after initializing vehicle.
    /// </summary>
    public void gatherVehicleColliders()
    {
        _vehicleColliders = new List<Collider>();
        base.gameObject.GetComponentsInChildren(includeInactive: true, _vehicleColliders);
        initCenterCollider();
        if (trainCars != null)
        {
            TrainCar[] array = trainCars;
            foreach (TrainCar obj in array)
            {
                _trainCarColliders.Clear();
                obj.root.GetComponentsInChildren(includeInactive: true, _trainCarColliders);
                _vehicleColliders.AddRange(_trainCarColliders);
            }
        }
    }

    /// <summary>
    /// Makes the collision detection system ignore all collisions between this vehicle and the given colliders.
    /// Used to prevent vehicle from colliding with attached items.
    /// </summary>
    public void ignoreCollisionWith(IEnumerable<Collider> otherColliders, bool shouldIgnore)
    {
        if (_vehicleColliders == null)
        {
            throw new Exception("gatherVehicleColliders was not called yet");
        }
        for (int num = _vehicleColliders.Count - 1; num >= 0; num--)
        {
            Collider collider = _vehicleColliders[num];
            if (collider == null)
            {
                _vehicleColliders.RemoveAtFast(num);
            }
            else
            {
                foreach (Collider otherCollider in otherColliders)
                {
                    if (!(otherCollider == null))
                    {
                        Physics.IgnoreCollision(collider, otherCollider, shouldIgnore);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Used to disable collision between skycrane and held vehicle.
    /// </summary>
    private void ignoreCollisionWithVehicle(InteractableVehicle otherVehicle, bool shouldIgnore)
    {
        ignoreCollisionWith(otherVehicle._vehicleColliders, shouldIgnore);
    }

    public Vector3 getClosestPointOnHull(Vector3 position)
    {
        if (_vehicleColliders == null)
        {
            throw new Exception("gatherVehicleColliders was not called yet");
        }
        return CollisionUtil.ClosestPoint(_vehicleColliders, position);
    }

    public float getSqrDistanceFromHull(Vector3 position)
    {
        return (getClosestPointOnHull(position) - position).sqrMagnitude;
    }

    /// <summary>
    /// Find collider with the largest volume to use for exit physics queries.
    /// </summary>
    private void initCenterCollider()
    {
        center = base.transform.Find("Center");
        if (center != null)
        {
            return;
        }
        center = new GameObject("Center").transform;
        center.parent = base.transform;
        center.localPosition = Vector3.zero;
        center.localRotation = Quaternion.identity;
        center.localScale = Vector3.one;
        float num = 0.001f;
        foreach (Collider vehicleCollider in _vehicleColliders)
        {
            if (vehicleCollider.isTrigger)
            {
                continue;
            }
            if (vehicleCollider is BoxCollider collider)
            {
                float boxVolume = collider.GetBoxVolume();
                if (boxVolume > num)
                {
                    num = boxVolume;
                    center.position = collider.TransformBoxCenter();
                }
            }
            else if (vehicleCollider is SphereCollider collider2)
            {
                float sphereVolume = collider2.GetSphereVolume();
                if (sphereVolume > num)
                {
                    num = sphereVolume;
                    center.position = collider2.TransformSphereCenter();
                }
            }
            else if (vehicleCollider is CapsuleCollider collider3)
            {
                float capsuleVolume = collider3.GetCapsuleVolume();
                if (capsuleVolume > num)
                {
                    num = capsuleVolume;
                    center.position = collider3.TransformCapsuleCenter();
                }
            }
        }
    }

    private void InitializeWheels()
    {
        if (asset.wheelConfiguration != null && asset.wheelConfiguration.Length != 0)
        {
            List<Wheel> list = new List<Wheel>(asset.wheelConfiguration.Length);
            VehicleWheelConfiguration[] wheelConfiguration = asset.wheelConfiguration;
            foreach (VehicleWheelConfiguration vehicleWheelConfiguration in wheelConfiguration)
            {
                WheelCollider wheelCollider = null;
                if (!string.IsNullOrEmpty(vehicleWheelConfiguration.wheelColliderPath))
                {
                    Transform transform = base.transform.Find(vehicleWheelConfiguration.wheelColliderPath);
                    if (transform == null)
                    {
                        Assets.ReportError(asset, "missing wheel collider transform at path \"" + vehicleWheelConfiguration.wheelColliderPath + "\"");
                    }
                    else
                    {
                        wheelCollider = transform.GetComponent<WheelCollider>();
                        if (wheelCollider == null)
                        {
                            Assets.ReportError(asset, "missing WheelCollider component at path \"" + vehicleWheelConfiguration.wheelColliderPath + "\"");
                        }
                        else if (asset.wheelColliderMassOverride.HasValue)
                        {
                            wheelCollider.mass = asset.wheelColliderMassOverride.Value;
                        }
                    }
                }
                Transform transform2 = null;
                if (!string.IsNullOrEmpty(vehicleWheelConfiguration.modelPath))
                {
                    transform2 = base.transform.Find(vehicleWheelConfiguration.modelPath);
                    if (transform2 == null)
                    {
                        Assets.ReportError(asset, "missing wheel model transform at path \"" + vehicleWheelConfiguration.modelPath + "\"");
                    }
                }
                if (!(wheelCollider == null) || !(transform2 == null))
                {
                    Wheel wheel = new Wheel(this, list.Count, wheelCollider, transform2, vehicleWheelConfiguration);
                    wheel.Reset();
                    wheel.aliveChanged += handleTireAliveChanged;
                    list.Add(wheel);
                }
            }
            _wheels = list.ToArray();
        }
        else
        {
            _wheels = new Wheel[0];
        }
    }

    /// <summary>
    /// Set material on DepthMask child renderer responsible for hiding water when interior of vehicle is submerged.
    /// </summary>
    private void ApplyDepthMaskMaterial()
    {
        Transform transform = base.transform.Find("DepthMask");
        if (transform != null)
        {
            Renderer component = transform.GetComponent<Renderer>();
            if (component != null)
            {
                component.sharedMaterial = Resources.Load<Material>("Materials/DepthMask");
            }
        }
    }

    private void InitializeAdditionalTransparentSections()
    {
        if (asset.extraTransparentSections == null || asset.extraTransparentSections.Length < 1)
        {
            return;
        }
        DynamicWaterTransparentSort dynamicWaterTransparentSort = DynamicWaterTransparentSort.Get();
        string[] extraTransparentSections = asset.extraTransparentSections;
        foreach (string text in extraTransparentSections)
        {
            Transform transform = base.transform.Find(text);
            if (transform == null)
            {
                Assets.ReportError(asset, "missing additional transparent section transform \"" + text + "\"");
                continue;
            }
            Renderer component = transform.GetComponent<Renderer>();
            if (component == null)
            {
                Assets.ReportError(asset, "additional transparent section \"" + text + "\" missing Renderer component");
                continue;
            }
            Material material = component.material;
            if (material == null)
            {
                Assets.ReportError(asset, "additional transparent section \"" + text + "\" missing material");
                continue;
            }
            materialsToDestroy.Add(material);
            object item = dynamicWaterTransparentSort.Register(transform, material);
            waterSortHandles.Add(item);
        }
    }

    private void ApplyExplosionBurnMaterials()
    {
        if (asset.explosionBurnMaterialSections == null)
        {
            HighlighterTool.color(base.transform, new Color(0.25f, 0.25f, 0.25f));
            if (frontModelTransform != null)
            {
                HighlighterTool.color(frontModelTransform, new Color(0.25f, 0.25f, 0.25f));
            }
            if (turrets != null)
            {
                for (int i = 0; i < turrets.Length; i++)
                {
                    HighlighterTool.color(turrets[i].turretYaw, new Color(0.25f, 0.25f, 0.25f));
                    HighlighterTool.color(turrets[i].turretPitch, new Color(0.25f, 0.25f, 0.25f));
                }
            }
            return;
        }
        PaintableVehicleSection[] explosionBurnMaterialSections = asset.explosionBurnMaterialSections;
        for (int j = 0; j < explosionBurnMaterialSections.Length; j++)
        {
            PaintableVehicleSection paintableVehicleSection = explosionBurnMaterialSections[j];
            Transform transform = base.transform.Find(paintableVehicleSection.path);
            if (transform == null)
            {
                Assets.ReportError(asset, "explosion burn section missing transform \"" + paintableVehicleSection.path + "\"");
                continue;
            }
            Renderer component = transform.GetComponent<Renderer>();
            if (component == null)
            {
                Assets.ReportError(asset, "explosion burn section missing renderer \"" + paintableVehicleSection.path + "\"");
                continue;
            }
            tempMaterialsList.Clear();
            component.GetMaterials(tempMaterialsList);
            if (paintableVehicleSection.allMaterials)
            {
                foreach (Material tempMaterials in tempMaterialsList)
                {
                    materialsToDestroy.Add(tempMaterials);
                    tempMaterials.color = new Color(0.25f, 0.25f, 0.25f);
                }
                continue;
            }
            foreach (Material tempMaterials2 in tempMaterialsList)
            {
                materialsToDestroy.Add(tempMaterials2);
            }
            if (paintableVehicleSection.materialIndex < 0 || paintableVehicleSection.materialIndex >= tempMaterialsList.Count)
            {
                Assets.ReportError(asset, $"explosion burn section \"{paintableVehicleSection.path}\" material index out of range (index: {paintableVehicleSection.materialIndex} length: {tempMaterialsList.Count})");
            }
            else
            {
                tempMaterialsList[paintableVehicleSection.materialIndex].color = new Color(0.25f, 0.25f, 0.25f);
            }
        }
    }

    private void InitializePaintableSections()
    {
        if (!asset.SupportsPaintColor)
        {
            return;
        }
        paintableMaterials = new List<Material>();
        PaintableVehicleSection[] paintableVehicleSections = asset.PaintableVehicleSections;
        for (int i = 0; i < paintableVehicleSections.Length; i++)
        {
            PaintableVehicleSection paintableVehicleSection = paintableVehicleSections[i];
            Transform transform = base.transform.Find(paintableVehicleSection.path);
            if (transform == null)
            {
                Assets.ReportError(asset, "paintable section missing transform \"" + paintableVehicleSection.path + "\"");
                continue;
            }
            Renderer component = transform.GetComponent<Renderer>();
            if (component == null)
            {
                Assets.ReportError(asset, "paintable section missing renderer \"" + paintableVehicleSection.path + "\"");
                continue;
            }
            tempMaterialsList.Clear();
            component.GetMaterials(tempMaterialsList);
            foreach (Material tempMaterials in tempMaterialsList)
            {
                materialsToDestroy.Add(tempMaterials);
            }
            if (paintableVehicleSection.allMaterials)
            {
                paintableMaterials.AddRange(tempMaterialsList);
            }
            else if (paintableVehicleSection.materialIndex < 0 || paintableVehicleSection.materialIndex >= tempMaterialsList.Count)
            {
                Assets.ReportError(asset, $"paintable section \"{paintableVehicleSection.path}\" material index out of range (index: {paintableVehicleSection.materialIndex} length: {tempMaterialsList.Count})");
            }
            else
            {
                paintableMaterials.Add(tempMaterialsList[paintableVehicleSection.materialIndex]);
            }
        }
        ApplyPaintColor();
    }

    private void ApplyPaintColor()
    {
        if (paintableMaterials == null || Dedicator.IsDedicatedServer)
        {
            return;
        }
        foreach (Material paintableMaterial in paintableMaterials)
        {
            paintableMaterial.SetColor(PAINT_COLOR_ID, PaintColor);
        }
    }

    private void ApplySkinToRenderer(Renderer renderer, Material skinMaterial, bool shared)
    {
        skinOriginalMaterials.Add(new VehicleSkinMaterialChange
        {
            renderer = renderer,
            originalMaterial = (shared ? renderer.sharedMaterial : renderer.material),
            shared = shared
        });
        if (shared)
        {
            renderer.material = skinMaterial;
        }
        else
        {
            renderer.sharedMaterial = skinMaterial;
        }
    }

    private void InitializeCrawlerTrackTilingMaterials()
    {
        if (asset.crawlerTrackTilingMaterials == null)
        {
            return;
        }
        crawlerTrackMaterials = new List<CrawlerTrackTilingMaterialInstance>(asset.crawlerTrackTilingMaterials.Length);
        CrawlerTrackTilingMaterial[] crawlerTrackTilingMaterials = asset.crawlerTrackTilingMaterials;
        for (int i = 0; i < crawlerTrackTilingMaterials.Length; i++)
        {
            CrawlerTrackTilingMaterial crawlerTrackTilingMaterial = crawlerTrackTilingMaterials[i];
            Transform transform = base.transform.Find(crawlerTrackTilingMaterial.path);
            if (transform == null)
            {
                Assets.ReportError(asset, "crawler track tiling material missing transform \"" + crawlerTrackTilingMaterial.path + "\"");
                continue;
            }
            Renderer component = transform.GetComponent<Renderer>();
            if (component == null)
            {
                Assets.ReportError(asset, "crawler track tiling material missing renderer \"" + crawlerTrackTilingMaterial.path + "\"");
                continue;
            }
            tempMaterialsList.Clear();
            component.GetMaterials(tempMaterialsList);
            foreach (Material tempMaterials in tempMaterialsList)
            {
                materialsToDestroy.Add(tempMaterials);
            }
            if (crawlerTrackTilingMaterial.materialIndex < 0 || crawlerTrackTilingMaterial.materialIndex >= tempMaterialsList.Count)
            {
                Assets.ReportError(asset, $"crawler track tiling material \"{crawlerTrackTilingMaterial.path}\" material index out of range (index: {crawlerTrackTilingMaterial.materialIndex} length: {tempMaterialsList.Count})");
                continue;
            }
            tempWheels.Clear();
            int[] wheelIndices = crawlerTrackTilingMaterial.wheelIndices;
            foreach (int num in wheelIndices)
            {
                Wheel wheelAtIndex = GetWheelAtIndex(num);
                if (wheelAtIndex == null)
                {
                    Assets.ReportError(asset, $"crawler track tiling material \"{crawlerTrackTilingMaterial.path}\" invalid wheel index: {num}");
                }
                else if (wheelAtIndex.wheel == null)
                {
                    Assets.ReportError(asset, $"crawler track tiling material \"{crawlerTrackTilingMaterial.path}\" wheel index {num} should have a collider (this wheel is visual-only)");
                }
                else
                {
                    tempWheels.Add(wheelAtIndex);
                }
            }
            if (tempWheels.Count < 1)
            {
                Assets.ReportError(asset, "crawler track tiling material \"" + crawlerTrackTilingMaterial.path + "\" has no wheels");
                continue;
            }
            crawlerTrackMaterials.Add(new CrawlerTrackTilingMaterialInstance
            {
                material = tempMaterialsList[crawlerTrackTilingMaterial.materialIndex],
                wheels = tempWheels.ToArray(),
                initialUvPosition = tempMaterialsList[crawlerTrackTilingMaterial.materialIndex].mainTextureOffset,
                repeatDistance = crawlerTrackTilingMaterial.repeatDistance,
                uvDirection = crawlerTrackTilingMaterial.uvDirection
            });
        }
        if (_wheels == null)
        {
            return;
        }
        Wheel[] wheels = _wheels;
        foreach (Wheel wheel in wheels)
        {
            if (wheel.config == null)
            {
                continue;
            }
            int copyCrawlerTrackSpeedIndex = wheel.config.copyCrawlerTrackSpeedIndex;
            if (copyCrawlerTrackSpeedIndex >= 0)
            {
                if (copyCrawlerTrackSpeedIndex >= crawlerTrackMaterials.Count)
                {
                    Assets.ReportError(asset, $"wheel CopyCrawlerTrackSpeedIndex out of bounds (index: {copyCrawlerTrackSpeedIndex} length: {crawlerTrackMaterials.Count})");
                }
                else
                {
                    wheel.copyCrawlerTrack = crawlerTrackMaterials[copyCrawlerTrackSpeedIndex];
                }
            }
        }
    }

    private void UpdateCrawlerTrackTilingMaterials()
    {
        foreach (CrawlerTrackTilingMaterialInstance crawlerTrackMaterial in crawlerTrackMaterials)
        {
            float num = 0f;
            if (isPhysical)
            {
                int num2 = 0;
                Wheel[] wheels = crawlerTrackMaterial.wheels;
                foreach (Wheel wheel in wheels)
                {
                    num += wheel.CalculateWheelSpeed();
                    num2++;
                }
                if (num2 > 0)
                {
                    num /= (float)num2;
                }
            }
            else
            {
                num = ReplicatedForwardVelocity;
            }
            crawlerTrackMaterial.speed = num;
            float num3 = num * Time.deltaTime;
            crawlerTrackMaterial.uvOffset = (crawlerTrackMaterial.uvOffset + num3 * crawlerTrackMaterial.repeatDistance) % 1f;
            crawlerTrackMaterial.material.mainTextureOffset = crawlerTrackMaterial.initialUvPosition + crawlerTrackMaterial.uvDirection * crawlerTrackMaterial.uvOffset;
        }
    }

    private void SetExhaustParticleSystemsRateOverTimeToZero()
    {
        ParticleSystem[] array = exhaustParticleSystems;
        for (int i = 0; i < array.Length; i++)
        {
            ParticleSystem.EmissionModule emission = array[i].emission;
            emission.rateOverTime = 0f;
        }
        isExhaustRateOverTimeZero = true;
    }

    private void ApplyAngularVelocityDamping(float deltaTime)
    {
        float num = base.transform.InverseTransformDirection(rootRigidbody.angularVelocity).z * (0f - asset.rollAngularVelocityDamping) * deltaTime * 50f;
        if (!MathfEx.IsNearlyZero(num, 0.001f))
        {
            rootRigidbody.AddRelativeTorque(0f, 0f, num, ForceMode.Acceleration);
        }
    }

    private void ApplyWheelBalancingForce(float deltaTime)
    {
        Vector3 zero = Vector3.zero;
        int num = 0;
        Wheel[] wheels = _wheels;
        foreach (Wheel wheel in wheels)
        {
            if (wheel.isGrounded)
            {
                zero += wheel.mostRecentGroundHit.normal;
                num++;
            }
        }
        Vector3 direction = zero / num;
        Vector3 vector = base.transform.InverseTransformDirection(direction);
        vector.z = 0f;
        vector = vector.normalized;
        float f = Mathf.Clamp01(1f - Vector3.Dot(Vector3.up, vector));
        f = Mathf.Pow(f, asset.wheelBalancingUprightExponent);
        float num2 = ((vector.x > 0f) ? (-1f) : 1f);
        f *= num2;
        if (!MathfEx.IsNearlyZero(f * asset.wheelBalancingForceMultiplier * deltaTime * 50f, 0.001f))
        {
            rootRigidbody.AddRelativeTorque(0f, 0f, f * asset.wheelBalancingForceMultiplier * deltaTime * 50f);
        }
    }

    private void PlayIgnitionSound()
    {
        if (!Dedicator.IsDedicatedServer && clipAudioSource != null && asset.ignition != null)
        {
            clipAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            clipAudioSource.PlayOneShot(asset.ignition);
        }
    }

    [Obsolete]
    public void tellState(Vector3 newPosition, byte newAngle_X, byte newAngle_Y, byte newAngle_Z, byte newSpeed, byte newPhysicsSpeed, byte newTurn)
    {
        Quaternion newRotation = Quaternion.Euler(MeasurementTool.byteToAngle2(newAngle_X), MeasurementTool.byteToAngle2(newAngle_Y), MeasurementTool.byteToAngle2(newAngle_Z));
        tellState(newPosition, newRotation, (int)newSpeed, (int)newPhysicsSpeed, (int)newTurn, 0f);
    }

    [Obsolete("This override uses the vanilla battery item rather than the equipped battery item.")]
    public void replaceBattery(Player player, byte quality)
    {
        replaceBattery(player, quality, new Guid("098b13be34a7411db7736b7f866ada69"));
    }

    [Obsolete]
    public void safeInit()
    {
        safeInit(Assets.find(EAssetType.VEHICLE, id) as VehicleAsset);
    }

    [Obsolete]
    public void init()
    {
        init(Assets.find(EAssetType.VEHICLE, id) as VehicleAsset);
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
