using System;
using System.Collections.Generic;
using SDG.Provider.Services;
using SDG.Provider.Services.Community;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace SDG.SteamworksProvider.Services.Community;

public class SteamworksCommunityService : Service, ICommunityService, IService
{
    private Dictionary<CSteamID, SteamGroup> cachedGroups;

    private Dictionary<CSteamID, Texture2D> cachedAvatars;

    public void setStatus(string status)
    {
        SteamFriends.SetRichPresence("status", status);
    }

    public Texture2D getIcon(int id)
    {
        if (id < 0)
        {
            return null;
        }
        if (!SteamUtils.GetImageSize(id, out var pnWidth, out var pnHeight))
        {
            return null;
        }
        if (pnWidth < 1 || pnHeight < 1 || pnWidth > 2048 || pnHeight > 2048)
        {
            return null;
        }
        byte[] array = new byte[pnWidth * pnHeight * 4];
        if (!SteamUtils.GetImageRGBA(id, array, array.Length))
        {
            return null;
        }
        int num = (int)pnWidth;
        int num2 = (int)pnHeight;
        int num3 = num2 / 2;
        for (int i = 0; i < num3; i++)
        {
            int num4 = num2 - 1 - i;
            int num5 = i * num * 4;
            int num6 = num4 * num * 4;
            for (int j = 0; j < num; j++)
            {
                int num7 = num5 + j * 4;
                int num8 = num6 + j * 4;
                for (int k = 0; k < 4; k++)
                {
                    int num9 = num7 + k;
                    int num10 = num8 + k;
                    byte b = array[num9];
                    array[num9] = array[num10];
                    array[num10] = b;
                }
            }
        }
        Texture2D texture2D;
        try
        {
            texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
            texture2D.hideFlags = HideFlags.HideAndDontSave;
            texture2D.LoadRawTextureData(array);
            texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        }
        catch (Exception e)
        {
            texture2D = null;
            UnturnedLog.exception(e, $"Caught exception creating Steam avatar {pnWidth}x{pnHeight}:");
        }
        return texture2D;
    }

    public Texture2D getIcon(CSteamID steamID, bool shouldCache = false)
    {
        Texture2D value = null;
        if (!shouldCache || !cachedAvatars.TryGetValue(steamID, out value))
        {
            value = getIcon(SteamFriends.GetSmallFriendAvatar(steamID));
            if (shouldCache)
            {
                cachedAvatars.Add(steamID, value);
            }
        }
        return value;
    }

    public SteamGroup getCachedGroup(CSteamID steamID)
    {
        cachedGroups.TryGetValue(steamID, out var value);
        return value;
    }

    public SteamGroup[] getGroups()
    {
        SteamGroup[] array = new SteamGroup[SteamFriends.GetClanCount()];
        for (int i = 0; i < array.Length; i++)
        {
            CSteamID clanByIndex = SteamFriends.GetClanByIndex(i);
            SteamGroup steamGroup = getCachedGroup(clanByIndex);
            if (steamGroup == null)
            {
                string clanName = SteamFriends.GetClanName(clanByIndex);
                Texture2D icon = getIcon(clanByIndex);
                steamGroup = new SteamGroup(clanByIndex, clanName, icon);
                cachedGroups.Add(clanByIndex, steamGroup);
            }
            array[i] = steamGroup;
        }
        return array;
    }

    public bool checkGroup(CSteamID steamID)
    {
        for (int i = 0; i < SteamFriends.GetClanCount(); i++)
        {
            if (SteamFriends.GetClanByIndex(i) == steamID)
            {
                return true;
            }
        }
        return false;
    }

    public SteamworksCommunityService()
    {
        cachedGroups = new Dictionary<CSteamID, SteamGroup>();
        cachedAvatars = new Dictionary<CSteamID, Texture2D>();
    }
}
