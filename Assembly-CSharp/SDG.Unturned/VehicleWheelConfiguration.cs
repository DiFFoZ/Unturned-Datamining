namespace SDG.Unturned;

internal class VehicleWheelConfiguration : IDatParseable
{
    /// <summary>
    /// If true, this configuration was created by <see cref="!:InteractableVehicle.BuildAutomaticWheelConfiguration" />.
    /// Otherwise, this configuration was loaded from the vehicle asset file.
    /// </summary>
    public bool wasAutomaticallyGenerated;

    /// <summary>
    /// Transform path relative to Vehicle prefab with WheelCollider component.
    /// </summary>
    public string wheelColliderPath;

    /// <summary>
    /// If true, WheelCollider's motorTorque is set according to accelerator input.
    /// </summary>
    public bool isColliderPowered;

    /// <summary>
    /// Transform path relative to Vehicle prefab. Animated to match WheelCollider state.
    /// </summary>
    public string modelPath;

    /// <summary>
    /// If true, model is animated according to steering input.
    /// Only kept for backwards compatibility. Prior to wheel configurations, only certain WheelColliders actually
    /// received steering input, while multiple models would appear to steer. For example, the APC's front 4 wheels
    /// appeared to rotate but only the front 2 actually affected physics.
    /// </summary>
    public bool isModelSteered;

    /// <summary>
    /// If true, model ignores isModelSteered and instead uses WheelCollider.GetWorldPose when simulating or the
    /// replicated state from the server when not simulating. Defaults to false.
    /// </summary>
    public bool modelUseColliderPose;

    /// <summary>
    /// If greater than zero, visual-only wheels (without a collider) like the extra wheels of the Snowmobile use
    /// this radius to calculate their rolling speed.
    /// </summary>
    public float modelRadius;

    /// <summary>
    /// If set, visual-only wheels without a collider (like the back wheels of the snowmobile) can copy RPM from
    /// a wheel that does have a collider. Requires modelRadius to also be set.
    /// </summary>
    public int copyColliderRpmIndex;

    /// <summary>
    /// Target steering angle is multiplied by this value. For example, can be set to a negative number for
    /// rear-wheel steering. Defaults to 1.
    /// </summary>
    public float steeringAngleMultiplier;

    /// <summary>
    /// Vertical offset of model from simulated suspension position.
    /// </summary>
    public float modelSuspensionOffset;

    /// <summary>
    /// How quickly to interpolate model toward suspension position in meters per second.
    /// If negative, position teleports immediately.
    /// </summary>
    public float modelSuspensionSpeed;

    /// <summary>
    /// Nelson 2024-12-06: Initially implemented as a minimum and maximum percentage of normalized forward velocity,
    /// but think this is more practical. I can't think of why we would use values other than -1, 0, +1 for that,
    /// and if we did we'd probably want some tuning for the angle particles are emitted at.
    /// </summary>
    public EWheelMotionEffectsMode motionEffectsMode;

    /// <summary>
    /// If true, wheel should fly off when vehicle explodes. Defaults to true.
    /// Used to simplify destroying vehicles with crawler tracks.
    /// </summary>
    public bool canExplode;

    public EWheelSteeringMode steeringMode;

    public ECrawlerTrackForwardMode crawlerTrackForwardMode;

    public bool TryParse(IDatNode node)
    {
        if (node is DatDictionary datDictionary)
        {
            wheelColliderPath = datDictionary.GetString("WheelColliderPath");
            isColliderPowered = datDictionary.ParseBool("IsColliderPowered");
            modelPath = datDictionary.GetString("ModelPath");
            isModelSteered = datDictionary.ParseBool("IsModelSteered");
            modelUseColliderPose = datDictionary.ParseBool("ModelUseColliderPose");
            modelRadius = datDictionary.ParseFloat("ModelRadius", -1f);
            copyColliderRpmIndex = datDictionary.ParseInt32("CopyColliderRpmIndex", -1);
            steeringAngleMultiplier = datDictionary.ParseFloat("SteeringAngleMultiplier", 1f);
            modelSuspensionOffset = datDictionary.ParseFloat("ModelSuspensionOffset");
            modelSuspensionSpeed = datDictionary.ParseFloat("ModelSuspensionSpeed", -1f);
            EWheelMotionEffectsMode defaultValue = (modelUseColliderPose ? EWheelMotionEffectsMode.BothDirections : EWheelMotionEffectsMode.None);
            motionEffectsMode = datDictionary.ParseEnum("MotionEffects", defaultValue);
            canExplode = datDictionary.ParseBool("CanExplode", defaultValue: true);
            if (datDictionary.ParseBool("IsColliderSteered"))
            {
                steeringMode = EWheelSteeringMode.SteeringAngle;
            }
            else
            {
                steeringMode = datDictionary.ParseEnum("SteeringMode", EWheelSteeringMode.None);
            }
            crawlerTrackForwardMode = datDictionary.ParseEnum("CrawlerTrackForwardMode", ECrawlerTrackForwardMode.Auto);
            return true;
        }
        return false;
    }
}
