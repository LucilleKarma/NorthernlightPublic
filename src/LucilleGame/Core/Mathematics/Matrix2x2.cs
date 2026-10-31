using System.Numerics;
using System.Runtime.InteropServices;
using Northernlight.Core.Utilities;

namespace Northernlight.Core.Mathematics;

/// <summary>
///     Represents an instance of a 2x2 matrix.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Matrix2x2
{
    private Vector2 rowA;

    private Vector2 rowB;

    /// <summary>
    ///     The first element of the first row.
    /// </summary>
    public float M11
    {
        readonly get => rowA.X;
        set => rowA.X = value;
    }

    /// <summary>
    ///     The second element of the first row.
    /// </summary>
    public float M12
    {
        readonly get => rowA.Y;
        set => rowA.Y = value;
    }

    /// <summary>
    ///     The first element of the second row.
    /// </summary>
    public float M21
    {
        readonly get => rowB.X;
        set => rowB.X = value;
    }

    /// <summary>
    ///     The second element of the second row.
    /// </summary>
    public float M22
    {
        readonly get => rowB.Y;
        set => rowB.Y = value;
    }

    /// <summary>
    ///     The determinant of this matrix.
    /// </summary>
    public readonly float Determinant => M11 * M22 - M12 * M21;

    /// <summary>
    ///     The trace of this matrix.
    /// </summary>
    public readonly float Trace => M11 + M22;

    /// <summary>
    ///     The first eigenvalue of this matrix.
    /// </summary>
    public readonly Complex EigenvalueA => CalculateEigenvalue(-1f);

    /// <summary>
    ///     The second eigenvalue of this matrix.
    /// </summary>
    public readonly Complex EigenvalueB => CalculateEigenvalue(1f);

    /// <summary>
    ///     The identity matrix.
    /// </summary>
    public static readonly Matrix2x2 Identity = new Matrix2x2(1f, 0f, 0f, 1f);

    public Matrix2x2(float a, float b, float c, float d)
    {
        M11 = a;
        M12 = b;
        M21 = c;
        M22 = d;
    }

    public Matrix2x2(Vector2 rowA, Vector2 rowB)
    {
        this.rowA = rowA;
        this.rowB = rowB;
    }

    private readonly Complex CalculateEigenvalue(float side)
    {
        float characteristicA = 1f;
        float characteristicB = -Trace;
        float characteristicC = Determinant;
        float discriminant = characteristicB.Squared() - characteristicA * characteristicC * 4f;

        return (-characteristicB + Complex.SqrtReal(discriminant) * side) / (characteristicA * 2f);
    }

    public static Matrix2x2 operator +(in Matrix2x2 a, in Matrix2x2 b)
    {
        return new Matrix2x2(a.rowA + b.rowA, a.rowB + b.rowB);
    }

    public static Matrix2x2 operator -(in Matrix2x2 a, in Matrix2x2 b)
    {
        return new Matrix2x2(a.rowA - b.rowA, a.rowB - b.rowB);
    }

    public static Matrix2x2 operator *(in Matrix2x2 a, in float scalar)
    {
        return new Matrix2x2(a.rowA * scalar, a.rowB * scalar);
    }

    public static Matrix2x2 operator /(in Matrix2x2 a, in float scalar)
    {
        return new Matrix2x2(a.rowA / scalar, a.rowB / scalar);
    }

    public static Vector2 operator *(in Matrix2x2 matrix, in Vector2 v)
    {
        float x = Vector2.Dot(v, matrix.rowA);
        float y = Vector2.Dot(v, matrix.rowB);
        return new Vector2(x, y);
    }

    public static Matrix2x2 operator *(in Matrix2x2 matrixA, in Matrix2x2 matrixB)
    {
        float a = matrixA.M11;
        float b = matrixA.M12;
        float c = matrixA.M21;
        float d = matrixA.M22;
        float e = matrixB.M11;
        float f = matrixB.M12;
        float g = matrixB.M21;
        float h = matrixB.M22;
        return new Matrix2x2(a * e + b * g, a * f + b * h,
                             c * e + d * g, c * f + d * h);
    }

    /// <summary>
    ///     Calculates the matrix exponential
    ///     of a given matrix with a given optional 
    ///     scalar coefficient, in accordance with
    ///     reductions from the Cayley-Hamilton theorem.
    /// </summary>
    public static Matrix2x2 Exp(in Matrix2x2 matrix, in float t = 1f)
    {
        // The Cayley-Hamilton theorem in the case of a 2x2 matrix
        // states that for any matrix, M, its square can be reduced down
        // to a * I + b * M, where I is the identity matrix and a and b
        // are scalar coefficients.
        // This rule holds for higher integer powers of the matrix, just
        // with changes in the a and b coefficients.

        // For a matrix exponential, which is normally defined via
        // a Taylor series of repeated matrix powers, this means that it
        // can be reduced down to the following:

        // exp(t * M) = a(t) * I + b(t) * M

        // Assuming lambda_a and lambda_b are the
        // two eigenvalues of the original matrix, the values of a and b
        // for the exponential can be calculated thusly through the
        // following derivations:

        // Since the exponential must agree on eigenspaces,
        // exp(t * lambda_a) = a + b * lambda_a
        // exp(t * lambda_b) = a + b * lambda_b

        // Reducing this results in the following definition,
        // after some work:
        // b = (exp(t * lambda_a) - exp(t * lambda_b)) / (lambda_a - lambda_b)
        // a = (lambda_a * exp(t * lambda_b) - lambda_b * exp(t * lambda_a)) / (lambda_a - lambda_b)

        // There's just one wrinkle left to work out, that being the denominator.
        // If the eigenvalues are extremely close to each other, this can
        // cause numerical instability or division by zero issues in practice.

        // In order to avoid this, we'll start by assuming
        // the following intermediate variables:
        // c = (lambda_a + lambda_b) / 2
        // d = (lambda_a - lambda_b) / 2

        // The eigenvalues can be rewritten thusly:
        // lambda_a = c + d
        // lambda_b = c - d

        // Substituting this into the equation for b
        // yields the following formulation:
        // b = (exp(t * (c + d)) - exp(t * (c - d))) / (d * 2)
        // b = exp(t * c) * (exp(t * d) - exp(-t * d)) / (d * 2)
        // b = exp(t * c) * sinh(t * d) / d

        // A similar, albeit slightly longer process can be done
        // for a, resulting in
        // a = exp(t * c) * (cosh(t * d) - c * sinh(t * d) / d)

        // It should be noted that these definitions do a lot of heavy
        // lifting via the complex exponential, accounting for exponential and
        // periodic branching cases that would otherwise have to be
        // messily accounted for by hand in this code.
        // This is not required in the final expression of a(t) * I + b(t) * M, though.
        // Under the hood all that should be needed is the real part of the
        // complex values for that.

        Complex eigenvalueA = matrix.EigenvalueA;
        Complex eigenvalueB = matrix.EigenvalueB;
        Complex c = (eigenvalueA + eigenvalueB) * 0.5f;
        Complex d = (eigenvalueA - eigenvalueB) * 0.5f;
        Complex commonCoefficient = Complex.Exp(t * c);

        Complex td = t * d;
        Complex sinhcTd;
        Complex coshTd;
        if (td.Length < 1e-6f)
        {
            // Just a trivial, shortly truncated Taylor series approximation
            // of sinh(x) / x.
            // Since this branch is definitionally nearly zero
            // it should suffice fine.
            Complex tdSquared = td * td;
            sinhcTd = 1f + tdSquared / 6f + tdSquared * tdSquared / 120f;

            // Also an equivalent Taylor series truncation for cosh, too.
            // We're already extremely close to zero, might as well
            // take advantage of the conditions for some performance gains
            // while we're here!
            coshTd = 1f + tdSquared * 0.5f + tdSquared * tdSquared / 24f;
        }
        else
        {
            sinhcTd = Complex.Sinh(td) / td;
            coshTd = Complex.Cosh(td);
        }

        Complex a = commonCoefficient * (coshTd - c * sinhcTd * t);
        Complex b = commonCoefficient * sinhcTd * t;

        return Identity * a.Real + matrix * b.Real;
    }

    /// <summary>
    ///     Calculates and returns the inverse of a
    ///     matrix, provided that it is invertible.
    /// </summary>
    public static bool TryInvert(in Matrix2x2 matrix, out Matrix2x2 result)
    {
        if (float.Abs(matrix.Determinant) < 1e-6f)
        {
            result = Identity;
            return false;
        }

        float inverseDeterminant = 1f / matrix.Determinant;
        result = new Matrix2x2(matrix.M22, -matrix.M12, -matrix.M21, matrix.M11) * inverseDeterminant;
        return true;
    }

    public override readonly string ToString() => $"[a: {rowA.X}, b: {rowA.Y}] [c: {rowB.X}, d: {rowB.Y}]";
}
