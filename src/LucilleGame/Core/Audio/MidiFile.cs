namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an instance of a MIDI file.
/// </summary>
public sealed class MidiFile
{
    /// <summary>
    ///     The format of this file.
    /// </summary>
    public MidiFormat Format
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of tracks contained within this file.
    /// </summary>
    public ushort TrackCount
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of pulses per quarter note.
    ///     Used to indicate the rate of the MIDI file's
    ///     contents
    /// </summary>
    public ushort PulsesPerQuarterNote
    {
        get;
        set;
    }

    /// <summary>
    ///     The set of all tracks contained within this file.
    /// </summary>
    public readonly List<MidiTrack> Tracks = [];
}
