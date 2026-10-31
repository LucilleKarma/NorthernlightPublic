namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an instance of a MIDI file track.
/// </summary>
public sealed class MidiTrack
{
    /// <summary>
    ///     The set of all events associated with this track.
    /// </summary>
    public readonly List<IMidiEvent> Events = [];
}
