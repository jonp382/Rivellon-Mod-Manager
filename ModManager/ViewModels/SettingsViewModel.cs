using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSLib.LS.Enums;
using ModManager.Resources;

namespace ModManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _dataDirectory;

    [ObservableProperty]
    private string _currentProfile;

    [ObservableProperty]
    private ObservableCollection<string> _allProfiles;

    [ObservableProperty]
    private bool _enableDarkTheme;

    [ObservableProperty]
    private string _workshopDirectory;

    [ObservableProperty]
    private string _gameDirectory;

    public SettingsViewModel()
    {
        _dataDirectory = IOHelper.UserSettings.Default.DataFolder;
        _workshopDirectory = IOHelper.UserSettings.Default.WorkshopFolder;
        _gameDirectory = IOHelper.UserSettings.Default.GameFolder;

        _allProfiles = new(IOHelper.CommonPaths.GetAllProfiles(_dataDirectory) ?? new List<string>());
        
        _currentProfile = IOHelper.UserSettings.Default.SelectedProfile;
        _enableDarkTheme = IOHelper.UserSettings.Default.EnableDarkTheme;

        
    }

    public void SaveUserSettings()
    {
        IOHelper.UserSettings.Default.DataFolder = DataDirectory;
        IOHelper.UserSettings.Default.WorkshopFolder = WorkshopDirectory;
        IOHelper.UserSettings.Default.GameFolder = GameDirectory;

        IOHelper.UserSettings.Default.SelectedProfile = CurrentProfile;
        IOHelper.UserSettings.Default.EnableDarkTheme = EnableDarkTheme;
    }

    [RelayCommand]
    public async Task SelectDataDirectory()
    {
        var path = await IOHelper.FileIO.SelectModsFolder("Select the Divinity 2 parent folder that contains Mods, PlayerProfiles etc.");

        // only update if a valid path was selected.
        if(path != null)
        {
            DataDirectory = path;
        } 
        
    }    

    [RelayCommand]
    public async Task AutoSelectDataDirectory()
    {
        var path = await IOHelper.CommonPaths.AutoFindGameDataFolder();

        if (!string.IsNullOrEmpty(path))
        {
            DataDirectory = path;
        }
    }

        [RelayCommand]
    public async Task SelectWorkshopDirectory()
    {
        var path = await IOHelper.FileIO.SelectModsFolder("Select the Divinity 2 parent folder that contains Mods, PlayerProfiles etc.");

        // only update if a valid path was selected.
        if(path != null)
        {
            WorkshopDirectory = path;
        } 
        
    }    

    [RelayCommand]
    public async Task AutoSelectWorkshopDirectory()
    {
        var path = await IOHelper.CommonPaths.AutoFindWorkshopFolder();

        if (!string.IsNullOrEmpty(path))
        {
            WorkshopDirectory = path;
        }
    }

    [RelayCommand]
    public async Task SelectGameDirectory()
    {
        var path = await IOHelper.FileIO.SelectAnyFolder("Select the Divinity 2 folder in your Steam directory, typically found at steamapps > common > Divinity Original Sin 2");

        // only update if a valid path was selected.
        if(!string.IsNullOrEmpty(path))
        {
            GameDirectory = path;
        } 
    }

    [RelayCommand]
    public async Task AutoSelectGameDirectory()
    {
        var path = await IOHelper.CommonPaths.AutoFindGameFolder();

        if (!string.IsNullOrEmpty(path))
        {
            GameDirectory = path;
        }
    }

}