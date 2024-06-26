using System;
using UnityEngine;

namespace SDG.Unturned;

public class StructureData
{
    private Structure _structure;

    public Vector3 point;

    public Quaternion rotation;

    [Obsolete("Replaced by rotation quaternion, but you should probably not be accessing either of these directly.")]
    public byte angle_x;

    [Obsolete("Replaced by rotation quaternion, but you should probably not be accessing either of these directly.")]
    public byte angle_y;

    [Obsolete("Replaced by rotation quaternion, but you should probably not be accessing either of these directly.")]
    public byte angle_z;

    public ulong owner;

    public ulong group;

    public uint objActiveDate;

    public Structure structure => _structure;

    public uint instanceID { get; private set; }

    public StructureData(Structure newStructure, Vector3 newPoint, Quaternion newRotation, ulong newOwner, ulong newGroup, uint newObjActiveDate, uint newInstanceID)
    {
        _structure = newStructure;
        point = newPoint;
        rotation = newRotation;
        owner = newOwner;
        group = newGroup;
        objActiveDate = newObjActiveDate;
        instanceID = newInstanceID;
    }

    [Obsolete]
    public StructureData(Structure newStructure, Vector3 newPoint, byte newAngle_X, byte newAngle_Y, byte newAngle_Z, ulong newOwner, ulong newGroup, uint newObjActiveDate, uint newInstanceID)
    {
        _structure = newStructure;
        point = newPoint;
        rotation = Quaternion.Euler((float)(int)newAngle_X * 2f, (float)(int)newAngle_Y * 2f, (float)(int)newAngle_Z * 2f);
        angle_x = newAngle_X;
        angle_y = newAngle_Y;
        angle_z = newAngle_Z;
        owner = newOwner;
        group = newGroup;
        objActiveDate = newObjActiveDate;
        instanceID = newInstanceID;
    }
}
