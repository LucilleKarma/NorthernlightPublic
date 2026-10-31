using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a global
///     sprite renderer, which can accept any
///     kind of sprite for rendering purposes.
/// </summary>
public sealed class SpriteRenderer : IRenderSubsystem
{
    public record Key(Texture Texture, BlendState BlendState, ShaderWithPreparations? OverridingShader, bool Translucent, int QueuePriorityInfluence);

    public record ShaderWithPreparations(Shader Shader, Action<FrameContext, RenderPass>? ExtraPreparations);

    private bool disposed;

    private readonly Lock initializationLock = new Lock();

    private readonly Dictionary<Key, SpriteQueue> queues = [];

    /// <summary>
    ///     This renderer's graphics device.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    /// <summary>
    ///     This renderer's window.
    /// </summary>
    public Window Window
    {
        get;
    }

    /// <summary>
    ///     The initialization parameters for this
    ///     renderer.
    /// </summary>
    public SpriteQueueParameters InitializationParameters
    {
        get;
        set;
    }

    public SpriteRenderer(GraphicsDevice device, Window window, SpriteQueueParameters parameters)
    {
        Device = device;
        Window = window;
        InitializationParameters = parameters;
    }

    /// <summary>
    ///     Enqueues a given sprite to this renderer.
    /// </summary>
    public void Enqueue(Texture texture, SpriteData sprite, BlendState blendState = default, bool drawToDepthBuffer = true, ShaderWithPreparations? overridingShader = null, bool translucent = false, int queuePriorityInfluence = 0)
    {
        Color tint = sprite.Tint;
        if (tint.R <= 0f && tint.G <= 0f && tint.B <= 0f && tint.A <= 0.001f)
            return;

        if (tint.A < 1f)
            translucent = true;

        if (blendState == default)
            blendState = BlendState.AlphaBlend;

        Key key = new Key(texture, blendState, overridingShader, translucent, queuePriorityInfluence);
        if (!queues.ContainsKey(key))
        {
            lock (initializationLock)
            {
                queues[key] = new SpriteQueue(Device, Window, texture, InitializationParameters with
                {
                    BlendState = key.BlendState,
                    OverridingFragmentShader = key.OverridingShader?.Shader,
                    DepthStencilState = drawToDepthBuffer ? DepthStencilState.Default : DepthStencilState.DepthRead,
                }, key.OverridingShader?.ExtraPreparations);
            }
        }

        queues[key].Enqueue(sprite);
    }

    /// <summary>
    ///     Enqueues a given sprite to this renderer.
    /// </summary>
    public void Enqueue(AtlasEntry texture, SpriteData sprite, BlendState blendState = default, bool drawToDepthBuffer = true, ShaderWithPreparations? overridingShader = null, bool translucent = false, int queuePriorityInfluence = 0) =>
        Enqueue(texture.BackingTexture, sprite, blendState, drawToDepthBuffer, overridingShader, translucent, queuePriorityInfluence);

    public void Prepare(FrameContext frame)
    {
        if (disposed)
            return;

        foreach (SpriteQueue queue in queues.OrderBy(LayerOrderingFunction).Select(kv => kv.Value))
            queue.Prepare(frame);
    }

    public void Render(FrameContext frame, RenderPass pass, Camera camera)
    {
        if (disposed)
            return;

        foreach (SpriteQueue queue in queues.OrderBy(LayerOrderingFunction).Select(kv => kv.Value))
            queue.Render(frame, pass, camera);
    }

    private static int LayerOrderingFunction(KeyValuePair<Key, SpriteQueue> kv) => kv.Key.QueuePriorityInfluence;

    public void Dispose()
    {
        if (disposed)
            return;

        foreach (SpriteQueue queue in queues.Values)
            queue.Dispose();
        disposed = true;
    }
}
