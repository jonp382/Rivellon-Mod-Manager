namespace IOHelper;

using System;
using System.Diagnostics;
using System.IO;

using System.Text.Json;

public class SharedPaths
{
    public static string GetConfigFolderPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModManager",
            "Config"
            );
    }

    public static string GetConfigFile()
    {
        return Path.Combine(
            GetConfigFolderPath(),
            "config.json"
        );
    }

    public static string GetCurrentProfle()
    {
        // gets the currrent profile from the current LSX
        string CurrentLSXPath = UserSettings.Default.SelectedModLSX;
        if(String.IsNullOrEmpty(CurrentLSXPath))
        {
            return "No profile selected!";
        }

        return Directory.GetParent(CurrentLSXPath).Name;
    }

    public static string AutoFindGameDataFolder()
    {

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "debian-installation", "steamapps", "compatdata", "435150", "pfx", "drive_c", "users", "steamuser", "Documents", "Larian Studios", "Divinity Original Sin 2 Definitive Edition");
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
}

public class UserSettings
{

    private static UserSettings? _instance;
    public static UserSettings Default => _instance ??= Load();


    public string SelectedModFolder {get; set; } = string.Empty;
    public string SelectedModLSX {get; set; } = string.Empty;
    public double WindowWidth {get; set; } = 1200;
    public double WindowHeight {get; set; } = 800;

    public static UserSettings Load()
    {
        var configFilePath = SharedPaths.GetConfigFile();
        if (File.Exists(configFilePath))
        {
            try
            {
                string json = File.ReadAllText(configFilePath);
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
        var directory = SharedPaths.GetConfigFolderPath();
        if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var configFilePath = SharedPaths.GetConfigFile();
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