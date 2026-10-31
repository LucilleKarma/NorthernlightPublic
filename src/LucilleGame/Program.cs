using System.Runtime.InteropServices;
using System.Text;
using Northernlight.Content;
using Northernlight.Content.ChapterSelect;
using Northernlight.Core;
using Northernlight.Core.Logging;
using SDL3;

namespace Northernlight;

/// <summary>
///     The central class responsible for the handling
///     of core game operations.
/// </summary>
public static class Program
{
    internal static readonly string _ = "JKIKSHKX\n\nEUA NGBK LARLORRKJ\nEUAX JKYZOTE\n\nHAZ.\n" +
                "OZ YZORR SUBKY.\n\nZNK OTJKROHRK OTQ UL ZNK\nHRGIQ VKT.\n\n" +
                "EUA CORR LARLORR ZNKOX JKYZOTE ZUU...\nCUT'Z EUA?\n\n\n\n...\n\n" +
                "TU SGZZKX.\n\nO RKGBK EUA\nUTRE UTK\nINUOIK".Replace("\n", Environment.NewLine);

    /// <summary>
    ///     The content root directory for all Northernlight stuff.
    /// </summary>
    public static readonly string ContentRoot = Path.Combine(Directory.GetCurrentDirectory(), "chapter6_windows");

    private static void InitializeSdl()
    {
        SDL.ClearError();

        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad | SDL.InitFlags.Audio))
            throw new Exception($"Failed to start SDL: {SDL.GetError()}");

        SDL.ClearError();

        if (!TTF.Init())
            throw new Exception($"Failed to start SDL TTF: {SDL.GetError()}");

        SDL.ClearError();

        if (!ShaderCross.Init())
            throw new Exception($"Failed to start SDL Shadercross: {SDL.GetError()}");

        SDL.ClearError();

        var mappings = Path.Combine(ContentRoot, "Assets", "gamecontrollerdb.txt");
        if (File.Exists(mappings))
        {
            if (SDL.AddGamepadMappingsFromFile(mappings) == -1)
                Console.WriteLine($"Failed to load game controller mappings: {SDL.GetError()}");
        }
        else
        {
            Console.WriteLine($"Failed to load game controller mappings: file not found");
        }

        SDL.ClearError();
        SDL.SetLogOutputFunction(InterceptSdlLogs, nint.Zero);
    }

    private static void InterceptSdlLogs(nint userData, SDL.LogCategory category, SDL.LogPriority priority, [MarshalAs(UnmanagedType.LPUTF8Str)] string message)
    {
        switch (priority)
        {
            case SDL.LogPriority.Trace:
            case SDL.LogPriority.Verbose:
            case SDL.LogPriority.Debug:
            case SDL.LogPriority.Info:
                Logger.Debug(message);
                break;
            case SDL.LogPriority.Warn:
                Logger.Warning(message);
                break;
            case SDL.LogPriority.Error:
            case SDL.LogPriority.Critical:
                Logger.Error(message);
                break;
        }
    }

    private static void Main()
    {
        SteamIntegration.Initialize();
        CreateFakeFiles();

        // Static linkage file that doesn't get
        // neatly contained in the project's self-contained
        // build, and isn't required.
        // Byebye!
        File.Delete("steam_api64.lib");

        {
            FineTimeElapseScope scope = new FineTimeElapseScope();
            InitializeSdl();
            scope.Dispose();

            Logger.Debug($"SDL initialized in {scope.Elapsed.TotalSeconds:n4}s.");
        }

        SDL.HideCursor();

        StartAndRunNorthernlight();
    }

    private static void CreateFakeFiles()
    {
        for (int i = 1; i <= 5; i++)
        {
            string directory = $"chapter{i}_windows";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        string optionsFilePath = "options.ini";
        if (!File.Exists(optionsFilePath))
            File.WriteAllText(optionsFilePath, "[Windows]\nSleepMargin=30\nUsex64=True");

        string dataFilePath = "data.win";
        if (!File.Exists(dataFilePath))
        {
            int size = 1607251;
            StringBuilder sb = new StringBuilder(size + 1024);

            sb.Append(_);

            Random rng = new Random(774);
            byte[] fakeData = new byte[size];
            rng.NextBytes(fakeData);

            for (int i = 0; i < size; i++)
                sb.Append((char)fakeData[i]);

            File.WriteAllText(dataFilePath, sb.ToString());
        }
    }

    private static void StartAndRunNorthernlight()
    {
        FineTimeElapseScope scope = new FineTimeElapseScope();
        GameParameters northernlightParameters = new GameParameters((int)NorthernlightGame.KnightFightResolution.X, (int)NorthernlightGame.KnightFightResolution.Y, "DELTARUNE")
        {
            // InputFocus ensures it opens with us focused on it, like the real
            // game.
            WindowFlags = SDL.WindowFlags.InputFocus,
            WindowProperties = CreateStupidPropertiesToReplicateOneFrameOfGameMakerLogicThatTobyFoxDoesNotFollowAFrameLater(),
        };
        NorthernlightGame game = new NorthernlightGame(northernlightParameters);
        ChapterSelectScene scene = new ChapterSelectScene(game);
        {
            game.RegisterScene(scene, initializeEarly: false);

            // Initialize early to beat the update frame delay at launch.
            scene.Initialize();
        }
        game.PresentScene(scene);

        scope.Dispose();
        Logger.Debug($"NORTHERNLIGHT initialized in {scope.Elapsed.TotalSeconds:n4}s.");

        game.Run();
    }

    private static uint CreateStupidPropertiesToReplicateOneFrameOfGameMakerLogicThatTobyFoxDoesNotFollowAFrameLater()
    {
        uint properties = SDL.CreateProperties();

        // Important to use UsableBounds... this basically just rests the window
        // right on top of the taskbar.
        SDL.GetDisplayUsableBounds(SDL.GetPrimaryDisplay(), out SDL.Rect rect);

        SDL.SetNumberProperty(properties, SDL.Props.WindowCreateYNumber, rect.H - (int)NorthernlightGame.KnightFightResolution.Y - 1);

        return properties;
    }
}
