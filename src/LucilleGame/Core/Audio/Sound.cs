using System.Runtime.InteropServices;

namespace Northernlight.Core.Audio;

/// <summary>
///     Represents a sound that can be played
///     by a sound player.
/// </summary>
public sealed class Sound : IDisposable
{
    private bool disposed;

    private readonly Lock readLock = new Lock();

    /// <summary>
    ///     The cancellation token used by this sound
    ///     for stopping purposes.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource
    {
        get;
        private set;
    }

    /// <summary>
    ///     This sound's audio buffer.
    /// </summary>
    public AudioBuffer Buffer
    {
        get;
    }

    /// <summary>
    ///     This sound's audio stream.
    /// </summary>
    public AudioStream Stream
    {
        get;
    }

    /// <summary>
    ///     The playback position of this
    ///     sound.
    /// </summary>
    public int PositionBytes
    {
        get;
        set;
    }

    /// <summary>
    ///     The current play position of this sound, in
    ///     seconds.
    /// </summary>
    public double PlayPositionSeconds
    {
        get => PositionBytes / (double)(Buffer.Specifications.SampleRate * sizeof(float));
        set
        {
            PositionBytes = (int)(value * Buffer.Specifications.SampleRate * sizeof(float));
            PositionBytes -= PositionBytes % AudioPlayer.ChunkSize;
        }
    }

    /// <summary>
    ///     How long this sound plays for, in seconds.
    /// </summary>
    public double Duration => Buffer.Length / (double)(Buffer.Specifications.SampleRate * sizeof(float));

    /// <summary>
    ///     The playback direction of this sound.
    ///     One equates to forward direction, negative
    ///     one equates to backward direction.
    /// </summary>
    public int PlaybackDirection
    {
        get;
        set;
    } = 1;

    /// <summary>
    ///     Whether this sound has finished
    ///     playing or not.
    /// </summary>
    public bool FinishedPlaying
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The loop starting point of this
    ///     sound.
    /// </summary>
    public int LoopStartBytes
    {
        get;
        set;
    }

    /// <summary>
    ///     The loop ending  point of this
    ///     sound.
    /// </summary>
    public int LoopEndBytes
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this sound should loop or not.
    /// </summary>
    public bool Loop
    {
        get;
        set;
    }

    ~Sound()
    {
        Dispose(false);
    }

    public Sound(AudioBuffer buffer, float volume = 1f, bool reversed = false)
    {
        Buffer = buffer;
        Stream = new AudioStream(Buffer.Specifications, immediatelyStartPlayback: false)
        {
            Volume = volume
        };

        LoopEndBytes = (int)buffer.Length;
        CancellationTokenSource = new CancellationTokenSource();

        if (reversed)
        {
            PositionBytes = LoopEndBytes;
            PlaybackDirection = -1;
        }
    }

    /// <summary>
    ///     Advances this sound's playback position
    ///     and writes data into a byte buffer.
    /// </summary>
    public int ReadNextChunk(ref byte[] destination)
    {
        lock (readLock)
        {
            if (FinishedPlaying || Buffer.Handle == nint.Zero || disposed)
                return 0;

            int bytesRemaining = PlaybackDirection > 0
                ? LoopEndBytes - PositionBytes
                : PositionBytes - LoopStartBytes;

            if (bytesRemaining <= 0)
            {
                if (Loop)
                {
                    PositionBytes = PlaybackDirection > 0 ? LoopStartBytes : LoopEndBytes;
                    bytesRemaining = PlaybackDirection > 0
                        ? LoopEndBytes - PositionBytes
                        : PositionBytes - LoopStartBytes;
                }
                else
                {
                    FinishedPlaying = true;
                    return 0;
                }
            }

            int bytesToRead = int.Min(destination.Length, bytesRemaining);
            if (PlaybackDirection > 0)
            {
                // An evil problem has been plaguing the game for
                // a while now. Event viewer logs seem indicate that this
                // method is responsible, on account of the underlying
                // Marshal.Copy going past its legal bounds and receiving
                // a rude access violation exception.

                // I'm not entirely sure what the origin of the problem is.
                // Maybe something race condition related in the
                // position increment?
                // Either way, I'll just clamp the offset to prevent that
                // entirely. Testing seems to suggest it works.
                int playbackOffset = PositionBytes;
                int nextPlaybackOffset = playbackOffset + bytesToRead;
                if (nextPlaybackOffset > Buffer.Length)
                {
                    int difference = (int)Buffer.Length - nextPlaybackOffset;
                    playbackOffset += difference;
                }

                Marshal.Copy(Buffer.Handle + playbackOffset, destination, 0, bytesToRead);
                PositionBytes += bytesToRead;
            }
            else
            {
                int playbackOffset = PositionBytes - bytesToRead;
                int nextPlaybackOffset = playbackOffset + bytesToRead;
                if (nextPlaybackOffset > Buffer.Length)
                {
                    int difference = (int)Buffer.Length - nextPlaybackOffset;
                    playbackOffset += difference;
                }

                Marshal.Copy(Buffer.Handle + playbackOffset, destination, 0, bytesToRead);

                PositionBytes -= bytesToRead;
            }

            return bytesToRead;
        }
    }

    /// <summary>
    ///     Stops this sound.
    /// </summary>
    public void Stop()
    {
        CancellationTokenSource.Cancel();
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    public void Dispose(bool disposeBuffer)
    {
        if (disposed)
            return;

        disposed = true;

        CancellationTokenSource.Cancel();
        if (disposeBuffer)
            Buffer.Dispose();
        Stream.Dispose();
    }
}
