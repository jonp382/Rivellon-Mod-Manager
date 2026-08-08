using Avalonia.Controls;
using Avalonia.Interactivity;
using ModManager.ViewModels;
using System.Diagnostics;

namespace ModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }

    
}