using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight.Bullets;

public sealed class ThrownBlackDagger : Bullet
{
    private bool beenThrown;

    private readonly List<(Vector2, Angle)> afterimages = [];

    /// <summary>
    ///     The rotation of this dagger.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    public ThrownBlackDagger(KnightFightScene scene, int damage, float grazePoints, Vector2 spawnPosition, Angle rotation) : base(scene, damage, grazePoints, spawnPosition, default)
    {
        Rotation = rotation;
    }

    protected override void UpdateInternal()
    {
        afterimages.Add((Position, Rotation));
        if (afterimages.Count >= 10)
            afterimages.RemoveAt(0);
    }

    /// <summary>
    ///     Throws this dagger.
    /// </summary>
    public void Throw()
    {
        if (beenThrown)
            return;

        beenThrown = true;
        Velocity = Rotation.Unit * 4850f;

        Scene.Audio.Play(new Sound(AudioBuffer.FromWav("ThrownDaggerFire.wav")));

        Sound spedUpSound = new Sound(AudioBuffer.FromWav("ThrownDaggerFire.wav"));
        spedUpSound.Stream.FrequencyRatio = 2f;

        Scene.Audio.Play(spedUpSound);
    }

    public override void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        AtlasEntry dagger = Atlases.KnightScene.BlackDagger;

        for (int i = 0; i < afterimages.Count; i++)
        {
            float opacity = (i + 1f) / afterimages.Count;
            sprites.Enqueue(dagger, new SpriteData(dagger)
            {
                Position = new Vector3(afterimages[i].Item1, DrawPosition.Z + 145f),
                Scale = Vector2.One * 1.2f,
                Tint = Color.White * opacity,
                Rotation = afterimages[i].Item2
            }, BlendState.Additive);
        }

        sprites.Enqueue(dagger, new SpriteData(dagger)
        {
            Position = DrawPosition + Vector3.UnitZ * 150f,
            Scale = Vector2.One * 1.2f,
            Tint = Color.White,
            Rotation = Rotation
        }, BlendState.NonPremultiplied);
    }

    public override bool CollidingWith(RectangleF box) => false;
}
