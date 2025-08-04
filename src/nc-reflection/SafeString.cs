using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection;

using System.Text.RegularExpressions;

public readonly struct SafeString
{
    public string Value { get; }

    public SafeString(string originalString)
    {
        Value = Sanitize(originalString);
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Value cannot be null or whitespace after sanitization.", nameof(originalString));
    }

    public static implicit operator SafeString(string raw) => new SafeString(raw);

    public static implicit operator string(SafeString safe) => safe.Value;

    public override string ToString() => Value;

    private static string Sanitize(string input)
    {
        var clean = Regex.Replace(input, @"[^\w\.]", "_"); // Keep letters, digits, underscores, periods
        if (char.IsDigit(clean[0])) clean = "_" + clean;
        return clean;
    }
}

