using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

using System.Xml.Linq;

using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ModManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

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
    private List<ModInfo> mods;

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
            SelectedFilePath = files[0].Path.LocalPath;

            if (string.IsNullOrEmpty(SelectedFilePath))
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

        Debug.WriteLine($"Selected file: {SelectedFilePath}");
        ParseLSXFile();
    }

    public void ParseLSXFile()
    {
        // TODO: Implement parsing of LSX files
        string filePath = SelectedFilePath;

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

        Mods = doc.Descendants("node")
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

        TextBoxText = $"{Mods.Count}";

        

        foreach(var mod in Mods)
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
