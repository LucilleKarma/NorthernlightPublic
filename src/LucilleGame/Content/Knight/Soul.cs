using System.Numerics;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of Kris' soul in battle.
/// </summary>
public sealed class Soul
{
    /// <summary>
    ///     The amount of afterimages this soul should render
    ///     with.
    /// </summary>
    public int AfterimageCount
    {
        get;
        set;
    }

    /// <summary>
    ///     The distance between afterimages for this
    ///     soul.
    /// </summary>
    public float AfterimageIncrement
    {
        get;
        set;
    }

    /// <summary>
    ///     The trailing direction of afterimages for this
    ///     soul.
    /// </summary>
    public Vector2 AfterimageDirection
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the soul should use the blurred texture or not.
    /// </summary>
    public bool RenderBlurred
    {
        get;
        set;
    }

    /// <summary>
    ///     The graze sound replay delay.
    /// </summary>
    public float GrazeSoundCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     A countdown which dictates how much longer
    ///     the soul benefits from immunity frames.
    /// </summary>
    public float ImmunityFramesCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     The game responsible for this soul.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        private set;
    }

    /// <summary>
    ///     The opacity of the soul. May be
    ///     changed when exiting/re-entering Kris'
    ///     body.
    /// </summary>
    public float Opacity
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The intensity of the graze indicator
    ///     visual around the soul.
    /// </summary>
    public float GrazeIndicatorIntensity
    {
        get;
        set;
    }

    /// <summary>
    ///     The rotation of this soul.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     The position of the soul.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     The velocity of the soul.
    /// </summary>
    public Vector3 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether the soul can be hurt currently
    ///     or not.
    /// </summary>
    public bool CanBeHit => ImmunityFramesCountdown <= 0f;

    /// <summary>
    ///     The hurtbox of the soul.
    /// </summary>
    public RectangleF Hurtbox => new RectangleF(Position.AsVector2(), Vector2.One * Scale * 20f);

    /// <summary>
    ///     The graze box of the soul.
    /// </summary>
    public RectangleF GrazeBox => new RectangleF(Position.AsVector2(), Vector2.One * Scale * GrazeBoxScale * 20f);

    /// <summary>
    ///     The soul's scale.
    /// </summary>
    public static float Scale => 1.5f;

    /// <summary>
    ///     How long the player benefits from
    ///     immunity frames upon being hit.
    /// </summary>
    public static float ImmunityFramesDuration => 1.333f;

    /// <summary>
    ///     The scaling factor of the graze hitbox relative to the
    ///     hurt box.
    /// </summary>
    public static float GrazeBoxScale => 3.4f;

    public Soul(KnightFightScene scene) => Scene = scene;

    /// <summary>
    ///     Keeps the soul dormant until it's needed
    ///     for battle.
    /// </summary>
    public void StayDormant(Vector3 krisCenter)
    {
        Position = krisCenter;
        Velocity = Vector3.Zero;
        Opacity = 0f;
        AfterimageCount = 0;
        AfterimageIncrement = 0f;
        AfterimageDirection = Vector2.Zero;
    }

    /// <summary>
    ///     Updates the soul.
    /// </summary>
    public void Update()
    {
        Velocity = Vector3.LerpDt(Velocity, Vector3.Zero, 8f, Scene.Time);

        Position += Velocity * Scene.Time.DeltaTime;
        ImmunityFramesCountdown = float.Max(0f, ImmunityFramesCountdown - Scene.Time.DeltaTime);

        float grazeIndicatorDuration = 0.6f;
        GrazeIndicatorIntensity = float.Max(0f, GrazeIndicatorIntensity - Scene.Time.DeltaTime / grazeIndicatorDuration);
        if (Scene.DeathScreen.FreezeFrameOngoing || Scene.DeathScreen.OverrideGameScreen)
            GrazeIndicatorIntensity = 0f;

        GrazeSoundCountdown = float.Max(0f, GrazeSoundCountdown - Scene.Time.DeltaTime);
    }

    /// <summary>
    ///     Prepares the soul for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        float iFramesProgress = float.InverseLerp(ImmunityFramesDuration, 0f, ImmunityFramesCountdown);
        float iFramesOpacity = float.Cos01(float.Tau * iFramesProgress * 4f);

        float scale = Scale;
        AtlasEntry soul = Atlases.KnightScene.Soul;
        if (RenderBlurred)
        {
            soul = Atlases.KnightScene.SoulBlur;
            scale *= 1.3f;
        }

        for (int i = AfterimageCount - 1; i >= 0; i--)
        {
            float afterimageOpacity = 1f - i / (float)AfterimageCount;
            sprites.Enqueue(soul, new SpriteData(soul)
            {
                Position = Position + AfterimageDirection.AsVector3() * i * AfterimageIncrement - Vector3.UnitZ * i,
                Tint = Color.White * Opacity * iFramesOpacity * afterimageOpacity,
                Scale = Vector2.One * scale,
                Rotation = Rotation
            }, BlendState.NonPremultiplied);
        }

        sprites.Enqueue(soul, new SpriteData(soul)
        {
            Position = Position,
            Tint = Color.White * Opacity * iFramesOpacity,
            Scale = Vector2.One * scale,
            Rotation = Rotation
        }, BlendState.NonPremultiplied);

        if (GrazeIndicatorIntensity > 0f && ImmunityFramesCountdown <= 0f)
        {
            AtlasEntry outerGrazeIndicator = Atlases.KnightScene.SoulGrazeOuter;
            sprites.Enqueue(outerGrazeIndicator, new SpriteData(outerGrazeIndicator)
            {
                Position = Position,
                Tint = Color.White * Opacity * GrazeIndicatorIntensity,
                Scale = Vector2.One * scale * GrazeBoxScale * 0.3125f,
                Rotation = Rotation
            }, BlendState.AlphaBlend);
            sprites.Enqueue(outerGrazeIndicator, new SpriteData(outerGrazeIndicator)
            {
                Position = Position,
                Tint = Color.White * Opacity * GrazeIndicatorIntensity,
                Scale = Vector2.One * scale * GrazeBoxScale * 0.4f,
                Rotation = Rotation
            }, BlendState.AlphaBlend);
        }
    }
}
