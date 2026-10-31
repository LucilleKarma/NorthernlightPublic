using System.Buffers;

namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an audio player.
/// </summary>
public sealed class AudioPlayer
{
    /// <summary>
    ///     Whether this player is paused or not.
    /// </summary>
    public bool Paused
    {
        get;
        set;
    }

    /// <summary>
    ///     The global volume of this audio player.
    /// </summary>
    public float Volume
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The size of chunks to partition readback data
    ///     into when playing streams.
    /// </summary>
    public const int ChunkSize = 4096;

    /// <summary>
    ///     The maximum amount of data to allow into
    ///     the audio buffer.
    /// </summary>
    public const int MaxQueuedBytes = ChunkSize * 4;

    private async Task ProcessSoundAsync(Sound sound, CancellationToken token)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        try
        {
            while (!sound.FinishedPlaying && !token.IsCancellationRequested)
            {
                sound.Stream.PlayerVolumeFactor = Volume;
                sound.Stream.Paused = Paused;

                if (Paused)
                {
                    await Task.Delay(5, token);
                    continue;
                }

                int readCount = sound.ReadNextChunk(ref buffer);
                if (readCount <= 0)
                    break;

                while (sound.Stream.Capacity >= MaxQueuedBytes && !token.IsCancellationRequested)
                    await Task.Delay(3, token);

                // PIN YOUR MEMORY GRAAAAA!!!!
                unsafe
                {
                    fixed (byte* bufferPointer = &buffer[0])
                    {
                        sound.Stream.Submit((nint)bufferPointer, readCount);
                    }
                }

                await Task.Delay(1, token);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    ///     Plays a given sound.
    /// </summary>
    public void Play(Sound sound)
    {
        sound.Stream.Paused = false;
        CancellationTokenSource source = sound.CancellationTokenSource;

        Task.Run(() => ProcessSoundAsync(sound, source.Token));
    }

    /// <summary>
    ///     Plays a given sound from an audio buffer.
    /// </summary>
    public Sound Play(AudioBuffer buffer, float volume = 1f)
    {
        Sound sound = new Sound(buffer);
        sound.Stream.Volume = volume;

        Play(sound);

        return sound;
    }
}
