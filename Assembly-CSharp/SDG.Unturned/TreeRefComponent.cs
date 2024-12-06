using System;
using UnityEngine;

namespace SDG.Unturned;

internal class TreeRefComponent : MonoBehaviour, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    public ResourceSpawnpoint owner;

    public bool IsEligibleForExplosionDamage
    {
        get
        {
            if (owner != null)
            {
                return !owner.isDead;
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
        if (!damageParameters.shouldAffectTrees)
        {
            return;
        }
        Vector3 vector = damageParameters.closestPoint - explosionParameters.point;
        float magnitude = vector.magnitude;
        if (magnitude > explosionParameters.damageRadius)
        {
            return;
        }
        Vector3 direction = vector / magnitude;
        if (!damageParameters.LineOfSightTest(explosionParameters.point, direction, magnitude, out var hit) || !(hit.transform != null) || hit.transform.IsChildOf(base.transform))
        {
            ResourceManager.damage(base.transform, direction, explosionParameters.resourceDamage, 1f - magnitude / explosionParameters.damageRadius, 1f, out var kill, out var xp, explosionParameters.killer, explosionParameters.damageOrigin);
            if (kill != 0)
            {
                damageParameters.kills.Add(kill);
            }
            damageParameters.xp += xp;
        }
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
