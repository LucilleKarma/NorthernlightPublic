namespace Northernlight.Core;

/// <summary>
///     An owned sub-timer which elapses along with a <see cref="Time"/> instance.
/// </summary>
public sealed class OwnedTimer
{
    private float time;

    private readonly bool ignoreGlobalSpeed;

    /// <summary>
    ///     The amount of time that has elapsed for this
    ///     time, in seconds.
    /// </summary>
    public float Value => time;

    internal OwnedTimer(bool ignoreGlobalSpeed = false)
    {
        this.ignoreGlobalSpeed = ignoreGlobalSpeed;
    }

    /// <summary>
    ///     Makes time pass for this timer.
    /// </summary>
    internal void Pass(Time time) => this.time += ignoreGlobalSpeed ? time.RawDeltaTime : time.DeltaTime;

    /// <summary>
    ///     Performs a cycle check, resetting the timer back
    ///     to zero if a certain amount of time
    ///     has elapsed.
    /// </summary>
    public bool Cycle(float frequencySeconds)
    {
        if (time >= frequencySeconds)
        {
            Reset();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Performs a cycle check, decrementing the timer down towards
    ///     zero if a certain amount of time has elapsed.
    /// </summary>
    public bool CycleWhile(float frequencySeconds)
    {
        if (time >= frequencySeconds)
        {
            time -= frequencySeconds;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Resets this timer's internal value to zero.
    /// </summary>
    public void Reset() => time = 0f;

    /// <summary>
    ///     Returns whether a given amount of time, in
    ///     seconds, has elapsed or not since the last
    ///     cycle.
    /// </summary>
    public bool Elapsed(float seconds) => time >= seconds;

    /// <summary>
    ///     Manually sets the underlying time value.
    /// </summary>
    public void Set(float time) => this.time = time;

    public static implicit operator float(OwnedTimer timer) => timer.Value;
}
