using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ModManager.Models;

public partial class ModInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = String.Empty;
    
    [ObservableProperty]
    private int _loadOrder = -1;

    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string _invalidReason = string.Empty;

    public string UUID { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MD5 {get; set; } = string.Empty; // almost always unused unless its a default fake mod like the base game.
    public string WorkshopID {get; set;} = string.Empty;

    // these are filled in after the mod list is shown (workshop sync runs in the background),
    // so they need to notify the UI when they change.
    [ObservableProperty]
    private PublishedFileDetail? _workshopDetails = null;

    [ObservableProperty]
    private Bitmap? _previewImage = null;

    public List<string> Dependencies {get; set; } = [];
}

public static class FixedModUUIDs
{
    public static readonly System.Collections.Frozen.FrozenSet<string> IDs =
    [
        "1301db3d-1f54-4e98-9be5-5094030916e4", // Divinity Origins - base game
        "eedf7638-36ff-4f26-a50a-076b87d53ba0", // I think this is gift bag 2?
        "b40e443e-badd-4727-82b3-f88a170c4db7" // I think this is gift bag 3?
    ];
}