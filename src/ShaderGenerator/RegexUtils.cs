using System.Globalization;
using System.Text.RegularExpressions;

namespace ShaderGenerator;

/// <summary>
///     A simple provider for regular expression utilities used
///     frequently throughout the shader definition generator.
/// </summary>
public static class RegexUtils
{
    /// <summary>
    ///     Counts the unique amount of numerical matches in a given text
    ///     based on a given set of regular expressions and matching group index.
    /// </summary>
    internal static int CountUniqueFromRegex(string text, int groupIndex, params Regex[] regularExpressions)
    {
        HashSet<int> usedSlots = [];
        List<Match> matches = [];
        foreach (Regex regex in regularExpressions)
            matches.AddRange(regex.Matches(text));

        foreach (Match match in matches)
        {
            string slotIndexText = match.Groups[groupIndex].Value;
            if (int.TryParse(slotIndexText, CultureInfo.InvariantCulture, out int slotIndex))
                usedSlots.Add(slotIndex);
        }

        return usedSlots.Count;
    }
}
