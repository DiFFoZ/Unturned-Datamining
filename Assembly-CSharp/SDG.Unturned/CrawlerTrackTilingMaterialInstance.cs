using UnityEngine;

namespace SDG.Unturned;

internal class CrawlerTrackTilingMaterialInstance
{
    public Material material;

    public Wheel[] wheels;

    public Vector2 initialUvPosition;

    public float uvOffset;

    public float repeatDistance;

    public Vector2 uvDirection;

    /// <summary>
    /// Calculated speed of this track. Used by some wheels.
    /// </summary>
    public float speed;
}
