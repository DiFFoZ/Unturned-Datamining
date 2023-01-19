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

    public ItemPantsAsset(Bundle bundle, Data data, Local localization, ushort id)
        : base(bundle, data, localization, id)
    {
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        _pants = loadRequiredAsset<Texture2D>(bundle, "Pants");
        if (pants != null && (bool)Assets.shouldValidateAssets)
        {
            if (pants.isReadable)
            {
                Assets.reportError(this, "texture 'Pants' can save memory by disabling read/write");
            }
            if (pants.format != TextureFormat.RGBA32 && (pants.width <= 256 || pants.height <= 256))
            {
                Assets.reportError(this, $"texture Pants looks weird because it is relatively low resolution but has compression enabled ({pants.format})");
            }
        }
        _emission = bundle.load<Texture2D>("Emission");
        if (emission != null && (bool)Assets.shouldValidateAssets)
        {
            if (emission.isReadable)
            {
                Assets.reportError(this, "texture 'Emission' can save memory by disabling read/write");
            }
            if (emission.width <= 256 || emission.height <= 256)
            {
                if (emission.format == TextureFormat.RGBA32)
                {
                    Assets.reportError(this, "texture Emission is relatively low resolution so RGB24 format is recommended");
                }
                else if (emission.format != TextureFormat.RGB24)
                {
                    Assets.reportError(this, $"texture Emission looks weird because it is relatively low resolution but has compression enabled ({emission.format})");
                }
            }
        }
        _metallic = bundle.load<Texture2D>("Metallic");
        if (metallic != null && (bool)Assets.shouldValidateAssets)
        {
            if (metallic.isReadable)
            {
                Assets.reportError(this, "texture 'Metallic' can save memory by disabling read/write");
            }
            if (metallic.format != TextureFormat.RGBA32 && (metallic.width <= 256 || metallic.height <= 256))
            {
                Assets.reportError(this, $"texture Metallic looks weird because it is relatively low resolution but has compression enabled ({metallic.format})");
            }
        }
    }
}
