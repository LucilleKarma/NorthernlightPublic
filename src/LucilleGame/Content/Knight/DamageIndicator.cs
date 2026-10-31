using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     A visual damage indicator that displays in the world.
/// </summary>
public sealed class DamageIndicator
{
    private int totalBounces;

    private float time;

    private float startY;

    private Vector3 startingVelocity;

    private static Font font;

    /// <summary>
    ///     The lifetime ratio progress
    ///     of this damage indicator.
    /// </summary>
    public float LifetimeRatio => float.InverseLerp(0f, 1.2f, time);

    /// <summary>
    ///     The boumce animation progress
    ///     of this damage indicator.
    /// </summary>
    public float BounceProgress => float.InverseLerp(0f, 0.4f, LifetimeRatio);

    /// <summary>
    ///     The disappearance animation progress
    ///     of this damage indicator.
    /// </summary>
    public float DisappearanceProgress => float.InverseLerp(0.72f, 1f, LifetimeRatio);

    /// <summary>
    ///     Whether this indicator indicates
    ///     a miss instead of a hit.
    /// </summary>
    public bool Miss
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this indicator indicates
    ///     a swoon instead of a hit.
    /// </summary>
    public bool Swoon
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this indicator indicates
    ///     a maximum value hit/heal.
    /// </summary>
    public bool Max
    {
        get;
        set;
    }

    /// <summary>
    ///     The position of this damage indicator.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    ///     The velocity of this damage indicator.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    ///     The scale of this damage indicator.
    /// </summary>
    public Vector2 Scale
    {
        get;
        set;
    }

    /// <summary>
    ///     The numerical value shown by this damage
    ///     indicator.
    /// </summary>
    public required int Value
    {
        get;
        set;
    }

    /// <summary>
    ///     The color of this damage indicator.
    /// </summary>
    public required Color Color
    {
        get;
        init;
    }

    /// <summary>
    ///     Updates this damage indicator.
    /// </summary>
    public void Update(Game game)
    {
        if (startY == 0f)
            startY = Position.Y;
        if (startingVelocity == Vector3.Zero)
            startingVelocity = Velocity;

        time += game.Time.DeltaTime;
        Position += Velocity * game.Time.DeltaTime;

        if (DisappearanceProgress <= 0f)
        {
            Velocity.X = startingVelocity.X * float.InverseLerp(0.4f, 0f, time);

            int maxBounces = 3;
            if (totalBounces <= maxBounces)
                Velocity.Y += game.Time.DeltaTime * 1200f;

            if (Position.Y > startY && totalBounces <= maxBounces)
            {
                Position.Y = startY;
                Velocity.Y = float.Abs(startingVelocity.Y) * -0.5f;
                totalBounces++;
            }

            if (totalBounces >= maxBounces)
            {
                Velocity.Y = 0f;
                Position.Y = startY;
            }
        }
        else
            Velocity -= Vector3.UnitY * game.Time.DeltaTime * DisappearanceProgress.Squared() * 4000f;
    }

    /// <summary>
    ///     Renders this damage.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, TextRenderer text)
    {
        font ??= new Font("DamageFont.ttf", 24f);

        string valueText = Value.ToString();

        float squishProgress = float.InverseLerp(0f, 0.1f, LifetimeRatio);
        float squish = float.Bump(squishProgress);
        float opacity = float.Pow(1f - DisappearanceProgress, 0.85f);
        Vector2 scale = Scale;

        scale.X *= 1f + squish * 1.2f;
        scale.Y *= 1f - squish * 0.95f;

        scale.Y *= 1f + DisappearanceProgress * 1.2f;
        if (Miss)
        {
            scale *= 1.25f;

            AtlasEntry miss = Atlases.KnightScene.Miss;
            sprites.Enqueue(miss, new SpriteData(miss)
            {
                Position = Position + Vector3.UnitY * scale.Y * miss.Area.Height * 0.5f,
                Scale = scale,
                Tint = Color * opacity
            });
            return;
        }
        if (Swoon)
        {
            scale *= 1.25f;

            AtlasEntry swoon = Atlases.KnightScene.Swoon;
            sprites.Enqueue(swoon, new SpriteData(swoon)
            {
                Position = Position + Vector3.UnitY * scale.Y * swoon.Area.Height * 0.5f,
                Scale = scale,
                Tint = Color * opacity
            });
            return;
        }
        if (Max)
        {
            scale *= 1.25f;

            AtlasEntry max = Atlases.KnightScene.Max;
            sprites.Enqueue(max, new SpriteData(max)
            {
                Position = Position + Vector3.UnitY * scale.Y * max.Area.Height * 0.5f,
                Scale = scale,
                Tint = Color * opacity
            });
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3(new Angle(float.Tau * i / 4f).Unit * scale * 3f, -10f);
            text.Enqueue(valueText, font, Position + offset, Color.Black * opacity, scale, new Vector2(0.5f, 1f));
        }
        text.Enqueue(valueText, font, Position, Color * opacity, scale, new Vector2(0.5f, 1f));
    }
}
