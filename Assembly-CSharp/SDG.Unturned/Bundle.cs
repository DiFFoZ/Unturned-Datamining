using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class Bundle
{
    private static List<AudioSource> audioSources = new List<AudioSource>();

    private static List<Renderer> renderers = new List<Renderer>();

    public bool convertShadersToStandard;

    public bool consolidateShaders = true;

    public AssetBundle asset { get; protected set; }

    public string resource { get; protected set; }

    public string name { get; protected set; }

    [Obsolete]
    public bool hasResource => asset == null;

    protected virtual bool willBeUnloadedDuringUse => true;

    protected void fixupMaterialForRenderer(Transform rootTransform, Renderer renderer, Material sharedMaterial)
    {
        Shader shader = sharedMaterial.shader;
        if (convertShadersToStandard || shader == null)
        {
            sharedMaterial.shader = Shader.Find("Standard");
        }
        else if (consolidateShaders)
        {
            Shader shader2 = ShaderConsolidator.findConsolidatedShader(shader);
            if (shader2 != null)
            {
                sharedMaterial.shader = shader2;
            }
            else
            {
                Transform transform = renderer.transform;
                string text = transform.name;
                while (transform != rootTransform)
                {
                    transform = transform.parent;
                    text = transform.name + "/" + text;
                }
                UnturnedLog.warn("Unable to find consolidated version of shader {0} for material {1} in {2} {3}", shader.name, sharedMaterial.name, name, text);
            }
        }
        else
        {
            UnturnedLog.error("fixupMaterialForRenderer should not have been called for {0}", name);
        }
        StandardShaderUtils.maybeFixupMaterial(sharedMaterial);
    }

    protected virtual void processLoadedGameObject(GameObject gameObject)
    {
        if (!Dedicator.IsDedicatedServer)
        {
            audioSources.Clear();
            gameObject.GetComponentsInChildren(includeInactive: true, audioSources);
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.outputAudioMixerGroup == null)
                {
                    audioSource.outputAudioMixerGroup = UnturnedAudioMixer.GetDefaultGroup();
                }
            }
        }
        if ((!convertShadersToStandard && !consolidateShaders) || Dedicator.IsDedicatedServer)
        {
            return;
        }
        renderers.Clear();
        gameObject.GetComponentsInChildren(includeInactive: true, renderers);
        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            foreach (Material material in sharedMaterials)
            {
                if (!(material == null))
                {
                    fixupMaterialForRenderer(gameObject.transform, renderer, material);
                }
            }
        }
    }

    protected virtual void processLoadedMaterial(Material material)
    {
        if (!convertShadersToStandard && !consolidateShaders)
        {
            return;
        }
        Shader shader = material.shader;
        if (convertShadersToStandard || shader == null)
        {
            material.shader = Shader.Find("Standard");
        }
        else if (consolidateShaders)
        {
            Shader shader2 = ShaderConsolidator.findConsolidatedShader(shader);
            if (shader2 != null)
            {
                material.shader = shader2;
            }
            else
            {
                UnturnedLog.warn("Unable to find consolidated version of shader {0} for material {1} in {2}", shader.name, material.name, name);
            }
        }
        StandardShaderUtils.maybeFixupMaterial(material);
    }

    protected virtual void processLoadedObject<T>(T loadedObject) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            processLoadedGameObject(loadedObject as GameObject);
        }
        else if (typeof(T) == typeof(AudioClip))
        {
            if (willBeUnloadedDuringUse && !Dedicator.IsDedicatedServer)
            {
                AudioClip audioClip = loadedObject as AudioClip;
                if ((bool)audioClip && !audioClip.preloadAudioData)
                {
                    audioClip.LoadAudioData();
                }
            }
        }
        else if (typeof(T) == typeof(Material) && !Dedicator.IsDedicatedServer)
        {
            processLoadedMaterial(loadedObject as Material);
        }
    }

    /// <summary>
    /// Save a reference to an object in the asset bundle, but defer loading it until requested by game code.
    /// </summary>
    public virtual void loadDeferred<T>(string name, out IDeferredAsset<T> asset, LoadedAssetDeferredCallback<T> callback = null) where T : UnityEngine.Object
    {
        NonDeferredAsset<T> nonDeferredAsset = default(NonDeferredAsset<T>);
        T val = (nonDeferredAsset.loadedObject = load<T>(name));
        asset = nonDeferredAsset;
        callback?.Invoke(val);
    }

    public virtual T load<T>(string name) where T : UnityEngine.Object
    {
        if (asset == null)
        {
            return Resources.Load<T>(resource + "/" + name);
        }
        if (asset.Contains(name))
        {
            T val = asset.LoadAsset<T>(name);
            processLoadedObject(val);
            return val;
        }
        return null;
    }

    public virtual string WhereLoadLookedToString(string name)
    {
        if (asset == null)
        {
            return resource + "/" + name + " in built-in resources";
        }
        return name + " in .unity3d asset bundle";
    }

    public T[] loadAll<T>() where T : UnityEngine.Object
    {
        if (!(asset != null))
        {
            return null;
        }
        return asset.LoadAllAssets<T>();
    }

    public void unload()
    {
        if (asset != null)
        {
            asset.Unload(unloadAllLoadedObjects: false);
        }
    }

    protected Bundle(string name)
    {
        asset = null;
        resource = null;
        this.name = name;
    }

    public Bundle(string path, bool usePath, string nameOverride = null)
    {
        if (ReadWrite.fileExists(path, useCloud: false, usePath))
        {
            string text = path.Replace(".unity3d", "_Linux.unity3d");
            if (ReadWrite.fileExists(text, useCloud: false, usePath))
            {
                asset = AssetBundle.LoadFromFile(usePath ? (ReadWrite.PATH + text) : text);
            }
            if (asset == null)
            {
                asset = AssetBundle.LoadFromFile(usePath ? (ReadWrite.PATH + path) : path);
            }
        }
        else
        {
            asset = null;
        }
        name = ((nameOverride != null) ? nameOverride : ReadWrite.fileName(path));
        if (asset == null)
        {
            resource = ReadWrite.folderPath(path).Substring(1);
        }
    }

    [Obsolete]
    public Bundle()
    {
        asset = null;
        name = "#NAME";
    }

    [Obsolete]
    public UnityEngine.Object[] load()
    {
        if (asset != null)
        {
            return asset.LoadAllAssets();
        }
        return null;
    }

    [Obsolete]
    public UnityEngine.Object[] load(Type type)
    {
        if (asset != null)
        {
            return asset.LoadAllAssets(type);
        }
        return null;
    }
}
