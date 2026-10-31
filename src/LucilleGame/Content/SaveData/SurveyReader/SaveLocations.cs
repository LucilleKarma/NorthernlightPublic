namespace Northernlight.Content.SaveData.SurveyReader;

public static class SaveLocations
{
    /// <summary>
    ///     The DELTARUNE save path on Windows.
    /// </summary>
    public static string WindowsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DELTARUNE");

    /// <summary>
    ///     The DELTARUNE save path on macOS.
    /// </summary>
    public static string MacDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Library", "Application Support", "com.tobyfox.deltarune");

    // TODO: Detection for WINE/Proton outside of Steam?
    //       Probably not necessary on desktops.
    /// <summary>
    ///     The DELTARUNE save path on Linux when using Proton from Steam Play.
    ///     <br />
    ///     This corresponds to the demo version.
    /// </summary>
    public static string LinuxSteamPlayDemoDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "compatdata", "1690940", "pfx", "drive_c", "users", "steamuser", "AppData", "Local", "DELTARUNE");

    /// <summary>
    ///     The DELTARUNE save path on Linux when using Proton from Steam Play.
    ///     <br />
    ///     This corresponds to the full release version.
    /// </summary>
    public static string LinuxSteamPlayFullDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "compatdata", "1671210", "pfx", "drive_c", "users", "steamuser", "AppData", "Local", "DELTARUNE");

    /// <summary>
    ///     The DELTARUNE save path on Linux when using the unofficial Linux
    ///     port by pugdev3.
    /// </summary>
    public static string LinuxUnofficialDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "DELTARUNE");
}
