using System;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Implemented by components to support taking damage from explosions.
/// Not intended for external use (yet?) and may need to change. 
/// </summary>
public interface IExplosionDamageable : IEquatable<IExplosionDamageable>
{
    /// <summary>
    /// Used to exclude dead entities from further evaluation.
    /// </summary>
    bool IsEligibleForExplosionDamage { get; }

    /// <summary>
    /// Used to sort damage from nearest to furthest.
    /// </summary>
    Vector3 GetClosestPointToExplosion(Vector3 explosionCenter);

    void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters);
}
