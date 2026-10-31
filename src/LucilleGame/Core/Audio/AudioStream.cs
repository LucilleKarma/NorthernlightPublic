using SDL3;

namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an audio stream.
/// </summary>
public sealed class AudioStream : IDisposable
{
    private bool disposed;

    // Audio streams start off paused and with a volume
    // of one when created by SDL.
    private bool paused = true;

    private float frequencyRatio = 1f;

    /// <summary>
    ///     This stream's handle.
    /// </summary>
    public nint Handle
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether this stream
    ///     has been paused or not.
    /// </summary>
    public bool Paused
    {
        get => paused;
        set
        {
            if (disposed)
                return;

            if (paused != value)
            {
                paused = value;

                if (paused)
                    SDL.PauseAudioStreamDevice(Handle);
                else
                    SDL.ResumeAudioStreamDevice(Handle);
            }
        }
    }

    /// <summary>
    ///     The volume of this stream.
    /// </summary>
    public float Volume
    {
        get;

        set
        {
            if (disposed)
                return;

            if (float.Abs(field - value) > float.Epsilon)
            {
                field = value;
                SDL.SetAudioStreamGain(Handle, field * PlayerVolumeFactor);
            }
        }
    } = 1f;

    internal float PlayerVolumeFactor
    {
        get;

        set
        {
            if (disposed)
                return;

            if (float.Abs(field - value) > float.Epsilon)
            {
                field = value;
                SDL.SetAudioStreamGain(Handle, Volume * field);
            }
        }
    } = 1f;

    /// <summary>
    ///     The stream frequency ratio of this stream.
    /// </summary>
    public float FrequencyRatio
    {
        get => frequencyRatio;
        set
        {
            if (disposed)
                return;

            if (frequencyRatio != value)
            {
                frequencyRatio = value;
                SDL.SetAudioStreamFrequencyRatio(Handle, frequencyRatio);
            }
        }
    }

    /// <summary>
    ///     The amount of bytes available for this stream.
    /// </summary>
    public int Capacity => SDL.GetAudioStreamAvailable(Handle);

    public AudioStream(AudioSpecifier preferredSpecifier, bool immediatelyStartPlayback)
    {
        Handle = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, new SDL.AudioSpec()
        {
            Channels = preferredSpecifier.Channels,
            Format = (SDL.AudioFormat)preferredSpecifier.Format,
            Freq = preferredSpecifier.SampleRate
        }, null, 0);

        if (immediatelyStartPlayback)
            Paused = false;
    }

    /// <summary>
    ///     Submits the contents of raw audio data to this
    ///     stream.
    /// </summary>
    public void Submit(ReadOnlySpan<byte> buffer)
    {
        SDL.PutAudioStreamData(Handle, buffer, buffer.Length);
    }

    /// <summary>
    ///     Submits the contents of raw audio data to this
    ///     stream.
    /// </summary>
    public void Submit(nint buffer, int length)
    {
        SDL.PutAudioStreamData(Handle, buffer, length);
    }

    /// <summary>
    ///     Submits the contents of an audio buffer to this
    ///     stream.
    /// </summary>
    public void Submit(AudioBuffer buffer)
    {
        SDL.PutAudioStreamData(Handle, buffer.Handle, (int)buffer.Length);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        SDL.DestroyAudioStream(Handle);
        Handle = 0;
    }
}
