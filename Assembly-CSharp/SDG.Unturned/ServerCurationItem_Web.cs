using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal class ServerCurationItem_Web : ServerCurationItem, IAssetErrorContext
{
    public int linkId;

    public string url;

    public bool isWaitingForResponse;

    private ServerListCurationFile file;

    private Coroutine coroutine;

    public override string DisplayName => file?.Name ?? url;

    public override string DisplayOrigin => url;

    public override Texture2D Icon => null;

    public override string IconUrl => file?.IconUrl;

    public override bool IsDeletable => true;

    public string AssetErrorPrefix => "Server List Curator at \"" + url + "\"";

    public override void Reload()
    {
        if (!isWaitingForResponse && (bool)Provider.allowWebRequests)
        {
            isWaitingForResponse = true;
            coroutine = curation.webRequestHandler.StartCoroutine(curation.webRequestHandler.SendRequest(this));
        }
    }

    public override void Delete()
    {
        if (isWaitingForResponse)
        {
            isWaitingForResponse = false;
            curation.webRequestHandler.StopCoroutine(coroutine);
        }
        IConvenientSavedata convenientSavedata = ConvenientSavedata.get();
        string key = $"ServerCurationWebLink_{linkId}_Active";
        convenientSavedata.DeleteBool(key);
        curation.RemoveUrl(this);
    }

    public override List<ServerListCurationRule> GetRules()
    {
        return file?.rules;
    }

    protected override void SaveActive()
    {
        string key = $"ServerCurationWebLink_{linkId}_Active";
        ConvenientSavedata.get().write(key, _isActive);
    }

    internal void NotifyRequestComplete(ServerListCurationFile file)
    {
        isWaitingForResponse = false;
        coroutine = null;
        bool num = file != null || (this.file != null && file == null);
        this.file = file;
        InvokeDataChanged();
        if (num)
        {
            curation.MarkDirty();
        }
    }

    public ServerCurationItem_Web(ServerListCuration curation, ServerListCurationWebLink link)
        : base(curation)
    {
        linkId = link.id;
        url = link.url;
        string key = $"ServerCurationWebLink_{linkId}_Active";
        if (!ConvenientSavedata.get().read(key, out _isActive))
        {
            _isActive = true;
        }
        if ((bool)Provider.allowWebRequests)
        {
            isWaitingForResponse = true;
            coroutine = curation.webRequestHandler.StartCoroutine(curation.webRequestHandler.SendRequest(this));
        }
    }
}
