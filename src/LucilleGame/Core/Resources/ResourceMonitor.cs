using System.Diagnostics;

namespace Northernlight.Core.Resources;

/// <summary>
///     A central manager responsible for the processing of
///     all reloadable resources if they receive outside file modifications.
/// </summary>
internal static class ResourceMonitor
{
    private const NotifyFilters WatcherFilters =
        NotifyFilters.Attributes | NotifyFilters.CreationTime |
        NotifyFilters.FileName | NotifyFilters.LastAccess |
        NotifyFilters.LastWrite | NotifyFilters.Size |
        NotifyFilters.Security;

    /// <summary>
    ///     The database used by this monitor for storing
    ///     references to reloadable resources.
    /// </summary>
    internal static ResourceMonitorDatabase Database = new ResourceMonitorDatabase();

    private static readonly Dictionary<string, FileSystemWatcher> watchersByDirectory = [];

    /// <summary>
    ///     The upper assets directory.
    /// </summary>
    internal static readonly string[] AssetsDirectories = CreateAssetDirectories();

    /// <summary>
    ///     The file format to subdirectory mappings to use
    ///     when copying over assets.
    /// </summary>
    internal static readonly Dictionary<string, string> FileFormatDirectoryMappings = new Dictionary<string, string>()
    {
        [".png"] = Path.Combine(Program.ContentRoot, "Assets", "Textures"),
        [".wav"] = Path.Combine(Program.ContentRoot, "Assets", "Sounds"),
        [".mid"] = Path.Combine("mus"),
        [".ttf"] = Path.Combine(Program.ContentRoot, "Assets", "Fonts"),
        [".json"] = Path.Combine(Program.ContentRoot, "Data"),
        [".hlsl"] = Path.Combine(Program.ContentRoot, "Shaders"),
        [".spv"] = Path.Combine(Program.ContentRoot, "Shaders"),
    };

    private static string[] CreateAssetDirectories()
    {
        string rootA = Path.GetFullPath(@"..\..\..\").TrimEnd('\\');
        string rootB = Path.GetFullPath(@"..\..\..\..\").TrimEnd('\\');
        string rootC = Path.GetFullPath(@"..\..\..\..\..\").TrimEnd('\\');
        return [Path.Combine(rootA, "Assets"), Path.Combine(rootB, "Assets"), Path.Combine(rootC, "Assets")];
    }

    static ResourceMonitor()
    {
        CopyAssetPathsToWorkingDirectory();
    }

    private static void CopyAssetPathsToWorkingDirectory()
    {
        foreach (string assets in AssetsDirectories)
        {
            if (!Directory.Exists(assets))
                continue;

            foreach (string file in Directory.GetFiles(assets, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string relativeDirectory = file.Replace(assets, string.Empty).Replace(fileName, string.Empty);
                bool musicFile = relativeDirectory.Contains("Music");

                string destinationDirectory = Program.ContentRoot;
                string fileExtension = Path.GetExtension(file).ToLowerInvariant();

                if (FileFormatDirectoryMappings.TryGetValue(fileExtension, out string? subdirectory))
                    destinationDirectory = Path.Combine(destinationDirectory, subdirectory);
                if (musicFile)
                    destinationDirectory = "mus";

                if (!Directory.Exists(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                string destinationPath = Path.Combine(destinationDirectory, fileName);
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Copy(file, destinationPath);
            }
            break;
        }
    }

    /// <summary>
    ///     Evaluates whether a distinct file watcher needs
    ///     to be created for a given file path, and if so, creates
    ///     one.
    /// </summary>
    internal static void CreateWatcherIfNecessary(string? filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (directory is null || watchersByDirectory.ContainsKey(directory))
            return;

        FileSystemWatcher watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            NotifyFilter = WatcherFilters
        };
        watcher.Changed += UpdateDatabaseIfNecessary;
        watchersByDirectory[directory] = watcher;
    }

    private static void UpdateDatabaseIfNecessary(object sender, FileSystemEventArgs fileChangeData)
    {
        if (fileChangeData.ChangeType == WatcherChangeTypes.Deleted)
            return;

        foreach (var kv in Database.resources)
        {
            if (Path.GetFileName(kv.Value.FilePath) != Path.GetFileName(fileChangeData.FullPath))
                continue;

            kv.Value.Reload(fileChangeData);
        }
    }

    /// <summary>
    ///     Takes in a given path, determines its file extension type, and
    ///     if the file doesn't exist, it then checks if an equivalent file
    ///     exists in its appropriate subdirectory.
    ///     <br></br>
    ///     If it does, the found subfile path is returned, otherwise the original
    ///     path is.
    /// </summary>
    public static string CheckForSubdirectoryEquivalent(string filePath)
    {
        if (!File.Exists(filePath))
        {
            filePath = Path.GetFullPath(filePath);

            string? directory = Path.GetDirectoryName(filePath);
            Debug.Assert(directory is not null);

            string fileName = Path.GetFileName(filePath);
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            if (FileFormatDirectoryMappings.TryGetValue(fileExtension, out string? subdirectory))
                directory = Path.Combine(directory, subdirectory);

            string potentialFilePath = Path.Combine(directory, fileName);
            if (File.Exists(potentialFilePath))
                return potentialFilePath;
        }

        return filePath;
    }
}
