namespace Northernlight.Core.Localization;

/// <summary>
///     A simple wrapper for language-agnostic text
///     whose value is only evaluated on the fly, in
///     the interest of allowing mid-game changes to
///     localization to be respected.
/// </summary>
public sealed class LanguageAgnosticText
{
    private string[] formatArguments = Array.Empty<string>();

    internal string key;

    /// <summary>
    ///     The value of this text.
    /// </summary>
    public string Value => formatArguments.Length >= 1 ? LocalizationSystem.Active.Get(key, formatArguments) : LocalizationSystem.Active.Get(key);

    /// <summary>
    ///     Specifies format arguments for this text.
    /// </summary>
    public LanguageAgnosticText WithArgs(params string[] formatArguments)
    {
        return new LanguageAgnosticText(key)
        {
            formatArguments = formatArguments
        };
    }

    public LanguageAgnosticText(string key) => this.key = key;
}
