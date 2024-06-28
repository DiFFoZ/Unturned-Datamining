using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class VehicleAsset : Asset, ISkinableAsset
{
    protected bool _shouldVerifyHash;

    protected string _vehicleName;

    protected float _size2_z;

    protected string _sharedSkinName;

    private Guid _sharedSkinLookupGuid;

    protected ushort _sharedSkinLookupID;

    protected EEngine _engine;

    protected EItemRarity _rarity;

    private GameObject loadedModel;

    /// <summary>
    /// Prevents calling getOrLoad redundantly if asset does not exist.
    /// </summary>
    private bool hasLoadedModel;

    private IDeferredAsset<GameObject> clientModel;

    private IDeferredAsset<GameObject> legacyServerModel;

    protected AudioClip _ignition;

    protected AudioClip _horn;

    protected float _pitchIdle;

    protected float _pitchDrive;

    internal EVehicleEngineSoundType engineSoundType;

    internal RpmEngineSoundConfiguration engineSoundConfiguration;

    protected float _steerMin;

    protected float _steerMax;

    protected float _brake;

    protected float _lift;

    protected ushort _fuelMin;

    protected ushort _fuelMax;

    protected ushort _fuel;

    protected ushort _healthMin;

    protected ushort _healthMax;

    protected ushort _health;

    private Guid _explosionEffectGuid;

    protected ushort _explosion;

    protected bool _hasHeadlights;

    protected bool _hasSirens;

    protected bool _hasHook;

    protected bool _hasZip;

    protected bool _hasBicycle;

    protected bool _hasLockMouse;

    protected bool _hasTraction;

    protected bool _hasSleds;

    protected float _exit;

    protected float _sqrDelta;

    protected float _camFollowDistance;

    protected float _bumperMultiplier;

    protected float _passengerExplosionArmor;

    protected TurretInfo[] _turrets;

    protected Texture2D _albedoBase;

    protected Texture2D _metallicBase;

    protected Texture2D _emissionBase;

    /// <summary>
    /// To non-explosions.
    /// </summary>
    public bool isVulnerable;

    public bool isVulnerableToExplosions;

    /// <summary>
    /// Mega zombie rocks, zombies, animals.
    /// </summary>
    public bool isVulnerableToEnvironment;

    /// <summary>
    /// Crashing into stuff.
    /// </summary>
    public bool isVulnerableToBumper;

    public bool canTiresBeDamaged;

    internal VehicleWheelConfiguration[] wheelConfiguration;

    /// <summary>
    /// Indices of wheels using replicated collider pose (if any).
    /// Null if not configured or no wheels using this feature.
    /// Allows client and server to replicate only the suspension value without other context.
    /// </summary>
    internal int[] replicatedWheelIndices;

    /// <summary>
    /// Indices of wheels with motor torque applied (if any).
    /// Used for engine RPM calculation.
    /// </summary>
    internal int[] poweredWheelIndices;

    internal float reverseGearRatio;

    internal float[] forwardGearRatios;

    /// <summary>
    /// When engine RPM dips below this value shift to the next lower gear if available.
    /// </summary>
    internal float gearShiftDownThresholdRpm;

    /// <summary>
    /// When engine RPM exceeds this value shift to the next higher gear if available.
    /// </summary>
    internal float gearShiftUpThresholdRpm;

    /// <summary>
    /// How long after changing gears before throttle is engaged again.
    /// </summary>
    internal float gearShiftDuration;

    /// <summary>
    /// How long between changing gears to allow another automatic gear change.
    /// </summary>
    internal float gearShiftInterval;

    /// <summary>
    /// Minimum engine RPM.
    /// </summary>
    internal float engineIdleRpm;

    /// <summary>
    /// Maximum engine RPM.
    /// </summary>
    internal float engineMaxRpm;

    /// <summary>
    /// How quickly RPM can increase in RPM/s.
    /// e.g., 1000 will take 2 seconds to go from 2000 to 4000 RPM.
    /// </summary>
    internal float engineRpmIncreaseRate;

    /// <summary>
    /// How quickly RPM can decrease in RPM/s.
    /// e.g., 1000 will take 2 seconds to go from 4000 to 2000 RPM.
    /// </summary>
    internal float engineRpmDecreaseRate;

    /// <summary>
    /// Maximum torque (multiplied by output of torque curve).
    /// </summary>
    internal float engineMaxTorque;

    private static readonly Guid VANILLA_BATTERY_ITEM = new Guid("098b13be34a7411db7736b7f866ada69");

    protected bool _hasCrawler;

    private static CommandLineFlag clLogWheelConfiguration = new CommandLineFlag(defaultValue: false, "-LogVehicleWheelConfigurations");

    public bool shouldVerifyHash => _shouldVerifyHash;

    internal override bool ShouldVerifyHash => _shouldVerifyHash;

    public string vehicleName => _vehicleName;

    public override string FriendlyName => _vehicleName;

    public float size2_z => _size2_z;

    public string sharedSkinName => _sharedSkinName;

    /// <summary>
    /// Please refer to: <seealso cref="M:SDG.Unturned.VehicleAsset.FindSharedSkinVehicleAsset" />
    /// </summary>
    public Guid SharedSkinLookupGuid => _sharedSkinLookupGuid;

    /// <summary>
    /// Please refer to: <seealso cref="M:SDG.Unturned.VehicleAsset.FindSharedSkinVehicleAsset" />
    /// </summary>
    [Obsolete]
    public ushort sharedSkinLookupID => _sharedSkinLookupID;

    public EEngine engine => _engine;

    public EItemRarity rarity => _rarity;

    public AudioClip ignition => _ignition;

    public AudioClip horn => _horn;

    public bool hasHorn { get; protected set; }

    public float pitchIdle => _pitchIdle;

    public float pitchDrive => _pitchDrive;

    /// <summary>
    /// Maximum (negative) velocity to aim for while accelerating backward.
    /// </summary>
    public float TargetReverseVelocity { get; private set; }

    /// <summary>
    /// Maximum speed to aim for while accelerating backward.
    /// </summary>
    public float TargetReverseSpeed => Mathf.Abs(TargetReverseVelocity);

    /// <summary>
    /// Maximum velocity to aim for while accelerating forward.
    /// </summary>
    public float TargetForwardVelocity { get; private set; }

    /// <summary>
    /// Maximum speed to aim for while accelerating forward.
    /// </summary>
    public float TargetForwardSpeed => Mathf.Abs(TargetForwardVelocity);

    /// <summary>
    /// Steering angle range at zero speed.
    /// </summary>
    public float steerMin => _steerMin;

    /// <summary>
    /// Steering angle range at target maximum speed (for the current forward/backward direction).
    /// </summary>
    public float steerMax => _steerMax;

    /// <summary>
    /// Steering angle rotation change in degrees per second.
    /// </summary>
    public float SteeringAngleTurnSpeed { get; private set; }

    public float brake => _brake;

    public float lift => _lift;

    public ushort fuelMin => _fuelMin;

    public ushort fuelMax => _fuelMax;

    public ushort fuel => _fuel;

    public ushort healthMin => _healthMin;

    public ushort healthMax => _healthMax;

    public ushort health => _health;

    public Guid ExplosionEffectGuid => _explosionEffectGuid;

    public ushort explosion
    {
        [Obsolete]
        get
        {
            return _explosion;
        }
    }

    public Vector3 minExplosionForce { get; set; }

    public Vector3 maxExplosionForce { get; set; }

    [Obsolete("Separated into ShouldExplosionCauseDamage and ShouldExplosionBurnMaterials.")]
    public bool isExplosive => !IsExplosionEffectRefNull();

    /// <summary>
    /// If true, explosion will damage nearby entities and kill passengers.
    /// </summary>
    public bool ShouldExplosionCauseDamage { get; protected set; }

    public bool ShouldExplosionBurnMaterials { get; protected set; }

    public bool hasHeadlights => _hasHeadlights;

    public bool hasSirens => _hasSirens;

    public bool hasHook => _hasHook;

    public bool hasZip => _hasZip;

    /// <summary>
    /// When true the bicycle animation is used and extra speed is stamina powered.
    /// Bad way to implement it.
    /// </summary>
    public bool hasBicycle => _hasBicycle;

    /// <summary>
    /// Can this vehicle ever spawn with a charged battery?
    /// Uses game mode battery stats when true, or overrides by preventing battery spawn when false.
    /// </summary>
    public bool canSpawnWithBattery { get; protected set; }

    /// <summary>
    /// Battery charge when first spawning in is multiplied by this [0, 1] number.
    /// </summary>
    public float batterySpawnChargeMultiplier { get; protected set; }

    /// <summary>
    /// Battery decrease per second.
    /// </summary>
    public float batteryBurnRate { get; protected set; }

    /// <summary>
    /// Battery increase per second.
    /// </summary>
    public float batteryChargeRate { get; protected set; }

    public EBatteryMode batteryDriving { get; protected set; }

    public EBatteryMode batteryEmpty { get; protected set; }

    public EBatteryMode batteryHeadlights { get; protected set; }

    public EBatteryMode batterySirens { get; protected set; }

    /// <summary>
    /// Battery item given to the player when a specific battery hasn't been manually
    /// installed yet. Defaults to the vanilla car battery (098b13be34a7411db7736b7f866ada69).
    /// </summary>
    public Guid defaultBatteryGuid { get; protected set; }

    /// <summary>
    /// Fuel decrease per second.
    /// </summary>
    public float fuelBurnRate { get; protected set; }

    public bool isReclined { get; protected set; }

    public bool hasLockMouse => _hasLockMouse;

    public bool hasTraction => _hasTraction;

    public bool hasSleds => _hasSleds;

    public float exit => _exit;

    public float sqrDelta => _sqrDelta;

    /// <summary>
    /// Client sends physics simulation results to server. If upward (+Y) speed exceeds this, mark the move invalid.
    /// </summary>
    public float validSpeedUp { get; protected set; }

    /// <summary>
    /// Client sends physics simulation results to server. If downward (-Y) speed exceeds this, mark the move invalid.
    /// </summary>
    public float validSpeedDown { get; protected set; }

    public float camFollowDistance => _camFollowDistance;

    /// <summary>
    /// Vertical first-person view translation.
    /// </summary>
    public float camDriverOffset { get; protected set; }

    /// <summary>
    /// Vertical first-person view translation.
    /// </summary>
    public float camPassengerOffset { get; protected set; }

    public float bumperMultiplier => _bumperMultiplier;

    public float passengerExplosionArmor => _passengerExplosionArmor;

    public TurretInfo[] turrets => _turrets;

    public Texture albedoBase => _albedoBase;

    public Texture metallicBase => _metallicBase;

    public Texture emissionBase => _emissionBase;

    public bool CanDecay { get; private set; }

    /// <summary>
    /// Can this vehicle be repaired by a seated player?
    /// </summary>
    public bool canRepairWhileSeated { get; protected set; }

    public float childExplosionArmorMultiplier { get; protected set; }

    public float airTurnResponsiveness { get; protected set; }

    public float airSteerMin { get; protected set; }

    public float airSteerMax { get; protected set; }

    public float bicycleAnimSpeed { get; protected set; }

    public float trainTrackOffset { get; protected set; }

    public float trainWheelOffset { get; protected set; }

    public float trainCarLength { get; protected set; }

    public float staminaBoost { get; protected set; }

    public bool useStaminaBoost { get; protected set; }

    public bool isStaminaPowered { get; protected set; }

    public bool isBatteryPowered { get; protected set; }

    /// <summary>
    /// Can mobile barricades e.g. bed or sentry guns be placed on this vehicle?
    /// </summary>
    [Obsolete("Replaced by BuildablePlacementRule")]
    public bool supportsMobileBuildables => BuildablePlacementRule == EVehicleBuildablePlacementRule.AlwaysAllow;

    public EVehicleBuildablePlacementRule BuildablePlacementRule { get; protected set; }

    /// <summary>
    /// Should capsule colliders be added to seat transforms?
    /// Useful to prevent bikes from leaning into walls.
    /// </summary>
    public bool shouldSpawnSeatCapsules { get; protected set; }

    /// <summary>
    /// Can players lock the vehicle to their clan/group?
    /// True by default, but mods want to be able to disable.
    /// </summary>
    public bool canBeLocked { get; protected set; }

    /// <summary>
    /// Can players steal the battery?
    /// </summary>
    public bool canStealBattery { get; protected set; }

    public byte trunkStorage_X { get; set; }

    public byte trunkStorage_Y { get; set; }

    /// <summary>
    /// Spawn table to drop items from on death.
    /// </summary>
    public ushort dropsTableId { get; protected set; }

    /// <summary>
    /// Minimum number of items to drop on death.
    /// </summary>
    public byte dropsMin { get; protected set; }

    /// <summary>
    /// Maximum number of items to drop on death.
    /// </summary>
    public byte dropsMax { get; protected set; }

    /// <summary>
    /// Item ID of compatible tire.
    /// </summary>
    public ushort tireID { get; protected set; }

    internal bool UsesEngineRpmAndGears
    {
        get
        {
            if (forwardGearRatios != null)
            {
                return forwardGearRatios.Length != 0;
            }
            return false;
        }
    }

    /// <summary>
    /// Was a center of mass specified in the .dat?
    /// </summary>
    public bool hasCenterOfMassOverride { get; protected set; }

    /// <summary>
    /// If hasCenterOfMassOverride, use this value.
    /// </summary>
    public Vector3 centerOfMass { get; protected set; }

    public float carjackForceMultiplier { get; protected set; }

    /// <summary>
    /// Multiplier for otherwise not-yet-configurable plane/heli/boat forces.
    /// Nelson 2024-03-06: Required for increasing mass of vehicles without significantly messing with behavior.
    /// </summary>
    public float engineForceMultiplier { get; protected set; }

    /// <summary>
    /// If set, override the wheel collider mass with this value.
    /// </summary>
    public float? wheelColliderMassOverride { get; protected set; }

    public AssetReference<VehiclePhysicsProfileAsset> physicsProfileRef { get; protected set; }

    /// <summary>
    /// Null if vehicle doesn't support paint color.
    /// </summary>
    public PaintableVehicleSection[] PaintableVehicleSections { get; protected set; }

    /// <summary>
    /// Null if vehicle doesn't support paint color.
    /// </summary>
    public List<Color32> DefaultPaintColors { get; protected set; }

    public bool SupportsPaintColor
    {
        get
        {
            if (PaintableVehicleSections != null)
            {
                return PaintableVehicleSections.Length != 0;
            }
            return false;
        }
    }

    /// <summary>
    /// If true, Vehicle Paint items can be used on this vehicle.
    /// Always false if <see cref="P:SDG.Unturned.VehicleAsset.SupportsPaintColor" /> is false.
    ///
    /// Certain vehicles may support paint colors without also being paintable by players. For example, the creator
    /// of a vehicle may want to use color variants without also allowing players to make it bright pink.
    /// </summary>
    public bool IsPaintable { get; protected set; }

    public override EAssetType assetCategory => EAssetType.VEHICLE;

    /// <summary>
    /// Number of tire visuals to rotate with steering wheel.
    /// </summary>
    [Obsolete("Replaced by VehicleWheelConfiguration. Only used for backwards compatibility.")]
    public int numSteeringTires { get; protected set; }

    [Obsolete("Replaced by VehicleWheelConfiguration. Only used for backwards compatibility.")]
    public int[] steeringTireIndices { get; protected set; }

    [Obsolete("Replaced by VehicleWheelConfiguration. Only used for backwards compatibility.")]
    public bool hasCrawler => _hasCrawler;

    [Obsolete("Renamed to TargetReverseVelocity.")]
    public float speedMin => TargetReverseVelocity;

    [Obsolete("Renamed to TargetForwardVelocity.")]
    public float speedMax => TargetForwardVelocity;

    /// <summary>
    /// Supports redirects by VehicleRedirectorAsset.
    ///
    /// "Shared Skins" were implemented when there were several asset variants of each vehicle. For example,
    /// Off_Roader_Orange, Off_Roader_Purple, Off_Roader_Green, etc. Each vehicle had their "shared skin" set to
    /// the same ID, and the skin asset had its target ID set to the shared ID. This isn't as necessary after
    /// merging vanilla vehicle variants, but some mods may rely on it, and it needed GUID support now that the
    /// target vehicle might not have a legacy ID.
    /// </summary>
    public VehicleAsset FindSharedSkinVehicleAsset()
    {
        Asset asset = Assets.FindBaseVehicleAssetByGuidOrLegacyId(_sharedSkinLookupGuid, _sharedSkinLookupID);
        if (asset is VehicleRedirectorAsset { TargetVehicle: var targetVehicle })
        {
            asset = targetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    public GameObject GetOrLoadModel()
    {
        if (!hasLoadedModel)
        {
            hasLoadedModel = true;
            if (legacyServerModel != null)
            {
                loadedModel = legacyServerModel.getOrLoad();
                if (loadedModel == null)
                {
                    loadedModel = clientModel.getOrLoad();
                }
            }
            else
            {
                loadedModel = clientModel.getOrLoad();
            }
        }
        return loadedModel;
    }

    protected void onModelLoaded(GameObject asset)
    {
        Transform transform = asset.transform.Find("Seats");
        if (transform == null)
        {
            Assets.reportError(this, "missing 'Seats' Transform");
        }
        else if (transform.childCount < 1)
        {
            Assets.reportError(this, "empty 'Seats' Transform has zero children");
        }
        Rigidbody component = asset.GetComponent<Rigidbody>();
        if (component == null)
        {
            Assets.reportError(this, "missing root Rigidbody");
        }
        else if (physicsProfileRef.isNull && MathfEx.IsNearlyEqual(component.mass, 1f))
        {
            bool flag = true;
            Transform transform2 = asset.transform.Find("Tires");
            if (transform2 != null)
            {
                for (int i = 0; i < transform2.childCount; i++)
                {
                    Transform child = transform2.GetChild(i);
                    if (!(child == null))
                    {
                        WheelCollider component2 = child.GetComponent<WheelCollider>();
                        if (!(component2 == null) && !MathfEx.IsNearlyEqual(component2.mass, 1f))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
            }
            if (flag)
            {
                switch (engine)
                {
                case EEngine.BOAT:
                    physicsProfileRef = VehiclePhysicsProfileAsset.defaultProfile_Boat;
                    break;
                case EEngine.CAR:
                    physicsProfileRef = VehiclePhysicsProfileAsset.defaultProfile_Car;
                    break;
                case EEngine.HELICOPTER:
                    physicsProfileRef = VehiclePhysicsProfileAsset.defaultProfile_Helicopter;
                    break;
                case EEngine.PLANE:
                    physicsProfileRef = VehiclePhysicsProfileAsset.defaultProfile_Plane;
                    break;
                }
            }
        }
        asset.SetTagIfUntaggedRecursively("Vehicle");
        if (wheelConfiguration == null)
        {
            BuildAutomaticWheelConfiguration(asset);
        }
    }

    /// <summary>
    /// Clip.prefab
    /// </summary>
    protected void OnServerModelLoaded(GameObject asset)
    {
        if (asset == null)
        {
            Assets.reportError(this, "missing \"Clip\" GameObject, loading \"Vehicle\" GameObject instead");
            return;
        }
        _hasHeadlights = true;
        _hasSirens = true;
        _hasHook = true;
        onModelLoaded(asset);
    }

    /// <summary>
    /// Vehicle.prefab
    /// </summary>
    protected void OnClientModelLoaded(GameObject asset)
    {
        if (asset == null)
        {
            Assets.reportError(this, "missing \"Vehicle\" GameObject");
            return;
        }
        AssetValidation.searchGameObjectForErrors(this, asset);
        _hasHeadlights = asset.transform.Find("Headlights") != null;
        _hasSirens = asset.transform.Find("Sirens") != null;
        _hasHook = asset.transform.Find("Hook") != null;
        if (_pitchIdle < 0f)
        {
            _pitchIdle = 0.5f;
            AudioSource component = asset.GetComponent<AudioSource>();
            if (component != null)
            {
                AudioClip clip = component.clip;
                if (clip != null)
                {
                    if (clip.name == "Engine_Large")
                    {
                        _pitchIdle = 0.625f;
                    }
                    else if (clip.name == "Engine_Small")
                    {
                        _pitchIdle = 0.75f;
                    }
                }
            }
        }
        if (_pitchDrive < 0f)
        {
            if (engine == EEngine.HELICOPTER)
            {
                _pitchDrive = 0.03f;
            }
            else if (engine == EEngine.BLIMP)
            {
                _pitchDrive = 0.1f;
            }
            else
            {
                _pitchDrive = 0.05f;
                AudioSource component2 = asset.GetComponent<AudioSource>();
                if (component2 != null)
                {
                    AudioClip clip2 = component2.clip;
                    if (clip2 != null)
                    {
                        if (clip2.name == "Engine_Large")
                        {
                            _pitchDrive = 0.025f;
                        }
                        else if (clip2.name == "Engine_Small")
                        {
                            _pitchDrive = 0.075f;
                        }
                    }
                }
            }
        }
        onModelLoaded(asset);
        if (Dedicator.IsDedicatedServer)
        {
            ServerPrefabUtil.RemoveClientComponents(asset);
        }
    }

    public void DebugDumpWheelConfigurationToStringBuilder(StringBuilder output)
    {
        output.Append(vehicleName);
        if (wheelConfiguration == null || wheelConfiguration.Length < 1)
        {
            output.AppendLine(" wheel configuration(s): N/A");
            return;
        }
        output.AppendLine(" wheel configuration(s):");
        for (int i = 0; i < wheelConfiguration.Length; i++)
        {
            output.Append(i);
            output.AppendLine(":");
            VehicleWheelConfiguration vehicleWheelConfiguration = wheelConfiguration[i];
            output.Append("Wheel collider path: \"");
            output.Append(vehicleWheelConfiguration.wheelColliderPath);
            output.AppendLine("\"");
            output.Append("Is collider steered: ");
            output.Append(vehicleWheelConfiguration.isColliderSteered);
            output.AppendLine();
            output.Append("Is collider powered: ");
            output.Append(vehicleWheelConfiguration.isColliderPowered);
            output.AppendLine();
            output.Append("Model path: \"");
            output.Append(vehicleWheelConfiguration.modelPath);
            output.AppendLine("\"");
            output.Append("Is model steered: ");
            output.Append(vehicleWheelConfiguration.isModelSteered);
            output.AppendLine();
        }
    }

    public string DebugDumpWheelConfigurationToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        DebugDumpWheelConfigurationToStringBuilder(stringBuilder);
        return stringBuilder.ToString();
    }

    private void LogWheelConfigurationDatConversion()
    {
        string message;
        using (StringWriter stringWriter = new StringWriter())
        {
            using DatWriter datWriter = new DatWriter(stringWriter);
            datWriter.WriteListStart("WheelConfigurations");
            VehicleWheelConfiguration[] array = wheelConfiguration;
            foreach (VehicleWheelConfiguration vehicleWheelConfiguration in array)
            {
                datWriter.WriteDictionaryStart();
                datWriter.WriteKeyValue("WheelColliderPath", vehicleWheelConfiguration.wheelColliderPath);
                datWriter.WriteKeyValue("IsColliderSteered", vehicleWheelConfiguration.isColliderSteered);
                datWriter.WriteKeyValue("IsColliderPowered", vehicleWheelConfiguration.isColliderPowered);
                datWriter.WriteKeyValue("ModelPath", vehicleWheelConfiguration.modelPath);
                datWriter.WriteKeyValue("IsModelSteered", vehicleWheelConfiguration.isModelSteered);
                datWriter.WriteDictionaryEnd();
            }
            datWriter.WriteListEnd();
            message = stringWriter.ToString();
        }
        UnturnedLog.info("Converted \"" + FriendlyName + "\" wheel configuration:");
        UnturnedLog.info(message);
    }

    /// <summary>
    /// Nelson 2024-02-28: Prior to the VehicleWheelConfiguration class, most of the wheel configuration was
    /// inferred during InteractableVehicle initialization from the children of the "Tires" and "Wheels" transforms.
    /// Confusingly, "Tires" only contains WheelColliders and "Wheels" only contains the visual models. Rather than
    /// keeping the old behavior in InteractableVehicle alongside the newer more configurable one, we match the old
    /// behavior here to generate an equivalent configuration.
    ///
    /// Note that <see cref="P:SDG.Unturned.VehicleAsset.steeringTireIndices" /> must be initialized before this is called (by loading model).
    /// </summary>
    private void BuildAutomaticWheelConfiguration(GameObject vehicleGameObject)
    {
        Transform transform = vehicleGameObject.transform;
        List<VehicleWheelConfiguration> list = new List<VehicleWheelConfiguration>();
        Transform transform2 = transform.Find("Tires");
        if (transform2 != null)
        {
            for (int i = 0; i < transform2.childCount; i++)
            {
                string text = "Tire_" + i;
                Transform transform3 = transform2.Find(text);
                if (transform3 == null)
                {
                    Assets.reportError(this, "missing \"{0}\" Transform", text);
                    continue;
                }
                if (transform3.GetComponent<WheelCollider>() == null)
                {
                    Assets.reportError(this, "missing \"{0}\" WheelCollider", text);
                    continue;
                }
                VehicleWheelConfiguration vehicleWheelConfiguration = new VehicleWheelConfiguration();
                vehicleWheelConfiguration.wasAutomaticallyGenerated = true;
                vehicleWheelConfiguration.wheelColliderPath = "Tires/" + text;
                vehicleWheelConfiguration.isColliderSteered = i < 2;
                vehicleWheelConfiguration.isColliderPowered = i >= transform2.childCount - 2;
                list.Add(vehicleWheelConfiguration);
            }
        }
        Transform transform4 = transform.Find("Wheels");
        if (transform4 != null)
        {
            foreach (VehicleWheelConfiguration item in list)
            {
                Transform transform5 = transform.Find(item.wheelColliderPath);
                if (transform5 == null)
                {
                    continue;
                }
                int num = -1;
                float num2 = 16f;
                for (int j = 0; j < transform4.childCount; j++)
                {
                    Transform child = transform4.GetChild(j);
                    float sqrMagnitude = (transform5.position - child.position).sqrMagnitude;
                    if (sqrMagnitude < num2)
                    {
                        num = j;
                        num2 = sqrMagnitude;
                    }
                }
                if (num == -1)
                {
                    continue;
                }
                Transform transform6 = transform4.GetChild(num);
                if (transform6.childCount < 1)
                {
                    Transform transform7 = transform.FindChildRecursive("Wheel_" + num);
                    if (transform7 != null)
                    {
                        transform6 = transform7;
                    }
                }
                string text2 = transform6.name;
                Transform parent = transform6.parent;
                while (parent != transform)
                {
                    text2 = parent.name + "/" + text2;
                    parent = parent.parent;
                }
                item.modelPath = text2;
            }
            foreach (Transform item2 in transform4)
            {
                if (item2.childCount < 1)
                {
                    continue;
                }
                bool flag = false;
                foreach (VehicleWheelConfiguration item3 in list)
                {
                    if (!string.IsNullOrEmpty(item3.modelPath))
                    {
                        Transform transform9 = transform.Find(item3.modelPath);
                        if (!(transform9 == null) && transform9 == item2)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (!flag)
                {
                    VehicleWheelConfiguration vehicleWheelConfiguration2 = new VehicleWheelConfiguration();
                    vehicleWheelConfiguration2.wasAutomaticallyGenerated = true;
                    vehicleWheelConfiguration2.modelPath = "Wheels/" + item2.name;
                    list.Add(vehicleWheelConfiguration2);
                }
            }
            if (steeringTireIndices != null)
            {
                int[] array = steeringTireIndices;
                for (int k = 0; k < array.Length; k++)
                {
                    int num3 = array[k];
                    string n = "Wheel_" + num3;
                    Transform transform10 = transform4.Find(n);
                    if (transform10 == null)
                    {
                        transform10 = transform.FindChildRecursive(n);
                        if (transform10 == null && num3 < transform4.childCount)
                        {
                            transform10 = transform4.GetChild(num3);
                        }
                    }
                    if (transform10 == null)
                    {
                        continue;
                    }
                    VehicleWheelConfiguration vehicleWheelConfiguration3 = null;
                    foreach (VehicleWheelConfiguration item4 in list)
                    {
                        if (!string.IsNullOrEmpty(item4.modelPath))
                        {
                            Transform transform11 = transform.Find(item4.modelPath);
                            if (!(transform11 == null) && transform11 == transform10)
                            {
                                vehicleWheelConfiguration3 = item4;
                                break;
                            }
                        }
                    }
                    if (vehicleWheelConfiguration3 != null)
                    {
                        vehicleWheelConfiguration3.isModelSteered = true;
                    }
                    else
                    {
                        Assets.reportError(this, "unable to match physical tire with steering tire model {0}", num3);
                    }
                }
            }
        }
        wheelConfiguration = list.ToArray();
        if ((bool)clLogWheelConfiguration)
        {
            LogWheelConfigurationDatConversion();
        }
    }

    public bool IsExplosionEffectRefNull()
    {
        if (_explosion == 0)
        {
            return _explosionEffectGuid.IsEmpty();
        }
        return false;
    }

    public EffectAsset FindExplosionEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(_explosionEffectGuid, _explosion);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (id < 200 && !base.OriginAllowsVanillaLegacyId && !data.ContainsKey("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 200");
        }
        _vehicleName = localization.format("Name");
        _pitchIdle = data.ParseFloat("Pitch_Idle", -1f);
        _pitchDrive = data.ParseFloat("Pitch_Drive", -1f);
        _engine = data.ParseEnum("Engine", EEngine.CAR);
        physicsProfileRef = data.readAssetReference<VehiclePhysicsProfileAsset>("Physics_Profile");
        int defaultValue = ((engine != 0) ? 1 : 2);
        _hasCrawler = data.ContainsKey("Crawler");
        if (hasCrawler)
        {
            defaultValue = 0;
        }
        numSteeringTires = data.ParseInt32("Num_Steering_Tires", defaultValue);
        steeringTireIndices = new int[numSteeringTires];
        for (int i = 0; i < numSteeringTires; i++)
        {
            steeringTireIndices[i] = data.ParseInt32("Steering_Tire_" + i, i);
        }
        if (Dedicator.IsDedicatedServer && data.ParseBool("Has_Clip_Prefab", defaultValue: true))
        {
            bundle.loadDeferred("Clip", out legacyServerModel, (LoadedAssetDeferredCallback<GameObject>)OnServerModelLoaded);
        }
        bundle.loadDeferred("Vehicle", out clientModel, (LoadedAssetDeferredCallback<GameObject>)OnClientModelLoaded);
        _size2_z = data.ParseFloat("Size2_Z");
        _sharedSkinName = data.GetString("Shared_Skin_Name");
        if (data.ContainsKey("Shared_Skin_Lookup_ID"))
        {
            _sharedSkinLookupID = data.ParseGuidOrLegacyId("Shared_Skin_Lookup_ID", out _sharedSkinLookupGuid);
        }
        else
        {
            _sharedSkinLookupGuid = GUID;
            _sharedSkinLookupID = id;
        }
        if (data.ContainsKey("Rarity"))
        {
            _rarity = (EItemRarity)Enum.Parse(typeof(EItemRarity), data.GetString("Rarity"), ignoreCase: true);
        }
        else
        {
            _rarity = EItemRarity.COMMON;
        }
        _hasZip = data.ContainsKey("Zip");
        _hasBicycle = data.ContainsKey("Bicycle");
        isReclined = data.ContainsKey("Reclined");
        _hasLockMouse = data.ContainsKey("LockMouse");
        _hasTraction = data.ContainsKey("Traction");
        _hasSleds = data.ContainsKey("Sleds");
        canSpawnWithBattery = !data.ContainsKey("Cannot_Spawn_With_Battery");
        if (data.ContainsKey("Battery_Spawn_Charge_Multiplier"))
        {
            batterySpawnChargeMultiplier = data.ParseFloat("Battery_Spawn_Charge_Multiplier");
        }
        else
        {
            batterySpawnChargeMultiplier = 1f;
        }
        if (data.ContainsKey("Battery_Burn_Rate"))
        {
            batteryBurnRate = data.ParseFloat("Battery_Burn_Rate");
        }
        else
        {
            batteryBurnRate = 20f;
        }
        if (data.ContainsKey("Battery_Charge_Rate"))
        {
            batteryChargeRate = data.ParseFloat("Battery_Charge_Rate");
        }
        else
        {
            batteryChargeRate = 20f;
        }
        batteryDriving = data.ParseEnum("BatteryMode_Driving", EBatteryMode.Charge);
        batteryEmpty = data.ParseEnum("BatteryMode_Empty", EBatteryMode.None);
        batteryHeadlights = data.ParseEnum("BatteryMode_Headlights", EBatteryMode.Burn);
        batterySirens = data.ParseEnum("BatteryMode_Sirens", EBatteryMode.Burn);
        defaultBatteryGuid = data.ParseGuid("Default_Battery", VANILLA_BATTERY_ITEM);
        float defaultValue2 = ((engine == EEngine.CAR) ? 2.05f : 4.2f);
        fuelBurnRate = data.ParseFloat("Fuel_Burn_Rate", defaultValue2);
        _ignition = LoadRedirectableAsset<AudioClip>(bundle, "Ignition", data, "IgnitionAudioClip");
        _horn = LoadRedirectableAsset<AudioClip>(bundle, "Horn", data, "HornAudioClip");
        hasHorn = data.ParseBool("Has_Horn", _horn != null);
        TargetReverseVelocity = data.ParseFloat("Speed_Min");
        TargetForwardVelocity = data.ParseFloat("Speed_Max");
        if (engine != EEngine.TRAIN)
        {
            TargetForwardVelocity *= 1.25f;
        }
        _steerMin = data.ParseFloat("Steer_Min");
        _steerMax = data.ParseFloat("Steer_Max") * 0.75f;
        SteeringAngleTurnSpeed = data.ParseFloat("Steering_Angle_Turn_Speed", _steerMax * 5f);
        _brake = data.ParseFloat("Brake");
        _lift = data.ParseFloat("Lift");
        _fuelMin = data.ParseUInt16("Fuel_Min", 0);
        _fuelMax = data.ParseUInt16("Fuel_Max", 0);
        _fuel = data.ParseUInt16("Fuel", 0);
        _healthMin = data.ParseUInt16("Health_Min", 0);
        _healthMax = data.ParseUInt16("Health_Max", 0);
        _health = data.ParseUInt16("Health", 0);
        _explosion = data.ParseGuidOrLegacyId("Explosion", out _explosionEffectGuid);
        bool defaultValue3 = !IsExplosionEffectRefNull();
        ShouldExplosionCauseDamage = data.ParseBool("ShouldExplosionCauseDamage", defaultValue3);
        ShouldExplosionBurnMaterials = data.ParseBool("ShouldExplosionBurnMaterials", defaultValue3);
        float num = data.ParseFloat("Explosion_Force_Multiplier", 1f);
        if (data.TryParseVector3("Explosion_Min_Force", out var value))
        {
            minExplosionForce = value * num;
        }
        else if (data.ContainsKey("Explosion_Min_Force_Y"))
        {
            minExplosionForce = data.LegacyParseVector3("Explosion_Min_Force") * num;
        }
        else
        {
            minExplosionForce = new Vector3(0f, 1024f * num, 0f);
        }
        if (data.TryParseVector3("Explosion_Max_Force", out var value2))
        {
            maxExplosionForce = value2 * num;
        }
        else if (data.ContainsKey("Explosion_Max_Force_Y"))
        {
            maxExplosionForce = data.LegacyParseVector3("Explosion_Max_Force") * num;
        }
        else
        {
            maxExplosionForce = new Vector3(0f, 1024f * num, 0f);
        }
        if (data.ContainsKey("Exit"))
        {
            _exit = data.ParseFloat("Exit");
        }
        else
        {
            _exit = 2f;
        }
        if (data.ContainsKey("Cam_Follow_Distance"))
        {
            _camFollowDistance = data.ParseFloat("Cam_Follow_Distance");
        }
        else
        {
            _camFollowDistance = 5.5f;
        }
        camDriverOffset = data.ParseFloat("Cam_Driver_Offset");
        camPassengerOffset = data.ParseFloat("Cam_Passenger_Offset");
        if (data.ContainsKey("Bumper_Multiplier"))
        {
            _bumperMultiplier = data.ParseFloat("Bumper_Multiplier");
        }
        else
        {
            _bumperMultiplier = 1f;
        }
        if (data.ContainsKey("Passenger_Explosion_Armor"))
        {
            _passengerExplosionArmor = data.ParseFloat("Passenger_Explosion_Armor");
        }
        else
        {
            _passengerExplosionArmor = 1f;
        }
        if (engine == EEngine.HELICOPTER || engine == EEngine.BLIMP)
        {
            _sqrDelta = MathfEx.Square(TargetForwardVelocity * 0.125f);
        }
        else
        {
            _sqrDelta = MathfEx.Square(TargetForwardVelocity * 0.1f);
        }
        if (data.ContainsKey("Valid_Speed_Horizontal"))
        {
            float x = data.ParseFloat("Valid_Speed_Horizontal") * PlayerInput.RATE;
            _sqrDelta = MathfEx.Square(x);
        }
        float defaultValue4;
        float defaultValue5;
        switch (engine)
        {
        case EEngine.CAR:
            defaultValue4 = 12.5f;
            defaultValue5 = 25f;
            break;
        case EEngine.BOAT:
            defaultValue4 = 3.25f;
            defaultValue5 = 25f;
            break;
        default:
            defaultValue4 = 100f;
            defaultValue5 = 100f;
            break;
        }
        validSpeedUp = data.ParseFloat("Valid_Speed_Up", defaultValue4);
        validSpeedDown = data.ParseFloat("Valid_Speed_Down", defaultValue5);
        _turrets = new TurretInfo[data.ParseUInt8("Turrets", 0)];
        for (byte b = 0; b < turrets.Length; b++)
        {
            TurretInfo turretInfo = new TurretInfo();
            turretInfo.seatIndex = data.ParseUInt8("Turret_" + b + "_Seat_Index", 0);
            turretInfo.itemID = data.ParseUInt16("Turret_" + b + "_Item_ID", 0);
            turretInfo.yawMin = data.ParseFloat("Turret_" + b + "_Yaw_Min");
            turretInfo.yawMax = data.ParseFloat("Turret_" + b + "_Yaw_Max");
            turretInfo.pitchMin = data.ParseFloat("Turret_" + b + "_Pitch_Min");
            turretInfo.pitchMax = data.ParseFloat("Turret_" + b + "_Pitch_Max");
            turretInfo.useAimCamera = !data.ContainsKey("Turret_" + b + "_Ignore_Aim_Camera");
            _turrets[b] = turretInfo;
        }
        isVulnerable = !data.ContainsKey("Invulnerable");
        isVulnerableToExplosions = !data.ContainsKey("Explosions_Invulnerable");
        isVulnerableToEnvironment = !data.ContainsKey("Environment_Invulnerable");
        isVulnerableToBumper = !data.ContainsKey("Bumper_Invulnerable");
        canTiresBeDamaged = !data.ContainsKey("Tires_Invulnerable");
        canRepairWhileSeated = data.ParseBool("Can_Repair_While_Seated");
        childExplosionArmorMultiplier = data.ParseFloat("Child_Explosion_Armor_Multiplier", 0.2f);
        if (data.ContainsKey("Air_Turn_Responsiveness"))
        {
            airTurnResponsiveness = data.ParseFloat("Air_Turn_Responsiveness");
        }
        else
        {
            airTurnResponsiveness = 2f;
        }
        if (data.ContainsKey("Air_Steer_Min"))
        {
            airSteerMin = data.ParseFloat("Air_Steer_Min");
        }
        else
        {
            airSteerMin = steerMin;
        }
        if (data.ContainsKey("Air_Steer_Max"))
        {
            airSteerMax = data.ParseFloat("Air_Steer_Max");
        }
        else
        {
            airSteerMax = steerMax;
        }
        bicycleAnimSpeed = data.ParseFloat("Bicycle_Anim_Speed");
        staminaBoost = data.ParseFloat("Stamina_Boost");
        useStaminaBoost = data.ContainsKey("Stamina_Boost");
        isStaminaPowered = data.ContainsKey("Stamina_Powered");
        isBatteryPowered = data.ContainsKey("Battery_Powered");
        if (data.TryParseEnum<EVehicleBuildablePlacementRule>("Buildable_Placement_Rule", out var value3))
        {
            BuildablePlacementRule = value3;
        }
        else if (data.ContainsKey("Supports_Mobile_Buildables"))
        {
            BuildablePlacementRule = EVehicleBuildablePlacementRule.AlwaysAllow;
        }
        else
        {
            BuildablePlacementRule = EVehicleBuildablePlacementRule.None;
        }
        shouldSpawnSeatCapsules = data.ParseBool("Should_Spawn_Seat_Capsules");
        canBeLocked = data.ParseBool("Can_Be_Locked", defaultValue: true);
        canStealBattery = data.ParseBool("Can_Steal_Battery", defaultValue: true);
        trunkStorage_X = data.ParseUInt8("Trunk_Storage_X", 0);
        trunkStorage_Y = data.ParseUInt8("Trunk_Storage_Y", 0);
        dropsTableId = data.ParseUInt16("Drops_Table_ID", 962);
        dropsMin = data.ParseUInt8("Drops_Min", 3);
        dropsMax = data.ParseUInt8("Drops_Max", 7);
        tireID = data.ParseUInt16("Tire_ID", 1451);
        hasCenterOfMassOverride = data.ParseBool("Override_Center_Of_Mass");
        if (hasCenterOfMassOverride)
        {
            centerOfMass = data.LegacyParseVector3("Center_Of_Mass");
        }
        carjackForceMultiplier = data.ParseFloat("Carjack_Force_Multiplier", 1f);
        engineForceMultiplier = data.ParseFloat("Engine_Force_Multiplier", 1f);
        if (data.ContainsKey("Wheel_Collider_Mass_Override"))
        {
            wheelColliderMassOverride = data.ParseFloat("Wheel_Collider_Mass_Override", 1f);
        }
        else
        {
            wheelColliderMassOverride = null;
        }
        trainTrackOffset = data.ParseFloat("Train_Track_Offset");
        trainWheelOffset = data.ParseFloat("Train_Wheel_Offset");
        trainCarLength = data.ParseFloat("Train_Car_Length");
        _shouldVerifyHash = !data.ContainsKey("Bypass_Hash_Verification");
        if (!Dedicator.IsDedicatedServer && id < 2000)
        {
            _albedoBase = bundle.load<Texture2D>("Albedo_Base");
            _metallicBase = bundle.load<Texture2D>("Metallic_Base");
            _emissionBase = bundle.load<Texture2D>("Emission_Base");
        }
        CanDecay = engine != EEngine.TRAIN && (isVulnerable | isVulnerableToExplosions | isVulnerableToEnvironment | isVulnerableToBumper);
        PaintableVehicleSections = data.ParseArrayOfStructs<PaintableVehicleSection>("PaintableSections");
        if (SupportsPaintColor)
        {
            IsPaintable = data.ParseBool("IsPaintable", defaultValue: true);
        }
        else
        {
            IsPaintable = false;
        }
        if (data.TryGetList("DefaultPaintColors", out var node))
        {
            DefaultPaintColors = new List<Color32>(node.Count);
            foreach (IDatNode item in node)
            {
                if (item is DatValue node2 && node2.TryParseColor32RGB(out var value4))
                {
                    DefaultPaintColors.Add(value4);
                }
            }
        }
        if (data.TryGetList("WheelConfigurations", out var node3))
        {
            List<VehicleWheelConfiguration> list = new List<VehicleWheelConfiguration>();
            List<int> list2 = new List<int>();
            List<int> list3 = new List<int>();
            foreach (IDatNode item2 in node3)
            {
                VehicleWheelConfiguration vehicleWheelConfiguration = new VehicleWheelConfiguration();
                if (vehicleWheelConfiguration.TryParse(item2))
                {
                    if (vehicleWheelConfiguration.modelUseColliderPose)
                    {
                        int count = list.Count;
                        list2.Add(count);
                    }
                    if (vehicleWheelConfiguration.isColliderPowered)
                    {
                        int count2 = list.Count;
                        list3.Add(count2);
                    }
                    list.Add(vehicleWheelConfiguration);
                }
                else
                {
                    Assets.reportError("Unable to parse entry in WheelConfigurations list: " + item2.DebugDumpToString());
                }
            }
            wheelConfiguration = list.ToArray();
            if (list2.Count > 0)
            {
                replicatedWheelIndices = list2.ToArray();
            }
            if (list3.Count > 0)
            {
                poweredWheelIndices = list3.ToArray();
            }
        }
        reverseGearRatio = data.ParseFloat("ReverseGearRatio", 1f);
        if (data.TryGetList("ForwardGearRatios", out var node4))
        {
            List<float> list4 = new List<float>();
            foreach (IDatNode item3 in node4)
            {
                if (item3 is DatValue datValue && datValue.TryParseFloat(out var value5))
                {
                    list4.Add(value5);
                }
            }
            if (list4.Count > 0)
            {
                forwardGearRatios = list4.ToArray();
            }
        }
        gearShiftDownThresholdRpm = data.ParseFloat("GearShift_DownThresholdRPM", 1500f);
        gearShiftUpThresholdRpm = data.ParseFloat("GearShift_UpThresholdRPM", 5500f);
        gearShiftDuration = data.ParseFloat("GearShift_Duration", 0.5f);
        gearShiftInterval = data.ParseFloat("GearShift_Interval", 1f);
        engineIdleRpm = data.ParseFloat("EngineIdleRPM", 1000f);
        engineMaxRpm = data.ParseFloat("EngineMaxRPM", 7000f);
        engineRpmIncreaseRate = data.ParseFloat("EngineRPM_IncreaseRate", 10000f);
        engineRpmDecreaseRate = data.ParseFloat("EngineRPM_DecreaseRate", 10000f);
        engineMaxTorque = data.ParseFloat("EngineMaxTorque", 1f);
        engineSoundType = data.ParseEnum("EngineSound_Type", EVehicleEngineSoundType.Legacy);
        if (engineSoundType == EVehicleEngineSoundType.EngineRPMSimple)
        {
            engineSoundConfiguration = new RpmEngineSoundConfiguration();
            if (data.TryGetDictionary("EngineSound", out var node5))
            {
                engineSoundConfiguration.TryParse(node5);
            }
        }
        if (UsesEngineRpmAndGears && (bool)Assets.shouldValidateAssets)
        {
            GameObject orLoadModel = GetOrLoadModel();
            if (orLoadModel != null && orLoadModel.GetComponent<EngineCurvesComponent>() == null)
            {
                Assets.reportError(this, "needs EngineCurvesComponent on vehicle prefab for engine RPM and gearbox to work properly");
            }
        }
    }
}
