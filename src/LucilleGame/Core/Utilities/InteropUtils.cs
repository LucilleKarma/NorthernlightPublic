using System.Runtime.InteropServices;
using System.Text;

namespace Northernlight.Core.Utilities;

/// <summary>
///     A utility class that provides various interoperability-oriented
///     operations.
/// </summary>
public static class InteropUtils
{
    private static unsafe byte* EncodeToUTF8Buffer(string s, out int length)
    {
        if (s is null)
        {
            length = 0;
            return null;
        }

        length = Encoding.UTF8.GetByteCount(s) + 1;
        byte* buffer = (byte*)NativeMemory.Alloc((nuint)length);
        Span<byte> span = new Span<byte>(buffer, length - 1);
        int byteCount = Encoding.UTF8.GetBytes(s, span);
        buffer[byteCount] = 0;

        return buffer;
    }

    /// <summary>
    ///     Encodes a string into a native buffer representation.
    ///     You <i>must</i> call NativeMemory.Free when this is no longer needed, in order
    ///     to prevent a memory leak.
    /// </summary>
    public static unsafe byte* EncodeToUTF8Buffer(string s) => EncodeToUTF8Buffer(s, out _);
}
