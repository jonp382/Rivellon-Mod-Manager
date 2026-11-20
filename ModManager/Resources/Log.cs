using System;
using System.Diagnostics;
using System.IO;

namespace ModManager.Resources;

// writes log messages to logs/modmanager.log next to the EXE, so problems can be diagnosed in
// release builds where Debug.WriteLine output isn't visible. each launch starts a fresh log, and
// the previous one is kept as modmanager.old.log.
public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string _logFile = Path.Combine(_logFolder, "modmanager.log");
    private static bool _initialized = false;

    public static string LogFilePath => _logFile;

    // only shown in the debugger output, never written to the file. for very frequent messages like drag events.
    public static void Debug(string message) => System.Diagnostics.Debug.WriteLine(message);

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex == null ? message : $"{message}{Environment.NewLine}{ex}");

    private static void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(line);

        // logging must never crash the app, and parallel downloads can log at the same time
        lock (_lock)
        {
            try
            {
                if (!_initialized) StartNewLog();
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }

    private static void StartNewLog()
    {
        _initialized = true;
        Directory.CreateDirectory(_logFolder);

        if (File.Exists(_logFile))
        {
            File.Move(_logFile, Path.Combine(_logFolder, "modmanager.old.log"), overwrite: true);
        }
    }
}
