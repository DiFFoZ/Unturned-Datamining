using System;
using UnityEngine;

namespace SDG.Unturned;

public class InputInfo
{
    public ERaycastInfoType type;

    public ERaycastInfoUsage usage;

    public Vector3 point;

    public Vector3 direction;

    public Vector3 normal;

    public Player player;

    public Zombie zombie;

    public Animal animal;

    public ELimb limb;

    public string materialName;

    [Obsolete("Replaced by materialName")]
    public EPhysicsMaterial material;

    public InteractableVehicle vehicle;

    /// <summary>
    /// Root transform.
    /// </summary>
    public Transform transform;

    /// <summary>
    /// Hit collider's transform. Can be null.
    /// </summary>
    public Transform colliderTransform;

    public byte section;
}
