namespace SDG.Unturned;

internal class MasterBundleHash
{
    public byte[] windowsHash;

    public byte[] macHash;

    public byte[] linuxHash;

    public byte[] GetPlatformHash(EClientPlatform clientPlatform)
    {
        return clientPlatform switch
        {
            EClientPlatform.Windows => windowsHash, 
            EClientPlatform.Mac => macHash, 
            EClientPlatform.Linux => linuxHash, 
            _ => null, 
        };
    }

    public bool DoesAnyHashMatch(byte[] hash)
    {
        if (windowsHash == null || macHash == null || linuxHash == null)
        {
            return true;
        }
        if (!Hash.verifyHash(hash, windowsHash) && !Hash.verifyHash(hash, macHash))
        {
            return Hash.verifyHash(hash, linuxHash);
        }
        return true;
    }

    public bool DoesPlatformHashMatch(byte[] hash, EClientPlatform clientPlatform)
    {
        byte[] platformHash = GetPlatformHash(clientPlatform);
        if (platformHash == null)
        {
            return true;
        }
        return Hash.verifyHash(hash, platformHash);
    }
}
