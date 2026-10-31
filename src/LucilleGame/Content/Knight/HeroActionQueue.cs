namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a set of actions that can be
///     performed by a hero at the end of a battle.
/// </summary>
public sealed class HeroActionQueue
{
    /// <summary>
    ///     The set of all queued actions to
    ///     perform.
    /// </summary>
    public Stack<QueuedHeroAction> Actions
    {
        get;
        internal set;
    } = [];

    /// <summary>
    ///     Adds a new action to the queue.
    /// </summary>
    public void AddNew(KnightFightScene scene, QueuedHeroAction action)
    {
        action.Select(scene);
        Actions.Push(action);
    }
}
