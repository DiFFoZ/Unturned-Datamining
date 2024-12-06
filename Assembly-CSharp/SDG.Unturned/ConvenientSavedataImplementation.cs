using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDG.Unturned;

internal class ConvenientSavedataImplementation : IConvenientSavedata
{
    public Dictionary<string, string> Strings = new Dictionary<string, string>();

    public Dictionary<string, DateTime> DateTimes = new Dictionary<string, DateTime>();

    public Dictionary<string, bool> Booleans = new Dictionary<string, bool>();

    public Dictionary<string, long> Integers = new Dictionary<string, long>();

    public HashSet<string> Flags = new HashSet<string>();

    [JsonIgnore]
    public bool isDirty;

    public bool read(string key, out string value)
    {
        return Strings.TryGetValue(key, out value);
    }

    public void write(string key, string value)
    {
        Strings[key] = value;
        isDirty = true;
    }

    public bool read(string key, out DateTime value)
    {
        return DateTimes.TryGetValue(key, out value);
    }

    public void write(string key, DateTime value)
    {
        DateTimes[key] = value;
        isDirty = true;
    }

    public bool read(string key, out bool value)
    {
        return Booleans.TryGetValue(key, out value);
    }

    public void write(string key, bool value)
    {
        Booleans[key] = value;
        isDirty = true;
    }

    public bool read(string key, out long value)
    {
        return Integers.TryGetValue(key, out value);
    }

    public void write(string key, long value)
    {
        Integers[key] = value;
        isDirty = true;
    }

    public bool hasFlag(string flag)
    {
        return Flags.Contains(flag);
    }

    public void setFlag(string flag)
    {
        Flags.Add(flag);
        isDirty = true;
    }

    public bool DeleteBool(string key)
    {
        return Booleans.Remove(key);
    }
}
