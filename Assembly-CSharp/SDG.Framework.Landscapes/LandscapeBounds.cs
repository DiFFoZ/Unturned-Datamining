using UnityEngine;

namespace SDG.Framework.Landscapes;

public struct LandscapeBounds
{
    public LandscapeCoord min;

    public LandscapeCoord max;

    public override string ToString()
    {
        return "[" + min.ToString() + ", " + max.ToString() + "]";
    }

    public LandscapeBounds(LandscapeCoord newMin, LandscapeCoord newMax)
    {
        min = newMin;
        max = newMax;
    }

    public LandscapeBounds(Bounds worldBounds)
    {
        min = new LandscapeCoord(worldBounds.min);
        max = new LandscapeCoord(worldBounds.max);
    }
}
