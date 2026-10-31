using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of an SDL surface, a
///     software-level analogue to hardware-level textures.
/// </summary>
public sealed class Surface : ReloadableResource, IDisposable
{
    private bool disposed;

    /// <summary>
    ///     The handle of this surface to its underlying
    ///     SDL resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    } = nint.Zero;

    private Surface(string? filePath) : base(filePath)
    { }

    /// <summary>
    ///     Generates a new surface from a PNG file path.
    /// </summary>
    public static Surface FromPNG(string filePath)
    {
        return new Surface(filePath)
        {
            Handle = SDL.LoadPNG(filePath)
        };
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        Return();
        GC.SuppressFinalize(this);
        SDL.DestroySurface(Handle);
    }

    protected internal override void Reload(FileSystemEventArgs fileChangeData)
    {
        if (disposed || FilePath is null)
            return;

        SDL.DestroySurface(Handle);
        Handle = SDL.LoadPNG(FilePath);
    }
}
