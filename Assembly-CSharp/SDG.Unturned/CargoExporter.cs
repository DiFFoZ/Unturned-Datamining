using System.Collections.Generic;
using System.IO;
using Unturned.SystemEx;

namespace SDG.Unturned;

/// <summary>
/// Helper for wiki writers to dump game data into a useful format.
/// </summary>
public static class CargoExporter
{
    public static void Export()
    {
        string text = Path.Join(ReadWrite.PATH, "Extras", "WikiCargoData");
        ReadWrite.createFolder(text, usePath: false);
        CargoBuilder cargoBuilder = new CargoBuilder();
        foreach (AssetOrigin assetOrigin in Assets.assetOrigins)
        {
            if (assetOrigin.assets.IsEmpty())
            {
                continue;
            }
            string text2 = PathEx.ReplaceInvalidFileNameChars(assetOrigin.name, '_');
            if (string.IsNullOrEmpty(text2))
            {
                UnturnedLog.error("Unable to export origin " + assetOrigin.name + " Asset IDs because file name would be empty");
                continue;
            }
            string text3 = Path.Join(text, text2);
            ReadWrite.createFolder(text3, usePath: false);
            foreach (Asset asset in assetOrigin.assets)
            {
                cargoBuilder.Clear();
                asset.BuildCargoData(cargoBuilder);
                if (cargoBuilder.declarations.Count < 1)
                {
                    continue;
                }
                string path = PathEx.ReplaceInvalidFileNameChars(asset.GetTypeFriendlyName(), '_');
                string text4 = Path.Combine(text3, path);
                Directory.CreateDirectory(text4);
                using FileStream stream = new FileStream(Path.Join(path2: PathEx.ReplaceInvalidFileNameChars(asset.name + " (" + asset.FriendlyName + ")", '_') + ".txt", path1: text4), FileMode.Create, FileAccess.Write);
                using StreamWriter streamWriter = new StreamWriter(stream);
                foreach (KeyValuePair<string, List<CargoDeclaration>> declaration in cargoBuilder.declarations)
                {
                    foreach (CargoDeclaration item in declaration.Value)
                    {
                        streamWriter.Write("{{Cargo/");
                        streamWriter.WriteLine(declaration.Key);
                        foreach (string line in item.lines)
                        {
                            streamWriter.WriteLine(line);
                        }
                        streamWriter.WriteLine("}}");
                        streamWriter.WriteLine();
                    }
                }
            }
        }
    }
}
