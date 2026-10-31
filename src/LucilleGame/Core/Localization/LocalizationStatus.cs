using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Northernlight.Core.Localization;

/// <summary>
///     A kind of status indicating the work
///     needed for a localization entry.
/// </summary>
/// <remarks>
///     This exists only for developmental
///     organization reasons.
/// </remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum LocalizationStatus
{
    RoughDraft,
    NeedsFurtherWork,
    Completed,
    NeedsReplacement,
    Obsolete
}
