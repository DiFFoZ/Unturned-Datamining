using UnityEngine;

namespace SDG.Unturned;

public class Skill
{
    public byte level;

    /// <summary>
    /// Vanilla maximum level.
    /// </summary>
    public byte max;

    /// <summary>
    /// If set, maximum skill level attainable through gameplay.
    /// </summary>
    public int maxUnlockableLevel = -1;

    /// <summary>
    /// Multiplier for XP upgrade cost.
    /// </summary>
    public float costMultiplier = 1f;

    private uint _cost;

    private float difficulty;

    public float mastery
    {
        get
        {
            if (level == 0)
            {
                return 0f;
            }
            if (level >= max)
            {
                return 1f;
            }
            return (float)(int)level / (float)(int)max;
        }
    }

    public uint cost => MathfEx.RoundAndClampToUInt((float)_cost * ((float)(int)level * difficulty + 1f) * costMultiplier);

    /// <summary>
    /// Get maximum level, or maxUnlockableLevel if set.
    /// </summary>
    /// <returns></returns>
    public int GetClampedMaxUnlockableLevel()
    {
        if (maxUnlockableLevel <= -1)
        {
            return max;
        }
        return Mathf.Min(max, maxUnlockableLevel);
    }

    public float NormalizeLevel(int inputLevel)
    {
        if (inputLevel <= 0)
        {
            return 0f;
        }
        if (inputLevel >= max)
        {
            return 1f;
        }
        return (float)inputLevel / (float)(int)max;
    }

    public void setLevelToMax()
    {
        level = max;
    }

    public Skill(byte newLevel, byte newMax, uint newCost, float newDifficulty)
    {
        level = newLevel;
        max = newMax;
        _cost = newCost;
        difficulty = newDifficulty;
    }
}
