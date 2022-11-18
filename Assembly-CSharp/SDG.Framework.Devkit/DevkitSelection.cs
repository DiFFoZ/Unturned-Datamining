using System;
using UnityEngine;

namespace SDG.Framework.Devkit;

public class DevkitSelection : IEquatable<DevkitSelection>
{
    public static DevkitSelection invalid = new DevkitSelection(null, null);

    public GameObject gameObject;

    public Collider collider;

    public Vector3 preTransformPosition;

    public Quaternion preTransformRotation;

    public Matrix4x4 localToWorld;

    public Matrix4x4 relativeToPivot;

    public Transform transform
    {
        get
        {
            if (!(gameObject != null))
            {
                return null;
            }
            return gameObject.transform;
        }
        set
        {
            gameObject = ((value != null) ? value.gameObject : null);
        }
    }

    public bool isValid
    {
        get
        {
            if (gameObject != null)
            {
                return collider != null;
            }
            return false;
        }
    }

    public static bool operator ==(DevkitSelection a, DevkitSelection b)
    {
        return a.gameObject == b.gameObject;
    }

    public static bool operator !=(DevkitSelection a, DevkitSelection b)
    {
        return !(a == b);
    }

    public bool Equals(DevkitSelection other)
    {
        return gameObject == other.gameObject;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }
        DevkitSelection devkitSelection = (DevkitSelection)obj;
        return gameObject == devkitSelection.gameObject;
    }

    public override int GetHashCode()
    {
        if (gameObject == null)
        {
            return -1;
        }
        return gameObject.GetHashCode();
    }

    public DevkitSelection(GameObject newGameObject, Collider newCollider)
    {
        gameObject = newGameObject;
        collider = newCollider;
    }
}
