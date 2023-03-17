using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class SkinAsset : Asset
{
    protected bool _isPattern;

    protected bool _hasSight;

    protected bool _hasTactical;

    protected bool _hasGrip;

    protected bool _hasBarrel;

    protected bool _hasMagazine;

    protected Material _primarySkin;

    protected Dictionary<ushort, Material> _secondarySkins;

    protected Material _attachmentSkin;

    protected Material _tertiarySkin;

    public List<Mesh> overrideMeshes;

    public bool isPattern => _isPattern;

    public bool hasSight => _hasSight;

    public bool hasTactical => _hasTactical;

    public bool hasGrip => _hasGrip;

    public bool hasBarrel => _hasBarrel;

    public bool hasMagazine => _hasMagazine;

    public Material primarySkin => _primarySkin;

    public Dictionary<ushort, Material> secondarySkins => _secondarySkins;

    public Material attachmentSkin => _attachmentSkin;

    public Material tertiarySkin => _tertiarySkin;

    public ELightingTime? lightingTime { get; private set; }

    public override EAssetType assetCategory => EAssetType.SKIN;

    public ERagdollEffect ragdollEffect { get; protected set; }

    public void SetMaterialProperties(Material instance)
    {
        if (lightingTime.HasValue && LevelLighting.times != null)
        {
            LightingInfo lightingInfo = LevelLighting.times[(int)lightingTime.Value];
            instance.SetVector("_SunColor", lightingInfo.colors[0] * 1.5f);
            instance.SetVector("_RaysColor", lightingInfo.colors[10] * 1.5f);
            instance.SetVector("_SkyColor", lightingInfo.colors[3]);
            instance.SetVector("_EquatorColor", lightingInfo.colors[4]);
            instance.SetVector("_GroundColor", lightingInfo.colors[5]);
        }
    }

    public SkinAsset(bool isPattern, Material primarySkin, Dictionary<ushort, Material> secondarySkins, Material attachmentSkin, Material tertiarySkin)
    {
        _isPattern = isPattern;
        _hasSight = true;
        _hasTactical = true;
        _hasGrip = true;
        _hasBarrel = true;
        _hasMagazine = true;
        _primarySkin = primarySkin;
        _secondarySkins = secondarySkins;
        _attachmentSkin = attachmentSkin;
        _tertiarySkin = tertiarySkin;
        overrideMeshes = new List<Mesh>(0);
    }

    public SkinAsset(Bundle bundle, Data data, Local localization, ushort id)
        : base(bundle, data, localization, id)
    {
        if (id < 2000 && !bundle.isCoreAsset && !data.has("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 2000");
        }
        _isPattern = data.has("Pattern");
        if (data.has("LightingTime"))
        {
            lightingTime = data.readEnum("LightingTime", ELightingTime.DAWN);
        }
        else
        {
            lightingTime = null;
        }
        _hasSight = data.has("Sight");
        _hasTactical = data.has("Tactical");
        _hasGrip = data.has("Grip");
        _hasBarrel = data.has("Barrel");
        _hasMagazine = data.has("Magazine");
        ragdollEffect = data.readEnum("Ragdoll_Effect", ERagdollEffect.NONE);
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        _primarySkin = loadRequiredAsset<Material>(bundle, "Skin_Primary");
        _secondarySkins = new Dictionary<ushort, Material>();
        ushort num = data.readUInt16("Secondary_Skins", 0);
        for (ushort num2 = 0; num2 < num; num2 = (ushort)(num2 + 1))
        {
            ushort key = data.readUInt16("Secondary_" + num2, 0);
            if (!secondarySkins.ContainsKey(key))
            {
                Material value = loadRequiredAsset<Material>(bundle, "Skin_Secondary_" + key);
                secondarySkins.Add(key, value);
            }
        }
        _attachmentSkin = bundle.load<Material>("Skin_Attachment");
        _tertiarySkin = bundle.load<Material>("Skin_Tertiary");
        ushort num3 = data.readUInt16("Override_Meshes", 0);
        overrideMeshes = new List<Mesh>(num3);
        for (ushort num4 = 0; num4 < num3; num4 = (ushort)(num4 + 1))
        {
            GameObject gameObject = bundle.load<GameObject>("Override_Mesh_" + num4);
            if (gameObject != null)
            {
                MeshFilter component = gameObject.GetComponent<MeshFilter>();
                if (component != null)
                {
                    if (component.sharedMesh != null)
                    {
                        overrideMeshes.Add(component.sharedMesh);
                    }
                    else
                    {
                        Assets.reportError("missing MeshFilter sharedMesh on " + gameObject.name);
                    }
                }
                else
                {
                    Assets.reportError("missing MeshFilter on " + gameObject.name);
                }
            }
            else
            {
                Assets.reportError("missing Override_Mesh_" + num4);
            }
        }
    }
}
