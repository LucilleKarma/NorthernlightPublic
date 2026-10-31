namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     Represents a kind of custom startup
///     animation that can be played before
///     a battle properly begins.
/// </summary>
public abstract class BattleStartupAnimation
{
    /// <summary>
    ///     Updates this animation, returning whether it is complete
    ///     or not.
    /// </summary>
    public abstract bool Update(BattleHandler battle, Scene scene);
}

public abstract class BattleStartupAnimation<TEnemy> : BattleStartupAnimation where TEnemy : Enemy
{
    /// <summary>
    ///     The enemy responsible for this animation.
    /// </summary>
    public TEnemy Owner
    {
        get;
    }

    public BattleStartupAnimation(TEnemy owner) => Owner = owner;
}
