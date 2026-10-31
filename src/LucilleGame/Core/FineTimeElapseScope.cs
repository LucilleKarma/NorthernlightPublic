using System.Diagnostics;

namespace Northernlight.Core;

/// <summary>
///     A simple mechanism that allows for measuring
///     elapsed time in a given duration.
/// </summary>
public ref struct FineTimeElapseScope : IDisposable
{
    private bool disposed;

    private readonly long timestampStart;

    /// <summary>
    ///     How long it's been between the time
    ///     this scope was entered and exited.
    /// </summary>
    public TimeSpan Elapsed
    {
        get;
        private set;
    }

    public FineTimeElapseScope() => timestampStart = Stopwatch.GetTimestamp();

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        Elapsed = Stopwatch.GetElapsedTime(timestampStart);
    }
}
