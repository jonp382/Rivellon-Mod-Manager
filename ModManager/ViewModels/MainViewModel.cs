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
using Avalonia.Media.Imaging;
using System.Text.Json;
using SteamAPI;

namespace ModManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{

    public MainViewModel()
    {
        IOHelper.UserSettings.Load();
        Update();

        // obtain workshop preview images. only ran in constructor to minimize API usage.
        UpdateWorkshopInfo();

        Debug.WriteLine($"Constructor complete");
        OnPropertyChanged();
    }

    public async void UpdateWorkshopInfo()
    {
        
        // only download images for missing mods
        List<Resources.ModInfo> batchIDs = AllMods.Where(
            n => !string.IsNullOrEmpty(n.WorkshopID)
            &&
            !File.Exists(Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "preview-images", $"{n.Folder}.png")
            )).ToList();

        StatusText = "Fetching Steam Workshop details for installed mods...";

        await WebHelper.WebRequest.GetWorkshopDetails(batchIDs);

        ParseWorkshopJson();

        await WebHelper.WebRequest.DownloadAllPreviewImages(batchIDs);

        StatusText = "Downloading missing preview images from Steam Workshop...";

        UpdatePreviewImages();

        StatusText = "Workshop sync complete!";

    }

    public void ParseWorkshopJson()
    {
        var jsonPath = Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "steam_api.json");
        var rawString = File.ReadAllText(jsonPath);

        var json = JsonSerializer.Deserialize<SteamAPIResponse>(rawString);

        foreach(var mod in AllMods)
        {
            if(string.IsNullOrWhiteSpace(mod.WorkshopID)) continue;

            // allow for null results
            mod.WorkshopDetails = json?.Response.PublishedFileDetails.FirstOrDefault(n => string.Equals(n.PublishedFileId, mod.WorkshopID)) ?? null;
        }

    }

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _enabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _disabledMods = [];

    [ObservableProperty]
    private ObservableCollection<Resources.ModInfo> _allMods = [];

    [ObservableProperty]
    private string _currentProfileText = string.Empty;

    [ObservableProperty]
    private Resources.ModInfo? _currentlySelectedMod = null;

    public void ParseModsDirectory()
    {
        StatusText = "Retrieving mods from Mods folder...";

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

        StatusText = $"Retrieving mods from Mods folder... Complete, found {AllMods.Count} mods!";
        
    }

    [RelayCommand]
    public async Task ExtractPakFile()
    {
        string? FilePath = await IOHelper.FileIO.SelectAnyFile("Select the PAK file to extract");
        if(string.IsNullOrEmpty(FilePath))
        {
            await MessageboxHelper.ErrorBox.ErrorMessageBox("No file was selected. Please try again.");
            return;
        }

        
        var outFilePath = 
            Path.Combine(
                IOHelper.CommonPaths.GetBaseFolderPath(),
                "Extracted PAK Files",
                Path.GetFileNameWithoutExtension(FilePath)
            );

        Resources.LSServices.ExtractPAKFile(FilePath, outFilePath);
        await MessageboxHelper.InfoBox.InfoMessageBox($"Successfully exported PAK file to {outFilePath}!", "PAK File Exporter");


    }

    public async void ParseLSXFile()
    {
        EnabledMods.Clear();
        DisabledMods.Clear();

        StatusText = $"Reading mod configuration from DOS2 profile {IOHelper.UserSettings.Default.SelectedProfile}...";

        string filePath = IOHelper.UserSettings.Default.DataFolder + "/PlayerProfiles" + $"/{IOHelper.UserSettings.Default.SelectedProfile}/" + "modsettings.lsx";

        Debug.WriteLine($"Attempting to parse LSX file at {filePath}");

        if(string.IsNullOrEmpty(filePath)) return;

        if(!File.Exists(filePath))
        {
            await MessageboxHelper.ErrorBox.ErrorMessageBox($"The file {filePath} was not found. Please try again.");
            return;    
        }
        

        if(AllMods == null || AllMods.Count <= 0)
        {
            await MessageboxHelper.ErrorBox.ErrorMessageBox($"The modsettings.lsx file requires the Mods folder to be parsed first. Unable to load profile.");
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

            // 1-indexed to match in-game mod menu.
            matchingModInAllMods.LoadOrder = i+1;
            EnabledMods.Add(matchingModInAllMods);
        }

        var allDisabledMods = AllMods.Where(mod => EnabledMods.FirstOrDefault(enabled => enabled.UUID == mod.UUID) == null);
        DisabledMods = new(allDisabledMods.ToList());

        StatusText = $"Reading mod configuration from DOS2 profile {IOHelper.UserSettings.Default.SelectedProfile}... Complete, found {EnabledMods.Count} enabled mods and {DisabledMods.Count} disabled mods!";

        OnPropertyChanged();

    }

    public void UpdateLoadOrders()
    {
        StatusText = $"Updating load order...";
        // don't sort DisabledMods since we don't care about load orders there.
        // set all load orders to -1 which im using as "invalid" or unloaded.
        foreach(var mod in DisabledMods)
        {
            mod.LoadOrder = -1;
        }

        for(int i = 0; i < EnabledMods.Count; i++)
        {
            EnabledMods[i].LoadOrder = i+1;

        }
        EnabledMods = new(EnabledMods.OrderBy(n => n.LoadOrder).ToList());
        
        ValidateLoadOrder();

        StatusText = "Successfully updated and validated load order!";

        
    }

    public void ValidateLoadOrder()
    {
        StatusText = $"Validating load order...";
        // don't sort DisabledMods since we don't care about load orders there.
        
        if(EnabledMods.Count > 0)
        {
            for(int i = 0; i < EnabledMods.Count; i++)
            {
                Resources.ModInfo Mod = EnabledMods[i];
                Mod.IsValid = true;
                Mod.InvalidReason = string.Empty;

                if(Mod.LoadOrder <= 0)
                {
                    Mod.IsValid = false;
                    Mod.InvalidReason = "Load order must be set for all enabled mods. Current Load Order: " + Mod.LoadOrder;
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
                        Mod.InvalidReason = $"A mod with UUID {dep} was expected but not found in the Mods folder.";
                    }
                    else if (matchingMod.LoadOrder <= 0)
                    {
                        Debug.WriteLine($"Load order error");
                        Debug.WriteLine($"Mod {Mod.Name} is invalid.");
                        Mod.IsValid = false;
                        Mod.InvalidReason = $"{matchingMod.Name} should be enabled and placed before this mod, but it is currently DISABLED.";
                    }
                    else if(matchingMod.LoadOrder > Mod.LoadOrder)
                    {
                        Debug.WriteLine($"Load order error");
                        Debug.WriteLine($"Mod {Mod.Name} is invalid.");
                        Mod.IsValid = false;
                        Mod.InvalidReason = $"{matchingMod.Name} should be placed before this mod, but it is currently placed AFTER this mod.";
                    }
                }


            }

        }
    }

    public async void ParseWorkshopFolder()
    {
        var workshopPath = IOHelper.UserSettings.Default.WorkshopFolder;
        if(string.IsNullOrEmpty(workshopPath)) return;

        string[] folders = Directory.GetDirectories(workshopPath);

        foreach(string folder in folders)
        {
            // should have one PAK file per folder
            string[] files = Directory.GetFiles(folder);
            if(files.Length > 1)
            {
                // if it somehow has more than one file, it's an unusual setup and it should be skipped
                // so as to not misidentify a mod.
                Debug.WriteLine($"{folder} has more than one PAK file, skipping.");
                continue;
            }

            string name = Path.GetFileNameWithoutExtension(files[0]);
            Resources.ModInfo? matchingMod = AllMods.FirstOrDefault(n => string.Equals(n.Folder, name, System.StringComparison.OrdinalIgnoreCase));

            if(matchingMod == null) continue;

            matchingMod.WorkshopID = Path.GetFileName(folder);
        }

    }

    // Updates the list of all mods and their load orders after a change to the settings are made.
    public async void Update()
    {

        StatusText = "Updating mods database...";

        AllMods.Clear();
        EnabledMods.Clear();
        DisabledMods.Clear();

        ParseModsDirectory();
        ParseLSXFile();
        ParseWorkshopFolder();
        UpdatePreviewImages(); // this only re-assigns images, it does not download them

        // update UI profile display
        CurrentProfileText = IOHelper.UserSettings.Default.SelectedProfile;

        UpdateLoadOrders();

        StatusText = "Update complete!";

    }

    [RelayCommand]
    public void ExportLSXToProfile()
    {
        Resources.LSServices.WriteProfileLSX(EnabledMods.ToList());
    }

    [RelayCommand]
    public async Task ExportLoadOrderToFile()
    {
        // only export enabled mods, no point to export disabled or all mods.
        // only export UUIDs
        var ExportList = EnabledMods.Select(n => n.UUID);

        // export to modlists subfolder in base directory
        var ExportDirectory  = Path.Combine(IOHelper.CommonPaths.GetBaseFolderPath(), "Mod Orders");
        if(!Directory.Exists(ExportDirectory)) Directory.CreateDirectory(ExportDirectory);

        var file = Path.Combine(ExportDirectory, $"{IOHelper.UserSettings.Default.SelectedProfile}.json");

        var json = JsonSerializer.Serialize(ExportList, new JsonSerializerOptions {WriteIndented=true});
        await IOHelper.FileIO.SaveToFile(json, "Save mod-list to JSON file", ExportDirectory);

        // TODO: check how laughing leader mod manager exports, this should be cross-compatible.

    }

    [RelayCommand]
    public async Task ImportLoadOrderFromFile()
    {
        var file = await IOHelper.FileIO.SelectAnyFile("Please select the mod order file you want to import");
        string? filePath = file?.ToString();

        if(string.IsNullOrWhiteSpace(file))
        {
            await MessageboxHelper.ErrorBox.ErrorMessageBox($"No file was selected. Please try again.");
            return;
        }

        List<Resources.ModInfo> tempList = new();
        var rawText = File.ReadAllText(file);
        List<string>? json;
        
        try { json = JsonSerializer.Deserialize<List<string>>(rawText); }
        catch { json = null; }

        if(json == null)
        {
            await MessageboxHelper.ErrorBox.ErrorMessageBox($"The selected file was of an invalid JSON format, and the mod order could not be imported.");
            return;
        }

        List<string> missingMods = [];
        foreach(string entry in json)
        {
            var matchingMod = AllMods.FirstOrDefault(n => string.Equals(n.UUID, entry));
            if(matchingMod != null)
            {
                tempList.Add(matchingMod);
                continue;
            }

            // mod is missing
            missingMods.Add(entry);

        }

        EnabledMods.Clear();
        DisabledMods.Clear();
        
        EnabledMods = new(tempList);
        DisabledMods = new(AllMods.Where(mod => EnabledMods.FirstOrDefault(enabled => enabled.UUID == mod.UUID) == null));

        UpdateLoadOrders();

        await MessageboxHelper.ErrorBox.ErrorMessageBox($"The following mods were not found among your installed list, and could not be enabled: \n{string.Join("\n", missingMods)}");

    }

    public void UpdatePreviewImages()
    {
        foreach(Resources.ModInfo mod in AllMods)
        {
            var imagePath = Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "preview-images", $"{mod.Folder}.png");
            if(File.Exists(imagePath)) 
            {
                mod.PreviewImage = new Bitmap(imagePath);
                Debug.WriteLine($"Assigned image to mod {mod.Name} from {imagePath}");
            }
        }
    }

}
