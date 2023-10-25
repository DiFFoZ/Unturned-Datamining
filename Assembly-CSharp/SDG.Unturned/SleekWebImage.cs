using UnityEngine;

namespace SDG.Unturned;

public class SleekWebImage : SleekWrapper
{
    /// <summary>
    /// If true, SizeOffset_X and SizeOffset_Y are used when image is available.
    /// Defaults to false.
    /// </summary>
    public bool useImageDimensions;

    /// <summary>
    /// If useImageDimensions is on and image width exceeds this value, scale down
    /// respecting aspect ratio.
    /// </summary>
    public float maxImageDimensionsWidth = -1f;

    private ISleekImage internalImage;

    public SleekColor color
    {
        get
        {
            return internalImage.TintColor;
        }
        set
        {
            internalImage.TintColor = value;
        }
    }

    public void Refresh(string url, bool shouldCache = true)
    {
        Provider.refreshIcon(new Provider.IconQueryParams(url, OnImageReady, shouldCache));
    }

    public override void OnDestroy()
    {
        internalImage = null;
    }

    public void Clear()
    {
        internalImage.Texture = null;
    }

    public SleekWebImage()
    {
        internalImage = Glazier.Get().CreateImage();
        internalImage.SizeScale_X = 1f;
        internalImage.SizeScale_Y = 1f;
        AddChild(internalImage);
    }

    private void OnImageReady(Texture2D icon, bool responsibleForDestroy)
    {
        if (useImageDimensions && icon != null)
        {
            if (maxImageDimensionsWidth > 0.5f && (float)icon.width > maxImageDimensionsWidth && icon.height > 0)
            {
                float num = (float)icon.width / (float)icon.height;
                base.SizeOffset_X = maxImageDimensionsWidth;
                base.SizeOffset_Y = maxImageDimensionsWidth / num;
            }
            else
            {
                base.SizeOffset_X = icon.width;
                base.SizeOffset_Y = icon.height;
            }
        }
        if (internalImage != null)
        {
            internalImage.SetTextureAndShouldDestroy(icon, responsibleForDestroy);
        }
        else if (responsibleForDestroy)
        {
            Object.Destroy(icon);
        }
    }
}
