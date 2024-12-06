using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Allow Unity events to apply damage in a sphere. (doesn't have any visual effects)
/// Intended to replace unsupported/unintentional use of "Grenade.cs" and "Rocket.cs" scripts.
/// </summary>
[AddComponentMenu("Unturned/Explosion Spawner")]
public class ExplosionSpawner : MonoBehaviour
{
    public float DamageRadius;

    public EDeathCause Cause = EDeathCause.GRENADE;

    /// <summary>
    /// Zombie explosion types have slight variations e.g. lighting zombies on fire.
    /// </summary>
    public EExplosionDamageType DamageType;

    /// <summary>
    /// If greater than -0.5, overrides default radius zombies and animals will be alerted within.
    /// </summary>
    public float AlertRadiusOverride = -1f;

    /// <summary>
    /// If true, per-surface effects like blood splatter are created.
    /// </summary>
    public bool PlayImpactEffects = true;

    /// <summary>
    /// If true, explosion damage passes through Barricades and Structures.
    /// </summary>
    public bool PenetrateBuildables = true;

    public float PlayerDamage;

    public float ZombieDamage;

    public float AnimalDamage;

    public float BarricadeDamage;

    public float StructureDamage;

    public float VehicleDamage;

    public float ResourceDamage;

    public float ObjectDamage;

    /// <summary>
    /// Speed to launch players away from blast position.
    /// </summary>
    public float LaunchSpeed;

    public void Explode()
    {
        if (Provider.isServer)
        {
            ExplosionParameters parameters = new ExplosionParameters(base.transform.position, DamageRadius, Cause, CSteamID.Nil);
            parameters.damageType = DamageType;
            if (AlertRadiusOverride > -0.5f)
            {
                parameters.alertRadius = AlertRadiusOverride;
            }
            parameters.playImpactEffect = PlayImpactEffects;
            parameters.penetrateBuildables = PenetrateBuildables;
            parameters.playerDamage = PlayerDamage;
            parameters.zombieDamage = ZombieDamage;
            parameters.animalDamage = AnimalDamage;
            parameters.barricadeDamage = BarricadeDamage;
            parameters.structureDamage = StructureDamage;
            parameters.vehicleDamage = VehicleDamage;
            parameters.resourceDamage = ResourceDamage;
            parameters.objectDamage = ObjectDamage;
            parameters.damageOrigin = EDamageOrigin.ExplosionSpawnerComponent;
            parameters.launchSpeed = LaunchSpeed;
            DamageTool.explode(parameters, out var _);
        }
    }
}
