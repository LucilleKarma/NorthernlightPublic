using System.Diagnostics.CodeAnalysis;

namespace Northernlight.Content.SaveData.SurveyReader;

public static class GameDataLocations
{
    /// <summary>
    ///     The set of all required files
    ///     that must be present within a candidate
    ///     folder in order for it to be considered
    ///     valid.
    /// </summary>
    private static readonly string[] RequiredFiles =
    [
        "DELTARUNE.exe",
        "data.win"
    ];

    /// <summary>
    ///     The set of all possible standard steam directories.
    /// </summary>
    private static readonly string[] PossibleSteamDirectories = FindSteamRootDirectories().ToArray();

    /// <summary>
    ///     The standard DELTARUNE save path for steam.
    /// </summary>
    private static readonly string[] StandardSteamDirectories = PossibleSteamDirectories.Select(s => Path.Combine(s, "steamapps", "common", "DELTARUNE")).ToArray();

    /// <summary>
    ///     The libraryfolders save path for steam.
    /// </summary>
    private static readonly string[] SteamLibraryFolderPaths = PossibleSteamDirectories.Select(s => Path.Combine(s, "steamapps", "libraryfolders.vdf")).ToArray();

    /// <summary>
    ///     The standard DELTARUNE save path for itch.io.
    /// </summary>
    private static readonly string StandardItchIoDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "itch", "apps", "DELTARUNE");

    private static List<string> FindSteamRootDirectories()
    {
        List<string> candidates = [];

        if (OperatingSystem.IsWindows())
        {
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"));
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            if (OperatingSystem.IsMacOS())
            {
                string homeDirectoryMac = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string macSteamRoot = Path.Combine(homeDirectoryMac, "Library", "Application Support", "Steam");
                candidates.Add(macSteamRoot);
            }

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string modernPath = Path.Combine(homeDirectory, ".local", "share", "Steam");
            string legacyPath = Path.Combine(homeDirectory, ".steam", "steam");
            string linuxSteamRoot = Directory.Exists(modernPath) ? modernPath : legacyPath;
            candidates.Add(linuxSteamRoot);
        }

        return candidates;
    }

    private static void ParseSteamDirectories(HashSet<string> candidates)
    {
        foreach (string directory in StandardSteamDirectories)
        {
            if (Directory.Exists(directory))
                candidates.Add(directory);
        }

        foreach (string libraryFolderPath in SteamLibraryFolderPaths)
        {
            if (!File.Exists(libraryFolderPath))
                continue;

            foreach (string library in ParseSteamLibrariesFromVdf(libraryFolderPath))
            {
                string candidate = Path.Combine(library, "steamapps", "common", "DELTARUNE");
                if (Directory.Exists(candidate))
                    candidates.Add(candidate);
            }
        }
    }

    private static List<string> ParseSteamLibrariesFromVdf(string vdfPath)
    {
        List<string> libraries = [];
        string[] lines = File.ReadAllLines(vdfPath);

        foreach (string line in lines)
        {
            if (line.Contains('"'))
            {
                string[] parts = line.Split('"');
                if (parts.Length >= 4)
                {
                    string path = parts[3];
                    if (Directory.Exists(path))
                        libraries.Add(path);
                }
            }
        }

        return libraries;
    }

    private static void ParseItchIoDirectories(HashSet<string> candidates)
    {
        if (Directory.Exists(StandardItchIoDirectory))
            candidates.Add(StandardItchIoDirectory);
    }

    private static bool ValidateDirectoryForDeltarune(string directory) => RequiredFiles.All(file => File.Exists(Path.Combine(directory, file)));

    /// <summary>
    ///     Tries to find a valid Deltarune install directory, and
    ///     returns it if one was found.
    /// </summary>
    public static bool TryFind([NotNullWhen(true)] out string? directory)
    {
        directory = null;
        try
        {
            HashSet<string> candidates = [];
            ParseSteamDirectories(candidates);
            ParseItchIoDirectories(candidates);

            if (OperatingSystem.IsMacOS())
            {
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DELTARUNE.app"));
            }

            foreach (string candidate in candidates)
            {
                if (ValidateDirectoryForDeltarune(candidate))
                {
                    directory = candidate;
                    return true;
                }
            }
        }
        catch (IOException)
        {
            return false;
        }

        return false;
    }
}
