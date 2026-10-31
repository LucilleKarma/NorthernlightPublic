using Northernlight.Core;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents a kind of action that one of the
///     heroes can perform at the end of their turn.
/// </summary>
public sealed class QueuedHeroAction
{
    private bool startedPerformanceYet;

    /// <summary>
    ///     Whether this action should occur at the
    ///     same time as other actions.
    /// </summary>
    /// <remarks>
    ///     Note that in this context "asynchronously" just means
    ///     that it doesn't go through the queue one by one, not
    ///     true asynchronous behavior at the CPU level.
    /// </remarks>
    public bool OccursAsynchronously
    {
        get;
        set;
    }

    /// <summary>
    ///     The priority of this action.
    ///     Used to make certain actions happen before others in
    ///     sequence.
    /// </summary>
    public int Priority
    {
        get;
        set;
    }

    /// <summary>
    ///     The max priority of this action, taking into acount
    ///     all sub-actions.
    /// </summary>
    public int MaxPriorityConsideringSubActions => int.Max(Priority, SubActions.Count <= 0 ? 0 : SubActions.Max(a => a.MaxPriorityConsideringSubActions));

    /// <summary>
    ///     The hero that will perform the action.
    /// </summary>
    public Hero Hero
    {
        get;
        init;
    }

    /// <summary>
    ///     A timer reserved for use when
    ///     this action is ongoing.
    /// </summary>
    public OwnedTimer AnimationTimer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The button that created this action.
    /// </summary>
    public BattleMenuActionInterface.Button Button
    {
        get;
        private set;
    }

    /// <summary>
    ///     A function indicating whether this
    ///     action has finished or not.
    /// </summary>
    public Func<QueuedHeroAction, bool> HasFinished
    {
        get;
        set;
    }

    /// <summary>
    ///     The set of all sub-actions performed by this
    ///     action.
    /// </summary>
    public readonly List<QueuedHeroAction> SubActions = [];

    /// <summary>
    ///     The event to fire when the action is
    ///     selected in the menu.
    /// </summary>
    /// <remarks>
    ///     This is primarily for things like animation
    ///     preparations, such as making Susie hold
    ///     her axe if she's gonna use Rude Buster.
    /// </remarks>
    public event Action<QueuedHeroAction> OnSelection;

    /// <summary>
    ///     The event to fire when the action is
    ///     ongoing.
    /// </summary>
    public event Action<QueuedHeroAction> OnUpdate;

    /// <summary>
    ///     The event to fire when the action begins
    ///     occuring.
    /// </summary>
    public event Action<QueuedHeroAction> OnStartPerformance;

    /// <summary>
    ///     The event to fire when the action ends.
    /// </summary>
    /// <remarks>
    ///     This can include things such as dealing
    ///     damage at the end of Susie's rude buster, which
    ///     has a delay between being used and actualling
    ///     doing damage.
    /// </remarks>
    public event Action<QueuedHeroAction> OnEndPerformance;

    /// <summary>
    ///     The event to fire when the action is cancelled
    ///     by the player.
    /// </summary>
    /// <remarks>
    ///     This primarily exists for the purposes of cleanup
    ///     work, such as making the hero undo their defending
    ///     animation, for example.
    /// </remarks>
    public event Action<QueuedHeroAction> OnCancellation;

    public QueuedHeroAction(Hero hero, BattleMenuActionInterface.Button button, Func<QueuedHeroAction, bool>? hasFinished = null)
    {
        Hero = hero;
        Button = button;
        HasFinished = hasFinished ?? (_ => true);
    }

    /// <summary>
    ///     Selects this hero action.
    /// </summary>
    public void Select(KnightFightScene scene)
    {
        OnSelection?.Invoke(this);

        if (!scene.battleMenu.actionsTakenByHero.ContainsKey(Hero))
            scene.battleMenu.actionsTakenByHero[Hero] = [];

        scene.battleMenu.actionsTakenByHero[Hero].Add(this);

        foreach (QueuedHeroAction subAction in SubActions)
            subAction.Select(scene);
    }

    /// <summary>
    ///     Cancels this hero action in the queue.
    /// </summary>
    public void Cancel(KnightFightScene scene)
    {
        OnCancellation?.Invoke(this);

        if (!scene.battleMenu.actionsTakenByHero.ContainsKey(Hero))
            scene.battleMenu.actionsTakenByHero[Hero] = [];

        scene.battleMenu.actionsTakenByHero[Hero].Remove(this);

        foreach (QueuedHeroAction subAction in SubActions)
            subAction.Cancel(scene);
        SubActions.Clear();

        var withoutThis = scene.battleMenu.PlayerActionQueue.Actions.Where(a => a != this);
        scene.battleMenu.PlayerActionQueue.Actions = new Stack<QueuedHeroAction>(withoutThis);
    }

    /// <summary>
    ///     Starts this hero action.
    /// </summary>
    public void StartPerformance(Game game)
    {
        AnimationTimer ??= game.Time.Rent();
        AnimationTimer.Reset();

        OnStartPerformance?.Invoke(this);

        foreach (QueuedHeroAction action in SubActions)
            action.StartPerformance(game);
    }

    /// <summary>
    ///     Clears all performance events for this
    ///     action.
    /// </summary>
    public void ClearPerformanceEvents()
    {
        OnStartPerformance = null!;
        OnEndPerformance = null!;
    }

    /// <summary>
    ///     Updates this hero action, returning whether
    ///     it's done or not.
    /// </summary>
    public bool Update(Game game)
    {
        if (!startedPerformanceYet)
        {
            startedPerformanceYet = true;
            StartPerformance(game);
        }

        OnUpdate?.Invoke(this);
        if (HasFinished(this) && SubActions.All(a => a.HasFinished(this)))
        {
            OnEndPerformance?.Invoke(this);
            AnimationTimer.Reset();
            return true;
        }

        foreach (QueuedHeroAction action in SubActions)
            action.Update(game);

        return false;
    }
}
