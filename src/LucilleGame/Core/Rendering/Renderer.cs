using Northernlight.Core.Resources;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Represents an instance of a universal renderer, which
///     permits the usage of subsystems for rendering, such
///     as sprites or bespoke 3D meshes.
/// </summary>
public class Renderer : IDisposable
{
    protected bool disposed;

    /// <summary>
    ///     The set of subsystems maintained
    ///     by this renderer.
    /// </summary>
    public Dictionary<RuntimeTypeHandle, IRenderSubsystem> Subsystems
    {
        get;
    } = [];

    /// <summary>
    ///     Attachs a new subsystem to this renderer.
    /// </summary>
    public void Add(IRenderSubsystem subsystem) => Subsystems[subsystem.GetType().TypeHandle] = subsystem;

    /// <summary>
    ///     Gets a given subsystem within this
    ///     renderer, and throws an exception if it's
    ///     not contained.
    /// </summary>
    public TSubsystem Get<TSubsystem>() where TSubsystem : IRenderSubsystem
    {
        if (!Subsystems.TryGetValue(typeof(TSubsystem).TypeHandle, out IRenderSubsystem? subsystem))
            throw new InvalidOperationException("Subsystem not contained within this renderer.");

        return (TSubsystem)subsystem;
    }

    /// <summary>
    ///     Prepares all subsystems maintained
    ///     by this renderer.
    /// </summary>
    public void Prepare(FrameContext frame)
    {
        if (disposed)
            return;

        foreach (IRenderSubsystem subsystem in Subsystems.Values)
            subsystem.Prepare(frame);
    }

    /// <summary>
    ///     Renders all subsystems maintained
    ///     by this renderer.
    /// </summary>
    public void Render(FrameContext frame, RenderPass pass, Camera camera)
    {
        if (disposed)
            return;

        foreach (IRenderSubsystem subsystem in Subsystems.Values)
            subsystem.Render(frame, pass, camera);
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        foreach (IRenderSubsystem subsystem in Subsystems.Values)
            subsystem.Dispose();
    }
}
