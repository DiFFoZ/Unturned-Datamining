using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class ResourceAsset : Asset
{
    private static List<MeshFilter> meshes = new List<MeshFilter>();

    private static Shader shader;

    protected string _resourceName;

    protected GameObject _modelGameObject;

    protected GameObject _stumpGameObject;

    protected GameObject _skyboxGameObject;

    protected GameObject _debrisGameObject;

    public ushort health;

    public uint rewardXP;

    [Obsolete("Replaced by MinRandomScale and MaxRandomScale.")]
    public float scale;

    public float verticalOffset;

    private Guid _explosionGuid;

    public ushort explosion;

    public ushort log;

    public ushort stick;

    public byte rewardMin;

    public byte rewardMax;

    public ushort rewardID;

    public bool isForage;

    /// <summary>
    /// Amount of experience to reward foraging player.
    /// </summary>
    public uint forageRewardExperience;

    /// <summary>
    /// Forageable resource message.
    /// </summary>
    public string interactabilityText;

    public bool hasDebris;

    /// <summary>
    /// Weapon must have matching blade ID to damage tree.
    /// Both weapons and trees default to zero so they can be damaged by default.
    /// </summary>
    public byte bladeID;

    public float reset;

    /// <summary>
    /// Tree to use during the Christmas event instead.
    /// </summary>
    public AssetReference<ResourceAsset> christmasRedirect;

    /// <summary>
    /// Tree to use during the Halloween event instead.
    /// </summary>
    public AssetReference<ResourceAsset> halloweenRedirect;

    public EObjectChart chart;

    public bool shouldExcludeFromLevelBatching;

    public string resourceName => holidayRestriction switch
    {
        ENPCHoliday.HALLOWEEN => _resourceName + " [HW]", 
        ENPCHoliday.CHRISTMAS => _resourceName + " [XMAS]", 
        _ => _resourceName, 
    };

    public override string FriendlyName => resourceName;

    public GameObject modelGameObject => _modelGameObject;

    public GameObject stumpGameObject => _stumpGameObject;

    public GameObject skyboxGameObject => _skyboxGameObject;

    public GameObject debrisGameObject => _debrisGameObject;

    public Material skyboxMaterial { get; private set; }

    /// <summary>
    /// Distance along tree's local up axis to offset debris spawn position. Defaults to 1.0.
    /// </summary>
    public float DebrisVerticalOffset { get; protected set; }

    public float MinRandomAngleDeviation { get; protected set; }

    /// <summary>
    /// Before <see cref="!:FoliageResourceInfoAsset" /> had randomization properties (which trees don't currently use
    /// as of 2024-12-11 because rotation/scale aren't saved) each tree has some random rotation and scale variation
    /// based on its position. This property controls the rotation away from upright.
    /// </summary>
    public float MaxRandomAngleDeviation { get; protected set; }

    public float MinRandomUniformScale { get; protected set; }

    public float MaxRandomUniformScale { get; protected set; }

    public Guid explosionGuid => _explosionGuid;

    public bool vulnerableToFists { get; protected set; }

    public bool vulnerableToAllMeleeWeapons { get; protected set; }

    /// <summary>
    /// If true, prevent collisions between falling tree and the stump. (i.e., debris can fall through stump)
    /// Defaults to true.
    /// </summary>
    public bool ShouldIgnoreCollisionBetweenStumpAndDebris { get; protected set; }

    public override EAssetType assetCategory => EAssetType.RESOURCE;

    /// <summary>
    /// Only activated during this holiday.
    /// </summary>
    public ENPCHoliday holidayRestriction { get; protected set; }

    public EffectAsset FindExplosionEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(_explosionGuid, explosion);
    }

    /// <summary>
    /// Get asset ref to replace this one for holiday, or null if it should not be redirected.
    /// </summary>
    public AssetReference<ResourceAsset> getHolidayRedirect()
    {
        return HolidayUtil.getActiveHoliday() switch
        {
            ENPCHoliday.CHRISTMAS => christmasRedirect, 
            ENPCHoliday.HALLOWEEN => halloweenRedirect, 
            _ => AssetReference<ResourceAsset>.invalid, 
        };
    }

    protected void applyDefaultLODs(LODGroup lod, bool fade)
    {
        LOD[] lODs = lod.GetLODs();
        lODs[0].screenRelativeTransitionHeight = (fade ? 0.7f : 0.6f);
        lODs[1].screenRelativeTransitionHeight = (fade ? 0.5f : 0.4f);
        lODs[2].screenRelativeTransitionHeight = 0.15f;
        lODs[3].screenRelativeTransitionHeight = 0.03f;
        lod.SetLODs(lODs);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (id < 50 && !base.OriginAllowsVanillaLegacyId && !data.ContainsKey("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 50");
        }
        _resourceName = localization.format("Name");
        if (Dedicator.IsDedicatedServer)
        {
            if (data.ParseBool("Has_Clip_Prefab", defaultValue: true))
            {
                _modelGameObject = bundle.load<GameObject>("Resource_Clip");
                if (_modelGameObject == null)
                {
                    Assets.ReportError(this, "missing \"Resource_Clip\" GameObject, loading \"Resource\" GameObject instead");
                }
                _stumpGameObject = bundle.load<GameObject>("Stump_Clip");
                if (_stumpGameObject == null)
                {
                    Assets.ReportError(this, "missing \"Stump_Clip\" GameObject, loading \"Stump\" GameObject instead");
                }
            }
            if (_modelGameObject == null)
            {
                _modelGameObject = bundle.load<GameObject>("Resource");
                if (_modelGameObject == null)
                {
                    Assets.ReportError(this, "missing \"Resource\" GameObject");
                }
                else
                {
                    ServerPrefabUtil.RemoveClientComponents(_modelGameObject);
                }
            }
            if (_stumpGameObject == null)
            {
                _stumpGameObject = bundle.load<GameObject>("Stump");
                if (_stumpGameObject == null)
                {
                    Assets.ReportError(this, "missing \"Stump\" GameObject");
                }
                else
                {
                    ServerPrefabUtil.RemoveClientComponents(_stumpGameObject);
                }
            }
        }
        else
        {
            _modelGameObject = bundle.load<GameObject>("Resource_Old");
            if (_modelGameObject == null)
            {
                _modelGameObject = bundle.load<GameObject>("Resource");
            }
            if (_modelGameObject == null)
            {
                Assets.ReportError(this, "missing \"Resource\" GameObject");
            }
            _stumpGameObject = bundle.load<GameObject>("Stump_Old");
            if (_stumpGameObject == null)
            {
                _stumpGameObject = bundle.load<GameObject>("Stump");
            }
            if (_stumpGameObject == null)
            {
                Assets.ReportError(this, "missing \"Stump\" GameObject");
            }
            _skyboxGameObject = bundle.load<GameObject>("Skybox_Old");
            if (_skyboxGameObject == null)
            {
                _skyboxGameObject = bundle.load<GameObject>("Skybox");
            }
            _debrisGameObject = bundle.load<GameObject>("Debris_Old");
            if (_debrisGameObject == null)
            {
                _debrisGameObject = bundle.load<GameObject>("Debris");
            }
            if (data.ContainsKey("Auto_Skybox") && (bool)skyboxGameObject)
            {
                Transform transform = modelGameObject.transform.Find("Model_0");
                if ((bool)transform)
                {
                    meshes.Clear();
                    transform.GetComponentsInChildren(includeInactive: true, meshes);
                    if (meshes.Count > 0)
                    {
                        Bounds bounds = default(Bounds);
                        for (int i = 0; i < meshes.Count; i++)
                        {
                            Mesh sharedMesh = meshes[i].sharedMesh;
                            if (!(sharedMesh == null))
                            {
                                Bounds bounds2 = sharedMesh.bounds;
                                bounds.Encapsulate(bounds2.min);
                                bounds.Encapsulate(bounds2.max);
                            }
                        }
                        if (bounds.min.y < 0f)
                        {
                            float num = Mathf.Abs(bounds.min.z);
                            bounds.center += new Vector3(0f, 0f, num / 2f);
                            bounds.size -= new Vector3(0f, 0f, num);
                        }
                        float num2 = Mathf.Max(bounds.size.x, bounds.size.y);
                        float z = bounds.size.z;
                        skyboxGameObject.transform.localScale = new Vector3(z, z, z);
                        Transform transform2 = UnityEngine.Object.Instantiate(modelGameObject).transform;
                        Transform transform3 = new GameObject().transform;
                        transform3.parent = transform2;
                        transform3.localPosition = new Vector3(0f, z / 2f, (0f - num2) / 2f);
                        transform3.localRotation = Quaternion.identity;
                        Transform transform4 = new GameObject().transform;
                        transform4.parent = transform2;
                        transform4.localPosition = new Vector3((0f - num2) / 2f, z / 2f, 0f);
                        transform4.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        if (!shader)
                        {
                            shader = Shader.Find("Custom/Card");
                        }
                        Texture2D card = ItemTool.getCard(transform2, transform3, transform4, 64, 64, z / 2f, num2);
                        skyboxMaterial = new Material(shader);
                        skyboxMaterial.mainTexture = card;
                    }
                }
            }
        }
        if (_modelGameObject != null)
        {
            _modelGameObject.SetTagIfUntaggedRecursively("Resource");
        }
        if (_stumpGameObject != null)
        {
            _stumpGameObject.SetTagIfUntaggedRecursively("Resource");
        }
        if (_skyboxGameObject != null)
        {
            _skyboxGameObject.SetTagIfUntaggedRecursively("Resource");
        }
        health = data.ParseUInt16("Health", 0);
        MinRandomAngleDeviation = data.ParseFloat("RandomAngleDeviation_Min", -5f);
        MaxRandomAngleDeviation = data.ParseFloat("RandomAngleDeviation_Max", 5f);
        if (data.TryParseFloat("Scale", out var value))
        {
            scale = value;
            MinRandomUniformScale = 1.1f;
            MaxRandomUniformScale = 1.1f + value * 2f;
        }
        else
        {
            MinRandomUniformScale = data.ParseFloat("RandomUniformScale_Min", 1.1f);
            MaxRandomUniformScale = data.ParseFloat("RandomUniformScale_Max", 1.1f);
            scale = (MaxRandomUniformScale - MinRandomUniformScale) * 0.5f;
        }
        verticalOffset = data.ParseFloat("Vertical_Offset", -0.75f);
        DebrisVerticalOffset = data.ParseFloat("Debris_Vertical_Offset", 1f);
        explosion = data.ParseGuidOrLegacyId("Explosion", out _explosionGuid);
        log = data.ParseUInt16("Log", 0);
        stick = data.ParseUInt16("Stick", 0);
        rewardID = data.ParseUInt16("Reward_ID", 0);
        rewardXP = data.ParseUInt32("Reward_XP");
        if (data.ContainsKey("Reward_Min"))
        {
            rewardMin = data.ParseUInt8("Reward_Min", 0);
        }
        else
        {
            rewardMin = 6;
        }
        if (data.ContainsKey("Reward_Max"))
        {
            rewardMax = data.ParseUInt8("Reward_Max", 0);
        }
        else
        {
            rewardMax = 9;
        }
        bladeID = data.ParseUInt8("BladeID", 0);
        vulnerableToFists = data.ParseBool("Vulnerable_To_Fists");
        vulnerableToAllMeleeWeapons = data.ParseBool("Vulnerable_To_All_Melee_Weapons");
        ShouldIgnoreCollisionBetweenStumpAndDebris = data.ParseBool("Ignore_Collision_Between_Stump_And_Debris", defaultValue: true);
        reset = data.ParseFloat("Reset");
        isForage = data.ContainsKey("Forage");
        if (isForage && _modelGameObject != null)
        {
            Transform transform5 = _modelGameObject.transform.Find("Forage");
            if (transform5 != null)
            {
                transform5.gameObject.layer = 14;
            }
            else
            {
                Assets.ReportError(this, "foragable resource missing \"Forage\" GameObject");
            }
        }
        forageRewardExperience = data.ParseUInt32("Forage_Reward_Experience", 1u);
        if (isForage)
        {
            interactabilityText = localization.read("Interact");
            interactabilityText = ItemTool.filterRarityRichText(interactabilityText);
        }
        hasDebris = !data.ContainsKey("No_Debris");
        if (data.ContainsKey("Holiday_Restriction"))
        {
            holidayRestriction = (ENPCHoliday)Enum.Parse(typeof(ENPCHoliday), data.GetString("Holiday_Restriction"), ignoreCase: true);
            if (holidayRestriction == ENPCHoliday.NONE)
            {
                Assets.ReportError(this, "has no holiday restriction, so value is ignored");
            }
        }
        else
        {
            holidayRestriction = ENPCHoliday.NONE;
        }
        christmasRedirect = data.readAssetReference<ResourceAsset>("Christmas_Redirect");
        halloweenRedirect = data.readAssetReference<ResourceAsset>("Halloween_Redirect");
        chart = data.ParseEnum("Chart", EObjectChart.NONE);
        shouldExcludeFromLevelBatching = data.ParseBool("Exclude_From_Level_Batching");
    }

    internal string OnGetRewardSpawnTableErrorContext()
    {
        return FriendlyName + " reward";
    }
}
