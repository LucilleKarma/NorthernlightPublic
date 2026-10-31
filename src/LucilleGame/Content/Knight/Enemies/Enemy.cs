using System.Numerics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     Represents an instance an enemy to be fought
///     in the battle sequence.
/// </summary>
public abstract class Enemy
{
    protected readonly KnightFightScene scene;

    /// <summary>
    ///     Whether this enemy should flash due to
    ///     being selected for something by the
    ///     player, such as an attack effect.
    /// </summary>
    public bool SelectionFlash
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of HP the enemy currently
    ///     has.
    public int HP
    {
        get;
        set;
    }

    /// <summary>
    ///     The maximum amount of HP the enemy
    ///     can have.
    /// </summary>
    public int MaxHP
    {
        get;
        init;
    }

    /// <summary>
    ///     The position of the enemy.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     The resting position of the enemy.
    /// </summary>
    public Vector3 RestingPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     This enemy's startup optional battle animation.
    /// </summary>
    public BattleStartupAnimation? StartupAnimation
    {
        get;
        init;
    }

    public Enemy(KnightFightScene scene, Vector3 spawnPosition, int maxHp)
    {
        this.scene = scene;
        MaxHP = maxHp;
        HP = MaxHP;
        Position = spawnPosition;
        RestingPosition = spawnPosition;
    }

    /// <summary>
    ///     Updates this enemy instance.
    /// </summary>
    public abstract void Update();

    /// <summary>
    ///     Executes on-kill events for this enemy.
    /// </summary>
    public virtual void OnKill() { }

    /// <summary>
    ///     Hurts this enemy for a given
    ///     damage quantity.
    /// </summary>
    public virtual void Hurt(int damage, bool strong)
    {
        HP -= damage;
        if (HP <= 0)
            OnKill();
    }

    /// <summary>
    ///     Creates a hurt text particle for this enemy.
    /// </summary>
    public DamageIndicator CreateHurtTextParticle(int damage, Color color)
    {
        Vector3 indicatorVelocity = new Vector3(scene.Rng.NextFloat(700f, 800f), scene.Rng.NextFloat(-210f, -150f), 0f);

        int tries = 0;
        Vector3 indicatorSpawnPosition;
        do
        {
            indicatorSpawnPosition = Position + new Vector3(0f, scene.Rng.NextFloat(-120f, 100f), 120f);
            tries++;
        }
        while (tries < 300 && scene.DamageIndicators.Any(d => Vector3.Distance(d.Position, indicatorSpawnPosition) < 100f));

        DamageIndicator indicator = new DamageIndicator()
        {
            Value = damage,
            Position = indicatorSpawnPosition,
            Velocity = indicatorVelocity,
            Scale = Vector2.One,
            Color = color
        };
        scene.DamageIndicators.Add(indicator);

        return indicator;
    }

    /// <summary>
    ///     Performs special optional effects when an attack ends.
    /// </summary>
    public virtual void OnAttackEnd() { }

    /// <summary>
    ///     Chooses a hero for this enemy to target if
    ///     the soul was hit by a bullet.
    /// </summary>
    public abstract Hero ChooseTargetForHurtDamage(BattleHandler battle, int expectedBaseDamage);

    /// <summary>
    ///     Chooses the next attack that this enemy should
    ///     perform in a battle.
    /// </summary>
    public abstract Attack ChooseNextAttack(BattleHandler battle);

    /// <summary>
    ///     Prepares this enemy for rendering.
    /// </summary>
    public abstract void PrepareRender(SpriteRenderer sprites);
}
