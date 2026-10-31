using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a render layer, which
///     exists as a mechanism to allow coarser layering
///     independent of the depth buffer.
/// </summary>
public sealed class RenderLayer
{
    private float forcedDefermentCountdown;

    private readonly Renderer screenShaderRenderer;

    private readonly Queue<Action<FrameContext>> preRenderPrepareQueue = [];

    /// <summary>
    ///     The renderer to use for this layer.
    /// </summary>
    public Renderer OutputRenderer => ScreenShaders.ScreenTarget.Renderer;

    /// <summary>
    ///     The depth buffer.
    /// </summary>
    public Texture? DepthBuffer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The screen shader handler.
    /// </summary>
    public ScreenShaderHandler ScreenShaders
    {
        get;
        private set;
    }

    /// <summary>
    ///     The set of all post draw actions to execute after
    ///     screen shaders.
    /// </summary>
    public readonly Queue<Action<Renderer>> PostDrawActions = [];

    /// <summary>
    ///     The event that decides what should be prepared
    ///     for this layer.
    /// </summary>
    public event Action<FrameContext, RenderLayer> Preparations;

    /// <summary>
    ///     Represents a set of render layers and corresponding
    ///     camera.
    /// </summary>
    public record RenderSet(RenderLayer Layer, Camera Camera);

    public RenderLayer(Game game)
    {
        screenShaderRenderer = new Renderer();
        screenShaderRenderer.Add(new SpriteRenderer(game.Device, game.Window, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = game.Device.PointClamp,
            InitialCapacity = 4,
            ColorTargetDescription = game.Device.GetSwapchainColorTargetDescription(game.Window),
            DepthStencilState = DepthStencilState.Default,
        }));

        ScreenShaders = new ScreenShaderHandler(game);
    }

    private void RegenerateDepthBuffer(GraphicsDevice gd, uint width, uint height)
    {
        DepthBuffer?.Dispose();

        nint depthBufferHandle = SDL.CreateGPUTexture(gd.Handle, new SDL.GPUTextureCreateInfo()
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = gd.DepthBufferFormat,
            Width = width,
            Height = height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.DepthStencilTarget
        });
        DepthBuffer = new Texture(depthBufferHandle, width, height, gd, gd.DepthBufferFormat);
    }

    /// <summary>
    ///     Enqueues a given action for the next render frame.
    /// </summary>
    public void EnqueueForBeforeNextRender(Action<FrameContext> a) => preRenderPrepareQueue.Enqueue(a);

    /// <summary>
    ///     Temporarily forces rendered contents to be deferred via render target.
    /// </summary>
    public void ForceDeferment() => forcedDefermentCountdown = 2f;

    /// <summary>
    ///     Renders a set of layers in a sequential order.
    /// </summary>
    public static void Render(RenderLayer[] layers, FrameContext frame, Time time, Color clearColor, Camera camera, Action<RenderLayer, RenderPass>? passCallback = null)
    {
        RenderSet[] sets = new RenderSet[layers.Length];
        for (int i = 0; i < sets.Length; i++)
            sets[i] = new RenderSet(layers[i], camera);
        Render(sets, frame, time, clearColor, passCallback);
    }

    private static bool ShouldSkipTargetDeferral(RenderLayer layer) => !layer.ScreenShaders.Active && layer.forcedDefermentCountdown <= 0f && layer.PostDrawActions.Count <= 0;

    /// <summary>
    ///     Renders a set of layers in a sequential order.
    /// </summary>
    public static void Render(RenderSet[] sets, FrameContext frame, Time time, Color clearColor, Action<RenderLayer, RenderPass>? passCallback = null)
    {
        Window window = frame.Window;

        for (int i = 0; i < sets.Length; i++)
        {
            RenderLayer layer = sets[i].Layer;

            layer.forcedDefermentCountdown = float.Max(0f, layer.forcedDefermentCountdown - time.DeltaTime);
            layer.ScreenShaders.Update();

            if (frame.Device.DepthBufferFormat != 0)
            {
                if (layer.DepthBuffer is null || layer.DepthBuffer.Width != window.Width || layer.DepthBuffer.Height != window.Height)
                    layer.RegenerateDepthBuffer(frame.Device, (uint)window.Width, (uint)window.Height);
            }

            layer.Preparations?.Invoke(frame, layer);
            layer.ScreenShaders.ScreenTarget.Renderer.Prepare(frame);
        }

        for (int i = 0; i < sets.Length; i++)
        {
            RenderLayer layer = sets[i].Layer;
            Camera camera = sets[i].Camera;

            SDL.GPUDepthStencilTargetInfo depthStencilInfo = default;
            if (layer.DepthBuffer is not null)
            {
                depthStencilInfo = new SDL.GPUDepthStencilTargetInfo
                {
                    Texture = layer.DepthBuffer.Handle,
                    LoadOp = SDL.GPULoadOp.Clear,
                    StoreOp = SDL.GPUStoreOp.Store,
                    ClearDepth = 1f
                };
            }

            while (layer.preRenderPrepareQueue.TryDequeue(out Action<FrameContext>? a))
                a(frame);

            bool skipDeferral = ShouldSkipTargetDeferral(layer);
            bool preserveContents = false;
            if (skipDeferral)
                preserveContents = i >= 1;

            using RenderPass pass = frame.BeginRenderPass(Color.Transparent, depthStencilInfo, skipDeferral ? null : layer.ScreenShaders.ScreenTarget.Texture, preservePreviousContents: preserveContents);
            passCallback?.Invoke(sets[i].Layer, pass);

            Vector3 originalCameraPosition = camera.Position;
            try
            {
                Texture texture = layer.ScreenShaders.ScreenTarget.Texture;
                if (skipDeferral && camera is RestrictedOrthographicCamera c)
                {
                    Vector3 baseDisplacement = new Vector3(texture.Width - c.EffectiveWidth, texture.Height - c.EffectiveHeight, 0f) * 0.5f;
                    Vector3 displacementFactor = new Vector3(baseDisplacement.X / c.EffectiveWidth, baseDisplacement.Y / c.EffectiveHeight, 0f);
                    Vector3 displacement = new Vector3(c.Width, c.Height, 1f) * displacementFactor / camera.Zoom;

                    camera.Position -= displacement;
                    pass.SetScissorRectangle(new Rectangle((int)baseDisplacement.X, (int)baseDisplacement.Y, c.EffectiveWidth, c.EffectiveHeight));
                }

                layer.ScreenShaders.ScreenTarget.Renderer.Render(frame, pass, camera);
            }
            finally
            {
                camera.Position = originalCameraPosition;
            }
        }

        for (int i = 0; i < sets.Length; i++)
        {
            RenderLayer layer = sets[i].Layer;
            if (!ShouldSkipTargetDeferral(layer))
                layer.ScreenShaders.ScreenTarget.Texture.CopyTo(frame.CommandBuffer, layer.ScreenShaders.AltScreenTarget.Texture);
        }

        for (int i = 0; i < sets.Length; i++)
        {
            RenderLayer layer = sets[i].Layer;
            Camera camera = sets[i].Camera;

            if (ShouldSkipTargetDeferral(layer))
                continue;

            {
                // TODO -- Remove restriction of only allowing one
                // screen shader.
                SpriteRenderer.ShaderWithPreparations? preparations = null;
                if (layer.ScreenShaders.Active)
                {
                    ScreenShader shader = layer.ScreenShaders.ActiveShaders[0];
                    preparations = shader.ShaderPreparations;
                }

                SpriteRenderer sprites = layer.screenShaderRenderer.Get<SpriteRenderer>();
                Texture texture = layer.ScreenShaders.ScreenTarget.Texture;
                Vector2 drawOffset = Vector2.Zero;
                if (camera is RestrictedOrthographicCamera c)
                    drawOffset = new Vector2(texture.Width - c.EffectiveWidth, texture.Height - c.EffectiveHeight) * 0.5f;

                sprites.Enqueue(texture, new SpriteData(texture)
                {
                    Position = new Vector3(texture.Size * 0.5f + drawOffset, 0f),
                    Scale = Vector2.One,
                    Tint = Color.White
                }, overridingShader: preparations);

                {
                    layer.screenShaderRenderer.Prepare(frame);

                    using RenderPass pass = frame.BeginRenderPass(clearColor, default, preservePreviousContents: i >= 1);
                    pass.SetScissorRectangle(new Rectangle(0, 0, texture.Width - (int)drawOffset.X, texture.Height));

                    layer.screenShaderRenderer.Render(frame, pass, new OrthographicCamera(texture.Size, Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, 0f, 10f));
                }

                bool anyPostDrawActions = false;
                while (layer.PostDrawActions.TryDequeue(out Action<Renderer>? action))
                {
                    action(layer.screenShaderRenderer);
                    anyPostDrawActions = true;
                }
                if (anyPostDrawActions)
                {
                    layer.screenShaderRenderer.Prepare(frame);

                    using RenderPass pass = frame.BeginRenderPass(clearColor, default, preservePreviousContents: true);
                    layer.screenShaderRenderer.Render(frame, pass, camera);
                }
            }
        }
    }
}
