using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Responsible for the per-process .log file in the Logs directory.
/// Kept multiple log files in the past, but now consolidates all information
/// into a single file named Client.log or Server_{Identifier}.log.
/// </summary>
public class Logs : MonoBehaviour
{
    /// <summary>
    /// Should setup of the default *.log file be disabled?
    /// </summary>
    public static CommandLineFlag noDefaultLog = new CommandLineFlag(defaultValue: false, "-NoDefaultLog");

    private static CommandLineFlag shouldRedactLogs = new CommandLineFlag(defaultValue: true, "-UnredactedLogs");

    private static LogFile debugLog = null;

    /// <summary>
    /// If true, information like IP addresses and login tokens should be censored in vanilla logs.
    /// Nelson 2024-11-11: Special thanks to Jdance and asineth0 in public issue #4740!
    /// </summary>
    public static bool ShouldRedactLogs => shouldRedactLogs.value;

    /// <summary>
    /// Text to replace with if <see cref="F:SDG.Unturned.Logs.shouldRedactLogs" /> is enabled.
    /// </summary>
    public static string RedactionReplacement { get; set; } = "[redacted]";


    /// <summary>
    /// *ATTEMPTS* to replace IPv4 address(es) with <see cref="P:SDG.Unturned.Logs.RedactionReplacement" />.
    /// Should only be called if <see cref="P:SDG.Unturned.Logs.ShouldRedactLogs" /> is enabled.
    /// Case-by-case redaction should be preferred for performance reasons over using this function. This function
    /// is intended for third-party messages (e.g., BattlEye) that we don't have control over.
    /// </summary>
    /// <returns>True if message was modified.</returns>
    public static bool RedactIPv4Addresses(ref string message)
    {
        StringBuilder stringBuilder = null;
        int num = 0;
        int num2 = -1;
        int num3 = 0;
        int num4 = 0;
        int num5 = 0;
        for (int i = 0; i < message.Length; i++)
        {
            if (char.IsDigit(message, i))
            {
                if (num2 < 0)
                {
                    num2 = i;
                    num3 = 0;
                    num4 = 0;
                    num5 = 1;
                }
                else if (num4 != num3)
                {
                    num3 = num4;
                    num5++;
                }
            }
            else if (message[i] == '.')
            {
                num4++;
            }
            else
            {
                if (num2 < 0)
                {
                    continue;
                }
                if (num5 == 4)
                {
                    if (stringBuilder == null)
                    {
                        stringBuilder = new StringBuilder(message.Length * 2);
                    }
                    stringBuilder.Append(message.Substring(num, num2 - num));
                    stringBuilder.Append(RedactionReplacement);
                    if (num4 > 3)
                    {
                        stringBuilder.Append('.');
                    }
                    num = i;
                }
                num2 = -1;
            }
        }
        if (num2 >= 0 && num5 == 4)
        {
            if (stringBuilder == null)
            {
                stringBuilder = new StringBuilder(message.Length * 2);
            }
            stringBuilder.Append(message.Substring(num, num2 - num));
            stringBuilder.Append(RedactionReplacement);
            if (num4 > 3)
            {
                stringBuilder.Append('.');
            }
            num = message.Length;
        }
        if (num < message.Length)
        {
            stringBuilder?.Append(message.Substring(num));
        }
        if (stringBuilder != null)
        {
            message = stringBuilder.ToString();
            return true;
        }
        return false;
    }

    public static void printLine(string message)
    {
        if (debugLog != null && !string.IsNullOrEmpty(message))
        {
            string text = message.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                string arg = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                debugLog.writeLine($"[{arg}] {text}");
            }
        }
    }

    /// <summary>
    /// Get logging to path.
    /// </summary>
    public static string getLogFilePath()
    {
        if (debugLog == null)
        {
            return null;
        }
        return debugLog.path;
    }

    /// <summary>
    /// Set path to log to.
    /// </summary>
    public static void setLogFilePath(string logFilePath)
    {
        if (!logFilePath.EndsWith(".log"))
        {
            throw new ArgumentException("should be a .log file", "logFilePath");
        }
        closeLogFile();
        try
        {
            string directoryName = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        try
        {
            if (File.Exists(logFilePath))
            {
                string text = logFilePath.Insert(logFilePath.Length - 4, "_Prev");
                if (File.Exists(text))
                {
                    File.Delete(text);
                }
                File.Move(logFilePath, text);
            }
        }
        catch (Exception exception2)
        {
            Debug.LogException(exception2);
        }
        try
        {
            debugLog = new LogFile(logFilePath);
        }
        catch (Exception exception3)
        {
            Debug.LogException(exception3);
        }
    }

    /// <summary>
    /// Close current log file.
    /// </summary>
    public static void closeLogFile()
    {
        if (debugLog != null)
        {
            debugLog.close();
            debugLog = null;
        }
    }

    public void awake()
    {
        if (!noDefaultLog)
        {
            string pATH = ReadWrite.PATH;
            pATH = ((!Dedicator.IsDedicatedServer) ? (pATH + "/Logs/Client.log") : (pATH + "/Logs/Server_" + Dedicator.serverID.Replace(' ', '_') + ".log"));
            double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
            setLogFilePath(pATH);
            double num = Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble;
            if (num > 0.1)
            {
                UnturnedLog.info($"Initializing logging took {num}s");
            }
            NetReflection.SetLogCallback(UnturnedLog.info);
        }
    }

    private void OnDestroy()
    {
        closeLogFile();
    }
}
