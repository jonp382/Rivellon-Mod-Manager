using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace ModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        string userInput = EnteredText.Text ?? string.Empty;
        Debug.WriteLine($"Button Clicked: {userInput}");
    }
}