using System;
using System.Collections.Generic;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Framework.Water;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class PlayerMovement : PlayerCaller
{
    public static readonly float HEIGHT_STAND = 2f;

    public static readonly float HEIGHT_CROUCH = 1.2f;

    public static readonly float HEIGHT_PRONE = 0.8f;

    public Landed onLanded;

    public Seated onSeated;

    public VehicleUpdated onVehicleUpdated;

    public SafetyUpdated onSafetyUpdated;

    public RadiationUpdated onRadiationUpdated;

    public PurchaseUpdated onPurchaseUpdated;

    public PlayerRegionUpdated onRegionUpdated;

    public PlayerBoundUpdated onBoundUpdated;

    private static readonly float SPEED_CLIMB = 4.5f;

    private static readonly float SPEED_SWIM = 3f;

    private static readonly float SPEED_SPRINT = 7f;

    private static readonly float SPEED_STAND = 4.5f;

    private static readonly float SPEED_CROUCH = 2.5f;

    private static readonly float SPEED_PRONE = 1.5f;

    private static readonly float JUMP = 7f;

    private static readonly float SWIM = 3f;

    [Obsolete("Was current value of interpolated aiming speed multiplier.")]
    public float _multiplier;

    [Obsolete("Was target value of interpolated aiming speed multiplier.")]
    public float multiplier;

    public float itemGravityMultiplier;

    public float pluginGravityMultiplier;

    public float pluginSpeedMultiplier;

    public float pluginJumpMultiplier = 1f;

    private float lastFootstep;

    private bool _isGrounded;

    private bool _isSafe;

    public SafezoneNode isSafeInfo;

    private bool _isRadiated;

    private HordePurchaseVolume _purchaseNode;

    [Obsolete]
    public bool inRain;

    public bool inSnow;

    private string materialName;

    private bool materialIsWater;

    public RaycastHit ground;

    internal EPlayerHeight height;

    private bool wasSizeAppliedYet;

    private bool _isMoving;

    private Vector3 _move;

    private byte _region_x;

    private byte _region_y;

    private byte _bound;

    private byte _nav;

    private byte updateRegionOld_X;

    private byte updateRegionOld_Y;

    private byte updateRegionNew_X;

    private byte updateRegionNew_Y;

    private byte updateRegionIndex;

    private LoadedRegion[,] _loadedRegions;

    private LoadedBound[] _loadedBounds;

    internal Vector3 velocity;

    public Vector3 pendingLaunchVelocity;

    private Vector3 lastUpdatePos;

    public PitchYawSnapshotInfo snapshot;

    private NetworkSnapshotBuffer<PitchYawSnapshotInfo> nsb;

    private byte _horizontal;

    private byte _vertical;

    private int warp_x;

    private int warp_y;

    internal int input_x;

    internal int input_y;

    private bool _jump;

    [Obsolete]
    public bool isAllowed;

    [Obsolete]
    public bool isUpdated;

    public List<PlayerStateUpdate> updates;

    public bool canAddSimulationResultsToUpdates;

    public bool bypassUndergroundWhitelist;

    internal bool hasPendingVehicleChange;

    private InteractableVehicle pendingVehicle;

    private byte pendingSeatIndex;

    private Transform pendingSeatTransform;

    private Vector3 pendingSeatPosition;

    private byte pendingSeatAngle;

    private Vector3 lastStatPos;

    private float lastStatTime;

    private InteractableVehicle vehicle;

    private byte seat;

    private static readonly ClientInstanceMethod<float> SendPluginGravityMultiplier = ClientInstanceMethod<float>.Get(typeof(PlayerMovement), "ReceivePluginGravityMultiplier");

    private static readonly ClientInstanceMethod<float> SendPluginJumpMultiplier = ClientInstanceMethod<float>.Get(typeof(PlayerMovement), "ReceivePluginJumpMultiplier");

    private static readonly ClientInstanceMethod<float> SendPluginSpeedMultiplier = ClientInstanceMethod<float>.Get(typeof(PlayerMovement), "ReceivePluginSpeedMultiplier");

    private ControllerColliderHit mostRecentControllerColliderHit;

    public static bool forceTrustClient
    {
        get
        {
            return GameplayConfigData._forceTrustClient;
        }
        set
        {
            GameplayConfigData._forceTrustClient.value = value;
            UnturnedLog.info("Set ForceTrustClient to: " + forceTrustClient);
        }
    }

    public CharacterController controller { get; protected set; }

    public float totalGravityMultiplier => itemGravityMultiplier * pluginGravityMultiplier;

    public float totalSpeedMultiplier => pluginSpeedMultiplier * base.player.clothing.speedMultiplier * (base.player.equipment.asset?.equipableMovementSpeedMultiplier ?? 1f) * (base.player.equipment.useable?.movementSpeedMultiplier ?? 1f);

    [Obsolete]
    public LandscapeHoleVolume landscapeHoleVolume => null;

    internal bool CanEnterTeleporter
    {
        get
        {
            if (base.player.life.IsAlive)
            {
                return getVehicle() == null;
            }
            return false;
        }
    }

    public bool isGrounded => _isGrounded;

    public bool isSafe
    {
        get
        {
            return _isSafe;
        }
        set
        {
            _isSafe = value;
        }
    }

    public bool isRadiated
    {
        get
        {
            return _isRadiated;
        }
        set
        {
            _isRadiated = value;
        }
    }

    public IDeadzoneNode ActiveDeadzone { get; private set; }

    public HordePurchaseVolume purchaseNode
    {
        get
        {
            return _purchaseNode;
        }
        set
        {
            _purchaseNode = value;
        }
    }

    public IAmbianceNode effectNode { get; private set; }

    public uint WeatherMask { get; protected set; }

    public bool isMoving => _isMoving;

    public float speed
    {
        get
        {
            if (base.player.stance.stance == EPlayerStance.SWIM)
            {
                return SPEED_SWIM * (1f + base.player.skills.mastery(0, 5) * 0.25f) * totalSpeedMultiplier;
            }
            float num = 1f + base.player.skills.mastery(0, 4) * 0.25f;
            if (base.player.stance.stance == EPlayerStance.CLIMB)
            {
                return SPEED_CLIMB * num * totalSpeedMultiplier;
            }
            if (base.player.stance.stance == EPlayerStance.SPRINT)
            {
                return SPEED_SPRINT * num * totalSpeedMultiplier;
            }
            if (base.player.stance.stance == EPlayerStance.STAND)
            {
                return SPEED_STAND * num * totalSpeedMultiplier;
            }
            if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                return SPEED_CROUCH * num * totalSpeedMultiplier;
            }
            if (base.player.stance.stance == EPlayerStance.PRONE)
            {
                return SPEED_PRONE * num * totalSpeedMultiplier;
            }
            return 0f;
        }
    }

    public Vector3 move => _move;

    public byte region_x => _region_x;

    public byte region_y => _region_y;

    public byte bound => _bound;

    public byte nav => _nav;

    public LoadedRegion[,] loadedRegions => _loadedRegions;

    public LoadedBound[] loadedBounds => _loadedBounds;

    public float fall => velocity.y;

    [Obsolete]
    public Vector3 real => base.transform.position;

    public byte horizontal => _horizontal;

    public byte vertical => _vertical;

    public bool jump => _jump;

    public event PlayerNavChanged PlayerNavChanged;

    private void TriggerPlayerNavChanged(byte oldNav, byte newNav)
    {
        if (this.PlayerNavChanged != null)
        {
            this.PlayerNavChanged(this, oldNav, newNav);
        }
    }

    private void DoTeleport(Transform inputTransform, Transform outputTransform)
    {
        Vector3 position = inputTransform.InverseTransformPoint(base.transform.position);
        Quaternion localRotation = inputTransform.InverseTransformRotation(base.transform.rotation);
        base.transform.position = outputTransform.TransformPoint(position);
        float y = outputTransform.TransformRotation(localRotation).eulerAngles.y;
        base.player.look.TeleportYaw(y);
        Vector3 position2 = inputTransform.InverseTransformPoint(lastUpdatePos);
        lastUpdatePos = outputTransform.TransformPoint(position2);
        base.player.PostTeleport();
    }

    internal void EnterCollisionTeleporter(CollisionTeleporter teleporter)
    {
        Transform inputTransform = teleporter.transform;
        Transform destinationTransform = teleporter.DestinationTransform;
        DoTeleport(inputTransform, destinationTransform);
    }

    internal void EnterTeleporterVolume(TeleporterEntranceVolume entrance, TeleporterExitVolume exit)
    {
        Transform inputTransform = entrance.transform;
        Transform outputTransform = exit.transform;
        DoTeleport(inputTransform, outputTransform);
    }

    public void setSize(EPlayerHeight newHeight)
    {
        if (newHeight != height)
        {
            height = newHeight;
            applySize();
        }
    }

    private void applySize()
    {
        float num = height switch
        {
            EPlayerHeight.STAND => HEIGHT_STAND, 
            EPlayerHeight.CROUCH => HEIGHT_CROUCH, 
            EPlayerHeight.PRONE => HEIGHT_PRONE, 
            _ => 2f, 
        };
        if ((base.channel.isOwner || Provider.isServer) && controller != null)
        {
            controller.height = num;
            controller.center = new Vector3(0f, num / 2f, 0f);
            if (wasSizeAppliedYet)
            {
                base.transform.localPosition += new Vector3(0f, 0.02f, 0f);
            }
            wasSizeAppliedYet = true;
        }
    }

    public InteractableVehicle getVehicle()
    {
        return vehicle;
    }

    public Passenger getVehicleSeat()
    {
        if (!(vehicle != null) || vehicle.passengers == null || seat >= vehicle.passengers.Length)
        {
            return null;
        }
        return vehicle.passengers[seat];
    }

    public byte getSeat()
    {
        return seat;
    }

    private void updateVehicle()
    {
        InteractableVehicle interactableVehicle = vehicle;
        vehicle = pendingVehicle;
        seat = pendingSeatIndex;
        bool flag = vehicle != null && seat == 0;
        if (vehicle == null)
        {
            base.player.transform.parent = pendingSeatTransform;
            base.player.ReceiveTeleport(pendingSeatPosition, pendingSeatAngle);
        }
        if (base.channel.isOwner)
        {
            if (flag && Level.info != null && Level.info.name.ToLower() != "tutorial" && Provider.provider.achievementsService.getAchievement("Wheel", out var has) && !has)
            {
                Provider.provider.achievementsService.setAchievement("Wheel");
            }
            if (vehicle != null)
            {
                PlayerUI.disableDot();
                if (base.player.equipment.useable is UseableGun useableGun)
                {
                    useableGun.UpdateCrosshairEnabled();
                }
            }
            else if (base.player.equipment.useable is UseableGun useableGun2)
            {
                useableGun2.UpdateCrosshairEnabled();
            }
            else
            {
                PlayerUI.enableDot();
            }
        }
        if (base.channel.isOwner || Provider.isServer)
        {
            controller.enabled = vehicle == null;
            if (vehicle != null)
            {
                if (flag)
                {
                    base.player.stance.checkStance(EPlayerStance.DRIVING);
                }
                else
                {
                    base.player.stance.checkStance(EPlayerStance.SITTING);
                }
            }
            else
            {
                base.player.stance.checkStance(EPlayerStance.STAND);
            }
        }
        if (base.channel.isOwner)
        {
            onSeated?.Invoke(flag, vehicle != null, interactableVehicle != null, interactableVehicle, vehicle);
            if (flag && onVehicleUpdated != null)
            {
                vehicle.getDisplayFuel(out var currentFuel, out var maxFuel);
                onVehicleUpdated(!vehicle.isUnderwater && !vehicle.isDead, currentFuel, maxFuel, vehicle.spedometer, vehicle.asset.speedMin, vehicle.asset.speedMax, vehicle.health, vehicle.asset.health, vehicle.batteryCharge);
            }
            if (vehicle != null)
            {
                if (flag)
                {
                    if (interactableVehicle == null)
                    {
                        PlayerUI.message(EPlayerMessage.VEHICLE_EXIT, "");
                    }
                    else
                    {
                        PlayerUI.message(EPlayerMessage.VEHICLE_SWAP, "");
                    }
                }
                else
                {
                    PlayerUI.message(EPlayerMessage.VEHICLE_SWAP, "");
                }
            }
        }
        if (vehicle != null)
        {
            base.player.transform.parent = pendingSeatTransform;
            base.player.transform.localPosition = pendingSeatPosition;
            base.player.transform.localRotation = Quaternion.identity;
            base.player.look.updateLook();
        }
    }

    public void setVehicle(InteractableVehicle newVehicle, byte newSeat, Transform newSeatingTransform, Vector3 newSeatingPosition, byte newSeatingAngle, bool forceUpdate)
    {
        hasPendingVehicleChange = true;
        pendingVehicle = newVehicle;
        pendingSeatIndex = newSeat;
        pendingSeatTransform = newSeatingTransform;
        pendingSeatPosition = newSeatingPosition;
        pendingSeatAngle = newSeatingAngle;
        if ((!base.channel.isOwner && !Provider.isServer) || !base.player.life.IsAlive || forceUpdate)
        {
            updateVehicle();
        }
    }

    [Obsolete]
    public void tellPluginGravityMultiplier(CSteamID steamID, float newPluginGravityMultiplier)
    {
        ReceivePluginGravityMultiplier(newPluginGravityMultiplier);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellPluginGravityMultiplier")]
    public void ReceivePluginGravityMultiplier(float newPluginGravityMultiplier)
    {
        pluginGravityMultiplier = newPluginGravityMultiplier;
    }

    public void sendPluginGravityMultiplier(float newPluginGravityMultiplier)
    {
        pluginGravityMultiplier = newPluginGravityMultiplier;
        if (!base.channel.isOwner)
        {
            SendPluginGravityMultiplier.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), newPluginGravityMultiplier);
        }
    }

    [Obsolete]
    public void tellPluginJumpMultiplier(CSteamID steamID, float newPluginJumpMultiplier)
    {
        ReceivePluginJumpMultiplier(newPluginJumpMultiplier);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellPluginJumpMultiplier")]
    public void ReceivePluginJumpMultiplier(float newPluginJumpMultiplier)
    {
        pluginJumpMultiplier = newPluginJumpMultiplier;
    }

    public void sendPluginJumpMultiplier(float newPluginJumpMultiplier)
    {
        pluginJumpMultiplier = newPluginJumpMultiplier;
        if (!base.channel.isOwner)
        {
            SendPluginJumpMultiplier.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), newPluginJumpMultiplier);
        }
    }

    [Obsolete]
    public void tellPluginSpeedMultiplier(CSteamID steamID, float newPluginSpeedMultiplier)
    {
        ReceivePluginSpeedMultiplier(newPluginSpeedMultiplier);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellPluginSpeedMultiplier")]
    public void ReceivePluginSpeedMultiplier(float newPluginSpeedMultiplier)
    {
        pluginSpeedMultiplier = newPluginSpeedMultiplier;
    }

    public void sendPluginSpeedMultiplier(float newPluginSpeedMultiplier)
    {
        pluginSpeedMultiplier = newPluginSpeedMultiplier;
        if (!base.channel.isOwner)
        {
            SendPluginSpeedMultiplier.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), newPluginSpeedMultiplier);
        }
    }

    public void tellState(Vector3 newPosition, byte newPitch, byte newYaw)
    {
        if (!base.channel.isOwner)
        {
            checkGround(newPosition);
            lastUpdatePos = newPosition;
            if (nsb != null)
            {
                nsb.addNewSnapshot(new PitchYawSnapshotInfo(newPosition, (int)newPitch, (float)(int)newYaw * 2f));
            }
        }
    }

    public void updateMovement()
    {
        lastUpdatePos = base.transform.localPosition;
        if (nsb != null)
        {
            nsb.updateLastSnapshot(new PitchYawSnapshotInfo(lastUpdatePos, base.player.look.pitch, base.player.look.yaw));
        }
        pendingLaunchVelocity = Vector3.zero;
        velocity = Vector3.zero;
        mostRecentControllerColliderHit = null;
    }

    private void checkGround(Vector3 position)
    {
        materialName = null;
        materialIsWater = false;
        int bLOCK_COLLISION = RayMasks.BLOCK_COLLISION;
        float num = PlayerStance.RADIUS - 0.001f;
        Physics.SphereCast(new Ray(position + new Vector3(0f, num, 0f), Vector3.down), num, out ground, 0.125f, bLOCK_COLLISION, QueryTriggerInteraction.Ignore);
        _isGrounded = ground.transform != null;
        if ((base.channel.isOwner || Provider.isServer) && controller.isGrounded)
        {
            _isGrounded = true;
        }
        if (base.player.stance.stance == EPlayerStance.CLIMB || base.player.stance.stance == EPlayerStance.SWIM || base.player.stance.stance == EPlayerStance.DRIVING || base.player.stance.stance == EPlayerStance.SITTING)
        {
            _isGrounded = true;
        }
        if (base.player.stance.stance == EPlayerStance.CLIMB)
        {
            materialName = "Tile";
        }
        else if (base.player.stance.stance == EPlayerStance.SWIM || WaterUtility.isPointUnderwater(base.transform.position))
        {
            materialName = "Water";
            materialIsWater = true;
        }
        else if (ground.transform != null)
        {
            if (ground.transform.CompareTag("Ground"))
            {
                materialName = PhysicsTool.GetTerrainMaterialName(base.transform.position);
            }
            else
            {
                materialName = ground.collider?.sharedMaterial?.name;
            }
        }
    }

    private bool PlayLandAudioClip()
    {
        if (base.player.stance.stance == EPlayerStance.PRONE || string.IsNullOrEmpty(materialName))
        {
            return false;
        }
        OneShotAudioDefinition audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, "BipedLand");
        if (audioDef == null)
        {
            return false;
        }
        AudioClip randomClip = audioDef.GetRandomClip();
        if (randomClip == null)
        {
            return false;
        }
        float num = 1f - base.player.skills.mastery(1, 0) * 0.75f;
        if (base.player.stance.stance == EPlayerStance.CROUCH)
        {
            num *= 0.5f;
        }
        num *= 0.15f;
        OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.transform, randomClip);
        oneShotAudioParameters.volume = num * audioDef.volumeMultiplier;
        oneShotAudioParameters.RandomizePitch(audioDef.minPitch, audioDef.maxPitch);
        oneShotAudioParameters.SetLinearRolloff(1f, 24f);
        oneShotAudioParameters.Play();
        lastFootstep = Time.time;
        return true;
    }

    private void PlayFootstepAudioClip()
    {
        string propertyName = ((base.player.stance.stance == EPlayerStance.SPRINT) ? "FootstepRun" : "FootstepWalk");
        OneShotAudioDefinition audioDef = PhysicMaterialCustomData.GetAudioDef(materialName, propertyName);
        if (audioDef == null)
        {
            return;
        }
        AudioClip randomClip = audioDef.GetRandomClip();
        if (!(randomClip == null))
        {
            float num = 1f - base.player.skills.mastery(1, 0) * 0.75f;
            if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                num *= 0.5f;
            }
            num *= 0.125f;
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.transform, randomClip);
            oneShotAudioParameters.volume = num * audioDef.volumeMultiplier;
            oneShotAudioParameters.RandomizePitch(audioDef.minPitch, audioDef.maxPitch);
            oneShotAudioParameters.SetLinearRolloff(1f, 32f);
            oneShotAudioParameters.Play();
        }
    }

    internal void PlaySwimAudioClip()
    {
        OneShotAudioDefinition oneShotAudioDefinition;
        if (base.player.stance.stance == EPlayerStance.SWIM)
        {
            if (base.player.stance.areEyesUnderwater)
            {
                oneShotAudioDefinition = Assets.coreMasterBundle.assetBundle.LoadAsset<OneShotAudioDefinition>("Assets/CoreMasterBundle/Effects/Physics/Swim/MediumWading/Swim_MediumWading.asset");
                if (oneShotAudioDefinition == null)
                {
                    UnturnedLog.warn("Missing built-in medium wading audio");
                }
            }
            else
            {
                oneShotAudioDefinition = Assets.coreMasterBundle.assetBundle.LoadAsset<OneShotAudioDefinition>("Assets/CoreMasterBundle/Effects/Physics/Swim/HeavyWading/Swim_HeavyWading.asset");
                if (oneShotAudioDefinition == null)
                {
                    UnturnedLog.warn("Missing built-in heavy wading audio");
                }
            }
        }
        else if (WaterUtility.isPointUnderwater(base.transform.position + new Vector3(0f, 0.5f, 0f)))
        {
            oneShotAudioDefinition = Assets.coreMasterBundle.assetBundle.LoadAsset<OneShotAudioDefinition>("Assets/CoreMasterBundle/Effects/Physics/Swim/LightWading/Swim_LightWading.asset");
            if (oneShotAudioDefinition == null)
            {
                UnturnedLog.warn("Missing built-in light wading audio");
            }
        }
        else
        {
            string propertyName = ((base.player.stance.stance == EPlayerStance.SPRINT) ? "FootstepRun" : "FootstepWalk");
            oneShotAudioDefinition = PhysicMaterialCustomData.GetAudioDef("Water", propertyName);
        }
        if (oneShotAudioDefinition == null)
        {
            return;
        }
        AudioClip randomClip = oneShotAudioDefinition.GetRandomClip();
        if (!(randomClip == null))
        {
            float num = 0.15f;
            if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                num *= 0.5f;
            }
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.transform, randomClip);
            oneShotAudioParameters.volume = num * oneShotAudioDefinition.volumeMultiplier;
            oneShotAudioParameters.RandomizePitch(oneShotAudioDefinition.minPitch, oneShotAudioDefinition.maxPitch);
            oneShotAudioParameters.SetLinearRolloff(1f, 32f);
            oneShotAudioParameters.Play();
        }
    }

    private void onVisionUpdated(bool isViewing)
    {
        if (isViewing)
        {
            warp_x = ((!((double)UnityEngine.Random.value < 0.25)) ? 1 : (-1));
            warp_y = ((!((double)UnityEngine.Random.value < 0.25)) ? 1 : (-1));
        }
        else
        {
            warp_x = 1;
            warp_y = 1;
        }
    }

    public bool forceRemoveFromVehicle()
    {
        if (vehicle != null && base.channel != null && base.channel.owner != null && vehicle.forceRemovePlayer(out var b, base.channel.owner.playerID.steamID, out var point, out var angle))
        {
            VehicleManager.sendExitVehicle(vehicle, b, point, angle, forceUpdate: true);
            return true;
        }
        if (hasPendingVehicleChange && pendingVehicle != null)
        {
            byte angle2 = MeasurementTool.angleToByte(base.transform.rotation.eulerAngles.y);
            VehicleManager.sendExitVehicle(pendingVehicle, pendingSeatIndex, base.transform.position, angle2, forceUpdate: true);
            return true;
        }
        return false;
    }

    public void simulate()
    {
        updateRegionAndBound();
        if (base.channel.isOwner)
        {
            lastUpdatePos = base.transform.position;
        }
        if (hasPendingVehicleChange)
        {
            hasPendingVehicleChange = false;
            updateVehicle();
        }
    }

    public void simulate(uint simulation, int recov, bool inputBrake, bool inputStamina, Vector3 point, Quaternion rotation, float speed, float physicsSpeed, int turn, float delta)
    {
        updateRegionAndBound();
        if (base.channel.isOwner)
        {
            lastUpdatePos = base.transform.position;
        }
        velocity = Vector3.zero;
        pendingLaunchVelocity = Vector3.zero;
        mostRecentControllerColliderHit = null;
        if (hasPendingVehicleChange)
        {
            hasPendingVehicleChange = false;
            updateVehicle();
        }
        else if (base.player.stance.stance == EPlayerStance.DRIVING && vehicle != null)
        {
            vehicle.simulate(simulation, recov, inputStamina, point, rotation, speed, physicsSpeed, turn, delta);
        }
    }

    public void simulate(uint simulation, int recov, int input_x, int input_y, float look_x, float look_y, bool inputJump, bool inputSprint, float deltaTime)
    {
        updateRegionAndBound();
        if (base.channel.isOwner)
        {
            lastUpdatePos = base.transform.position;
        }
        if (hasPendingVehicleChange)
        {
            hasPendingVehicleChange = false;
            updateVehicle();
        }
        _move.x = input_x;
        _move.z = input_y;
        if (base.player.stance.stance == EPlayerStance.SITTING)
        {
            _isMoving = false;
            checkGround(base.transform.position);
            mostRecentControllerColliderHit = null;
            velocity = Vector3.zero;
            pendingLaunchVelocity = Vector3.zero;
            if (getVehicle() != null && getVehicle().passengers[getSeat()].turret != null && (Mathf.Abs(base.player.look.lastAngle - base.player.look.angle) > 1 || Mathf.Abs(base.player.look.lastRot - base.player.look.rot) > 1))
            {
                base.player.look.lastAngle = base.player.look.angle;
                base.player.look.lastRot = base.player.look.rot;
                if (canAddSimulationResultsToUpdates)
                {
                    updates.Add(new PlayerStateUpdate(base.transform.position, base.player.look.angle, base.player.look.rot));
                }
            }
            return;
        }
        if (base.player.stance.stance == EPlayerStance.DRIVING)
        {
            _isMoving = false;
            checkGround(base.transform.position);
            mostRecentControllerColliderHit = null;
            velocity = Vector3.zero;
            pendingLaunchVelocity = Vector3.zero;
            if (base.channel.isOwner)
            {
                vehicle.simulate(simulation, recov, input_x, input_y, look_x, look_y, inputJump, inputSprint, deltaTime);
                if (onVehicleUpdated != null)
                {
                    vehicle.getDisplayFuel(out var currentFuel, out var maxFuel);
                    onVehicleUpdated(!vehicle.isUnderwater && !vehicle.isDead, currentFuel, maxFuel, vehicle.speed, vehicle.asset.speedMin, vehicle.asset.speedMax, vehicle.health, vehicle.asset.health, vehicle.batteryCharge);
                }
            }
            return;
        }
        if (base.player.stance.stance == EPlayerStance.CLIMB)
        {
            _isMoving = (double)Mathf.Abs(move.x) > 0.1 || (double)Mathf.Abs(move.z) > 0.1;
            checkGround(base.transform.position);
            pendingLaunchVelocity = Vector3.zero;
            velocity = new Vector3(0f, _move.z * speed * 0.5f, 0f);
            mostRecentControllerColliderHit = null;
            controller.CheckedMove(velocity * deltaTime);
        }
        else if (base.player.stance.stance == EPlayerStance.SWIM)
        {
            _isMoving = (double)Mathf.Abs(move.x) > 0.1 || (double)Mathf.Abs(move.z) > 0.1;
            checkGround(base.transform.position);
            pendingLaunchVelocity = Vector3.zero;
            if (base.player.stance.isSubmerged || (base.player.look.pitch > 110f && (double)move.z > 0.1))
            {
                velocity = base.player.look.aim.rotation * move.normalized * speed * 1.5f;
                if (inputJump)
                {
                    velocity.y = SWIM * pluginJumpMultiplier;
                }
                mostRecentControllerColliderHit = null;
                controller.CheckedMove(velocity * deltaTime);
            }
            else
            {
                WaterUtility.getUnderwaterInfo(base.transform.position, out var _, out var surfaceElevation);
                velocity = base.transform.rotation * move.normalized * speed * 1.5f;
                velocity.y = (surfaceElevation - 1.275f - base.transform.position.y) / 8f;
                mostRecentControllerColliderHit = null;
                controller.CheckedMove(velocity * deltaTime);
            }
        }
        else
        {
            _isMoving = (double)Mathf.Abs(move.x) > 0.1 || (double)Mathf.Abs(move.z) > 0.1;
            bool flag = isGrounded;
            checkGround(base.transform.position);
            bool flag2 = false;
            bool flag3 = false;
            Vector3 rhs = Vector3.up;
            if (isGrounded && ground.normal.y > 0f)
            {
                float num = Vector3.Angle(Vector3.up, ground.normal);
                float num2 = 59f;
                if (Level.info != null && Level.info.configData != null && Level.info.configData.Max_Walkable_Slope > -0.5f)
                {
                    num2 = Level.info.configData.Max_Walkable_Slope;
                }
                flag3 = num > num2;
                rhs = ground.normal;
            }
            if (!flag3 && mostRecentControllerColliderHit != null && mostRecentControllerColliderHit.collider != null && mostRecentControllerColliderHit.gameObject != null && mostRecentControllerColliderHit.normal.y > 0f && mostRecentControllerColliderHit.gameObject.CompareTag("Agent"))
            {
                flag3 = true;
                rhs = mostRecentControllerColliderHit.normal;
            }
            if (flag3)
            {
                Vector3 normalized = Vector3.Cross(Vector3.Cross(Vector3.up, rhs).normalized, rhs).normalized;
                velocity += normalized * 16f * deltaTime;
                flag2 = true;
            }
            else
            {
                Vector3 vector = base.transform.rotation * move.normalized * speed;
                if (isGrounded)
                {
                    PhysicsMaterialCharacterFrictionProperties characterFrictionProperties = PhysicMaterialCustomData.GetCharacterFrictionProperties(materialName);
                    if (characterFrictionProperties.mode == EPhysicsMaterialCharacterFrictionMode.ImmediatelyResponsive)
                    {
                        vector = Vector3.Cross(Vector3.Cross(Vector3.up, vector).normalized, ground.normal).normalized * speed;
                        vector.y = Mathf.Min(vector.y, 0f);
                        velocity = vector;
                    }
                    else
                    {
                        Vector3 vector2 = Vector3.ProjectOnPlane(velocity, ground.normal);
                        float magnitude = vector2.magnitude;
                        Vector3 vector3 = Vector3.Cross(Vector3.Cross(Vector3.up, vector).normalized, ground.normal).normalized * speed;
                        vector3 *= characterFrictionProperties.maxSpeedMultiplier;
                        float magnitude2 = vector3.magnitude;
                        float maxMagnitude;
                        if (magnitude > magnitude2)
                        {
                            float num3 = -2f * characterFrictionProperties.decelerationMultiplier;
                            maxMagnitude = Mathf.Max(magnitude2, magnitude + num3 * deltaTime);
                        }
                        else
                        {
                            maxMagnitude = magnitude2;
                        }
                        Vector3 vector4 = vector3 * characterFrictionProperties.accelerationMultiplier;
                        Vector3 vector5 = vector2 + vector4 * deltaTime;
                        velocity = vector5.ClampMagnitude(maxMagnitude);
                        flag2 = true;
                    }
                }
                else
                {
                    velocity.y += Physics.gravity.y * ((fall <= 0f) ? totalGravityMultiplier : 1f) * deltaTime * 3f;
                    float a = ((totalGravityMultiplier < 0.99f) ? (Physics.gravity.y * 2f * totalGravityMultiplier) : (-100f));
                    velocity.y = Mathf.Max(a, velocity.y);
                    float horizontalMagnitude = vector.GetHorizontalMagnitude();
                    Vector3 vector6 = velocity.GetHorizontal();
                    float horizontalMagnitude2 = velocity.GetHorizontalMagnitude();
                    float maxMagnitude2;
                    if (horizontalMagnitude2 > horizontalMagnitude)
                    {
                        float num4 = 2f * Provider.modeConfigData.Gameplay.AirStrafing_Deceleration_Multiplier;
                        maxMagnitude2 = Mathf.Max(horizontalMagnitude, horizontalMagnitude2 - num4 * deltaTime);
                    }
                    else
                    {
                        maxMagnitude2 = horizontalMagnitude;
                    }
                    Vector3 vector7 = vector * (4f * Provider.modeConfigData.Gameplay.AirStrafing_Acceleration_Multiplier);
                    Vector3 vector8 = vector6 + vector7 * deltaTime;
                    vector8 = vector8.ClampHorizontalMagnitude(maxMagnitude2);
                    velocity.x = vector8.x;
                    velocity.z = vector8.z;
                    flag2 = true;
                }
            }
            if (inputJump && isGrounded && !base.player.life.isBroken && (float)(int)base.player.life.stamina >= 10f * (1f - base.player.skills.mastery(0, 6) * 0.5f) && (base.player.stance.stance == EPlayerStance.STAND || base.player.stance.stance == EPlayerStance.SPRINT) && !MathfEx.IsNearlyZero(pluginJumpMultiplier, 0.001f))
            {
                velocity.y = JUMP * (1f + base.player.skills.mastery(0, 6) * 0.25f) * pluginJumpMultiplier;
                base.player.life.askTire((byte)(10f * (1f - base.player.skills.mastery(0, 6) * 0.5f)));
            }
            velocity += pendingLaunchVelocity;
            pendingLaunchVelocity = Vector3.zero;
            if (base.channel.isOwner && LoadingUI.isBlocked)
            {
                velocity = Vector3.zero;
            }
            else
            {
                Vector3 position = base.transform.position;
                mostRecentControllerColliderHit = null;
                controller.CheckedMove(velocity * deltaTime);
                if (!flag)
                {
                    checkGround(base.transform.position);
                    if (isGrounded)
                    {
                        onLanded?.Invoke(velocity.y);
                        if (!base.player.input.isResimulating && Mathf.Abs(velocity.y) > 1f)
                        {
                            PlayLandAudioClip();
                        }
                    }
                }
                if (flag2)
                {
                    velocity = (base.transform.position - position) / deltaTime;
                }
            }
        }
        if (Level.info != null && Level.info.configData.Use_Legacy_Clip_Borders)
        {
            Vector3 position2 = base.transform.position;
            float num5 = (float)(int)Level.size / 2f - (float)(int)Level.border;
            float num6 = num5 + 8f;
            bool flag4 = false;
            if (position2.x > 0f - num6 && position2.x < 0f - num5)
            {
                position2.x = 0f - num5 + 1f;
                flag4 = true;
            }
            else if (position2.x < num6 && position2.x > num5)
            {
                position2.x = num5 - 1f;
                flag4 = true;
            }
            if (position2.z > 0f - num6 && position2.z < 0f - num5)
            {
                position2.z = 0f - num5 + 1f;
                flag4 = true;
            }
            else if (position2.z < num6 && position2.z > num5)
            {
                position2.z = num5 - 1f;
                flag4 = true;
            }
            if (flag4)
            {
                position2.y += 8f;
            }
            position2.y = Mathf.Clamp(position2.y, 0f, Level.HEIGHT);
            base.transform.position = position2;
        }
        if (Provider.isServer && !bypassUndergroundWhitelist && (!Dedicator.IsDedicatedServer || !base.channel.owner.isAdmin))
        {
            Vector3 worldspacePosition = base.transform.position;
            if (UndergroundAllowlist.AdjustPosition(ref worldspacePosition, 0.5f))
            {
                base.transform.position = worldspacePosition;
            }
        }
        if (base.channel.isOwner || !Provider.isServer || updates == null)
        {
            return;
        }
        Vector3 position3 = base.transform.position;
        if (Mathf.Abs(base.player.look.lastAngle - base.player.look.angle) > 1 || Mathf.Abs(base.player.look.lastRot - base.player.look.rot) > 1 || Mathf.Abs(lastUpdatePos.x - position3.x) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatePos.y - position3.y) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatePos.z - position3.z) > Provider.UPDATE_DISTANCE)
        {
            base.player.look.lastAngle = base.player.look.angle;
            base.player.look.lastRot = base.player.look.rot;
            lastUpdatePos = position3;
            if (canAddSimulationResultsToUpdates)
            {
                updates.Add(new PlayerStateUpdate(position3, base.player.look.angle, base.player.look.rot));
            }
            else
            {
                updates.Add(new PlayerStateUpdate(Vector3.zero, 0, 0));
            }
        }
    }

    private void Update()
    {
        if (nsb != null)
        {
            snapshot = nsb.getCurrentSnapshot();
        }
        if (base.channel.isOwner)
        {
            if (!PlayerUI.window.showCursor && !LoadingUI.isBlocked)
            {
                _jump = InputEx.GetKey(ControlsSettings.jump);
                if (getVehicle() != null)
                {
                    if (InputEx.GetKeyDown(ControlsSettings.locker))
                    {
                        VehicleManager.sendVehicleLock();
                    }
                    if (InputEx.GetKeyDown(ControlsSettings.primary))
                    {
                        VehicleManager.sendVehicleHorn();
                    }
                    if (InputEx.GetKeyDown(ControlsSettings.secondary))
                    {
                        VehicleManager.sendVehicleHeadlights();
                    }
                    if (InputEx.GetKeyDown(ControlsSettings.other))
                    {
                        VehicleManager.sendVehicleBonus();
                    }
                }
                if (getVehicle() != null && getVehicle().asset != null && (getVehicle().asset.engine == EEngine.PLANE || getVehicle().asset.engine == EEngine.HELICOPTER || getVehicle().asset.engine == EEngine.BLIMP))
                {
                    if (InputEx.GetKey(ControlsSettings.yawLeft))
                    {
                        input_x = -1;
                    }
                    else if (InputEx.GetKey(ControlsSettings.yawRight))
                    {
                        input_x = 1;
                    }
                    else
                    {
                        input_x = 0;
                    }
                    if (InputEx.GetKey(ControlsSettings.thrustIncrease))
                    {
                        input_y = 1;
                    }
                    else if (InputEx.GetKey(ControlsSettings.thrustDecrease))
                    {
                        input_y = -1;
                    }
                    else
                    {
                        input_y = 0;
                    }
                }
                else
                {
                    if (InputEx.GetKey(ControlsSettings.left))
                    {
                        input_x = -1;
                    }
                    else if (InputEx.GetKey(ControlsSettings.right))
                    {
                        input_x = 1;
                    }
                    else
                    {
                        input_x = 0;
                    }
                    if (InputEx.GetKey(ControlsSettings.up))
                    {
                        input_y = 1;
                    }
                    else if (InputEx.GetKey(ControlsSettings.down))
                    {
                        input_y = -1;
                    }
                    else
                    {
                        input_y = 0;
                    }
                }
            }
            else
            {
                _jump = false;
                input_x = 0;
                input_y = 0;
            }
            input_x *= warp_x;
            input_y *= warp_y;
            if (base.player.look.isOrbiting)
            {
                _jump = false;
                _horizontal = 1;
                _vertical = 1;
            }
            else
            {
                _horizontal = (byte)(input_x + 1);
                _vertical = (byte)(input_y + 1);
            }
        }
        if (!Dedicator.IsDedicatedServer && Time.time - lastFootstep > 2.1f / speed)
        {
            lastFootstep = Time.time;
            bool flag = false;
            if (!base.channel.isOwner)
            {
                bool num = isGrounded;
                checkGround(base.transform.position);
                if (!num && isGrounded)
                {
                    flag = PlayLandAudioClip();
                }
            }
            if (isGrounded && !flag && isMoving && base.player.stance.stance != EPlayerStance.PRONE)
            {
                if (materialIsWater || base.player.stance.stance == EPlayerStance.SWIM)
                {
                    PlaySwimAudioClip();
                }
                else if (!string.IsNullOrEmpty(materialName))
                {
                    PlayFootstepAudioClip();
                }
            }
        }
        if (base.channel.isOwner)
        {
            if (base.player.look.isOrbiting && (!base.player.workzone.isBuilding || InputEx.GetKey(ControlsSettings.secondary)))
            {
                base.player.look.orbitSpeed = Mathf.Clamp(base.player.look.orbitSpeed + Input.GetAxis("mouse_z") * 0.2f * base.player.look.orbitSpeed, 0.5f, 2048f);
                base.player.look.orbitPosition += MainCamera.instance.transform.right * input_x * Time.deltaTime * base.player.look.orbitSpeed;
                base.player.look.orbitPosition += MainCamera.instance.transform.forward * input_y * Time.deltaTime * base.player.look.orbitSpeed;
                float num2 = (InputEx.GetKey(ControlsSettings.ascend) ? 1f : ((!InputEx.GetKey(ControlsSettings.descend)) ? 0f : (-1f)));
                base.player.look.orbitPosition += Vector3.up * num2 * Time.deltaTime * base.player.look.orbitSpeed;
            }
            if (base.player.stance.stance == EPlayerStance.DRIVING || base.player.stance.stance == EPlayerStance.SITTING)
            {
                base.player.first.localPosition = Vector3.zero;
                base.player.third.localPosition = Vector3.zero;
            }
            else
            {
                base.player.first.position = Vector3.Lerp(lastUpdatePos, base.transform.position, (Time.realtimeSinceStartup - base.player.input.tick) / PlayerInput.RATE);
                if (base.player.stance.stance == EPlayerStance.PRONE)
                {
                    base.player.first.position += Vector3.down * 0.1f;
                }
                base.player.third.position = base.player.first.position;
            }
            base.player.look.aim.parent.transform.position = base.player.first.position;
            if (vehicle != null)
            {
                if ((base.transform.position - lastStatPos).sqrMagnitude > 1024f)
                {
                    lastStatPos = base.transform.position;
                }
                else if (Time.realtimeSinceStartup - lastStatTime > 1f)
                {
                    lastStatTime = Time.realtimeSinceStartup;
                    if ((base.transform.position - lastStatPos).sqrMagnitude > 0.1f)
                    {
                        if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Travel_Vehicle", out int data))
                        {
                            Provider.provider.statisticsService.userStatisticsService.setStatistic("Travel_Vehicle", data + (int)(base.transform.position - lastStatPos).magnitude);
                        }
                        lastStatPos = base.transform.position;
                    }
                }
            }
            else if ((base.transform.position - lastStatPos).sqrMagnitude > 256f)
            {
                lastStatPos = base.transform.position;
            }
            else if (Time.realtimeSinceStartup - lastStatTime > 1f)
            {
                lastStatTime = Time.realtimeSinceStartup;
                if ((base.transform.position - lastStatPos).sqrMagnitude > 0.1f)
                {
                    if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Travel_Foot", out int data2))
                    {
                        Provider.provider.statisticsService.userStatisticsService.setStatistic("Travel_Foot", data2 + (int)(base.transform.position - lastStatPos).magnitude);
                    }
                    lastStatPos = base.transform.position;
                }
            }
        }
        else if (!Provider.isServer)
        {
            if (base.player.stance.stance == EPlayerStance.SITTING || base.player.stance.stance == EPlayerStance.DRIVING)
            {
                _isMoving = false;
                base.transform.localPosition = Vector3.zero;
            }
            else
            {
                if (Mathf.Abs(lastUpdatePos.x - base.transform.position.x) > 0.01f || Mathf.Abs(lastUpdatePos.y - base.transform.position.y) > 0.01f || Mathf.Abs(lastUpdatePos.z - base.transform.position.z) > 0.01f)
                {
                    _isMoving = true;
                }
                else
                {
                    _isMoving = false;
                }
                base.transform.localPosition = snapshot.pos;
            }
        }
        if (!base.channel.isOwner && base.player.third != null)
        {
            if (base.player.stance.stance == EPlayerStance.PRONE)
            {
                base.player.third.localPosition = new Vector3(0f, -0.1f, 0f);
            }
            else
            {
                base.player.third.localPosition = Vector3.zero;
            }
        }
    }

    private void updateRegionAndBound()
    {
        if (Regions.tryGetCoordinate(base.transform.position, out var x, out var y) && (x != region_x || y != region_y))
        {
            byte b = region_x;
            byte b2 = region_y;
            _region_x = x;
            _region_y = y;
            updateRegionOld_X = b;
            updateRegionOld_Y = b2;
            updateRegionNew_X = x;
            updateRegionNew_Y = y;
            updateRegionIndex = 0;
        }
        if (updateRegionIndex < 6)
        {
            bool canIncrementIndex = true;
            onRegionUpdated?.Invoke(base.player, updateRegionOld_X, updateRegionOld_Y, updateRegionNew_X, updateRegionNew_Y, updateRegionIndex, ref canIncrementIndex);
            if (canIncrementIndex)
            {
                updateRegionIndex++;
            }
        }
        LevelNavigation.tryGetBounds(base.transform.position, out var b3);
        if (b3 != bound)
        {
            byte oldBound = bound;
            _bound = b3;
            onBoundUpdated?.Invoke(base.player, oldBound, b3);
        }
        if (Provider.isServer)
        {
            LevelNavigation.tryGetNavigation(base.transform.position, out var b4);
            if (b4 != nav)
            {
                byte oldNav = nav;
                _nav = b4;
                TriggerPlayerNavChanged(oldNav, b4);
            }
        }
        bool flag = LevelNodes.isPointInsideSafezone(base.transform.position, out isSafeInfo);
        bool flag2 = false;
        IDeadzoneNode activeDeadzone = null;
        HordePurchaseVolume firstOverlappingVolume = VolumeManager<HordePurchaseVolume, HordePurchaseVolumeManager>.Get().GetFirstOverlappingVolume(base.transform.position);
        effectNode = null;
        inSnow = LevelLighting.isPositionSnowy(base.transform.position);
        AmbianceVolume firstOverlappingVolume2 = VolumeManager<AmbianceVolume, AmbianceVolumeManager>.Get().GetFirstOverlappingVolume(base.transform.position);
        if (firstOverlappingVolume2 != null)
        {
            effectNode = firstOverlappingVolume2;
            if (!inSnow && Level.info.configData.Use_Snow_Volumes)
            {
                inSnow = (firstOverlappingVolume2.weatherMask & 2) != 0;
            }
            WeatherMask = firstOverlappingVolume2.weatherMask;
        }
        else
        {
            WeatherMask = (uint)(((int?)Level.getAsset()?.globalWeatherMask) ?? (-1));
        }
        inSnow &= LevelLighting.snowyness == ELightingSnow.BLIZZARD;
        DeadzoneVolume mostDangerousOverlappingVolume = VolumeManager<DeadzoneVolume, DeadzoneVolumeManager>.Get().GetMostDangerousOverlappingVolume(base.transform.position);
        if (mostDangerousOverlappingVolume != null)
        {
            flag2 = true;
            activeDeadzone = mostDangerousOverlappingVolume;
        }
        if (flag != isSafe)
        {
            _isSafe = flag;
            onSafetyUpdated?.Invoke(isSafe);
        }
        ActiveDeadzone = activeDeadzone;
        if (flag2 != isRadiated)
        {
            _isRadiated = flag2;
            onRadiationUpdated?.Invoke(isRadiated);
        }
        if (firstOverlappingVolume != purchaseNode)
        {
            _purchaseNode = firstOverlappingVolume;
            onPurchaseUpdated?.Invoke(purchaseNode);
        }
        base.player.inventory.closeDistantStorage();
    }

    internal void InitializePlayer()
    {
        itemGravityMultiplier = 1f;
        pluginGravityMultiplier = 1f;
        pluginSpeedMultiplier = 1f;
        _region_x = byte.MaxValue;
        _region_y = byte.MaxValue;
        _bound = byte.MaxValue;
        _nav = byte.MaxValue;
        if (base.channel.isOwner || Provider.isServer)
        {
            _loadedRegions = new LoadedRegion[Regions.WORLD_SIZE, Regions.WORLD_SIZE];
            for (byte b = 0; b < Regions.WORLD_SIZE; b = (byte)(b + 1))
            {
                for (byte b2 = 0; b2 < Regions.WORLD_SIZE; b2 = (byte)(b2 + 1))
                {
                    loadedRegions[b, b2] = new LoadedRegion();
                }
            }
            _loadedBounds = new LoadedBound[LevelNavigation.bounds.Count];
            for (byte b3 = 0; b3 < LevelNavigation.bounds.Count; b3 = (byte)(b3 + 1))
            {
                loadedBounds[b3] = new LoadedBound();
            }
        }
        warp_x = 1;
        warp_y = 1;
        if (Provider.isServer || base.channel.isOwner)
        {
            controller = GetComponent<CharacterController>();
            controller.enableOverlapRecovery = false;
        }
        if (Provider.isServer)
        {
            PlayerLife life = base.player.life;
            life.onVisionUpdated = (VisionUpdated)Delegate.Combine(life.onVisionUpdated, new VisionUpdated(onVisionUpdated));
        }
        else
        {
            nsb = new NetworkSnapshotBuffer<PitchYawSnapshotInfo>(Provider.UPDATE_TIME, Provider.UPDATE_DELAY);
        }
        applySize();
        if (Dedicator.IsDedicatedServer)
        {
            base.gameObject.AddComponent<Rigidbody>();
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
        }
        updateMovement();
        updates = new List<PlayerStateUpdate>();
        canAddSimulationResultsToUpdates = true;
        lastFootstep = Time.time;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        mostRecentControllerColliderHit = hit;
    }

    private void OnDrawGizmos()
    {
        if (nsb != null)
        {
            for (int i = 0; i < nsb.snapshots.Length && !(nsb.snapshots[i].timestamp <= 0.01f); i++)
            {
                PitchYawSnapshotInfo info = nsb.snapshots[i].info;
                Gizmos.DrawLine(info.pos, info.pos + Vector3.up * 2f);
            }
        }
    }

    private void OnDestroy()
    {
        updates = null;
    }
}
