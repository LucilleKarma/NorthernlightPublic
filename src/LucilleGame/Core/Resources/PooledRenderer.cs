using Northernlight.Core.Rendering;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a pool of renderers.
/// </summary>
public sealed class PooledRenderer : Renderer
{
    internal bool returned;

    /// <summary>
    ///     The pool responsible for this renderer.
    /// </summary>
    internal RendererPool Pool
    {
        get;
        private set;
    }

    internal PooledRenderer(RendererPool pool) => Pool = pool;

    public override void Dispose()
    {
        if (disposed || returned)
            return;

        // base.Dispose is deliberately not called
        // here, in the interest of not destroying underlying
        // graphics resources that may be used in the
        // future by successive rents from the pool.
        returned = true;

        Pool.Active.Remove(this);
        Pool.Reserves.Add(this);
    }

    /// <summary>
    ///     Destroys the graphics resources of this
    ///     renderer and banishes it from the
    ///     pool completely if required.
    /// </summary>
    public void Destroy(bool removeFromPool = true)
    {
        if (disposed)
            return;

        base.Dispose();

        if (removeFromPool)
        {
            Pool.Active.Remove(this);
            Pool.Reserves.Remove(this);
        }
    }
}
