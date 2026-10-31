namespace Northernlight.Content.Knight.Dialogue;

/// <summary>
///     Represents a system that handles all mid-battle
///     dialogue.
/// </summary>
public sealed class CharacterDialogueHandler
{
    /// <summary>
    ///     The handler responsible for this dialogue system.
    /// </summary>
    public BattleHandler Battle
    {
        get;
    }

    /// <summary>
    ///     The mechanism responsible for the dialogue handling.
    /// </summary>
    public BattleDialogueExchange? Dialogue
    {
        get;
        private set;
    }

    /// <summary>
    ///     The game responsible for this dialogue.
    /// </summary>
    public KnightFightScene Scene => Battle.Scene;

    /// <summary>
    ///     Whether this handler is active or not.
    /// </summary>
    public bool Active => Dialogue is not null && !Dialogue.Done;

    /// <summary>
    ///     The pause indicator character.
    /// </summary>
    public const char PauseIndicator = '#';

    public CharacterDialogueHandler(BattleHandler battle)
    {
        Battle = battle;
    }

    /// <summary>
    ///     Evaluates whether text needs to be applied based on
    ///     battle context and applies it for the end
    ///     of the upcoming turn if so.
    /// </summary>
    public void Evaluate()
    {
    }

    /// <summary>
    ///     Resets the dialogue manager to say specific lines.
    /// </summary>
    public void SetLines(params CharacterDialogueLine[] lines) =>
        Dialogue = new BattleDialogueExchange(Battle.Scene, lines);
}
