using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using SDG.Framework.Modules;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Provider;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal static class ServerMessageHandler_ReadyToConnect
{
    private struct AddressRateLimitingEntry
    {
        public uint address;

        public int counter;

        public double realtime;
    }

    private struct SteamIdRateLimitingEntry
    {
        public CSteamID steamId;

        public int counter;

        public double realtime;
    }

    private enum ERateLimitingResult
    {
        NOT_IN_LIST,
        HIT_RATE_LIMIT,
        WITHIN_RATE_LIMIT
    }

    private static List<ulong> pendingPackageSkins = new List<ulong>();

    private static List<AddressRateLimitingEntry> addressRateLimitingLog = new List<AddressRateLimitingEntry>();

    private static List<SteamIdRateLimitingEntry> steamIdRateLimitingLog = new List<SteamIdRateLimitingEntry>();

    [Conditional("LOG_CONNECT_ARGS")]
    private static void LogRead(string key, object value)
    {
        UnturnedLog.info("{0} = {1}", key, value);
    }

    internal static void ReadMessage(ITransportConnection transportConnection, NetPakReader reader)
    {
        if (Provider.findPendingPlayer(transportConnection) != null)
        {
            Provider.reject(transportConnection, ESteamRejection.ALREADY_PENDING);
            return;
        }
        if (Provider.findPlayer(transportConnection) != null)
        {
            Provider.reject(transportConnection, ESteamRejection.ALREADY_CONNECTED);
            return;
        }
        reader.ReadUInt8(out var value);
        reader.ReadString(out var value2);
        value2 = value2.Trim();
        reader.ReadString(out var value3);
        value3 = value3.Trim();
        byte[] array = new byte[20];
        reader.ReadBytes(array, 20);
        byte[] array2 = new byte[20];
        reader.ReadBytes(array2, 20);
        byte[] array3 = new byte[20];
        reader.ReadBytes(array3, 20);
        byte[] array4 = new byte[20];
        reader.ReadBytes(array4, 20);
        reader.ReadEnum(out var value4);
        reader.ReadUInt32(out var value5);
        reader.ReadBit(out var value6);
        reader.ReadUInt16(out var value7);
        reader.ReadString(out var value8);
        value8 = value8.Trim();
        reader.ReadSteamID(out CSteamID value9);
        reader.ReadUInt8(out var value10);
        reader.ReadUInt8(out var value11);
        reader.ReadUInt8(out var value12);
        reader.ReadColor32RGB(out Color32 value13);
        reader.ReadColor32RGB(out Color32 value14);
        reader.ReadColor32RGB(out Color32 value15);
        reader.ReadBit(out var value16);
        reader.ReadUInt64(out var value17);
        reader.ReadUInt64(out var value18);
        reader.ReadUInt64(out var value19);
        reader.ReadUInt64(out var value20);
        reader.ReadUInt64(out var value21);
        reader.ReadUInt64(out var value22);
        reader.ReadUInt64(out var value23);
        pendingPackageSkins.Clear();
        reader.ReadList(pendingPackageSkins, reader.ReadUInt64, Provider.MAX_SKINS_LENGTH);
        reader.ReadEnum(out var value24);
        reader.ReadString(out var value25);
        reader.ReadString(out var value26);
        reader.ReadSteamID(out CSteamID value27);
        reader.ReadUInt32(out var value28);
        reader.ReadUInt8(out var value29);
        if (value29 > 8)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_ASSEMBLY);
            return;
        }
        byte[][] array5 = new byte[value29][];
        for (byte b = 0; b < value29; b++)
        {
            array5[b] = new byte[20];
            reader.ReadBytes(array5[b]);
        }
        byte[] array6 = new byte[20];
        reader.ReadBytes(array6, 20);
        reader.ReadSteamID(out CSteamID value30);
        if (transportConnection.TryGetSteamId(out var steamId) && value30.m_SteamID != steamId)
        {
            Provider.reject(transportConnection, ESteamRejection.STEAM_ID_MISMATCH);
            return;
        }
        if (IsBlockedBySteamIdRateLimiting(value30))
        {
            Provider.reject(transportConnection, ESteamRejection.CONNECT_RATE_LIMITING);
            return;
        }
        if (Provider.findPendingPlayerBySteamId(value30) != null)
        {
            Provider.reject(transportConnection, ESteamRejection.ALREADY_PENDING);
            return;
        }
        if (PlayerTool.getSteamPlayer(value30) != null)
        {
            Provider.reject(transportConnection, ESteamRejection.ALREADY_CONNECTED);
            return;
        }
        if (!Provider.modeConfigData.Players.Allow_Per_Character_Saves)
        {
            value = 0;
        }
        SteamPlayerID steamPlayerID = new SteamPlayerID(value30, value, value2, value3, value8, value9, array5);
        if (!Provider.canClientVersionJoinServer(value5))
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_VERSION, Provider.APP_VERSION);
            return;
        }
        if (value28 != Level.packedVersion)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_LEVEL_VERSION, Level.version);
            return;
        }
        if (string.IsNullOrWhiteSpace(steamPlayerID.playerName) || NameTool.containsRichText(steamPlayerID.playerName) || steamPlayerID.playerName.ContainsNewLine())
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_INVALID);
            return;
        }
        if (string.IsNullOrWhiteSpace(steamPlayerID.characterName) || NameTool.containsRichText(steamPlayerID.characterName) || steamPlayerID.characterName.ContainsNewLine())
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_INVALID);
            return;
        }
        if (string.IsNullOrWhiteSpace(steamPlayerID.nickName) || NameTool.containsRichText(steamPlayerID.nickName) || steamPlayerID.nickName.ContainsNewLine())
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PRIVATE_INVALID);
            return;
        }
        if (steamPlayerID.playerName.Length < 2)
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_SHORT);
            return;
        }
        if (steamPlayerID.characterName.Length < 2)
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_SHORT);
            return;
        }
        if (steamPlayerID.playerName.Length > 32)
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_LONG);
            return;
        }
        if (steamPlayerID.characterName.Length > 32)
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_LONG);
            return;
        }
        if (steamPlayerID.nickName.Length > 32)
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PRIVATE_LONG);
            return;
        }
        if (long.TryParse(steamPlayerID.playerName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _) || double.TryParse(steamPlayerID.playerName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _))
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_NUMBER);
            return;
        }
        if (long.TryParse(steamPlayerID.characterName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _) || double.TryParse(steamPlayerID.characterName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _))
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_NUMBER);
            return;
        }
        if (long.TryParse(steamPlayerID.nickName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _) || double.TryParse(steamPlayerID.nickName, NumberStyles.Any, CultureInfo.InvariantCulture, out var _))
        {
            Provider.reject(transportConnection, ESteamRejection.NAME_PRIVATE_NUMBER);
            return;
        }
        if (Provider.filterName)
        {
            if (!NameTool.isValid(steamPlayerID.playerName))
            {
                Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_INVALID);
                return;
            }
            if (!NameTool.isValid(steamPlayerID.characterName))
            {
                Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_INVALID);
                return;
            }
            if (!NameTool.isValid(steamPlayerID.nickName))
            {
                Provider.reject(transportConnection, ESteamRejection.NAME_PRIVATE_INVALID);
                return;
            }
        }
        if (steamPlayerID.steamID.m_SteamID != 76561198036822957L && steamPlayerID.steamID.m_SteamID != 76561198267201306L)
        {
            if (IsNameBlockedByNelsonFilter(value2))
            {
                Provider.reject(transportConnection, ESteamRejection.NAME_PLAYER_INVALID);
                return;
            }
            if (IsNameBlockedByNelsonFilter(value3))
            {
                Provider.reject(transportConnection, ESteamRejection.NAME_CHARACTER_INVALID);
                return;
            }
        }
        uint address;
        bool flag = transportConnection.TryGetIPv4Address(out address);
        Provider.checkBanStatus(steamPlayerID, address, out var isBanned, out var banReason, out var banRemainingDuration);
        if (isBanned)
        {
            Provider.notifyBannedInternal(transportConnection, banReason, banRemainingDuration);
            return;
        }
        if (flag && !Provider.configData.Server.Use_FakeIP && IsBlockedByAddressRateLimiting(address))
        {
            Provider.reject(transportConnection, ESteamRejection.CONNECT_RATE_LIMITING);
            return;
        }
        bool flag2 = SteamWhitelist.checkWhitelisted(value30);
        if (Provider.isWhitelisted && !flag2)
        {
            Provider.reject(transportConnection, ESteamRejection.WHITELISTED);
            return;
        }
        if (Provider.clients.Count + 1 > Provider.maxPlayers && Provider.pending.Count + 1 > Provider.queueSize)
        {
            Provider.reject(transportConnection, ESteamRejection.SERVER_FULL);
            return;
        }
        if (array.Length != 20)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_PASSWORD);
            return;
        }
        if (array2.Length != 20)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_LEVEL);
            return;
        }
        if (array3.Length != 20)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_ASSEMBLY);
            return;
        }
        if (array4.Length != 20)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_RESOURCES);
            return;
        }
        if (Provider.configData.Server.Validate_EconInfo_Hash && !Hash.verifyHash(array6, TempSteamworksEconomy.econInfoHash) && !steamPlayerID.BypassIntegrityChecks)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_ECON);
            return;
        }
        ModuleDependency[] array7;
        if (string.IsNullOrEmpty(value25))
        {
            array7 = new ModuleDependency[0];
        }
        else
        {
            string[] array8 = value25.Split(';');
            array7 = new ModuleDependency[array8.Length];
            for (int i = 0; i < array7.Length; i++)
            {
                string[] array9 = array8[i].Split(',');
                if (array9.Length == 2)
                {
                    array7[i] = new ModuleDependency();
                    array7[i].Name = array9[0];
                    uint.TryParse(array9[1], NumberStyles.Any, CultureInfo.InvariantCulture, out array7[i].Version_Internal);
                }
            }
        }
        List<Module> critMods = Provider.critMods;
        Provider.critMods.Clear();
        ModuleHook.getRequiredModules(critMods);
        bool flag3 = true;
        for (int j = 0; j < array7.Length; j++)
        {
            bool flag4 = false;
            if (array7[j] != null)
            {
                for (int k = 0; k < critMods.Count; k++)
                {
                    if (critMods[k] != null && critMods[k].config != null && critMods[k].config.Name == array7[j].Name && critMods[k].config.Version_Internal >= array7[j].Version_Internal)
                    {
                        flag4 = true;
                        break;
                    }
                }
            }
            if (!flag4)
            {
                flag3 = false;
                break;
            }
        }
        if (!flag3)
        {
            Provider.reject(transportConnection, ESteamRejection.CLIENT_MODULE_DESYNC);
            return;
        }
        bool flag5 = true;
        for (int l = 0; l < critMods.Count; l++)
        {
            bool flag6 = false;
            if (critMods[l] != null && critMods[l].config != null)
            {
                for (int m = 0; m < array7.Length; m++)
                {
                    if (array7[m] != null && array7[m].Name == critMods[l].config.Name && array7[m].Version_Internal >= critMods[l].config.Version_Internal)
                    {
                        flag6 = true;
                        break;
                    }
                }
            }
            if (!flag6)
            {
                flag5 = false;
                break;
            }
        }
        if (!flag5)
        {
            Provider.reject(transportConnection, ESteamRejection.SERVER_MODULE_DESYNC);
            return;
        }
        if (!string.IsNullOrEmpty(Provider.serverPassword) && !Hash.verifyHash(array, Provider._serverPasswordHash))
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_PASSWORD);
            return;
        }
        if (!Hash.verifyHash(array2, Level.hash) && !steamPlayerID.BypassIntegrityChecks)
        {
            Provider.reject(transportConnection, ESteamRejection.WRONG_HASH_LEVEL);
            return;
        }
        if (value7 > Provider.configData.Server.Max_Ping_Milliseconds)
        {
            Provider.reject(transportConnection, ESteamRejection.PING, Provider.configData.Server.Max_Ping_Milliseconds.ToString());
            return;
        }
        if (Provider.modeConfigData.Players.Enable_Terrain_Color_Kick && IsSkinColorWithinThresholdOfTerrainColor(value13))
        {
            Provider.reject(transportConnection, ESteamRejection.SKIN_COLOR_WITHIN_THRESHOLD_OF_TERRAIN_COLOR);
            return;
        }
        SteamPending steamPending = new SteamPending(transportConnection, steamPlayerID, value6, value10, value11, value12, value13, value14, value15, value16, value17, value18, value19, value20, value21, value22, value23, pendingPackageSkins.ToArray(), value24, value26, value27, value4);
        byte queuePosition;
        bool flag7;
        if (!Provider.isWhitelisted && flag2)
        {
            if (Provider.pending.Count == 0)
            {
                Provider.pending.Add(steamPending);
                queuePosition = 0;
                flag7 = true;
            }
            else
            {
                Provider.pending.Insert(1, steamPending);
                queuePosition = 1;
                flag7 = false;
            }
        }
        else
        {
            queuePosition = MathfEx.ClampToByte(Provider.pending.Count);
            Provider.pending.Add(steamPending);
            flag7 = queuePosition == 0;
        }
        UnturnedLog.info($"Added {steamPlayerID} to queue position {queuePosition} (shouldVerify: {flag7})");
        steamPending.lastNotifiedQueuePosition = queuePosition;
        NetMessages.SendMessageToClient(EClientMessage.QueuePositionChanged, ENetReliability.Reliable, transportConnection, delegate(NetPakWriter writer)
        {
            writer.WriteUInt8(queuePosition);
        });
        if (flag7)
        {
            Provider.verifyNextPlayerInQueue();
        }
    }

    /// <summary>
    /// Kick players maybe trying to impersonate me. I guess nobody else named Nelson is allowed in the game!
    /// 2023-09-19: relaxed this a bit by trimming names and using Equals/Starts/Ends rather than Contains
    /// because there was a player with Nelson in their username.
    /// </summary>
    private static bool IsNameBlockedByNelsonFilter(string name)
    {
        if (name.Equals("Nelson", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        if (name.StartsWith("SDG", StringComparison.InvariantCultureIgnoreCase) && name.EndsWith("Nelson", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        return false;
    }

    private static bool IsSkinColorWithinThresholdOfTerrainColor(Color32 skinColor)
    {
        LevelAsset asset = Level.getAsset();
        if (asset == null || asset.terrainColorRules == null || asset.terrainColorRules.Count < 1)
        {
            return false;
        }
        Color.RGBToHSV(skinColor, out var H, out var S, out var V);
        foreach (LevelAsset.TerrainColorRule terrainColorRule in asset.terrainColorRules)
        {
            if (terrainColorRule != null && terrainColorRule.CompareColors(H, S, V) == LevelAsset.TerrainColorRule.EComparisonResult.TooSimilar)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsBlockedByAddressRateLimiting(uint connectionAddress)
    {
        double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
        float join_Rate_Limit_Window_Seconds = Provider.configData.Server.Join_Rate_Limit_Window_Seconds;
        ERateLimitingResult eRateLimitingResult = ERateLimitingResult.NOT_IN_LIST;
        for (int num = addressRateLimitingLog.Count - 1; num >= 0; num--)
        {
            AddressRateLimitingEntry value = addressRateLimitingLog[num];
            if (realtimeSinceStartupAsDouble - value.realtime > (double)join_Rate_Limit_Window_Seconds)
            {
                addressRateLimitingLog.RemoveAt(num);
            }
            else if (eRateLimitingResult == ERateLimitingResult.NOT_IN_LIST && value.address == connectionAddress)
            {
                value.counter++;
                value.realtime = realtimeSinceStartupAsDouble;
                addressRateLimitingLog[num] = value;
                eRateLimitingResult = ((value.counter > 2) ? ERateLimitingResult.HIT_RATE_LIMIT : ERateLimitingResult.WITHIN_RATE_LIMIT);
            }
        }
        if (eRateLimitingResult != 0)
        {
            return eRateLimitingResult == ERateLimitingResult.HIT_RATE_LIMIT;
        }
        AddressRateLimitingEntry item = default(AddressRateLimitingEntry);
        item.address = connectionAddress;
        item.counter = 1;
        item.realtime = realtimeSinceStartupAsDouble;
        addressRateLimitingLog.Add(item);
        return false;
    }

    private static bool IsBlockedBySteamIdRateLimiting(CSteamID connectionSteamId)
    {
        double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
        float join_Rate_Limit_Window_Seconds = Provider.configData.Server.Join_Rate_Limit_Window_Seconds;
        ERateLimitingResult eRateLimitingResult = ERateLimitingResult.NOT_IN_LIST;
        for (int num = steamIdRateLimitingLog.Count - 1; num >= 0; num--)
        {
            SteamIdRateLimitingEntry value = steamIdRateLimitingLog[num];
            if (realtimeSinceStartupAsDouble - value.realtime > (double)join_Rate_Limit_Window_Seconds)
            {
                steamIdRateLimitingLog.RemoveAt(num);
            }
            else if (eRateLimitingResult == ERateLimitingResult.NOT_IN_LIST && value.steamId == connectionSteamId)
            {
                value.counter++;
                value.realtime = realtimeSinceStartupAsDouble;
                steamIdRateLimitingLog[num] = value;
                eRateLimitingResult = ((value.counter > 2) ? ERateLimitingResult.HIT_RATE_LIMIT : ERateLimitingResult.WITHIN_RATE_LIMIT);
            }
        }
        if (eRateLimitingResult != 0)
        {
            return eRateLimitingResult == ERateLimitingResult.HIT_RATE_LIMIT;
        }
        SteamIdRateLimitingEntry item = default(SteamIdRateLimitingEntry);
        item.steamId = connectionSteamId;
        item.counter = 1;
        item.realtime = realtimeSinceStartupAsDouble;
        steamIdRateLimitingLog.Add(item);
        return false;
    }
}
