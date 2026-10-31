using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Bullets;

public sealed class RedFlurrySlashResidue : Bullet
{
    private readonly OwnedTimer timer;

    /// <summary>
    ///     How long this particle should exist for.
    /// </summary>
    public float Lifetime
    {
        get;
        set;
    }

    /// <summary>
    ///     The direction of this particle.
    /// </summary>
    public Angle Direction
    {
        get;
        set;
    }

    public RedFlurrySlashResidue(KnightFightScene scene, float lifetime, Vector2 spawnPosition, Angle direction) : base(scene, 0, 0f, spawnPosition, default)
    {
        Lifetime = lifetime;
        timer = scene.Time.Rent();
        Direction = direction;
    }

    protected override void UpdateInternal()
    {
        if (timer.Elapsed(Lifetime))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        SpriteRenderer.ShaderWithPreparations? insideOfBoxShader = new SpriteRenderer.ShaderWithPreparations(RadialWaveParticle.GetRestrictionShader(Scene.Game), RedFlurrySlash.PrepareRestrictionParameters_WithoutInversion);

        float lifetimeRatio = timer / Lifetime;
        Vector2 scale = new Vector2(2f, (1f - lifetimeRatio).Cubed() * 0.05f);
        AtlasEntry triangle = Atlases.KnightScene.IsoscelesTriangle;
        sprites.Enqueue(triangle, new SpriteData(triangle)
        {
            Position = DrawPosition + new Vector3(Direction.Unit * scale.X * -100f, 120f),
            Scale = scale,
            Tint = Color.White * 0.7f,
            Rotation = Direction
        }, BlendState.Additive, drawToDepthBuffer: false, overridingShader: insideOfBoxShader);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
