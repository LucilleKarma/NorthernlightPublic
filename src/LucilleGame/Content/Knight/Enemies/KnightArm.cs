using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Enemies;

public struct KnightArm
{
    /// <summary>
    ///     The rotation of the arm.
    /// </summary>
    public Angle ArmRotation;

    /// <summary>
    ///     The rotation of the forearm.
    /// </summary>
    public Angle ForearmRotation;

    /// <summary>
    ///     The length of the arm.
    /// </summary>
    public float ArmLength;

    /// <summary>
    ///     The length of the forearm.
    /// </summary>
    public float ForearmLength;

    /// <summary>
    ///     Whether this is a left arm.
    /// </summary>
    public bool LeftSide;

    /// <summary>
    ///     The end position of this arm.
    /// </summary>
    public Vector2 End;

    /// <summary>
    ///     The velocity of this arm's end effector.
    /// </summary>
    public Vector2 Velocity;

    public KnightArm(Vector2 origin, bool leftSide)
    {
        ArmLength = 24f;
        ForearmLength = 41f;
        LeftSide = leftSide;
        End = origin + Vector2.UnitY * (ArmLength + ForearmLength);

        SolvePose(origin);
    }

    /// <summary>
    ///     Solves this arm's pose and
    ///     recalculates the (fore)arm angles
    ///     accordingly.
    /// </summary>
    public void SolvePose(Vector2 origin)
    {
        float c = Vector2.Distance(origin, End);
        float unboundedCosine = (c.Squared() + ArmLength.Squared() - ForearmLength.Squared()) / (c * ArmLength * 2f);
        float boundedCosine = float.Clamp(unboundedCosine, -1f, 1f);
        Angle theta = float.Acos(boundedCosine) * (LeftSide ? 1f : -1f);

        Vector2 elbowPosition = origin + new Angle(theta + (End - origin)).Unit * ArmLength;

        ArmRotation = elbowPosition - origin;
        ForearmRotation = End - elbowPosition;
    }

    /// <summary>
    ///     Updates this arm.
    /// </summary>
    public void Update(KnightFightScene scene, Vector2 ownerVelocity, Vector2 origin)
    {
        End += (Velocity + ownerVelocity) * scene.Time.DeltaTime;
        SolvePose(origin);
    }

    /// <summary>
    ///     Moves this arm to a given destation.
    /// </summary>
    public void MoveTowards(KnightFightScene scene, Vector2 destination, float speedFactor = 6f, float maxSpeed = 310f)
    {
        Vector2 velocity = (destination - End) * speedFactor;
        float speed = velocity.Length();
        if (speed > maxSpeed)
            velocity *= maxSpeed / speed;

        Velocity = Vector2.LerpDt(Velocity, velocity, 40f, scene.Time);

        if (speedFactor >= 20f)
        {
            Velocity = Vector2.Zero;
            End = destination;
        }
    }

    /// <summary>
    ///     Prepares this arm for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, Vector3 start, Vector2 scale)
    {
        scale.Y *= LeftSide ? 1f : -1f;

        AtlasEntry arm = Atlases.KnightScene.KnightGuitarHoldArm;
        AtlasEntry forearm = Atlases.KnightScene.KnightGuitarHoldForearm;

        Vector3 position = start;
        sprites.Enqueue(arm, new SpriteData(arm)
        {
            Position = position + new Vector3(ArmRotation.Unit * arm.Area.Width * scale.X * 0.25f, 0f),
            Tint = Color.White,
            Scale = scale,
            Rotation = ArmRotation
        });

        position += new Vector3(ArmLength * ArmRotation.Unit * scale.X * 0.5f, 2f);
        sprites.Enqueue(forearm, new SpriteData(forearm)
        {
            Position = position + new Vector3(ForearmRotation.Unit * arm.Area.Width * scale.X * 0.5f, 0f),
            Tint = Color.White,
            Scale = scale,
            Rotation = ForearmRotation
        });
    }
}
