using Northernlight.Core.Localization;

namespace Northernlight.Content.Knight.Dialogue;

/// <summary>
///     Represents an instance of a dialogue line
///     that a character can say.
/// </summary>
public sealed class CharacterDialogueLine
{
    /// <summary>
    ///     This line's speaker.
    /// </summary>
    public ISpeakingCharacter Speaker
    {
        get;
        private set;
    }

    /// <summary>
    ///     The full text to spell out.
    /// </summary>
    public LanguageAgnosticText FullText
    {
        get;
        private set;
    }

    public CharacterDialogueLine(ISpeakingCharacter speaker, LanguageAgnosticText fullText)
    {
        Speaker = speaker;
        FullText = fullText;
    }
}
