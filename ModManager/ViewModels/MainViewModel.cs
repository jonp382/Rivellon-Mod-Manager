using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

using System.Collections.ObjectModel;

using System.Xml.Linq;

using System.IO;

using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using LSLib.LS;

namespace ModManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{

    public MainViewModel()
    {
        IOHelper.UserSettings.Load();
        ParseModsDirectory();
        ParseLSXFile();
        Debug.WriteLine($"Constructor complete");
        OnPropertyChanged();
    }

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
    private ObservableCollection<Resources.ModInfo> _enabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _disabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _allMods = [];

    [ObservableProperty]
    private string _currentProfileText = string.Empty;

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
            string FilePathLSX = files[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathLSX))
            {
                Debug.WriteLine($"Invalid file path (path was blank). Please try again.");
                return;
            }
            IOHelper.UserSettings.Default.SelectedModLSX = files[0].Path.LocalPath;
        }
        else
        {
            Debug.WriteLine($"No file selected. Please try again.");
            return;
        }

        Debug.WriteLine($"Selected file: {IOHelper.UserSettings.Default.SelectedModLSX}");
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
            string FilePathMods = directory[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathMods))
            {
                Debug.WriteLine($"Invalid directory path (path was blank). Please try again.");
                return;
            }

            IOHelper.UserSettings.Default.SelectedModFolder = FilePathMods;
        }
        else
        {
            Debug.WriteLine($"No directory selected. Please try again.");
            return;
        }

        Debug.WriteLine($"Selected directory: {IOHelper.UserSettings.Default.SelectedModFolder}");
        ParseModsDirectory();
    }

    public void ParseModsDirectory()
    {
        string filePath = IOHelper.UserSettings.Default.SelectedModFolder;

        if(string.IsNullOrEmpty(filePath)) return;

        if(!Directory.Exists(filePath)) return;

        List<ModInfo> Mods = new List<ModInfo>();


        foreach(string file in Directory.GetFiles(filePath))
        {
            var Extension = Path.GetExtension(file);
            if(Extension != ".pak") continue;

            var Name = Path.GetFileNameWithoutExtension(file);

            Debug.WriteLine($"Found PAK file {Name}");

            Package? package = Resources.LSServices.ExtractPakFile(file);

            if(package == null)
            {
                Debug.WriteLine($"Null package found: {file}");
                continue;
            }

            AllMods.Add(Resources.LSServices.ExtractMetadata(package));
            
        }
        TextBoxText = $"Total Mod Count: {AllMods.Count}";
    }

    public void ParseLSXFile()
    {
        EnabledMods.Clear();
        DisabledMods.Clear();

        string filePath = IOHelper.UserSettings.Default.SelectedModLSX;

        if(string.IsNullOrEmpty(filePath)) return;

        if(!File.Exists(filePath)) return;

        if(AllMods == null || AllMods.Count <= 0)
        {
            Debug.WriteLine($"Please load the mod folder first!");
            return;
        }

        // if the UUID exists in the modsettings.LSX, add the ModInfo fro
        // AllMods to EnabledMods.
        // after EnabledMods are filled, do a LINQ Where check and add all missing ones
        // to Disabled Mods. or maybe copy it and removeall that are in enabled, not sure
        // which is faster.

        Dictionary<string, List<Resources.ModInfo>> ModSettings = Resources.LSServices.ReadModSettings(filePath);

        List<Resources.ModInfo> Mods = ModSettings["Mods"];
        List<Resources.ModInfo> ModOrder = ModSettings["ModOrder"];

        Debug.WriteLine($"Found {Mods.Count} mods, and {ModOrder.Count} mod order entries in ModSettings.LSX.");

        for(int i = 0; i < ModOrder.Count; i++)
        {
            var mod = ModOrder[i];
            // assume that the list read in the order correctly
            // no sorting that would break the list

            // can't use direct Mods.Contains(mod) because they're different object instances
            var matchingModInOrder = ModOrder.FirstOrDefault(n => n.UUID == mod.UUID);
            if (matchingModInOrder == null)
            {
                Debug.WriteLine($"The mod UUID {mod.UUID} has no matching mod in the Mods section of the modsettings.lsx file! Skipping.");
                continue;
            }


            // by virtue of being in the modsettings.LSX, it's enabled.
            // we know the order from 'i'
            var matchingModInAllMods = AllMods.FirstOrDefault(match => match.UUID == mod.UUID);
            if (matchingModInAllMods == null)
            {
                Debug.WriteLine($"The mod UUID {mod.UUID} has no matching mod in the Mods folder (AllMods)!");
                continue;
            }

            matchingModInAllMods.LoadOrder = i;
            EnabledMods.Add(matchingModInAllMods);
        }

        var allDisabledMods = AllMods.Where(mod => EnabledMods.FirstOrDefault(enabled => enabled.UUID == mod.UUID) == null);
        DisabledMods = new(allDisabledMods.ToList());

        CurrentProfileText = $"Current Profile: {IOHelper.SharedPaths.GetCurrentProfle()}";
        OnPropertyChanged();

        /* Debug file output to Downloads folder for testing enabled vs disabled mods.
        var outFilePath = 
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads",
                "DebugOutput.txt"
            );
            
        if(!File.Exists(outFilePath)) File.Create(outFilePath);
        File.WriteAllText(outFilePath, string.Empty);
        using var fileStream = File.Open(outFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(fileStream);
        
        foreach(var mod in EnabledMods)
        {
            writer.WriteLine($"Enabled mod | {mod.Name} | {mod.UUID}");
        }

        foreach(var mod in DisabledMods)
        {
            writer.WriteLine($"Disabled mod | {mod.Name} | {mod.UUID}");
        }
        */

    }

    public void UpdateLoadOrders()
    {
        // don't sort DisabledMods since we don't care about load orders there.
        // set all load orders to -1 which im using as "invalid" or unloaded.
        foreach(var mod in DisabledMods)
        {
            mod.LoadOrder = -1;
        }

        for(int i = 0; i < EnabledMods.Count; i++)
        {
            EnabledMods[i].LoadOrder = i;

        }
        EnabledMods = new(EnabledMods.OrderBy(n => n.LoadOrder).ToList());

        
    }

}
