using System;
using UnityEngine;

namespace SDG.Unturned;

public class ItemGlassesAsset : ItemGearAsset
{
    protected GameObject _glasses;

    private ELightingVision _vision;

    public Color nightvisionColor;

    public float nightvisionFogIntensity;

    public GameObject glasses => _glasses;

    public ELightingVision vision => _vision;

    public PlayerSpotLightConfig lightConfig { get; protected set; }

    public bool isBlindfold { get; protected set; }

    /// <summary>
    /// If true, NVGs work in third-person, not just first-person.
    /// Defaults to false.
    /// </summary>
    public bool isNightvisionAllowedInThirdPerson { get; protected set; }

    internal override GameObject ClothingPrefab => glasses;

    public override byte[] getState(EItemOrigin origin)
    {
        if (vision != 0)
        {
            return new byte[1] { 1 };
        }
        return new byte[0];
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (!Dedicator.IsDedicatedServer)
        {
            _glasses = loadRequiredAsset<GameObject>(bundle, "Glasses");
            if ((bool)Assets.shouldValidateAssets)
            {
                AssetValidation.ValidateLayersEqual(this, _glasses, 10);
                AssetValidation.ValidateClothComponents(this, _glasses);
            }
        }
        if (data.ContainsKey("Vision"))
        {
            _vision = (ELightingVision)Enum.Parse(typeof(ELightingVision), data.GetString("Vision"), ignoreCase: true);
            if (vision == ELightingVision.HEADLAMP)
            {
                lightConfig = new PlayerSpotLightConfig(data);
            }
            else if (vision == ELightingVision.CIVILIAN)
            {
                nightvisionColor = data.LegacyParseColor32RGB("Nightvision_Color", LevelLighting.NIGHTVISION_CIVILIAN);
                nightvisionFogIntensity = data.ParseFloat("Nightvision_Fog_Intensity", 0.5f);
                nightvisionColor.g = nightvisionColor.r;
                nightvisionColor.b = nightvisionColor.r;
            }
            else if (vision == ELightingVision.MILITARY)
            {
                nightvisionColor = data.LegacyParseColor32RGB("Nightvision_Color", LevelLighting.NIGHTVISION_MILITARY);
                nightvisionFogIntensity = data.ParseFloat("Nightvision_Fog_Intensity", 0.25f);
            }
            isNightvisionAllowedInThirdPerson = data.ParseBool("Nightvision_Allowed_In_ThirdPerson");
        }
        else
        {
            _vision = ELightingVision.NONE;
        }
        isBlindfold = data.ContainsKey("Blindfold");
    }

    protected override bool GetDefaultTakesPriorityOverCosmetic()
    {
        if (vision == ELightingVision.NONE)
        {
            return isBlindfold;
        }
        return true;
    }
}
