using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

using System.Diagnostics;

namespace ModManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedFilePath = string.Empty;


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

    [RelayCommand]
    public async Task LoadProfileFile() {
        
        var files = await StorageService.GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
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

    }
}
