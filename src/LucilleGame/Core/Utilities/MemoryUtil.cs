using System.Runtime.InteropServices;
using SDL3;

namespace Northernlight.Core.Utilities;

public static class MemoryUtil
{
    public ref struct DeferFreeGlobalHandler(nint handle) : IDisposable
    {
        public void Dispose()
        {
            if (handle == 0)
            {
                return;
            }

            Marshal.FreeHGlobal(handle);
        }
    }

    public ref struct DeferFreeSdlHandler(nint handle) : IDisposable
    {
        public void Dispose()
        {
            if (handle == 0)
            {
                return;
            }

            SDL.Free(handle);
        }
    }

    /// <summary>
    ///     Guarantees the <paramref name="handle"/> will be released at the end
    ///     of the scope.  This frees from global heap memory allocated by
    ///     <see cref="Marshal.AllocHGlobal(nint)"/>.  These allocations may
    ///     occur during marshalling of C# types.
    /// </summary>
    public static DeferFreeGlobalHandler DeferFreeGlobal(nint handle)
    {
        return new DeferFreeGlobalHandler(handle);
    }

    /// <summary>
    ///     Guarantees the <paramref name="handle"/> will be released at the end
    ///     of the scope.  This frees from global heap memory allocated by SDL
    ///     itself.
    /// </summary>
    public static DeferFreeSdlHandler DeferFreeSdl(nint handle)
    {
        return new DeferFreeSdlHandler(handle);
    }
}
