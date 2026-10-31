using System.Numerics;

namespace Northernlight.Core.Physics;

/// <summary>
///     A representation of a system that imbues the motion
///     of a given system, such as that of a gameplay entity,
///     with a given characteristic, such as anticipation, damping,
///     oscillatory motion, etc.
/// </summary>
public sealed class MotionCharacteristicImbuer
{
    /// <summary>
    ///     The base position that affects the shape of the
    ///     output.
    /// </summary>
    /// <remarks>
    ///     When performing velocity calculations relative to
    ///     position, use this instead of the actual position.
    ///     
    ///     <br></br>
    ///     <br></br>
    ///         
    ///     e.g. <code>Destination - BaselinePosition;</code> instead 
    ///     of <code>Destination - NPC.Center;</code>
    /// </remarks>
    public Vector3 BaselinePosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The underlying differential equation solver that
    ///     governs this system.
    /// </summary>
    public SecondOrderDynamics Solver
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether this system is in use currently or not.
    /// </summary>
    public bool InUse
    {
        get;
        set;
    } = true;

    public MotionCharacteristicImbuer(Vector3 startingPosition, Vector3 initialVelocity, float frequency, float damping, float initialResponse)
    {
        Solver = new SecondOrderDynamics(startingPosition, initialVelocity, frequency, damping, initialResponse);
        BaselinePosition = startingPosition;
    }

    public MotionCharacteristicImbuer(Vector3 startingPosition, Vector3 initialVelocity, MotionSystemSettings settings) :
        this(startingPosition, initialVelocity, settings.Frequency, settings.Damping, settings.InitialResponse)
    { }

    /// <summary>
    ///     Updates this system, ensuring that the solver has
    ///     correct underlying data, even if a simulation
    ///     step is not performed this frame.
    /// </summary>
    public Vector3 Update(Vector3 position, Vector3 velocity, float dt, bool accountForAutomaticMovement = false)
    {
        if (!InUse)
        {
            if (!accountForAutomaticMovement)
                position += velocity * dt;

            BaselinePosition = position;
            Solver.Position = position;
            Solver.Velocity = velocity;
            return position;
        }

        return StepOne(velocity, dt, accountForAutomaticMovement);
    }

    /// <summary>
    ///     Teleports the system to a new position.
    /// </summary>
    public void Teleport(Vector3 position, bool resetVelocity)
    {
        BaselinePosition = position;
        Solver.Position = position;

        if (resetVelocity)
            Solver.Velocity = Vector3.Zero;
    }

    /// <summary>
    ///     Updates this system's solver by one frame, returning
    ///     the new position.
    /// </summary>
    private Vector3 StepOne(Vector3 velocity, float dt, bool accountForAutomaticMovement = false)
    {
        BaselinePosition += velocity * dt;
        Vector3 newPosition = Solver.Update(dt, BaselinePosition, velocity);
        if (InUse && accountForAutomaticMovement)
            newPosition -= velocity * dt;

        return newPosition;
    }
}
