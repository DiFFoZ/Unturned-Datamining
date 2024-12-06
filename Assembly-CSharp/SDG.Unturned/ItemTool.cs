using System;
using System.Collections.Generic;
using SDG.Provider;
using UnityEngine;
using UnityEngine.Rendering;

namespace SDG.Unturned;

public class ItemTool : MonoBehaviour
{
    private static readonly Color RARITY_COMMON_HIGHLIGHT = Color.white;

    private static readonly Color RARITY_COMMON_UI = Color.white;

    private static readonly Color RARITY_UNCOMMON_HIGHLIGHT = Color.green;

    private static readonly Color RARITY_UNCOMMON_UI = new Color(0.12156863f, 0.5294118f, 0.12156863f);

    private static readonly Color RARITY_RARE_HIGHLIGHT = Color.blue;

    private static readonly Color RARITY_RARE_UI = new Color(0.29411766f, 20f / 51f, 50f / 51f);

    private static readonly Color RARITY_EPIC_HIGHLIGHT = new Color(0.33f, 0f, 1f);

    private static readonly Color RARITY_EPIC_UI = new Color(0.5882353f, 0.29411766f, 50f / 51f);

    private static readonly Color RARITY_LEGENDARY_HIGHLIGHT = Color.magenta;

    private static readonly Color RARITY_LEGENDARY_UI = new Color(40f / 51f, 10f / 51f, 50f / 51f);

    private static readonly Color RARITY_MYTHICAL_HIGHLIGHT = Color.red;

    private static readonly Color RARITY_MYTHICAL_UI = new Color(50f / 51f, 10f / 51f, 5f / 51f);

    private static Queue<ItemIconInfo> icons;

    private string currentIconTags;

    private string currentIconDynamicProps;

    private List<Renderer> renderers = new List<Renderer>();

    private Camera cameraComponent;

    private Light lightComponent;

    private Transform pendingItem;

    private ItemIconInfo pendingInfo;

    private static ItemTool tool;

    private static Dictionary<ItemAsset, Texture2D> iconCache = new Dictionary<ItemAsset, Texture2D>();

    public static string filterRarityRichText(string desc)
    {
        if (!string.IsNullOrEmpty(desc))
        {
            desc = desc.Replace("color=common", "color=#ffffff");
            desc = desc.Replace("color=gold", "color=#d2bf22");
            desc = desc.Replace("color=uncommon", "color=#1f871f");
            desc = desc.Replace("color=rare", "color=#4b64fa");
            desc = desc.Replace("color=epic", "color=#964bfa");
            desc = desc.Replace("color=legendary", "color=#c832fa");
            desc = desc.Replace("color=mythical", "color=#fa3219");
            desc = desc.Replace("color=red", "color=#bf1f1f");
            desc = desc.Replace("color=green", "color=#1f871f");
            desc = desc.Replace("color=blue", "color=#3298c8");
            desc = desc.Replace("color=orange", "color=#ab8019");
            desc = desc.Replace("color=yellow", "color=#dcb413");
            desc = desc.Replace("color=purple", "color=#6a466d");
        }
        return desc;
    }

    public static Color getRarityColorHighlight(EItemRarity rarity)
    {
        return rarity switch
        {
            EItemRarity.COMMON => RARITY_COMMON_HIGHLIGHT, 
            EItemRarity.UNCOMMON => RARITY_UNCOMMON_HIGHLIGHT, 
            EItemRarity.RARE => RARITY_RARE_HIGHLIGHT, 
            EItemRarity.EPIC => RARITY_EPIC_HIGHLIGHT, 
            EItemRarity.LEGENDARY => RARITY_LEGENDARY_HIGHLIGHT, 
            EItemRarity.MYTHICAL => RARITY_MYTHICAL_HIGHLIGHT, 
            _ => Color.white, 
        };
    }

    public static Color getRarityColorUI(EItemRarity rarity)
    {
        return rarity switch
        {
            EItemRarity.COMMON => RARITY_COMMON_UI, 
            EItemRarity.UNCOMMON => RARITY_UNCOMMON_UI, 
            EItemRarity.RARE => RARITY_RARE_UI, 
            EItemRarity.EPIC => RARITY_EPIC_UI, 
            EItemRarity.LEGENDARY => RARITY_LEGENDARY_UI, 
            EItemRarity.MYTHICAL => RARITY_MYTHICAL_UI, 
            _ => Color.white, 
        };
    }

    public static Color getQualityColor(float quality)
    {
        if (quality < 0.5f)
        {
            return Color.Lerp(Palette.COLOR_R, Palette.COLOR_Y, quality * 2f);
        }
        return Color.Lerp(Palette.COLOR_Y, Palette.COLOR_G, (quality - 0.5f) * 2f);
    }

    public static void ApplyMythicalEffectToMultipleTransforms(Transform[] bones, MythicalEffectController[] systems, ushort mythicID, EEffectType type)
    {
        if (bones == null || systems == null)
        {
            return;
        }
        if (mythicID == 0)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                systems[i] = null;
            }
        }
        else if (!(Assets.find(EAssetType.MYTHIC, mythicID) is MythicAsset mythicAsset))
        {
            for (int j = 0; j < bones.Length; j++)
            {
                systems[j] = null;
            }
        }
        else
        {
            for (int k = 0; k < bones.Length; k++)
            {
                systems[k] = ApplyMythicalEffect(bones[k], mythicAsset, type);
            }
        }
    }

    public static MythicalEffectController ApplyMythicalEffect(Transform parent, ushort mythicID, EEffectType type)
    {
        if (mythicID == 0)
        {
            return null;
        }
        if (parent == null)
        {
            return null;
        }
        if (!(Assets.find(EAssetType.MYTHIC, mythicID) is MythicAsset mythicAsset))
        {
            return null;
        }
        return ApplyMythicalEffect(parent, mythicAsset, type);
    }

    private static MythicalEffectController ApplyMythicalEffect(Transform parent, MythicAsset mythicAsset, EEffectType type)
    {
        if (mythicAsset == null)
        {
            return null;
        }
        if (parent == null)
        {
            return null;
        }
        GameObject gameObject;
        switch (type)
        {
        case EEffectType.AREA:
            gameObject = mythicAsset.systemArea;
            break;
        case EEffectType.HOOK:
            gameObject = mythicAsset.systemHook;
            break;
        case EEffectType.FIRST:
            gameObject = mythicAsset.systemFirst;
            break;
        case EEffectType.THIRD:
            gameObject = mythicAsset.systemThird;
            break;
        default:
            return null;
        }
        if (gameObject == null)
        {
            return null;
        }
        Transform transform = parent.Find("Effect");
        MythicalEffectController mythicalEffectController = ((transform != null) ? transform : parent).gameObject.AddComponent<MythicalEffectController>();
        mythicalEffectController.systemPrefab = gameObject;
        return mythicalEffectController;
    }

    public static bool tryForceGiveItem(Player player, ushort id, byte amount)
    {
        if (!(Assets.find(EAssetType.ITEM, id) is ItemAsset { isPro: false }))
        {
            return false;
        }
        for (int i = 0; i < amount; i++)
        {
            Item item = new Item(id, EItemOrigin.ADMIN);
            player.inventory.forceAddItem(item, auto: true);
        }
        return true;
    }

    public static bool checkUseable(byte page, ushort id)
    {
        if (!(Assets.find(EAssetType.ITEM, id) is ItemAsset itemAsset))
        {
            return false;
        }
        if (!itemAsset.canPlayerEquip)
        {
            return false;
        }
        return itemAsset.slot.canEquipInPage(page);
    }

    /// <summary>
    /// No longer used in vanilla. Kept in case plugins are using it.
    /// </summary>
    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, GetStatTrackerValueHandler statTrackerCallback)
    {
        ItemAsset itemAsset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
        return getItem(id, skin, quality, state, viewmodel, itemAsset, statTrackerCallback);
    }

    /// <summary>
    /// No longer used in vanilla. Kept in case plugins are using it.
    /// </summary>
    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback)
    {
        SkinAsset skinAsset = Assets.find(EAssetType.SKIN, skin) as SkinAsset;
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders: false, outTempMeshes, out tempMaterial, statTrackerCallback);
    }

    /// <summary>
    /// No longer used in vanilla. Kept in case plugins are using it.
    /// </summary>
    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, bool shouldDestroyColliders, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback)
    {
        SkinAsset skinAsset = Assets.find(EAssetType.SKIN, skin) as SkinAsset;
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders, outTempMeshes, out tempMaterial, statTrackerCallback);
    }

    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, GetStatTrackerValueHandler statTrackerCallback)
    {
        SkinAsset skinAsset = Assets.find(EAssetType.SKIN, skin) as SkinAsset;
        Material tempMaterial;
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders: false, null, out tempMaterial, statTrackerCallback);
    }

    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, bool shouldDestroyColliders, GetStatTrackerValueHandler statTrackerCallback)
    {
        SkinAsset skinAsset = Assets.find(EAssetType.SKIN, skin) as SkinAsset;
        Material tempMaterial;
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders, null, out tempMaterial, statTrackerCallback);
    }

    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, SkinAsset skinAsset, GetStatTrackerValueHandler statTrackerCallback)
    {
        Material tempMaterial;
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders: false, null, out tempMaterial, statTrackerCallback);
    }

    public static Transform getItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, SkinAsset skinAsset, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback)
    {
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders: false, outTempMeshes, out tempMaterial, statTrackerCallback);
    }

    internal static Transform getItem(byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, SkinAsset skinAsset, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback, GameObject prefabOverride = null)
    {
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders: false, outTempMeshes, out tempMaterial, statTrackerCallback, prefabOverride);
    }

    [Obsolete("Removed id and skin parameters because itemAsset and skinAsset are required")]
    internal static Transform InstantiateItem(ushort id, ushort skin, byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, SkinAsset skinAsset, bool shouldDestroyColliders, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback, GameObject prefabOverride = null)
    {
        return InstantiateItem(quality, state, viewmodel, itemAsset, skinAsset, shouldDestroyColliders, outTempMeshes, out tempMaterial, statTrackerCallback, prefabOverride);
    }

    /// <summary>
    /// Actual internal implementation.
    /// </summary>
    internal static Transform InstantiateItem(byte quality, byte[] state, bool viewmodel, ItemAsset itemAsset, SkinAsset skinAsset, bool shouldDestroyColliders, List<Mesh> outTempMeshes, out Material tempMaterial, GetStatTrackerValueHandler statTrackerCallback, GameObject prefabOverride = null)
    {
        tempMaterial = null;
        GameObject gameObject = prefabOverride;
        if (itemAsset != null && gameObject == null)
        {
            gameObject = itemAsset.item;
        }
        if (gameObject == null)
        {
            Transform transform = new GameObject().transform;
            transform.name = itemAsset.instantiatedItemName;
            if (viewmodel)
            {
                transform.tag = "Viewmodel";
                transform.gameObject.layer = 11;
            }
            else
            {
                transform.tag = "Item";
                transform.gameObject.layer = 13;
            }
            return transform;
        }
        Transform transform2 = UnityEngine.Object.Instantiate(gameObject).transform;
        transform2.name = itemAsset.instantiatedItemName;
        if (shouldDestroyColliders && itemAsset.shouldDestroyItemColliders)
        {
            PrefabUtil.DestroyCollidersInChildren(transform2.gameObject, includeInactive: true);
        }
        if (viewmodel)
        {
            Layerer.viewmodel(transform2);
        }
        if (skinAsset != null)
        {
            if (skinAsset.overrideMeshes != null && skinAsset.overrideMeshes.Count > 0)
            {
                HighlighterTool.remesh(transform2, skinAsset.overrideMeshes, outTempMeshes);
            }
            else
            {
                outTempMeshes?.Clear();
            }
            if (skinAsset.primarySkin != null)
            {
                if (skinAsset.isPattern)
                {
                    Material material = UnityEngine.Object.Instantiate(skinAsset.primarySkin);
                    itemAsset.applySkinBaseTextures(material);
                    skinAsset.SetMaterialProperties(material);
                    HighlighterTool.rematerialize(transform2, material, out tempMaterial);
                    transform2.gameObject.AddComponent<DestroyMaterialOnDestroy>().instantiatedMaterial = material;
                }
                else
                {
                    HighlighterTool.rematerialize(transform2, skinAsset.primarySkin, out tempMaterial);
                }
            }
        }
        else
        {
            outTempMeshes?.Clear();
        }
        if (itemAsset.type == EItemType.GUN)
        {
            Attachments attachments = transform2.gameObject.AddComponent<Attachments>();
            attachments.isSkinned = true;
            attachments.shouldDestroyColliders = shouldDestroyColliders;
            attachments.updateGun((ItemGunAsset)itemAsset, skinAsset);
            attachments.updateAttachments(state, viewmodel);
        }
        if (!Dedicator.IsDedicatedServer && statTrackerCallback != null && statTrackerCallback(out var _, out var _))
        {
            StatTracker statTracker = transform2.gameObject.AddComponent<StatTracker>();
            statTracker.statTrackerCallback = statTrackerCallback;
            statTracker.updateStatTracker(viewmodel);
            if (statTracker.statTrackerHook != null && skinAsset != null && skinAsset.hasStatTrackerTransformOverride)
            {
                statTracker.statTrackerHook.SetLocalPositionAndRotation(skinAsset.statTrackerPosition, skinAsset.statTrackerRotation);
            }
        }
        return transform2;
    }

    public static Texture2D getCard(Transform item, Transform hook_0, Transform hook_1, int width, int height, float size, float range)
    {
        if (item == null)
        {
            return null;
        }
        item.position = new Vector3(-256f, -256f, 0f);
        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        temporary.name = "Card_Render";
        RenderTexture.active = temporary;
        tool.cameraComponent.targetTexture = temporary;
        tool.cameraComponent.orthographicSize = size;
        Texture2D texture2D = new Texture2D(width * 2, height, TextureFormat.ARGB32, mipChain: false, linear: false);
        texture2D.name = "Card_Atlas";
        texture2D.filterMode = FilterMode.Point;
        texture2D.wrapMode = TextureWrapMode.Clamp;
        bool fog = RenderSettings.fog;
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientSkyColor = RenderSettings.ambientSkyColor;
        Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
        Color ambientGroundColor = RenderSettings.ambientGroundColor;
        Texture customReflection = RenderSettings.customReflection;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.white;
        RenderSettings.ambientEquatorColor = Color.white;
        RenderSettings.ambientGroundColor = Color.white;
        RenderSettings.customReflection = null;
        if (Provider.isConnected)
        {
            LevelLighting.setEnabled(isEnabled: false);
        }
        tool.cameraComponent.cullingMask = 16384;
        tool.cameraComponent.farClipPlane = range;
        tool.transform.position = hook_0.position;
        tool.transform.rotation = hook_0.rotation;
        tool.cameraComponent.clearFlags = CameraClearFlags.Color;
        tool.cameraComponent.Render();
        texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        tool.transform.position = hook_1.position;
        tool.transform.rotation = hook_1.rotation;
        tool.cameraComponent.Render();
        texture2D.ReadPixels(new Rect(0f, 0f, width, height), width, 0);
        if (Provider.isConnected)
        {
            LevelLighting.setEnabled(isEnabled: true);
        }
        RenderSettings.fog = fog;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.customReflection = customReflection;
        item.position = new Vector3(0f, -256f, -256f);
        UnityEngine.Object.Destroy(item.gameObject);
        for (int i = 0; i < texture2D.width; i++)
        {
            for (int j = 0; j < texture2D.height; j++)
            {
                Color32 color = texture2D.GetPixel(i, j);
                if (color.r == byte.MaxValue && color.g == byte.MaxValue && color.b == byte.MaxValue)
                {
                    color.a = 0;
                }
                else
                {
                    color.a = byte.MaxValue;
                }
                texture2D.SetPixel(i, j, color);
            }
        }
        texture2D.Apply();
        RenderTexture.ReleaseTemporary(temporary);
        return texture2D;
    }

    public static void getIcon(ushort id, byte quality, byte[] state, ItemIconReady callback)
    {
        ItemAsset itemAsset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
        getIcon(id, quality, state, itemAsset, callback);
    }

    public static void getIcon(ushort id, byte quality, byte[] state, ItemAsset itemAsset, ItemIconReady callback)
    {
        getIcon(id, quality, state, itemAsset, itemAsset.size_x * 50, itemAsset.size_y * 50, callback);
    }

    public static void getIcon(ushort id, byte quality, byte[] state, ItemAsset itemAsset, int x, int y, ItemIconReady callback)
    {
        ushort num = 0;
        SkinAsset skinAsset = null;
        string tags = string.Empty;
        string dynamic_props = string.Empty;
        if (Player.player != null)
        {
            bool flag = itemAsset != null && itemAsset.sharedSkinLookupID != itemAsset.id;
            ushort itemID = (flag ? itemAsset.sharedSkinLookupID : id);
            if (Player.player.channel.owner.getItemSkinItemDefID(itemID, out var itemdefid) && itemdefid != 0)
            {
                if (!flag || itemAsset.SharedSkinShouldApplyVisuals)
                {
                    num = Provider.provider.economyService.getInventorySkinID(itemdefid);
                    skinAsset = Assets.find(EAssetType.SKIN, num) as SkinAsset;
                }
                Player.player.channel.owner.getTagsAndDynamicPropsForItem(itemdefid, out tags, out dynamic_props);
            }
        }
        getIcon(id, num, quality, state, itemAsset, skinAsset, tags, dynamic_props, x, y, scale: false, readableOnCPU: false, callback);
    }

    public static void getIcon(ushort id, ushort skin, byte quality, byte[] state, ItemAsset itemAsset, SkinAsset skinAsset, string tags, string dynamic_props, int x, int y, bool scale, bool readableOnCPU, ItemIconReady callback)
    {
        if (itemAsset == null)
        {
            itemAsset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
            if (itemAsset == null)
            {
                UnturnedLog.warn($"getIcon called with null item, unable to find by legacy id {id}");
                return;
            }
            UnturnedLog.warn($"getIcon called with null item, found \"{itemAsset.name}\" by legacy id {id}");
        }
        bool flag = state.Length == 0 && skinAsset == null && x == itemAsset.size_x * 50 && y == itemAsset.size_y * 50 && !readableOnCPU;
        if (flag)
        {
            if (iconCache.TryGetValue(itemAsset, out var value))
            {
                if (value != null)
                {
                    callback(value);
                    return;
                }
                iconCache.Remove(itemAsset);
            }
            foreach (ItemIconInfo icon in icons)
            {
                if (icon.isEligibleForCaching && icon.itemAsset == itemAsset)
                {
                    icon.callback = (ItemIconReady)Delegate.Combine(icon.callback, callback);
                    return;
                }
            }
        }
        ItemIconInfo itemIconInfo = new ItemIconInfo();
        itemIconInfo.id = itemAsset.id;
        itemIconInfo.skin = skinAsset?.id ?? 0;
        itemIconInfo.quality = quality;
        itemIconInfo.state = state;
        itemIconInfo.itemAsset = itemAsset;
        itemIconInfo.skinAsset = skinAsset;
        itemIconInfo.tags = tags;
        itemIconInfo.dynamic_props = dynamic_props;
        itemIconInfo.x = x;
        itemIconInfo.y = y;
        itemIconInfo.scale = scale;
        itemIconInfo.readableOnCPU = readableOnCPU;
        itemIconInfo.isEligibleForCaching = flag;
        itemIconInfo.callback = callback;
        icons.Enqueue(itemIconInfo);
    }

    public static Texture2D captureIcon(ushort id, ushort skin, Transform model, Transform icon, int width, int height, float orthoSize, bool readableOnCPU)
    {
        tool.transform.position = icon.position;
        tool.transform.rotation = icon.rotation;
        int antiAliasing = ((!GraphicsSettings.IsItemIconAntiAliasingEnabled) ? 1 : 4);
        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, antiAliasing);
        temporary.name = "Render_" + id + "_" + skin;
        RenderTexture.active = temporary;
        tool.cameraComponent.targetTexture = temporary;
        tool.cameraComponent.orthographicSize = orthoSize;
        bool fog = RenderSettings.fog;
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientSkyColor = RenderSettings.ambientSkyColor;
        Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
        Color ambientGroundColor = RenderSettings.ambientGroundColor;
        Texture customReflection = RenderSettings.customReflection;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.white;
        RenderSettings.ambientEquatorColor = Color.white;
        RenderSettings.ambientGroundColor = Color.white;
        RenderSettings.customReflection = null;
        if (Provider.isConnected)
        {
            LevelLighting.setEnabled(isEnabled: false);
        }
        tool.lightComponent.enabled = true;
        GL.Clear(clearDepth: true, clearColor: true, ColorEx.BlackZeroAlpha);
        tool.cameraComponent.cullingMask = 67313664;
        tool.cameraComponent.farClipPlane = 16f;
        tool.cameraComponent.clearFlags = CameraClearFlags.Nothing;
        tool.cameraComponent.Render();
        tool.lightComponent.enabled = false;
        if (Provider.isConnected)
        {
            LevelLighting.setEnabled(isEnabled: true);
        }
        RenderSettings.fog = fog;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.customReflection = customReflection;
        model.position = new Vector3(0f, -256f, -256f);
        UnityEngine.Object.Destroy(model.gameObject);
        Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
        texture2D.name = "Icon_" + id + "_" + skin;
        texture2D.filterMode = FilterMode.Point;
        texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture2D.Apply(updateMipmaps: false, !readableOnCPU);
        RenderTexture.ReleaseTemporary(temporary);
        return texture2D;
    }

    private bool getIconStatTrackerValue(out EStatTrackerType type, out int kills)
    {
        return new DynamicEconDetails(currentIconTags, currentIconDynamicProps).getStatTrackerValue(out type, out kills);
    }

    /// <summary>
    /// World to local bounds only works well for axis-aligned icons.
    /// </summary>
    private bool IsTransformAxisAligned(Transform cameraTransform)
    {
        Vector3 eulerAngles = cameraTransform.localRotation.eulerAngles;
        eulerAngles.x = Mathf.Abs(eulerAngles.x) % 90f;
        eulerAngles.y = Mathf.Abs(eulerAngles.y) % 90f;
        eulerAngles.z = Mathf.Abs(eulerAngles.z) % 90f;
        if ((eulerAngles.x < 1f || eulerAngles.x > 89f) && (eulerAngles.y < 1f || eulerAngles.y > 89f))
        {
            if (!(eulerAngles.z < 1f))
            {
                return eulerAngles.z > 89f;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Unity's Camera.orthographicSize is half the height of the viewing volume. Width is calculated from aspect ratio.
    /// </summary>
    private float CalculateOrthographicSize(ItemAsset assetContext, GameObject modelGameObject, Transform cameraTransform, int renderWidth, int renderHeight)
    {
        renderers.Clear();
        modelGameObject.GetComponentsInChildren(includeInactive: false, renderers);
        Bounds bounds = default(Bounds);
        bool flag = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                if (flag)
                {
                    bounds.Encapsulate(renderer.bounds);
                    continue;
                }
                flag = true;
                bounds = renderer.bounds;
            }
        }
        if (!flag)
        {
            return 1f;
        }
        Vector3 extents = bounds.extents;
        if (extents.ContainsInfinity() || extents.ContainsNaN() || extents.IsNearlyZero())
        {
            Assets.ReportError(assetContext, "has invalid icon world extent {0}", extents);
            return 1f;
        }
        Bounds bounds2 = new Bounds(cameraTransform.InverseTransformVector(extents), Vector3.zero);
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(-extents));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, extents.y, 0f - extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
        Vector3 extents2 = bounds2.extents;
        if (extents2.ContainsInfinity() || extents2.ContainsNaN() || extents2.IsNearlyZero())
        {
            Assets.ReportError(assetContext, "has invalid icon local extent {0} Maybe camera scale {1} is wrong?", extents2, cameraTransform.localScale);
            return 1f;
        }
        float num = Mathf.Abs(extents2.x);
        float num2 = Mathf.Abs(extents2.y);
        float num3 = Mathf.Abs(extents2.z);
        float nearClipPlane = cameraComponent.nearClipPlane;
        cameraTransform.position = bounds.center - cameraTransform.forward * (num3 + 0.02f + nearClipPlane);
        if (assetContext.iconCameraOrthographicSize > 0f && !IsTransformAxisAligned(cameraTransform))
        {
            return assetContext.iconCameraOrthographicSize;
        }
        num *= (float)(renderWidth + 16) / (float)renderWidth;
        num2 *= (float)(renderHeight + 16) / (float)renderHeight;
        float num4 = (float)renderWidth / (float)renderHeight;
        float num5 = num / num2;
        float num6 = ((num5 > num4) ? (num5 / num4) : 1f);
        return num2 * num6;
    }

    private void Update()
    {
        if (pendingItem == null)
        {
            if (icons != null && icons.Count != 0)
            {
                pendingInfo = icons.Dequeue();
                if (pendingInfo != null && pendingInfo.itemAsset != null)
                {
                    currentIconTags = pendingInfo.tags;
                    currentIconDynamicProps = pendingInfo.dynamic_props;
                    pendingItem = getItem(pendingInfo.itemAsset.id, pendingInfo.skinAsset?.id ?? 0, pendingInfo.quality, pendingInfo.state, viewmodel: false, pendingInfo.itemAsset, pendingInfo.skinAsset, getIconStatTrackerValue);
                    pendingItem.position = new Vector3(-256f, -256f, 0f);
                }
            }
            return;
        }
        ItemIconInfo itemIconInfo = pendingInfo;
        Transform transform = pendingItem;
        pendingInfo = null;
        pendingItem = null;
        Transform transform2;
        if (itemIconInfo.scale && itemIconInfo.skinAsset != null)
        {
            transform2 = transform.Find("Icon2");
            if (transform2 == null)
            {
                transform.position = new Vector3(0f, -256f, -256f);
                UnityEngine.Object.Destroy(transform.gameObject);
                Assets.ReportError(itemIconInfo.itemAsset, "missing 'Icon2' Transform");
                return;
            }
        }
        else
        {
            transform2 = transform.Find("Icon");
            if (transform2 == null)
            {
                transform.position = new Vector3(0f, -256f, -256f);
                UnityEngine.Object.Destroy(transform.gameObject);
                Assets.ReportError(itemIconInfo.itemAsset, "missing 'Icon' Transform");
                return;
            }
        }
        Texture2D texture2D = captureIcon(orthoSize: (itemIconInfo.scale && itemIconInfo.skinAsset != null) ? itemIconInfo.itemAsset.econIconCameraOrthographicSize : ((!itemIconInfo.itemAsset.isEligibleForAutomaticIconMeasurements) ? itemIconInfo.itemAsset.iconCameraOrthographicSize : CalculateOrthographicSize(itemIconInfo.itemAsset, transform.gameObject, transform2, itemIconInfo.x, itemIconInfo.y)), id: itemIconInfo.itemAsset.id, skin: itemIconInfo.skinAsset?.id ?? 0, model: transform, icon: transform2, width: itemIconInfo.x, height: itemIconInfo.y, readableOnCPU: itemIconInfo.readableOnCPU);
        if (itemIconInfo.callback != null)
        {
            try
            {
                itemIconInfo.callback(texture2D);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception during item icon capture callback:");
            }
        }
        if (itemIconInfo.isEligibleForCaching && !iconCache.ContainsKey(itemIconInfo.itemAsset))
        {
            iconCache.Add(itemIconInfo.itemAsset, texture2D);
        }
    }

    private void Start()
    {
        tool = this;
        cameraComponent = GetComponent<Camera>();
        lightComponent = GetComponent<Light>();
        icons = new Queue<ItemIconInfo>();
    }
}
