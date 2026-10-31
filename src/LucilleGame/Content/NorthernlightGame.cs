using System.Numerics;
using Northernlight.Content.Borders;
using Northernlight.Content.Knight.Items;
using Northernlight.Content.SaveData;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Content;

public sealed class NorthernlightGame : Game
{
    internal const string Message = "Swatch's limitless source of spagetti code expands before you. You are filled with the power of pasta-shaped flavor text.";

    private bool checkedConfigControls;

    private sealed record RegisteredSceneData(bool InitializeEarly);

    /// <summary>
    ///     The blackening overlay countdown.
    /// </summary>
    public float BlackCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     The active game configuration.
    /// </summary>
    public Config Config
    {
        get;
        set;
    } = Config.Load();

    /// <summary>
    ///     The game resolution the Knight fight was designed around.
    /// </summary>
    public static Vector2 KnightFightResolution => new Vector2(1280, 960);

    /// <summary>
    ///     The actual native resolution of the game.
    /// </summary>
    public static Vector2 BaseResolution => new Vector2(640, 480);

    // Gamemaker!
    public override float TargetFps => 30f;

    /// <summary>
    ///     This game's UI camera.
    /// </summary>
    public RestrictedOrthographicCamera UICamera
    {
        get;
        set;
    }

    /// <summary>
    ///     This game's borders system.
    /// </summary>
    public BorderSystem Borders
    {
        get;
        private set;
    }

    /// <summary>
    ///     The root directory of all local files used
    ///     by Northernlight.
    /// </summary>
    public static string RootDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NORTHERNLIGHT");

    public Scene? CurrentScene
    {
        get;
        private set;
    }

    private readonly Dictionary<Scene, RegisteredSceneData> scenes = [];

    static NorthernlightGame()
    {
        if (!Directory.Exists(RootDirectory))
            Directory.CreateDirectory(RootDirectory);
    }

    public NorthernlightGame(GameParameters parameters) : base(parameters)
    {
        MainCamera = new RestrictedOrthographicCamera(Window, BaseResolution, Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, -1000f, 1000f);
        UICamera = new RestrictedOrthographicCamera(Window, BaseResolution, Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, -1000f, 1000f);

        Window.PauseUpdatesOnLossOfFocus = false;
        Window.PauseMusicOnLossOfFocus = false;

        Audio.Volume = RemapVolume(1f, music: false);
        Music.Player.Volume = RemapVolume(1f, music: true);

        VanillaGameItemRegistry.Initialize(this);
    }

    public override void Initialize()
    {
        base.Initialize();
        LocalizationSystem.TrySetLanguage(Config.Language);

        Borders = new BorderSystem(this);
    }

    private bool delayedPcInit;
    private bool centerWindow;
    private bool isFullscreen;

    public override void Update()
    {
        var nowFullscreen = SDL.GetWindowFlags(Window.Handle).HasFlag(SDL.WindowFlags.Fullscreen);
        if (isFullscreen != nowFullscreen)
        {
            DeltaruneGlobalData.SetIniValue("true_config.ini", "SCREEN", "FULLSCREEN", nowFullscreen ? 1f : 0f);

            if (!nowFullscreen)
            {
                SDL.SetWindowSize(Window.Handle, 640 * windowSizeMultiplier, 480 * windowSizeMultiplier);
                centerWindow = true;
            }
        }

        if (centerWindow)
        {
            // All of this logic is to replicate what I observe when DELTARUNE
            // tries (and fails) to center itself.  No idea.
            SDL.GetDisplayBounds(SDL.GetPrimaryDisplay(), out var rect);
            SDL.SetWindowPosition(Window.Handle, rect.W / 2 - Window.Width / 2 + 8, rect.H / 2 - Window.Height / 2 + 31);
            centerWindow = false;
        }

        if (!delayedPcInit)
        {
            DoWeirdTobyFoxPcInitShenanigans();
            delayedPcInit = true;
        }

        bool justChangedWindowMode = ActiveKeymap.ChangeWindowMode.WasJustPressed || ActiveKeymap.ChangeWindowModeAlt.WasJustPressed;
        if (justChangedWindowMode)
        {
            SDL.SetWindowFullscreen(Window.Handle, !nowFullscreen);
        }

        // TODO: We could eventually delay these across frames if there are
        //       performance concerns.
        foreach (var (scene, data) in scenes)
        {
            if (data.InitializeEarly && !scene.IsInitialized)
            {
                scene.Initialize();
            }
        }

        if (CurrentScene is not null)
        {
            if (!CurrentScene.IsInitialized)
            {
                CurrentScene.Initialize();
            }

            // Don't update the scene on the frame
            // the user changed the window mode, so that
            // the enter key press doesn't get
            // interpreted twice.
            if (!justChangedWindowMode)
            {
                CurrentScene.Update();
            }
        }

        Borders.Update();

        BlackCountdown = float.Max(0f, BlackCountdown - Time.DeltaTime);
        if (!checkedConfigControls)
        {
            Config.ApplyKeymapSetting();
            checkedConfigControls = true;
        }
    }

    private int windowSizeMultiplier = 1;

    private void DoWeirdTobyFoxPcInitShenanigans()
    {
        SDL.GetDisplayBounds(SDL.GetPrimaryDisplay(), out var rect);
        var displayWidth = rect.W;
        var displayHeight = rect.H;

        for (var multiplier = 1; multiplier < 12; multiplier++)
        {
            if (displayWidth > (640 * multiplier) && displayHeight > (480 * multiplier))
            {
                windowSizeMultiplier = multiplier;
            }
        }

        var setWindowSize = false;

        if (DeltaruneGlobalData.FileExists("true_config.ini"))
        {
            var doFullscreen = DeltaruneGlobalData.GetIniValue("true_config.ini", "SCREEN", "FULLSCREEN");

            if (doFullscreen != 0f)
            {
                SDL.SetWindowFullscreen(Window.Handle, fullscreen: true);
            }
            else
            {
                setWindowSize = true;
            }
        }
        else
        {
            setWindowSize = true;
        }

        if (setWindowSize && windowSizeMultiplier > 1)
        {
            SDL.SetWindowSize(Window.Handle, 640 * windowSizeMultiplier, 480 * windowSizeMultiplier);
            centerWindow = true;
        }

        // TODO: Read locale from true_config:
        /*

    var _locale = os_get_language();
    var _lang = (substr(_locale, 1, 2) != "ja") ? "en" : "ja";
    global.lang = _lang;

    if (ossafe_file_exists("true_config.ini"))
    {
        ossafe_ini_open("true_config.ini");
        global.lang = ini_read_string("LANG", "LANG", _lang);
        ossafe_ini_close();
    }
        */

        // Ura data would get ported here:
        /*
    if (!scr_has_ura_data())
    {
        for (var i = 0; i < UnknownEnum.Value_5; i++)
        {
            var chapter = i + 1;
            scr_convert_ura_data(chapter);
        }
    }
        */

        // _parent.trigger_event("init_complete");

        isFullscreen = SDL.GetWindowFlags(Window.Handle).HasFlag(SDL.WindowFlags.Fullscreen);

        // _input.init();
    }

    public override void Render(FrameContext frame)
    {
        if (BlackCountdown > 0f)
        {
            using var _ = frame.BeginRenderPass(Color.Black, default, preservePreviousContents: false);
            return;
        }

        if (CurrentScene is not { IsInitialized: true })
        {
            // TODO: This is whatever... just gives us something to do before
            //       the scene is initialized.  Ideally, we initialize before
            //       presenting and this is never hit.
            base.Render(frame);
        }
        else
        {
            CurrentScene.Render(frame);
            Borders.PrepareRender(frame, Renderer.Get<SpriteRenderer>(), CurrentScene);

            Renderer.Prepare(frame);

            OrthographicCamera camera = new OrthographicCamera(Window.Size, Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, -1000f, 1000f);
            using RenderPass pass = frame.BeginRenderPass(Color.Transparent, default, null, preservePreviousContents: true);
            Renderer.Render(frame, pass, camera);
        }
    }

    public void RegisterScene(Scene scene, bool initializeEarly)
    {
        // TODO: Could be an error?
        if (scenes.ContainsKey(scene))
        {
            return;
        }

        scenes.Add(scene, new RegisteredSceneData(initializeEarly));
    }

    public void PresentScene(Scene scene)
    {
        // TODO: Could eventually do a scene stack?
        if (CurrentScene is not null)
        {
            CurrentScene.Hide();
            scenes.Remove(CurrentScene);
            CurrentScene.Dispose(); // queue for disposal later if expensive?
        }

        MainCamera.Width = (int)BaseResolution.X;
        MainCamera.Height = (int)BaseResolution.Y;
        MainCamera.Zoom = 2f;
        UICamera.Width = (int)BaseResolution.X;
        UICamera.Height = (int)BaseResolution.Y;
        UICamera.Zoom = 2f;

        // TODO: Could be an error?
        if (!scenes.ContainsKey(scene))
        {
            RegisterScene(scene, initializeEarly: false);
        }

        CurrentScene = scene;
        CurrentScene.Show();
    }

    /// <summary>
    ///     Remaps a game volume value so as to be
    ///     in line with the master volume.
    /// </summary>
    public float RemapVolume(float volumeInterpolant, bool music)
    {
        float volumeFactor = music ? Config.MusicVolume : Config.SoundVolume;
        return volumeInterpolant * volumeFactor * 0.333f;
    }
}
