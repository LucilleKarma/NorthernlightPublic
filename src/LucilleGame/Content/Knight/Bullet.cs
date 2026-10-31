using System.Numerics;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of a bullet
///     that enemies can summon.
/// </summary>
public abstract class Bullet
{
    internal bool Dead;

    /// <summary>
    ///     The amount of damage this bullet does.
    /// </summary>
    public int Damage
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of tension points that this
    ///     bullet should give if grazed by the soul
    ///     for one second total.
    /// </summary>
    /// <remarks>
    ///     Tension point awards are linearly proportioned relative
    ///     to this one-second baseline.
    /// </remarks>
    public float GrazePoints
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this bullet should swoon heroes
    ///     upon causing them to become downed.
    /// </summary>
    public bool SwoonsOnDowning
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     The game responsible for this bullet.
    /// </summary>
    public KnightFightScene Scene
    {
        get;
        protected set;
    }

    /// <summary>
    ///     The position of the bullet.
    /// </summary>
    public Vector2 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     The velocity of the bullet.
    /// </summary>
    public Vector2 Velocity
    {
        get;
        set;
    }

    /// <summary>
    ///     The position to use for rendering.
    /// </summary>
    public Vector3 DrawPosition => new Vector3(Position, 150f);

    public Bullet(KnightFightScene scene, int damage, float grazePoints, Vector2 spawnPosition, Vector2 velocity = default)
    {
        Scene = scene;
        Damage = damage;
        Velocity = velocity;
        GrazePoints = grazePoints;
        Position = spawnPosition;
    }

    protected abstract void UpdateInternal();

    /// <summary>
    ///     Updates the bullet.
    /// </summary>
    public void Update()
    {
        UpdateInternal();
        Position += Velocity * Scene.Time.DeltaTime;

        Soul soul = Scene.BattleHandler.Soul;

        int damage = Damage;
        if (Scene.SideB)
            damage = (int)float.Round(damage * StatsRepository.BulletDamageFactorSideB);

        if (damage >= 1)
        {
            bool collidingWithSoul = CollidingWith(soul.Hurtbox);
            if (soul.CanBeHit && collidingWithSoul)
            {
                Hero target = Scene.BattleHandler.Hurt(damage, SwoonsOnDowning);
                OnHit(target);
            }
            if (!collidingWithSoul && GrazePoints > 0f && CollidingWith(soul.GrazeBox) && soul.ImmunityFramesCountdown <= 0f)
                AwardGrazeTensionPoints(soul);
        }
    }

    /// <summary>
    ///     Awards tension points to the player
    ///     for grazing this bullet.
    /// </summary>
    public void AwardGrazeTensionPoints(Soul soul)
    {
        if (soul.GrazeSoundCountdown <= 0f)
        {
            Scene.Audio.Play(new Sound(AudioBuffer.FromWav("BulletGraze.wav"), 0.65f));
            soul.GrazeSoundCountdown = 0.2f;
        }

        float tensionPointsAward = GrazePoints * Scene.Time.DeltaTime;
        Scene.BattleHandler.TensionPoints = float.Clamp(Scene.BattleHandler.TensionPoints + tensionPointsAward, 0f, 100f);
        Scene.BattleHandler.Soul.GrazeIndicatorIntensity = 1f;
    }

    /// <summary>
    ///     Kills this bullet on the next frame.
    /// </summary>
    public void Kill()
    {
        if (Dead)
            return;

        Dead = true;
        OnKill();
    }

    /// <summary>
    ///     Applies optional secondary effects on the
    ///     death of this bullet.
    /// </summary>
    public virtual void OnKill() { }

    /// <summary>
    ///     Applies optional secondary effects on hitting
    ///     the soul.
    /// </summary>
    public virtual void OnHit(Hero target) { }

    /// <summary>
    ///     Prepares the bullet for rendering.
    /// </summary>
    public abstract void PrepareRender(SpriteRenderer sprites, FrameContext context);

    /// <summary>
    ///     Determines whether this bullet is colliding with
    ///     a soul box or not.
    /// </summary>
    public abstract bool CollidingWith(RectangleF box);
}
