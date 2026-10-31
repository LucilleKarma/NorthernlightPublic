using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a texture sampler.
/// </summary>
public sealed class Sampler : IDisposable
{
    private bool disposed;

    /// <summary>
    ///     The handle of this sampler's underlying
    ///     resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    } = nint.Zero;

    /// <summary>
    ///     The graphics device that owns this sampler.
    /// </summary>
    public GraphicsDevice Device
    {
        get;
    }

    public Sampler(GraphicsDevice device, SDL.GPUSamplerCreateInfo samplerInfo)
    {
        Device = device;
        Handle = SDL.CreateGPUSampler(device.Handle, samplerInfo);
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        SDL.ReleaseGPUSampler(Device.Handle, Handle);
    }
}
