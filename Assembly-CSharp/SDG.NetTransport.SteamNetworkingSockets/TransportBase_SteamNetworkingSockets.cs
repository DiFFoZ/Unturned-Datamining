using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using SDG.Unturned;
using Steamworks;

namespace SDG.NetTransport.SteamNetworkingSockets;

public abstract class TransportBase_SteamNetworkingSockets : TransportBase
{
    private struct DebugOutput
    {
        public ESteamNetworkingSocketsDebugOutputType type;

        public string message;
    }

    /// <summary>
    /// Should certificate authentication be disabled for UDP connections?
    /// </summary>
    protected static CommandLineFlag clAllowWithoutAuth = new CommandLineFlag(defaultValue: false, "-SNS_AllowWithoutAuth");

    /// <summary>
    /// Thanks DiFFoZ! Ensures GC does not release the delegate.
    /// </summary>
    private FSteamNetworkingSocketsDebugOutput debugOutputFunc;

    private ConcurrentQueue<DebugOutput> debugOutputQueue = new ConcurrentQueue<DebugOutput>();

    /// <summary>
    /// Does host want extra debug output?
    /// </summary>
    private static CommandLineInt clLogSteamNetworkingSockets = new CommandLineInt("-LogSteamNetworkingSockets");

    /// <summary>
    /// Overrides k_ESteamNetworkingConfig_SendBufferSize.
    /// </summary>
    private static CommandLineInt clSendBufferSize = new CommandLineInt("-SNS_SendBufferSize");

    /// <summary>
    /// Overrides k_ESteamNetworkingConfig_EnableDiagnosticsUI.
    /// </summary>
    private static CommandLineFlag clEnableDiagnosticsUI = new CommandLineFlag(defaultValue: false, "-SNS_EnableDiagnosticsUI");

    /// <summary>
    /// Log verbose information that should not be included in release builds.
    /// </summary>
    [Conditional("LOG_NETTRANSPORT_STEAMNETWORKINGSOCKETS")]
    internal void DebugLog(string format, params object[] args)
    {
        UnturnedLog.info(format, args);
    }

    /// <summary>
    /// Log helpful information that should be included in release builds.
    /// </summary>
    internal void Log(string format, params object[] args)
    {
        UnturnedLog.info(format, args);
    }

    internal string AddressToString(SteamNetworkingIPAddr address, bool withPort = true)
    {
        address.ToString(out var buf, withPort);
        return buf;
    }

    internal string IdentityToString(SteamNetworkingIdentity identity)
    {
        identity.ToString(out var buf);
        return buf;
    }

    internal string IdentityToString(ref SteamNetworkingMessage_t message)
    {
        return IdentityToString(message.m_identityPeer);
    }

    internal string IdentityToString(ref SteamNetConnectionStatusChangedCallback_t callback)
    {
        return IdentityToString(callback.m_info.m_identityRemote);
    }

    protected void DumpSteamNetworkingMessage(SteamNetworkingMessage_t message)
    {
        Log("Message Number {0}", message.m_nMessageNumber);
        Log("\tData: {0}", message.m_pData);
        Log("\tSize: {0}", message.m_cbSize);
        Log("\tConnection: {0}", message.m_conn);
        Log("\tPeer Identity: {0}", IdentityToString(message.m_identityPeer));
    }

    protected void LogDebugOutput()
    {
        DebugOutput result;
        while (debugOutputQueue.TryDequeue(out result))
        {
            string text = result.type switch
            {
                ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Bug => "Bug", 
                ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Error => "Error", 
                ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Important => "Important", 
                ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Warning => "Warning", 
                _ => null, 
            };
            if (string.IsNullOrEmpty(text))
            {
                UnturnedLog.info("SteamNetworkingSockets: " + result.message);
            }
            else
            {
                UnturnedLog.info("SteamNetworkingSockets " + text + ": " + result.message);
            }
        }
    }

    internal int ReliabilityToSendFlags(ENetReliability reliability)
    {
        if (reliability == ENetReliability.Reliable || reliability != ENetReliability.Unreliable)
        {
            return 8;
        }
        return 0;
    }

    protected ESteamNetworkingSocketsDebugOutputType SelectDebugOutputDetailLevel()
    {
        if (clLogSteamNetworkingSockets.hasValue)
        {
            try
            {
                return (ESteamNetworkingSocketsDebugOutputType)clLogSteamNetworkingSockets.value;
            }
            catch
            {
                Log("Unable to match {0} with a SNS output type", clLogSteamNetworkingSockets.value);
            }
        }
        return ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_None;
    }

    protected virtual List<SteamNetworkingConfigValue_t> BuildDefaultConfig()
    {
        List<SteamNetworkingConfigValue_t> list = new List<SteamNetworkingConfigValue_t>();
        if ((bool)clAllowWithoutAuth)
        {
            SteamNetworkingConfigValue_t item = default(SteamNetworkingConfigValue_t);
            item.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
            item.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_IP_AllowWithoutAuth;
            item.m_val.m_int32 = 1;
            list.Add(item);
        }
        if (clSendBufferSize.hasValue && clSendBufferSize.value > 0)
        {
            SteamNetworkingConfigValue_t item2 = default(SteamNetworkingConfigValue_t);
            item2.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
            item2.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize;
            item2.m_val.m_int32 = clSendBufferSize.value;
            list.Add(item2);
        }
        if ((bool)clEnableDiagnosticsUI)
        {
            SteamNetworkingConfigValue_t item3 = default(SteamNetworkingConfigValue_t);
            item3.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
            item3.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_EnableDiagnosticsUI;
            item3.m_val.m_int32 = 1;
            list.Add(item3);
        }
        SteamNetworkingConfigValue_t item4 = default(SteamNetworkingConfigValue_t);
        item4.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        item4.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial;
        item4.m_val.m_int32 = 30000;
        list.Add(item4);
        SteamNetworkingConfigValue_t item5 = default(SteamNetworkingConfigValue_t);
        item5.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        item5.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected;
        item5.m_val.m_int32 = 30000;
        list.Add(item5);
        return list;
    }

    protected FSteamNetworkingSocketsDebugOutput GetDebugOutputFunction()
    {
        debugOutputFunc = OnDebugOutput;
        return debugOutputFunc;
    }

    /// <summary>
    /// This callback may be called from a service thread. It must be threadsafe and fast! Do not make any other
    /// Steamworks calls from within the handler.
    /// </summary>
    private void OnDebugOutput(ESteamNetworkingSocketsDebugOutputType nType, IntPtr pszMsg)
    {
        try
        {
            string text = InteropHelp.PtrToStringUTF8(pszMsg);
            if (!string.IsNullOrEmpty(text))
            {
                DebugOutput item = default(DebugOutput);
                item.type = nType;
                item.message = text;
                debugOutputQueue.Enqueue(item);
            }
        }
        catch
        {
        }
    }
}
