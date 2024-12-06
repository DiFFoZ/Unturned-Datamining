using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SDG.Unturned;

public static class AssetValidation
{
    private static List<int> texturePropertyNameIDs = new List<int>();

    private static List<MeshFilter> staticMeshComponents = new List<MeshFilter>();

    private static List<MeshCollider> meshColliderComponents = new List<MeshCollider>();

    private static List<Renderer> allRenderers = new List<Renderer>();

    private static List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    private static List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    private static List<Cloth> clothComponents = new List<Cloth>();

    private static List<LODGroup> lodGroupComponents = new List<LODGroup>();

    public static void ValidateLayersEqual(Asset owningAsset, GameObject gameObject, int expectedLayer)
    {
        int layer = gameObject.layer;
        if (layer != expectedLayer)
        {
            Assets.ReportError(owningAsset, "expected '{0}' to have layer {1}, but it actually has layer {2}", gameObject.name, expectedLayer, layer);
        }
    }

    public static void ValidateLayersEqualRecursive(Asset owningAsset, GameObject gameObject, int expectedLayer)
    {
        ValidateLayersEqual(owningAsset, gameObject, expectedLayer);
        foreach (Transform item in gameObject.transform)
        {
            ValidateLayersEqualRecursive(owningAsset, item.gameObject, expectedLayer);
        }
    }

    public static void ValidateClothComponents(Asset owningAsset, GameObject gameObject)
    {
        clothComponents.Clear();
        gameObject.GetComponentsInChildren(clothComponents);
        foreach (Cloth clothComponent in clothComponents)
        {
            if (clothComponent.capsuleColliders.Length != 0 || clothComponent.sphereColliders.Length != 0)
            {
                Assets.ReportError(owningAsset, gameObject.name + " cloth component \"" + clothComponent.name + "\" has colliders which is problematic because unfortunately the game does not yet have a way for weapons to ignore them");
            }
        }
    }

    /// <summary>
    /// Relatively efficiently find mesh components, and log an error if their mesh is missing, among other checks.
    /// </summary>
    public static void searchGameObjectForErrors(Asset owningAsset, GameObject gameObject)
    {
        if (gameObject == null)
        {
            throw new ArgumentNullException("gameObject");
        }
        if (!Assets.shouldValidateAssets)
        {
            return;
        }
        staticMeshComponents.Clear();
        gameObject.GetComponentsInChildren(staticMeshComponents);
        foreach (MeshFilter staticMeshComponent in staticMeshComponents)
        {
            internalValidateMesh(owningAsset, gameObject, staticMeshComponent, staticMeshComponent.sharedMesh, 50000);
        }
        meshColliderComponents.Clear();
        gameObject.GetComponentsInChildren(meshColliderComponents);
        foreach (MeshCollider meshColliderComponent in meshColliderComponents)
        {
            internalValidateMesh(owningAsset, gameObject, meshColliderComponent, meshColliderComponent.sharedMesh, 25000);
        }
        allRenderers.Clear();
        gameObject.GetComponentsInChildren(allRenderers);
        foreach (Renderer allRenderer in allRenderers)
        {
            if (allRenderer.motionVectorGenerationMode == MotionVectorGenerationMode.ForceNoMotion)
            {
                Assets.ReportError(owningAsset, "{0} Renderer \"{1}\" motion vectors disabled could be a problem for TAA", gameObject.name, allRenderer.name);
            }
        }
        meshRenderers.Clear();
        gameObject.GetComponentsInChildren(meshRenderers);
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            if (meshRenderer.GetComponent<MeshFilter>() == null)
            {
                if (meshRenderer.GetComponent<TextMeshPro>() == null)
                {
                    Assets.ReportError(owningAsset, "{0} missing MeshFilter or TextMesh for MeshRenderer '{1}'", gameObject.name, meshRenderer.name);
                }
            }
            else if (meshRenderer.name != "DepthMask")
            {
                internalValidateRendererMaterials(owningAsset, gameObject, meshRenderer);
            }
        }
        skinnedMeshRenderers.Clear();
        gameObject.GetComponentsInChildren(skinnedMeshRenderers);
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            internalValidateMesh(owningAsset, gameObject, skinnedMeshRenderer, skinnedMeshRenderer.sharedMesh, 50000);
            internalValidateRendererMaterials(owningAsset, gameObject, skinnedMeshRenderer);
        }
        lodGroupComponents.Clear();
        gameObject.GetComponentsInChildren(includeInactive: true, lodGroupComponents);
        foreach (LODGroup lodGroupComponent in lodGroupComponents)
        {
            InternalValidateLodGroupComponent(owningAsset, lodGroupComponent);
        }
        InternalValidateRendererMultiLodRegistration(owningAsset);
    }

    private static void internalValidateMesh(Asset owningAsset, GameObject gameObject, Component component, Mesh sharedMesh, int maximumVertexCount)
    {
        if (sharedMesh == null)
        {
            if (!(component.GetComponent<TextMeshPro>() != null))
            {
                Assets.ReportError(owningAsset, "{0} missing mesh for {1} '{2}'", gameObject.name, component.GetType().Name, component.name);
            }
        }
        else if (sharedMesh.vertexCount > maximumVertexCount)
        {
            Assets.ReportError(owningAsset, "{0} mesh for {1} '{2}' has {3} vertices (ideal maximum of {4}) and might have room for optimization.", gameObject.name, component.GetType().Name, component.name, sharedMesh.vertexCount, maximumVertexCount);
        }
    }

    private static void internalValidateRendererMaterials(Asset owningAsset, GameObject gameObject, Renderer component)
    {
        int num = component.sharedMaterials.Length;
        if (num == 0)
        {
            Assets.ReportError(owningAsset, "{0} missing materials for Renderer '{1}'", gameObject.name, component.name);
            return;
        }
        if (num > 4)
        {
            Assets.ReportError(owningAsset, "{0} Renderer '{1}' has {2} separate materials (ideal maximum of {3}) which should be optimized to reduce draw calls.", gameObject.name, component.name, num, 4);
        }
        for (int i = 0; i < num; i++)
        {
            Material material = component.sharedMaterials[i];
            if (material == null)
            {
                Assets.ReportError(owningAsset, "{0} missing material[{1}] for Renderer '{2}'", gameObject.name, i, component.name);
            }
            else
            {
                internalValidateMaterialTextures(owningAsset, gameObject, component, material);
            }
        }
    }

    private static void internalValidateMaterialTextures(Asset owningAsset, GameObject gameObject, Renderer component, Material sharedMaterial)
    {
        texturePropertyNameIDs.Clear();
        sharedMaterial.GetTexturePropertyNameIDs(texturePropertyNameIDs);
        foreach (int texturePropertyNameID in texturePropertyNameIDs)
        {
            Texture texture = sharedMaterial.GetTexture(texturePropertyNameID);
            if (texture != null && !owningAsset.ignoreTextureReadable && !(texture is RenderTexture) && texture.isReadable)
            {
                Assets.ReportError(owningAsset, "{0} texture '{1}' referenced by material '{2}' used by Renderer '{3}' can save memory by disabling read/write.", gameObject.name, texture.name, sharedMaterial.name, component.name);
            }
            Texture2D texture2D = texture as Texture2D;
            if (texture2D != null && !owningAsset.ignoreNPOT && (!Mathf.IsPowerOfTwo(texture2D.width) || !Mathf.IsPowerOfTwo(texture2D.height)))
            {
                Assets.ReportError(owningAsset, "{0} texture '{1}' referenced by material '{2}' used by Renderer '{3}' has NPOT dimensions ({4} x {5})", gameObject.name, texture.name, sharedMaterial.name, component.name, texture2D.width, texture2D.height);
            }
        }
    }

    private static void InternalValidateLodGroupComponent(Asset owningAsset, LODGroup component)
    {
        LOD[] lODs = component.GetLODs();
        for (int i = 0; i < lODs.Length; i++)
        {
            LOD lOD = lODs[i];
            if (lOD.renderers.Length < 1)
            {
                Assets.ReportError(owningAsset, "LOD group on \"{0}\" LOD level {1} is empty", component.GetSceneHierarchyPath(), i);
                continue;
            }
            int num = 0;
            for (int j = 0; j < lOD.renderers.Length; j++)
            {
                if (lOD.renderers[j] == null)
                {
                    num++;
                }
            }
            if (num > 0)
            {
                Assets.ReportError(owningAsset, "LOD group on \"{0}\" LOD level {1} missing {2} renderer(s)", component.GetSceneHierarchyPath(), i, num);
            }
        }
    }

    /// <summary>
    /// Unity warns about renderers registered with more than one LOD group, so we do our own validation as part of
    /// asset loading to make it easier to find these.
    /// </summary>
    private static void InternalValidateRendererMultiLodRegistration(Asset owningAsset)
    {
        foreach (Renderer allRenderer in allRenderers)
        {
            LODGroup lODGroup = null;
            int num = 0;
            foreach (LODGroup lodGroupComponent in lodGroupComponents)
            {
                LOD[] lODs = lodGroupComponent.GetLODs();
                int num2 = 0;
                while (num2 < lODs.Length)
                {
                    Renderer[] renderers = lODs[num2].renderers;
                    int num3 = 0;
                    while (true)
                    {
                        if (num3 < renderers.Length)
                        {
                            if (renderers[num3] == allRenderer)
                            {
                                if (lODGroup == null)
                                {
                                    lODGroup = lodGroupComponent;
                                    num = num2;
                                }
                                else if (lodGroupComponent != lODGroup)
                                {
                                    Assets.ReportError(owningAsset, "renderer on \"{0}\" is registered with more than one LOD group, found in \"{1}\" LOD level {2} and \"{3}\" LOD level {4}", allRenderer.GetSceneHierarchyPath(), lODGroup.GetSceneHierarchyPath(), num, lodGroupComponent.GetSceneHierarchyPath(), num2);
                                    goto end_IL_00e5;
                                }
                            }
                            num3++;
                            continue;
                        }
                        num2++;
                        break;
                    }
                }
                continue;
                end_IL_00e5:
                break;
            }
        }
    }
}
