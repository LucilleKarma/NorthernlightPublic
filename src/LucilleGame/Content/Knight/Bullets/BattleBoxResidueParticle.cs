using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class BattleBoxResidueParticle : Bullet
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
    ///     The maximum scale of this particle.
    /// </summary>
    public float MaxScale
    {
        get;
        set;
    }

    public BattleBoxResidueParticle(KnightFightScene scene, float lifetime, float maxScale, Vector2 spawnPosition) : base(scene, 0, 0f, spawnPosition)
    {
        Lifetime = lifetime;
        MaxScale = maxScale;
        timer = scene.Time.Rent();
    }

    protected override void UpdateInternal()
    {
        if (timer.Elapsed(Lifetime))
            Kill();
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        float lifetimeRatio = timer / Lifetime;
        int frame = (int)float.Round(float.Lerp(0f, 3f, lifetimeRatio));

        AtlasEntry wave = Atlases.KnightScene.BattleBoxResidue;
        Color color = Color.Black;
        sprites.Enqueue(wave, new SpriteData(wave, 0, frame, 1, 4)
        {
            Position = DrawPosition,
            Scale = Vector2.One * MaxScale,
            Tint = color
        }, BlendState.AlphaBlend);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
