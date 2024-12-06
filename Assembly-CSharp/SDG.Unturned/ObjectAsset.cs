using System;
using System.Collections.Generic;
using System.Text;
using SDG.Framework.Foliage;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class ObjectAsset : Asset
{
    protected string _objectName;

    public EObjectType type;

    private GameObject loadedModel;

    /// <summary>
    /// Prevents calling getOrLoad redundantly if asset does not exist.
    /// </summary>
    private bool hasLoadedModel;

    private IDeferredAsset<GameObject> clientModel;

    private IDeferredAsset<GameObject> legacyServerModel;

    public IDeferredAsset<GameObject> skyboxGameObject;

    public IDeferredAsset<GameObject> navGameObject;

    public IDeferredAsset<GameObject> slotsGameObject;

    public IDeferredAsset<GameObject> triggersGameObject;

    public bool isSnowshoe;

    public bool shouldExcludeFromCullingVolumes;

    public bool shouldExcludeFromLevelBatching;

    public EObjectChart chart;

    public bool isFuel;

    public bool isRefill;

    public bool isSoft;

    /// <summary>
    /// Should landing on this object inflict fall damage?
    /// </summary>
    public bool causesFallDamage;

    public bool isCollisionImportant;

    public bool useScale;

    public EObjectLOD lod;

    public float lodBias;

    public Vector3 cullingVolumeLocalPositionOffset;

    public Vector3 cullingVolumeSizeOffset;

    public INPCCondition[] conditions;

    public EObjectInteractability interactability;

    public bool interactabilityRemote;

    public float interactabilityDelay;

    public EObjectInteractabilityHint interactabilityHint;

    public string interactabilityText;

    public EObjectInteractabilityPower interactabilityPower;

    public EObjectInteractabilityEditor interactabilityEditor;

    public EObjectInteractabilityNav interactabilityNav;

    public float interactabilityReset;

    public ushort interactabilityResource;

    private byte[] interactabilityResourceState;

    public ushort[] interactabilityDrops;

    public ushort interactabilityRewardID;

    public Guid interactabilityEffectGuid;

    [Obsolete]
    public ushort interactabilityEffect;

    internal AssetReference<DialogueAsset> interactabilityDialogueRef;

    /// <summary>
    /// Same as interactabilityDialogueRef, not public because it really needs to be cleaned up. :(
    /// </summary>
    internal string interactabilityChildPathOverride;

    public INPCCondition[] interactabilityConditions;

    protected NPCRewardsList interactabilityRewards;

    public EObjectRubble rubble;

    public float rubbleReset;

    public ushort rubbleHealth;

    /// <summary>
    /// Effect played when single segment is destroyed.
    /// </summary>
    public Guid rubbleEffectGuid;

    [Obsolete]
    public ushort rubbleEffect;

    /// <summary>
    /// Effect played when all segments are destroyed.
    /// </summary>
    public Guid rubbleFinaleGuid;

    [Obsolete]
    public ushort rubbleFinale;

    public EObjectRubbleEditor rubbleEditor;

    public ushort rubbleRewardID;

    /// <summary>
    /// Weapon must have matching blade ID to damage object.
    /// Both weapons and objects default to zero so they can be damaged by default.
    /// </summary>
    public byte rubbleBladeID;

    /// <summary>
    /// [0, 1] probability of dropping any rewards.
    /// </summary>
    public float rubbleRewardProbability;

    public byte rubbleRewardsMin;

    public byte rubbleRewardsMax;

    public uint rubbleRewardXP;

    public bool rubbleIsVulnerable;

    public bool rubbleProofExplosion;

    public AssetReference<FoliageInfoCollectionAsset> foliage;

    public bool useWaterHeightTransparentSort;

    public AssetReference<MaterialPaletteAsset> materialPalette;

    public EGraphicQuality landmarkQuality;

    public bool shouldAddNightLightScript;

    /// <summary>
    /// Should colliders in the Triggers GameObject with "Kill" name kill players?
    /// If Triggers GameObject is not set, searches Object instead.
    /// </summary>
    public bool shouldAddKillTriggers;

    public bool allowStructures;

    /// <summary>
    /// Should this object only be visible if gore is enabled?
    /// Allows pre-placed blood splatters to be hidden for younger players.
    /// </summary>
    public bool isGore;

    /// <summary>
    /// Object to use during the Christmas event instead.
    /// </summary>
    public AssetReference<ObjectAsset> christmasRedirect;

    /// <summary>
    /// Object to use during the Halloween event instead.
    /// </summary>
    public AssetReference<ObjectAsset> halloweenRedirect;

    private static HashSet<ushort> tempAssociatedFlags = new HashSet<ushort>();

    private static List<MeshCollider> navMCs = new List<MeshCollider>();

    public string objectName => holidayRestriction switch
    {
        ENPCHoliday.HALLOWEEN => _objectName + " [HW]", 
        ENPCHoliday.CHRISTMAS => _objectName + " [XMAS]", 
        ENPCHoliday.APRIL_FOOLS => _objectName + " [AF]", 
        ENPCHoliday.VALENTINES => _objectName + " [V]", 
        ENPCHoliday.PRIDE_MONTH => _objectName + " [PM]", 
        _ => _objectName, 
    };

    public override string FriendlyName => objectName;

    /// <summary>
    /// If true, object will be hidden when rendering GPS/satellite view.
    /// Defaults to true if <see cref="P:SDG.Unturned.ObjectAsset.holidayRestriction" /> is set.
    /// </summary>
    public bool ShouldExcludeFromSatelliteCapture { get; private set; }

    /// <summary>
    /// If true, Nav game object will be instantiated in singleplayer and on dedicated server. Useful for objects
    /// which need to affect navmesh baking without colliding with zombies during gameplay.
    /// Defaults to true for "medium" and "large" objects.
    /// </summary>
    public bool ShouldLoadNavOnServer { get; private set; }

    /// <summary>
    /// If true, Nav game object will be instantiated in the level editor. Useful for objects which need collision
    /// with zombies during gameplay without affecting navmesh baking.
    /// Defaults to true for "medium" and "large" objects.
    /// </summary>
    public bool ShouldLoadNavInEditor { get; private set; }

    /// <summary>
    /// If true, zombies can attack this object if it's blocking them. Defaults to false.
    /// </summary>
    public bool RubbleCanZombiesDamage { get; protected set; }

    /// <summary>
    /// Multiplier for damage from zombies if RubbleCanZombiesDamage is true.
    /// </summary>
    public float RubbleZombieDamageMultiplier { get; protected set; }

    /// <summary>
    /// Controls how rubble affects Nav game object.
    /// </summary>
    public EObjectRubbleNavMode RubbleNavMode { get; protected set; }

    /// <summary>
    /// Only activated during this holiday.
    /// </summary>
    public ENPCHoliday holidayRestriction { get; protected set; }

    public override EAssetType assetCategory => EAssetType.OBJECT;

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

    protected void validateModel(GameObject asset)
    {
        if (Mathf.Abs(asset.transform.localScale.x - 1f) > 0.01f || Mathf.Abs(asset.transform.localScale.y - 1f) > 0.01f || Mathf.Abs(asset.transform.localScale.z - 1f) > 0.01f)
        {
            useScale = false;
            Assets.ReportError(this, "should have a scale of one");
        }
        else
        {
            useScale = true;
        }
        Transform transform = asset.transform.Find("Block");
        if (transform != null && transform.GetComponent<Collider>() != null && transform.GetComponent<Collider>().sharedMaterial == null)
        {
            Assets.ReportError(this, "has a 'Block' collider but no physics material");
        }
        Transform transform2 = asset.transform.Find("Model_0");
        string expectedTag = string.Empty;
        int num = -1;
        if (type == EObjectType.SMALL)
        {
            expectedTag = "Small";
            num = 17;
        }
        else if (type == EObjectType.MEDIUM)
        {
            expectedTag = "Medium";
            num = 16;
        }
        else if (type == EObjectType.LARGE)
        {
            expectedTag = "Large";
            num = 15;
        }
        if (num == -1)
        {
            Assets.ReportError(this, "has an unknown tag/layer because it has an unhandled EObjectType");
        }
        else
        {
            fixTagAndLayer(asset, expectedTag, num);
            if (transform2 != null)
            {
                fixTagAndLayer(transform2.gameObject, expectedTag, num);
            }
            AssetValidation.searchGameObjectForErrors(this, asset);
        }
        if (!Assets.shouldValidateAssets)
        {
            return;
        }
        if (interactability == EObjectInteractability.BINARY_STATE)
        {
            Animation animation = asset.transform.Find("Root")?.GetComponent<Animation>();
            if (animation != null)
            {
                validateAnimation(animation, "Open");
                validateAnimation(animation, "Close");
            }
        }
        if (interactability == EObjectInteractability.RUBBLE || rubble != 0)
        {
            Transform transform3 = asset.transform.Find("Sections");
            if (transform3 != null && transform3.childCount > 8)
            {
                Assets.ReportError(this, $"destructible has {transform3.childCount} sections, but the maximum supported is 8");
            }
        }
    }

    /// <summary>
    /// Clip.prefab
    /// </summary>
    protected void OnServerModelLoaded(GameObject asset)
    {
        if (asset == null && type != EObjectType.SMALL)
        {
            Assets.ReportError(this, "missing \"Clip\" GameObject, loading \"Object\" GameObject instead");
        }
        if (asset != null)
        {
            validateModel(asset);
        }
    }

    /// <summary>
    /// Object.prefab
    /// </summary>
    protected void OnClientModelLoaded(GameObject asset)
    {
        if (asset == null)
        {
            Assets.ReportError(this, "missing \"Object\" GameObject");
            return;
        }
        validateModel(asset);
        if (Dedicator.IsDedicatedServer)
        {
            ServerPrefabUtil.RemoveClientComponents(asset);
        }
    }

    protected void onNavGameObjectLoaded(GameObject asset)
    {
        if (asset == null && type == EObjectType.LARGE)
        {
            Assets.ReportError(this, "missing Nav GameObject. Highly recommended to fix.");
        }
        if (asset != null)
        {
            fixTagAndLayer(asset, "Navmesh", 22);
            if ((bool)Assets.shouldValidateAssets)
            {
                ensureNavMeshReadable();
            }
        }
    }

    protected void onSlotsGameObjectLoaded(GameObject asset)
    {
        if (asset != null)
        {
            asset.SetTagIfUntaggedRecursively("Logic");
        }
    }

    public EffectAsset FindInteractabilityEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(interactabilityEffectGuid, interactabilityEffect);
    }

    /// <summary>
    /// Property is not exposed at the moment because interactability properties should really be moved into some
    /// sort of sub-asset.
    /// </summary>
    public DialogueAsset FindInteractabilityDialogueAsset()
    {
        return interactabilityDialogueRef.Find();
    }

    public EffectAsset FindRubbleEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(rubbleEffectGuid, rubbleEffect);
    }

    public bool IsRubbleFinaleEffectRefNull()
    {
        if (rubbleFinale == 0)
        {
            return rubbleFinaleGuid.IsEmpty();
        }
        return false;
    }

    public EffectAsset FindRubbleFinaleEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(rubbleFinaleGuid, rubbleFinale);
    }

    /// <summary>
    /// Get asset ref to replace this one for holiday, or null if it should not be redirected.
    /// </summary>
    public AssetReference<ObjectAsset> getHolidayRedirect()
    {
        return HolidayUtil.getActiveHoliday() switch
        {
            ENPCHoliday.CHRISTMAS => christmasRedirect, 
            ENPCHoliday.HALLOWEEN => halloweenRedirect, 
            _ => AssetReference<ObjectAsset>.invalid, 
        };
    }

    public virtual byte[] getState()
    {
        byte[] array = ((interactability == EObjectInteractability.BINARY_STATE) ? new byte[1] { (byte)((Level.isEditor && interactabilityEditor != 0) ? 1u : 0u) } : ((interactability != EObjectInteractability.WATER && interactability != EObjectInteractability.FUEL) ? null : new byte[2]
        {
            interactabilityResourceState[0],
            interactabilityResourceState[1]
        }));
        if (rubble == EObjectRubble.DESTROY)
        {
            if (array != null)
            {
                byte[] array2 = new byte[array.Length + 1];
                Array.Copy(array, array2, array.Length);
                array = array2;
            }
            else
            {
                array = new byte[1];
            }
            array[^1] = (byte)((!Level.isEditor || rubbleEditor != EObjectRubbleEditor.DEAD) ? byte.MaxValue : 0);
        }
        return array;
    }

    public bool areConditionsMet(Player player)
    {
        if (conditions != null)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!conditions[i].isConditionMet(player))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// If any conditions use flags they will be added to a set,
    /// otherwise null is returned.
    /// </summary>
    internal HashSet<ushort> GetConditionAssociatedFlags()
    {
        if (conditions == null)
        {
            return null;
        }
        INPCCondition[] array = conditions;
        for (int i = 0; i < array.Length; i++)
        {
            array[i].GatherAssociatedFlags(tempAssociatedFlags);
        }
        if (tempAssociatedFlags.Count > 0)
        {
            HashSet<ushort> result = tempAssociatedFlags;
            tempAssociatedFlags = new HashSet<ushort>();
            return result;
        }
        return null;
    }

    public bool areInteractabilityConditionsMet(Player player)
    {
        if (interactabilityConditions != null)
        {
            for (int i = 0; i < interactabilityConditions.Length; i++)
            {
                if (!interactabilityConditions[i].isConditionMet(player))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void ApplyInteractabilityConditions(Player player)
    {
        if (interactabilityConditions != null)
        {
            for (int i = 0; i < interactabilityConditions.Length; i++)
            {
                interactabilityConditions[i].ApplyCondition(player);
            }
        }
    }

    public void GrantInteractabilityRewards(Player player)
    {
        interactabilityRewards.Grant(player);
    }

    /// <summary>
    /// Recursively change all children including root from oldTag to newTag.
    /// Aborts if a child doesn't match the old tag because it might be something we shouldn't change the tag of.
    /// <return>True if tags were all successfully changed.</return>
    /// </summary>
    protected bool recursivelyFixTag(GameObject parentGameObject, string oldTag, string newTag)
    {
        if (parentGameObject.CompareTag(oldTag))
        {
            parentGameObject.tag = newTag;
            int childCount = parentGameObject.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject gameObject = parentGameObject.transform.GetChild(i).gameObject;
                if (!recursivelyFixTag(gameObject, oldTag, newTag))
                {
                    return false;
                }
            }
            return true;
        }
        Assets.ReportError(this, "unable to automatically fix tag for " + objectName + "'s " + parentGameObject.name + "! Trying to convert tag " + oldTag + " to " + newTag);
        return false;
    }

    /// <summary>
    /// Recursively change all children including root from oldLayer to newLayer.
    /// Aborts if a child doesn't match the old layer because it might be something we shouldn't change the layer of.
    /// <return>True if layers were all successfully changed.</return>
    /// </summary>
    protected bool recursivelyFixLayer(GameObject parentGameObject, int oldLayer, int newLayer)
    {
        if (parentGameObject.layer == oldLayer)
        {
            parentGameObject.layer = newLayer;
            int childCount = parentGameObject.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject gameObject = parentGameObject.transform.GetChild(i).gameObject;
                if (!recursivelyFixLayer(gameObject, oldLayer, newLayer))
                {
                    return false;
                }
            }
            return true;
        }
        Assets.ReportError(this, "Unable to automatically fix layer for " + objectName + "'s " + parentGameObject.name + "! Trying to convert layer " + oldLayer + " to " + newLayer);
        return false;
    }

    protected void fixTagAndLayer(GameObject rootGameObject, string expectedTag, int expectedLayer)
    {
        if (!rootGameObject.CompareTag(expectedTag))
        {
            string tag = rootGameObject.tag;
            recursivelyFixTag(rootGameObject, tag, expectedTag);
        }
        if (rootGameObject.layer != expectedLayer)
        {
            int layer = rootGameObject.layer;
            recursivelyFixLayer(rootGameObject, layer, expectedLayer);
        }
    }

    /// <summary>
    /// Called if we have a valid Nav GameObject.
    /// Recast requires any meshes used on the Nav objects to be CPU readable, so we log errors here if they're not marked as such.
    /// </summary>
    private void ensureNavMeshReadable()
    {
        navMCs.Clear();
        navGameObject?.getOrLoad().GetComponentsInChildren(includeInactive: true, navMCs);
        foreach (MeshCollider navMC in navMCs)
        {
            if (navMC.sharedMesh == null)
            {
                Assets.ReportError(this, "missing mesh for MeshCollider '" + navMC.name + "'");
            }
            else if (!navMC.sharedMesh.isReadable)
            {
                Assets.ReportError(this, "mesh must have read/write enabled for MeshCollider '" + navMC.name + "'");
            }
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _objectName = localization.format("Name");
        type = (EObjectType)Enum.Parse(typeof(EObjectType), data.GetString("Type"), ignoreCase: true);
        if (type == EObjectType.NPC)
        {
            if (Dedicator.IsDedicatedServer)
            {
                loadedModel = Resources.Load<GameObject>("Characters/NPC_Server");
            }
            else
            {
                loadedModel = Resources.Load<GameObject>("Characters/NPC_Client");
            }
            hasLoadedModel = true;
            useScale = true;
            interactability = EObjectInteractability.NPC;
            chart = EObjectChart.IGNORE;
        }
        else if (type == EObjectType.DECAL)
        {
            float num = data.ParseFloat("Decal_X", 1f);
            float num2 = data.ParseFloat("Decal_Y", 1f);
            float num3 = 1f;
            if (data.ContainsKey("Decal_LOD_Bias"))
            {
                num3 = data.ParseFloat("Decal_LOD_Bias");
            }
            Texture2D texture2D = bundle.load<Texture2D>("Decal");
            if (texture2D == null)
            {
                Assets.ReportError(this, "missing 'Decal' Texture2D. It will show as pure white without one.");
            }
            bool flag = data.ContainsKey("Decal_Alpha");
            hasLoadedModel = true;
            loadedModel = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(flag ? "Materials/Decal_Template_Alpha" : "Materials/Decal_Template_Masked"));
            loadedModel.transform.position = new Vector3(-10000f, -10000f, -10000f);
            loadedModel.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(loadedModel);
            loadedModel.GetComponent<BoxCollider>().size = new Vector3(num2, num, 1f);
            Decal component = loadedModel.transform.Find("Decal").GetComponent<Decal>();
            Material material = UnityEngine.Object.Instantiate(component.material);
            material.name = "Decal_Deferred";
            material.hideFlags = HideFlags.DontSave;
            material.SetTexture("_MainTex", texture2D);
            component.material = material;
            component.lodBias = num3;
            component.transform.localScale = new Vector3(num, num2, 1f);
            MeshRenderer component2 = loadedModel.transform.Find("Mesh").GetComponent<MeshRenderer>();
            Material material2 = UnityEngine.Object.Instantiate(component2.sharedMaterial);
            material2.name = "Decal_Forward";
            material2.hideFlags = HideFlags.DontSave;
            material2.SetTexture("_MainTex", texture2D);
            component2.sharedMaterial = material2;
            component2.transform.localScale = new Vector3(num2, num, 1f);
            useScale = true;
            chart = EObjectChart.IGNORE;
        }
        else
        {
            if (data.ContainsKey("Interactability"))
            {
                interactability = (EObjectInteractability)Enum.Parse(typeof(EObjectInteractability), data.GetString("Interactability"), ignoreCase: true);
                interactabilityRemote = data.ContainsKey("Interactability_Remote");
                interactabilityDelay = data.ParseFloat("Interactability_Delay");
                interactabilityReset = data.ParseFloat("Interactability_Reset");
                if (data.ContainsKey("Interactability_Hint"))
                {
                    interactabilityHint = (EObjectInteractabilityHint)Enum.Parse(typeof(EObjectInteractabilityHint), data.GetString("Interactability_Hint"), ignoreCase: true);
                }
                if (interactability == EObjectInteractability.NOTE)
                {
                    ushort num4 = data.ParseUInt16("Interactability_Text_Lines", 0);
                    StringBuilder stringBuilder = new StringBuilder();
                    for (ushort num5 = 0; num5 < num4; num5++)
                    {
                        string desc = localization.format("Interactability_Text_Line_" + num5);
                        desc = ItemTool.filterRarityRichText(desc);
                        RichTextUtil.replaceNewlineMarkup(ref desc);
                        stringBuilder.AppendLine(desc);
                    }
                    interactabilityText = stringBuilder.ToString();
                }
                else
                {
                    interactabilityText = localization.read("Interact");
                    if (string.IsNullOrWhiteSpace(interactabilityText))
                    {
                        if (interactability == EObjectInteractability.QUEST)
                        {
                            Assets.ReportError(this, "Interact text empty");
                        }
                    }
                    else
                    {
                        interactabilityText = ItemTool.filterRarityRichText(interactabilityText);
                        RichTextUtil.replaceNewlineMarkup(ref interactabilityText);
                    }
                }
                if (data.ContainsKey("Interactability_Power"))
                {
                    interactabilityPower = (EObjectInteractabilityPower)Enum.Parse(typeof(EObjectInteractabilityPower), data.GetString("Interactability_Power"), ignoreCase: true);
                }
                else
                {
                    interactabilityPower = EObjectInteractabilityPower.NONE;
                }
                if (data.ContainsKey("Interactability_Editor"))
                {
                    interactabilityEditor = (EObjectInteractabilityEditor)Enum.Parse(typeof(EObjectInteractabilityEditor), data.GetString("Interactability_Editor"), ignoreCase: true);
                }
                else
                {
                    interactabilityEditor = EObjectInteractabilityEditor.NONE;
                }
                if (data.ContainsKey("Interactability_Nav"))
                {
                    interactabilityNav = (EObjectInteractabilityNav)Enum.Parse(typeof(EObjectInteractabilityNav), data.GetString("Interactability_Nav"), ignoreCase: true);
                }
                else
                {
                    interactabilityNav = EObjectInteractabilityNav.NONE;
                }
                interactabilityDrops = new ushort[data.ParseUInt8("Interactability_Drops", 0)];
                for (byte b = 0; b < interactabilityDrops.Length; b++)
                {
                    interactabilityDrops[b] = data.ParseUInt16("Interactability_Drop_" + b, 0);
                }
                interactabilityRewardID = data.ParseUInt16("Interactability_Reward_ID", 0);
                interactabilityEffect = data.ParseGuidOrLegacyId("Interactability_Effect", out interactabilityEffectGuid);
                if (interactability == EObjectInteractability.DIALOGUE)
                {
                    interactabilityDialogueRef = data.readAssetReference<DialogueAsset>("Interactability_Dialogue");
                }
                if (interactability == EObjectInteractability.BINARY_STATE)
                {
                    interactabilityChildPathOverride = data.GetString("Interactability_Animation_Component_Path");
                }
                interactabilityConditions = new INPCCondition[data.ParseUInt8("Interactability_Conditions", 0)];
                NPCTool.readConditions(data, localization, "Interactability_Condition_", interactabilityConditions, this);
                interactabilityRewards.Parse(data, localization, this, "Interactability_Rewards", "Interactability_Reward_");
                interactabilityResource = data.ParseUInt16("Interactability_Resource", 0);
                interactabilityResourceState = BitConverter.GetBytes(interactabilityResource);
            }
            else
            {
                interactability = EObjectInteractability.NONE;
                interactabilityPower = EObjectInteractabilityPower.NONE;
                interactabilityEditor = EObjectInteractabilityEditor.NONE;
            }
            if (interactability == EObjectInteractability.RUBBLE)
            {
                rubble = EObjectRubble.DESTROY;
                rubbleReset = data.ParseFloat("Interactability_Reset");
                rubbleHealth = data.ParseUInt16("Interactability_Health", 0);
                rubbleEffect = data.ParseGuidOrLegacyId("Interactability_Effect", out rubbleEffectGuid);
                rubbleFinale = data.ParseGuidOrLegacyId("Interactability_Finale", out rubbleFinaleGuid);
                rubbleRewardID = data.ParseUInt16("Interactability_Reward_ID", 0);
                rubbleBladeID = data.ParseUInt8("Interactability_Blade_ID", 0);
                rubbleRewardProbability = data.ParseFloat("Interactability_Reward_Probability", 1f);
                rubbleRewardsMin = data.ParseUInt8("Interactability_Rewards_Min", 1);
                rubbleRewardsMax = data.ParseUInt8("Interactability_Rewards_Max", 1);
                rubbleRewardXP = data.ParseUInt32("Interactability_Reward_XP");
                rubbleIsVulnerable = !data.ContainsKey("Interactability_Invulnerable");
                rubbleProofExplosion = data.ContainsKey("Interactability_Proof_Explosion");
                RubbleCanZombiesDamage = false;
                RubbleZombieDamageMultiplier = 1f;
                RubbleNavMode = EObjectRubbleNavMode.Unaffected;
            }
            else if (data.ContainsKey("Rubble"))
            {
                rubble = (EObjectRubble)Enum.Parse(typeof(EObjectRubble), data.GetString("Rubble"), ignoreCase: true);
                rubbleReset = data.ParseFloat("Rubble_Reset");
                rubbleHealth = data.ParseUInt16("Rubble_Health", 0);
                rubbleEffect = data.ParseGuidOrLegacyId("Rubble_Effect", out rubbleEffectGuid);
                rubbleFinale = data.ParseGuidOrLegacyId("Rubble_Finale", out rubbleFinaleGuid);
                rubbleRewardID = data.ParseUInt16("Rubble_Reward_ID", 0);
                rubbleBladeID = data.ParseUInt8("Rubble_Blade_ID", 0);
                rubbleRewardProbability = data.ParseFloat("Rubble_Reward_Probability", 1f);
                rubbleRewardsMin = data.ParseUInt8("Rubble_Rewards_Min", 1);
                rubbleRewardsMax = data.ParseUInt8("Rubble_Rewards_Max", 1);
                rubbleRewardXP = data.ParseUInt32("Rubble_Reward_XP");
                rubbleIsVulnerable = !data.ContainsKey("Rubble_Invulnerable");
                rubbleProofExplosion = data.ContainsKey("Rubble_Proof_Explosion");
                RubbleCanZombiesDamage = data.ParseBool("Rubble_Can_Zombies_Damage");
                RubbleZombieDamageMultiplier = data.ParseFloat("Rubble_Zombie_Damage_Multiplier", 1f);
                RubbleNavMode = data.ParseEnum("Rubble_Nav_Mode", EObjectRubbleNavMode.Unaffected);
                if (data.ContainsKey("Rubble_Editor"))
                {
                    rubbleEditor = (EObjectRubbleEditor)Enum.Parse(typeof(EObjectRubbleEditor), data.GetString("Rubble_Editor"), ignoreCase: true);
                }
                else
                {
                    rubbleEditor = EObjectRubbleEditor.ALIVE;
                }
            }
            if (Dedicator.IsDedicatedServer && data.ParseBool("Has_Clip_Prefab", defaultValue: true))
            {
                bundle.loadDeferred("Clip", out legacyServerModel, (LoadedAssetDeferredCallback<GameObject>)OnServerModelLoaded);
            }
            bundle.loadDeferred("Object", out clientModel, (LoadedAssetDeferredCallback<GameObject>)OnClientModelLoaded);
            if (!Dedicator.IsDedicatedServer)
            {
                bundle.loadDeferred("Skybox", out skyboxGameObject, (LoadedAssetDeferredCallback<GameObject>)null);
            }
            bundle.loadDeferred("Nav", out navGameObject, (LoadedAssetDeferredCallback<GameObject>)onNavGameObjectLoaded);
            bundle.loadDeferred("Slots", out slotsGameObject, (LoadedAssetDeferredCallback<GameObject>)onSlotsGameObjectLoaded);
            bundle.loadDeferred("Triggers", out triggersGameObject, (LoadedAssetDeferredCallback<GameObject>)null);
            isSnowshoe = data.ContainsKey("Snowshoe");
            if (data.ContainsKey("Chart"))
            {
                chart = (EObjectChart)Enum.Parse(typeof(EObjectChart), data.GetString("Chart"), ignoreCase: true);
            }
            else
            {
                chart = EObjectChart.NONE;
            }
            isFuel = data.ContainsKey("Fuel");
            isRefill = data.ContainsKey("Refill");
            isSoft = data.ContainsKey("Soft");
            causesFallDamage = data.ParseBool("Causes_Fall_Damage", defaultValue: true);
            isCollisionImportant = data.ContainsKey("Collision_Important") || type == EObjectType.LARGE;
            shouldExcludeFromCullingVolumes = data.ParseBool("Exclude_From_Culling_Volumes");
            if (isFuel || isRefill)
            {
                Assets.ReportError(this, "is using the legacy fuel/water system");
            }
            if (data.ContainsKey("LOD"))
            {
                lod = (EObjectLOD)Enum.Parse(typeof(EObjectLOD), data.GetString("LOD"), ignoreCase: true);
                lodBias = data.ParseFloat("LOD_Bias");
                if (lodBias < 0.01f)
                {
                    lodBias = 1f;
                }
                cullingVolumeLocalPositionOffset = data.LegacyParseVector3("LOD_Center");
                cullingVolumeSizeOffset = data.LegacyParseVector3("LOD_Size");
            }
            if (data.ContainsKey("Foliage"))
            {
                foliage = new AssetReference<FoliageInfoCollectionAsset>(new Guid(data.GetString("Foliage")));
            }
            useWaterHeightTransparentSort = data.ContainsKey("Use_Water_Height_Transparent_Sort");
            shouldAddNightLightScript = data.ContainsKey("Add_Night_Light_Script");
            shouldAddKillTriggers = data.ParseBool("Add_Kill_Triggers");
            allowStructures = data.ContainsKey("Allow_Structures");
            if (data.ContainsKey("Material_Palette"))
            {
                materialPalette = new AssetReference<MaterialPaletteAsset>(data.ParseGuid("Material_Palette"));
            }
            if (data.ContainsKey("Landmark_Quality"))
            {
                landmarkQuality = (EGraphicQuality)Enum.Parse(typeof(EGraphicQuality), data.GetString("Landmark_Quality"), ignoreCase: true);
                if (landmarkQuality < EGraphicQuality.LOW)
                {
                    landmarkQuality = EGraphicQuality.LOW;
                }
            }
            else
            {
                landmarkQuality = EGraphicQuality.LOW;
            }
        }
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
        christmasRedirect = data.readAssetReference<ObjectAsset>("Christmas_Redirect");
        halloweenRedirect = data.readAssetReference<ObjectAsset>("Halloween_Redirect");
        isGore = data.ParseBool("Is_Gore");
        shouldExcludeFromLevelBatching = data.ParseBool("Exclude_From_Level_Batching");
        shouldExcludeFromLevelBatching |= type == EObjectType.NPC || type == EObjectType.DECAL;
        bool defaultValue = holidayRestriction != ENPCHoliday.NONE;
        ShouldExcludeFromSatelliteCapture = data.ParseBool("Exclude_From_Satellite_Capture", defaultValue);
        bool defaultValue2 = type == EObjectType.MEDIUM || type == EObjectType.LARGE;
        ShouldLoadNavOnServer = data.ParseBool("Load_Nav_On_Server", defaultValue2);
        ShouldLoadNavInEditor = data.ParseBool("Load_Nav_In_Editor", defaultValue2);
        conditions = new INPCCondition[data.ParseUInt8("Conditions", 0)];
        NPCTool.readConditions(data, localization, "Condition_", conditions, this);
    }

    [Obsolete("Removed shouldSend parameter")]
    public void applyInteractabilityConditions(Player player, bool shouldSend)
    {
        ApplyInteractabilityConditions(player);
    }

    [Obsolete("Removed shouldSend parameter")]
    public void grantInteractabilityRewards(Player player, bool shouldSend)
    {
        GrantInteractabilityRewards(player);
    }
}
