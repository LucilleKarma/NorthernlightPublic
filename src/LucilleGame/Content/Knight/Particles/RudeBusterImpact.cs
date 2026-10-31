using System.Numerics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     Represents an instance of a rude buster shot impact.
/// </summary>
public sealed class RudeBusterImpact
{
    private float xScale = 1f;

    private float time;

    private Vector2 position;

    private Vector2 velocity;

    private readonly float deceleration;

    private readonly float xScaleDecay;

    private readonly KnightFightScene scene;

    public RudeBusterImpact(KnightFightScene scene, Vector2 position, Vector2 velocity, float decelerationFactor)
    {
        this.scene = scene;
        this.position = position;
        this.velocity = velocity;

        // OK so in Deltarune the game is 30FPS.
        // In Deltarune, the two spreads of impacts
        // have a deceleration factor of 0.75x and 0.8x.
        // In order to recreate this with the 1 - 0.5^(dt * s)
        // model, it's simply a matter
        // of plugging in these original deceleration
        // factors in to get the appropriate corresponding
        // value for s.

        // 0.5^(dt * s) = 0.8
        // ln(0.5^(dt * s)) = ln(0.8)
        // (dt * s) * ln(0.5) = ln(0.8)
        // (dt * s) = ln(0.8) / ln(0.5)
        // s = ln(0.8) / ln(0.5) / dt
        float fps = scene.Game.TargetFps;
        float dt = 1f / fps;
        deceleration = float.Log(decelerationFactor) / (float.Log(0.5f) * dt);

        // Same deal as with the above comment for the
        // X scale decay, though for this the base value
        // is a constant of 0.8.
        xScaleDecay = float.Log(0.8f) / (float.Log(0.5f) * dt);
    }

    /// <summary>
    ///     Updates this impact.
    /// </summary>
    public void Update()
    {
        velocity = Vector2.LerpDt(velocity, Vector2.Zero, deceleration, scene.Time);
        xScale = float.LerpDt(xScale, 0f, xScaleDecay, scene.Time);

        time += scene.Time.DeltaTime;
        if (time >= 0.6f)
            scene.RudeBusterImpacts.Remove(this);

        position += velocity * scene.Time.DeltaTime;
    }

    /// <summary>
    ///     Prepares this impact for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        Vector2 scale = new Vector2(xScale, 1f) * 2f;
        AtlasEntry shot = Atlases.KnightScene.RudeBusterImpact;
        sprites.Enqueue(shot, new SpriteData(shot)
        {
            Position = new Vector3(position, 400f),
            Rotation = velocity,
            Scale = scale,
            Tint = Color.White * float.InverseLerp(0.6f, 0.45f, time)
        });
    }
}
