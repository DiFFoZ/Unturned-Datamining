using System;
using System.Collections.Generic;
using SDG.Framework.Devkit;
using SDG.Framework.Foliage;
using UnityEngine;
using UnityEngine.Rendering;

namespace SDG.Unturned;

public class LevelObject
{
    private static List<Rigidbody> reuseableRigidbodyList = new List<Rigidbody>();

    /// <summary>
    /// If true, object is within a culling volume.
    /// Name is old and not very specific, but not changing because it's public.
    /// </summary>
    public bool isSpeciallyCulled;

    private bool isDecal;

    private Transform _transform;

    private Transform _skybox;

    private List<Renderer> renderers;

    private ushort _id;

    private Guid _GUID;

    private uint _instanceID;

    internal AssetReference<MaterialPaletteAsset> customMaterialOverride;

    internal int materialIndexOverride = -1;

    /// <summary>
    /// If true, <see cref="F:SDG.Unturned.LevelObject.ownedCullingVolume" /> can be instantiated. Defaults to true.
    /// Enables mappers to remove culling volumes embedded in objects if they're causing issues.
    /// </summary>
    internal bool isOwnedCullingVolumeAllowed;

    public byte[] state;

    private ObjectAsset _asset;

    private InteractableObject _interactableObj;

    private InteractableObjectRubble _rubble;

    internal CullingVolume ownedCullingVolume;

    private bool areConditionsMet;

    private bool haveConditionsBeenChecked;

    private bool interactableWantsNavActive = true;

    private bool rubbleWantsNavActive = true;

    /// <summary>
    /// Assume renderers default to enabled.
    /// </summary>
    private bool areRenderersEnabled = true;

    private GameObject navGameObject;

    private HashSet<ushort> associatedFlags;

    private static CommandLineFlag disableCullingVolumes = new CommandLineFlag(defaultValue: false, "-DisableCullingVolumes");

    public Transform transform => _transform;

    /// <summary>
    /// Transform created to preserve objects whose assets failed to load.
    /// Separate from default transform to avoid messing with old behavior when transform is null.
    /// </summary>
    public Transform placeholderTransform { get; protected set; }

    public Transform skybox => _skybox;

    public ushort id => _id;

    public Guid GUID => _GUID;

    public uint instanceID => _instanceID;

    public ObjectAsset asset => _asset;

    public InteractableObject interactable => _interactableObj;

    public InteractableObjectRubble rubble => _rubble;

    /// <summary>
    /// Can this object's rubble be damaged?
    /// Allows holiday restrictions to be taken into account. (Otherwise holiday presents could be destroyed out of season.)
    /// </summary>
    public bool canDamageRubble => areConditionsMet;

    public ELevelObjectPlacementOrigin placementOrigin { get; protected set; }

    [Obsolete]
    public bool isCollisionEnabled { get; private set; }

    [Obsolete]
    public bool isVisualEnabled { get; private set; }

    [Obsolete]
    public bool isSkyboxEnabled { get; private set; }

    public bool isLandmarkQualityMet
    {
        get
        {
            if (asset == null)
            {
                return false;
            }
            if (Dedicator.IsDedicatedServer)
            {
                return false;
            }
            return GraphicsSettings.landmarkQuality >= asset.landmarkQuality;
        }
    }

    /// <summary>
    /// Object activation is time-sliced, so this does not necessarily match whether the region is active.
    /// </summary>
    internal bool isActiveInRegion { get; private set; }

    /// <summary>
    /// Defaults to true because most objects are not inside a culling volume. 
    /// </summary>
    internal bool isVisibleInCullingVolume { get; private set; } = true;


    [Obsolete]
    public string name => null;

    internal void SetIsActiveInRegion(bool isActive)
    {
        if (isActiveInRegion != isActive)
        {
            isActiveInRegion = isActive;
            UpdateActiveAndRenderersEnabled();
        }
    }

    internal void SetIsVisibleInCullingVolume(bool isVisible)
    {
        if (isVisibleInCullingVolume != isVisible)
        {
            isVisibleInCullingVolume = isVisible;
            UpdateActiveAndRenderersEnabled();
        }
    }

    internal void SetIsActiveOverrideForSatelliteCapture(bool isActive)
    {
        if (transform != null)
        {
            transform.gameObject.SetActive(isActive);
        }
        if (skybox != null)
        {
            skybox.gameObject.SetActive(value: false);
        }
        SetRenderersEnabled(isEnabled: true);
    }

    public void destroy()
    {
        if ((bool)transform)
        {
            UnityEngine.Object.Destroy(transform.gameObject);
        }
        if ((bool)skybox)
        {
            UnityEngine.Object.Destroy(skybox.gameObject);
        }
        if (ownedCullingVolume != null)
        {
            UnityEngine.Object.Destroy(ownedCullingVolume.gameObject);
            ownedCullingVolume = null;
        }
    }

    internal void ReapplyMaterialOverrides()
    {
        Material materialOverride = GetMaterialOverride();
        if (materialOverride == null)
        {
            return;
        }
        if (skybox != null)
        {
            renderers.Clear();
            skybox.GetComponentsInChildren(includeInactive: true, renderers);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = materialOverride;
            }
        }
        renderers.Clear();
        transform.GetComponentsInChildren(includeInactive: true, renderers);
        foreach (Renderer renderer2 in renderers)
        {
            renderer2.sharedMaterial = materialOverride;
        }
    }

    internal void ReapplyOwnedCullingVolumeAllowed()
    {
        if (asset.lod != 0 && isOwnedCullingVolumeAllowed)
        {
            if (ownedCullingVolume == null)
            {
                GameObject gameObject = new GameObject();
                ownedCullingVolume = gameObject.AddComponent<CullingVolume>();
                ownedCullingVolume.SetupForLevelObject(this);
            }
        }
        else if (ownedCullingVolume != null)
        {
            UnityEngine.Object.Destroy(ownedCullingVolume.gameObject);
            ownedCullingVolume = null;
        }
    }

    /// <summary>
    /// Used by InteractableObjectBinaryState to indicate whether it wants navGameObject to be active. It's only
    /// active if both IOBS and Rubble want it active.
    /// </summary>
    internal void SetInteractableWantsNavActive(bool wantsNavActive)
    {
        if (interactableWantsNavActive != wantsNavActive)
        {
            interactableWantsNavActive = wantsNavActive;
            UpdateNavActive();
        }
    }

    /// <summary>
    /// Used by InteractableObjectRubble to indicate whether it wants navGameObject to be active. It's only
    /// active if both IOBS and Rubble want it active.
    /// </summary>
    internal void SetRubbleWantsNavActive(bool wantsNavActive)
    {
        if (rubbleWantsNavActive != wantsNavActive)
        {
            rubbleWantsNavActive = wantsNavActive;
            UpdateNavActive();
        }
    }

    private Material GetMaterialOverride()
    {
        Material material = null;
        AssetReference<MaterialPaletteAsset> materialPalette = customMaterialOverride;
        if (!materialPalette.isValid)
        {
            materialPalette = asset.materialPalette;
        }
        if (materialPalette.isValid)
        {
            MaterialPaletteAsset materialPaletteAsset = Assets.find(materialPalette);
            if (materialPaletteAsset != null && materialPaletteAsset.materials != null && materialPaletteAsset.materials.Count > 0)
            {
                int num;
                if (materialIndexOverride == -1)
                {
                    UnityEngine.Random.State obj = UnityEngine.Random.state;
                    UnityEngine.Random.InitState((int)instanceID);
                    num = UnityEngine.Random.Range(0, materialPaletteAsset.materials.Count);
                    UnityEngine.Random.state = obj;
                }
                else
                {
                    num = Mathf.Clamp(materialIndexOverride, 0, materialPaletteAsset.materials.Count - 1);
                }
                material = Assets.load(materialPaletteAsset.materials[num]);
                if (material == null)
                {
                    UnturnedLog.warn($"Object \"{asset?.FriendlyName}\" with palette \"{materialPaletteAsset.FriendlyName}\" has invalid material at index {num}");
                }
            }
            else
            {
                string arg = ((_transform != null) ? _transform.position.ToString() : "(no transform)");
                UnturnedLog.warn($"Object \"{asset?.FriendlyName}\" at {arg} has invalid material palette {materialPalette}");
            }
        }
        return material;
    }

    private void updateConditions()
    {
        if (asset != null)
        {
            bool flag = true;
            if (!Dedicator.IsDedicatedServer)
            {
                flag = asset.areConditionsMet(Player.player);
                flag &= OptionsSettings.gore || !asset.isGore;
            }
            if (flag && asset.holidayRestriction != 0)
            {
                flag = HolidayUtil.isHolidayActive(asset.holidayRestriction);
            }
            if (areConditionsMet != flag || !haveConditionsBeenChecked)
            {
                areConditionsMet = flag;
                haveConditionsBeenChecked = true;
                UpdateActiveAndRenderersEnabled();
            }
        }
    }

    private void onExternalConditionsUpdated()
    {
        updateConditions();
    }

    private void OnLocalPlayerQuestsChanged(ushort id)
    {
        updateConditions();
    }

    /// <summary>
    /// Used if the object asset has weather blend alpha conditions.
    /// </summary>
    private void OnWeatherBlendAlphaChanged(WeatherAssetBase weatherAsset, float blendAlpha)
    {
        updateConditions();
    }

    /// <summary>
    /// Used if the object asset has weather status conditions.
    /// </summary>
    private void OnWeatherStatusChanged(WeatherAssetBase weatherAsset, EWeatherStatusChange statusChange)
    {
        updateConditions();
    }

    private void onFlagsUpdated()
    {
        updateConditions();
    }

    /// <summary>
    /// Callback when an individual quest flag changes for the local player.
    /// Refreshes visibility conditions if the flag was relevant to this object.
    /// </summary>
    private void onFlagUpdated(ushort id)
    {
        if (associatedFlags != null && associatedFlags.Contains(id))
        {
            updateConditions();
        }
    }

    private void onPlayerCreated(Player player)
    {
        if (!player.channel.IsLocalPlayer)
        {
            return;
        }
        Player.onPlayerCreated = (PlayerCreated)Delegate.Remove(Player.onPlayerCreated, new PlayerCreated(onPlayerCreated));
        bool flag = false;
        bool flag2 = false;
        INPCCondition[] conditions = asset.conditions;
        foreach (INPCCondition iNPCCondition in conditions)
        {
            if (iNPCCondition is NPCTimeOfDayCondition || iNPCCondition is NPCIsFullMoonCondition || iNPCCondition is NPCDateCounterCondition)
            {
                flag = true;
            }
            else if (iNPCCondition is NPCQuestCondition)
            {
                flag2 = true;
            }
        }
        conditions = asset.conditions;
        foreach (INPCCondition iNPCCondition2 in conditions)
        {
            if (iNPCCondition2 is NPCWeatherBlendAlphaCondition { weather: var weather })
            {
                WeatherEventListenerManager.AddBlendAlphaListener(weather.GUID, OnWeatherBlendAlphaChanged);
            }
            else if (iNPCCondition2 is NPCWeatherStatusCondition { weather: var weather2 })
            {
                WeatherEventListenerManager.AddStatusListener(weather2.GUID, OnWeatherStatusChanged);
            }
        }
        if (flag)
        {
            PlayerQuests quests = Player.player.quests;
            quests.onExternalConditionsUpdated = (ExternalConditionsUpdated)Delegate.Combine(quests.onExternalConditionsUpdated, new ExternalConditionsUpdated(onExternalConditionsUpdated));
        }
        PlayerQuests quests2 = Player.player.quests;
        quests2.onFlagsUpdated = (FlagsUpdated)Delegate.Combine(quests2.onFlagsUpdated, new FlagsUpdated(onFlagsUpdated));
        associatedFlags = asset.GetConditionAssociatedFlags();
        if (associatedFlags != null)
        {
            PlayerQuests quests3 = Player.player.quests;
            quests3.onFlagUpdated = (FlagUpdated)Delegate.Combine(quests3.onFlagUpdated, new FlagUpdated(onFlagUpdated));
        }
        if (flag2)
        {
            Player.player.quests.OnLocalPlayerQuestsChanged += OnLocalPlayerQuestsChanged;
        }
        updateConditions();
    }

    private void LoadAsset()
    {
        if (!Assets.shouldLoadAnyAssets)
        {
            _asset = null;
            return;
        }
        if (GUID == Guid.Empty)
        {
            _asset = Assets.find(EAssetType.OBJECT, id) as ObjectAsset;
            if (asset != null)
            {
                UnturnedLog.info("Object without GUID loaded by legacy ID {0}, updating to {1} \"{2}\"", asset.id, asset.GUID, asset.FriendlyName);
                _GUID = asset.GUID;
            }
            else
            {
                UnturnedLog.warn("Unable to find object by legacy ID {0}", id);
            }
            return;
        }
        _asset = Assets.find(new AssetReference<ObjectAsset>(GUID));
        if (!Dedicator.IsDedicatedServer)
        {
            ClientAssetIntegrity.QueueRequest(GUID, asset, "Object");
        }
        if (asset == null)
        {
            ClientAssetIntegrity.ServerAddKnownMissingAsset(GUID, "Object");
            _asset = Assets.find(EAssetType.OBJECT, id) as ObjectAsset;
            if (asset != null)
            {
                UnturnedLog.info($"Unable to find object for GUID {GUID:N} found by legacy ID {id}, updating to {asset.GUID:N} \"{asset.FriendlyName}\"");
                _GUID = asset.GUID;
            }
            else
            {
                UnturnedLog.warn($"Unable to find object for GUID {GUID:N}, nor by legacy ID {id}");
            }
        }
    }

    [Obsolete]
    public LevelObject(Vector3 newPoint, Quaternion newRotation, Vector3 newScale, ushort newID, string newName, Guid newGUID, ELevelObjectPlacementOrigin newPlacementOrigin, uint newInstanceID)
        : this(newPoint, newRotation, newScale, newID, newName, newGUID, newPlacementOrigin, newInstanceID, AssetReference<MaterialPaletteAsset>.invalid, -1, newIsHierarchyItem: false)
    {
    }

    [Obsolete]
    public LevelObject(Vector3 newPoint, Quaternion newRotation, Vector3 newScale, ushort newID, string newName, Guid newGUID, ELevelObjectPlacementOrigin newPlacementOrigin, uint newInstanceID, AssetReference<MaterialPaletteAsset> customMaterialOverride, int materialIndexOverride, bool newIsHierarchyItem)
        : this(newPoint, newRotation, newScale, newID, newGUID, newPlacementOrigin, newInstanceID, customMaterialOverride, materialIndexOverride, null, NetId.INVALID)
    {
    }

    [Obsolete]
    internal LevelObject(Vector3 newPoint, Quaternion newRotation, Vector3 newScale, ushort newID, Guid newGUID, ELevelObjectPlacementOrigin newPlacementOrigin, uint newInstanceID, AssetReference<MaterialPaletteAsset> customMaterialOverride, int materialIndexOverride, DevkitHierarchyWorldObject devkitOwner, NetId netId)
        : this(newPoint, newRotation, newScale, newID, newGUID, newPlacementOrigin, newInstanceID, customMaterialOverride, materialIndexOverride, netId, isOwnedCullingVolumeAllowed: true)
    {
    }

    internal LevelObject(Vector3 newPoint, Quaternion newRotation, Vector3 newScale, ushort newID, Guid newGUID, ELevelObjectPlacementOrigin newPlacementOrigin, uint newInstanceID, AssetReference<MaterialPaletteAsset> customMaterialOverride, int materialIndexOverride, NetId netId, bool isOwnedCullingVolumeAllowed)
    {
        _id = newID;
        _GUID = newGUID;
        _instanceID = newInstanceID;
        placementOrigin = newPlacementOrigin;
        this.customMaterialOverride = customMaterialOverride;
        this.materialIndexOverride = materialIndexOverride;
        this.isOwnedCullingVolumeAllowed = isOwnedCullingVolumeAllowed;
        LoadAsset();
        if (asset == null)
        {
            if ((bool)LevelObjects.preserveMissingAssets)
            {
                placeholderTransform = new GameObject().transform;
                placeholderTransform.position = newPoint;
                placeholderTransform.rotation = newRotation;
                placeholderTransform.localScale = newScale;
            }
            return;
        }
        state = asset.getState();
        areConditionsMet = true;
        haveConditionsBeenChecked = false;
        GameObject orLoadModel = asset.GetOrLoadModel();
        if (Dedicator.IsDedicatedServer)
        {
            if (orLoadModel != null)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(orLoadModel, newPoint, newRotation);
                _transform = gameObject.transform;
                gameObject.name = asset.name;
                NetIdRegistry.AssignTransform(netId, _transform);
                isDecal = this.transform.Find("Decal");
                if (isDecal)
                {
                    gameObject.hideFlags = HideFlags.None;
                }
                if (asset.useScale)
                {
                    this.transform.localScale = newScale;
                }
            }
            renderers = null;
        }
        else if (orLoadModel != null)
        {
            GameObject gameObject2 = UnityEngine.Object.Instantiate(orLoadModel, newPoint, newRotation);
            _transform = gameObject2.transform;
            gameObject2.name = asset.name;
            if (!netId.IsNull())
            {
                NetIdRegistry.AssignTransform(netId, _transform);
            }
            isDecal = this.transform.Find("Decal");
            if (isDecal)
            {
                gameObject2.hideFlags = HideFlags.None;
            }
            if (asset.useScale)
            {
                this.transform.localScale = newScale;
            }
            if (asset.useWaterHeightTransparentSort)
            {
                this.transform.gameObject.AddComponent<WaterHeightTransparentSort>();
            }
            if (asset.shouldAddNightLightScript)
            {
                NightLight nightLight = this.transform.gameObject.AddComponent<NightLight>();
                Transform transform = this.transform.Find("Light");
                if ((bool)transform)
                {
                    nightLight.target = transform.GetComponent<Light>();
                }
            }
            renderers = new List<Renderer>();
            Material materialOverride = GetMaterialOverride();
            GameObject gameObject3 = asset.skyboxGameObject?.getOrLoad();
            if (gameObject3 != null)
            {
                GameObject gameObject4 = UnityEngine.Object.Instantiate(gameObject3, newPoint, newRotation);
                _skybox = gameObject4.transform;
                gameObject4.name = asset.name + "_Skybox";
                if (asset.useScale)
                {
                    skybox.localScale = newScale;
                }
                skybox.GetComponentsInChildren(includeInactive: true, renderers);
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                    if (materialOverride != null)
                    {
                        renderers[i].sharedMaterial = materialOverride;
                    }
                }
                renderers.Clear();
            }
            this.transform.GetComponentsInChildren(includeInactive: true, renderers);
            if (materialOverride != null)
            {
                for (int j = 0; j < renderers.Count; j++)
                {
                    renderers[j].sharedMaterial = materialOverride;
                }
            }
            UpdateActiveAndRenderersEnabled();
        }
        if (!(this.transform != null))
        {
            return;
        }
        if (isDecal && !Level.isEditor && asset.interactability == EObjectInteractability.NONE && asset.rubble == EObjectRubble.NONE)
        {
            Collider component = this.transform.GetComponent<Collider>();
            if (component != null)
            {
                UnityEngine.Object.Destroy(component);
            }
        }
        if (Level.isEditor)
        {
            Rigidbody orAddComponent = this.transform.GetOrAddComponent<Rigidbody>();
            orAddComponent.useGravity = false;
            orAddComponent.isKinematic = true;
        }
        else
        {
            Rigidbody component2 = this.transform.GetComponent<Rigidbody>();
            if (component2 != null)
            {
                UnityEngine.Object.Destroy(component2);
            }
            if (asset.type == EObjectType.SMALL && asset.interactability == EObjectInteractability.NONE && asset.rubble == EObjectRubble.NONE)
            {
                Collider component3 = this.transform.GetComponent<Collider>();
                if (component3 != null)
                {
                    UnityEngine.Object.Destroy(component3);
                }
            }
        }
        bool flag = false;
        if (Provider.isServer)
        {
            flag = asset.ShouldLoadNavOnServer;
        }
        else if (Level.isEditor)
        {
            flag = asset.ShouldLoadNavInEditor;
        }
        if (flag)
        {
            GameObject gameObject5 = asset.navGameObject?.getOrLoad();
            if (gameObject5 != null)
            {
                navGameObject = UnityEngine.Object.Instantiate(gameObject5);
                Transform transform2 = navGameObject.transform;
                transform2.name = "Nav";
                transform2.parent = this.transform;
                transform2.localPosition = Vector3.zero;
                transform2.localRotation = Quaternion.identity;
                transform2.localScale = Vector3.one;
                if (Level.isEditor)
                {
                    Rigidbody orAddComponent2 = transform2.GetOrAddComponent<Rigidbody>();
                    orAddComponent2.useGravity = false;
                    orAddComponent2.isKinematic = true;
                }
                else
                {
                    reuseableRigidbodyList.Clear();
                    transform2.GetComponentsInChildren(reuseableRigidbodyList);
                    foreach (Rigidbody reuseableRigidbody in reuseableRigidbodyList)
                    {
                        UnityEngine.Object.Destroy(reuseableRigidbody);
                    }
                }
            }
        }
        if (Provider.isServer)
        {
            GameObject gameObject6 = asset.triggersGameObject?.getOrLoad();
            Transform transform3;
            if (gameObject6 != null)
            {
                Transform obj = UnityEngine.Object.Instantiate(gameObject6).transform;
                obj.name = "Triggers";
                obj.parent = this.transform;
                obj.localPosition = Vector3.zero;
                obj.localRotation = Quaternion.identity;
                obj.localScale = Vector3.one;
                transform3 = obj;
            }
            else
            {
                transform3 = this.transform;
            }
            if (asset.shouldAddKillTriggers)
            {
                foreach (Transform item in transform3)
                {
                    if (item.name.Equals("Kill", StringComparison.OrdinalIgnoreCase))
                    {
                        item.tag = "Trap";
                        item.gameObject.layer = 30;
                        item.gameObject.AddComponent<Barrier>();
                    }
                }
            }
        }
        if (asset.type != EObjectType.SMALL)
        {
            if (Level.isEditor)
            {
                Transform transform5 = this.transform.Find("Block");
                if (transform5 != null && this.transform.GetComponent<Collider>() == null)
                {
                    BoxCollider component4 = transform5.GetComponent<BoxCollider>();
                    if (component4 != null)
                    {
                        BoxCollider boxCollider = this.transform.gameObject.AddComponent<BoxCollider>();
                        boxCollider.center = component4.center;
                        boxCollider.size = component4.size;
                    }
                }
            }
            else if (Provider.isClient)
            {
                GameObject gameObject7 = asset.slotsGameObject?.getOrLoad();
                if (gameObject7 != null)
                {
                    Transform obj2 = UnityEngine.Object.Instantiate(gameObject7).transform;
                    obj2.name = "Slots";
                    obj2.parent = this.transform;
                    obj2.localPosition = Vector3.zero;
                    obj2.localRotation = Quaternion.identity;
                    obj2.localScale = Vector3.one;
                    reuseableRigidbodyList.Clear();
                    obj2.GetComponentsInChildren(reuseableRigidbodyList);
                    foreach (Rigidbody reuseableRigidbody2 in reuseableRigidbodyList)
                    {
                        UnityEngine.Object.Destroy(reuseableRigidbody2);
                    }
                }
            }
        }
        if (asset.interactability != 0)
        {
            if (asset.interactability == EObjectInteractability.BINARY_STATE)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectBinaryState>();
            }
            else if (asset.interactability == EObjectInteractability.DROPPER)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectDropper>();
            }
            else if (asset.interactability == EObjectInteractability.NOTE)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectNote>();
            }
            else if (asset.interactability == EObjectInteractability.WATER || asset.interactability == EObjectInteractability.FUEL)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectResource>();
            }
            else if (asset.interactability == EObjectInteractability.NPC)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectNPC>();
            }
            else if (asset.interactability == EObjectInteractability.QUEST)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectQuest>();
            }
            else if (asset.interactability == EObjectInteractability.DIALOGUE)
            {
                _interactableObj = this.transform.gameObject.AddComponent<InteractableObjectDialogue>();
            }
            if (interactable != null)
            {
                interactable.owningLevelObject = this;
                interactable.updateState(asset, state);
            }
        }
        if (asset.rubble != 0)
        {
            if (asset.rubble == EObjectRubble.DESTROY)
            {
                _rubble = this.transform.gameObject.AddComponent<InteractableObjectRubble>();
                _rubble.owningLevelObject = this;
            }
            if (rubble != null)
            {
                rubble.updateState(asset, state);
            }
            if (asset.rubbleEditor == EObjectRubbleEditor.DEAD && Level.isEditor)
            {
                Transform transform6 = this.transform.Find("Editor");
                if (transform6 != null)
                {
                    transform6.gameObject.SetActive(value: true);
                }
            }
        }
        bool flag2 = false;
        if (asset.conditions != null && asset.conditions.Length != 0 && !Level.isEditor && !Dedicator.IsDedicatedServer)
        {
            areConditionsMet = false;
            flag2 = true;
            Player.onPlayerCreated = (PlayerCreated)Delegate.Combine(Player.onPlayerCreated, new PlayerCreated(onPlayerCreated));
        }
        if (!flag2 && (asset.holidayRestriction != 0 || asset.isGore) && !Level.isEditor)
        {
            areConditionsMet = false;
            updateConditions();
        }
        if (asset.foliage.isValid)
        {
            FoliageSurfaceComponent foliageSurfaceComponent = this.transform.gameObject.AddComponent<FoliageSurfaceComponent>();
            foliageSurfaceComponent.foliage = asset.foliage;
            foliageSurfaceComponent.surfaceCollider = this.transform.gameObject.GetComponent<Collider>();
        }
        if (asset.lod != EObjectLOD.NONE && isOwnedCullingVolumeAllowed)
        {
            ReapplyOwnedCullingVolumeAllowed();
        }
    }

    internal void UpdateActiveAndRenderersEnabled()
    {
        isCollisionEnabled = isActiveInRegion;
        isVisualEnabled = isActiveInRegion;
        bool active;
        bool renderersEnabled;
        if (isDecal || (asset != null && asset.type == EObjectType.NPC))
        {
            active = ((isActiveInRegion && isVisibleInCullingVolume) || Dedicator.IsDedicatedServer) && areConditionsMet;
            renderersEnabled = true;
        }
        else
        {
            bool flag = (asset != null && asset.isCollisionImportant && Provider.isServer) || Dedicator.IsDedicatedServer;
            active = (isActiveInRegion || flag) && areConditionsMet;
            renderersEnabled = isActiveInRegion && (isVisibleInCullingVolume || (bool)disableCullingVolumes) && areConditionsMet;
        }
        if (transform != null)
        {
            transform.gameObject.SetActive(active);
        }
        SetRenderersEnabled(renderersEnabled);
        UpdateSkyboxActive();
    }

    /// <summary>
    /// Separate from UpdateActiveAndRenderersEnabled so graphics settings can call it.
    /// </summary>
    internal void UpdateSkyboxActive()
    {
        isSkyboxEnabled = !isActiveInRegion;
        if (skybox != null)
        {
            skybox.gameObject.SetActive(!isActiveInRegion && isLandmarkQualityMet && areConditionsMet);
        }
    }

    private void SetRenderersEnabled(bool isEnabled)
    {
        if (areRenderersEnabled == isEnabled)
        {
            return;
        }
        areRenderersEnabled = isEnabled;
        if (renderers == null)
        {
            return;
        }
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = areRenderersEnabled;
            }
        }
    }

    private void UpdateNavActive()
    {
        if (navGameObject != null)
        {
            navGameObject.SetActive(interactableWantsNavActive && rubbleWantsNavActive);
        }
    }

    [Obsolete("Replaced by SetIsActiveInRegion(true)")]
    public void enableCollision()
    {
        SetIsActiveInRegion(isActive: true);
    }

    [Obsolete("Replaced by SetIsActiveInRegion(true)")]
    public void enableVisual()
    {
        SetIsActiveInRegion(isActive: true);
    }

    [Obsolete("Replaced by SetIsActiveInRegion(false)")]
    public void enableSkybox()
    {
        SetIsActiveInRegion(isActive: false);
    }

    [Obsolete("Replaced by SetIsActiveInRegion(false)")]
    public void disableCollision()
    {
        SetIsActiveInRegion(isActive: false);
    }

    [Obsolete("Replaced by SetIsActiveInRegion(false)")]
    public void disableVisual()
    {
        SetIsActiveInRegion(isActive: false);
    }

    [Obsolete("Replaced by SetIsActiveInRegion(true)")]
    public void disableSkybox()
    {
        SetIsActiveInRegion(isActive: true);
    }
}
