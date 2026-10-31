using System.Numerics;
using Northernlight.Content.Borders;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     Represents an instance of a rude buster shot.
/// </summary>
public sealed class RudeBusterShot
{
    private bool checkedForCriticalStrikeBonus;

    private bool appliedCriticalStrikeBonus;

    private float opacity = 1f;

    private readonly KnightFightScene scene;

    private readonly OwnedTimer animationTimer;

    /// <summary>
    ///     How much damage this shot should do
    ///     on impact as a baseline, not factoring
    ///     in critical strikes.
    /// </summary>
    public int Damage
    {
        get;
        set;
    }

    /// <summary>
    ///     The rotation of this shot.
    /// </summary>
    public Angle Rotation
    {
        get;
        set;
    }

    /// <summary>
    ///     The frame of this shot.
    /// </summary>
    public int Frame
    {
        get;
        set;
    }

    /// <summary>
    ///     The position of this shot
    ///     in world space.
    /// </summary>
    public Vector2 Position
    {
        get;
        set;
    }

    public RudeBusterShot(KnightFightScene scene)
    {
        this.scene = scene;
        animationTimer = scene.Time.Rent();
    }

    /// <summary>
    ///     Updates this shot.
    /// </summary>
    public void Update()
    {
        int previousFrame = Frame;
        bool frameChange = animationTimer.Cycle(1f / 60f);

        if (Damage >= 1)
        {
            Vector2 destination = scene.RoaringKnight.Position.AsVector2() + Vector2.UnitY * 50f;
            Vector2 velocity = (destination - Position).SafeNormalize(Vector2.Zero) * 1600f;
            Position += velocity * scene.Time.DeltaTime;
            Rotation = velocity;

            // It's like pressing B at the right time when
            // using a Pokeball except it's a real mechanic
            // in the real Deltarune!
            if (!checkedForCriticalStrikeBonus && ActiveKeymap.Confirm.WasJustPressed)
            {
                checkedForCriticalStrikeBonus = true;
                ApplyCriticalStrikeBonus(velocity, destination);
            }

            if (Vector2.Distance(Position, destination) <= 40f)
            {
                Explode();
                return;
            }

            if (frameChange)
            {
                scene.RudeBusterShots.Add(new RudeBusterShot(scene)
                {
                    Position = Position,
                    Rotation = Rotation,
                    Frame = previousFrame,
                    Damage = 0,
                });
            }
        }
        else
        {
            frameChange = false;
            opacity -= scene.Time.DeltaTime * 2f;
            if (opacity <= 0f)
            {
                scene.RudeBusterShots.Remove(this);
                return;
            }
        }

        if (frameChange)
            Frame = (Frame + 1) % 7;
    }

    private void ApplyCriticalStrikeBonus(Vector2 destination, Vector2 velocity)
    {
        float speed = velocity.Length();
        float distanceFromDestination = Vector2.Distance(Position, destination);
        float secondsUntilHit = distanceFromDestination / speed;
        int framesUntilHit = (int)(secondsUntilHit * 60f);

        int damageBoost = 0;
        if (framesUntilHit <= 1)
            damageBoost = 30;
        else if (framesUntilHit <= 2)
            damageBoost = 28;
        else if (framesUntilHit <= 3)
            damageBoost = 20;
        else if (framesUntilHit <= 5)
            damageBoost = 13;
        else if (framesUntilHit <= 6)
            damageBoost = 10;
        else if (framesUntilHit <= 9)
            damageBoost = 7;

        if (damageBoost >= 1)
        {
            Damage += damageBoost;
            appliedCriticalStrikeBonus = true;
        }
    }

    private void Explode()
    {
        KnightBorder.Instance.ShineIntensityFactor = 4f;

        scene.Audio.Play(new Sound(AudioBuffer.FromWav("RudeBusterHit.wav")));
        scene.RudeBusterShots.Remove(this);

        scene.AddScreenShake(24f);
        scene.RoaringKnight.AddShake(30f);
        scene.RoaringKnight.Hurt(Damage, true);
        scene.RoaringKnight.CreateHurtTextParticle(Damage, scene.Heroes.Susie.DamageColor);

        Vector2 impactOrigin = scene.RoaringKnight.Position.AsVector2();
        for (int i = 0; i < 4; i++)
        {
            Vector2 impactDirection = (float.Tau * (i + 0.5f) / 4f).ToUnit();
            Vector2 impactVelocity = impactDirection * 1500f;
            if (appliedCriticalStrikeBonus)
                impactVelocity *= 1.5f;

            scene.RudeBusterImpacts.Add(new RudeBusterImpact(scene, impactOrigin, impactVelocity, 0.75f));
            scene.RudeBusterImpacts.Add(new RudeBusterImpact(scene, impactOrigin, impactVelocity, 0.8f));
        }
    }

    /// <summary>
    ///     Prepares this shot for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        Vector2 scale = new Vector2(1f, 1f - (1f - opacity) * 0.9f) * 1.5f;
        if (Damage >= 1)
            scale.Y *= 1.3f;

        AtlasEntry shot = Atlases.KnightScene.RudeBusterShot;
        sprites.Enqueue(shot, new SpriteData(shot, Frame, 0, 7, 1)
        {
            Position = new Vector3(Position, 400f - (1f - opacity) * 32f),
            Rotation = Rotation,
            Scale = scale,
            Tint = Color.White * opacity
        });
    }
}
