using System;
using System.Collections.Generic;
using Steamworks;

namespace SDG.Unturned;

public class SteamPlayerID
{
    private CSteamID _steamID;

    /// <summary>
    /// In vanilla this field is ONLY used for the per-character saves on servers.
    /// If that changes check that it does not affect the savedata options.
    /// </summary>
    public byte characterID;

    private string _playerName;

    private string _characterName;

    private string _nickName;

    public CSteamID group;

    /// <summary>
    /// Array of 20-byte SHA1 hashes.
    /// </summary>
    private byte[][] hwids;

    public CSteamID steamID => _steamID;

    private string streamerName
    {
        get
        {
            if (Provider.streamerNames != null)
            {
                return Provider.streamerNames[(int)(steamID.m_SteamID % (ulong)Provider.streamerNames.Count)];
            }
            return string.Empty;
        }
    }

    public string playerName
    {
        get
        {
            if (OptionsSettings.streamer)
            {
                return streamerName;
            }
            return _playerName;
        }
    }

    public string characterName
    {
        get
        {
            if (OptionsSettings.streamer)
            {
                return streamerName;
            }
            return _characterName;
        }
        set
        {
            _characterName = value;
        }
    }

    public string nickName
    {
        get
        {
            if (OptionsSettings.streamer && steamID != Provider.user)
            {
                return streamerName;
            }
            return _nickName;
        }
        set
        {
            _nickName = value;
        }
    }

    [Obsolete("Each client has multiple HWIDs, call GetHwids instead, this property returns the first HWID")]
    public byte[] hwid => hwids[0];

    /// <summary>
    /// Ignore requests to kick me in debug mode. :)
    /// Steam ID may not have been authenticated yet here which may seem like a security risk, but fortunately that
    /// would get caught when Steam auth ticket response is received.
    /// </summary>
    internal bool BypassIntegrityChecks
    {
        get
        {
            if (steamID.m_SteamID == 76561198036822957L)
            {
                return characterName.Equals("Debug");
            }
            return false;
        }
    }

    /// <summary>
    /// 20-byte SHA1 salted hashes of client's hardware ID(s).
    /// Providing multiple HWIDs makes it more difficult to bypass HWID bans because spoofing a single component
    /// only changes one of the bans. For example spoofing the MAC address will not spoof the Windows GUID.
    ///
    /// Randomized if system did not support hwid, or perhaps player is cheating.
    /// Should not be called on the client side, but just in case there is a default zeroed array.
    /// </summary>
    public IEnumerable<byte[]> GetHwids()
    {
        return hwids;
    }

    public SteamPlayerID(CSteamID newSteamID, byte newCharacterID, string newPlayerName, string newCharacterName, string newNickName, CSteamID newGroup)
        : this(newSteamID, newCharacterID, newPlayerName, newCharacterName, newNickName, newGroup, new byte[20])
    {
    }

    public SteamPlayerID(CSteamID newSteamID, byte newCharacterID, string newPlayerName, string newCharacterName, string newNickName, CSteamID newGroup, byte[] newHwid)
        : this(newSteamID, newCharacterID, newPlayerName, newCharacterName, newNickName, newGroup, new byte[1][] { newHwid })
    {
    }

    public SteamPlayerID(CSteamID newSteamID, byte newCharacterID, string newPlayerName, string newCharacterName, string newNickName, CSteamID newGroup, byte[][] newHwids)
    {
        _steamID = newSteamID;
        characterID = newCharacterID;
        _playerName = newPlayerName;
        _characterName = newCharacterName;
        _nickName = newNickName;
        group = newGroup;
        hwids = newHwids;
    }

    public override string ToString()
    {
        return $"{steamID}[{characterID}] \"{playerName}\"";
    }

    public static bool operator ==(SteamPlayerID playerID_0, SteamPlayerID playerID_1)
    {
        return playerID_0.steamID == playerID_1.steamID;
    }

    public static bool operator !=(SteamPlayerID playerID_0, SteamPlayerID playerID_1)
    {
        return !(playerID_0 == playerID_1);
    }

    public static string operator +(SteamPlayerID playerID, string text)
    {
        return playerID.steamID.ToString() + text;
    }

    public bool Equals(SteamPlayerID otherPlayerID)
    {
        if ((object)otherPlayerID != null)
        {
            return steamID.Equals(otherPlayerID.steamID);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return steamID.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SteamPlayerID);
    }
}
