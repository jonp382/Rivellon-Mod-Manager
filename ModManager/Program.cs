using Avalonia;
using System;

namespace ModManager;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // make sure crashes end up in the log file, so there's something to go on when a user reports one
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Resources.Log.Error("Unhandled exception", e.ExceptionObject as Exception);

        Resources.Log.Info($"Mod Manager starting, logging to {Resources.Log.LogFilePath}");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
