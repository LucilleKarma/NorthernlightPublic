namespace Northernlight.Core.Audio;

/// <summary>
///     Represents a temporal MIDI file readback event.
/// </summary>
public interface IMidiEvent
{
    /// <summary>
    ///     The amount of elapsed time units between
    ///     this event and the last one.
    /// </summary>
    public uint DeltaTime
    {
        get;
        set;
    }

    /// <summary>
    ///     The status of this event.
    /// </summary>
    public byte Status
    {
        get;
        set;
    }

    /// <summary>
    ///     The identifier associated with this event.
    /// </summary>
    public byte Identifier
    {
        get;
        set;
    }

    /// <summary>
    ///     Local data associated with this event.
    /// </summary>
    public int[] Data
    {
        get;
        set;
    }
}
