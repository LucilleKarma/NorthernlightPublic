using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Northernlight.Core.Logging;

public static partial class Logger
{
    public enum LogType
    {
        Debug,
        Warning,
        Error
    }

    private static readonly string LogFilePath = Path.Combine(Program.ContentRoot, "ErrorLogs", "main.txt");

    private static readonly Lock logLock = new Lock();

    private static readonly Regex usernameRemovingRegex = UsernameRemovingRegex();

    [ModuleInitializer]
    public static void Load()
    {
        AppDomain.CurrentDomain.UnhandledException += LogException;

        try
        {
            string? directory = Path.GetDirectoryName(LogFilePath);
            if (directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Delete old log files on load.
            if (File.Exists(LogFilePath))
                File.Delete(LogFilePath);

            using (File.Create(LogFilePath)) { }

            Debug("Game started.");
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Logger failed to initialize: {ex}");
        }
    }

    private static void LogException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            // I don't want to see people's deadnames or whatever.
            string sanitizedText = usernameRemovingRegex.Replace(ex.ToString(), "$1[USER]$2");

            Error($"Unhandled Exception caught: {sanitizedText}");
        }
    }

    [Conditional("DEBUG")]
    public static void Debug(string message) => Log(message, LogType.Debug);

    public static void Warning(string message) => Log(message, LogType.Warning);

    public static void Error(string message) => Log(message, LogType.Error);

    /// <summary>
    ///     Logs a new entry to the log file.
    /// </summary>
    private static void Log(string message, LogType type)
    {
        lock (logLock)
        {
            int maxRetries = 4;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using FileStream stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

                    writer.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {type} - {message}");
                    break;
                }
                catch (IOException)
                {
                    if (i >= maxRetries - 1)
                        Console.WriteLine($"Failed to write to log after {maxRetries} attempts.");

                    Thread.Sleep(4);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log due to the following unknown exception: {ex}.");
                    break;
                }
            }
        }
    }

    [GeneratedRegex(@"([\\/]Users[\\/])[^\\/]+([\\/])", RegexOptions.IgnoreCase)]
    private static partial Regex UsernameRemovingRegex();
}
