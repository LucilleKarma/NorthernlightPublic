using System.Diagnostics;

namespace Northernlight.Core;

/// <summary>
///     The system responsible for the handling of
///     game time.
/// </summary>
public sealed class Time
{
    private readonly Stopwatch watch = new Stopwatch();

    /// <summary>
    ///     The set of of all sub-timers maintained
    ///     by this main timekeeper.
    /// </summary>
    internal readonly List<WeakReference<OwnedTimer>> Timers = [];

    /// <summary>
    ///     How long the previous frame went on
    ///     for, in seconds, taking into account
    ///     <see cref="GlobalSpeed"/>.
    /// </summary>
    public float DeltaTime
    {
        get;
        internal set;
    }

    /// <summary>
    ///     How long the previous frame went on
    ///     for, in seconds, not taking into account
    ///     <see cref="GlobalSpeed"/>.
    /// </summary>
    public float RawDeltaTime
    {
        get;
        internal set;
    }

    /// <summary>
    ///     How long the game has been active
    ///     for, in seconds.
    /// </summary>
    public float ElapsedTime
    {
        get;
        private set;
    }

    /// <summary>
    ///     A global speed factor for all delta-time
    ///     based events under the influence of this
    ///     timer.
    /// </summary>
    public float GlobalSpeed
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     Executes an update frame and records
    ///     the amount of time that elapsed.
    /// </summary>
    internal void Record(Func<Stopwatch, bool> update, float targetFps)
    {
        float targetFrameTime = targetFps > 0f ? 1f / targetFps : 0f;

        watch.Restart();
        bool interrupted = !update(watch);

        if (interrupted)
        {
            RawDeltaTime = 0f;
            DeltaTime = 0f;
            return;
        }

        if (targetFrameTime > 0f)
        {
            while (watch.ElapsedTicks / (float)Stopwatch.Frequency < targetFrameTime)
            {
                float remainingMilliseconds = (targetFrameTime - (watch.ElapsedTicks / (float)Stopwatch.Frequency)) * 1000f;

                if (remainingMilliseconds >= 1.5f)
                    Thread.Sleep(1);
                else
                    Thread.SpinWait(10);
            }
        }

        watch.Stop();

        float actualElapsedSeconds = watch.ElapsedTicks / (float)Stopwatch.Frequency;
        float stutterLimit = 0.25f;
        RawDeltaTime = float.Min(actualElapsedSeconds, stutterLimit);
        DeltaTime = RawDeltaTime * GlobalSpeed;
        ElapsedTime += DeltaTime;

        for (int i = Timers.Count - 1; i >= 0; i--)
        {
            if (Timers[i].TryGetTarget(out OwnedTimer? timer))
                timer.Pass(this);
        }

        Timers.RemoveAll(t => !t.TryGetTarget(out _));
    }

    /// <summary>
    ///     Rents a timer out.
    /// </summary>
    public OwnedTimer Rent(bool ignoreGlobalSpeed = false)
    {
        OwnedTimer t = new OwnedTimer(ignoreGlobalSpeed);
        Timers.Add(new WeakReference<OwnedTimer>(t));

        return t;
    }
}
