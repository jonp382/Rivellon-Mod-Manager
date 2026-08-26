using LSLib.LS;
using System.Diagnostics;
using System.IO;
namespace ModManager.Resources;

public class LSServices
{
    public static Package? ExtractPakFile(string Path)
    {

        if(File.Exists(Path)){
            using var stream = File.OpenRead(Path);
            var reader = new PackageReader();
            Debug.WriteLine($"Selected PAK Path: {Path}");
            var package = reader.Read(Path);

            return package;            
        }

        return null;
        

    }
}