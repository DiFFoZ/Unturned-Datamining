using System;
using System.Collections.Generic;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class ItemStructureAsset : ItemPlaceableAsset
{
    protected GameObject _structure;

    protected GameObject _nav;

    protected AudioClip _use;

    protected EConstruct _construct;

    protected ushort _health;

    protected float _range;

    private Guid _explosionGuid;

    protected ushort _explosion;

    public bool canBeDamaged = true;

    public bool eligibleForPooling = true;

    public bool requiresPillars = true;

    protected bool _isVulnerable;

    protected bool _isRepairable;

    protected bool _proofExplosion;

    protected bool _isUnpickupable;

    protected bool _isSalvageable;

    protected bool _isSaveable;

    public MasterBundleReference<GameObject> placementPreviewRef;

    private static List<Transform> transformsToDestroy = new List<Transform>();

    public GameObject structure => _structure;

    [Obsolete("Only one of Structure.prefab or Clip.prefab are loaded now as _structure")]
    public GameObject clip => _structure;

    public GameObject nav => _nav;

    public AudioClip use => _use;

    public EConstruct construct => _construct;

    public ushort health => _health;

    public float range => _range;

    public Guid explosionGuid => _explosionGuid;

    public ushort explosion
    {
        [Obsolete]
        get
        {
            return _explosion;
        }
    }

    public bool isVulnerable => _isVulnerable;

    public bool isRepairable => _isRepairable;

    public bool proofExplosion => _proofExplosion;

    public bool isUnpickupable => _isUnpickupable;

    public bool isSalvageable => _isSalvageable;

    public float salvageDurationMultiplier { get; protected set; }

    public bool isSaveable => _isSaveable;

    public EArmorTier armorTier { get; protected set; }

    public float foliageCutRadius { get; protected set; }

    public float terrainTestHeight { get; protected set; }

    public override bool shouldFriendlySentryTargetUser => true;

    public EffectAsset FindExplosionEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(_explosionGuid, _explosion);
    }

    public override bool canBeUsedInSafezone(SafezoneNode safezone, bool byAdmin)
    {
        return !safezone.noBuildables;
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        bool flag;
        if (Dedicator.IsDedicatedServer && data.ParseBool("Has_Clip_Prefab", defaultValue: true))
        {
            _structure = bundle.load<GameObject>("Clip");
            if (structure == null)
            {
                flag = true;
                Assets.reportError(this, "missing \"Clip\" GameObject, loading \"Structure\" GameObject instead");
            }
            else
            {
                flag = false;
                AssetValidation.searchGameObjectForErrors(this, structure);
            }
        }
        else
        {
            flag = true;
        }
        if (flag)
        {
            _structure = bundle.load<GameObject>("Structure");
            if (structure == null)
            {
                Assets.reportError(this, "missing \"Structure\" GameObject");
            }
            else
            {
                AssetValidation.searchGameObjectForErrors(this, structure);
                if (Dedicator.IsDedicatedServer)
                {
                    ServerPrefabUtil.RemoveClientComponents(_structure);
                    RemoveClientComponents(_structure);
                }
                else
                {
                    LODGroup component = structure.GetComponent<LODGroup>();
                    if (component != null)
                    {
                        component.DisableCulling();
                    }
                }
            }
        }
        placementPreviewRef = data.readMasterBundleReference<GameObject>("PlacementPreviewPrefab");
        _nav = bundle.load<GameObject>("Nav");
        _use = LoadRedirectableAsset<AudioClip>(bundle, "Use", data, "PlacementAudioClip");
        _construct = (EConstruct)Enum.Parse(typeof(EConstruct), data.GetString("Construct"), ignoreCase: true);
        _health = data.ParseUInt16("Health", 0);
        _range = data.ParseFloat("Range");
        _explosion = data.ParseGuidOrLegacyId("Explosion", out _explosionGuid);
        canBeDamaged = data.ParseBool("Can_Be_Damaged", defaultValue: true);
        eligibleForPooling = data.ParseBool("Eligible_For_Pooling", defaultValue: true);
        requiresPillars = data.ParseBool("Requires_Pillars", defaultValue: true);
        _isVulnerable = data.ContainsKey("Vulnerable");
        _isRepairable = !data.ContainsKey("Unrepairable");
        _proofExplosion = data.ContainsKey("Proof_Explosion");
        _isUnpickupable = data.ContainsKey("Unpickupable");
        _isSalvageable = !data.ContainsKey("Unsalvageable");
        salvageDurationMultiplier = data.ParseFloat("Salvage_Duration_Multiplier", 1f);
        _isSaveable = !data.ContainsKey("Unsaveable");
        if (data.ContainsKey("Armor_Tier"))
        {
            armorTier = (EArmorTier)Enum.Parse(typeof(EArmorTier), data.GetString("Armor_Tier"), ignoreCase: true);
        }
        else if (name.Contains("Metal") || name.Contains("Brick"))
        {
            armorTier = EArmorTier.HIGH;
        }
        else
        {
            armorTier = EArmorTier.LOW;
        }
        foliageCutRadius = data.ParseFloat("Foliage_Cut_Radius", 6f);
        terrainTestHeight = data.ParseFloat("Terrain_Test_Height", 10f);
    }

    protected override AudioReference GetDefaultInventoryAudio()
    {
        if (name.Contains("Metal", StringComparison.InvariantCultureIgnoreCase))
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/SmallMetal.asset");
        }
        if (size_x <= 1 || size_y <= 1)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/LightMetalEquipment.asset");
        }
        if (size_x <= 2 || size_y <= 2)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/MediumMetalEquipment.asset");
        }
        return new AudioReference("core.masterbundle", "Sounds/Inventory/HeavyMetalEquipment.asset");
    }

    private void RemoveClientComponents(GameObject gameObject)
    {
        foreach (Transform item in gameObject.transform)
        {
            if (item.name == "Climb" || item.name == "Hatch" || item.name == "Slot" || item.name == "Door" || item.name == "Gate")
            {
                transformsToDestroy.Add(item);
            }
        }
        foreach (Transform item2 in transformsToDestroy)
        {
            UnityEngine.Object.DestroyImmediate(item2.gameObject, allowDestroyingAssets: true);
        }
        transformsToDestroy.Clear();
    }
}
