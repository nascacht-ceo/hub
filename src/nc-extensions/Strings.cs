using System.Text.RegularExpressions;

/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if the input string matches the specified wildcard pattern.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="pattern">The wildcard pattern to match against (* for any sequence, ? for a single character).</param>
    /// <param name="options">Regex options. Defaults to <see cref="RegexOptions.IgnoreCase"/>.</param>
    /// <param name="milliseconds">Timeout in milliseconds.</param>
    /// <returns>True if the input matches the pattern; otherwise, false.</returns>
    public static bool IsWildcardMatch(this string input, string? pattern, RegexOptions options = RegexOptions.IgnoreCase, int milliseconds = 500)
    {
        if (input == null) return false;
        if (pattern == null) return false;

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, options, TimeSpan.FromMilliseconds(milliseconds));
    }
}
