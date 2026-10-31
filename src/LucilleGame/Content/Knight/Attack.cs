using System.Numerics;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;

namespace Northernlight.Content.Knight;

public abstract class Attack
{
    /// <summary>
    ///     The battle handler responsible for this attack.
    /// </summary>
    public BattleHandler Battle
    {
        get;
        private set;
    }

    /// <summary>
    ///     The enemy responsible for attacking.
    /// </summary>
    public virtual Enemy AttackerGeneric
    {
        get;
        set;
    }

    /// <summary>
    ///     The set of all bullets active in the battle.
    /// </summary>
    public readonly List<Bullet> Bullets = [];

    /// <summary>
    ///     The attack timer.
    /// </summary>
    public OwnedTimer AttackTimer => Battle.StateTimer;

    /// <summary>
    ///     The position of the target soul.
    /// </summary>
    public Vector2 TargetPosition => Scene.BattleHandler.Soul.Position.AsVector2();

    /// <summary>
    ///     The game responsible for this attack.
    /// </summary>
    public KnightFightScene Scene => Battle.Scene;

    /// <summary>
    ///     Whether this attack skips the box appearance animation
    ///     or not,
    /// </summary>
    public virtual bool SkipsBoxAppearance => false;

    /// <summary>
    ///     The size that the battle box should
    ///     grow to at the start of this attack.
    /// </summary>
    public abstract Vector2 StartingBoxSize
    {
        get;
    }

    /// <summary>
    ///     The flavor text to display in the game menu
    ///     before this attack occurs, thus serving as a subtle
    ///     indicator to the player if they realize.
    /// </summary>
    public abstract LanguageAgnosticText MenuIndicatorFlavorText
    {
        get;
    }

    public Attack(BattleHandler battle) => Battle = battle;

    protected abstract void UpdateInternal();

    /// <summary>
    ///     Updates this attack.
    /// </summary>
    public void Update()
    {
        // ??????
        if (Battle.Box.Size == Vector2.Zero)
            Battle.Box.Size = StartingBoxSize;

        UpdateInternal();
        for (int i = 0; i < Bullets.Count; i++)
            Bullets[i].Update();

        Bullets.RemoveAll(b => b.Dead);
    }

    /// <summary>
    ///     Renders all bullets associated with this
    ///     attack.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, FrameContext context)
    {
        ExtraPrepareRender(sprites);

        foreach (Bullet bullet in Bullets)
            bullet.PrepareRender(sprites, context);
    }

    /// <summary>
    ///     Allows for optional overriding of the battle box's
    ///     rendering.
    /// </summary>
    public virtual bool OverrideBattleBoxRender(Renderer renderer, FrameContext frame) => false;

    /// <summary>
    ///     Allows for specific actions to occur the moment the attack begins.
    /// </summary>
    public virtual void OnStart() { }

    /// <summary>
    ///     Allows for the automatic cleanup of certain
    ///     visuals at the end of the attack.
    /// </summary>
    public virtual void OnEnd() { }

    /// <summary>
    ///     Prepares optional extra effects for this attack.
    /// </summary>
    public virtual void ExtraPrepareRender(SpriteRenderer sprites) { }

    /// <summary>
    ///     Prepares optional extra effects for this attack
    ///     on top of the soul layer.
    /// </summary>
    public virtual void ExtraPrepareRender_SoulLayer(SpriteRenderer sprites, FrameContext context) { }
}

/// <summary>
///     Represents an instance of a bullet pattern
///     attack that enemies can perform.
/// </summary>
public abstract class Attack<TEnemy> : Attack where TEnemy : Enemy
{
    public Attack(BattleHandler battle) : base(battle)
    {
    }

    public override Enemy AttackerGeneric
    {
        get => Attacker;
        set
        {
            if (value is TEnemy t)
                Attacker = t;
        }
    }

    /// <summary>
    ///     The explicit form of <see cref="AttackerGeneric"/>.
    /// </summary>
    public TEnemy Attacker
    {
        get;
        internal set;
    }

    /// <summary>
    ///     Ends this attack.
    /// </summary>
    public void End()
    {
        OnEnd();
        Bullets.Clear();
        Battle.EndAttack();
        Attacker.OnAttackEnd();

        if (Battle.Box.Size.Length() >= 10000f)
        {
            Battle.Box.Size = Vector2.One;
            Battle.Box.ClearAfterimages(false);
        }
    }
}
