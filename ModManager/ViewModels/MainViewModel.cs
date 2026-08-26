using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

using System.Collections.ObjectModel;

using System.Xml.Linq;

using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using LSLib.LS;

namespace ModManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _filePathLSX = string.Empty;

    [ObservableProperty]
    private string _filePathMods = string.Empty;

    [ObservableProperty]
    private string _textBoxText = string.Empty;


    public static class StorageService
    {
        public static IStorageProvider? GetStorageProvider()
        {
            // Check if the app is running on a standard Desktop (Windows/macOS/Linux)
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Return the main window's storage provider
                return desktop.MainWindow?.StorageProvider;
            }

            return null;
        }
    }

    [ObservableProperty]
    private ObservableCollection<ModInfo> _loadedMods = [];

    [ObservableProperty]
    private ObservableCollection<ModInfo> _allMods = [];

    [RelayCommand]
    public async Task LoadProfileFile() {
        

        var provider = StorageService.GetStorageProvider();
        if (provider == null) return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a ModSettings.LSX file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("LSX Files") {Patterns = new[] {"*.lsx"}}
            }
        });

        if (files.Count > 0) 
        {
            FilePathLSX = files[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathLSX))
            {
                Debug.WriteLine($"Invalid file path (path was blank). Please try again.");
                return;
            }
        }
        else
        {
            Debug.WriteLine($"No file selected. Please try again.");
            return;
        }

        Debug.WriteLine($"Selected file: {FilePathLSX}");
        ParseLSXFile();
    }

    [RelayCommand]
    public async Task LoadModsDirectory()
    {
        var provider = StorageService.GetStorageProvider();
        if (provider == null) return;

        var directory = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the Mods folder",
            AllowMultiple = false,
            
        });

        if (directory.Count > 0) 
        {
            FilePathMods = directory[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathMods))
            {
                Debug.WriteLine($"Invalid directory path (path was blank). Please try again.");
                return;
            }
        }
        else
        {
            Debug.WriteLine($"No directory selected. Please try again.");
            return;
        }

        Debug.WriteLine($"Selected directory: {FilePathMods}");
        ParseModsDirectory();
    }

    public void ParseModsDirectory()
    {
        string filePath = FilePathMods;

        if(string.IsNullOrEmpty(filePath)) return;

        if(!System.IO.Directory.Exists(filePath)) return;

        List<ModInfo> Mods = new List<ModInfo>();


        foreach(string file in System.IO.Directory.GetFiles(filePath))
        {
            var Extension = System.IO.Path.GetExtension(file);
            if(Extension != ".pak") continue;

            var Name = System.IO.Path.GetFileNameWithoutExtension(file);

            Debug.WriteLine($"Found PAK file {Name}");

            ModInfo mod = new()
            {
                Folder = Name
            };
            Mods.Add(mod);
        }
        TextBoxText = $"Total Mod Count: {AllMods.Count}";

        AllMods = new(Mods);
    }

    [RelayCommand]
    public async Task OpenPakFile()
    {
        var provider = StorageService.GetStorageProvider();
        if (provider == null) return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a ModSettings.LSX file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PAK Files") {Patterns = new[] {"*.pak"}}
            }
        });
        string PakFilePath;
        if (files.Count > 0) 
        {
            PakFilePath = files[0].Path.LocalPath;

            if (string.IsNullOrEmpty(PakFilePath))
            {
                Debug.WriteLine($"Invalid file path (path was blank). Please try again.");
                return;
            }
        }
        else
        {
            Debug.WriteLine($"No file selected. Please try again.");
            return;
        }

        Debug.WriteLine($"Selected file: {PakFilePath}");
        Resources.LSServices.ExtractPakFile(PakFilePath);
    }

    public void ParseLSXFile()
    {
        // TODO: Implement parsing of LSX files
        string filePath = FilePathLSX;

        if(string.IsNullOrEmpty(filePath)) return;

        if(!System.IO.File.Exists(filePath)) return;

        // Open the file and read its contents
        XDocument doc = XDocument.Load(filePath);

        var ActiveUUIDs = doc.Descendants("node")
            .Where(node => (string)node.Attribute("id") == "Module")
            .Select(node => new ModInfo
            {
                UUID = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "UUID")
                    ?.Attribute("value")?.Value,
            }).ToHashSet().Select(node => node.UUID).ToHashSet();

        var Mods = doc.Descendants("node")
            .Where(node => (string)node.Attribute("id") == "ModuleShortDesc")
            .Select(node => new ModInfo
            {
                Name = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "Name")
                    ?.Attribute("value")?.Value,
                MD5 = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "MD5")
                    ?.Attribute("value")?.Value,
                Folder = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "Folder")
                    ?.Attribute("value")?.Value,
                UUID = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "UUID")
                    ?.Attribute("value")?.Value,
                Version = node.Elements("attribute")
                    .FirstOrDefault(attribute => (string)attribute.Attribute("id") == "Version")
                    ?.Attribute("value")?.Value,
            }).ToList();

        Mods.ForEach(mod => mod.Enabled = ActiveUUIDs.Contains(mod.UUID));

        LoadedMods = new(Mods);

        TextBoxText = $"{LoadedMods.Count}";

        

        foreach(var mod in LoadedMods)
        {
            Debug.WriteLine($"[Mod] {mod.Name} | {mod.Folder} | {mod.MD5} | {mod.UUID} | {mod.Version}");
        }

        foreach(var uuid in ActiveUUIDs)
        {
            Debug.WriteLine($"[Mod] {uuid}");
        }


    }

    public class ModInfo
    {
        public string Folder { get; set; } = "";
        public string MD5 { get; set; } = "";
        public string Name { get; set; } = "";
        public string UUID { get; set; } = "";
        public string Version { get; set; } = "";

        public bool Enabled { get; set; } = false;

    }
}
