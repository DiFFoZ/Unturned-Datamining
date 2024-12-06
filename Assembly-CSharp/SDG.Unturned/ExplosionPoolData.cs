using System.Collections.Generic;

namespace SDG.Unturned;

/// <summary>
/// Data that we pool to reduce allocations, but needs to be separate per-invocation of explosion in case it's
/// invoked recursively. (for example, by blowing up a vehicle)
/// </summary>
internal struct ExplosionPoolData
{
    public List<ExplosionDamageCandidate> damageCandidates;

    public List<EPlayerKill> kills;
}
