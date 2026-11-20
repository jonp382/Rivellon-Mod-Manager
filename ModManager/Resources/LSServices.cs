using LSLib.LS;
// LSLib has its own ModInfo class, so make it clear which one is meant
using ModInfo = ModManager.Models.ModInfo;
using ModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace ModManager.Resources;

public class LSServices
{
    public static Package? GetPakAsPackage(string Path)
    {

        if(File.Exists(Path)){
            var reader = new PackageReader();
            Log.Info($"Selected PAK Path: {Path}");
            var package = reader.Read(Path);

            return package;
        }

        return null;
        

    }

    public static ModInfo ExtractMetadata(Package package)
    {

        var MetaFile = package.Files.FirstOrDefault(file => file.Name.EndsWith("meta.lsx", System.StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException("meta.lsx not found in PAK archive");
        using var stream = MetaFile.CreateContentReader();
        
        using var resourceReader = new LSXReader(stream);
        Resource resource = resourceReader.Read();
        
        return ParseResource(resource);
        
    }

    public static void ExtractPAKFile(string Path, string Destination)
    {
        if(!File.Exists(Path)) throw new FileNotFoundException($"No PAK file was found at the path {Path}!");

        Directory.CreateDirectory(Destination);

        var packager = new Packager();
        Log.Info($"Extracting {Path} to {Destination}");

        // errors are left for the caller to handle, so the user is told when extraction fails
        packager.UncompressPackage(Path, Destination);
        
    }

    private static ModInfo ParseResource(Resource resource)
    {
        Node? moduleInfoNode = resource.Regions["Config"]
            .Children["ModuleInfo"].FirstOrDefault() 
            ?? throw new InvalidDataException("Invalid meta.lsx structure. Unable to parse ModuleInfo.");


        string GetAttribute(Node node, string name) =>
            node.Attributes.TryGetValue(name, out var attr) ? attr.Value.ToString()! : string.Empty;

        ModInfo modInfo = new ModInfo
        {
            Name = GetAttribute(moduleInfoNode, "Name"),
            UUID = GetAttribute(moduleInfoNode, "UUID"),
            Folder = GetAttribute(moduleInfoNode, "Folder"),
            Version = GetAttribute(moduleInfoNode, "Version"),
            Author = GetAttribute(moduleInfoNode, "Author"),
            Description = GetAttribute(moduleInfoNode, "Description")
        };

        Log.Info($"Accessing dependencies for {modInfo.Name}");
        
        List<Node> Dependencies;
        try
        {
            Dependencies = resource.Regions["Config"]
                .Children["Dependencies"].FirstOrDefault()?.Children["ModuleShortDesc"] ?? new List<Node>();
            
            Log.Info($"Mod {modInfo.Name} has {Dependencies.Count} dependencies");
        }
        catch
        {
            Log.Info($"No dependencies node found for {modInfo.Name}");
            Dependencies = [];
        }


        if(Dependencies.Count > 0)
        {
            Dependencies.RemoveAll(n => FixedModUUIDs.IDs.Contains(GetAttribute(n, "UUID")));
            foreach(Node dependency in Dependencies)
            {
                modInfo.Dependencies.Add(GetAttribute(dependency, "UUID"));
            }
        }

        return modInfo;


    }

    public static string? GetLSXFromProfile()
    {
        var dataPath = IOHelper.UserSettings.Default.DataFolder;
        var playerProfilesPath = Path.Combine(dataPath, "PlayerProfiles");

        var profilePath = Directory.GetDirectories(playerProfilesPath).FirstOrDefault(n => n.EndsWith(IOHelper.UserSettings.Default.SelectedProfile, StringComparison.OrdinalIgnoreCase));
        if(profilePath == null)
        {
            Log.Warn($"Unable to find ProfilePath for {IOHelper.UserSettings.Default.SelectedProfile} from DataPath");
            return null;
        }

        var files = Directory.GetFiles(profilePath).Where(n => n.EndsWith("modsettings.lsx", StringComparison.OrdinalIgnoreCase)).ToList();
        if(files == null || files.Count == 0)
        {
            Log.Warn($"Unable to find any .lsx files in {profilePath}");
            return null;
        }

        // assume there is only one modsettings.lsx file, which there should be.
        return files[0];
    }

    public static List<ModInfo> AddFakeMods()
    {
        List<ModInfo> AllFakeNodes = [];
        var DivinityOrigins = new ModInfo
        {
            Name = "Divinity: Original Sin 2",
            UUID = "1301db3d-1f54-4e98-9be5-5094030916e4",
            Folder = "DivinityOrigins_1301db3d-1f54-4e98-9be5-5094030916e4",
            Version = "373234071",
            MD5="73d13f95607b70c953cc32e56d62b7d7"
        };

        AllFakeNodes.Add(DivinityOrigins);

        return AllFakeNodes;
    }

    // returns the path that was written to, or null if the profile's modsettings.lsx couldn't be found.
    public static string? WriteProfileLSX(List<ModInfo> EnabledMods)
    {
        var LSXPath = GetLSXFromProfile();
        if(LSXPath == null) return null;

        // keep a copy of the previous file in case the export goes wrong
        File.Copy(LSXPath, LSXPath + ".bak", overwrite: true);

        Log.Info($"Writing to {LSXPath}");

        var resource = new Resource();
        var region = new Region
        {
            RegionName = "ModuleSettings",
            Name="root"
        };
        resource.Regions["ModuleSettings"] = region;

        var modOrderNode = new Node { Name = "ModOrder" };
        var modsNode = new Node{ Name = "Mods" };

        region.Children["ModOrder"] = new List<Node> { modOrderNode };
        region.Children["Mods"] = new List<Node> { modsNode };

        var orderList = new List<Node>();
        var modList = new List<Node>();

        // add all fake mods so the game doesnt freak out
        // this includes "divinity origins" which is just the base game.
        var AllFakeMods = AddFakeMods();
        foreach(var mod in AllFakeMods)
        {
            modList.Add(CreateNodeFromModInfo(mod));
        }

        foreach(var mod in EnabledMods)
        {
            var orderEntry = new Node{ Name = "Module" };
            
            orderEntry.Attributes["UUID"] = new NodeAttribute(LSLib.LS.AttributeType.FixedString)
            {
                Value=mod.UUID
            };

            orderList.Add(orderEntry);


            

            modList.Add(CreateNodeFromModInfo(mod));

        }

        modOrderNode.Children["Children"] = orderList;
        modsNode.Children["Children"] = modList;

        using (var stream = new FileStream(LSXPath, FileMode.Create, FileAccess.Write))
        {
            var writer = new LSXWriter(stream)
            {
                PrettyPrint = true
            };

            writer.Write(resource);
        }

        return LSXPath;
    }

    public static Node CreateNodeFromModInfo(ModInfo mod)
    {
        var modEntry = new Node { Name = "ModuleShortDesc" };
        modEntry.Attributes["Folder"] = new NodeAttribute(LSLib.LS.AttributeType.LSWString)
        {
            Value=mod.Folder,
        };
        modEntry.Attributes["MD5"] = new NodeAttribute(LSLib.LS.AttributeType.LSString)
        {
            Value=mod.MD5 // Usually this is just blank, unless its a default "mod"
        };
        modEntry.Attributes["Name"] = new NodeAttribute(LSLib.LS.AttributeType.FixedString)
        {
            Value=mod.Name
        };
        modEntry.Attributes["UUID"] = new NodeAttribute(LSLib.LS.AttributeType.FixedString)
        {
            Value=mod.UUID
        };
        modEntry.Attributes["Version"] = new NodeAttribute(LSLib.LS.AttributeType.Int)
        {
           // some mods have a blank or non-numeric version, which shouldn't stop the export
           Value=int.TryParse(mod.Version, out int version) ? version : 0
        };

        return modEntry;
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

        Log.Info($"Found {ModOrderNodes.Count} ModOrder nodes in the LSX file");

        Dictionary<string, List<ModInfo>> ReturnDict = [];

        List<ModInfo> ModOrder = [];
        foreach(Node modOrderNode in ModOrderNodes)
        {
            string GetAttribute(string name) =>
                modOrderNode.Attributes.TryGetValue(name, out var attr) ? attr.Value?.ToString()! : string.Empty;

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

        Log.Info($"Found {ModsNodes.Count} Mod nodes in the LSX file");

        List<ModInfo> Mods = [];

        foreach(Node modNode in ModsNodes)
        {
            string GetAttribute(string name) =>
                modNode.Attributes.TryGetValue(name, out var attr) ? attr.Value?.ToString()! : string.Empty;
            

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
