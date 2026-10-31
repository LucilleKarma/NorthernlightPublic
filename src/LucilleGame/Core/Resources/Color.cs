using System.Numerics;
using System.Runtime.InteropServices;
using SDL3;

namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an instance of a color encoded
///     in an float-packed RGBA format.
/// </summary>
/// <remarks>
///     RGB values may exceed their traditional 0-1 range if
///     desired.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Color :
    IEquatable<Color>,
    IAdditionOperators<Color, Color, Color>,
    ISubtractionOperators<Color, Color, Color>,
    IMultiplyOperators<Color, Color, Color>,
    IMultiplyOperators<Color, float, Color>
{
    /// <summary>
    ///     The red channel.
    /// </summary>
    public readonly float R;

    /// <summary>
    ///     The green channel.
    /// </summary>
    public readonly float G;

    /// <summary>
    ///     The blue channel.
    /// </summary>
    public readonly float B;

    /// <summary>
    ///     The alpha channel.
    /// </summary>
    public readonly float A;

    /// <summary>
    ///     A white color.
    /// </summary>
    public static readonly Color White = new Color(1f, 1f, 1f, 1f);

    /// <summary>
    ///     A black color.
    /// </summary>
    public static readonly Color Black = new Color(0f, 0f, 0f, 1f);

    /// <summary>
    ///     A transparent color.
    /// </summary>
    public static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

    public Color(int r, int g, int b) : this(r / 255f, g / 255f, b / 255f)
    { }

    public Color(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
        A = 1f;
    }

    public Color(float r, float g, float b, float a) :
        this(r, g, b)
    {
        A = float.Clamp(a, 0f, 1f);
    }

    public Color(float brightness)
    {
        R = brightness;
        G = brightness;
        B = brightness;
        A = 1f;
    }

    /// <summary>
    ///     Linearly interpolates between two colors.
    /// </summary>
    public static Color Lerp(Color colorA, Color colorB, float interpolant)
    {
        float r = float.Lerp(colorA.R, colorB.R, interpolant);
        float g = float.Lerp(colorA.G, colorB.G, interpolant);
        float b = float.Lerp(colorA.B, colorB.B, interpolant);
        float a = float.Lerp(colorA.A, colorB.A, interpolant);
        return new Color(r, g, b, a);
    }

    /// <summary>
    ///     Linearly interpolates between multiple colors.
    /// </summary>
    public static Color MultiLerp(ReadOnlySpan<Color> colors, float interpolant)
    {
        if (colors.Length <= 0)
            return default;
        if (colors.Length == 1)
            return colors[0];
        if (colors.Length == 2)
            return Lerp(colors[0], colors[1], interpolant);

        if (interpolant <= 0f)
            return colors[0];
        if (interpolant >= 1f)
            return colors[^1];

        int startingIndex = (int)float.Floor((colors.Length - 1f) * interpolant);
        startingIndex = int.Clamp(startingIndex, 0, colors.Length - 2);
        int endingIndex = startingIndex + 1;

        float localInterpolant = (colors.Length - 1f) * interpolant % 1f;
        return Lerp(colors[startingIndex], colors[endingIndex], localInterpolant);
    }

    public bool Equals(Color other)
    {
        return R == other.R && G == other.G &&
               B == other.B && A == other.A;
    }

    public static Color operator +(Color left, Color right)
    {
        return new Color(left.R + right.R, left.G + right.G, left.B + right.B, float.Max(left.A, right.A));
    }

    public static Color operator -(Color left, Color right)
    {
        return new Color(left.R - right.R, left.G - right.G, left.B - right.B, float.Min(left.A, right.A));
    }

    public static Color operator *(Color left, Color right)
    {
        return new Color(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    public static Color operator *(Color left, float right)
    {
        return new Color(left.R * right, left.G * right, left.B * right, left.A * right);
    }

    public static bool operator ==(Color left, Color right) => Equals(left, right);

    public static bool operator !=(Color left, Color right) => !(left == right);

    /// <summary>
    ///     Converts this color to an SDL-structure format.
    /// </summary>
    public SDL.FColor AsSdl() => new SDL.FColor(R, G, B, A);

    /// <summary>
    ///     Converts this color to an 4D vector format.
    /// </summary>
    public Vector4 AsVector4() => new Vector4(R, G, B, A);

    public override bool Equals(object? obj) => obj is Color color && Equals(color);

    public override int GetHashCode() => R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
}
