using UnityEngine;

namespace SDG.Unturned;

public class BulletInfo
{
    /// <summary>
    /// Starting position when the bullet was fired.
    /// </summary>
    public Vector3 origin;

    public byte steps;

    public float quality;

    public byte pellet;

    public ushort dropID;

    public byte dropAmount;

    public byte dropQuality;

    public ItemBarrelAsset barrelAsset;

    public ItemMagazineAsset magazineAsset;

    /// <summary>
    /// Combination of gun and attachments' bullet gravity multipliers.
    /// </summary>
    internal float gravityMultiplier = 1f;

    /// <summary>
    /// Only available on the server. For use by plugins developers who want to analyze deviation between approximate
    /// start direction and final hit position using <see cref="E:SDG.Unturned.UseableGun.onBulletSpawned" /> and <see cref="E:SDG.Unturned.UseableGun.onBulletHit" />
    /// per public issue #4450. Note that origin and direction on server are not necessarily exactly the same as on
    /// the client for a variety of reasons, including that bullets on the client can be spawned between simulation
    /// frames when the aim direction was different. (Aim direction is updated every drawn frame on the client as
    /// opposed to every simulation frame on the server.) Rather than kicking for one particularly large deviation
    /// we would recommend tracking stats for each shot's actual deviation vs max theoretical deviation. Remember
    /// to account for bullet drop and that aim spread is relative to this direction. (For example, a shotgun may
    /// fire ~8 pellets in a cone around this direction.) Note also that in third-person the bullet can turn up to
    /// 90 degrees from the aim direction if the camera is up against a wall.
    /// </summary>
    public Vector3 ApproximatePlayerAimDirection { get; internal set; }

    public Vector3 position { get; internal set; }

    public Vector3 velocity { get; internal set; }

    public Vector3 GetDirection()
    {
        return velocity.normalized;
    }
}
