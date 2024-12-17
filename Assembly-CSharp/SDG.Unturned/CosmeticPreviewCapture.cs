using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class CosmeticPreviewCapture : MonoBehaviour
{
    public Camera shirtCamera;

    public Camera pantsCamera;

    public Camera backpackCamera;

    public Camera vestCamera;

    public Camera hatCamera;

    public Camera outfitCamera;

    private List<Renderer> renderers = new List<Renderer>();

    private HumanClothes clothes;

    private RenderTexture targetTexture4096;

    private RenderTexture targetTexture800;

    private RenderTexture targetTexture400;

    private RenderTexture downsampleTexture2048;

    private RenderTexture downsampleTexture400;

    private RenderTexture downsampleTexture200;

    private Texture2D exportTexture2048;

    private Texture2D exportTexture400;

    private Texture2D exportTexture200;

    public void CaptureCosmetics()
    {
        StartCoroutine(CaptureAllCosmeticsCoroutine());
    }

    public void CaptureOutfit(Guid guid)
    {
        if (Assets.find(guid) is OutfitAsset item)
        {
            List<OutfitAsset> outfitAssets = new List<OutfitAsset> { item };
            StartCoroutine(CaptureOutfitsCoroutine(outfitAssets));
        }
    }

    public void CaptureAllOutfits()
    {
        List<OutfitAsset> list = new List<OutfitAsset>();
        Assets.find(list);
        StartCoroutine(CaptureOutfitsCoroutine(list));
    }

    private IEnumerator CaptureAllCosmeticsCoroutine()
    {
        yield return RenderDefaultCharacter();
        string dirPath201 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "CosmeticPreviews_2048x2048");
        string dirPath202 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "CosmeticPreviews_400x400");
        string dirPath200 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "CosmeticPreviews_200x200");
        List<ItemAsset> list = new List<ItemAsset>();
        Assets.find(list);
        foreach (ItemAsset item in list)
        {
            if (!item.isPro || (item.type != EItemType.SHIRT && item.type != EItemType.PANTS && item.type != 0 && item.type != EItemType.BACKPACK && item.type != EItemType.VEST && item.type != EItemType.GLASSES && item.type != EItemType.MASK))
            {
                continue;
            }
            string text = Path.Combine(dirPath201, item.GUID.ToString("N") + ".png");
            string filePath201 = Path.Combine(dirPath202, item.GUID.ToString("N") + ".png");
            string filePath200 = Path.Combine(dirPath200, item.GUID.ToString("N") + ".png");
            if (!File.Exists(text) || !File.Exists(filePath201) || !File.Exists(filePath200))
            {
                UnturnedLog.info($"Capture cosmetic {item.FriendlyName} ({item.GUID:N})");
                ResetOutfit();
                ApplyItemToOutfit(item);
                clothes.apply();
                GetCamera(item);
                Camera itemCamera = GetCamera(item);
                GameObject clothingGameObject = GetClothingGameObject(item);
                if (clothingGameObject != null)
                {
                    Bounds worldBounds = GetWorldBounds(clothingGameObject);
                    FitCameraToBounds(itemCamera, worldBounds);
                }
                yield return Render(itemCamera, targetTexture4096, downsampleTexture2048, exportTexture2048, text);
                yield return Render(itemCamera, targetTexture800, downsampleTexture400, exportTexture400, filePath201);
                yield return Render(itemCamera, targetTexture400, downsampleTexture200, exportTexture200, filePath200);
            }
        }
    }

    private IEnumerator CaptureOutfitsCoroutine(List<OutfitAsset> outfitAssets)
    {
        yield return RenderDefaultCharacter();
        string dirPath201 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "OutfitPreviews_2048x2048");
        string dirPath202 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "OutfitPreviews_400x400");
        string dirPath200 = PathEx.Join(UnturnedPaths.RootDirectory, "Extras", "OutfitPreviews_200x200");
        foreach (OutfitAsset outfitAsset in outfitAssets)
        {
            ResetOutfit();
            AssetReference<ItemAsset>[] itemAssets = outfitAsset.itemAssets;
            foreach (AssetReference<ItemAsset> assetReference in itemAssets)
            {
                ItemAsset itemAsset = assetReference.Find();
                if (itemAsset != null)
                {
                    ApplyItemToOutfit(itemAsset);
                }
            }
            clothes.apply();
            Bounds worldBounds = new Bounds(base.transform.position + new Vector3(0f, 0.95f, 0f), new Vector3(0.1f, 1.9f, 0.1f));
            if (clothes.hatModel != null)
            {
                worldBounds.Encapsulate(GetWorldBounds(clothes.hatModel.gameObject));
            }
            if (clothes.backpackModel != null)
            {
                worldBounds.Encapsulate(GetWorldBounds(clothes.backpackModel.gameObject));
            }
            if (clothes.vestModel != null)
            {
                worldBounds.Encapsulate(GetWorldBounds(clothes.vestModel.gameObject));
            }
            if (clothes.glassesModel != null)
            {
                worldBounds.Encapsulate(GetWorldBounds(clothes.glassesModel.gameObject));
            }
            if (clothes.maskModel != null)
            {
                worldBounds.Encapsulate(GetWorldBounds(clothes.maskModel.gameObject));
            }
            worldBounds.Expand(-0.2f);
            FitCameraToBounds(outfitCamera, worldBounds);
            string exportFilePath = Path.Combine(dirPath201, outfitAsset.GUID.ToString("N") + ".png");
            string filePath201 = Path.Combine(dirPath202, outfitAsset.GUID.ToString("N") + ".png");
            string filePath200 = Path.Combine(dirPath200, outfitAsset.GUID.ToString("N") + ".png");
            yield return Render(outfitCamera, targetTexture4096, downsampleTexture2048, exportTexture2048, exportFilePath);
            yield return Render(outfitCamera, targetTexture800, downsampleTexture400, exportTexture400, filePath201);
            yield return Render(outfitCamera, targetTexture400, downsampleTexture200, exportTexture200, filePath200);
        }
    }

    private void ResetOutfit()
    {
        clothes.shirtGuid = Guid.Empty;
        clothes.pantsGuid = Guid.Empty;
        clothes.hatGuid = Guid.Empty;
        clothes.backpackGuid = Guid.Empty;
        clothes.vestGuid = Guid.Empty;
        clothes.glassesGuid = Guid.Empty;
        clothes.maskGuid = Guid.Empty;
    }

    private void ApplyItemToOutfit(ItemAsset itemAsset)
    {
        switch (itemAsset.type)
        {
        case EItemType.SHIRT:
            clothes.shirtGuid = itemAsset.GUID;
            break;
        case EItemType.PANTS:
            clothes.pantsGuid = itemAsset.GUID;
            break;
        case EItemType.HAT:
            clothes.hatGuid = itemAsset.GUID;
            break;
        case EItemType.BACKPACK:
            clothes.backpackGuid = itemAsset.GUID;
            break;
        case EItemType.VEST:
            clothes.vestGuid = itemAsset.GUID;
            break;
        case EItemType.GLASSES:
            clothes.glassesGuid = itemAsset.GUID;
            break;
        case EItemType.MASK:
            clothes.maskGuid = itemAsset.GUID;
            break;
        }
    }

    private GameObject GetClothingGameObject(ItemAsset itemAsset)
    {
        return itemAsset.type switch
        {
            EItemType.HAT => clothes.hatModel?.gameObject, 
            EItemType.BACKPACK => clothes.backpackModel?.gameObject, 
            EItemType.VEST => clothes.vestModel?.gameObject, 
            EItemType.GLASSES => clothes.glassesModel?.gameObject, 
            EItemType.MASK => clothes.maskModel?.gameObject, 
            _ => null, 
        };
    }

    private Camera GetCamera(ItemAsset itemAsset)
    {
        return itemAsset.type switch
        {
            EItemType.SHIRT => shirtCamera, 
            EItemType.PANTS => pantsCamera, 
            EItemType.HAT => hatCamera, 
            EItemType.BACKPACK => backpackCamera, 
            EItemType.VEST => vestCamera, 
            EItemType.GLASSES => hatCamera, 
            EItemType.MASK => hatCamera, 
            _ => null, 
        };
    }

    /// <summary>
    /// Render character with hair and skin otherwise it might be cyan.
    /// (public issue #3615)
    /// </summary>
    private IEnumerator RenderDefaultCharacter()
    {
        yield return new WaitForEndOfFrame();
        clothes.hair = 1;
        clothes.apply();
        outfitCamera.targetTexture = targetTexture400;
        outfitCamera.Render();
        outfitCamera.targetTexture = null;
        clothes.hair = 0;
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator Render(Camera cameraComponent, RenderTexture targetTexture, RenderTexture downsampleTexture, Texture2D exportTexture, string exportFilePath)
    {
        yield return new WaitForEndOfFrame();
        cameraComponent.targetTexture = targetTexture;
        cameraComponent.Render();
        cameraComponent.targetTexture = null;
        Graphics.Blit(targetTexture, downsampleTexture);
        RenderTexture.active = downsampleTexture;
        exportTexture.ReadPixels(new Rect(0f, 0f, downsampleTexture.width, downsampleTexture.height), 0, 0);
        RenderTexture.active = null;
        byte[] bytes = exportTexture.EncodeToPNG();
        File.WriteAllBytes(exportFilePath, bytes);
    }

    private void OnEnable()
    {
        clothes = GetComponent<HumanClothes>();
        clothes.skin = new Color32(210, 210, 210, byte.MaxValue);
        clothes.color = new Color32(175, 175, 175, byte.MaxValue);
        targetTexture4096 = RenderTexture.GetTemporary(4096, 4096, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        targetTexture4096.filterMode = FilterMode.Bilinear;
        targetTexture800 = RenderTexture.GetTemporary(800, 800, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        targetTexture800.filterMode = FilterMode.Bilinear;
        targetTexture400 = RenderTexture.GetTemporary(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        targetTexture400.filterMode = FilterMode.Bilinear;
        downsampleTexture2048 = RenderTexture.GetTemporary(2048, 2048, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        downsampleTexture400 = RenderTexture.GetTemporary(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        downsampleTexture200 = RenderTexture.GetTemporary(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        exportTexture2048 = new Texture2D(2048, 2048, TextureFormat.ARGB32, mipChain: false, linear: false);
        exportTexture400 = new Texture2D(400, 400, TextureFormat.ARGB32, mipChain: false, linear: false);
        exportTexture200 = new Texture2D(200, 200, TextureFormat.ARGB32, mipChain: false, linear: false);
        GetComponent<Animation>()["Idle_Stand"].speed = 0f;
        GetComponent<Animation>()["Idle_Stand"].normalizedTime = 0.2f;
    }

    private void OnDisable()
    {
        RenderTexture.ReleaseTemporary(targetTexture4096);
        RenderTexture.ReleaseTemporary(targetTexture800);
        RenderTexture.ReleaseTemporary(targetTexture400);
        RenderTexture.ReleaseTemporary(downsampleTexture2048);
        RenderTexture.ReleaseTemporary(downsampleTexture400);
        RenderTexture.ReleaseTemporary(downsampleTexture200);
        UnityEngine.Object.Destroy(exportTexture2048);
        UnityEngine.Object.Destroy(exportTexture400);
        UnityEngine.Object.Destroy(exportTexture200);
    }

    private Bounds GetWorldBounds(GameObject parent)
    {
        Bounds result = default(Bounds);
        bool flag = false;
        ParticleSystem.Particle[] array = new ParticleSystem.Particle[1024];
        parent.GetComponentsInChildren(renderers);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer particleSystemRenderer)
            {
                int particles = particleSystemRenderer.GetComponent<ParticleSystem>().GetParticles(array);
                for (int i = 0; i < particles; i++)
                {
                    ParticleSystem.Particle particle = array[i];
                    Vector3 center = renderer.transform.TransformPoint(particle.position);
                    if (flag)
                    {
                        result.Encapsulate(new Bounds(center, new Vector3(0.1f, 0.1f, 0.1f)));
                        continue;
                    }
                    result = new Bounds(center, Vector3.zero);
                    flag = true;
                }
            }
            else if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                if (flag)
                {
                    result.Encapsulate(renderer.bounds);
                    continue;
                }
                result = renderer.bounds;
                flag = true;
            }
        }
        return result;
    }

    private void FitCameraToBounds(Camera cameraComponent, Bounds worldBounds)
    {
        float magnitude = worldBounds.extents.magnitude;
        float f = cameraComponent.fieldOfView * 0.5f * (MathF.PI / 180f);
        float b = magnitude / Mathf.Sin(f);
        b = Mathf.Max(0.55f, b);
        float num = b + cameraComponent.nearClipPlane;
        cameraComponent.transform.position = worldBounds.center - cameraComponent.transform.forward * num;
    }
}
