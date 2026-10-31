using Northernlight.Core.Rendering;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a pool of renderers.
/// </summary>
public sealed class RendererPool : IDisposable
{
    private bool disposed;

    private readonly GraphicsDevice device;

    private readonly Window window;

    /// <summary>
    ///     The set of all reserved renderers in the pool.
    /// </summary>
    internal readonly List<PooledRenderer> Reserves = [];

    /// <summary>
    ///     The set of all active renderers in the pool.
    /// </summary>
    internal readonly List<PooledRenderer> Active = [];

    public RendererPool(GraphicsDevice device, Window window, int initialCapacity = 2)
    {
        this.device = device;
        this.window = window;

        for (int i = 0; i < initialCapacity; i++)
        {
            PooledRenderer renderer = CreateDefaultRenderer();
            Reserves.Add(renderer);
        }
    }

    /// <summary>
    ///     Rents a renderer out from this pool.
    /// </summary>
    public PooledRenderer Rent()
    {
        PooledRenderer toRent;
        if (Reserves.Count >= 1)
        {
            toRent = Reserves[0];
            Reserves.RemoveAt(0);
            Active.Add(toRent);

            toRent.returned = false;

            return toRent;
        }

        toRent = CreateDefaultRenderer();
        Active.Add(toRent);

        return toRent;
    }

    private PooledRenderer CreateDefaultRenderer()
    {
        PooledRenderer renderer = new PooledRenderer(this);
        renderer.Add(new SpriteRenderer(device, window, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = device.PointClamp,
            InitialCapacity = 4,
            ColorTargetDescription = device.GetSwapchainColorTargetDescription(window),
            DepthStencilState = DepthStencilState.Default
        }));
        renderer.Add(new MeshRenderer(device, window, 2048, 6144));
        renderer.Add(new TextRenderer(device, new SpriteQueueParameters()
        {
            BlendState = BlendState.AlphaBlend,
            Sampler = device.PointClamp,
            ColorTargetDescription = device.GetSwapchainColorTargetDescription(window),
            DepthStencilState = DepthStencilState.Default
        }));

        return renderer;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);

        foreach (PooledRenderer renderer in Reserves)
            renderer.Destroy(removeFromPool: false);
        foreach (PooledRenderer renderer in Active)
            renderer.Destroy(removeFromPool: false);
    }
}
