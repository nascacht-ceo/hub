using System.Text.RegularExpressions;

namespace nc.Hub;

public readonly struct SafeString
{
    public string Value { get; }

    public SafeString(string originalString, string replacement = "_")
    {
        Value = Sanitize(originalString, replacement);
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Value cannot be null or whitespace after sanitization.", nameof(originalString));
    }

    public static implicit operator SafeString(string raw) => new SafeString(raw);

    public static implicit operator string(SafeString safe) => safe.Value;

    public override string ToString() => Value;

    private static string Sanitize(string input, string replacement = "_")
    {
        var clean = Regex.Replace(input, @"[^\w\.]", replacement); // Keep letters, digits, underscores, periods
        if (char.IsDigit(clean[0])) clean = replacement + clean;
        return clean;
    }
}

