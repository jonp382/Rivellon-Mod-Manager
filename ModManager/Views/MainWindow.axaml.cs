using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ModManager.ViewModels;
using ModManager.Resources;

using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia;

namespace ModManager.Views;

public partial class MainWindow : Window
{
    private Point? _pressPosition;
    private static ModInfo? _draggedMod;
    private PointerPressedEventArgs? _pressedEvent;
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

        
        EnabledGrid.AddHandler(DataGrid.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        EnabledGrid.AddHandler(DataGrid.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        EnabledGrid.AddHandler(DataGrid.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

        DisabledGrid.AddHandler(DataGrid.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        DisabledGrid.AddHandler(DataGrid.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        DisabledGrid.AddHandler(DataGrid.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

    }

    private async void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Debug.WriteLine($"Firing OnPointerPressed");
        if(sender is not DataGrid srcGrid) return;

        if(srcGrid.SelectedItem is not ModInfo selectedMod) return;

        _draggedMod = selectedMod;
        _pressPosition = e.GetPosition(srcGrid);
        _pressedEvent = e;

        var item = DataTransferItem.Create(ModItemFormat, selectedMod);

        var dragData = new DataTransfer();
        dragData.Add(item);

    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Clear state if the mouse is released without dragging
        _pressPosition = null;
        _draggedMod = null;
        _pressedEvent = null;
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressPosition == null || _draggedMod == null) return;
        if (sender is not DataGrid srcGrid) return;

        var currentPos = e.GetPosition(srcGrid);
        var diff = currentPos - _pressPosition.Value;

        // Require a small movement threshold (e.g., 5 pixels) before starting drag
        if (System.Math.Abs(diff.X) > 5 || System.Math.Abs(diff.Y) > 5)
        {
            var modToDrag = _draggedMod;
            var pressEvent = _pressedEvent;
            
            // Clear tracking variables so it doesn't re-trigger
            _pressPosition = null;
            _draggedMod = null;
            _pressedEvent = null;

            var item = DataTransferItem.Create(ModItemFormat, modToDrag);
            var dragData = new DataTransfer();
            dragData.Add(item);

            await DragDrop.DoDragDropAsync(pressEvent, dragData, DragDropEffects.Move);
        }
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