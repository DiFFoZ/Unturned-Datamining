using System;
using SDG.Unturned;
using UnityEngine;

namespace SDG.Provider;

public struct DynamicEconDetails
{
    public string tags;

    public string dynamic_props;

    public bool getStatTrackerType(out EStatTrackerType type)
    {
        type = EStatTrackerType.NONE;
        if (tags.Contains("stat_tracker:total_kills"))
        {
            type = EStatTrackerType.TOTAL;
            return true;
        }
        if (tags.Contains("stat_tracker:player_kills"))
        {
            type = EStatTrackerType.PLAYER;
            return true;
        }
        return false;
    }

    public bool getRagdollEffect(out ERagdollEffect effect)
    {
        int num = tags.IndexOf("ragdoll_effect:");
        if (num >= 0)
        {
            num += "ragdoll_effect:".Length;
            if (num < tags.Length - 1)
            {
                ReadOnlySpan<char> span = tags.AsSpan(num, tags.Length - num);
                if (span.StartsWith("zero_kelvin"))
                {
                    effect = ERagdollEffect.ZERO_KELVIN;
                    return true;
                }
                if (span.StartsWith("jaded"))
                {
                    effect = ERagdollEffect.JADED;
                    return true;
                }
                if (span.StartsWith("soulcrystal_green"))
                {
                    effect = ERagdollEffect.SOUL_CRYSTAL_GREEN;
                    return true;
                }
                if (span.StartsWith("soulcrystal_magenta"))
                {
                    effect = ERagdollEffect.SOUL_CRYSTAL_MAGENTA;
                    return true;
                }
                if (span.StartsWith("soulcrystal_red"))
                {
                    effect = ERagdollEffect.SOUL_CRYSTAL_RED;
                    return true;
                }
                if (span.StartsWith("soulcrystal_yellow"))
                {
                    effect = ERagdollEffect.SOUL_CRYSTAL_YELLOW;
                    return true;
                }
            }
        }
        effect = ERagdollEffect.NONE;
        return false;
    }

    /// <summary>
    /// Parse dynamic tag mythic effect.
    /// </summary>
    /// <returns>ID of mythical asset, or zero if not in tags.</returns>
    public ushort getParticleEffect()
    {
        int num = tags.IndexOf("particle_effect:");
        if (num >= 0)
        {
            int num2 = num + "particle_effect:".Length;
            if (num2 < tags.Length)
            {
                int num3 = tags.IndexOf(';', num2);
                if (num3 < 0)
                {
                    num3 = tags.Length;
                }
                int length = num3 - num2;
                if (ushort.TryParse(tags.Substring(num2, length), out var result))
                {
                    return result;
                }
                return 0;
            }
            return 0;
        }
        return 0;
    }

    public bool getStatTrackerValue(out EStatTrackerType type, out int kills)
    {
        kills = -1;
        if (!getStatTrackerType(out type))
        {
            return false;
        }
        switch (type)
        {
        case EStatTrackerType.TOTAL:
            if (string.IsNullOrEmpty(dynamic_props))
            {
                kills = 0;
            }
            else
            {
                kills = JsonUtility.FromJson<StatTrackerTotalKillsJson>(dynamic_props).total_kills;
            }
            return true;
        case EStatTrackerType.PLAYER:
            if (string.IsNullOrEmpty(dynamic_props))
            {
                kills = 0;
            }
            else
            {
                kills = JsonUtility.FromJson<StatTrackerPlayerKillsJson>(dynamic_props).player_kills;
            }
            return true;
        default:
            return false;
        }
    }

    public string getPredictedDynamicPropsJsonForStatTracker(EStatTrackerType type, int kills)
    {
        switch (type)
        {
        case EStatTrackerType.TOTAL:
        {
            StatTrackerTotalKillsJson statTrackerTotalKillsJson = default(StatTrackerTotalKillsJson);
            statTrackerTotalKillsJson.total_kills = kills;
            return JsonUtility.ToJson(statTrackerTotalKillsJson);
        }
        case EStatTrackerType.PLAYER:
        {
            StatTrackerPlayerKillsJson statTrackerPlayerKillsJson = default(StatTrackerPlayerKillsJson);
            statTrackerPlayerKillsJson.player_kills = kills;
            return JsonUtility.ToJson(statTrackerPlayerKillsJson);
        }
        default:
            return string.Empty;
        }
    }

    public DynamicEconDetails(string tags, string dynamic_props)
    {
        this.tags = (string.IsNullOrEmpty(tags) ? string.Empty : tags);
        this.dynamic_props = (string.IsNullOrEmpty(dynamic_props) ? string.Empty : dynamic_props);
    }
}
