using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace MessageboxHelper;

public static class ErrorBox
{
    public static async Task ErrorMessageBox(string text, string title = "Error")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            title: title,
            text: text,
            ButtonEnum.Ok,
            Icon.Error
        );

        await box.ShowWindowAsync();
    }

}

public static class InfoBox
{
    public static async Task InfoMessageBox(string text, string title = "Mod Manager")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            title: title,
            text: text,
            ButtonEnum.Ok,
            Icon.Info
        );

        await box.ShowWindowAsync();
    }
}

public static class PromptBox
{
    public static async Task<ButtonResult> PromptMessagebox(string text, string title = "Mod Manager")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            title: title,
            text: text,
            ButtonEnum.YesNoCancel,
            Icon.Info
        );

        ButtonResult result = await box.ShowWindowAsync();
        return result;
    }
}

public enum OverwriteChoice
{
    Yes,
    YesToAll,
    No,
    NoToAll,
    Cancel
}

public static class OverwritePromptBox
{
    // asks whether an existing file should be overwritten. the "to all" options are only shown
    // when there is more than one conflict, since they're meaningless for a single file.
    public static async Task<OverwriteChoice> PromptOverwrite(string fileName, bool multipleConflicts, string title = "Mod Manager")
    {
        var buttons = new List<ButtonDefinition> { new() { Name = "Yes", IsDefault = true } };
        if (multipleConflicts) buttons.Add(new() { Name = "Yes to All" });
        buttons.Add(new() { Name = "No" });
        if (multipleConflicts) buttons.Add(new() { Name = "No to All" });
        buttons.Add(new() { Name = "Cancel", IsCancel = true });

        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = title,
            ContentMessage = $"A mod named '{fileName}' is already installed. Do you want to overwrite it?",
            ButtonDefinitions = buttons,
            Icon = Icon.Warning,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        string result = await box.ShowWindowAsync();

        return result switch
        {
            "Yes" => OverwriteChoice.Yes,
            "Yes to All" => OverwriteChoice.YesToAll,
            "No" => OverwriteChoice.No,
            "No to All" => OverwriteChoice.NoToAll,
            _ => OverwriteChoice.Cancel // closing the window counts as cancel
        };
    }
}
