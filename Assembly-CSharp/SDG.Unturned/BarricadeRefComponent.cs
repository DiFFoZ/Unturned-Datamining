using System;
using UnityEngine;

namespace SDG.Unturned;

internal class BarricadeRefComponent : MonoBehaviour, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    internal BarricadeDrop tempNotSureIfBarricadeShouldBeAComponentYet;

    public bool IsEligibleForExplosionDamage
    {
        get
        {
            BarricadeDrop barricadeDrop = tempNotSureIfBarricadeShouldBeAComponentYet;
            if (barricadeDrop != null)
            {
                ItemBarricadeAsset asset = barricadeDrop.asset;
                if (asset != null && !asset.proofExplosion)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool Equals(IExplosionDamageable obj)
    {
        return this == obj;
    }

    public Vector3 GetClosestPointToExplosion(Vector3 explosionCenter)
    {
        return CollisionUtil.ClosestPoint(base.gameObject, explosionCenter, includeInactive: false, -4194305);
    }

    public void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        if (!damageParameters.shouldAffectBarricades)
        {
            return;
        }
        Vector3 vector = damageParameters.closestPoint - explosionParameters.point;
        float magnitude = vector.magnitude;
        if (!(magnitude > explosionParameters.damageRadius))
        {
            Vector3 direction = vector / magnitude;
            if (!damageParameters.LineOfSightTest(explosionParameters.point, direction, magnitude, out var hit) || !(hit.transform != null) || hit.transform.IsChildOf(base.transform))
            {
                BarricadeManager.damage(base.transform, explosionParameters.barricadeDamage, 1f - magnitude / explosionParameters.damageRadius, armor: true, explosionParameters.killer, explosionParameters.damageOrigin);
            }
        }
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
