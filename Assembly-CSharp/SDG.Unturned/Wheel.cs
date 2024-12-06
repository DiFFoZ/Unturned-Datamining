using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class Wheel
{
    private InteractableVehicle _vehicle;

    private WheelCollider _wheel;

    public Transform model;

    public Quaternion rest;

    private ECrawlerTrackForwardMode crawlerTrackForwardMode;

    public bool isPowered;

    private VehicleWheelConfiguration config;

    /// <summary>
    /// Does this wheel affect brake torque?
    /// </summary>
    public bool hasBrakes;

    private bool _isGrounded;

    internal WheelHit mostRecentGroundHit;

    private bool _isAlive;

    public float stiffnessTractionMultiplier = 0.25f;

    public float stiffnessSideways = 1f;

    public float stiffnessForward = 2f;

    public float motorTorqueMultiplier = 1f;

    public float motorTorqueClampMultiplier = 0.5f;

    public float brakeTorqueMultiplier = 1f;

    public float brakeTorqueTractionMultiplier = 0.5f;

    private float latestLocalSteeringInput;

    private float latestLocalAccelerationInput;

    private bool latestLocalBrakingInput;

    private bool _isPhysical;

    /// <summary>
    /// [0.0, 1.0] normalized position of wheel along suspension.
    /// </summary>
    internal float replicatedSuspensionState;

    /// <summary>
    /// [0.0, 1.0] normalized position animated toward replicatedSuspensionState.
    /// </summary>
    private float animatedSuspensionState;

    /// <summary>
    /// Model position interpolated toward animatedSuspensionState according to modelSuspensionSpeed.
    /// </summary>
    private float animatedModelSuspension;

    internal PhysicsMaterialNetId replicatedGroundMaterial;

    /// <summary>
    /// [0, 360] angle of rotation around wheel axle. Measured in degrees because Quaternion.AngleAxis takes degrees.
    ///
    /// We track rather than using GetWorldPose so that we can alternate between using replicated and simulated
    /// results without snapping transforms.
    /// </summary>
    private float rollAngleDegrees;

    /// <summary>
    /// List is created if this wheel has a collider and uses collider pose. Null when vehicle is destroyed to
    /// prevent creation of more effects.
    /// </summary>
    private List<TireMotionEffectInstance> motionEffectInstances;

    /// <summary>
    /// Instance corresponding to current ground material. Doesn't necessarily mean the particle system is active.
    /// </summary>
    private TireMotionEffectInstance currentGroundEffect;

    private static List<TireMotionEffectInstance> motionEffectInstancesPool = new List<TireMotionEffectInstance>();

    private static readonly AssetReference<EffectAsset> Rubber_0_Ref = new AssetReference<EffectAsset>("a87c5007b22542dcbf3599ee3faceadd");

    public InteractableVehicle vehicle => _vehicle;

    public int index { get; private set; }

    public WheelCollider wheel => _wheel;

    public bool isGrounded => _isGrounded;

    public bool isAlive
    {
        get
        {
            return _isAlive;
        }
        set
        {
            if (isAlive != value)
            {
                _isAlive = value;
                if (model != null)
                {
                    model.gameObject.SetActive(isAlive);
                }
                UpdateColliderEnabled();
                triggerAliveChanged();
            }
        }
    }

    public bool IsDead => !_isAlive;

    /// <summary>
    /// Turn on/off physics as needed. Overridden by isAlive.
    /// </summary>
    public bool isPhysical
    {
        get
        {
            return _isPhysical;
        }
        set
        {
            _isPhysical = value;
            UpdateColliderEnabled();
        }
    }

    public event WheelAliveChangedHandler aliveChanged;

    private void triggerAliveChanged()
    {
        this.aliveChanged?.Invoke(this);
    }

    public void askRepair()
    {
        if (!isAlive)
        {
            isAlive = true;
            vehicle.sendTireAliveMaskUpdate();
        }
    }

    public void askDamage()
    {
        if (isAlive)
        {
            isAlive = false;
            vehicle.sendTireAliveMaskUpdate();
            EffectAsset effectAsset = Rubber_0_Ref.Find();
            if (effectAsset != null)
            {
                TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                parameters.relevantDistance = EffectManager.SMALL;
                parameters.position = wheel.transform.position;
                parameters.SetDirection(wheel.transform.up);
                EffectManager.triggerEffect(parameters);
            }
        }
    }

    private void UpdateColliderEnabled()
    {
        if (wheel != null)
        {
            wheel.gameObject.SetActive(isPhysical && isAlive);
        }
    }

    /// <summary>
    /// Called after construction and on all clients and server when a player stops driving.
    /// </summary>
    internal void Reset()
    {
        latestLocalSteeringInput = 0f;
        latestLocalAccelerationInput = 0f;
        latestLocalBrakingInput = false;
        if (wheel != null)
        {
            wheel.steerAngle = 0f;
            wheel.motorTorque = 0f;
            wheel.brakeTorque = vehicle.asset.brake * 0.25f;
            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = 0.25f;
            wheel.sidewaysFriction = sidewaysFriction;
            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.stiffness = 0.25f;
            wheel.forwardFriction = forwardFriction;
        }
    }

    /// <summary>
    /// Called when vehicles explodes.
    /// </summary>
    internal void Explode()
    {
        if (model == null || IsDead)
        {
            return;
        }
        Collider component = model.GetComponent<Collider>();
        if (!(component == null))
        {
            EffectManager.RegisterDebris(model.gameObject);
            model.transform.parent = null;
            component.enabled = true;
            Rigidbody orAddComponent = model.gameObject.GetOrAddComponent<Rigidbody>();
            orAddComponent.interpolation = RigidbodyInterpolation.Interpolate;
            orAddComponent.collisionDetectionMode = CollisionDetectionMode.Discrete;
            orAddComponent.drag = 0.5f;
            orAddComponent.angularDrag = 0.1f;
            UnityEngine.Object.Destroy(model.gameObject, 8f);
            if (index % 2 == 0)
            {
                orAddComponent.AddForce(-model.right * 512f + Vector3.up * 128f);
            }
            else
            {
                orAddComponent.AddForce(model.right * 512f + Vector3.up * 128f);
            }
        }
    }

    internal void UpdateGrounded()
    {
        if (!(wheel == null))
        {
            _isGrounded = wheel.GetGroundHit(out mostRecentGroundHit);
            if (_isGrounded)
            {
                string materialName = PhysicsTool.GetMaterialName(mostRecentGroundHit);
                replicatedGroundMaterial = PhysicsMaterialNetTable.GetNetId(materialName);
            }
            else
            {
                replicatedGroundMaterial = PhysicsMaterialNetId.NULL;
            }
        }
    }

    /// <summary>
    /// Called during FixedUpdate if vehicle is driven by the local player.
    /// </summary>
    internal void ClientSimulate(float input_x, float input_y, bool inputBrake, float delta)
    {
        if (!(wheel == null))
        {
            latestLocalSteeringInput = input_x;
            latestLocalAccelerationInput = input_y;
            latestLocalBrakingInput = inputBrake;
            UpdateGrounded();
        }
    }

    internal void OnVehicleDestroyed()
    {
        if (motionEffectInstances != null)
        {
            foreach (TireMotionEffectInstance motionEffectInstance in motionEffectInstances)
            {
                motionEffectInstance.DestroyEffect();
                motionEffectInstancesPool.Add(motionEffectInstance);
            }
            motionEffectInstances.Clear();
            motionEffectInstances = null;
            currentGroundEffect = null;
        }
        if (model != null && model.transform.parent == null)
        {
            UnityEngine.Object.Destroy(model.gameObject);
        }
    }

    /// <summary>
    /// Calculate suspension state from GetWorldPose result.
    ///
    /// Nelson 2024-03-25: Originally we used the result of GetWorldPose for the model transform and calculated
    /// the suspension state from it because I thought Unity was internally using the spring position that isn't
    /// (currently) exposed to the API. Whether or not it is, it seems fine to calculate the spring position using
    /// the ground hit point instead. We switched entirely away from GetWorldPose so that the wheel can retain
    /// its roll angle when transitioning between locally simulated and replicated.
    /// </summary>
    private float CalculateNormalizedSuspensionPosition(Vector3 worldPosePosition)
    {
        if (_wheel.suspensionDistance > float.Epsilon)
        {
            Vector3 vector = _wheel.transform.TransformPoint(_wheel.center);
            Vector3 rhs = -_wheel.transform.up;
            return Mathf.Clamp01(Vector3.Dot(worldPosePosition - vector, rhs) / _wheel.suspensionDistance);
        }
        return 0f;
    }

    private float CalculateNormalizedSuspensionPosition(float distanceAlongSuspension)
    {
        if (_wheel.suspensionDistance > float.Epsilon)
        {
            return Mathf.Clamp01(distanceAlongSuspension / _wheel.suspensionDistance);
        }
        return 0f;
    }

    private TireMotionEffectInstance FindOrAddMotionEffect(string materialName)
    {
        foreach (TireMotionEffectInstance motionEffectInstance in motionEffectInstances)
        {
            if (motionEffectInstance.materialName == materialName)
            {
                return motionEffectInstance;
            }
        }
        TireMotionEffectInstance tireMotionEffectInstance;
        if (motionEffectInstancesPool.Count > 0)
        {
            tireMotionEffectInstance = motionEffectInstancesPool.GetAndRemoveTail();
            tireMotionEffectInstance.Reset();
        }
        else
        {
            tireMotionEffectInstance = new TireMotionEffectInstance();
        }
        tireMotionEffectInstance.materialName = materialName;
        motionEffectInstances.Add(tireMotionEffectInstance);
        return tireMotionEffectInstance;
    }

    private void UpdateMotionEffect(Vector3 groundHitPosition, bool isVisualGrounded)
    {
        if (motionEffectInstances == null)
        {
            return;
        }
        string materialName = PhysicsMaterialNetTable.GetMaterialName(replicatedGroundMaterial);
        TireMotionEffectInstance tireMotionEffectInstance = (string.IsNullOrEmpty(materialName) ? null : ((currentGroundEffect != null && !(currentGroundEffect.materialName != materialName)) ? currentGroundEffect : FindOrAddMotionEffect(materialName)));
        if (currentGroundEffect != tireMotionEffectInstance)
        {
            if (currentGroundEffect != null)
            {
                currentGroundEffect.StopParticleSystem();
            }
            currentGroundEffect = tireMotionEffectInstance;
            if (currentGroundEffect != null)
            {
                currentGroundEffect.hasTriedToInstantiateEffect = false;
            }
        }
        if (currentGroundEffect != null)
        {
            if (isVisualGrounded)
            {
                if (!currentGroundEffect.hasTriedToInstantiateEffect && !MathfEx.IsNearlyZero(vehicle.ReplicatedForwardVelocity, 0.1f))
                {
                    currentGroundEffect.InstantiateEffect();
                }
                if (currentGroundEffect.particleSystem != null)
                {
                    Vector3 up = _wheel.transform.up;
                    Vector3 b = _wheel.transform.forward * (0f - Mathf.Sign(vehicle.AnimatedForwardVelocity));
                    float t = vehicle.GetAnimatedForwardSpeedPercentageOfTargetSpeed() * 0.5f;
                    Quaternion rotation = Quaternion.LookRotation(Vector3.Lerp(up, b, t));
                    currentGroundEffect.transform.SetPositionAndRotation(groundHitPosition, rotation);
                    if (currentGroundEffect.isReadyToPlay && !currentGroundEffect.particleSystem.isPlaying)
                    {
                        currentGroundEffect.particleSystem.Play();
                    }
                    currentGroundEffect.isReadyToPlay = true;
                }
            }
            else
            {
                currentGroundEffect.StopParticleSystem();
            }
        }
        for (int num = motionEffectInstances.Count - 1; num >= 0; num--)
        {
            TireMotionEffectInstance tireMotionEffectInstance2 = motionEffectInstances[num];
            if (tireMotionEffectInstance2 != currentGroundEffect && (tireMotionEffectInstance2.particleSystem == null || !tireMotionEffectInstance2.particleSystem.IsAlive()))
            {
                tireMotionEffectInstance2.DestroyEffect();
                motionEffectInstances.RemoveAtFast(num);
                motionEffectInstancesPool.Add(tireMotionEffectInstance2);
            }
        }
    }

    /// <summary>
    /// Called during Update on dedicated server only if replicated suspension state is enabled.
    /// </summary>
    internal void UpdateServerSuspensionAndPhysicsMaterial()
    {
        if (_wheel != null)
        {
            _isGrounded = _wheel.GetGroundHit(out mostRecentGroundHit);
            float distanceAlongSuspension;
            if (_isGrounded)
            {
                Vector3 vector = _wheel.transform.TransformPoint(_wheel.center);
                Vector3 rhs = -vehicle.transform.up;
                distanceAlongSuspension = Vector3.Dot(mostRecentGroundHit.point - vector, rhs) - _wheel.radius;
                string materialName = PhysicsTool.GetMaterialName(mostRecentGroundHit);
                replicatedGroundMaterial = PhysicsMaterialNetTable.GetNetId(materialName);
            }
            else
            {
                distanceAlongSuspension = _wheel.suspensionDistance;
                replicatedGroundMaterial = PhysicsMaterialNetId.NULL;
            }
            replicatedSuspensionState = CalculateNormalizedSuspensionPosition(distanceAlongSuspension);
        }
    }

    /// <summary>
    /// Set replicated suspension state AND animated suspension state when vehicle is first received.
    /// </summary>
    /// <param name="state"></param>
    internal void TeleportSuspensionState(float state)
    {
        replicatedSuspensionState = state;
        animatedSuspensionState = state;
        if (wheel != null)
        {
            animatedModelSuspension = state * wheel.suspensionDistance;
        }
    }

    /// <summary>
    /// Supported when locally simulated and on remote clients.
    /// </summary>
    internal float CalculateWheelSpeed()
    {
        if (_wheel != null && isPhysical)
        {
            float num = _wheel.rpm / 60f;
            float num2 = MathF.PI * 2f * _wheel.radius;
            return num * num2;
        }
        return 0f;
    }

    /// <summary>
    /// Called during Update on client.
    /// </summary>
    internal void UpdateModel(float deltaTime)
    {
        if (config.modelUseColliderPose && _wheel != null)
        {
            Vector3 vector = _wheel.transform.TransformPoint(_wheel.center);
            Vector3 vector2 = -vehicle.transform.up;
            Vector3 onNormal = -_wheel.transform.up;
            if (_isPhysical)
            {
                float num;
                if (_wheel.GetGroundHit(out var hit))
                {
                    Vector3 point = hit.point;
                    num = Vector3.Dot(point - vector, vector2) - _wheel.radius;
                    string materialName = PhysicsTool.GetMaterialName(hit);
                    replicatedGroundMaterial = PhysicsMaterialNetTable.GetNetId(materialName);
                    UpdateMotionEffect(point, isVisualGrounded: true);
                }
                else
                {
                    num = _wheel.suspensionDistance;
                    replicatedGroundMaterial = PhysicsMaterialNetId.NULL;
                    UpdateMotionEffect(Vector3.zero, isVisualGrounded: false);
                }
                MoveModelSuspension(num, deltaTime);
                Vector3 vector3 = Vector3.Project(vector2 * (animatedModelSuspension - config.modelSuspensionOffset), onNormal);
                Vector3 position = vector + vector3;
                float num2 = _wheel.rpm / 60f * 360f * deltaTime;
                rollAngleDegrees += num2;
                rollAngleDegrees = (rollAngleDegrees % 360f + 360f) % 360f;
                Quaternion quaternion = rest;
                quaternion = Quaternion.AngleAxis(rollAngleDegrees, Vector3.right) * quaternion;
                quaternion = Quaternion.AngleAxis(wheel.steerAngle, Vector3.up) * quaternion;
                Quaternion rotation = model.parent.TransformRotation(quaternion);
                model.SetPositionAndRotation(position, rotation);
                replicatedSuspensionState = CalculateNormalizedSuspensionPosition(num);
                animatedSuspensionState = replicatedSuspensionState;
                return;
            }
            float t = 1f - Mathf.Pow(2f, -13f * Time.deltaTime);
            animatedSuspensionState = Mathf.Lerp(animatedSuspensionState, replicatedSuspensionState, t);
            float num3 = animatedSuspensionState * _wheel.suspensionDistance;
            MoveModelSuspension(num3, deltaTime);
            Vector3 vector4 = Vector3.Project(vector2 * (animatedModelSuspension - config.modelSuspensionOffset), onNormal);
            Vector3 position2 = vector + vector4;
            if (_wheel.radius > float.Epsilon)
            {
                float num4 = vehicle.AnimatedForwardVelocity * deltaTime;
                float num5 = MathF.PI * 2f * _wheel.radius;
                float num6 = num4 / num5 * 360f;
                rollAngleDegrees += num6;
                rollAngleDegrees = (rollAngleDegrees % 360f + 360f) % 360f;
            }
            Quaternion quaternion2 = rest;
            quaternion2 = Quaternion.AngleAxis(rollAngleDegrees, Vector3.right) * quaternion2;
            if (config.steeringMode == EWheelSteeringMode.SteeringAngle)
            {
                quaternion2 = Quaternion.AngleAxis(vehicle.AnimatedSteeringAngle * config.steeringAngleMultiplier, Vector3.up) * quaternion2;
            }
            Quaternion rotation2 = model.parent.TransformRotation(quaternion2);
            model.SetPositionAndRotation(position2, rotation2);
            if (animatedSuspensionState < 0.99f)
            {
                UpdateMotionEffect(vector + vector2 * (num3 + _wheel.radius), isVisualGrounded: true);
            }
            else
            {
                UpdateMotionEffect(Vector3.zero, isVisualGrounded: false);
            }
            return;
        }
        if (config.modelRadius > float.Epsilon)
        {
            if (_isPhysical && config.copyColliderRpmIndex >= 0)
            {
                Wheel wheelAtIndex = vehicle.GetWheelAtIndex(config.copyColliderRpmIndex);
                if (wheelAtIndex != null && wheelAtIndex.wheel != null && wheelAtIndex.wheel.radius > float.Epsilon)
                {
                    float num7 = wheelAtIndex.wheel.radius * wheelAtIndex.wheel.rpm / config.modelRadius / 60f * 360f * deltaTime;
                    rollAngleDegrees += num7;
                    rollAngleDegrees = (rollAngleDegrees % 360f + 360f) % 360f;
                }
            }
            else
            {
                float num8 = vehicle.AnimatedForwardVelocity * deltaTime;
                float num9 = MathF.PI * 2f * config.modelRadius;
                float num10 = num8 / num9 * 360f;
                rollAngleDegrees += num10;
                rollAngleDegrees = (rollAngleDegrees % 360f + 360f) % 360f;
            }
        }
        else
        {
            rollAngleDegrees += vehicle.AnimatedForwardVelocity * 45f * deltaTime;
            rollAngleDegrees = (rollAngleDegrees % 360f + 360f) % 360f;
        }
        model.localRotation = rest;
        if (config.isModelSteered)
        {
            float animatedSteeringAngle = vehicle.AnimatedSteeringAngle;
            animatedSteeringAngle *= config.steeringAngleMultiplier;
            model.Rotate(0f, animatedSteeringAngle, 0f, Space.Self);
        }
        model.Rotate(rollAngleDegrees, 0f, 0f, Space.Self);
    }

    /// <summary>
    /// Called during Update if vehicle is driven by the local player.
    /// </summary>
    internal void UpdateLocallyDriven(float delta, float availableTorque)
    {
        if (wheel == null)
        {
            return;
        }
        if (config.steeringMode == EWheelSteeringMode.SteeringAngle)
        {
            float num = Mathf.Lerp(vehicle.asset.steerMax, vehicle.asset.steerMin, vehicle.GetReplicatedForwardSpeedPercentageOfTargetSpeed());
            float num2 = latestLocalSteeringInput * num;
            num2 *= config.steeringAngleMultiplier;
            float maxDelta = vehicle.asset.SteeringAngleTurnSpeed * delta;
            wheel.steerAngle = Mathf.MoveTowards(wheel.steerAngle, num2, maxDelta);
        }
        bool flag = false;
        float num3;
        bool flag2;
        if (latestLocalAccelerationInput > 0.01f)
        {
            if (vehicle.ReplicatedForwardVelocity > -0.05f)
            {
                if (vehicle.asset.UsesEngineRpmAndGears)
                {
                    num3 = availableTorque * latestLocalAccelerationInput;
                }
                else
                {
                    num3 = vehicle.asset.TargetForwardVelocity * latestLocalAccelerationInput * motorTorqueMultiplier;
                    if (vehicle.ReplicatedForwardVelocity > vehicle.asset.TargetForwardVelocity)
                    {
                        num3 *= motorTorqueClampMultiplier;
                    }
                }
                flag2 = false;
            }
            else
            {
                num3 = 0f;
                flag2 = true;
            }
        }
        else if (latestLocalAccelerationInput < -0.01f)
        {
            if (vehicle.ReplicatedForwardVelocity < 0.05f)
            {
                if (vehicle.asset.UsesEngineRpmAndGears)
                {
                    num3 = availableTorque * latestLocalAccelerationInput;
                }
                else
                {
                    num3 = vehicle.asset.TargetReverseVelocity * (0f - latestLocalAccelerationInput) * motorTorqueMultiplier;
                    if (vehicle.ReplicatedForwardVelocity < vehicle.asset.TargetReverseVelocity)
                    {
                        num3 *= motorTorqueClampMultiplier;
                    }
                }
                flag2 = false;
            }
            else
            {
                num3 = 0f;
                flag2 = true;
            }
        }
        else
        {
            num3 = 0f;
            flag2 = false;
            flag = true;
        }
        float num4 = (isPowered ? num3 : 0f);
        float num5 = 1f;
        if (config.steeringMode == EWheelSteeringMode.CrawlerTrack)
        {
            float num6 = Mathf.Lerp(1f, vehicle.asset.CrawlerTrackSteeringMaxSpeedScale, vehicle.GetReplicatedForwardSpeedPercentageOfTargetSpeed());
            if (!MathfEx.IsNearlyZero(latestLocalSteeringInput))
            {
                flag = false;
                float crawlerTrackSteeringSidewaysFrictionMultiplier = vehicle.asset.CrawlerTrackSteeringSidewaysFrictionMultiplier;
                float t = Mathf.Abs(latestLocalSteeringInput) * num6;
                num5 *= Mathf.Lerp(1f, crawlerTrackSteeringSidewaysFrictionMultiplier, t);
            }
            float num7 = 1f;
            if (latestLocalAccelerationInput < -0.01f)
            {
                num7 = -1f;
            }
            float num8 = vehicle.asset.CrawlerTrackSteeringTorque * num6;
            switch (crawlerTrackForwardMode)
            {
            case ECrawlerTrackForwardMode.Clockwise:
                num4 += num7 * latestLocalSteeringInput * num8;
                break;
            case ECrawlerTrackForwardMode.CounterClockwise:
                num4 -= num7 * latestLocalSteeringInput * num8;
                break;
            }
        }
        wheel.motorTorque = num4;
        if (hasBrakes && (flag2 || latestLocalBrakingInput))
        {
            float num9 = Mathf.Lerp(1f, brakeTorqueTractionMultiplier, vehicle.slip);
            num9 *= brakeTorqueMultiplier;
            wheel.brakeTorque = vehicle.asset.brake * num9;
        }
        else if (flag)
        {
            wheel.brakeTorque = 1f;
        }
        else
        {
            wheel.brakeTorque = 0f;
        }
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        if (vehicle.asset.hasSleds)
        {
            sidewaysFriction.stiffness = Mathf.Lerp(wheel.sidewaysFriction.stiffness, 0.25f, 4f * delta);
            forwardFriction.stiffness = Mathf.Lerp(wheel.forwardFriction.stiffness, 0.25f, 4f * delta);
        }
        else
        {
            float num10 = Mathf.Lerp(1f, stiffnessTractionMultiplier, vehicle.slip);
            sidewaysFriction.stiffness = Mathf.Lerp(wheel.sidewaysFriction.stiffness, stiffnessSideways * num10, 4f * delta);
            forwardFriction.stiffness = Mathf.Lerp(wheel.forwardFriction.stiffness, stiffnessForward * num10, 4f * delta);
        }
        sidewaysFriction.stiffness *= num5;
        wheel.sidewaysFriction = sidewaysFriction;
        wheel.forwardFriction = forwardFriction;
    }

    /// <summary>
    /// Called during Update on the server while vehicle is driven by player.
    /// </summary>
    internal void CheckForTraps()
    {
        Physics.Raycast(new Ray(wheel.transform.position, -wheel.transform.up), out var hitInfo, wheel.suspensionDistance + wheel.radius, 134217728);
        if (hitInfo.transform != null && hitInfo.transform.CompareTag("Barricade") && hitInfo.transform.GetComponent<InteractableTrapDamageTires>() != null)
        {
            askDamage();
        }
    }

    internal Wheel(InteractableVehicle newVehicle, int newIndex, WheelCollider newWheel, Transform newModel, VehicleWheelConfiguration newConfiguration)
    {
        _vehicle = newVehicle;
        index = newIndex;
        _wheel = newWheel;
        model = newModel;
        config = newConfiguration;
        if (wheel != null)
        {
            if (config.wasAutomaticallyGenerated)
            {
                wheel.forceAppPointDistance = 0f;
            }
            replicatedSuspensionState = wheel.suspensionSpring.targetPosition;
            animatedSuspensionState = replicatedSuspensionState;
            animatedModelSuspension = wheel.suspensionSpring.targetPosition * wheel.suspensionDistance;
            if (config.modelUseColliderPose)
            {
                motionEffectInstances = new List<TireMotionEffectInstance>();
            }
            currentGroundEffect = null;
        }
        if (config.steeringMode == EWheelSteeringMode.CrawlerTrack)
        {
            switch (config.crawlerTrackForwardMode)
            {
            case ECrawlerTrackForwardMode.Auto:
                if (newWheel != null)
                {
                    if (newVehicle.transform.InverseTransformPoint(newWheel.transform.position).x < 0f)
                    {
                        crawlerTrackForwardMode = ECrawlerTrackForwardMode.Clockwise;
                    }
                    else
                    {
                        crawlerTrackForwardMode = ECrawlerTrackForwardMode.CounterClockwise;
                    }
                }
                else
                {
                    Assets.ReportError(vehicle.asset, $"wheel at index {index} has Auto CrawlerTrackForwardMode without a collider");
                }
                break;
            default:
                crawlerTrackForwardMode = config.crawlerTrackForwardMode;
                break;
            }
        }
        isPowered = config.isColliderPowered;
        hasBrakes = true;
        isAlive = true;
        if (model != null)
        {
            rest = model.localRotation;
        }
    }

    private void MoveModelSuspension(float target, float deltaTime)
    {
        if (config.modelSuspensionSpeed < -0.01f)
        {
            animatedModelSuspension = target;
            return;
        }
        float maxDelta = config.modelSuspensionSpeed * deltaTime;
        animatedModelSuspension = Mathf.MoveTowards(animatedModelSuspension, target, maxDelta);
    }

    [Obsolete("Should not have been public.")]
    public void checkForTraps()
    {
        CheckForTraps();
    }

    [Obsolete("Should not have been public.")]
    public void update(float delta)
    {
        UpdateLocallyDriven(delta, 0f);
    }

    [Obsolete("Should not have been public.")]
    public void simulate(float input_x, float input_y, bool inputBrake, float delta)
    {
        ClientSimulate(input_x, input_y, inputBrake, delta);
    }

    [Obsolete("Should not have been public.")]
    public void reset()
    {
        Reset();
    }
}
