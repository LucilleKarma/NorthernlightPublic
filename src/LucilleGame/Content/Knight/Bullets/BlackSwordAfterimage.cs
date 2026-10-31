using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BlackSwordAfterimage : Bullet
{
    private float disappearanceProgress;

    private readonly Angle rotation;

    private readonly Color color;

    public BlackSwordAfterimage(KnightFightScene scene, Color color, Vector2 spawnPosition, Angle rotation) : base(scene, 0, 0f, spawnPosition, default)
    {
        this.color = color;
        this.rotation = rotation;
    }

    protected override void UpdateInternal()
    {
        disappearanceProgress += Scene.Time.DeltaTime * 1.5f;
        if (disappearanceProgress >= 1f)
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry sword = Atlases.KnightScene.BlackSword;
        sprites.Enqueue(sword, new SpriteData(sword)
        {
            Position = DrawPosition - Vector3.UnitZ * 24f,
            Tint = color * (1f - disappearanceProgress) * 0.5f,
            Rotation = rotation,
            Scale = Vector2.One * BlackSword.Scale
        }, BlendState.Merge);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
