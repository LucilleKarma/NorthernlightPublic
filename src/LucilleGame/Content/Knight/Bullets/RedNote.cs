using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedNote : Bullet
{
    public enum NoteKind
    {
        Half,
        Quarter,
        Eighth,
        Count
    }

    private float existenceTimer;

    /// <summary>
    ///     This shard's scale.
    /// </summary>
    public float Scale
    {
        get;
        set;
    } = 1f;

    /// <summary>
    ///     The kind of noe that this is.
    /// </summary>
    public NoteKind Kind
    {
        get;
        private set;
    }

    public RedNote(KnightFightScene scene, Vector2 spawnPosition, Vector2 velocity, int damage, float grazePoints, NoteKind kind) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        Velocity = velocity;
        Kind = kind;
    }

    protected override void UpdateInternal()
    {
        existenceTimer += Scene.Time.DeltaTime;
        if (existenceTimer >= 10f)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry noteTexture = Kind switch
        {
            NoteKind.Half => Atlases.KnightScene.RedNoteHalf,
            NoteKind.Quarter => Atlases.KnightScene.RedNoteQuarter,
            _ => Atlases.KnightScene.RedNoteEighth,
        };

        Vector2 directionedScale = Vector2.One;
        Angle rotation = Velocity;
        if (Velocity.X < 0f)
        {
            rotation += Angle.Pi;
        }

        float pulse = existenceTimer * 2f % 1f;
        Vector2 jitter = Scene.Rng.NextCircular(3f);
        sprites.Enqueue(noteTexture, new SpriteData(noteTexture)
        {
            Position = DrawPosition - Vector3.UnitZ * 1.5f,
            Scale = directionedScale * Scale * (1f + pulse * 1.5f),
            Tint = Color.White * float.Pow(1f - pulse, 0.8f) * 0.9f,
            Rotation = rotation
        }, BlendState.Additive, drawToDepthBuffer: false);

        for (int i = 0; i < 10; i++)
        {
            float progress = i / 10f;
            Vector2 backwards = Velocity * progress * -0.4f;
            sprites.Enqueue(noteTexture, new SpriteData(noteTexture)
            {
                Position = DrawPosition - Vector3.UnitZ * 1.5f + new Vector3(backwards, 0f),
                Scale = directionedScale * Scale * (1f - progress * 0.5f),
                Tint = Color.White * (1f - progress).Squared() * 0.9f,
                Rotation = rotation
            }, BlendState.Additive, drawToDepthBuffer: false);
        }

        sprites.Enqueue(noteTexture, new SpriteData(noteTexture)
        {
            Position = DrawPosition + new Vector3(jitter, 0f),
            Scale = directionedScale * Scale,
            Tint = Color.White,
            Rotation = rotation
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box)
    {
        RectangleF hurtBox = new RectangleF(Position, Atlases.KnightScene.RedNoteEighth.Area.Size * Scale * 0.34f);
        return box.Intersects(hurtBox);
    }
}
