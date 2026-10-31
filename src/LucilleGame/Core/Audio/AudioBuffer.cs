using Northernlight.Core.Resources;
using SDL3;

namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an audio data buffer.
/// </summary>
public sealed class AudioBuffer : ReloadableResource, IDisposable
{
    private bool disposed;

    public AudioBuffer(nint handle, uint length, AudioSpecifier specifications, string? path = null) : base(path)
    {
        Handle = handle;
        Length = length;
        Specifications = specifications;
    }

    /// <summary>
    ///     This buffer's handle to its underlying unmanaged
    ///     resource.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The length of this buffer.
    /// </summary>
    public uint Length
    {
        get;
        private set;
    }

    /// <summary>
    ///     The specifications for this buffer.
    /// </summary>
    public AudioSpecifier Specifications
    {
        get;
        private set;
    }

    /// <summary>
    ///     Generates an audio buffer from a .wav file.
    /// </summary>
    public static AudioBuffer FromWav(string path, bool musicDirectory = false)
    {
        if (musicDirectory)
            path = Path.Combine("mus", path);
        else
            path = ResourceMonitor.CheckForSubdirectoryEquivalent(path);

        if (!Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The inputted path must be a .wav file.");

        if (!SDL.LoadWAV(path, out var specifications, out nint bufferHandle, out uint length))
            throw new InvalidOperationException(SDL.GetError());

        return new AudioBuffer(bufferHandle, length, new AudioSpecifier()
        {
            Format = (AudioFormat)specifications.Format,
            Channels = specifications.Channels,
            SampleRate = specifications.Freq
        }, path);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        SDL.Free(Handle);
    }

    protected internal override void Reload(FileSystemEventArgs fileChangeData)
    {
        if (string.IsNullOrEmpty(FilePath))
            return;

        // TODO -- This assumes a .wav file. Generalize later.
        SDL.Free(Handle);
        SDL.LoadWAV(FilePath, out var specifications, out nint bufferHandle, out uint length);
        Specifications = new AudioSpecifier()
        {
            Format = (AudioFormat)specifications.Format,
            Channels = specifications.Channels,
            SampleRate = specifications.Freq
        };
        Handle = bufferHandle;
        Length = length;
    }
}
