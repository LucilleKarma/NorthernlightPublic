using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class Crack : Bullet
{
    /// <summary>
    ///     The scale of this crack.
    /// </summary>
    public float Scale
    {
        get;
        set;
    }

    /// <summary>
    ///     The angle of this crack.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     The protrusion directino of this crack.
    /// </summary>
    public Angle ProtrusionDirection
    {
        get;
        set;
    }

    public Crack(KnightFightScene scene, float scale, Angle rotation, Angle protrusionDirection) : base(scene, 0, 0f, default)
    {
        Scale = scale;
        Rotation = rotation;
        ProtrusionDirection = protrusionDirection;
    }

    protected override void UpdateInternal()
    {
        BattleBox box = Scene.BattleHandler.Box;
        Position = box.Position + BattleBox.AngleToSquare(ProtrusionDirection + box.Rotation) * box.Size * 0.5f;
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        BattleBox box = Scene.BattleHandler.Box;
        Vector2 scale = new Vector2(-1f, 1f) * Scale;
        AtlasEntry crack = Atlases.KnightScene.Cracks;
        Color color = new Color(0, 140, 0);
        Angle rotation = Rotation + box.Rotation;
        sprites.Enqueue(crack, new SpriteData(crack)
        {
            Position = DrawPosition + new Vector3(rotation.Unit * scale * (crack.Area.Width * 0.5f + 4f), 0f),
            Scale = scale,
            Tint = color * 0.7f,
            Rotation = rotation
        }, BlendState.Additive);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
