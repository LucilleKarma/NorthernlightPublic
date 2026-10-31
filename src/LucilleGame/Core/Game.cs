using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Northernlight.Core.Audio;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core;

/// <summary>
///     Represents an instance of a game.
/// </summary>
public abstract class Game : IDisposable
{
    private bool disposed;

    private readonly Queue<Action<FrameContext>> preRenderPrepareQueue = [];

    /// <summary>
    ///     Whether this game instance is currently running
    ///     or not.
    /// </summary>
    public bool Running
    {
        get;
        private set;
    } = true;

    /// <summary>
    ///     The window instance owned by this game
    ///     instance.
    /// </summary>
    public Window Window
    {
        get;
        private set;
    }

    /// <summary>
    ///     The graphics device instance owned by this
    ///     game instance.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
        private set;
    }

    /// <summary>
    ///     The universal game renderer.
    /// </summary>
    public Renderer Renderer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The main audio device for this game.
    /// </summary>
    public AudioPlayer Audio
    {
        get;
        private set;
    }

    /// <summary>
    ///     The primary command buffer of this game.
    /// </summary>
    public CommandBuffer PrimaryCommandBuffer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The music player for this game.
    /// </summary>
    public MusicPlayer Music
    {
        get;
        private set;
    }

    /// <summary>
    ///     This game's camera.
    /// </summary>
    public Camera MainCamera
    {
        get;
        set;
    }

    /// <summary>
    ///     The mouse position relative to this
    ///     game instance, in screen space.
    /// </summary>
    public Vector2 MouseScreen
    {
        get;
        set;
    }

    /// <summary>
    ///     The mouse position relative to this
    ///     game instance, in world space.
    /// </summary>
    public Vector2 MouseWorld
    {
        get;
        set;
    }

    /// <summary>
    ///     The system responsible for time handling.
    /// </summary>
    public Time Time
    {
        get;
        private set;
    } = new Time();

    /// <summary>
    ///     A designated random number generator for this game.
    /// </summary>
    public Random Rng
    {
        get;
    } = new Random();

    /// <summary>
    ///     A dedicated pool for use when renting out
    ///     transient renderer objects.
    /// </summary>
    public RendererPool SharedRenderers
    {
        get;
    }

    /// <summary>
    ///     The standard render layer for this game.
    /// </summary>
    public RenderLayer DefaultLayer
    {
        get;
    }

    /// <summary>
    ///     The UI render layer for this game.
    /// </summary>
    public RenderLayer UILayer
    {
        get;
    }

    /// <summary>
    ///     The FPS target of this game.
    ///     A value of zero indicates that the FPS is uncapped.
    /// </summary>
    public virtual float TargetFps => 0f;

    public Game(GameParameters parameters)
    {
        Device = new GraphicsDevice(SDL.GPUShaderFormat.SPIRV, SDL.GPUTextureFormat.D16Unorm);

        Window = new Window(parameters.Width, parameters.Height, parameters.Title, parameters.WindowFlags, parameters.WindowProperties);
        Device.ClaimWindow(Window);

        // Ensure that the resource monitor's
        // static constructor is run early on.
        RuntimeHelpers.RunClassConstructor(typeof(ResourceMonitor).TypeHandle);

        Shaders.Load(Device);

        Renderer = new Renderer();
        Renderer.Add(new SpriteRenderer(Device, Window, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = Device.PointClamp,
            ColorTargetDescription = Device.GetSwapchainColorTargetDescription(Window),
            DepthStencilState = DepthStencilState.Default
        }));

        SharedRenderers = new RendererPool(Device, Window);

        DefaultLayer = CreateSimpleLayer();
        UILayer = CreateSimpleLayer();

        Audio = new AudioPlayer();
        Music = new MusicPlayer(Window);

        if (parameters.MainCamera is not null)
            MainCamera = parameters.MainCamera;

        Initialize();

        SDLEventProcessor.PreProcess += () =>
        {
            Input.MouseWheelMotion = 0f;
        };
        SDLEventProcessor.OnEvent += Input.ProcessEvents;
        SDLEventProcessor.OnEvent += (e, type) =>
        {
            if (!disposed && type == SDL.EventType.Quit)
                Running = false;
        };
    }

    /// <summary>
    ///     Creates a simple render layer
    ///     for this game.
    /// </summary>
    public RenderLayer CreateSimpleLayer()
    {
        RenderLayer layer = new RenderLayer(this);
        layer.OutputRenderer.Add(new SpriteRenderer(Device, Window, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = Device.PointClamp,
            ColorTargetDescription = Device.GetSwapchainColorTargetDescription(Window),
            DepthStencilState = DepthStencilState.Default
        }));

        layer.OutputRenderer.Add(new TextRenderer(Device, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = Device.PointClamp,
            ColorTargetDescription = Device.GetSwapchainColorTargetDescription(Window),
            DepthStencilState = DepthStencilState.Default
        }));

        return layer;
    }

    /// <summary>
    ///     Runs this game in perpetuity until quitting.
    /// </summary>
    public void Run()
    {
        if (disposed)
            return;

        while (Running)
            Time.Record(RunOneFrame, TargetFps);

        Dispose();
    }

    private bool RunOneFrame(Stopwatch watch)
    {
        if (disposed)
            return false;

        SDLEventProcessor.Process();
        Music.Update();

        PrimaryCommandBuffer = Device.GenerateCommandBuffer();
        using FrameContext frameContext = Device.BeginFrame(Window, PrimaryCommandBuffer);

        while (preRenderPrepareQueue.TryDequeue(out Action<FrameContext>? a))
            a(frameContext);

        if (frameContext.SkipFrame)
        {
            watch.Stop();
            RenderScene(frameContext);
            PrimaryCommandBuffer.Dispose();
            return false;
        }

        Input.Process(this);
        Update();

        RenderScene(frameContext);
        PrimaryCommandBuffer.Dispose();
        return true;
    }

    private void RenderScene(FrameContext frame)
    {
        if (Window.IsMinimized)
            return;

        Render(frame);
    }

    /// <summary>
    ///     Enqueues a given action for the next render frame.
    /// </summary>
    public void EnqueueForBeforeNextRender(Action<FrameContext> a) => preRenderPrepareQueue.Enqueue(a);

    /// <summary>
    ///     Performs extra initialization logic for this game.
    /// </summary>
    public virtual void Initialize()
    {
        using CommandBuffer commandBuffer = Device.GenerateCommandBuffer();
        using FrameContext frameContext = Device.BeginFrame(Window, commandBuffer);

        Atlases.Initialize(Device, frameContext);
        TopologyData<TriangleListTopology>.Instance = new TopologyData<TriangleListTopology>(frameContext.CommandBuffer);
    }

    /// <summary>
    ///     Renders all prepared contents for this game.
    /// </summary>
    public virtual void Render(FrameContext frame)
    {
        RenderLayer.Render([DefaultLayer, UILayer], frame, Time, Color.White, MainCamera);
    }

    /// <summary>
    ///     Performs an update loop for this game.
    /// </summary>
    /// <remarks>
    ///     This should be separate from rendering
    ///     logic.
    /// </remarks>
    public abstract void Update();

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);

        Renderer.Dispose();
        Device.Dispose();
        Window.Dispose();
    }
}
