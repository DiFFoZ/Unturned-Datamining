using System;
using System.IO;
using System.Security.Cryptography;

namespace SDG.Unturned;

public class TimeSliceHash<T> : IDisposable where T : HashAlgorithm, new()
{
    private T algo;

    private Stream stream;

    private byte[] buffer;

    private bool disposed;

    public float progress => (float)((double)stream.Position / (double)stream.Length);

    public TimeSliceHash(Stream stream)
    {
        algo = new T();
        algo.Initialize();
        this.stream = stream;
        buffer = new byte[8192];
    }

    public bool advance()
    {
        for (int i = 0; i < 122; i++)
        {
            int num = stream.Read(buffer, 0, buffer.Length);
            if (num > 0)
            {
                algo.TransformBlock(buffer, 0, num, buffer, 0);
                continue;
            }
            algo.TransformFinalBlock(buffer, 0, 0);
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                algo.Dispose();
            }
            disposed = true;
        }
    }

    public byte[] computeHash()
    {
        return algo.Hash;
    }
}
