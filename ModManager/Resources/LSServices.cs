using CommunityToolkit.HighPerformance.Helpers;
using LSLib.LS;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    public static ModInfo ExtractMetadata(Package package)
    {

        // foreach(var file in package.Files)
        // {
        //     Debug.WriteLine($"Found file {file.Name}");
        // }

        var MetaFile = package.Files.FirstOrDefault(file => file.Name.EndsWith("meta.lsx", System.StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException("meta.lsx not found in PAK archive");
        using var stream = MetaFile.CreateContentReader();
        
        using var resourceReader = new LSXReader(stream);
        Resource resource = resourceReader.Read();
        
        // Debug.WriteLine($"Parsing resource");
        return ParseResource(resource);
        
    }

    private static ModInfo ParseResource(Resource resource)
    {
        Node moduleInfoNode = resource.Regions["Config"]
            .Children["ModuleInfo"].FirstOrDefault();

        if (moduleInfoNode == null)
        {
            throw new InvalidDataException("Invalid meta.lsx structure. Unable to parse ModuleInfo.");
        }

        string GetAttribute(string name) =>
            moduleInfoNode.Attributes.TryGetValue(name, out var attr) ? attr.Value?.ToString() : string.Empty;

        return new ModInfo
        {
            Name = GetAttribute("Name"),
            UUID = GetAttribute("UUID"),
            Folder = GetAttribute("Folder"),
            Version = GetAttribute("Version"),
            Author = GetAttribute("Author"),
            Description = GetAttribute("Description")
        };


    }

     public static Dictionary<string, List<ModInfo>> ReadModSettings(string path)
    {
        // assuming the path is already verified to exist
        using var stream = File.OpenRead(path);
        using var reader = new LSXReader(stream);

        Resource resource = reader.Read();
        return ParseModSettings(resource);
    }

    private static Dictionary<string, List<ModInfo>> ParseModSettings(Resource resource)
    {

        // Mod order part of LSX file
        Node ModOrderContainer = resource.Regions["ModuleSettings"]
            .Children["ModOrder"].FirstOrDefault()
            ?? throw new InvalidDataException("Invalid meta.lsx structure. Unable to parse ModuleInfo.");

        List<Node> ModOrderNodes = ModOrderContainer.Children.TryGetValue("Module", out var modOrderModules)
            ? modOrderModules
            : new List<Node>();

        Debug.WriteLine($"Found {ModOrderNodes.Count} ModOrder nodes in the LSX file");

        Dictionary<string, List<ModInfo>> ReturnDict = [];

        List<ModInfo> ModOrder = [];
        foreach(Node modOrderNode in ModOrderNodes)
        {
            string GetAttribute(string name) =>
                modOrderNode.Attributes.TryGetValue(name, out var attr) ? attr.Value?.ToString() : string.Empty;

            ModInfo mod = new ModInfo
            {
                UUID = GetAttribute("UUID")
            };

            ModOrder.Add(mod);
            
        }


        // Mods metadata part of LSX file
        // Mod order part of LSX file
        Node ModsContainer = resource.Regions["ModuleSettings"]
            .Children["Mods"].FirstOrDefault()
            ?? throw new InvalidDataException("Invalid meta.lsx structure. Unable to parse ModuleInfo.");

        List<Node> ModsNodes = ModsContainer.Children.TryGetValue("ModuleShortDesc", out var modModules)
            ? modModules
            : new List<Node>();

        Debug.WriteLine($"Found {ModsNodes.Count} Mod nodes in the LSX file");

        List<ModInfo> Mods = [];

        foreach(Node modNode in ModsNodes)
        {
            string GetAttribute(string name) =>
                modNode.Attributes.TryGetValue(name, out var attr) ? attr.Value?.ToString() : string.Empty;
            

            ModInfo mod = new ModInfo
            {
                Name = GetAttribute("Name"),
                UUID = GetAttribute("UUID"),
                Folder = GetAttribute("Folder"),
                Version = GetAttribute("Version")
            };
            Mods.Add(mod);
        }

        ReturnDict.Add("Mods", Mods);
        ReturnDict.Add("ModOrder", ModOrder);

        return ReturnDict;

        
    }
}

public class ModInfo
{
    public string Name { get; set; }
    public int LoadOrder {get; set; } = -1;
    public string UUID { get; set; }
    public string Folder { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
}