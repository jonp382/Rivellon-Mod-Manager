using Avalonia.Controls;

using ModManager.Resources;
using ModManager.ViewModels;

namespace ModManager.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        DataContext = new SettingsViewModel();
    }
}