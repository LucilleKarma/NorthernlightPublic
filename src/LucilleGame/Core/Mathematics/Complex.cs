using System.Numerics;
using Northernlight.Core.Utilities;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of a complex number.
/// </summary>
/// <remarks>
///     Internally, this type is represented as a 2D vector, whose
///     X component is the real part, and whose Y component is the imaginary
///     part.
/// </remarks>
public struct Complex
{
    private Vector2 value;

    /// <summary>
    ///     The real part of this complex number.
    /// </summary>
    public float Real
    {
        readonly get => value.X;
        set => this.value.X = value;
    }

    /// <summary>
    ///     The imaginary part of this complex number.
    /// </summary>
    public float Imaginary
    {
        readonly get => value.Y;
        set => this.value.Y = value;
    }

    /// <summary>
    ///     The length of this complex number.
    /// </summary>
    public readonly float Length => value.Length();

    /// <summary>
    ///     The imaginary unit.
    /// </summary>
    public static readonly Complex ImaginaryUnit = new Complex(0f, 1f);

    public Complex(in float real, float imaginary) => value = new Vector2(real, imaginary);

    public Complex(in float real) => value = new Vector2(real, 0f);

    public Complex(in Vector2 realImaginary) => value = realImaginary;

    public static Complex operator *(in Complex a, in float b) => new Complex(a.value * b);

    public static Complex operator /(in Complex a, in float b) => new Complex(a.value / b);

    public static Complex operator +(in Complex a, in Complex b) => new Complex(a.value + b.value);

    public static Complex operator -(in Complex a, in Complex b) => new Complex(a.value - b.value);

    public static Complex operator -(in Complex a) => new Complex(-a.value);

    public static Complex operator *(in Complex ca, in Complex cb)
    {
        float a = ca.Real;
        float b = ca.Imaginary;
        float c = cb.Real;
        float d = cb.Imaginary;
        return new Complex(a * c - b * d, a * d + b * c);
    }

    public static Complex operator /(in Complex ca, in Complex cb)
    {
        float a = ca.Real;
        float b = ca.Imaginary;
        float c = cb.Real;
        float d = cb.Imaginary;
        float denominator = c.Squared() + d.Squared();
        return new Complex(a * c + b * d, b * c - a * d) / denominator;
    }

    public static implicit operator Complex(in float a) => new Complex(a);

    /// <summary>
    ///     Calculates the square root of a given real number,
    ///     returning a complex number instead of a NaN real number
    ///     in the negative case.
    /// </summary>
    public static Complex SqrtReal(in float a)
    {
        float squareRoot = float.Sqrt(float.Abs(a));
        if (a < 0f)
            return new Complex(0f, squareRoot);

        return new Complex(squareRoot);
    }

    /// <summary>
    ///     Calculates and returns the natural exponential
    ///     of a given complex number.
    /// </summary>
    public static Complex Exp(in Complex a)
    {
        float expReal = float.Exp(a.Real);
        (float sinImaginary, float cosImaginary) = float.SinCos(a.Imaginary);
        return new Complex(cosImaginary, sinImaginary) * expReal;
    }

    /// <summary>
    ///     Calculates and returns the hyperbolic sine
    ///     of a given complex number.
    /// </summary>
    public static Complex Sinh(in Complex a)
    {
        float real = float.Sinh(a.Real) * float.Cos(a.Imaginary);
        float imaginary = float.Cosh(a.Real) * float.Sin(a.Imaginary);
        return new Complex(real, imaginary);
    }

    /// <summary>
    ///     Calculates and returns the hyperbolic cosine
    ///     of a given complex number.
    /// </summary>
    public static Complex Cosh(in Complex a)
    {
        float real = float.Cosh(a.Real) * float.Cos(a.Imaginary);
        float imaginary = float.Sinh(a.Real) * float.Sin(a.Imaginary);
        return new Complex(real, imaginary);
    }

    public override readonly string ToString()
    {
        if (value.Y < 0f)
            return $"{value.X} - {-value.Y}i";

        return $"{value.X} + {value.Y}i";
    }
}
