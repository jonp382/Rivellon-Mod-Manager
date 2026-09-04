using System.Threading.Tasks;
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