using CommunityToolkit.Mvvm.ComponentModel;
using ModManager.Resources;

namespace ModManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _modDirectory;

    [ObservableProperty]
    private string _profileDirectory;

    public SettingsViewModel()
    {
        _modDirectory = IOHelper.UserSettings.Default.SelectedModFolder;
        _profileDirectory = IOHelper.SharedPaths.GetCurrentProfle();
    }
}