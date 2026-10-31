namespace Northernlight.Core.Audio;

/// <summary>
///     Represents an audio data format.
/// </summary>
public enum AudioFormat : uint
{
    /// <summary>
    ///     Unspecified audio format.
    /// </summary>
    Unknown = 0x0000u,

    /// <summary>
    ///     Unsigned 8-bit samples.
    /// </summary>
    AudioU8 = 0x0008u,

    /// <summary>
    ///     Signed 8-bit samples.
    /// </summary>
    AudioS8 = 0x8008u,

    /// <summary>
    ///     Signed 16-bit samples.
    /// </summary>
    AudioS16LE = 0x8010u,

    /// <summary>
    ///     As above, but big-endian byte order.
    /// </summary>
    AudioS16BE = 0x9010u,

    /// <summary>
    ///     32-bit integer samples
    /// </summary>
    AudioS32LE = 0x8020u,

    /// <summary>
    ///     As above, but big-endian byte order.
    /// </summary>
    AudioS32BE = 0x9020u,

    /// <summary>
    ///     32-bit floating point samples.
    /// </summary>
    AudioF32LE = 0x8120u,

    /// <summary>
    ///     As above, but big-endian byte order.
    /// </summary>
    AudioF32BE = 0x9120u,
}
