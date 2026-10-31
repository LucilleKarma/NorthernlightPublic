using System.Diagnostics;
using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Core.Physics;

/// <summary>
///     A representation of a system that solves a
///     second order differential equation of the following form:
/// 
///     <br></br>
///     <br></br>
/// 
///     y + k1 * y' + k2 * y'' = x + k3 * x'
///     
///     <br></br>
///     <br></br>
///     Where y is the output as a consequence of the coefficients, and
///     x is the baseline input position (and naturally, x' the velocity).
/// </summary>
public sealed class SecondOrderDynamics
{
    private float k1;

    private float k2;

    private float k3;

    private float frequency;

    private float damping;

    private float initialResponse;

    private Matrix2x2 systemMatrix;

    private Matrix2x2 systemMatrixInverse;

    private Matrix2x2 systemMatrixInverseSquared;

    private StoredExponentialMatrixState previousExponentialMatrixState;

    /// <summary>
    ///     The current position of the system, known
    ///     mathematically as y in the original equation.
    /// </summary>
    public Vector3 Position
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The current velocity of the system, known
    ///     mathematically as y' in the original equation.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The frequency of the system.
    ///     Determines how rapid oscillatory behavior
    ///     should be, as well as how rapidly changes
    ///     in motion are responded to.
    /// </summary>
    /// <remarks>
    ///     To prevent NaN issues, this value is internally
    ///     clamped if set to a value less than the <see cref="FrequencyClampLimit"/>.
    /// </remarks>
    public float Frequency
    {
        get => frequency;
        set
        {
            value = float.Max(FrequencyClampLimit, value);
            if (float.Abs(frequency - value) >= 0.00001f)
                RecalculateInternalCoefficients(value, damping, initialResponse);
        }
    }

    /// <summary>
    ///     The damping of the system.
    ///     Determines how rapidly extraneous changes, such
    ///     as overshoot or oscillatory motion, decay to
    ///     the resting value.
    /// </summary>
    public float Damping
    {
        get => damping;
        set
        {
            if (float.Abs(damping - value) >= 0.00001f)
                RecalculateInternalCoefficients(frequency, value, initialResponse);
        }
    }

    /// <summary>
    ///     Determines how sudden changes are responded to.
    ///     Positive values less than one cause immediate changes
    ///     towards the target.
    ///     <br></br>
    /// 
    ///     Positive values greater than one cause overshooting.
    ///     <br></br>
    ///     
    ///     Negative values cause anticipatory motion.
    /// </summary>
    public float InitialResponse
    {
        get => initialResponse;
        set
        {
            if (float.Abs(initialResponse - value) >= 0.00001f)
                RecalculateInternalCoefficients(frequency, damping, value);
        }
    }

    /// <summary>
    ///     The clamping point at which frequency inputs
    ///     get internally clamped for system safety.
    /// </summary>
    public const float FrequencyClampLimit = 0.001f;

    /// <summary>
    ///     Represents an instance of a stored state
    ///     for the underlying exponential matrix and
    ///     corresponding time step.
    /// </summary>
    public readonly record struct StoredExponentialMatrixState(Matrix2x2 ExponentialState, float DeltaTime);

    public SecondOrderDynamics(Vector3 startingPosition, Vector3 initialVelocity, float frequency, float damping, float initialResponse)
    {
        RecalculateInternalCoefficients(frequency, damping, initialResponse);

        Position = startingPosition;
        Velocity = initialVelocity;
    }

    private void RecalculateInternalCoefficients(float frequency, float damping, float initialResponse)
    {
        this.frequency = float.Max(FrequencyClampLimit, frequency);
        this.damping = damping;
        this.initialResponse = initialResponse;

        k1 = damping / (float.Pi * frequency);
        k2 = 1f / (float.Tau * frequency).Squared();
        k3 = initialResponse * damping / (float.Tau * frequency);

        // Refer to the Update method for in-depth reasoning
        // behind this formulation.
        systemMatrix = new Matrix2x2(
            0f, 1f,
            -1f / k2, -k1 / k2
        );

        // Mathematically speaking, the system matrix should
        // always be invertible, since the determinant, formulated
        // as ad - bc for a 2x2 matrix, reduces thusly
        // when expanding and plugging in the coefficients:

        // a = 0
        // b = 1
        // c = -1 / k2
        // d = -k1 / k2

        // c = -1 / (1 / (tau * frequency)^2)
        // c = -(tau * frequency)^2

        // det(M) = a * d - b * c
        // det(M) = 0 * d - 1 * -(tau * frequency)^2
        // det(M) = (tau * frequency)^2

        // Consequently, the only way in which the matrix
        // should be non-invertible in theory is
        // if the frequency becomes zero and the determinant thus
        // would have to divide by zero, which
        // is impossible due to the FrequencyClampLimit.

        // Just in case, however, if anything goes wrong due to
        // the frequency limit being made too tiny or other numerical
        // considerations, I'll have the debugger holler about it.

        bool successfullyInverted = Matrix2x2.TryInvert(systemMatrix, out systemMatrixInverse);
        Debug.Assert(successfullyInverted);

        systemMatrixInverseSquared = systemMatrixInverse * systemMatrixInverse;
    }

    /// <summary>
    ///     Updates the state of the system analytically, returning
    ///     the new position of the system, y.
    /// </summary>
    /// <param name="dt">The base time step value.</param>
    /// <param name="basePosition">The base position, x.</param>
    /// <param name="baseVelocity">The base velocity, x'.</param>
    public Vector3 Update(float dt, Vector3 basePosition, Vector3 baseVelocity)
    {
        bool exponentialMatrixUninitialized = previousExponentialMatrixState == default;
        bool timestepPerceptiblyChanged = float.Abs(previousExponentialMatrixState.DeltaTime - dt) >= 0.0001f;
        if (exponentialMatrixUninitialized || timestepPerceptiblyChanged)
        {
            Matrix2x2 exponentialMatrix = Matrix2x2.Exp(systemMatrix, dt);
            previousExponentialMatrixState = new StoredExponentialMatrixState(exponentialMatrix, dt);
        }

        Vector2 stateX = CalculateUpdateForAxis(0, dt, basePosition, baseVelocity);
        Vector2 stateY = CalculateUpdateForAxis(1, dt, basePosition, baseVelocity);
        Vector2 stateZ = CalculateUpdateForAxis(2, dt, basePosition, baseVelocity);

        Position = new Vector3(stateX.X, stateY.X, stateZ.X);
        Velocity = new Vector3(stateX.Y, stateY.Y, stateZ.Y);

        return Position;
    }

    private Vector2 CalculateUpdateForAxis(int dimension, float dt, Vector3 basePosition, Vector3 baseVelocity)
    {
        // Imagine an intermediate state vector
        // of s, defined in relation to y and y' in the
        // original formalation.
        // In this context, s = <y, y'>

        // Under this formulation, we can solve the
        // differential equation by representing it as
        // a linear system like so:

        // s' = d/dt <y, y'> = M * <y, y'> + <a, b> * (x + k3 * x'),
        // where M is some matrix that allows for
        // linear combinations of the terms, and a and
        // b are terms that account for the x parts of the
        // equation.

        // Before proceeding further to determine what
        // the unknown terms should be, recall the
        // original formulation. A bit of algebraic
        // manipulations yields the following rewritten definition for
        // the acceleration term:

        // y'' = (x + k3 * x' - y - k1 * y') / k2, or
        // y'' = (x / k2) + (k3 * x' / k2) - (y / k2) - (k1 * y' / k2)

        // We omit terms involving x for the purposes of this
        // reduction to focus solely on the matrix for now.
        // We'll come back to x later.
        // y'' = -(y / k2) - (k1 * y' / k2) + ...

        // Recalling the column-wise definitions of matrix
        // construction (each column encodes the per-dimension
        // dot product coefficients for the input dimension), and the
        // matrix appears thusly:

        // M = [0,          1]
        //     [-1/k2, -k1/k2]

        // The reason for the 0, 1 term is deduced from trivial reasoning
        // regarding the fact that the second dimension encodes
        // the derivative of y naturally.

        // To determine the a and b terms, return to the
        // original equation. Recall the following:

        // y'' = (x / k2) + (k3 * x' / k2) + ...

        // Since we seek only an influence in relation to y'', the influence
        // of a is simply zero. Any other value would imply
        // necessity of the first derivative, leavily only b to
        // influence things.
        // I've chosen a bit of sleight of hand for the above
        // definitions in terms of multiplying by (x + k3 * x')
        // This only works in this context because in the latest
        // formulation, both terms are divided by a constant 1/k2.
        // This ends up being the definition of b, resulting in the
        // following state space evolution:

        // d/dt [y ] = [0,          1] * [y ] + [  0 ] * (x + k3 * x')
        //      [y']   [-1/k2, -k1/k2]   [y']   [1/k2]

        // Now for the fun stuff.
        // s' = M * s + ..., right?
        // We'll omit the extra stuff for now to
        // illustrate the idea better.

        // Well, what if s were a scalar?
        // The derivative would be defined thusly:

        // s' = m * s
        // The solution of this form of differential equation can
        // be solved explicitly as such:

        // s(t) = exp(m * t) * s(0)

        // The magic is in the fact that we
        // can generalize the natural exponential function to work
        // for the matrix form with the infinite (or, truncated, in practice) Taylor
        // expansion of exp(x), to create the following
        // solution:

        // s(t) = exp(M * t) * s(0), where exp(M * t)
        // corresponds, at least in mathematical theory, to the
        // matrix exponential function derived from Taylor series of exp(x).

        // One last detour that I gotta go over. The x terms and time step.
        // Introducing them updates s(t) accordingly:

        // s(t + dt) = exp(M * dt) * s(t) + ∫(0, dt) * exp(M * (dt - s)) * N * u(s) * ds,
        // where N is the aforementioned [0, 1/k2] matrix, and
        // u(t) = x(t) + k3 * x'(t)

        // For now, we'll assume u is constant to make things a bit
        // easier.
        // With this assumption, we can extract is out of the integral
        // to end up with the following derivation for that side of the equation:
        // (N * u) * ∫(0, dt) * exp(M * (dt - s)) * ds

        // For ordinary scalars, ∫(0, t) exp(a * x) * dx = exp(a * x) / a
        // And this formulation holds for matrices, becoming A^(-1) * exp(A * x)

        // So under this constant formulation, we arrive at a
        // definition that creates a correction term of (M^(-1) * (exp(M * dt) - I)) * N * u,
        // where I is the identity matrix.

        // So, s(t + dt) = exp(M * dt) * s(t) + (M^(-1) * (exp(M * dt) - I)) * N * u, right?
        // Yay! It's solved!
        // NOT!!!

        // Unfortunately, that assumption that u(t) is constant isn't necessarily correct
        // because we have a velocity term in there too in the form of baseVelocity.
        // I'll skip the integration weeds of this, since it's a bit long-winded and
        // I'm getting a bit tired of writing this enormous comment block, but the
        // result falls out nicely as a term much analogous to the first, constant term,
        // albeit with a suggested higher "order" via the square of the inverse system
        // matrix:

        // c = (M^(-1) * (exp(M * dt) - I)) * N * u
        // d = (M^(-2) * (exp(M * dt) - I) - M^(-1) * dt) * N * x' * 
        // s(t + dt) = exp(M * dt) * s(t) + c + d

        // This does technically omit x'' forcing terms and such, and that does preclude
        // this system from true, exact analytical precision, but honestly at this point I don't
        // mind that, given that the application is a video game where most motion
        // is handled exclusively in the first derivative via velocity.

        float u = basePosition[dimension] + k3 * baseVelocity[dimension];
        Vector2 n = Vector2.UnitY / k2;
        Vector2 state = new Vector2(Position[dimension], Velocity[dimension]);
        Matrix2x2 exponentialState = previousExponentialMatrixState.ExponentialState;
        Matrix2x2 c = systemMatrixInverse * (exponentialState - Matrix2x2.Identity);
        Matrix2x2 d = systemMatrixInverseSquared * (exponentialState - Matrix2x2.Identity) - systemMatrixInverse * dt;
        return exponentialState * state + c * (n * u) + d * (n * baseVelocity[dimension]);
    }

    /// <summary>
    ///     Displays a debug visualization of the step response
    ///     for a given system configuration.
    /// </summary>
    public static void DebugDisplayStepResponse(Renderer renderer, Vector3 drawPosition, MotionSystemSettings settings, float canvasSize, float timeRangeSeconds = 2f, int steps = 1024)
    {
        float dt = timeRangeSeconds / steps;
        AtlasEntry pixel = Atlases.KnightScene.Pixel;
        SpriteRenderer sprites = renderer.Get<SpriteRenderer>();
        SecondOrderDynamics solver = new SecondOrderDynamics(Vector3.Zero, Vector3.Zero, settings.Frequency, settings.Damping, settings.InitialResponse);

        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = drawPosition,
            Scale = Vector2.One * canvasSize,
            Tint = Color.White
        });

        Vector3 SampleDrawPosition(Vector3 p)
        {
            return drawPosition + new Vector3(-canvasSize * 0.5f + p.X * canvasSize, p.Y * canvasSize, 1f);
        }

        Vector3 previousY = Vector3.Zero;
        for (int i = 0; i < steps; i++)
        {
            float progress = i / (float)steps;
            float nextProgress = (i + 1) / (float)steps;
            Vector3 currentX = new Vector3(progress, progress >= 0.33f ? -0.2f : 0f, 0f);
            Vector3 nextX = new Vector3(nextProgress, nextProgress >= 0.33f ? -0.2f : 0f, 0f);
            Vector3 velocity = (nextX - currentX) / dt;
            Vector3 nextY = solver.Update(dt, currentX, velocity);

            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = SampleDrawPosition((nextX + currentX) * 0.5f) + Vector3.UnitZ * 16f,
                Scale = new Vector2(Vector3.Distance(currentX, nextX) * canvasSize, 2f),
                Tint = Color.Black,
                Rotation = (nextX - currentX).AsVector2()
            });
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = SampleDrawPosition(nextY),
                Scale = new Vector2(Vector3.Distance(nextY, previousY) * canvasSize + 2f, 2f),
                Tint = new Color(1f, 0.1f, 0.05f),
                Rotation = (nextY - previousY).AsVector2()
            });

            previousY = nextY;
        }
    }
}
