using SDG.NetPak;
using UnityEngine;

namespace SDG.Unturned;

[NetInvokableGeneratedClass(typeof(BarricadeDrop))]
public static class BarricadeDrop_NetMethods
{
    private static void ReceiveHealth_DeferredRead(object voidNetObj, in ClientInvocationContext context)
    {
        if (voidNetObj is BarricadeDrop barricadeDrop)
        {
            context.reader.ReadUInt8(out var value);
            barricadeDrop.ReceiveHealth(value);
        }
    }

    [NetInvokableGeneratedMethod("ReceiveHealth", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveHealth_Read(in ClientInvocationContext context)
    {
        NetPakReader reader = context.reader;
        if (reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj == null)
            {
                NetInvocationDeferralRegistry.Defer(value, in context, ReceiveHealth_DeferredRead);
            }
            else if (obj is BarricadeDrop barricadeDrop)
            {
                reader.ReadUInt8(out var value2);
                barricadeDrop.ReceiveHealth(value2);
            }
        }
    }

    [NetInvokableGeneratedMethod("ReceiveHealth", ENetInvokableGeneratedMethodPurpose.Write)]
    public static void ReceiveHealth_Write(NetPakWriter writer, byte hp)
    {
        writer.WriteUInt8(hp);
    }

    private static void ReceiveTransform_DeferredRead(object voidNetObj, in ClientInvocationContext context)
    {
        if (voidNetObj is BarricadeDrop barricadeDrop)
        {
            NetPakReader reader = context.reader;
            reader.ReadUInt8(out var value);
            reader.ReadUInt8(out var value2);
            reader.ReadUInt16(out var value3);
            reader.ReadClampedVector3(out var value4, 13, 11);
            reader.ReadSpecialYawOrQuaternion(out var value5, 23);
            barricadeDrop.ReceiveTransform(in context, value, value2, value3, value4, value5);
        }
    }

    [NetInvokableGeneratedMethod("ReceiveTransform", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveTransform_Read(in ClientInvocationContext context)
    {
        NetPakReader reader = context.reader;
        if (reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj == null)
            {
                NetInvocationDeferralRegistry.Defer(value, in context, ReceiveTransform_DeferredRead);
            }
            else if (obj is BarricadeDrop barricadeDrop)
            {
                reader.ReadUInt8(out var value2);
                reader.ReadUInt8(out var value3);
                reader.ReadUInt16(out var value4);
                reader.ReadClampedVector3(out var value5, 13, 11);
                reader.ReadSpecialYawOrQuaternion(out var value6, 23);
                barricadeDrop.ReceiveTransform(in context, value2, value3, value4, value5, value6);
            }
        }
    }

    [NetInvokableGeneratedMethod("ReceiveTransform", ENetInvokableGeneratedMethodPurpose.Write)]
    public static void ReceiveTransform_Write(NetPakWriter writer, byte old_x, byte old_y, ushort oldPlant, Vector3 point, Quaternion rotation)
    {
        writer.WriteUInt8(old_x);
        writer.WriteUInt8(old_y);
        writer.WriteUInt16(oldPlant);
        writer.WriteClampedVector3(point, 13, 11);
        writer.WriteSpecialYawOrQuaternion(rotation, 23);
    }

    [NetInvokableGeneratedMethod("ReceiveTransformRequest", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveTransformRequest_Read(in ServerInvocationContext context)
    {
        NetPakReader reader = context.reader;
        if (reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj != null && obj is BarricadeDrop barricadeDrop)
            {
                reader.ReadClampedVector3(out var value2, 13, 11);
                reader.ReadSpecialYawOrQuaternion(out var value3, 23);
                barricadeDrop.ReceiveTransformRequest(in context, value2, value3);
            }
        }
    }

    [NetInvokableGeneratedMethod("ReceiveTransformRequest", ENetInvokableGeneratedMethodPurpose.Write)]
    public static void ReceiveTransformRequest_Write(NetPakWriter writer, Vector3 point, Quaternion rotation)
    {
        writer.WriteClampedVector3(point, 13, 11);
        writer.WriteSpecialYawOrQuaternion(rotation, 23);
    }

    private static void ReceiveOwnerAndGroup_DeferredRead(object voidNetObj, in ClientInvocationContext context)
    {
        if (voidNetObj is BarricadeDrop barricadeDrop)
        {
            NetPakReader reader = context.reader;
            reader.ReadUInt64(out var value);
            reader.ReadUInt64(out var value2);
            barricadeDrop.ReceiveOwnerAndGroup(value, value2);
        }
    }

    [NetInvokableGeneratedMethod("ReceiveOwnerAndGroup", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveOwnerAndGroup_Read(in ClientInvocationContext context)
    {
        NetPakReader reader = context.reader;
        if (reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj == null)
            {
                NetInvocationDeferralRegistry.Defer(value, in context, ReceiveOwnerAndGroup_DeferredRead);
            }
            else if (obj is BarricadeDrop barricadeDrop)
            {
                reader.ReadUInt64(out var value2);
                reader.ReadUInt64(out var value3);
                barricadeDrop.ReceiveOwnerAndGroup(value2, value3);
            }
        }
    }

    [NetInvokableGeneratedMethod("ReceiveOwnerAndGroup", ENetInvokableGeneratedMethodPurpose.Write)]
    public static void ReceiveOwnerAndGroup_Write(NetPakWriter writer, ulong newOwner, ulong newGroup)
    {
        writer.WriteUInt64(newOwner);
        writer.WriteUInt64(newGroup);
    }

    private static void ReceiveUpdateState_DeferredRead(object voidNetObj, in ClientInvocationContext context)
    {
        if (voidNetObj is BarricadeDrop barricadeDrop)
        {
            NetPakReader reader = context.reader;
            reader.ReadUInt8(out var value);
            byte[] array = new byte[value];
            reader.ReadBytes(array);
            barricadeDrop.ReceiveUpdateState(array);
        }
    }

    [NetInvokableGeneratedMethod("ReceiveUpdateState", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveUpdateState_Read(in ClientInvocationContext context)
    {
        NetPakReader reader = context.reader;
        if (reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj == null)
            {
                NetInvocationDeferralRegistry.Defer(value, in context, ReceiveUpdateState_DeferredRead);
            }
            else if (obj is BarricadeDrop barricadeDrop)
            {
                reader.ReadUInt8(out var value2);
                byte[] array = new byte[value2];
                reader.ReadBytes(array);
                barricadeDrop.ReceiveUpdateState(array);
            }
        }
    }

    [NetInvokableGeneratedMethod("ReceiveUpdateState", ENetInvokableGeneratedMethodPurpose.Write)]
    public static void ReceiveUpdateState_Write(NetPakWriter writer, byte[] newState)
    {
        byte b = (byte)newState.Length;
        writer.WriteUInt8(b);
        writer.WriteBytes(newState, b);
    }

    [NetInvokableGeneratedMethod("ReceiveSalvageRequest", ENetInvokableGeneratedMethodPurpose.Read)]
    public static void ReceiveSalvageRequest_Read(in ServerInvocationContext context)
    {
        if (context.reader.ReadNetId(out var value))
        {
            object obj = NetIdRegistry.Get(value);
            if (obj != null && obj is BarricadeDrop barricadeDrop)
            {
                barricadeDrop.ReceiveSalvageRequest(in context);
            }
        }
    }
}
