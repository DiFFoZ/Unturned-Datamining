using System;

namespace SDG.Unturned;

public interface IConvenientSavedata
{
    bool read(string key, out string value);

    void write(string key, string value);

    bool read(string key, out DateTime value);

    void write(string key, DateTime value);

    bool read(string key, out bool value);

    void write(string key, bool value);

    bool read(string key, out long value);

    void write(string key, long value);

    bool hasFlag(string flag);

    void setFlag(string flag);

    /// <returns>true if key existed and was removed.</returns>
    bool DeleteBool(string key);
}
