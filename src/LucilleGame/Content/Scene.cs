using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content;

/// <summary>
///     Represents a game scene.
/// </summary>
public abstract class Scene(NorthernlightGame game) : IDisposable
{
    #region Forwarding properties
    /// <summary>
    ///     The game this scene belongs to.
    /// </summary>
    public NorthernlightGame Game => game;

    /// <inheritdoc cref="Game.Window"/>
    public Window Window => Game.Window;

    /// <inheritdoc cref="Game.Device"/>
    public GraphicsDevice Device => Game.Device;

    /// <inheritdoc cref="Game.Renderer"/>
    public Renderer Renderer => Game.Renderer;

    /// <inheritdoc cref="Game.Audio"/>
    public AudioPlayer Audio => Game.Audio;

    /// <inheritdoc cref="Game.Music"/>
    public MusicPlayer Music => Game.Music;

    /// <inheritdoc cref="Game.MainCamera"/>
    public Camera MainCamera => Game.MainCamera;

    /// <inheritdoc cref="Game.MouseScreen"/>
    public Vector2 MouseScreen
    {
        get => Game.MouseScreen;
        set => Game.MouseScreen = value;
    }

    /// <inheritdoc cref="Game.MouseWorld"/>
    public Vector2 MouseWorld
    {
        get => Game.MouseWorld;
        set => Game.MouseWorld = value;
    }

    /// <inheritdoc cref="Game.Time"/>
    public Time Time => Game.Time;

    /// <inheritdoc cref="Game.Rng"/>
    public Random Rng => Game.Rng;

    /// <inheritdoc cref="Game.DefaultLayer"/>
    public RenderLayer DefaultLayer => Game.DefaultLayer;

    /// <inheritdoc cref="Game.UILayer"/>
    public RenderLayer UILayer => Game.UILayer;

    /// <inheritdoc cref="NorthernlightGame.UICamera"/>
    public RestrictedOrthographicCamera UICamera => Game.UICamera;

    /// <inheritdoc cref="NorthernlightGame.SharedRenderers"/>
    public RendererPool SharedRenderers => Game.SharedRenderers;
    #endregion

    public bool IsInitialized
    {
        get;
        protected set;
    }

    public bool IsDisposed
    {
        get;
        protected set;
    }

    public float QuitTimer
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether using the escape key to exit the game
    ///     is supported in this scene.
    /// </summary>
    public virtual bool PlayerCanUseQuitButton => true;

    /// <summary>
    ///     Performs initialization logic the first time this scene is shown, or
    ///     optionally ahead of time.
    /// </summary>
    public virtual void Initialize()
    {
        IsInitialized = true;
        UILayer.Preparations += PrepareQuitButtonIndicator;
    }

    private void PrepareQuitButtonIndicator(FrameContext frame, RenderLayer layer)
    {
        if (PlayerCanUseQuitButton && QuitTimer > 0f)
        {
            SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();

            int totalFrames = 5;
            int currentFrame = int.Clamp((int)(QuitTimer * 30f) / 7, 0, totalFrames - 1);
            float opacity = float.InverseLerp(0f, 0.5f, QuitTimer);
            float scale = UICamera.Width / NorthernlightGame.BaseResolution.X;

            AtlasEntry quitText = Atlases.KnightScene.QuitText;
            Vector2 drawPosition = Vector2.One * 2f + quitText.Area.Size * new Vector2(0.5f, 0.5f / totalFrames) * scale;

            sprites.Enqueue(quitText, new SpriteData(quitText, 0, currentFrame, 1, totalFrames)
            {
                Position = new Vector3(drawPosition, 500f),
                Tint = Color.White * opacity * 0.999f,
                Scale = Vector2.One * scale
            });
        }
    }

    /// <summary>
    ///     Performs logic the first frame this scene is shown.
    /// </summary>
    public virtual void Show() { }

    /// <summary>
    ///     Performs logic once the scene is hidden.
    /// </summary>
    public virtual void Hide() => UILayer.Preparations -= PrepareQuitButtonIndicator;

    /// <summary>
    ///     Performs an update loop within the scene, so long as it's being
    ///     shown.
    /// </summary>
    public virtual void Update()
    {
        float quitUpdateRate = -2f;
        if (PlayerCanUseQuitButton && ActiveKeymap.Quit.PressDuration > 0f)
            quitUpdateRate = 1f;

        QuitTimer = float.Clamp(QuitTimer + Time.DeltaTime * quitUpdateRate, 0f, 1f);
        if (QuitTimer >= 1f)
            Environment.Exit(0);
    }

    /// <summary>
    ///     Renders all prepared contents within this scene, so long as it's
    ///     being shown.
    /// </summary>
    public virtual void Render(FrameContext frame)
    {
    }

    public virtual void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        GC.SuppressFinalize(this);

        // Currently no actual disposal logic to be had here.
    }
}
