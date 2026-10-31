using System.Diagnostics;
using Northernlight.Core.Localization;

namespace Northernlight.Content.Knight.Items;

/// <summary>
///     Represents an instance of a consumable
///     item that is usable in battle.
/// </summary>
[DebuggerDisplay("{Name.Value}")]
public sealed class Item
{
    /// <summary>
    ///     Whether this item is about to be used up
    ///     in battle or not.
    /// </summary>
    public bool IntendedToBeUsedUp
    {
        get;
        set;
    }

    /// <summary>
    ///     The tension point bonus percentage that this item
    ///     grants.
    /// </summary>
    public int TensionPointBonusPercentage
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this item affects all heroes
    ///     or not.
    /// </summary>
    public bool AffectsEveryone
    {
        get;
        set;
    }

    /// <summary>
    ///     The displayed name of this item.
    /// </summary>
    public required LanguageAgnosticText Name
    {
        get;
        init;
    }

    /// <summary>
    ///     The displayed description lines of this item.
    /// </summary>
    public required LanguageAgnosticText[] DescriptionLines
    {
        get;
        init;
    }

    /// <summary>
    ///     The action to perform when this item is used.
    /// </summary>
    public required Action<Hero> OnUse
    {
        get;
        init;
    }
}
