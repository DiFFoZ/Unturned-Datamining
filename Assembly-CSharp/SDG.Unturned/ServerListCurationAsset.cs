using UnityEngine;

namespace SDG.Unturned;

public class ServerListCurationAsset : Asset
{
    internal ServerListCurationFile curationFile;

    /// <summary>
    /// Optional image bundled alongside the asset file.
    /// </summary>
    public Texture2D Icon { get; protected set; }

    public override string FriendlyName => curationFile?.Name ?? name;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        Icon = LoadRedirectableAsset<Texture2D>(bundle, "Icon", data, "IconAssetPath");
        curationFile = new ServerListCurationFile();
        curationFile.Populate(this, data, localization);
        if (string.IsNullOrEmpty(curationFile.Name))
        {
            curationFile.Name = name;
        }
    }
}
