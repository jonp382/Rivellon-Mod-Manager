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
using System;
using System.Collections.Generic;

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

        this.Closing += MainWindowClosing;

        Width = IOHelper.UserSettings.Default.WindowWidth;
        Height = IOHelper.UserSettings.Default.WindowHeight;

        Application.Current.RequestedThemeVariant = IOHelper.UserSettings.Default.EnableDarkTheme
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;

    }

    private async void OnOpenSettingsClicked(object? sender, RoutedEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return;
        
        var settingsWindow = new SettingsWindow();
        var dialogResult = await settingsWindow.ShowDialog<bool>(this); 
        
        // don't update anything if the user cancelled the settings window (also includes hitting [X])
        if (dialogResult) 
        {
            vm.Update();

            Application.Current.RequestedThemeVariant = IOHelper.UserSettings.Default.EnableDarkTheme
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
            
                
        }
    }

    private void MainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // wrap in try-catch so an error can never block the closing process
        try
        {
            IOHelper.UserSettings.Default.WindowWidth = Width;
            IOHelper.UserSettings.Default.WindowHeight = Height;
            
            IOHelper.UserSettings.Default.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred in MainWindowClosing: {ex.Message}");
        }
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
        GridDropIndicator.IsVisible = false;
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
            GridDropIndicator.IsVisible = false;
            return;
        }

        if(sender is not DataGrid targetGrid) return;

        var indicator = GridDropIndicator;
        var dropPoint = e.GetPosition(targetGrid);
        var hitElement = targetGrid.InputHitTest(dropPoint) as Visual;
        var row = hitElement?.FindAncestorOfType<DataGridRow>();

        if(row != null)
        {
            var rowTopLeft = row.TranslatePoint(new Point(0,0), MainGrid);

            if (rowTopLeft.HasValue)
            {
                double xPosition = rowTopLeft.Value.X;
                double yPosition = rowTopLeft.Value.Y + row.Bounds.Height;
                
                indicator.Margin = new Thickness(xPosition, yPosition, 0, 0);
                indicator.Width = targetGrid.Bounds.Width;
                indicator.IsVisible = true;

            }
        }
        else
        {
            // indicator.IsVisible = false;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine($"Firing OnDrop");
        GridDropIndicator.IsVisible = false;
        if(sender is not DataGrid targetGrid) return;
        if(DataContext is not MainViewModel vm) return;

        if(e.DataTransfer.TryGetValue(ModItemFormat) is not ModInfo droppedMod) return;

        ObservableCollection<ModInfo> targetList = targetGrid == EnabledGrid ? vm.EnabledMods : vm.DisabledMods;
        
        ObservableCollection<ModInfo> sourceList = null;
        if (vm.EnabledMods.Contains(droppedMod))
        {
            sourceList = vm.EnabledMods;
        }
        else
        {
            sourceList = vm.DisabledMods;
        }

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
        
        vm.UpdateLoadOrders();
    }

    public void EnableSelectedItem(object? sender, RoutedEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return;

        var sourceList = vm.DisabledMods;
        var targetList = vm.EnabledMods;

        var sourceGrid = DisabledGrid;

        ModInfo? targetMod = (ModInfo)sourceGrid.SelectedItem;
        if(targetMod == null) return;

        MoveItem([targetMod], sourceList, targetList);
    }

    public void DisableSelectedItem(object? sender, RoutedEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return;

        var sourceList = vm.EnabledMods;
        var targetList = vm.DisabledMods;

        var sourceGrid = EnabledGrid;

        ModInfo targetMod = (ModInfo)sourceGrid.SelectedItem;
        if(targetMod == null) return;

        MoveItem([targetMod], sourceList, targetList);
    }

    public void EnableAllItems(object? sender, RoutedEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return;

        var sourceList = vm.DisabledMods;
        var targetList = vm.EnabledMods;

        MoveItem(sourceList.ToList(), sourceList, targetList);

    }

    public void DisableAllItems(object? sender, RoutedEventArgs e)
    {
        if(DataContext is not MainViewModel vm) return;

        var sourceList = vm.EnabledMods;
        var targetList = vm.DisabledMods;

        MoveItem(sourceList.ToList(), sourceList, targetList);
    }

    public void MoveItem(List<ModInfo> ModsToMove, ObservableCollection<ModInfo> SourceList, ObservableCollection<ModInfo> TargetList)
    {
        if(DataContext is not MainViewModel vm) return;

        foreach(ModInfo modToRemove in ModsToMove)
        {
            if(modToRemove == null) continue; // skip null mods
            SourceList.Remove(modToRemove); // remove mod from source list
            TargetList.Add(modToRemove); // add mod to target list

        }

        vm.UpdateLoadOrders();
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

    private void Grid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        DataGrid grid = (DataGrid)sender!;
        

        ModInfo? selected = (ModInfo?)grid.SelectedItem;
        if(selected != null)
        {
            vm.CurrentlySelectedMod = (ModInfo)grid.SelectedItem;
        }
    }
}