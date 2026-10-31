using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of an angle in 2D context.
/// </summary>
/// <remarks>
///     All internal data is handled in radians.
///     <br></br>
///     A <see langword="default"/> angle will have an
///     internal value of zero, and thus rest on
///     the horizontal at a direction of (1, 0).
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("Radians: {theta}, Degrees: {Degrees}")]
public readonly struct Angle :
    IEquatable<Angle>,
    IAdditionOperators<Angle, Angle, Angle>,
    ISubtractionOperators<Angle, Angle, Angle>,
    IUnaryNegationOperators<Angle, Angle>,
    IComparisonOperators<Angle, Angle, bool>
{
    public enum InputMode
    {
        Radians,
        Degrees,
    }

    /// <summary>
    ///     The internal representation of this angle, as
    ///     a measure of radians.
    /// </summary>
    private readonly float theta;

    /// <summary>
    ///     The value of this angle, in degrees.
    /// </summary>
    [JsonIgnore]
    public readonly float Degrees => theta * 57.2957795f;

    // I did briefly consider precalculating this
    // in the constructor and using an encapsulating
    // getter around a private Vector2, but honestly
    // considering this type is used on many
    // occasions as an implicit conversion from floats
    // with only a few of those cases needing the unit
    // vector I dunno if it'd be the wisest to calculate
    // (co)sines every single time that transient
    // conversion occurs.
    /// <summary>
    ///     The unit direction vector of this angle.
    /// </summary>
    [JsonIgnore]
    public readonly Vector2 Unit
    {
        get
        {
            (float s, float c) = float.SinCos(theta);
            return new Vector2(c, s);
        }
    }

    /// <summary>
    ///     The value of this angle, in radians.
    /// </summary>
    public readonly float Radians => theta;

    /// <summary>
    ///     A zero angle.
    /// </summary>
    public static readonly Angle Zero = new Angle(0f);

    /// <summary>
    ///     An eighth turn.
    /// </summary>
    public static readonly Angle PiOver4 = new Angle(float.Pi * 0.25f);

    /// <summary>
    ///     A quarter turn.
    /// </summary>
    public static readonly Angle PiOver2 = new Angle(float.Pi * 0.5f);

    /// <summary>
    ///     A half turn.
    /// </summary>
    public static readonly Angle Pi = new Angle(float.Pi);

    /// <summary>
    ///     A full turn.
    /// </summary>
    public static readonly Angle Tau = new Angle(float.Tau * 0.99999f);

    public Angle(float angle, InputMode mode = InputMode.Radians)
    {
        if (mode == InputMode.Degrees)
            angle *= 0.0174533f;

        theta = angle % float.Tau;
        if (theta < 0f)
            theta += float.Tau;
    }

    public Angle(Vector2 direction)
    {
        theta = float.Atan2(direction.Y, direction.X);
        if (theta < 0f)
            theta += float.Tau;
    }

    public bool Equals(Angle other) => theta == other.theta;

    public override bool Equals(object? obj) => obj is Angle other && Equals(other);

    public override int GetHashCode() => theta.GetHashCode();

    /// <summary>
    ///     Steps this angle towards another angle
    ///     at a constant rate and returns the
    ///     resulting angle.
    /// </summary>
    public Angle StepTowards(Angle other, float stepRate)
    {
        static float WrapAngle(float angle)
        {
            if (angle > -Pi && angle <= Pi)
                return angle;

            angle %= float.Tau;
            if (angle <= -Pi)
                return angle + float.Tau;

            if (angle > Pi)
                return angle - float.Tau;

            return angle;
        }

        float currentAngle = WrapAngle(theta);
        float targetAngle = WrapAngle(other.theta);

        if (currentAngle < targetAngle)
        {
            if (targetAngle - currentAngle > float.Pi)
                currentAngle += float.Tau;
        }
        else if (currentAngle - targetAngle > float.Pi)
            currentAngle -= float.Tau;

        currentAngle += float.Clamp(targetAngle - currentAngle, -stepRate, stepRate);

        return new Angle(currentAngle);
    }

    public static bool operator ==(Angle left, Angle right) => left.Equals(right);

    public static bool operator !=(Angle left, Angle right) => !(left == right);

    public static bool operator >(Angle left, Angle right) => left.theta > right.theta;

    public static bool operator >=(Angle left, Angle right) => left.theta >= right.theta;

    public static bool operator <(Angle left, Angle right) => left.theta < right.theta;

    public static bool operator <=(Angle left, Angle right) => left.theta <= right.theta;

    public static Angle operator +(Angle a, float b) => new Angle(a.theta + b);

    public static Angle operator +(Angle a, Angle b) => new Angle(a.theta + b.theta);

    public static Angle operator -(Angle a, float b) => new Angle(a.theta - b);

    public static Angle operator -(Angle a, Angle b) => new Angle(a.theta - b.theta);

    public static Angle operator -(Angle angle) => new Angle(-angle.theta);

    public static implicit operator float(Angle a) => a.theta;

    public static implicit operator Angle(float radians) => new Angle(radians);

    public static implicit operator Angle(Vector2 direction) => new Angle(direction);
}
