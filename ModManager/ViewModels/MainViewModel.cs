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

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _enabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _disabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _allMods = [];

    [ObservableProperty]
    private string _currentProfileText = string.Empty;

    public void ParseModsDirectory()
    {
        string filePath = IOHelper.UserSettings.Default.DataFolder + "/Mods";

        Debug.WriteLine($"Attempting to parse mods directory at {filePath}");

        if(string.IsNullOrEmpty(filePath)) return;

        if(!Directory.Exists(filePath)) return;

        List<ModInfo> Mods = new List<ModInfo>();


        foreach(string file in Directory.GetFiles(filePath))
        {
            var Extension = Path.GetExtension(file);
            if(Extension != ".pak") continue;

            var Name = Path.GetFileNameWithoutExtension(file);

            Debug.WriteLine($"Found PAK file {Name}");

            Package? package = Resources.LSServices.GetPakAsPackage(file);

            if(package == null)
            {
                Debug.WriteLine($"Null package found: {file}");
                continue;
            }

            AllMods.Add(Resources.LSServices.ExtractMetadata(package));
            
        }
        TextBoxText = $"Total Mod Count: {AllMods.Count}";
    }

    [RelayCommand]
    public async Task ExtractPakFile()
    {
        string? FilePath = await IOHelper.OpenFolder.SelectAnyFile("Select the PAK file to extract");
        if(string.IsNullOrEmpty(FilePath)) return;

        // Debug file output to Downloads folder for testing.
        var outFilePath = 
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Test Folder"
            );

        Resources.LSServices.ExtractPAKFile(FilePath, outFilePath);


    }

    public void ParseLSXFile()
    {
        EnabledMods.Clear();
        DisabledMods.Clear();

        string filePath = IOHelper.UserSettings.Default.DataFolder + "/PlayerProfiles" + $"/{IOHelper.UserSettings.Default.SelectedProfile}/" + "modsettings.lsx";

        Debug.WriteLine($"Attempting to parse LSX file at {filePath}");

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

        // CurrentProfileText = $"Current Profile: {IOHelper.SharedPaths.GetCurrentProfle()}";
        OnPropertyChanged();

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
        
        ValidateLoadOrder();

        
    }

    public void ValidateLoadOrder()
    {
        // don't sort DisabledMods since we don't care about load orders there.
        
        if(EnabledMods.Count > 0)
        {
            for(int i = 0; i < EnabledMods.Count; i++)
            {
                Resources.ModInfo Mod = EnabledMods[i];
                Mod.IsValid = true;

                if(Mod.LoadOrder < 0)
                {
                    Mod.IsValid = false;
                }

                foreach(var dep in Mod.Dependencies)
                {
                    if(Resources.FixedModUUIDs.IDs.Contains(dep)) continue;
                    
                    var matchingMod = AllMods.FirstOrDefault(n => string.Equals(n.UUID, dep, System.StringComparison.OrdinalIgnoreCase));
                    if(matchingMod == null) 
                    {
                        Debug.WriteLine($"UUID not found: {dep}");
                        Debug.WriteLine($"Mod {Mod.Name} is invalid.");
                        Mod.IsValid = false;
                    }
                    else if (matchingMod.LoadOrder < 0 || matchingMod.LoadOrder > Mod.LoadOrder)
                    {
                        Debug.WriteLine($"Load order error");
                        Debug.WriteLine($"Mod {Mod.Name} is invalid.");
                        Mod.IsValid = false;
                    }
                }


            }

        }
    }

    // Updates the list of all mods and their load orders after a change to the settings are made.
    public void Update()
    {

        Debug.WriteLine($"Running Update() in MainViewModel");

        AllMods.Clear();
        EnabledMods.Clear();
        DisabledMods.Clear();

        ParseModsDirectory();
        ParseLSXFile();

        UpdateLoadOrders();

    }

}
