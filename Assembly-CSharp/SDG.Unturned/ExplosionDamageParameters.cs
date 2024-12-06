using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Intended for internal use only.
/// </summary>
public struct ExplosionDamageParameters
{
    private const float MIN_LINE_OF_SIGHT_DISTANCE = 0.01f;

    public Vector3 closestPoint;

    public List<EPlayerKill> kills;

    public uint xp;

    public int obstructionMask;

    public bool shouldAffectStructures;

    public bool shouldAffectTrees;

    public bool shouldAffectObjects;

    public bool shouldAffectBarricades;

    public bool canDealPlayerDamage;

    public bool shouldAffectPlayers;

    public bool shouldAffectZombies;

    public bool shouldAffectAnimals;

    public bool shouldAffectVehicles;

    internal bool LineOfSightTest(Vector3 explosionCenter, Vector3 direction, float distance, out RaycastHit hit)
    {
        if (distance > 0.01f)
        {
            Ray ray = new Ray(explosionCenter, direction);
            float maxDistance = distance - 0.01f;
            return Physics.Raycast(ray, out hit, maxDistance, obstructionMask, QueryTriggerInteraction.Ignore);
        }
        hit = default(RaycastHit);
        return false;
    }
}
