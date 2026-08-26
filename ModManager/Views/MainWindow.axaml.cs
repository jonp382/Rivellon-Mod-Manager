using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ModManager.ViewModels;
using ModManager.Resources;

using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;

namespace ModManager.Views;

public partial class MainWindow : Window
{
    private static ModInfo? _draggedMod;
    private static readonly DataFormat<ModInfo> ModItemFormat = 
        DataFormat.CreateInProcessFormat<ModInfo>("application/x-mod-item");

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();

        DragDrop.AddDragOverHandler(EnabledGrid, OnDragOver);
        DragDrop.AddDropHandler(EnabledGrid, OnDrop);

        DragDrop.AddDragOverHandler(DisabledGrid, OnDragOver);
        DragDrop.AddDropHandler(DisabledGrid, OnDrop);

        
        EnabledGrid.AddHandler(DataGrid.PointerPressedEvent, OnPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble, true);
        DisabledGrid.AddHandler(DataGrid.PointerPressedEvent, OnPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble, true);

    }

    private async void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Debug.WriteLine($"Firing OnPointerPressed");
        if(sender is not DataGrid srcGrid) return;

        if(srcGrid.SelectedItem is not ModInfo selectedMod) return;

        _draggedMod = selectedMod;

        var item = DataTransferItem.Create(ModItemFormat, selectedMod);

        var dragData = new DataTransfer();
        dragData.Add(item);
        
        await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Move);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        Debug.WriteLine($"Firing OnDragOver");
        if (e.DataTransfer.Formats.Contains(ModItemFormat))
        {
            e.DragEffects = DragDropEffects.Move;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine($"Firing OnDrop");
        Debug.WriteLine($"Firing OnPointerPressed");
        if(sender is not DataGrid targetGrid) return;
        if(DataContext is not MainViewModel vm) return;

        if(e.DataTransfer.TryGetValue(ModItemFormat) is not ModInfo droppedMod) return;

        ObservableCollection<ModInfo> targetList = targetGrid == EnabledGrid ? vm.EnabledMods : vm.DisabledMods;
        ObservableCollection<ModInfo> sourceList = targetGrid == EnabledGrid ? vm.DisabledMods : vm.EnabledMods;

        if (sourceList.Contains(droppedMod))
        {
            sourceList.Remove(droppedMod);
            targetList.Add(droppedMod);

            
        }
    }
}