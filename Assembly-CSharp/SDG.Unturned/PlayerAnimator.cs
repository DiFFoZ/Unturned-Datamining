using System;
using System.Collections.Generic;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class PlayerAnimator : PlayerCaller
{
    public delegate void InventoryGestureListener(bool InInventory);

    public static readonly byte SAVEDATA_VERSION = 2;

    private static readonly float BOB_SPRINT = 0.075f;

    private static readonly float BOB_STAND = 0.05f;

    private static readonly float BOB_CROUCH = 0.025f;

    private static readonly float BOB_PRONE = 0.0125f;

    private static readonly float BOB_SWIM = 0.025f;

    private static readonly float TILT_SPRINT = 5f;

    private static readonly float TILT_STAND = 3f;

    private static readonly float TILT_CROUCH = 2f;

    private static readonly float TILT_PRONE = 1f;

    private static readonly float TILT_SWIM = 10f;

    private static readonly float SPEED_SPRINT = 10f;

    private static readonly float SPEED_STAND = 8f;

    private static readonly float SPEED_CROUCH = 6f;

    private static readonly float SPEED_PRONE = 4f;

    private static readonly float SPEED_SWIM = 6f;

    public GestureUpdated onGestureUpdated;

    /// <summary>
    /// Empty transform created at the world origin.
    /// The first-person Viewmodel transform is re-parented to this.
    /// </summary>
    public Transform viewmodelParentTransform;

    private CharacterAnimator firstAnimator;

    private CharacterAnimator thirdAnimator;

    private HumanAnimator characterAnimator;

    private SkinnedMeshRenderer firstRenderer_0;

    private SkinnedMeshRenderer thirdRenderer_0;

    private SkinnedMeshRenderer thirdRenderer_1;

    private Transform _firstSkeleton;

    private Transform _thirdSkeleton;

    /// <summary>
    /// Child of the first-person skull transform.
    /// </summary>
    public Transform viewmodelCameraTransform;

    /// <summary>
    /// Camera near world origin masking the first-person arms and weapon.
    /// </summary>
    public Camera viewmodelCamera;

    /// <summary>
    /// Used by gun to hide viewmodel arms while aiming 2D scope, and by chainsaw to shake the viewmodel.
    /// </summary>
    public Vector3 viewmodelCameraLocalPositionOffset;

    /// <summary>
    /// Used to hide viewmodel arms while using a vehicle turret gun.
    /// </summary>
    public Vector3 drivingViewmodelCameraLocalPositionOffset;

    /// <summary>
    /// Offsets main camera and aim rotation while aiming with a scoped gun.
    /// </summary>
    public Vector3 scopeSway;

    /// <summary>
    /// Animated toward viewmodelSwayMultiplier.
    /// </summary>
    private float blendedViewmodelSwayMultiplier;

    /// <summary>
    /// Small number (0.1) while aiming, 1 while not aiming.
    /// Reduces viewmodel animation while aiming to make 3D sights more usable.
    /// </summary>
    public float viewmodelSwayMultiplier;

    /// <summary>
    /// Animated toward viewmodelOffsetPreferenceMultiplier.
    /// </summary>
    private float blendedViewmodelOffsetPreferenceMultiplier;

    /// <summary>
    /// 0 while aiming, 1 while not aiming.
    /// Players can customize the 3D position of the viewmodel on screen, but this needs
    /// to be blended out while aiming down sights otherwise it would not line up with
    /// the center of the screen.
    /// </summary>
    public float viewmodelOffsetPreferenceMultiplier;

    /// <summary>
    /// Animated toward viewmodelCameraLocalPositionOffset, recoil, and bayonet offsets.
    /// </summary>
    private Vector3 blendedViewmodelCameraLocalPositionOffset;

    /// <summary>
    /// Abruptly offset when gun is fired, then animated back toward zero.
    /// </summary>
    public Rk4Spring3 recoilViewmodelCameraOffset;

    /// <summary>
    /// Abruptly offset when gun is fired, then animated back toward zero.
    /// x = pitch, y = yaw, z = roll
    /// </summary>
    public Rk4Spring3 recoilViewmodelCameraRotation;

    public Vector3 recoilViewmodelCameraMask = Vector3.one;

    /// <summary>
    /// Abruptly offset when bayonet is used, then animated back toward zero.
    /// </summary>
    private Vector3 bayonetViewmodelCameraOffset;

    /// <summary>
    /// Animated while player is moving.
    /// </summary>
    public Rk4Spring2 viewmodelMovementOffset;

    /// <summary>
    /// Blended from multiple viewmodel parameters and then applied to viewmodelCameraTransform.
    /// </summary>
    private Vector3 viewmodelCameraLocalPosition;

    public Rk4SpringQ viewmodelTargetExplosionLocalRotation;

    /// <summary>
    /// Smoothing adds some initial blend-in which felt nicer for explosion rumble.
    /// </summary>
    private Quaternion viewmodelSmoothedExplosionLocalRotation = Quaternion.identity;

    public float viewmodelExplosionSmoothingSpeed;

    /// <summary>
    /// Meshes are disabled until clothing is received.
    /// </summary>
    private bool isHiddenWaitingForClothing;

    /// <summary>
    /// Target viewmodelCameraLocalPosition except while driving.
    /// </summary>
    private Vector3 desiredViewmodelCameraLocalPosition;

    /// <summary>
    /// Animated while playing is moving.
    /// x = pitch, y = roll
    /// </summary>
    public Rk4Spring2 viewmodelCameraMovementLocalRotation;

    private Vector3 viewmodelCameraLocalRotation;

    /// <summary>
    /// Used to measure change in pitch between frames.
    /// </summary>
    private float lastFramePitchInput;

    /// <summary>
    /// Used to measure change in yaw between frames.
    /// </summary>
    private float lastFrameYawInput;

    /// <summary>
    /// Animated according to change in pitch/yaw input between frames so that gun rolls slightly while turning.
    /// </summary>
    public Rk4Spring3 rotationInputViewmodelRoll;

    private bool lastFrameHadItemPosition;

    private Vector3 lastFrameItemPosition;

    /// <summary>
    /// Animated according to change in item position between frames so that animations have more inertia.
    /// </summary>
    public Rk4Spring3 viewmodelItemInertiaRotation;

    /// <summary>
    /// Degrees per meter of item distance travelled.
    /// Pitch is driven by vertical displacement, yaw and roll are driven by horizontal.
    /// x = pitch, y = yaw, z = roll
    /// </summary>
    public Vector3 viewmodelItemInertiaMask;

    private bool inputWantsToLeanLeft;

    private bool inputWantsToLeanRight;

    internal bool leanObstructed;

    /// <summary>
    /// In third-person this delays leaning in case player only wanted
    /// to switch camera side without leaning.
    /// </summary>
    private float lastCameraSideInputRealtime;

    private int lastLean;

    private int _lean;

    private float _shoulder;

    private float _shoulder2;

    private bool inputWantsThirdPersonCameraOnLeftSide;

    private EPlayerGesture _gesture;

    public CSteamID captorID;

    public ushort captorItem;

    public ushort captorStrength;

    private static readonly ClientInstanceMethod<byte> SendLean = ClientInstanceMethod<byte>.Get(typeof(PlayerAnimator), "ReceiveLean");

    private static readonly ClientInstanceMethod<EPlayerGesture> SendGesture = ClientInstanceMethod<EPlayerGesture>.Get(typeof(PlayerAnimator), "ReceiveGesture");

    /// <summary>
    /// Event for server plugins to monitor whether player is in-inventory.
    /// </summary>
    public InventoryGestureListener onInventoryGesture;

    private static readonly ServerInstanceMethod<EPlayerGesture> SendGestureRequest = ServerInstanceMethod<EPlayerGesture>.Get(typeof(PlayerAnimator), "ReceiveGestureRequest");

    private static Collider[] leanHits = new Collider[1];

    private static readonly AssetReference<EffectAsset> Metal_1_Ref = new AssetReference<EffectAsset>("805bb3b0752749d1b5cf9959d17e104e");

    private bool hasCalledUpdateLocalPlayerModelVisibility;

    private bool wasLocalPlayerFirstPersonModelVisible;

    private bool wasLocalPlayerThirdPersonModelVisible;

    private bool wasLoadCalled;

    public Transform firstSkeleton => _firstSkeleton;

    public Transform thirdSkeleton => _thirdSkeleton;

    public bool leanLeft => inputWantsToLeanLeft;

    public bool leanRight => inputWantsToLeanRight;

    public int lean => _lean;

    public float shoulder => _shoulder;

    public float shoulder2 => _shoulder2;

    public bool side => inputWantsThirdPersonCameraOnLeftSide;

    public EPlayerGesture gesture => _gesture;

    public float bob
    {
        get
        {
            if (Player.player.stance.stance == EPlayerStance.SPRINT)
            {
                return BOB_SPRINT * blendedViewmodelSwayMultiplier;
            }
            if (Player.player.stance.stance == EPlayerStance.STAND)
            {
                return BOB_STAND * blendedViewmodelSwayMultiplier;
            }
            if (Player.player.stance.stance == EPlayerStance.CROUCH)
            {
                return BOB_CROUCH * blendedViewmodelSwayMultiplier;
            }
            if (Player.player.stance.stance == EPlayerStance.PRONE)
            {
                return BOB_PRONE * blendedViewmodelSwayMultiplier;
            }
            if (Player.player.stance.stance == EPlayerStance.SWIM)
            {
                return BOB_SWIM * blendedViewmodelSwayMultiplier;
            }
            return 0f;
        }
    }

    public float tilt
    {
        get
        {
            if (Player.player.stance.stance == EPlayerStance.SPRINT)
            {
                return TILT_SPRINT * (1f - blendedViewmodelSwayMultiplier / 2f);
            }
            if (Player.player.stance.stance == EPlayerStance.STAND)
            {
                return TILT_STAND * (1f - blendedViewmodelSwayMultiplier / 2f);
            }
            if (Player.player.stance.stance == EPlayerStance.CROUCH)
            {
                return TILT_CROUCH * (1f - blendedViewmodelSwayMultiplier / 2f);
            }
            if (Player.player.stance.stance == EPlayerStance.PRONE)
            {
                return TILT_PRONE * (1f - blendedViewmodelSwayMultiplier / 2f);
            }
            if (Player.player.stance.stance == EPlayerStance.SWIM)
            {
                return TILT_SWIM * (1f - blendedViewmodelSwayMultiplier / 2f);
            }
            return 0f;
        }
    }

    public float roll
    {
        get
        {
            if (Player.player.stance.stance == EPlayerStance.SPRINT)
            {
                return Mathf.Sin(TILT_SPRINT * Time.time * 0.25f) * TILT_SPRINT;
            }
            if (Player.player.stance.stance == EPlayerStance.STAND)
            {
                return Mathf.Sin(TILT_STAND * Time.time * 0.5f) * TILT_STAND * 0.5f;
            }
            if (Player.player.stance.stance == EPlayerStance.SWIM)
            {
                return Mathf.Sin(TILT_SWIM * Time.time * 0.25f) * TILT_SWIM * 0.25f;
            }
            return 0f;
        }
    }

    public float speed
    {
        get
        {
            if (Player.player.stance.stance == EPlayerStance.SPRINT)
            {
                return SPEED_SPRINT;
            }
            if (Player.player.stance.stance == EPlayerStance.STAND)
            {
                return SPEED_STAND;
            }
            if (Player.player.stance.stance == EPlayerStance.CROUCH)
            {
                return SPEED_CROUCH;
            }
            if (Player.player.stance.stance == EPlayerStance.PRONE)
            {
                return SPEED_PRONE;
            }
            if (Player.player.stance.stance == EPlayerStance.SWIM)
            {
                return SPEED_SWIM;
            }
            return 0f;
        }
    }

    /// <summary>
    /// Invoked after tellGesture is called with the new gesture.
    /// </summary>
    public static event Action<PlayerAnimator, EPlayerGesture> OnGestureChanged_Global;

    public static event Action<PlayerAnimator> OnLeanChanged_Global;

    public void AddEquippedItemAnimation(AnimationClip clip, Transform firstPersonModel, Transform thirdPersonModel, Transform characterModel)
    {
        if (!(clip == null))
        {
            if (firstAnimator != null)
            {
                firstAnimator.AddEquippedItemAnimation(clip, firstPersonModel);
            }
            if (thirdAnimator != null)
            {
                thirdAnimator.AddEquippedItemAnimation(clip, thirdPersonModel);
            }
            if (characterAnimator != null)
            {
                characterAnimator.AddEquippedItemAnimation(clip, characterModel);
            }
        }
    }

    public void removeAnimation(AnimationClip clip)
    {
        if (!(clip == null))
        {
            if (firstAnimator != null)
            {
                firstAnimator.removeAnimation(clip);
            }
            if (thirdAnimator != null)
            {
                thirdAnimator.removeAnimation(clip);
            }
            if (characterAnimator != null)
            {
                characterAnimator.removeAnimation(clip);
            }
        }
    }

    public void setAnimationSpeed(string name, float speed)
    {
        if (firstAnimator != null)
        {
            firstAnimator.setAnimationSpeed(name, speed);
        }
        if (thirdAnimator != null)
        {
            thirdAnimator.setAnimationSpeed(name, speed);
        }
        if (characterAnimator != null)
        {
            characterAnimator.setAnimationSpeed(name, speed);
        }
    }

    public float getAnimationLength(string name)
    {
        return GetAnimationLength(name);
    }

    /// <param name="scaled">If true, include current animation speed modifier.</param>
    public float GetAnimationLength(string name, bool scaled = true)
    {
        if (firstAnimator != null)
        {
            return firstAnimator.GetAnimationLength(name, scaled);
        }
        if (thirdAnimator != null)
        {
            return thirdAnimator.GetAnimationLength(name, scaled);
        }
        return 0f;
    }

    public bool checkExists(string name)
    {
        if (firstAnimator != null)
        {
            return firstAnimator.checkExists(name);
        }
        if (thirdAnimator != null)
        {
            return thirdAnimator.checkExists(name);
        }
        if (characterAnimator != null)
        {
            return characterAnimator.checkExists(name);
        }
        return false;
    }

    public void play(string name, bool smooth)
    {
        bool flag = false;
        if (firstAnimator != null)
        {
            flag |= firstAnimator.play(name, smooth);
        }
        if (thirdAnimator != null)
        {
            flag |= thirdAnimator.play(name, smooth);
        }
        if (characterAnimator != null)
        {
            flag |= characterAnimator.play(name, smooth);
        }
        if (flag && gesture != 0)
        {
            _gesture = EPlayerGesture.NONE;
        }
    }

    public void stop(string name)
    {
        if (firstAnimator != null)
        {
            firstAnimator.stop(name);
        }
        if (thirdAnimator != null)
        {
            thirdAnimator.stop(name);
        }
        if (characterAnimator != null)
        {
            characterAnimator.stop(name);
        }
    }

    public void mixAnimation(string name)
    {
        if (firstAnimator != null)
        {
            firstAnimator.mixAnimation(name);
        }
        if (thirdAnimator != null)
        {
            thirdAnimator.mixAnimation(name);
        }
        if (characterAnimator != null)
        {
            characterAnimator.mixAnimation(name);
        }
    }

    public void mixAnimation(string name, bool mixLeftShoulder, bool mixRightShoulder)
    {
        mixAnimation(name, mixLeftShoulder, mixRightShoulder, mixSkull: false);
    }

    public void mixAnimation(string name, bool mixLeftShoulder, bool mixRightShoulder, bool mixSkull)
    {
        if (firstAnimator != null)
        {
            firstAnimator.mixAnimation(name, mixLeftShoulder, mixRightShoulder, mixSkull);
        }
        if (thirdAnimator != null)
        {
            thirdAnimator.mixAnimation(name, mixLeftShoulder, mixRightShoulder, mixSkull);
        }
        if (characterAnimator != null)
        {
            characterAnimator.mixAnimation(name, mixLeftShoulder, mixRightShoulder, mixSkull);
        }
    }

    public void AddRecoilViewmodelCameraOffset(float shake_x, float shake_y, float shake_z)
    {
        recoilViewmodelCameraOffset.currentPosition.x += shake_x;
        recoilViewmodelCameraOffset.currentPosition.y += shake_y;
        recoilViewmodelCameraOffset.currentPosition.z += shake_z;
    }

    public void AddRecoilViewmodelCameraRotation(float cameraYaw, float cameraPitch)
    {
        recoilViewmodelCameraRotation.currentPosition.x += cameraPitch * recoilViewmodelCameraMask.x;
        recoilViewmodelCameraRotation.currentPosition.y += cameraYaw * recoilViewmodelCameraMask.y;
        recoilViewmodelCameraRotation.currentPosition.z += cameraYaw * recoilViewmodelCameraMask.z;
    }

    public void AddBayonetViewmodelCameraOffset(float fling_x, float fling_y, float fling_z)
    {
        bayonetViewmodelCameraOffset.x += fling_x;
        bayonetViewmodelCameraOffset.y += fling_y;
        bayonetViewmodelCameraOffset.z += fling_z;
    }

    /// <summary>
    /// At this point camera is already being shook, we just add some of the same shake to viewmodel for secondary motion.
    /// </summary>
    internal void FlinchFromExplosion(Vector3 worldRotationAxis, float adjustedMagnitudeDegrees)
    {
        Vector3 axis = viewmodelCameraTransform.InverseTransformDirection(worldRotationAxis);
        adjustedMagnitudeDegrees *= 0.25f;
        viewmodelTargetExplosionLocalRotation.currentRotation *= Quaternion.AngleAxis(adjustedMagnitudeDegrees, axis);
    }

    private void onLifeUpdated(bool isDead)
    {
        if (gesture != 0)
        {
            if (gesture == EPlayerGesture.INVENTORY_START)
            {
                stop("Gesture_Inventory");
            }
            else if (gesture == EPlayerGesture.SURRENDER_START)
            {
                stop("Gesture_Surrender");
            }
            else if (gesture == EPlayerGesture.ARREST_START)
            {
                stop("Gesture_Arrest");
            }
            else if (gesture == EPlayerGesture.REST_START)
            {
                stop("Gesture_Rest");
            }
            captorID = CSteamID.Nil;
            captorItem = 0;
            captorStrength = 0;
            _gesture = EPlayerGesture.NONE;
            onGestureUpdated?.Invoke(gesture);
        }
        if (base.channel.IsLocalPlayer)
        {
            UpdateLocalPlayerModelVisibility(isDead, base.player.look.perspective, base.player.quests.IsCutsceneModeActive());
            return;
        }
        if (!Dedicator.IsDedicatedServer && !isHiddenWaitingForClothing)
        {
            if (thirdRenderer_0 != null)
            {
                thirdRenderer_0.enabled = !isDead;
            }
            if (thirdRenderer_1 != null)
            {
                thirdRenderer_1.enabled = !isDead;
            }
        }
        thirdSkeleton.gameObject.SetActive(!isDead);
    }

    /// <summary>
    /// Called by clothing to make mesh renderers visible.
    /// </summary>
    public void NotifyClothingIsVisible()
    {
        isHiddenWaitingForClothing = false;
        if (!base.channel.IsLocalPlayer && !Dedicator.IsDedicatedServer && base.player.life.IsAlive)
        {
            if (thirdRenderer_0 != null)
            {
                thirdRenderer_0.enabled = true;
            }
            if (thirdRenderer_1 != null)
            {
                thirdRenderer_1.enabled = true;
            }
            thirdSkeleton.gameObject.SetActive(value: true);
        }
    }

    [Obsolete]
    public void tellLean(CSteamID steamID, byte newLean)
    {
        ReceiveLean(newLean);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellLean")]
    public void ReceiveLean(byte newLean)
    {
        _lean = newLean - 1;
    }

    [Obsolete]
    public void tellGesture(CSteamID steamID, byte id)
    {
        ReceiveGesture((EPlayerGesture)id);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellGesture")]
    public void ReceiveGesture(EPlayerGesture newGesture)
    {
        if (newGesture == EPlayerGesture.INVENTORY_START && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Inventory", smooth: true);
            _gesture = EPlayerGesture.INVENTORY_START;
        }
        else if (newGesture == EPlayerGesture.INVENTORY_STOP && gesture == EPlayerGesture.INVENTORY_START)
        {
            stop("Gesture_Inventory");
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.PICKUP)
        {
            play("Gesture_Pickup", smooth: false);
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.PUNCH_LEFT)
        {
            play("Punch_Left", smooth: false);
            _gesture = EPlayerGesture.NONE;
            if (!Dedicator.IsDedicatedServer)
            {
                base.player.equipment.PlayPunchAudioClip();
            }
        }
        else if (newGesture == EPlayerGesture.PUNCH_RIGHT)
        {
            play("Punch_Right", smooth: false);
            _gesture = EPlayerGesture.NONE;
            if (!Dedicator.IsDedicatedServer)
            {
                base.player.equipment.PlayPunchAudioClip();
            }
        }
        else if (newGesture == EPlayerGesture.SURRENDER_START && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Surrender", smooth: true);
            _gesture = EPlayerGesture.SURRENDER_START;
        }
        else if (newGesture == EPlayerGesture.SURRENDER_STOP && gesture == EPlayerGesture.SURRENDER_START)
        {
            stop("Gesture_Surrender");
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.REST_START && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Rest", smooth: true);
            _gesture = EPlayerGesture.REST_START;
        }
        else if (newGesture == EPlayerGesture.REST_STOP && gesture == EPlayerGesture.REST_START)
        {
            stop("Gesture_Rest");
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.ARREST_START)
        {
            play("Gesture_Arrest", smooth: true);
            _gesture = EPlayerGesture.ARREST_START;
        }
        else if (newGesture == EPlayerGesture.ARREST_STOP && gesture == EPlayerGesture.ARREST_START)
        {
            stop("Gesture_Arrest");
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.POINT && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Point", smooth: false);
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.WAVE && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Wave", smooth: false);
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.SALUTE && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Salute", smooth: false);
            _gesture = EPlayerGesture.NONE;
        }
        else if (newGesture == EPlayerGesture.FACEPALM && gesture == EPlayerGesture.NONE)
        {
            play("Gesture_Facepalm", smooth: false);
            _gesture = EPlayerGesture.NONE;
        }
        onGestureUpdated?.Invoke(gesture);
    }

    [Obsolete]
    public void askGesture(CSteamID steamID, byte id)
    {
        ReceiveGestureRequest((EPlayerGesture)id);
    }

    /// <summary>
    /// Rate limit is relatively high because this RPC handles open/close inventory notification.
    /// </summary>
    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 15, legacyName = "askGesture")]
    public void ReceiveGestureRequest(EPlayerGesture newGesture)
    {
        if (newGesture == EPlayerGesture.INVENTORY_STOP && base.player.inventory.isStoring && base.player.inventory.shouldInventoryStopGestureCloseStorage)
        {
            base.player.inventory.closeStorage();
        }
        if (gesture != EPlayerGesture.ARREST_START && !base.player.equipment.HasValidUseable && base.player.stance.stance != EPlayerStance.PRONE && base.player.stance.stance != EPlayerStance.DRIVING && base.player.stance.stance != EPlayerStance.SITTING && (newGesture == EPlayerGesture.INVENTORY_START || newGesture == EPlayerGesture.INVENTORY_STOP || newGesture == EPlayerGesture.SURRENDER_START || newGesture == EPlayerGesture.SURRENDER_STOP || newGesture == EPlayerGesture.POINT || newGesture == EPlayerGesture.WAVE || newGesture == EPlayerGesture.SALUTE || newGesture == EPlayerGesture.FACEPALM || newGesture == EPlayerGesture.REST_START || newGesture == EPlayerGesture.REST_STOP))
        {
            bool flag = newGesture != EPlayerGesture.INVENTORY_START && newGesture != EPlayerGesture.INVENTORY_STOP;
            sendGesture(newGesture, flag);
            if (!flag && onInventoryGesture != null)
            {
                onInventoryGesture(newGesture == EPlayerGesture.INVENTORY_START);
            }
        }
    }

    public void sendGesture(EPlayerGesture gesture, bool all)
    {
        if (!Dedicator.IsDedicatedServer && gesture == EPlayerGesture.INVENTORY_STOP && base.player.inventory.isStoring && base.player.inventory.shouldInventoryStopGestureCloseStorage)
        {
            base.player.inventory.closeStorage();
        }
        if (Provider.isServer)
        {
            if (gesture == EPlayerGesture.REST_START && base.player.stance.stance != EPlayerStance.CROUCH)
            {
                if (base.player.stance.stance != EPlayerStance.STAND && base.player.stance.stance != EPlayerStance.PRONE)
                {
                    return;
                }
                base.player.stance.checkStance(EPlayerStance.CROUCH, all: true);
                if (base.player.stance.stance != EPlayerStance.CROUCH)
                {
                    return;
                }
            }
            if (all)
            {
                SendGesture.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), gesture);
            }
            else
            {
                SendGesture.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GatherRemoteClientConnectionsExcludingOwner(), gesture);
            }
            PlayerAnimator.OnGestureChanged_Global?.TryInvoke("OnGestureChanged_Global", this, gesture);
        }
        else if (gesture == EPlayerGesture.INVENTORY_STOP || (!base.player.equipment.HasValidUseable && base.player.stance.stance != EPlayerStance.PRONE && base.player.stance.stance != EPlayerStance.DRIVING && base.player.stance.stance != EPlayerStance.SITTING))
        {
            SendGestureRequest.Invoke(GetNetId(), ENetReliability.Reliable, gesture);
        }
    }

    private void updateState(CharacterAnimator charAnim)
    {
        if (base.player.movement.isMoving)
        {
            if (base.player.stance.stance == EPlayerStance.CLIMB)
            {
                charAnim.state("Move_Climb");
            }
            else if (base.player.stance.stance == EPlayerStance.SWIM)
            {
                charAnim.state("Move_Swim");
            }
            else if (base.player.stance.stance == EPlayerStance.SPRINT)
            {
                charAnim.state("Move_Run");
            }
            else if (base.player.stance.stance == EPlayerStance.STAND)
            {
                charAnim.state("Move_Walk");
            }
            else if (base.player.stance.stance == EPlayerStance.CROUCH)
            {
                charAnim.state("Move_Crouch");
            }
            else if (base.player.stance.stance == EPlayerStance.PRONE)
            {
                charAnim.state("Move_Prone");
            }
        }
        else if (base.player.stance.stance == EPlayerStance.DRIVING)
        {
            if (base.player.movement.getVehicle() != null && base.player.movement.getVehicle().asset.hasZip)
            {
                charAnim.state("Idle_Zip");
            }
            else if (base.player.movement.getVehicle() != null && base.player.movement.getVehicle().asset.hasBicycle)
            {
                charAnim.state("Idle_Bicycle");
                charAnim.setAnimationSpeed("Idle_Bicycle", base.player.movement.getVehicle().ReplicatedForwardVelocity * base.player.movement.getVehicle().asset.bicycleAnimSpeed);
            }
            else if (base.player.movement.getVehicle() != null && base.player.movement.getVehicle().asset.isReclined)
            {
                charAnim.state("Idle_Reclined");
            }
            else
            {
                charAnim.state("Idle_Drive");
            }
        }
        else if (base.player.stance.stance == EPlayerStance.SITTING)
        {
            if (base.player.movement.getVehicle() != null && base.player.movement.getVehicle().passengers[base.player.movement.getSeat()].turret != null)
            {
                charAnim.state("Idle_Drive");
            }
            else
            {
                charAnim.state("Idle_Sit");
            }
        }
        else if (base.player.stance.stance == EPlayerStance.CLIMB)
        {
            charAnim.state("Idle_Climb");
        }
        else if (base.player.stance.stance == EPlayerStance.SWIM)
        {
            charAnim.state("Idle_Swim");
        }
        else if (base.player.stance.stance == EPlayerStance.STAND || base.player.stance.stance == EPlayerStance.SPRINT)
        {
            charAnim.state("Idle_Stand");
        }
        else if (base.player.stance.stance == EPlayerStance.CROUCH)
        {
            charAnim.state("Idle_Crouch");
        }
        else if (base.player.stance.stance == EPlayerStance.PRONE)
        {
            charAnim.state("Idle_Prone");
        }
    }

    private void updateHuman(HumanAnimator humanAnim)
    {
        humanAnim.lean = (base.player.channel.owner.IsLeftHanded ? (-lean) : lean);
        if (base.player.stance.stance == EPlayerStance.DRIVING || base.player.stance.stance == EPlayerStance.SITTING)
        {
            humanAnim.pitch = 90f;
        }
        else
        {
            humanAnim.pitch = base.player.look.pitch;
        }
        if (base.player.stance.stance == EPlayerStance.CROUCH)
        {
            humanAnim.offset = 0.1f;
        }
        else if (base.player.stance.stance == EPlayerStance.PRONE)
        {
            humanAnim.offset = 0.2f;
        }
        else
        {
            humanAnim.offset = 0f;
        }
        if (!base.channel.IsLocalPlayer && Provider.isServer)
        {
            humanAnim.force();
        }
    }

    private void onLanded(float velocity)
    {
        if (velocity < 0f)
        {
            velocity = ((!(base.player.movement.totalGravityMultiplier < 0.67f)) ? Mathf.Max(velocity, -30f) : Mathf.Max(velocity, -5f));
            viewmodelCameraMovementLocalRotation.currentPosition.x = velocity * -0.5f;
        }
    }

    private bool isLeanSpaceEmpty(Vector3 direction)
    {
        Vector3 vector = base.transform.position + base.transform.up * base.player.look.heightLook;
        float rADIUS = PlayerStance.RADIUS;
        float num = 1.2f - rADIUS;
        Vector3 point = vector + direction * num;
        return Physics.OverlapCapsuleNonAlloc(vector, point, rADIUS, leanHits, RayMasks.BLOCK_LEAN) == 0;
    }

    private bool ShouldSnapLeanRotationToZero()
    {
        if (leanObstructed)
        {
            return true;
        }
        if (_lean == 1)
        {
            leanObstructed = !isLeanSpaceEmpty(-base.transform.right);
        }
        else if (_lean == -1)
        {
            leanObstructed = !isLeanSpaceEmpty(base.transform.right);
        }
        return leanObstructed;
    }

    public void simulate(uint simulation, bool inputLeanLeft, bool inputLeanRight)
    {
        if (base.player.stance.stance != 0 && base.player.stance.stance != EPlayerStance.SPRINT && base.player.stance.stance != EPlayerStance.DRIVING && base.player.stance.stance != EPlayerStance.SITTING)
        {
            if (inputLeanLeft)
            {
                if (isLeanSpaceEmpty(-base.transform.right))
                {
                    _lean = 1;
                    leanObstructed = false;
                }
                else
                {
                    _lean = 0;
                    leanObstructed = true;
                }
            }
            else if (inputLeanRight)
            {
                if (isLeanSpaceEmpty(base.transform.right))
                {
                    _lean = -1;
                    leanObstructed = false;
                }
                else
                {
                    _lean = 0;
                    leanObstructed = true;
                }
            }
            else
            {
                _lean = 0;
                leanObstructed = false;
            }
        }
        else
        {
            _lean = 0;
            leanObstructed = false;
        }
        if (lastLean == lean)
        {
            return;
        }
        lastLean = lean;
        if (!Provider.isServer)
        {
            return;
        }
        if ((lean == -1 || lean == 1) && captorStrength > 0)
        {
            captorStrength--;
            if (captorStrength == 0)
            {
                captorID = CSteamID.Nil;
                captorItem = 0;
                sendGesture(EPlayerGesture.ARREST_STOP, all: true);
                EffectAsset effectAsset = Metal_1_Ref.Find();
                if (effectAsset != null)
                {
                    TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                    parameters.relevantDistance = EffectManager.MEDIUM;
                    parameters.position = base.transform.position;
                    EffectManager.triggerEffect(parameters);
                }
            }
        }
        SendLean.Invoke(GetNetId(), ENetReliability.Unreliable, base.channel.GatherRemoteClientConnectionsExcludingOwner(), (byte)(lean + 1));
        PlayerAnimator.OnLeanChanged_Global.TryInvoke("OnLeanChanged_Global", this);
    }

    [Obsolete]
    public void askEmote(CSteamID steamID)
    {
    }

    internal void SendInitialPlayerState(SteamPlayer client)
    {
        if (gesture != 0)
        {
            SendGesture.Invoke(GetNetId(), ENetReliability.Reliable, client.transportConnection, gesture);
        }
    }

    internal void SendInitialPlayerState(List<ITransportConnection> transportConnections)
    {
        if (gesture != 0)
        {
            SendGesture.Invoke(GetNetId(), ENetReliability.Reliable, transportConnections, gesture);
        }
    }

    /// <summary>
    /// Nelson 2024-03-20: Adding this method because (at the time of writing) first and third-person renderers
    /// and skeletons are activated/enabled in InitializePlayer, onPerspectiveUpdated, and onLifeUpdated, and I
    /// want them to be consistent with the addition of the new NPC Cutscene Mode option.
    /// </summary>
    private void UpdateLocalPlayerModelVisibility(bool isDead, EPlayerPerspective perspective, bool isCutsceneModeActive)
    {
        bool flag = !isDead && perspective == EPlayerPerspective.FIRST && !isCutsceneModeActive;
        bool flag2 = !isDead && perspective == EPlayerPerspective.THIRD;
        if (!hasCalledUpdateLocalPlayerModelVisibility || wasLocalPlayerFirstPersonModelVisible != flag)
        {
            wasLocalPlayerFirstPersonModelVisible = flag;
            if (firstRenderer_0 != null)
            {
                firstRenderer_0.enabled = flag;
            }
            firstSkeleton.gameObject.SetActive(flag);
        }
        if (!hasCalledUpdateLocalPlayerModelVisibility || wasLocalPlayerThirdPersonModelVisible != flag2)
        {
            wasLocalPlayerThirdPersonModelVisible = flag2;
            if (thirdRenderer_0 != null)
            {
                thirdRenderer_0.enabled = flag2;
            }
            if (thirdRenderer_1 != null)
            {
                thirdRenderer_1.enabled = flag2;
            }
            thirdSkeleton.gameObject.SetActive(flag2);
        }
        hasCalledUpdateLocalPlayerModelVisibility = true;
    }

    internal void NotifyLocalPlayerCutsceneModeActiveChanged(bool isCutsceneModeActive)
    {
        UpdateLocalPlayerModelVisibility(base.player.life.isDead, base.player.look.perspective, isCutsceneModeActive);
    }

    private void onPerspectiveUpdated(EPlayerPerspective newPerspective)
    {
        UpdateLocalPlayerModelVisibility(base.player.life.isDead, newPerspective, base.player.quests.IsCutsceneModeActive());
    }

    private void Update()
    {
        float aimingInertaMultiplier;
        if (base.channel.IsLocalPlayer)
        {
            if (!PlayerUI.window.showCursor)
            {
                if (!base.player.look.IsControllingFreecam)
                {
                    if (ControlsSettings.leaning == EControlMode.TOGGLE)
                    {
                        if (InputEx.GetKeyDown(ControlsSettings.leanLeft))
                        {
                            if (base.player.look.perspective == EPlayerPerspective.FIRST || side)
                            {
                                if (leanLeft)
                                {
                                    inputWantsToLeanLeft = false;
                                    inputWantsToLeanRight = false;
                                }
                                else
                                {
                                    inputWantsToLeanLeft = true;
                                    inputWantsToLeanRight = false;
                                }
                            }
                            if (!side && leanRight)
                            {
                                inputWantsToLeanLeft = false;
                                inputWantsToLeanRight = false;
                            }
                            inputWantsThirdPersonCameraOnLeftSide = true;
                        }
                        if (InputEx.GetKeyDown(ControlsSettings.leanRight))
                        {
                            if (base.player.look.perspective == EPlayerPerspective.FIRST || !side)
                            {
                                if (leanRight)
                                {
                                    inputWantsToLeanLeft = false;
                                    inputWantsToLeanRight = false;
                                }
                                else
                                {
                                    inputWantsToLeanLeft = false;
                                    inputWantsToLeanRight = true;
                                }
                            }
                            if (side && leanLeft)
                            {
                                inputWantsToLeanLeft = false;
                                inputWantsToLeanRight = false;
                            }
                            inputWantsThirdPersonCameraOnLeftSide = false;
                        }
                    }
                    else
                    {
                        if (InputEx.GetKeyDown(ControlsSettings.leanLeft))
                        {
                            inputWantsThirdPersonCameraOnLeftSide = true;
                            lastCameraSideInputRealtime = Time.realtimeSinceStartup;
                        }
                        if (InputEx.GetKeyDown(ControlsSettings.leanRight))
                        {
                            inputWantsThirdPersonCameraOnLeftSide = false;
                            lastCameraSideInputRealtime = Time.realtimeSinceStartup;
                        }
                        if (InputEx.GetKey(ControlsSettings.leanLeft))
                        {
                            if (base.player.look.perspective == EPlayerPerspective.FIRST || Time.realtimeSinceStartup - lastCameraSideInputRealtime > 0.075f)
                            {
                                inputWantsToLeanLeft = true;
                            }
                            else
                            {
                                inputWantsToLeanLeft = false;
                            }
                        }
                        else
                        {
                            inputWantsToLeanLeft = false;
                        }
                        if (InputEx.GetKey(ControlsSettings.leanRight))
                        {
                            if (base.player.look.perspective == EPlayerPerspective.FIRST || Time.realtimeSinceStartup - lastCameraSideInputRealtime > 0.075f)
                            {
                                inputWantsToLeanRight = true;
                            }
                            else
                            {
                                inputWantsToLeanRight = false;
                            }
                        }
                        else
                        {
                            inputWantsToLeanRight = false;
                        }
                    }
                }
            }
            else
            {
                inputWantsToLeanLeft = false;
                inputWantsToLeanRight = false;
            }
            if (firstAnimator != null)
            {
                if (firstAnimator.getAnimationPlaying())
                {
                    firstAnimator.state("Idle_Stand");
                }
                else
                {
                    updateState(firstAnimator);
                }
            }
            if (thirdAnimator != null)
            {
                updateState(thirdAnimator);
                updateHuman((HumanAnimator)thirdAnimator);
            }
            blendedViewmodelSwayMultiplier = Mathf.Lerp(blendedViewmodelSwayMultiplier, viewmodelSwayMultiplier, 16f * Time.deltaTime);
            blendedViewmodelOffsetPreferenceMultiplier = Mathf.Lerp(blendedViewmodelOffsetPreferenceMultiplier, viewmodelOffsetPreferenceMultiplier, 16f * Time.deltaTime);
            if (base.player.movement.isMoving)
            {
                viewmodelMovementOffset.targetPosition.x = Mathf.Sin(speed * Time.time) * bob;
                viewmodelMovementOffset.targetPosition.y = Mathf.Abs(viewmodelMovementOffset.targetPosition.x);
            }
            else
            {
                viewmodelMovementOffset.targetPosition = Vector2.zero;
            }
            viewmodelMovementOffset.Update(Time.deltaTime);
            GetAimingViewmodelAlignment(out var aimingAlignmentOffset, out aimingInertaMultiplier);
            blendedViewmodelCameraLocalPositionOffset = Vector3.Lerp(blendedViewmodelCameraLocalPositionOffset, viewmodelCameraLocalPositionOffset - recoilViewmodelCameraOffset.currentPosition - bayonetViewmodelCameraOffset, 16f * Time.deltaTime);
            recoilViewmodelCameraOffset.Update(Time.deltaTime);
            bayonetViewmodelCameraOffset = Vector3.Lerp(bayonetViewmodelCameraOffset, Vector3.zero, 16f * Time.deltaTime);
            desiredViewmodelCameraLocalPosition.x = 0f - viewmodelMovementOffset.currentPosition.y - blendedViewmodelCameraLocalPositionOffset.y;
            desiredViewmodelCameraLocalPosition.y = viewmodelMovementOffset.currentPosition.x + blendedViewmodelCameraLocalPositionOffset.x;
            desiredViewmodelCameraLocalPosition.z = blendedViewmodelCameraLocalPositionOffset.z;
            desiredViewmodelCameraLocalPosition.x += Provider.preferenceData.Viewmodel.Offset_Vertical * blendedViewmodelOffsetPreferenceMultiplier;
            desiredViewmodelCameraLocalPosition.y += Provider.preferenceData.Viewmodel.Offset_Horizontal * blendedViewmodelOffsetPreferenceMultiplier;
            desiredViewmodelCameraLocalPosition.z -= Provider.preferenceData.Viewmodel.Offset_Depth * blendedViewmodelOffsetPreferenceMultiplier;
            if (base.player.stance.stance == EPlayerStance.DRIVING)
            {
                viewmodelCameraLocalPosition.x = Mathf.Lerp(viewmodelCameraLocalPosition.x, 0f - drivingViewmodelCameraLocalPositionOffset.y - 0.65f - Mathf.Abs(base.player.look.yaw) / 90f * 0.25f, 8f * Time.deltaTime);
                viewmodelCameraLocalPosition.y = Mathf.Lerp(viewmodelCameraLocalPosition.y, drivingViewmodelCameraLocalPositionOffset.x + (float)((!base.channel.owner.IsLeftHanded) ? 1 : (-1)) * base.player.movement.getVehicle().AnimatedSteeringAngle * -0.01f, 8f * Time.deltaTime);
                viewmodelCameraLocalPosition.z = Mathf.Lerp(viewmodelCameraLocalPosition.z, drivingViewmodelCameraLocalPositionOffset.z - 0.25f, 8f * Time.deltaTime);
            }
            else
            {
                viewmodelCameraLocalPosition.x = desiredViewmodelCameraLocalPosition.x - 0.45f;
                viewmodelCameraLocalPosition.y = desiredViewmodelCameraLocalPosition.y;
                viewmodelCameraLocalPosition.z = desiredViewmodelCameraLocalPosition.z;
            }
            AddNearDeathViewmodelShake(ref viewmodelCameraLocalPosition);
            viewmodelCameraTransform.localPosition = viewmodelCameraLocalPosition + aimingAlignmentOffset;
            if (base.player.movement.isMoving)
            {
                viewmodelCameraMovementLocalRotation.targetPosition.x = base.player.movement.move.z * tilt * viewmodelSwayMultiplier + roll * viewmodelSwayMultiplier;
                viewmodelCameraMovementLocalRotation.targetPosition.y = base.player.movement.move.x * tilt + roll * viewmodelSwayMultiplier;
            }
            else
            {
                viewmodelCameraMovementLocalRotation.targetPosition = Vector2.zero;
            }
            if (!base.player.movement.isGrounded)
            {
                viewmodelCameraMovementLocalRotation.targetPosition.x -= 5f;
            }
            viewmodelCameraMovementLocalRotation.Update(Time.deltaTime);
            viewmodelCameraLocalRotation.x = viewmodelCameraMovementLocalRotation.currentPosition.x;
            viewmodelCameraLocalRotation.y = 0f;
            viewmodelCameraLocalRotation.z = viewmodelCameraMovementLocalRotation.currentPosition.y;
            viewmodelCameraLocalRotation += recoilViewmodelCameraRotation.currentPosition;
            recoilViewmodelCameraRotation.Update(Time.deltaTime);
            float num = Mathf.DeltaAngle(base.player.look.pitch, lastFramePitchInput);
            lastFramePitchInput = base.player.look.pitch;
            float num2 = Mathf.DeltaAngle(base.player.look.yaw, lastFrameYawInput);
            lastFrameYawInput = base.player.look.yaw;
            rotationInputViewmodelRoll.Update(Time.deltaTime);
            rotationInputViewmodelRoll.currentPosition.x += num * -0.03f * viewmodelSwayMultiplier;
            rotationInputViewmodelRoll.currentPosition.y += num2 * -0.015f * viewmodelSwayMultiplier;
            rotationInputViewmodelRoll.currentPosition.z += num2 * -0.05f;
            rotationInputViewmodelRoll.currentPosition = MathfEx.Clamp(rotationInputViewmodelRoll.currentPosition, -10f, 10f);
            viewmodelCameraLocalRotation += rotationInputViewmodelRoll.currentPosition;
            viewmodelItemInertiaRotation.Update(Time.deltaTime);
            if (base.player.look.perspective == EPlayerPerspective.FIRST && base.player.equipment.firstModel != null)
            {
                ItemAsset asset = base.player.equipment.asset;
                if (asset != null && asset.shouldProcedurallyAnimateInertia)
                {
                    Vector3 vector = viewmodelParentTransform.transform.InverseTransformPoint(base.player.equipment.firstModel.position);
                    if (lastFrameHadItemPosition)
                    {
                        Vector3 vector2 = vector - lastFrameItemPosition;
                        viewmodelItemInertiaRotation.currentPosition.x += vector2.y * viewmodelItemInertiaMask.x;
                        viewmodelItemInertiaRotation.currentPosition.y += vector2.x * viewmodelItemInertiaMask.y;
                        viewmodelItemInertiaRotation.currentPosition.z += vector2.x * viewmodelItemInertiaMask.z;
                    }
                    lastFrameItemPosition = vector;
                    lastFrameHadItemPosition = true;
                    goto IL_0986;
                }
            }
            lastFrameHadItemPosition = false;
            goto IL_0986;
        }
        if (thirdAnimator != null)
        {
            updateState(thirdAnimator);
            updateHuman((HumanAnimator)thirdAnimator);
        }
        goto IL_0cdc;
        IL_0986:
        viewmodelItemInertiaRotation.currentPosition = MathfEx.Clamp(viewmodelItemInertiaRotation.currentPosition, -5f, 5f);
        viewmodelCameraLocalRotation += viewmodelItemInertiaRotation.currentPosition * aimingInertaMultiplier;
        viewmodelSmoothedExplosionLocalRotation = Quaternion.Lerp(viewmodelSmoothedExplosionLocalRotation, viewmodelTargetExplosionLocalRotation.currentRotation, viewmodelExplosionSmoothingSpeed * Time.deltaTime);
        viewmodelTargetExplosionLocalRotation.Update(Time.deltaTime);
        if (base.player.stance.stance == EPlayerStance.DRIVING)
        {
            viewmodelCameraTransform.localRotation = Quaternion.Lerp(viewmodelCameraTransform.localRotation, Quaternion.Euler(base.player.look.yaw * 60f / MainCamera.instance.fieldOfView * (float)(base.channel.owner.IsLeftHanded ? 1 : (-1)), (base.player.look.pitch - 90f) * 60f / MainCamera.instance.fieldOfView, 90f + base.player.movement.getVehicle().AnimatedSteeringAngle * (float)((!base.channel.owner.IsLeftHanded) ? 1 : (-1))), 8f * Time.deltaTime);
        }
        else if (base.player.stance.stance == EPlayerStance.CLIMB)
        {
            viewmodelCameraTransform.localRotation = Quaternion.Lerp(viewmodelCameraTransform.localRotation, Quaternion.Euler(0f, (base.player.look.pitch - 90f) * 60f / MainCamera.instance.fieldOfView, 90f), 8f * Time.deltaTime);
        }
        else
        {
            viewmodelCameraTransform.localRotation = viewmodelTargetExplosionLocalRotation.currentRotation * Quaternion.Euler(viewmodelCameraLocalRotation.y, 0f - viewmodelCameraLocalRotation.x, viewmodelCameraLocalRotation.z + 90f);
        }
        if (ShouldSnapLeanRotationToZero())
        {
            base.player.first.transform.localRotation = Quaternion.identity;
        }
        else
        {
            base.player.first.transform.localRotation = Quaternion.Lerp(base.player.first.transform.localRotation, Quaternion.Euler(0f, 0f, (float)lean * HumanAnimator.LEAN), 4f * Time.deltaTime);
        }
        viewmodelCamera.fieldOfView = Mathf.Lerp(Provider.preferenceData.Viewmodel.Field_Of_View_Aim, Provider.preferenceData.Viewmodel.Field_Of_View_Hip, blendedViewmodelOffsetPreferenceMultiplier);
        if (Provider.modeConfigData.Gameplay.Allow_Shoulder_Camera)
        {
            _shoulder = Mathf.Lerp(shoulder, (!side) ? 1 : (-1), 8f * Time.deltaTime);
        }
        else
        {
            _shoulder = 0f;
        }
        _shoulder2 = Mathf.Lerp(shoulder2, -lean, 8f * Time.deltaTime);
        goto IL_0cdc;
        IL_0cdc:
        if (characterAnimator != null)
        {
            updateState(characterAnimator);
            updateHuman(characterAnimator);
        }
    }

    /// <summary>
    /// 2023-01-18: Viewmodel camera position was originally set during Update (and still is for compatibility),
    /// but for aiming alignment that uses the previous frame's animation position, so we also modify during
    /// LateUpdate to use this frame's animation position.
    /// </summary>
    private void LateUpdate()
    {
        if (base.channel.IsLocalPlayer)
        {
            GetAimingViewmodelAlignment(out var aimingAlignmentOffset, out var _);
            viewmodelCameraTransform.localPosition = viewmodelCameraLocalPosition + aimingAlignmentOffset;
        }
    }

    internal void InitializePlayer()
    {
        isHiddenWaitingForClothing = true;
        if (base.channel.IsLocalPlayer)
        {
            if (base.player.first != null)
            {
                viewmodelParentTransform = new GameObject().transform;
                viewmodelParentTransform.name = "View";
                viewmodelParentTransform.transform.localPosition = Vector3.zero;
                firstAnimator = MainCamera.instance.transform.Find("Viewmodel").GetComponent<CharacterAnimator>();
                Vector3 localPosition = firstAnimator.transform.localPosition;
                Quaternion localRotation = firstAnimator.transform.localRotation;
                firstAnimator.transform.parent = viewmodelParentTransform;
                firstAnimator.transform.localPosition = localPosition;
                firstAnimator.transform.localRotation = localRotation;
                firstAnimator.transform.localScale = new Vector3((!base.channel.owner.IsLeftHanded) ? 1 : (-1), 1f, 1f);
                firstRenderer_0 = (SkinnedMeshRenderer)firstAnimator.transform.Find("Model_0").GetComponent<Renderer>();
                _firstSkeleton = firstAnimator.transform.Find("Skeleton");
            }
            if (base.player.third != null)
            {
                thirdAnimator = base.player.third.GetComponent<CharacterAnimator>();
                thirdAnimator.transform.localScale = new Vector3((!base.channel.owner.IsLeftHanded) ? 1 : (-1), 1f, 1f);
                thirdRenderer_0 = (SkinnedMeshRenderer)thirdAnimator.transform.Find("Model_0").GetComponent<Renderer>();
                thirdRenderer_1 = (SkinnedMeshRenderer)thirdAnimator.transform.Find("Model_1").GetComponent<Renderer>();
                _thirdSkeleton = thirdAnimator.transform.Find("Skeleton");
                thirdSkeleton.Find("Spine").GetComponent<Collider>().enabled = false;
                thirdSkeleton.Find("Spine").Find("Skull").GetComponent<Collider>()
                    .enabled = false;
                thirdSkeleton.Find("Spine").Find("Left_Shoulder").Find("Left_Arm")
                    .GetComponent<Collider>()
                    .enabled = false;
                thirdSkeleton.Find("Spine").Find("Right_Shoulder").Find("Right_Arm")
                    .GetComponent<Collider>()
                    .enabled = false;
                thirdSkeleton.Find("Left_Hip").Find("Left_Leg").GetComponent<Collider>()
                    .enabled = false;
                thirdSkeleton.Find("Right_Hip").Find("Right_Leg").GetComponent<Collider>()
                    .enabled = false;
            }
            if (Provider.cameraMode == ECameraMode.THIRD)
            {
                UpdateLocalPlayerModelVisibility(isDead: false, EPlayerPerspective.THIRD, base.player.quests.IsCutsceneModeActive());
            }
            else
            {
                UpdateLocalPlayerModelVisibility(isDead: false, EPlayerPerspective.FIRST, base.player.quests.IsCutsceneModeActive());
            }
            viewmodelCameraTransform = firstSkeleton.Find("Spine").Find("Skull").Find("ViewmodelCamera");
            viewmodelCamera = viewmodelCameraTransform.GetComponent<Camera>();
            UnturnedPostProcess.instance.setOverlayCamera(viewmodelCamera);
            viewmodelCameraLocalPositionOffset = Vector3.zero;
            drivingViewmodelCameraLocalPositionOffset = Vector3.zero;
            scopeSway = Vector3.zero;
            bayonetViewmodelCameraOffset = Vector3.zero;
            viewmodelCameraLocalPosition = Vector3.zero;
            viewmodelTargetExplosionLocalRotation.currentRotation = Quaternion.identity;
            viewmodelTargetExplosionLocalRotation.targetRotation = Quaternion.identity;
            blendedViewmodelSwayMultiplier = 1f;
            viewmodelSwayMultiplier = 1f;
            blendedViewmodelOffsetPreferenceMultiplier = 1f;
            viewmodelOffsetPreferenceMultiplier = 1f;
            if (base.player.character != null)
            {
                characterAnimator = base.player.character.GetComponent<HumanAnimator>();
                characterAnimator.transform.localScale = new Vector3((!base.channel.owner.IsLeftHanded) ? 1 : (-1), 1f, 1f);
            }
            PlayerMovement movement = base.player.movement;
            movement.onLanded = (Landed)Delegate.Combine(movement.onLanded, new Landed(onLanded));
            inputWantsThirdPersonCameraOnLeftSide = base.player.channel.owner.IsLeftHanded;
            PlayerLook look = base.player.look;
            look.onPerspectiveUpdated = (PerspectiveUpdated)Delegate.Combine(look.onPerspectiveUpdated, new PerspectiveUpdated(onPerspectiveUpdated));
        }
        else if (base.player.third != null)
        {
            thirdAnimator = base.player.third.GetComponent<CharacterAnimator>();
            thirdAnimator.transform.localScale = new Vector3((!base.channel.owner.IsLeftHanded) ? 1 : (-1), 1f, 1f);
            if (!Dedicator.IsDedicatedServer)
            {
                thirdRenderer_0 = (SkinnedMeshRenderer)thirdAnimator.transform.Find("Model_0").GetComponent<Renderer>();
                thirdRenderer_1 = (SkinnedMeshRenderer)thirdAnimator.transform.Find("Model_1").GetComponent<Renderer>();
            }
            _thirdSkeleton = thirdAnimator.transform.Find("Skeleton");
        }
        if (Dedicator.IsDedicatedServer)
        {
            thirdSkeleton.gameObject.SetActive(value: true);
        }
        mixAnimation("Gesture_Inventory", mixLeftShoulder: true, mixRightShoulder: true, mixSkull: true);
        mixAnimation("Gesture_Pickup", mixLeftShoulder: false, mixRightShoulder: true);
        mixAnimation("Punch_Left", mixLeftShoulder: true, mixRightShoulder: false);
        mixAnimation("Punch_Right", mixLeftShoulder: false, mixRightShoulder: true);
        mixAnimation("Gesture_Point", mixLeftShoulder: false, mixRightShoulder: true);
        mixAnimation("Gesture_Surrender", mixLeftShoulder: true, mixRightShoulder: true);
        mixAnimation("Gesture_Arrest", mixLeftShoulder: true, mixRightShoulder: true);
        mixAnimation("Gesture_Wave", mixLeftShoulder: true, mixRightShoulder: true, mixSkull: true);
        mixAnimation("Gesture_Salute", mixLeftShoulder: false, mixRightShoulder: true);
        mixAnimation("Gesture_Rest");
        mixAnimation("Gesture_Facepalm", mixLeftShoulder: false, mixRightShoulder: true, mixSkull: true);
        PlayerLife life = base.player.life;
        life.onLifeUpdated = (LifeUpdated)Delegate.Combine(life.onLifeUpdated, new LifeUpdated(onLifeUpdated));
        if (Provider.isServer)
        {
            load();
        }
    }

    private void AddNearDeathViewmodelShake(ref Vector3 position)
    {
        if (base.player.life.health < 25)
        {
            Vector3 vector = new Vector3(UnityEngine.Random.Range(-0.005f, 0.005f), UnityEngine.Random.Range(-0.005f, 0.005f), UnityEngine.Random.Range(-0.005f, 0.005f));
            float num = 1f - (float)(int)Player.player.life.health / 25f;
            float num2 = 1f - base.player.skills.mastery(1, 3) * 0.75f;
            position += vector * num * num2;
        }
    }

    private void GetAimingViewmodelAlignment(out Vector3 aimingAlignmentOffset, out float aimingInertaMultiplier)
    {
        aimingAlignmentOffset = Vector3.zero;
        aimingInertaMultiplier = 1f;
        if (base.player.equipment.useable is UseableGun useableGun)
        {
            useableGun.GetAimingViewmodelAlignment(out var alignmentTransform, out var alignmentOffset, out var alpha);
            if (alignmentTransform != null && alpha > 0f)
            {
                Vector3 position = alignmentTransform.TransformPoint(alignmentOffset);
                aimingAlignmentOffset = viewmodelCameraTransform.parent.InverseTransformPoint(position);
                aimingAlignmentOffset.x += 0.45f;
                aimingAlignmentOffset *= alpha;
                aimingInertaMultiplier -= alpha;
            }
        }
    }

    public void load()
    {
        wasLoadCalled = true;
        if (PlayerSavedata.fileExists(base.channel.owner.playerID, "/Player/Anim.dat") && Level.info.type == ELevelType.SURVIVAL)
        {
            Block block = PlayerSavedata.readBlock(base.channel.owner.playerID, "/Player/Anim.dat", 0);
            byte num = block.readByte();
            _gesture = (EPlayerGesture)block.readByte();
            captorID = block.readSteamID();
            if (num > 1)
            {
                captorItem = block.readUInt16();
            }
            else
            {
                captorItem = 0;
            }
            captorStrength = block.readUInt16();
            if (gesture != EPlayerGesture.ARREST_START)
            {
                _gesture = EPlayerGesture.NONE;
            }
        }
        else
        {
            _gesture = EPlayerGesture.NONE;
            captorID = CSteamID.Nil;
            captorItem = 0;
            captorStrength = 0;
        }
    }

    public void save()
    {
        if (!wasLoadCalled)
        {
            return;
        }
        if (base.player.life.isDead)
        {
            if (PlayerSavedata.fileExists(base.channel.owner.playerID, "/Player/Anim.dat"))
            {
                PlayerSavedata.deleteFile(base.channel.owner.playerID, "/Player/Anim.dat");
            }
            return;
        }
        Block block = new Block();
        block.writeByte(SAVEDATA_VERSION);
        block.writeByte((byte)gesture);
        block.writeSteamID(captorID);
        block.writeUInt16(captorItem);
        block.writeUInt16(captorStrength);
        PlayerSavedata.writeBlock(base.channel.owner.playerID, "/Player/Anim.dat", block);
    }
}
