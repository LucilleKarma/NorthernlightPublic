using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;
using SDL3;

namespace Northernlight.Content.Knight.AttackPatterns;

/// <summary>
///     A simple handler responsible for the
///     rendering of the Knight's depths visual.
/// </summary>
public sealed class FinalAttackDepthsHandler
{
    private struct Ball
    {
        public float Timer;

        public Vector3 Position;

        public Vector2 Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Uniforms
    {
        public Vector4 DepthsTextureBounds;

        public Vector4 DepthsTextureZoom;

        public Vector4 Time;
    }

    private static float depthsTimer;

    private readonly Scene scene;

    private readonly List<Ball> balls = [];

    /// <summary>
    ///     How long this handler has been active, in seconds.
    /// </summary>
    public float ActiveTimer
    {
        get;
        internal set;
    }

    /// <summary>
    ///     How far the sea effect has risen.
    /// </summary>
    public float SeaRiseHeight
    {
        get;
        set;
    }

    /// <summary>
    ///     The overlay opacity of this handler.
    /// </summary>
    public float OverlayOpacity
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The top of this sea.
    /// </summary>
    public Vector2 SeaTop => NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 1f) - Vector2.UnitY * SeaRiseHeight;

    /// <summary>
    ///     The render target that contains all depths
    ///     balls.
    /// </summary>
    public static Texture DepthsBallsTarget
    {
        get;
        private set;
    }

    public FinalAttackDepthsHandler(Scene scene)
    {
        this.scene = scene;
        SDL.GPUTextureCreateInfo textureCreationInfo = new SDL.GPUTextureCreateInfo()
        {
            Width = (uint)NorthernlightGame.KnightFightResolution.X,
            Height = (uint)NorthernlightGame.KnightFightResolution.X,
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        };
        DepthsBallsTarget ??= new Texture(textureCreationInfo, scene.Device);
    }

    /// <summary>
    ///     Creates a new ball at a given 2D point
    ///     with a given velocity.
    /// </summary>
    public void CreateBall(Vector2 position, Vector2 velocity) => CreateBall(new Vector3(position, 0f), velocity);

    /// <summary>
    ///     Clears all balls.
    /// </summary>
    public void ClearBalls() => balls.Clear();

    /// <summary>
    ///     Creates a new ball at a given point
    ///     with a given velocity.
    /// </summary>
    public void CreateBall(Vector3 position, Vector2 velocity)
    {
        balls.Add(new Ball()
        {
            Position = position,
            Velocity = velocity
        });
    }

    /// <summary>
    ///     Updates this handler.
    /// </summary>
    public void Update()
    {
        ActiveTimer += scene.Time.DeltaTime;
        depthsTimer += scene.Time.DeltaTime;

        Span<Ball> span = CollectionsMarshal.AsSpan(balls);

        for (int i = 0; i < balls.Count; i++)
        {
            ref Ball ball = ref span[i];
            ball.Timer += scene.Time.DeltaTime;
            ball.Position += new Vector3(ball.Velocity, 0f) * scene.Time.DeltaTime;
        }
        balls.RemoveAll(b => b.Timer >= 5f);
    }

    /// <summary>
    ///     Renders this handler's contents.
    /// </summary>
    public void Render(SpriteRenderer sprites)
    {
        if (balls.Count <= 0 || OverlayOpacity <= 0f || scene is not KnightFightScene fightScene)
            return;

        fightScene.SoulLayer.EnqueueForBeforeNextRender(RenderBallsIntoTarget);

        sprites.Enqueue(DepthsBallsTarget, new SpriteData(DepthsBallsTarget)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 0.5f), 500f),
            Tint = new Color(105, 105, 105) * OverlayOpacity,
            Scale = Vector2.One
        }, overridingShader: new SpriteRenderer.ShaderWithPreparations(Shaders.DepthsBallMaskingShader, PrepareShaderParameters));

        Vector2 fountainScale = new Vector2(1f, 0.25f);
        float expandProgress = float.InverseLerp(0f, 0.1f, ActiveTimer);
        fountainScale = Vector2.Lerp(fountainScale, new Vector2(15f + ActiveTimer * 0.6f, 0.19f), float.Smoothstep(expandProgress));

        float riseProgress = float.InverseLerp(0.1f, 0.3f, ActiveTimer);
        fountainScale = Vector2.Lerp(fountainScale, new Vector2(2f, 11f), new Vector2(riseProgress, float.Pow(riseProgress, 5f)));
        fountainScale.X += float.Sin(ActiveTimer * 120f) * riseProgress * 0.1f;

        float fadeTaper = float.InverseLerp(2f, 0.4f, ActiveTimer);
        fountainScale.X *= fadeTaper.Cubed();

        AtlasEntry ballTextureFull = Atlases.KnightScene.FountainBall;
        sprites.Enqueue(ballTextureFull, new SpriteData(ballTextureFull)
        {
            Position = new Vector3(NorthernlightGame.KnightFightResolution * new Vector2(0.5f, 1.02f) - Vector2.UnitY * ballTextureFull.Area.Height * fountainScale.Y * 0.5f, 500f),
            Tint = Color.White * fadeTaper * OverlayOpacity,
            Scale = fountainScale
        });
    }

    private void RenderBallsIntoTarget(FrameContext frame)
    {
        using TextureWriteScope scope = DepthsBallsTarget.Scope(scene.SharedRenderers, frame, scene.Device.PointClamp);

        AtlasEntry ballTexture = Atlases.LargeTextures.DepthsBall;
        AtlasEntry ballTextureFull = Atlases.LargeTextures.DepthsBallFull;
        SpriteRenderer scopedSprites = scope.Renderer.Get<SpriteRenderer>();

        AtlasEntry seaTexture = Atlases.LargeTextures.DepthsBallSea;
        Vector2 seaScale = NorthernlightGame.KnightFightResolution / seaTexture.Area.Size * 1.1f;
        Vector2 seaCenter = SeaTop + Vector2.UnitY * (seaTexture.Area.Size.Y * seaScale.Y * 0.5f + 150f);
        scopedSprites.Enqueue(seaTexture, new SpriteData(seaTexture)
        {
            Position = new Vector3(seaCenter, 500f),
            Tint = Color.White,
            Scale = seaScale
        });
        for (int i = 0; i < 12; i++)
        {
            float progress = i / 11f;
            float x = float.Lerp(0f, NorthernlightGame.KnightFightResolution.X, progress);
            float y = SeaTop.Y + 150f;
            y += float.Cos01(float.Tau * progress + ActiveTimer * 5f) * 50f;

            Vector2 scale = Vector2.One * 1.3f;
            scopedSprites.Enqueue(ballTexture, new SpriteData(ballTexture)
            {
                Position = new Vector3(x, y, 0f),
                Tint = Color.White,
                Scale = scale,
            });

            scopedSprites.Enqueue(ballTextureFull, new SpriteData(ballTextureFull)
            {
                Position = new Vector3(x, y, 0f),
                Tint = new Color(0, 255, 255),
                Scale = scale * 0.9f,
            }, BlendState.Subtractive);
        }

        foreach (Ball ball in balls)
        {
            Vector2 scale = Vector2.One * (float.InverseLerp(0f, 0.75f, ball.Timer) + ball.Timer * 0.15f);
            scopedSprites.Enqueue(ballTexture, new SpriteData(ballTexture)
            {
                Position = ball.Position,
                Tint = Color.White,
                Scale = scale,
            });

            scopedSprites.Enqueue(ballTextureFull, new SpriteData(ballTextureFull)
            {
                Position = ball.Position,
                Tint = new Color(0, 255, 255),
                Scale = scale * 0.9f,
            }, BlendState.Subtractive);
        }
    }

    private static void PrepareShaderParameters(FrameContext frame, RenderPass pass)
    {
        frame.CommandBuffer.BindFragmentUniformData(0, new Uniforms()
        {
            DepthsTextureZoom = Vector4.One * 0.325f,
            DepthsTextureBounds = Atlases.LargeTextures.IMAGE_DEPTH_OmitPadding.VectorBounds,
            Time = Vector4.UnitX * depthsTimer * -0.25f
        });
        pass.BindFragmentSamplerTextures(1, new RenderPass.SamplerTexture(frame.Device.PointWrap, Atlases.LargeTextures.Atlas.Value.BackingTexture));
    }
}
