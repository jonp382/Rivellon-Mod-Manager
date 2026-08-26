using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ModManager.ViewModels;
using ModManager.Resources;

using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.VisualTree;

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

        int targetIndex = GetTargetIndex(targetGrid, e);

        sourceList.Remove(droppedMod);
            
        if(targetIndex < 0 || targetIndex > targetList.Count) 
        {
            targetList.Add(droppedMod);
        }
        else
        {
            targetList.Insert(targetIndex, droppedMod);
        }
        
    }

    private int GetTargetIndex(DataGrid targetGrid, DragEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return -1;

        var dropPoint = e.GetPosition(targetGrid);
        var hitElement = targetGrid.InputHitTest(dropPoint) as Visual;

        var row = hitElement?.FindAncestorOfType<DataGridRow>();

        if(row != null && row.DataContext is ModInfo targetMod)
        {
            var list = targetGrid == EnabledGrid ? vm.EnabledMods : vm.DisabledMods;

            int index = list.IndexOf(targetMod)+1;
            return index != -1 ? index : list.Count;
        }

        return targetGrid == EnabledGrid ? vm.EnabledMods.Count : vm.DisabledMods.Count;


    }
}