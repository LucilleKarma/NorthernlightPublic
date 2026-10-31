using System.Runtime.InteropServices;

namespace Northernlight.Core.Audio;

/// <summary>
///     A format specifier for audio data.
/// </summary>
/// <since>This struct is available since SDL 3.2.0</since>
/// <seealso cref="AudioFormat"/>
[StructLayout(LayoutKind.Sequential)]
public struct AudioSpecifier
{
    /// <summary>
    ///     Audio data format.
    /// </summary>
    public AudioFormat Format;

    /// <summary>
    ///     Number of channels: 1 mono, 2 stereo, etc.
    /// </summary>
    public int Channels;

    /// <summary>
    ///     Sample rate: sample frames per second.
    /// </summary>
    public int SampleRate;
}
