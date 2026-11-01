using System.Runtime.InteropServices;
using Northernlight.Core.Resources;

namespace Northernlight.Core.Audio;

/// <summary>
///     Represents a music player.
/// </summary>
public sealed class MusicPlayer
{
    private readonly List<Sound> tracks = [];

    /// <summary>
    ///     Whether this music player has been manually paused
    ///     or not.
    /// </summary>
    public bool ManuallyPaused
    {
        get;
        set;
    }

    /// <summary>
    ///     The active tracks handled by this music.
    /// </summary>
    public ReadOnlySpan<Sound> Tracks => CollectionsMarshal.AsSpan(tracks);

    /// <summary>
    ///     The audio player backing this music player.
    /// </summary>
    public AudioPlayer Player
    {
        get;
    }

    /// <summary>
    ///     The window that owns this music player.
    /// </summary>
    public Window Window
    {
        get;
    }

    public MusicPlayer(Window window)
    {
        Player = new AudioPlayer();
        Window = window;
    }

    /// <summary>
    ///     Updates this player.
    /// </summary>
    public void Update()
    {
        Player.Paused = Window is { IsFocused: false, PauseMusicOnLossOfFocus: true } || ManuallyPaused;
    }

    /// <summary>
    ///     Adds a music track to this player.
    /// </summary>
    public void Add(Sound music, bool autoLoop = true)
    {
        if (autoLoop)
            music.Loop = true;

        tracks.Add(music);
        Player.Play(music);
    }

    /// <summary>
    ///     Immediately stops all music.
    /// </summary>
    public void Stop()
    {
        foreach (Sound music in tracks)
            music.Dispose();

        tracks.Clear();
    }
}
