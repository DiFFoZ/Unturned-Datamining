using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal class CargoDeclaration
{
    internal List<string> lines = new List<string>();

    public void AppendBool(string key, bool value)
    {
        string text = (value ? "yes" : "no");
        lines.Add("| " + key + " = " + text);
    }

    public void AppendString(string key, string value)
    {
        lines.Add("| " + key + " = " + value);
    }

    public void AppendGuid(string key, Guid guid)
    {
        lines.Add($"| {key} = {guid:N}");
    }

    public void AppendByte(string key, byte value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendUShort(string key, ushort value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendUInt(string key, uint value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendULong(string key, ulong value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendSByte(string key, sbyte value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendShort(string key, short value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendInt(string key, int value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendLong(string key, long value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendFloat(string key, float value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendDouble(string key, double value)
    {
        lines.Add($"| {key} = {value}");
    }

    public void AppendColor32(string key, Color32 value)
    {
        lines.Add("| " + key + " = " + Palette.hex(value));
    }

    public void AppendToString(string key, object value)
    {
        lines.Add($"| {key} = {value}");
    }
}
