using System.Runtime.InteropServices;

namespace Northernlight;

/// <summary>
///     Provides basic Steam integration.  Just serves to make it look like
///     you're actually playing DELTARUNE on Steam.
/// </summary>
internal static unsafe partial class SteamIntegration
{
    private const string app_id = "1671210";

    public static void Initialize()
    {
        // The bundled Facepunch.Steamworks version expects a lower version than
        // the bundled native binaries, so just manually init here.  Our
        // integration is only for the sake of showing you as playing the game
        // on Steam.
        // SteamClient.Init(1671210);

        Environment.SetEnvironmentVariable("SteamAppId", app_id);
        Environment.SetEnvironmentVariable("SteamGameId", app_id);

        var errors = stackalloc byte[1024];
        var terminator = (byte)0;

        try
        {
            SteamAPI_Init64(&terminator, errors);
        }
        catch
        {
            try
            {
                SteamAPI_Init(&terminator, errors);
            }
            catch
            {
                // ignore
            }
        }
    }

    [LibraryImport("steam_api", EntryPoint = "SteamInternal_SteamAPI_Init")]
    private static partial InitResult SteamAPI_Init(byte* pszInternalCheckInterfaceVersions, byte* pOutErrMsg);

    [LibraryImport("steam_api64", EntryPoint = "SteamInternal_SteamAPI_Init")]
    private static partial InitResult SteamAPI_Init64(byte* pszInternalCheckInterfaceVersions, byte* pOutErrMsg);

    private enum InitResult
    {
        Ok,
        FailedGeneric,
        NoSteamClient,
        VersionMismatch,
    }
}
