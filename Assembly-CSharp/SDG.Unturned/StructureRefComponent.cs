using System;
using UnityEngine;

namespace SDG.Unturned;

internal class StructureRefComponent : MonoBehaviour, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    internal StructureDrop tempNotSureIfStructureShouldBeAComponentYet;

    public bool IsEligibleForExplosionDamage
    {
        get
        {
            StructureDrop structureDrop = tempNotSureIfStructureShouldBeAComponentYet;
            if (structureDrop != null)
            {
                ItemStructureAsset asset = structureDrop.asset;
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
        return CollisionUtil.ClosestPoint(base.gameObject, explosionCenter, includeInactive: false);
    }

    public void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        if (!damageParameters.shouldAffectStructures)
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
                StructureManager.damage(base.transform, direction, explosionParameters.structureDamage, 1f - magnitude / explosionParameters.damageRadius, armor: true, explosionParameters.killer, explosionParameters.damageOrigin);
            }
        }
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
