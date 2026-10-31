namespace Northernlight.Core.Audio;

/// <summary>
///     Represents a temporal MIDI file readback event
///     of the system-exclusive type.
/// </summary>
public sealed class SystemExclusiveMidiEvent : IMidiEvent
{
    public uint DeltaTime
    {
        get;
        set;
    }

    public byte Status
    {
        get;
        set;
    }

    public byte Channel
    {
        get;
        set;
    }

    public byte Identifier
    {
        get;
        set;
    }

    public int[] Data
    {
        get;
        set;
    }
}
