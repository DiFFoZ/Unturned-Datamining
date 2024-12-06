using System;
using UnityEngine;

namespace SDG.Unturned;

public class HumanClothes : MonoBehaviour
{
    private static Shader shader;

    private static Shader clothingShader;

    private Mesh[] humanMeshes;

    private Material materialClothing;

    private Material materialHair;

    private Transform spine;

    private Transform skull;

    private Transform[] upperBones;

    private MythicalEffectController[] upperSystems;

    private Transform[] lowerBones;

    private MythicalEffectController[] lowerSystems;

    public bool isMine;

    public bool isView;

    public bool canWearPro;

    public bool isRagdoll;

    private SkinnedMeshRenderer[] characterMeshRenderers;

    private bool _isVisual = true;

    private bool _isMythic = true;

    private bool _isLeftHanded;

    private bool _hasBackpack = true;

    private bool isUpper;

    private bool isLower;

    private ItemShirtAsset visualShirtAsset;

    private ItemPantsAsset visualPantsAsset;

    private ItemHatAsset visualHatAsset;

    private ItemBackpackAsset visualBackpackAsset;

    private ItemVestAsset visualVestAsset;

    private ItemMaskAsset visualMaskAsset;

    private ItemGlassesAsset visualGlassesAsset;

    private int _visualShirt;

    private int _visualPants;

    private int _visualHat;

    public int _visualBackpack;

    public int _visualVest;

    public int _visualMask;

    public int _visualGlasses;

    private ItemShirtAsset _shirtAsset;

    private ItemPantsAsset _pantsAsset;

    private ItemHatAsset _hatAsset;

    private ItemBackpackAsset _backpackAsset;

    private ItemVestAsset _vestAsset;

    private ItemMaskAsset _maskAsset;

    private ItemGlassesAsset _glassesAsset;

    private byte _face = byte.MaxValue;

    private byte _hair;

    private byte _beard;

    private Color _skinColor;

    private Color _hairColor;

    private bool hasHair;

    private bool hasBeard;

    private bool usingHumanMeshes = true;

    private bool usingHumanMaterials = true;

    private bool hairDirty;

    private bool beardDirty;

    private bool skinColorDirty;

    private bool faceDirty;

    private bool shirtDirty;

    private bool pantsDirty;

    private bool hatDirty;

    private bool backpackDirty;

    private bool vestDirty;

    private bool maskDirty;

    private bool glassesDirty;

    internal static readonly int skinColorPropertyID = Shader.PropertyToID("_SkinColor");

    internal static readonly int flipShirtPropertyID = Shader.PropertyToID("_FlipShirt");

    internal static readonly int faceAlbedoTexturePropertyID = Shader.PropertyToID("_FaceAlbedoTexture");

    internal static readonly int faceEmissionTexturePropertyID = Shader.PropertyToID("_FaceEmissionTexture");

    internal static readonly int shirtAlbedoTexturePropertyID = Shader.PropertyToID("_ShirtAlbedoTexture");

    internal static readonly int shirtEmissionTexturePropertyID = Shader.PropertyToID("_ShirtEmissionTexture");

    internal static readonly int shirtMetallicTexturePropertyID = Shader.PropertyToID("_ShirtMetallicTexture");

    internal static readonly int pantsAlbedoTexturePropertyID = Shader.PropertyToID("_PantsAlbedoTexture");

    internal static readonly int pantsEmissionTexturePropertyID = Shader.PropertyToID("_PantsEmissionTexture");

    internal static readonly int pantsMetallicTexturePropertyID = Shader.PropertyToID("_PantsMetallicTexture");

    public Transform hatModel { get; private set; }

    public Transform backpackModel { get; private set; }

    public Transform vestModel { get; private set; }

    public Transform maskModel { get; private set; }

    public Transform glassesModel { get; private set; }

    public Transform hairModel { get; private set; }

    public Transform beardModel { get; private set; }

    public bool isVisual
    {
        get
        {
            return _isVisual;
        }
        set
        {
            if (isVisual != value)
            {
                _isVisual = value;
                markAllDirty(isDirty: true);
            }
        }
    }

    public bool isMythic
    {
        get
        {
            return _isMythic;
        }
        set
        {
            if (isMythic != value)
            {
                _isMythic = value;
                markAllDirty(isDirty: true);
            }
        }
    }

    public bool hand
    {
        get
        {
            return _isLeftHanded;
        }
        set
        {
            if (_isLeftHanded != value)
            {
                _isLeftHanded = value;
                markAllDirty(isDirty: true);
            }
        }
    }

    public bool hasBackpack
    {
        get
        {
            return _hasBackpack;
        }
        set
        {
            if (value != _hasBackpack)
            {
                _hasBackpack = value;
                if (backpackModel != null)
                {
                    backpackModel.gameObject.SetActive(hasBackpack);
                }
            }
        }
    }

    public int visualShirt
    {
        get
        {
            return _visualShirt;
        }
        set
        {
            if (visualShirt == value)
            {
                return;
            }
            _visualShirt = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualShirt != 0)
            {
                try
                {
                    visualShirtAsset = Assets.find<ItemShirtAsset>(Provider.provider.economyService.getInventoryItemGuid(visualShirt));
                }
                catch
                {
                    visualShirtAsset = null;
                }
                if (visualShirtAsset != null && !visualShirtAsset.isPro)
                {
                    _visualShirt = 0;
                    visualShirtAsset = null;
                }
            }
            else
            {
                visualShirtAsset = null;
            }
            shirtDirty = true;
        }
    }

    public int visualPants
    {
        get
        {
            return _visualPants;
        }
        set
        {
            if (visualPants == value)
            {
                return;
            }
            _visualPants = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualPants != 0)
            {
                try
                {
                    visualPantsAsset = Assets.find<ItemPantsAsset>(Provider.provider.economyService.getInventoryItemGuid(visualPants));
                }
                catch
                {
                    visualPantsAsset = null;
                }
                if (visualPantsAsset != null && !visualPantsAsset.isPro)
                {
                    _visualPants = 0;
                    visualPantsAsset = null;
                }
            }
            else
            {
                visualPantsAsset = null;
            }
            pantsDirty = true;
        }
    }

    public int visualHat
    {
        get
        {
            return _visualHat;
        }
        set
        {
            if (visualHat == value)
            {
                return;
            }
            _visualHat = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualHat != 0)
            {
                try
                {
                    visualHatAsset = Assets.find<ItemHatAsset>(Provider.provider.economyService.getInventoryItemGuid(visualHat));
                }
                catch
                {
                    visualHatAsset = null;
                }
                if (visualHatAsset != null && !visualHatAsset.isPro)
                {
                    _visualHat = 0;
                    visualHatAsset = null;
                }
            }
            else
            {
                visualHatAsset = null;
            }
            hatDirty = true;
        }
    }

    public int visualBackpack
    {
        get
        {
            return _visualBackpack;
        }
        set
        {
            if (visualBackpack == value)
            {
                return;
            }
            _visualBackpack = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualBackpack != 0)
            {
                try
                {
                    visualBackpackAsset = Assets.find<ItemBackpackAsset>(Provider.provider.economyService.getInventoryItemGuid(visualBackpack));
                }
                catch
                {
                    visualBackpackAsset = null;
                }
                if (visualBackpackAsset != null && !visualBackpackAsset.isPro)
                {
                    _visualBackpack = 0;
                    visualBackpackAsset = null;
                }
            }
            else
            {
                visualBackpackAsset = null;
            }
            backpackDirty = true;
        }
    }

    public int visualVest
    {
        get
        {
            return _visualVest;
        }
        set
        {
            if (visualVest == value)
            {
                return;
            }
            _visualVest = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualVest != 0)
            {
                try
                {
                    visualVestAsset = Assets.find<ItemVestAsset>(Provider.provider.economyService.getInventoryItemGuid(visualVest));
                }
                catch
                {
                    visualVestAsset = null;
                }
                if (visualVestAsset != null && !visualVestAsset.isPro)
                {
                    _visualVest = 0;
                    visualVestAsset = null;
                }
            }
            else
            {
                visualVestAsset = null;
            }
            vestDirty = true;
        }
    }

    public int visualMask
    {
        get
        {
            return _visualMask;
        }
        set
        {
            if (visualMask == value)
            {
                return;
            }
            _visualMask = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualMask != 0)
            {
                try
                {
                    visualMaskAsset = Assets.find<ItemMaskAsset>(Provider.provider.economyService.getInventoryItemGuid(visualMask));
                }
                catch
                {
                    visualMaskAsset = null;
                }
                if (visualMaskAsset != null && !visualMaskAsset.isPro)
                {
                    _visualMask = 0;
                    visualMaskAsset = null;
                }
            }
            else
            {
                visualMaskAsset = null;
            }
            maskDirty = true;
        }
    }

    public int visualGlasses
    {
        get
        {
            return _visualGlasses;
        }
        set
        {
            if (visualGlasses == value)
            {
                return;
            }
            _visualGlasses = value;
            if (Dedicator.IsDedicatedServer)
            {
                return;
            }
            if (visualGlasses != 0)
            {
                try
                {
                    visualGlassesAsset = Assets.find<ItemGlassesAsset>(Provider.provider.economyService.getInventoryItemGuid(visualGlasses));
                }
                catch
                {
                    visualGlassesAsset = null;
                }
                if (visualGlassesAsset != null && !visualGlassesAsset.isPro)
                {
                    _visualGlasses = 0;
                    visualGlassesAsset = null;
                }
            }
            else
            {
                visualGlassesAsset = null;
            }
            glassesDirty = true;
        }
    }

    public ItemShirtAsset shirtAsset
    {
        get
        {
            return _shirtAsset;
        }
        internal set
        {
            _shirtAsset = value;
            shirtDirty = true;
        }
    }

    public ItemPantsAsset pantsAsset
    {
        get
        {
            return _pantsAsset;
        }
        internal set
        {
            _pantsAsset = value;
            pantsDirty = true;
        }
    }

    public ItemHatAsset hatAsset
    {
        get
        {
            return _hatAsset;
        }
        internal set
        {
            _hatAsset = value;
            hatDirty = true;
        }
    }

    public ItemBackpackAsset backpackAsset
    {
        get
        {
            return _backpackAsset;
        }
        internal set
        {
            _backpackAsset = value;
            backpackDirty = true;
        }
    }

    public ItemVestAsset vestAsset
    {
        get
        {
            return _vestAsset;
        }
        internal set
        {
            _vestAsset = value;
            vestDirty = true;
        }
    }

    public ItemMaskAsset maskAsset
    {
        get
        {
            return _maskAsset;
        }
        internal set
        {
            _maskAsset = value;
            maskDirty = true;
        }
    }

    public ItemGlassesAsset glassesAsset
    {
        get
        {
            return _glassesAsset;
        }
        internal set
        {
            _glassesAsset = value;
            glassesDirty = true;
        }
    }

    public Guid shirtGuid
    {
        get
        {
            return _shirtAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _shirtAsset = Assets.find(value) as ItemShirtAsset;
            shirtDirty = true;
        }
    }

    public ushort shirt
    {
        get
        {
            return _shirtAsset?.id ?? 0;
        }
        set
        {
            _shirtAsset = Assets.find(EAssetType.ITEM, value) as ItemShirtAsset;
            shirtDirty = true;
        }
    }

    public Guid pantsGuid
    {
        get
        {
            return _pantsAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _pantsAsset = Assets.find(value) as ItemPantsAsset;
            pantsDirty = true;
        }
    }

    public ushort pants
    {
        get
        {
            return _pantsAsset?.id ?? 0;
        }
        set
        {
            _pantsAsset = Assets.find(EAssetType.ITEM, value) as ItemPantsAsset;
            pantsDirty = true;
        }
    }

    public Guid hatGuid
    {
        get
        {
            return _hatAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _hatAsset = Assets.find(value) as ItemHatAsset;
            hatDirty = true;
        }
    }

    public ushort hat
    {
        get
        {
            return _hatAsset?.id ?? 0;
        }
        set
        {
            _hatAsset = Assets.find(EAssetType.ITEM, value) as ItemHatAsset;
            hatDirty = true;
        }
    }

    public Guid backpackGuid
    {
        get
        {
            return _backpackAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _backpackAsset = Assets.find(value) as ItemBackpackAsset;
            backpackDirty = true;
        }
    }

    public ushort backpack
    {
        get
        {
            return _backpackAsset?.id ?? 0;
        }
        set
        {
            _backpackAsset = Assets.find(EAssetType.ITEM, value) as ItemBackpackAsset;
            backpackDirty = true;
        }
    }

    public Guid vestGuid
    {
        get
        {
            return _vestAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _vestAsset = Assets.find(value) as ItemVestAsset;
            vestDirty = true;
        }
    }

    public ushort vest
    {
        get
        {
            return _vestAsset?.id ?? 0;
        }
        set
        {
            _vestAsset = Assets.find(EAssetType.ITEM, value) as ItemVestAsset;
            vestDirty = true;
        }
    }

    public Guid maskGuid
    {
        get
        {
            return _maskAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _maskAsset = Assets.find(value) as ItemMaskAsset;
            maskDirty = true;
        }
    }

    public ushort mask
    {
        get
        {
            return _maskAsset?.id ?? 0;
        }
        set
        {
            _maskAsset = Assets.find(EAssetType.ITEM, value) as ItemMaskAsset;
            maskDirty = true;
        }
    }

    public Guid glassesGuid
    {
        get
        {
            return _glassesAsset?.GUID ?? Guid.Empty;
        }
        set
        {
            _glassesAsset = Assets.find(value) as ItemGlassesAsset;
            glassesDirty = true;
        }
    }

    public ushort glasses
    {
        get
        {
            return _glassesAsset?.id ?? 0;
        }
        set
        {
            _glassesAsset = Assets.find(EAssetType.ITEM, value) as ItemGlassesAsset;
            glassesDirty = true;
        }
    }

    public byte face
    {
        get
        {
            return _face;
        }
        set
        {
            if (face != value)
            {
                _face = value;
                faceDirty = true;
            }
        }
    }

    public byte hair
    {
        get
        {
            return _hair;
        }
        set
        {
            if (hair != value)
            {
                _hair = value;
                hairDirty = true;
            }
        }
    }

    public byte beard
    {
        get
        {
            return _beard;
        }
        set
        {
            if (beard != value)
            {
                _beard = value;
                beardDirty = true;
            }
        }
    }

    public Color skin
    {
        get
        {
            return _skinColor;
        }
        set
        {
            _skinColor = value;
            skinColorDirty = true;
        }
    }

    public Color color
    {
        get
        {
            return _hairColor;
        }
        set
        {
            _hairColor = value;
        }
    }

    private void markAllDirty(bool isDirty)
    {
        hairDirty = isDirty;
        beardDirty = isDirty;
        skinColorDirty = isDirty;
        faceDirty = isDirty;
        shirtDirty = isDirty;
        pantsDirty = isDirty;
        hatDirty = isDirty;
        backpackDirty = isDirty;
        vestDirty = isDirty;
        maskDirty = isDirty;
        glassesDirty = isDirty;
    }

    private void ApplyHairOverride(ItemGearAsset itemAsset, Transform rootModel)
    {
        if (string.IsNullOrEmpty(itemAsset.hairOverride))
        {
            return;
        }
        Transform transform = rootModel.FindChildRecursive(itemAsset.hairOverride);
        if (transform == null)
        {
            Assets.ReportError(itemAsset, "cannot find hair override \"{0}\"", itemAsset.hairOverride);
            return;
        }
        Renderer component = transform.GetComponent<Renderer>();
        if (component != null)
        {
            component.sharedMaterial = materialHair;
        }
        else
        {
            Assets.ReportError(itemAsset, "hair override \"{0}\" does not have a renderer component", itemAsset.hairOverride);
        }
    }

    private void ApplySkinOverride(ItemClothingAsset itemAsset, Transform rootModel)
    {
        if (string.IsNullOrEmpty(itemAsset.skinOverride))
        {
            return;
        }
        Transform transform = rootModel.FindChildRecursive(itemAsset.skinOverride);
        if (transform == null)
        {
            Assets.ReportError(itemAsset, "cannot find skin override \"{0}\"", itemAsset.skinOverride);
            return;
        }
        Renderer component = transform.GetComponent<Renderer>();
        if (component != null)
        {
            component.sharedMaterial = materialClothing;
        }
        else
        {
            Assets.ReportError(itemAsset, "skin override \"{0}\" does not have a renderer component", itemAsset.skinOverride);
        }
    }

    public void apply()
    {
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        if (_shirtAsset != null && _shirtAsset.isPro && !canWearPro)
        {
            _shirtAsset = null;
            shirtDirty = true;
        }
        if (_pantsAsset != null && _pantsAsset.isPro && !canWearPro)
        {
            _pantsAsset = null;
            pantsDirty = true;
        }
        if (_hatAsset != null && _hatAsset.isPro && !canWearPro)
        {
            _hatAsset = null;
            hatDirty = true;
        }
        if (_backpackAsset != null && _backpackAsset.isPro && !canWearPro)
        {
            _backpackAsset = null;
            backpackDirty = true;
        }
        if (_vestAsset != null && _vestAsset.isPro && !canWearPro)
        {
            _vestAsset = null;
            vestDirty = true;
        }
        if (_maskAsset != null && _maskAsset.isPro && !canWearPro)
        {
            _maskAsset = null;
            maskDirty = true;
        }
        if (_glassesAsset != null && _glassesAsset.isPro && !canWearPro)
        {
            _glassesAsset = null;
            glassesDirty = true;
        }
        ItemShirtAsset itemShirtAsset = ((visualShirtAsset != null && isVisual && (shirtAsset == null || !shirtAsset.TakesPriorityOverCosmetic)) ? visualShirtAsset : shirtAsset);
        ItemPantsAsset itemPantsAsset = ((visualPantsAsset != null && isVisual && (pantsAsset == null || !pantsAsset.TakesPriorityOverCosmetic)) ? visualPantsAsset : pantsAsset);
        if (skinColorDirty)
        {
            materialClothing.SetColor(skinColorPropertyID, _skinColor);
        }
        if (faceDirty)
        {
            materialClothing.SetTexture(faceAlbedoTexturePropertyID, Resources.Load<Texture2D>("Faces/" + face + "/Texture"));
            materialClothing.SetTexture(faceEmissionTexturePropertyID, Resources.Load<Texture2D>("Faces/" + face + "/Emission"));
        }
        if (shirtDirty)
        {
            bool flag = true;
            bool flag2 = true;
            if (itemShirtAsset != null && itemShirtAsset.shouldBeVisible(isRagdoll))
            {
                materialClothing.SetTexture(shirtAlbedoTexturePropertyID, itemShirtAsset.shirt);
                materialClothing.SetTexture(shirtEmissionTexturePropertyID, itemShirtAsset.emission);
                materialClothing.SetTexture(shirtMetallicTexturePropertyID, itemShirtAsset.metallic);
                materialClothing.SetFloat(flipShirtPropertyID, (_isLeftHanded && itemShirtAsset.ignoreHand) ? 1f : 0f);
                Mesh[] array = (isMine ? itemShirtAsset.characterMeshOverride1pLODs : itemShirtAsset.characterMeshOverride3pLODs);
                if (array != null)
                {
                    flag = false;
                    setCharacterMeshes(array);
                }
                if (itemShirtAsset.characterMaterialOverride != null)
                {
                    flag2 = false;
                    setCharacterMaterial(itemShirtAsset.characterMaterialOverride);
                }
            }
            else
            {
                materialClothing.SetTexture(shirtAlbedoTexturePropertyID, null);
                materialClothing.SetTexture(shirtEmissionTexturePropertyID, null);
                materialClothing.SetTexture(shirtMetallicTexturePropertyID, null);
            }
            if (flag != usingHumanMeshes)
            {
                usingHumanMeshes = flag;
                if (usingHumanMeshes)
                {
                    setCharacterMeshes(humanMeshes);
                }
            }
            if (flag2 != usingHumanMaterials)
            {
                usingHumanMaterials = flag2;
                if (usingHumanMaterials)
                {
                    setCharacterMaterial(materialClothing);
                }
            }
        }
        if (pantsDirty)
        {
            if (itemPantsAsset != null && itemPantsAsset.shouldBeVisible(isRagdoll))
            {
                materialClothing.SetTexture(pantsAlbedoTexturePropertyID, itemPantsAsset.pants);
                materialClothing.SetTexture(pantsEmissionTexturePropertyID, itemPantsAsset.emission);
                materialClothing.SetTexture(pantsMetallicTexturePropertyID, itemPantsAsset.metallic);
            }
            else
            {
                materialClothing.SetTexture(pantsAlbedoTexturePropertyID, null);
                materialClothing.SetTexture(pantsEmissionTexturePropertyID, null);
                materialClothing.SetTexture(pantsMetallicTexturePropertyID, null);
            }
        }
        if (!isMine)
        {
            bool flag3 = true;
            bool flag4 = true;
            if (shirtDirty)
            {
                if (isUpper && upperSystems != null)
                {
                    for (int i = 0; i < upperSystems.Length; i++)
                    {
                        MythicalEffectController mythicalEffectController = upperSystems[i];
                        if (mythicalEffectController != null)
                        {
                            UnityEngine.Object.Destroy(mythicalEffectController);
                        }
                    }
                    isUpper = false;
                }
                if (isVisual && isMythic && visualShirt != 0)
                {
                    ushort inventoryMythicID = Provider.provider.economyService.getInventoryMythicID(visualShirt);
                    if (inventoryMythicID != 0)
                    {
                        ItemTool.ApplyMythicalEffectToMultipleTransforms(upperBones, upperSystems, inventoryMythicID, EEffectType.AREA);
                        isUpper = true;
                    }
                }
            }
            if (itemShirtAsset != null)
            {
                flag3 &= itemShirtAsset.hairVisible;
                flag4 &= itemShirtAsset.beardVisible;
            }
            if (pantsDirty)
            {
                if (isLower && lowerSystems != null)
                {
                    for (int j = 0; j < lowerSystems.Length; j++)
                    {
                        MythicalEffectController mythicalEffectController2 = lowerSystems[j];
                        if (mythicalEffectController2 != null)
                        {
                            UnityEngine.Object.Destroy(mythicalEffectController2);
                        }
                    }
                    isLower = false;
                }
                if (isVisual && isMythic && visualPants != 0)
                {
                    ushort inventoryMythicID2 = Provider.provider.economyService.getInventoryMythicID(visualPants);
                    if (inventoryMythicID2 != 0)
                    {
                        ItemTool.ApplyMythicalEffectToMultipleTransforms(lowerBones, lowerSystems, inventoryMythicID2, EEffectType.AREA);
                        isLower = true;
                    }
                }
            }
            if (itemPantsAsset != null)
            {
                flag3 &= itemPantsAsset.hairVisible;
                flag4 &= itemPantsAsset.beardVisible;
            }
            ItemHatAsset itemHatAsset = ((visualHatAsset != null && isVisual && (hatAsset == null || !hatAsset.TakesPriorityOverCosmetic)) ? visualHatAsset : hatAsset);
            ItemBackpackAsset itemBackpackAsset = ((visualBackpackAsset != null && isVisual && (backpackAsset == null || !backpackAsset.TakesPriorityOverCosmetic)) ? visualBackpackAsset : backpackAsset);
            ItemVestAsset itemVestAsset = ((visualVestAsset != null && isVisual && (vestAsset == null || !vestAsset.TakesPriorityOverCosmetic)) ? visualVestAsset : vestAsset);
            ItemMaskAsset itemMaskAsset = ((visualMaskAsset != null && isVisual && (maskAsset == null || !maskAsset.TakesPriorityOverCosmetic)) ? visualMaskAsset : maskAsset);
            ItemGlassesAsset itemGlassesAsset = ((visualGlassesAsset != null && isVisual && (glassesAsset == null || !glassesAsset.TakesPriorityOverCosmetic)) ? visualGlassesAsset : glassesAsset);
            if (hatDirty)
            {
                if (hatModel != null)
                {
                    UnityEngine.Object.Destroy(hatModel.gameObject);
                }
                if (itemHatAsset != null && itemHatAsset.hat != null && itemHatAsset.shouldBeVisible(isRagdoll))
                {
                    hatModel = UnityEngine.Object.Instantiate(itemHatAsset.hat).transform;
                    hatModel.name = "Hat";
                    hatModel.transform.parent = skull;
                    hatModel.transform.localPosition = Vector3.zero;
                    hatModel.transform.localRotation = Quaternion.identity;
                    hatModel.transform.localScale = new Vector3(1f, (_isLeftHanded && itemHatAsset.shouldMirrorLeftHandedModel) ? (-1f) : 1f, 1f);
                    if (!isView && itemHatAsset.shouldDestroyClothingColliders)
                    {
                        PrefabUtil.DestroyCollidersInChildren(hatModel.gameObject, includeInactive: true);
                    }
                    hatModel.DestroyRigidbody();
                    if (isVisual && isMythic && visualHat != 0)
                    {
                        ushort inventoryMythicID3 = Provider.provider.economyService.getInventoryMythicID(visualHat);
                        if (inventoryMythicID3 != 0)
                        {
                            centerHeadEffect(skull, hatModel);
                            ItemTool.ApplyMythicalEffect(hatModel, inventoryMythicID3, EEffectType.HOOK);
                        }
                    }
                    ApplyHairOverride(itemHatAsset, hatModel);
                    ApplySkinOverride(itemHatAsset, hatModel);
                }
            }
            if (itemHatAsset != null && itemHatAsset.hat != null)
            {
                flag3 &= itemHatAsset.hairVisible;
                flag4 &= itemHatAsset.beardVisible;
            }
            if (backpackDirty)
            {
                if (backpackModel != null)
                {
                    UnityEngine.Object.Destroy(backpackModel.gameObject);
                }
                if (itemBackpackAsset != null && itemBackpackAsset.backpack != null && itemBackpackAsset.shouldBeVisible(isRagdoll))
                {
                    backpackModel = UnityEngine.Object.Instantiate(itemBackpackAsset.backpack).transform;
                    backpackModel.name = "Backpack";
                    backpackModel.transform.parent = spine;
                    backpackModel.transform.localPosition = Vector3.zero;
                    backpackModel.transform.localRotation = Quaternion.identity;
                    backpackModel.transform.localScale = new Vector3(1f, (_isLeftHanded && itemBackpackAsset.shouldMirrorLeftHandedModel) ? (-1f) : 1f, 1f);
                    if (!isView && itemBackpackAsset.shouldDestroyClothingColliders)
                    {
                        PrefabUtil.DestroyCollidersInChildren(backpackModel.gameObject, includeInactive: true);
                    }
                    backpackModel.DestroyRigidbody();
                    if (isVisual && isMythic && visualBackpack != 0)
                    {
                        ushort inventoryMythicID4 = Provider.provider.economyService.getInventoryMythicID(visualBackpack);
                        if (inventoryMythicID4 != 0)
                        {
                            ItemTool.ApplyMythicalEffect(backpackModel, inventoryMythicID4, EEffectType.HOOK);
                        }
                    }
                    backpackModel.gameObject.SetActive(hasBackpack);
                    ApplySkinOverride(itemBackpackAsset, backpackModel);
                }
            }
            if (itemBackpackAsset != null)
            {
                flag3 &= itemBackpackAsset.hairVisible;
                flag4 &= itemBackpackAsset.beardVisible;
            }
            if (vestDirty)
            {
                if (vestModel != null)
                {
                    UnityEngine.Object.Destroy(vestModel.gameObject);
                }
                if (itemVestAsset != null && itemVestAsset.vest != null && itemVestAsset.shouldBeVisible(isRagdoll))
                {
                    vestModel = UnityEngine.Object.Instantiate(itemVestAsset.vest).transform;
                    vestModel.name = "Vest";
                    vestModel.transform.parent = spine;
                    vestModel.transform.localPosition = Vector3.zero;
                    vestModel.transform.localRotation = Quaternion.identity;
                    vestModel.transform.localScale = new Vector3(1f, (_isLeftHanded && itemVestAsset.shouldMirrorLeftHandedModel) ? (-1f) : 1f, 1f);
                    if (!isView && itemVestAsset.shouldDestroyClothingColliders)
                    {
                        PrefabUtil.DestroyCollidersInChildren(vestModel.gameObject, includeInactive: true);
                    }
                    vestModel.DestroyRigidbody();
                    if (isVisual && isMythic && visualVest != 0)
                    {
                        ushort inventoryMythicID5 = Provider.provider.economyService.getInventoryMythicID(visualVest);
                        if (inventoryMythicID5 != 0)
                        {
                            ItemTool.ApplyMythicalEffect(vestModel, inventoryMythicID5, EEffectType.HOOK);
                        }
                    }
                    ApplySkinOverride(itemVestAsset, vestModel);
                }
            }
            if (itemVestAsset != null)
            {
                flag3 &= itemVestAsset.hairVisible;
                flag4 &= itemVestAsset.beardVisible;
            }
            if (maskDirty)
            {
                if (maskModel != null)
                {
                    UnityEngine.Object.Destroy(maskModel.gameObject);
                }
                if (itemMaskAsset != null && itemMaskAsset.mask != null && itemMaskAsset.shouldBeVisible(isRagdoll))
                {
                    maskModel = UnityEngine.Object.Instantiate(itemMaskAsset.mask).transform;
                    maskModel.name = "Mask";
                    maskModel.transform.parent = skull;
                    maskModel.transform.localPosition = Vector3.zero;
                    maskModel.transform.localRotation = Quaternion.identity;
                    maskModel.transform.localScale = new Vector3(1f, (_isLeftHanded && itemMaskAsset.shouldMirrorLeftHandedModel) ? (-1f) : 1f, 1f);
                    if (!isView && itemMaskAsset.shouldDestroyClothingColliders)
                    {
                        PrefabUtil.DestroyCollidersInChildren(maskModel.gameObject, includeInactive: true);
                    }
                    maskModel.DestroyRigidbody();
                    ushort num = 0;
                    if (isVisual && isMythic && visualMask != 0)
                    {
                        num = Provider.provider.economyService.getInventoryMythicID(visualMask);
                    }
                    if (num != 0)
                    {
                        centerHeadEffect(skull, maskModel);
                        ItemTool.ApplyMythicalEffect(maskModel, num, EEffectType.HOOK);
                    }
                    ApplyHairOverride(itemMaskAsset, maskModel);
                    ApplySkinOverride(itemMaskAsset, maskModel);
                }
            }
            if (itemMaskAsset != null && itemMaskAsset.mask != null)
            {
                flag3 &= itemMaskAsset.hairVisible;
                flag4 &= itemMaskAsset.beardVisible;
            }
            if (glassesDirty)
            {
                if (glassesModel != null)
                {
                    UnityEngine.Object.Destroy(glassesModel.gameObject);
                }
                if (itemGlassesAsset != null && itemGlassesAsset.glasses != null && itemGlassesAsset.shouldBeVisible(isRagdoll))
                {
                    glassesModel = UnityEngine.Object.Instantiate(itemGlassesAsset.glasses).transform;
                    glassesModel.name = "Glasses";
                    glassesModel.transform.parent = skull;
                    glassesModel.transform.localPosition = Vector3.zero;
                    glassesModel.transform.localRotation = Quaternion.identity;
                    glassesModel.localScale = new Vector3(1f, (_isLeftHanded && itemGlassesAsset.shouldMirrorLeftHandedModel) ? (-1f) : 1f, 1f);
                    if (!isView && itemGlassesAsset.shouldDestroyClothingColliders)
                    {
                        PrefabUtil.DestroyCollidersInChildren(glassesModel.gameObject, includeInactive: true);
                    }
                    glassesModel.DestroyRigidbody();
                    if (isVisual && isMythic && visualGlasses != 0)
                    {
                        ushort inventoryMythicID6 = Provider.provider.economyService.getInventoryMythicID(visualGlasses);
                        if (inventoryMythicID6 != 0)
                        {
                            centerHeadEffect(skull, glassesModel);
                            ItemTool.ApplyMythicalEffect(glassesModel, inventoryMythicID6, EEffectType.HOOK);
                        }
                    }
                    ApplyHairOverride(itemGlassesAsset, glassesModel);
                    ApplySkinOverride(itemGlassesAsset, glassesModel);
                }
            }
            if (itemGlassesAsset != null && itemGlassesAsset.glasses != null)
            {
                flag3 &= itemGlassesAsset.hairVisible;
                flag4 &= itemGlassesAsset.beardVisible;
            }
            if (materialHair != null)
            {
                materialHair.color = color;
            }
            if (hasHair != flag3)
            {
                hasHair = flag3;
                hairDirty = true;
            }
            if (hairDirty)
            {
                if (hairModel != null)
                {
                    UnityEngine.Object.Destroy(hairModel.gameObject);
                }
                if (hasHair)
                {
                    UnityEngine.Object @object = Resources.Load("Hairs/" + hair + "/Hair");
                    if (@object != null)
                    {
                        hairModel = ((GameObject)UnityEngine.Object.Instantiate(@object)).transform;
                        hairModel.name = "Hair";
                        hairModel.transform.parent = skull;
                        hairModel.transform.localPosition = Vector3.zero;
                        hairModel.transform.localRotation = Quaternion.identity;
                        hairModel.transform.localScale = Vector3.one;
                        if (hairModel.Find("Model_0") != null)
                        {
                            hairModel.Find("Model_0").GetComponent<Renderer>().sharedMaterial = materialHair;
                        }
                        hairModel.DestroyRigidbody();
                    }
                }
            }
            if (hasBeard != flag4)
            {
                hasBeard = flag4;
                beardDirty = true;
            }
            if (beardDirty)
            {
                if (beardModel != null)
                {
                    UnityEngine.Object.Destroy(beardModel.gameObject);
                }
                if (hasBeard)
                {
                    UnityEngine.Object object2 = Resources.Load("Beards/" + beard + "/Beard");
                    if (object2 != null)
                    {
                        beardModel = ((GameObject)UnityEngine.Object.Instantiate(object2)).transform;
                        beardModel.name = "Beard";
                        beardModel.transform.parent = skull;
                        beardModel.transform.localPosition = Vector3.zero;
                        beardModel.transform.localRotation = Quaternion.identity;
                        beardModel.localScale = Vector3.one;
                        if (beardModel.Find("Model_0") != null)
                        {
                            beardModel.Find("Model_0").GetComponent<Renderer>().sharedMaterial = materialHair;
                        }
                        beardModel.DestroyRigidbody();
                    }
                }
            }
        }
        markAllDirty(isDirty: false);
    }

    /// <summary>
    /// Center mythical effect hook horizontally, but maintain vertical placement.
    /// Lots of hats/masks/glasses have off-center effects intentionally, but community
    /// feedback suggests centering to make effects like circling atoms look better.
    /// </summary>
    private void centerHeadEffect(Transform skull, Transform model)
    {
        Transform transform = model.Find("Effect");
        if (transform == null)
        {
            transform = new GameObject("Effect").transform;
            transform.parent = model;
            transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            transform.localScale = Vector3.one;
        }
        else
        {
            Vector3 localPosition = transform.localPosition;
            localPosition.y = 0f;
            localPosition.z = 0f;
            transform.localPosition = localPosition;
        }
    }

    /// <summary>
    /// Set mesh of all character mesh renderers.
    /// Tries to match renderer index to mesh LOD index.
    /// </summary>
    private void setCharacterMeshes(Mesh[] meshes)
    {
        SkinnedMeshRenderer[] array;
        if (meshes == null || meshes.Length < 1)
        {
            array = characterMeshRenderers;
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in array)
            {
                if (!(skinnedMeshRenderer == null))
                {
                    skinnedMeshRenderer.sharedMesh = null;
                }
            }
            return;
        }
        int num = 0;
        array = characterMeshRenderers;
        foreach (SkinnedMeshRenderer skinnedMeshRenderer2 in array)
        {
            if (!(skinnedMeshRenderer2 == null))
            {
                if (num < meshes.Length)
                {
                    skinnedMeshRenderer2.sharedMesh = meshes[num];
                }
                else
                {
                    skinnedMeshRenderer2.sharedMesh = meshes[^1];
                }
                num++;
            }
        }
    }

    /// <summary>
    /// Set material of all character mesh renderers.
    /// </summary>
    private void setCharacterMaterial(Material material)
    {
        SkinnedMeshRenderer[] array = characterMeshRenderers;
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in array)
        {
            if (!(skinnedMeshRenderer == null))
            {
                skinnedMeshRenderer.sharedMaterial = material;
            }
        }
    }

    private void Awake()
    {
        spine = base.transform.Find("Skeleton").Find("Spine");
        skull = spine.Find("Skull");
        upperBones = new Transform[5]
        {
            spine,
            spine.Find("Left_Shoulder/Left_Arm"),
            spine.Find("Left_Shoulder/Left_Arm/Left_Hand"),
            spine.Find("Right_Shoulder/Right_Arm"),
            spine.Find("Right_Shoulder/Right_Arm/Right_Hand")
        };
        upperSystems = new MythicalEffectController[upperBones.Length];
        lowerBones = new Transform[4]
        {
            spine.parent.Find("Left_Hip/Left_Leg"),
            spine.parent.Find("Left_Hip/Left_Leg/Left_Foot"),
            spine.parent.Find("Right_Hip/Right_Leg"),
            spine.parent.Find("Right_Hip/Right_Leg/Right_Foot")
        };
        lowerSystems = new MythicalEffectController[lowerBones.Length];
        Transform obj = base.transform.Find("Model_0");
        Transform transform = base.transform.Find("Model_1");
        characterMeshRenderers = new SkinnedMeshRenderer[(transform == null) ? 1 : 2];
        if (obj != null)
        {
            characterMeshRenderers[0] = base.transform.Find("Model_0").GetComponent<SkinnedMeshRenderer>();
        }
        if (transform != null)
        {
            characterMeshRenderers[1] = base.transform.Find("Model_1").GetComponent<SkinnedMeshRenderer>();
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (clothingShader == null)
            {
                clothingShader = Shader.Find("Standard/Clothes");
            }
            humanMeshes = new Mesh[characterMeshRenderers.Length];
            for (int i = 0; i < humanMeshes.Length; i++)
            {
                if (characterMeshRenderers[i] != null)
                {
                    humanMeshes[i] = characterMeshRenderers[i].sharedMesh;
                }
            }
            materialClothing = new Material(clothingShader);
            materialClothing.hideFlags = HideFlags.HideAndDontSave;
            materialHair = new Material(shader);
            materialHair.name = "Hair";
            materialHair.hideFlags = HideFlags.HideAndDontSave;
            materialHair.SetFloat("_Glossiness", 0f);
        }
        setCharacterMaterial(materialClothing);
        markAllDirty(isDirty: true);
    }

    private void OnDestroy()
    {
        if (materialClothing != null)
        {
            UnityEngine.Object.DestroyImmediate(materialClothing);
            materialClothing = null;
        }
        if (materialHair != null)
        {
            UnityEngine.Object.DestroyImmediate(materialHair);
            materialHair = null;
        }
    }
}
