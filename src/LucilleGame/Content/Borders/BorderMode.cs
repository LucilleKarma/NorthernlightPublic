using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Northernlight.Content.Borders;

/// <summary>
///     Represents a kind of user-selected
///     mode for borders.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum BorderMode
{
    None,
    Simple,
    Dynamic
}
