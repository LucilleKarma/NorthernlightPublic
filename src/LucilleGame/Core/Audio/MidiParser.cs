namespace Northernlight.Core.Audio;

/// <summary>
///     The dedicated MIDI file parser system.
/// </summary>
public static class MidiParser
{
    private const string ExpectedMidiHeaderId = "MThd";

    private const string ExpectedMidiChunkHeaderId = "MTrk";

    /// <summary>
    ///     Parses a MIDI file from a stream.
    /// </summary>
    public static MidiFile Parse(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);

        MidiFile file = new MidiFile();

        string chunkId = new string(reader.ReadChars(4));
        if (chunkId != ExpectedMidiHeaderId)
            throw new InvalidDataException("The header's data does not align with the expected magic string.");

        uint headerLength = ReadUInt32BigEndian(reader);
        if (headerLength < 6)
            throw new InvalidDataException("The header length is too small.");

        file.Format = (MidiFormat)ReadUInt16BigEndian(reader);
        file.TrackCount = ReadUInt16BigEndian(reader);
        file.PulsesPerQuarterNote = ReadUInt16BigEndian(reader);

        uint remainingHeaderData = headerLength - 6;
        if (remainingHeaderData >= 1)
            reader.ReadBytes((int)remainingHeaderData);

        for (int i = 0; i < file.TrackCount; i++)
            file.Tracks.Add(ParseTrack(reader));

        return file;
    }

    private static MidiTrack ParseTrack(BinaryReader reader)
    {
        string chunkId = new string(reader.ReadChars(4));
        if (chunkId != ExpectedMidiChunkHeaderId)
            throw new InvalidDataException("The header chunk's data does not align with the expected magic string.");

        uint length = ReadUInt32BigEndian(reader);
        long trackEndPos = reader.BaseStream.Position + length;

        MidiTrack track = new MidiTrack();

        byte runningStatus = 0;
        while (reader.BaseStream.Position < trackEndPos)
        {
            uint delta = ReadVariableLengthQuantity(reader);

            byte first = reader.ReadByte();

            byte status;
            bool isStatusByte = (first & 0x80) != 0;

            if (isStatusByte)
            {
                status = first;
                runningStatus = status;
            }
            else
            {
                if (runningStatus == 0)
                    throw new InvalidDataException("Running status is present used before any status byte.");

                status = runningStatus;
            }

            if (status == 0xFF)
            {
                byte metaType = isStatusByte ? reader.ReadByte() : first;
                uint metaDataLength = ReadVariableLengthQuantity(reader);
                byte[] data = reader.ReadBytes((int)metaDataLength);

                track.Events.Add(new MetaMidiEvent
                {
                    DeltaTime = delta,
                    Status = status,
                    Identifier = metaType,
                    Data = data.Select(d => (int)d).ToArray()
                });

                if (metaType == 0x2F)
                    break;
            }
            else if (status == 0xF0 || status == 0xF7)
            {
                uint systemExclusiveDataLength = ReadVariableLengthQuantity(reader);
                byte[] data = reader.ReadBytes((int)systemExclusiveDataLength);

                track.Events.Add(new SystemExclusiveMidiEvent
                {
                    DeltaTime = delta,
                    Status = status,
                    Data = data.Select(d => (int)d).ToArray()
                });
            }
            else
            {
                int statusHighNibble = status & 0xF0;
                byte channel = (byte)(status & 0x0F);

                byte data1;
                byte data2 = 0;

                if (isStatusByte)
                    data1 = reader.ReadByte();
                else
                    data1 = first;

                bool hasTwoDataBytes = statusHighNibble switch
                {
                    0xC0 => false, // Program Change.
                    0xD0 => false, // Channel Pressure.
                    _ => true   // Most others.
                };

                if (hasTwoDataBytes)
                    data2 = reader.ReadByte();

                track.Events.Add(new ChannelMidiEvent
                {
                    DeltaTime = delta,
                    Status = status,
                    Channel = channel,
                    Data = [data1, data2]
                });
            }
        }

        reader.BaseStream.Position = trackEndPos;

        return track;
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        byte b1 = reader.ReadByte();
        byte b2 = reader.ReadByte();
        return (ushort)((b1 << 8) | b2);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        byte b1 = reader.ReadByte();
        byte b2 = reader.ReadByte();
        byte b3 = reader.ReadByte();
        byte b4 = reader.ReadByte();
        return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
    }

    private static uint ReadVariableLengthQuantity(BinaryReader reader)
    {
        uint value = 0;
        byte b;
        do
        {
            b = reader.ReadByte();
            value = (value << 7) | (uint)(b & 0x7F);
        }
        while ((b & 0x80) != 0);

        return value;
    }
}
