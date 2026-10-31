using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of an SDL-TTF font.
/// </summary>
public sealed class Font : IDisposable
{
    private float size;

    private bool disposed;

    /// <summary>
    ///     The handle of this font to its underlying
    ///     GPU resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    } = nint.Zero;

    /// <summary>
    ///     Whether kerning is enabled for this font or not.
    /// </summary>
    public bool KerningEnabled
    {
        get
        {
            if (disposed)
                return false;

            return TTF.GetFontKerning(Handle);
        }

        set
        {
            if (disposed)
                return;

            TTF.SetFontKerning(Handle, value);
        }
    }

    /// <summary>
    ///     The font's size.
    /// </summary>
    public float Size
    {
        get => size;
        set
        {
            if (size != value && !disposed)
            {
                size = value;
                TTF.SetFontSize(Handle, size);
            }
        }
    }

    public Font(string ttfFilename, float fontSize, bool kerning = true)
    {
        ttfFilename = ResourceMonitor.CheckForSubdirectoryEquivalent(ttfFilename);

        KerningEnabled = kerning;
        Size = fontSize;
        Handle = TTF.OpenFont(ttfFilename, Size);
    }

    public void Dispose()
    {
        if (disposed || Handle == nint.Zero)
            return;

        disposed = true;
        GC.SuppressFinalize(this);
        TTF.CloseFont(Handle);
    }
}
