using System;
using System.Collections.Generic;

namespace SDG.Unturned;

/// <summary>
/// Work-in-progress plan to allow modders to create custom physics effects.
/// </summary>
internal static class PhysicMaterialCustomData
{
    private class CombinedPhysicMaterialInfo
    {
        public PhysicsMaterialAsset baseAsset;

        public CombinedPhysicMaterialInfo fallback;

        public Dictionary<string, MasterBundleReference<OneShotAudioDefinition>> audioDefs = new Dictionary<string, MasterBundleReference<OneShotAudioDefinition>>(StringComparer.OrdinalIgnoreCase);

        public AssetReference<EffectAsset> bulletImpactEffect;

        public AssetReference<EffectAsset> tireMotionEffect;

        public EPhysicsMaterialCharacterFrictionMode characterFrictionMode;

        public bool? isArable;

        public bool? hasOil;

        public float? characterAccelerationMultiplier;

        public float? characterDecelerationMultiplier;

        public float? characterMaxSpeedMultiplier;
    }

    private static Dictionary<string, CombinedPhysicMaterialInfo> nameInfos = new Dictionary<string, CombinedPhysicMaterialInfo>(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<Guid, PhysicsMaterialAsset> baseAssets = new Dictionary<Guid, PhysicsMaterialAsset>();

    private static Dictionary<Guid, PhysicsMaterialExtensionAsset> extensionAssets = new Dictionary<Guid, PhysicsMaterialExtensionAsset>();

    private static List<CombinedPhysicMaterialInfo> enumerableInfos = new List<CombinedPhysicMaterialInfo>();

    private static bool needsRebuild = false;

    public static OneShotAudioDefinition GetAudioDef(string materialName, string propertyName)
    {
        OneShotAudioDefinition result = null;
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (item.audioDefs.TryGetValue(propertyName, out var value))
            {
                result = value.loadAsset();
                break;
            }
        }
        return result;
    }

    public static AssetReference<EffectAsset> WipDoNotUseTemp_GetBulletImpactEffect(string materialName)
    {
        AssetReference<EffectAsset> result = default(AssetReference<EffectAsset>);
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (item.bulletImpactEffect.isValid)
            {
                result = item.bulletImpactEffect;
                return result;
            }
        }
        return result;
    }

    public static AssetReference<EffectAsset> GetTireMotionEffect(string materialName)
    {
        AssetReference<EffectAsset> result = default(AssetReference<EffectAsset>);
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (item.tireMotionEffect.isValid)
            {
                result = item.tireMotionEffect;
                return result;
            }
        }
        return result;
    }

    public static PhysicsMaterialCharacterFrictionProperties GetCharacterFrictionProperties(string materialName)
    {
        PhysicsMaterialCharacterFrictionProperties result = default(PhysicsMaterialCharacterFrictionProperties);
        result.mode = EPhysicsMaterialCharacterFrictionMode.ImmediatelyResponsive;
        result.accelerationMultiplier = 1f;
        result.decelerationMultiplier = 1f;
        result.maxSpeedMultiplier = 1f;
        bool flag = false;
        bool flag2 = false;
        bool flag3 = false;
        bool flag4 = false;
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (!flag && item.characterFrictionMode != 0)
            {
                result.mode = item.characterFrictionMode;
                flag = true;
            }
            if (!flag2 && item.characterAccelerationMultiplier.HasValue)
            {
                flag2 = true;
                result.accelerationMultiplier = item.characterAccelerationMultiplier.Value;
            }
            if (!flag3 && item.characterDecelerationMultiplier.HasValue)
            {
                flag3 = true;
                result.decelerationMultiplier = item.characterDecelerationMultiplier.Value;
            }
            if (!flag4 && item.characterMaxSpeedMultiplier.HasValue)
            {
                flag4 = true;
                result.maxSpeedMultiplier = item.characterMaxSpeedMultiplier.Value;
            }
            if (flag && flag2 && flag3 && flag4)
            {
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Can crops be planted on a given material?
    /// </summary>
    public static bool IsArable(string materialName)
    {
        bool result = false;
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (item.isArable.HasValue)
            {
                result = item.isArable.Value;
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Can oil drills be placed on a given material?
    /// </summary>
    public static bool HasOil(string materialName)
    {
        bool result = false;
        foreach (CombinedPhysicMaterialInfo item in EnumerateInfo(materialName))
        {
            if (item.hasOil.HasValue)
            {
                result = item.hasOil.Value;
                break;
            }
        }
        return result;
    }

    public static void RegisterAsset(PhysicsMaterialAsset asset)
    {
        baseAssets[asset.GUID] = asset;
        needsRebuild = true;
    }

    public static void RegisterAsset(PhysicsMaterialExtensionAsset asset)
    {
        extensionAssets[asset.GUID] = asset;
        needsRebuild = true;
    }

    public static Dictionary<Guid, PhysicsMaterialAsset> GetAssets()
    {
        return baseAssets;
    }

    public static Dictionary<Guid, PhysicsMaterialExtensionAsset> GetExtensionAssets()
    {
        return extensionAssets;
    }

    private static List<CombinedPhysicMaterialInfo> EnumerateInfo(string materialName)
    {
        enumerableInfos.Clear();
        if (string.IsNullOrEmpty(materialName))
        {
            return enumerableInfos;
        }
        if (needsRebuild)
        {
            needsRebuild = false;
            Rebuild();
        }
        if (nameInfos.TryGetValue(materialName, out var value))
        {
            do
            {
                enumerableInfos.Add(value);
                value = value.fallback;
            }
            while (value != null);
        }
        return enumerableInfos;
    }

    private static void PopulateInfo(CombinedPhysicMaterialInfo info, PhysicsMaterialAssetBase asset)
    {
        if (asset.audioDefs == null)
        {
            return;
        }
        foreach (KeyValuePair<string, MasterBundleReference<OneShotAudioDefinition>> audioDef in asset.audioDefs)
        {
            info.audioDefs.Add(audioDef.Key, audioDef.Value);
        }
    }

    private static void Rebuild()
    {
        nameInfos.Clear();
        Dictionary<Guid, CombinedPhysicMaterialInfo> dictionary = new Dictionary<Guid, CombinedPhysicMaterialInfo>();
        foreach (KeyValuePair<Guid, PhysicsMaterialAsset> baseAsset in baseAssets)
        {
            PhysicsMaterialAsset value = baseAsset.Value;
            CombinedPhysicMaterialInfo value2 = null;
            string[] physicMaterialNames = value.physicMaterialNames;
            foreach (string text in physicMaterialNames)
            {
                if (nameInfos.TryGetValue(text, out value2))
                {
                    Assets.ReportError(value, "physics material name \"" + text + "\" already taken by " + value2.baseAsset.name);
                    break;
                }
            }
            if (value2 != null)
            {
                continue;
            }
            if (dictionary.TryGetValue(value.GUID, out value2))
            {
                Assets.ReportError(value, $"guid \"{value.GUID}\" already taken by {value2.baseAsset.name}");
                continue;
            }
            value2 = new CombinedPhysicMaterialInfo();
            value2.baseAsset = value;
            physicMaterialNames = value.physicMaterialNames;
            foreach (string key in physicMaterialNames)
            {
                nameInfos[key] = value2;
            }
            dictionary[value.GUID] = value2;
            value2.bulletImpactEffect = value.bulletImpactEffect;
            value2.tireMotionEffect = value.tireMotionEffect;
            value2.characterFrictionMode = value.characterFrictionMode;
            value2.isArable = value.isArable;
            value2.hasOil = value.hasOil;
            value2.characterAccelerationMultiplier = value.characterAccelerationMultiplier;
            value2.characterDecelerationMultiplier = value.characterDecelerationMultiplier;
            value2.characterMaxSpeedMultiplier = value.characterMaxSpeedMultiplier;
            PopulateInfo(value2, value);
        }
        foreach (KeyValuePair<string, CombinedPhysicMaterialInfo> nameInfo in nameInfos)
        {
            CombinedPhysicMaterialInfo value3 = nameInfo.Value;
            if (value3.baseAsset.fallbackRef.isValid && !dictionary.TryGetValue(value3.baseAsset.fallbackRef.GUID, out value3.fallback))
            {
                Assets.ReportError(value3.baseAsset, $"unable to find fallback asset {value3.baseAsset.fallbackRef}");
            }
        }
        foreach (KeyValuePair<Guid, PhysicsMaterialExtensionAsset> extensionAsset in extensionAssets)
        {
            PhysicsMaterialExtensionAsset value4 = extensionAsset.Value;
            if (!dictionary.TryGetValue(value4.baseRef.GUID, out var value5))
            {
                Assets.ReportError(value4, $"unable to find base asset {value4.baseRef}");
            }
            else
            {
                PopulateInfo(value5, value4);
            }
        }
    }
}
