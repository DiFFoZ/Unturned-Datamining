namespace SDG.Unturned;

public class ServerSavedata
{
    public static string directoryName
    {
        get
        {
            if (Dedicator.IsDedicatedServer)
            {
                return "Servers";
            }
            return "Worlds";
        }
    }

    public static string directory
    {
        get
        {
            if (Dedicator.IsDedicatedServer)
            {
                return "/Servers";
            }
            return "/Worlds";
        }
    }

    public static string transformPath(string path)
    {
        return directory + "/" + Provider.serverID + path;
    }

    public static void serializeJSON<T>(string path, T instance)
    {
        ReadWrite.serializeJSON(directory + "/" + Provider.serverID + path, useCloud: false, instance);
    }

    public static T deserializeJSON<T>(string path)
    {
        return ReadWrite.deserializeJSON<T>(directory + "/" + Provider.serverID + path, useCloud: false);
    }

    public static void populateJSON(string path, object target)
    {
        ReadWrite.populateJSON(directory + "/" + Provider.serverID + path, target);
    }

    public static void writeData(string path, Data data)
    {
        ReadWrite.writeData(directory + "/" + Provider.serverID + path, useCloud: false, data);
    }

    public static Data readData(string path)
    {
        return ReadWrite.readData(directory + "/" + Provider.serverID + path, useCloud: false);
    }

    public static void writeBlock(string path, Block block)
    {
        ReadWrite.writeBlock(directory + "/" + Provider.serverID + path, useCloud: false, block);
    }

    public static Block readBlock(string path, byte prefix)
    {
        return ReadWrite.readBlock(directory + "/" + Provider.serverID + path, useCloud: false, prefix);
    }

    public static River openRiver(string path, bool isReading)
    {
        return new River(directory + "/" + Provider.serverID + path, usePath: true, useCloud: false, isReading);
    }

    public static void deleteFile(string path)
    {
        ReadWrite.deleteFile(directory + "/" + Provider.serverID + path, useCloud: false);
    }

    public static bool fileExists(string path)
    {
        return ReadWrite.fileExists(directory + "/" + Provider.serverID + path, useCloud: false);
    }

    public static void createFolder(string path)
    {
        ReadWrite.createFolder(directory + "/" + Provider.serverID + path);
    }

    public static void deleteFolder(string path)
    {
        ReadWrite.deleteFolder(directory + "/" + Provider.serverID + path);
    }

    public static bool folderExists(string path)
    {
        return ReadWrite.folderExists(directory + "/" + Provider.serverID + path);
    }
}
