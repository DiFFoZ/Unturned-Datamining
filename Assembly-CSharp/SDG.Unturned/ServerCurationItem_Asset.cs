using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal class ServerCurationItem_Asset : ServerCurationItem
{
    public ServerListCurationAsset asset;

    public override string DisplayName => asset.curationFile.Name;

    public override string DisplayOrigin => asset.GetOriginName();

    public override Texture2D Icon => asset.Icon;

    public override string IconUrl => asset.curationFile.IconUrl;

    public override bool IsDeletable => false;

    public override void Reload()
    {
        Assets.ReloadAsset(asset);
    }

    public override void Delete()
    {
    }

    public override List<ServerListCurationRule> GetRules()
    {
        return asset.curationFile.rules;
    }

    protected override void SaveActive()
    {
        string key = $"{asset.GUID:N}_Active";
        ConvenientSavedata.get().write(key, _isActive);
    }

    internal void NotifyAssetChanged(ServerListCurationAsset asset)
    {
        if (this.asset != asset)
        {
            this.asset = asset;
            InvokeDataChanged();
        }
    }

    public ServerCurationItem_Asset(ServerListCuration curation, ServerListCurationAsset asset)
        : base(curation)
    {
        this.asset = asset;
        string key = $"{asset.GUID:N}_Active";
        if (!ConvenientSavedata.get().read(key, out _isActive))
        {
            _isActive = false;
        }
    }
}
