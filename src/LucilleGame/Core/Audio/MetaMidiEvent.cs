namespace Northernlight.Core.Audio;

/// <summary>
///     Represents a temporal MIDI file readback event
///     of the meta type.
/// </summary>
public sealed class MetaMidiEvent : IMidiEvent
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
