using UnityEngine;

namespace SDG.Unturned;

public class ItemPantsAsset : ItemBagAsset
{
    protected Texture2D _pants;

    protected Texture2D _emission;

    protected Texture2D _metallic;

    public Texture2D pants => _pants;

    public Texture2D emission => _emission;

    public Texture2D metallic => _metallic;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        _pants = loadRequiredAsset<Texture2D>(bundle, "Pants");
        if (pants != null && (bool)Assets.shouldValidateAssets)
        {
            if (pants.isReadable)
            {
                Assets.ReportError(this, "texture 'Pants' can save memory by disabling read/write");
            }
            if (pants.format != TextureFormat.RGBA32 && pants.format != TextureFormat.RGB24 && (pants.width <= 128 || pants.height <= 128))
            {
                Assets.ReportError(this, $"texture Pants might look weird because it is relatively low resolution but has compression enabled ({pants.format})");
            }
        }
        _emission = bundle.load<Texture2D>("Emission");
        if (emission != null && (bool)Assets.shouldValidateAssets)
        {
            if (emission.isReadable)
            {
                Assets.ReportError(this, "texture 'Emission' can save memory by disabling read/write");
            }
            if (emission.width <= 128 || emission.height <= 128)
            {
                if (emission.format == TextureFormat.RGBA32)
                {
                    Assets.ReportError(this, "texture Emission is relatively low resolution so RGB24 format is recommended");
                }
                else if (emission.format != TextureFormat.RGB24)
                {
                    Assets.ReportError(this, $"texture Emission might look weird because it is relatively low resolution but has compression enabled ({emission.format})");
                }
            }
        }
        _metallic = bundle.load<Texture2D>("Metallic");
        if (metallic != null && (bool)Assets.shouldValidateAssets)
        {
            if (metallic.isReadable)
            {
                Assets.ReportError(this, "texture 'Metallic' can save memory by disabling read/write");
            }
            if (metallic.format != TextureFormat.RGBA32 && (metallic.width <= 128 || metallic.height <= 128))
            {
                Assets.ReportError(this, $"texture Metallic might look weird because it is relatively low resolution but has compression enabled ({metallic.format})");
            }
        }
    }
}
