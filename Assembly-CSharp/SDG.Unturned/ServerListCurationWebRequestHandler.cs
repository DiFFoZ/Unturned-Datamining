using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SDG.Unturned;

internal class ServerListCurationWebRequestHandler : MonoBehaviour
{
    internal IEnumerator SendRequest(ServerCurationItem_Web webItem)
    {
        using UnityWebRequest request = UnityWebRequest.Get(webItem.url);
        request.timeout = 10;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            UnturnedLog.error("Error getting server curation file from \"" + webItem.url + "\": \"" + request.error + "\"");
            webItem.ErrorMessage = $"{request.result}: \"{request.error}\"";
            webItem.NotifyRequestComplete(null);
            yield break;
        }
        try
        {
            DatParser datParser = new DatParser();
            DatDictionary data = datParser.Parse(request.downloadHandler.data);
            if (datParser.HasError)
            {
                Debug.LogError("Error parsing server curation file from \"" + webItem.url + "\": \"" + datParser.ErrorMessage + "\"");
                webItem.ErrorMessage = "Parsing error: \"" + datParser.ErrorMessage + "\"";
                webItem.NotifyRequestComplete(null);
            }
            else
            {
                webItem.ErrorMessage = null;
                ServerListCurationFile serverListCurationFile = new ServerListCurationFile();
                serverListCurationFile.Populate(webItem, data, null);
                webItem.NotifyRequestComplete(serverListCurationFile);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Caught exception getting server curation file from \"" + webItem.url + "\":");
            Debug.LogException(ex);
            webItem.ErrorMessage = "Exception: \"" + ex.Message + "\"";
            webItem.NotifyRequestComplete(null);
        }
    }
}
