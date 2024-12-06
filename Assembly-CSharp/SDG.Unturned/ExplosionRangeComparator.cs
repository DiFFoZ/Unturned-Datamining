using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal class ExplosionRangeComparator : IComparer<ExplosionDamageCandidate>
{
    public Vector3 explosionCenter;

    public int Compare(ExplosionDamageCandidate lhs, ExplosionDamageCandidate rhs)
    {
        return Mathf.RoundToInt(((lhs.closestPoint - explosionCenter).sqrMagnitude - (rhs.closestPoint - explosionCenter).sqrMagnitude) * 100f);
    }
}
