using UnityEngine;

namespace SDG.Unturned;

internal struct ExplosionDamageCandidate
{
    public IExplosionDamageable target;

    public Vector3 closestPoint;
}
