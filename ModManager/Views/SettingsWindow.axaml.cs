using System.Diagnostics;
using Avalonia.Controls;

using ModManager.Resources;
using ModManager.ViewModels;

namespace ModManager.Views;

public partial class SettingsWindow : Window
{
    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;
    public SettingsWindow()
    {
        InitializeComponent();

        DataContext = new SettingsViewModel();
    }

    private void ApplyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.SaveUserSettings();
        Close(true);
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private void DataFolderTextbox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = ((TextBox)sender!).Text ?? string.Empty;
        
        var profiles = IOHelper.CommonPaths.GetAllProfiles(text);
        if (profiles == null)
        {
            ViewModel.AllProfiles.Clear();
            Debug.WriteLine($"Null profiles return");
        }
        else
        {
            ViewModel.AllProfiles = new(profiles);
        }

    }
}