using System.Diagnostics.CodeAnalysis;

namespace Northernlight.Core.Physics;

/// <summary>
///     A compact representation of motion characteristic system settings.
/// </summary>
public readonly record struct MotionSystemSettings
{
    private readonly float frequency;

    /// <summary>
    ///     The frequency of the system. Determines how rapid oscillatory behavior should be.
    /// </summary>
    public required float Frequency
    {
        get => frequency;
        init => frequency = float.Max(value, SecondOrderDynamics.FrequencyClampLimit);
    }

    /// <summary>
    ///     Determines how rapidly extraneous changes, such as
    ///     overshoot or oscillatory motion, decay to the resting value.
    /// </summary>
    public required float Damping
    {
        get;
        init;
    }

    /// <summary>
    ///     Determines how suddenly changes are responded to.
    /// </summary>
    public required float InitialResponse
    {
        get;
        init;
    }

    /// <summary>
    ///     The default system settings for a (mostly) unaltered system.
    ///     This almost (but not exactly!) maps to the baseline movement, with
    ///     minimal easing applied.
    /// </summary>
    public static readonly MotionSystemSettings Default = new MotionSystemSettings(2.5f, 1f, 0f);

    /// <summary>
    ///     Standard settings for a typical ease-out kind of motion, whereupon
    ///     motion starts immediately before gently leveling out to
    ///     changes.
    /// </summary>
    public static readonly MotionSystemSettings StandardEaseOut = new MotionSystemSettings(0.2f, 0.85f, 1f);

    /// <summary>
    ///     Standard settings for a typical ease-in-out kind of motion, whereupon
    ///     motion takes a moment to speed up and settle down but accelerates
    ///     to a natural pace once moving.
    /// </summary>
    public static readonly MotionSystemSettings StandardEaseInOut = new MotionSystemSettings(0.21f, 1f, 0f);

    /// <summary>
    ///     Standard settings for an elastic ease-in-out kind of motion, whereupon
    ///     motion accelerates and decelerates naturally, but oscillates rapidly in
    ///     response to sudden changes. Useful for jerky robotic motion.
    /// </summary>
    public static readonly MotionSystemSettings ElasticEaseInOut = new MotionSystemSettings(4.32f, 0.24f, 23f);

    /// <summary>
    ///     Standard settings for a bouncy ease-in-out kind of motion, whereupon
    ///     motion accelerates and decelerates naturally, but oscillates about once
    ///     in anticipation or approach of a given destination. Useful for
    ///     rubber-banding motion.
    /// </summary>
    public static readonly MotionSystemSettings BounceEaseInOut = new MotionSystemSettings(1.32f, 0.85f, 15.6f);

    [SetsRequiredMembers]
    private MotionSystemSettings(float frequency, float damping, float initialResponse)
    {
        Frequency = frequency;
        Damping = damping;
        InitialResponse = initialResponse;
    }

    /// <summary>
    ///     Applies the contents of these settings to a system.
    /// </summary>
    public void ApplyTo(MotionCharacteristicImbuer system)
    {
        system.Solver.Frequency = Frequency;
        system.Solver.Damping = Damping;
        system.Solver.InitialResponse = InitialResponse;
        system.InUse = true;
    }

    /// <summary>
    ///     Interpolates between two setting states.
    /// </summary>
    public static MotionSystemSettings Lerp(MotionSystemSettings a, MotionSystemSettings b, float interpolant, bool useBetterFrequencyInterpolation = true)
    {
        float frequency;
        if (useBetterFrequencyInterpolation)
        {
            // Frequency perception tends to be logarithmic, not
            // linear.
            // It can thusly be better to rethink the interpolation
            // to be in the following format:

            // exp(lerp(ln(a), ln(b), t))

            // This will preserve the interpolation keyframe meetings, but
            // reach through them in accordance with exponential
            // interpolation instead, and better respect the aforementioned
            // property.

            // There is, however, a bit of algebratic work that
            // can be done to reduce this and make it simpler to
            // calculate:

            // exp(lerp(ln(a), ln(b), t)) =
            // exp(ln(a) * (1 - t) + ln(b) * t) =
            // exp(ln(a) * (1 - t)) * exp(ln(b) * t) =
            // a ^ (1 - x) * b ^ t
            frequency = float.Pow(a.Frequency, 1f - interpolant) * float.Pow(b.Frequency, interpolant);
        }
        else
            frequency = float.Lerp(a.Frequency, b.Frequency, interpolant);

        float damping = float.Lerp(a.Damping, b.Damping, interpolant);
        float initialResponse = float.Lerp(a.InitialResponse, b.InitialResponse, interpolant);
        return new MotionSystemSettings(frequency, damping, initialResponse);
    }
}
