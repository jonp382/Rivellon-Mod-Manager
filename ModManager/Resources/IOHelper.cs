namespace IOHelper;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

public class CommonPaths
{
    public static string GetConfigFolderPath()
    {
        // place config file in EXE folder\config
        return Path.Combine(
                AppContext.BaseDirectory,
                "config"
                );
        // return Path.Combine(
        //     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        //     "ModManager",
        //     "Config"
        //     );

            
    }

    public static string GetResourcesFolderPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "resources"
            );
    }

    // where the EXE is stored
    public static string GetBaseFolderPath() { return AppContext.BaseDirectory; }

        

    public static string GetConfigFile()
    {
        return Path.Combine(
            GetConfigFolderPath(),
            "config.json"
        );
    }

    public static List<string>? GetAllProfiles(string folder)
    {
        Debug.WriteLine($"Trying to get all profiles from {folder}");
        if(folder.EndsWith("/") || folder.EndsWith(@"\"))
        {
            folder = folder.Remove(folder.Length-1,1);
        }
        if( // protect against cases where there's a trailing slash
            !folder.EndsWith("Divinity Original Sin 2 Definitive Edition", StringComparison.OrdinalIgnoreCase)
            ) 
        {
            Debug.WriteLine($"Invalid data folder");
            return null;
        }
        try
        {
            var ProfilesPath = Directory.GetDirectories(folder + "/PlayerProfiles");
            if (ProfilesPath == null) 
            {
                Debug.WriteLine($"no profiles found");
                return null;
            }

            var profiles = ProfilesPath.Select(n => Path.GetFileName(n)).ToList();
            profiles.Sort();

            return profiles;
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception occurred when getting all profiles: {ex.Message}");
            return null;
        }
    }

    public async static Task<string> AutoFindGameDataFolder()
    {
        // the game data folder is probably consistent on windows but could vary on linux
        // on windows assume its in MyDocuments
        // on linux ask the user to select their root steam folder first

        if (OperatingSystem.IsLinux())
        {
            var steamPath = await FileIO.SelectAnyFolder("Please select your steamapps folder in your Steam directory.");
            if(string.IsNullOrEmpty(steamPath)) 
            {
                Debug.WriteLine($"No folder selected");
                return string.Empty;
            }

            // once they select Steamapps its guaranteed navigation from there.
            // compatdata, 435150, pfc, drive_c
            // in the case of divinity, its users -> steamuser -> Documents -> Larian Studios -> Divinity Original Sin 2 Definitive Edition
            
            return Path.Combine(steamPath, "compatdata", "435150", "pfx", "drive_c", "users", "steamuser", "Documents", "Larian Studios", "Divinity Original Sin 2 Definitive Edition");
        }
        else if(OperatingSystem.IsWindows()){
            var home = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(home, "Larian Studios", "Divinity Original Sin 2 Definitive Edition");
        }
        else
        {
            Debug.WriteLine($"Invalid operating system!");
            return string.Empty;
        }

    }

    public async static Task<string> AutoFindWorkshopFolder()
    {

        var steamPath = await FileIO.SelectAnyFolder("Please select your steamapps folder in your Steam directory.");
        if(string.IsNullOrEmpty(steamPath)) 
        {
            Debug.WriteLine($"No folder selected");
            return string.Empty;
        }


        return Path.Combine(steamPath, "workshop", "content", "435150");
    }

    public static string GetLSXFromProfile()
    {
        return string.Empty;
    }
}

public static class FileIO
{
    public async static Task<string?> SelectModsFolder(string prompt = "Please make a selection")
    {
        var provider = StorageService.GetStorageProvider();
        if (provider == null) return null;

        var directory = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = prompt,
            AllowMultiple = false,
            
        });

        if (directory.Count > 0) 
        {
            string FilePathMods = directory[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathMods))
            {
                Debug.WriteLine($"Invalid directory path (path was blank). Please try again.");
                return null;
            }

            // IOHelper.UserSettings.Default.SelectedModFolder = FilePathMods;
            return FilePathMods;
        }
        else
        {
            Debug.WriteLine($"No directory selected. Please try again.");
            return null;
        }

        // Debug.WriteLine($"Selected directory: {IOHelper.UserSettings.Default.SelectedModFolder}");
    }

    public async static Task<string?> SelectAnyFile(string prompt = "Please make a selection")
    {
        var provider = StorageService.GetStorageProvider();
        if (provider == null) return null;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = prompt,
            AllowMultiple = false
        });

        if (files.Count > 0) 
        {
            string FilePath = files[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePath))
            {
                Debug.WriteLine($"Invalid file path (path was blank). Please try again.");
                return null;
            }
            // IOHelper.UserSettings.Default.SelectedModLSX = files[0].Path.LocalPath;
            return FilePath;
        }
        else
        {
            Debug.WriteLine($"No file selected. Please try again.");
            return null;
        }
    }

        public async static Task<string?> SelectAnyFolder(string prompt = "Please make a selection")
    {
        var provider = StorageService.GetStorageProvider();
        if (provider == null) return null;

        IStorageFolder? startLocation = await provider.TryGetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.Personal));

        var directory = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = prompt,
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
            
        });

        if (directory.Count > 0) 
        {
            string FilePathMods = directory[0].Path.LocalPath;

            if (string.IsNullOrEmpty(FilePathMods))
            {
                Debug.WriteLine($"Invalid directory path (path was blank). Please try again.");
                return null;
            }

            // IOHelper.UserSettings.Default.SelectedModFolder = FilePathMods;
            return FilePathMods;
        }
        else
        {
            Debug.WriteLine($"No directory selected. Please try again.");
            return null;
        }

        // Debug.WriteLine($"Selected directory: {IOHelper.UserSettings.Default.SelectedModFolder}");
    }
}

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

public class UserSettings
{

    private static UserSettings? _instance;
    public static UserSettings Default => _instance ??= Load();
    public string SelectedProfile {get; set; } = string.Empty;
    public string DataFolder {get; set; } = string.Empty;
    public string WorkshopFolder {get; set; } = string.Empty;
    public double WindowWidth {get; set; } = 1200;
    public double WindowHeight {get; set; } = 800;
    public bool EnableDarkTheme {get; set; } = false;

    public static UserSettings Load()
    {
        var configFilePath = CommonPaths.GetConfigFile();
        if (File.Exists(configFilePath))
        {
            try
            {
                string json = File.ReadAllText(configFilePath);
                Debug.WriteLine($"Read in user-config file from {configFilePath}");
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read and deserialize the config file at {configFilePath}: {ex.Message}");
                return new UserSettings();
            }
        }
        else
        {
            return new UserSettings();
        }
    }

    public void Save()
    {
        var directory = CommonPaths.GetConfigFolderPath();
        if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var configFilePath = CommonPaths.GetConfigFile();
        try
        {
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            Debug.WriteLine($"Saved user settings to {configFilePath}!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save the UserSettings file at {configFilePath}: {ex.Message}");
        }
    }

}