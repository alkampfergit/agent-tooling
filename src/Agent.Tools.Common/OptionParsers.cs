using System.CommandLine.Parsing;

namespace Agent.Tools.Common;

/// <summary>
/// Custom parsers for System.CommandLine options, shared by every tool so that
/// out-of-range and non-numeric values are reported the same way everywhere.
/// </summary>
public static class OptionParsers
{
    /// <summary>
    /// Converts an option token to an int within an inclusive range. Both the conversion
    /// and the range check happen during parsing, so a bad value is reported as a parse
    /// error instead of reaching the command handler.
    /// </summary>
    /// <example>
    /// <code>
    /// new Option&lt;int&gt;("--limit")
    /// {
    ///     Description = "Maximum items to return (1-50, default 10).",
    ///     DefaultValueFactory = _ => 10,
    ///     CustomParser = result => OptionParsers.BoundedInt(result, "--limit", 1, 50)
    /// };
    /// </code>
    /// </example>
    public static int BoundedInt(ArgumentResult result, string optionName, int min, int max)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError($"{optionName} requires a value between {min} and {max}.");
            return min;
        }

        if (!int.TryParse(result.Tokens[0].Value, out var value))
        {
            result.AddError($"{optionName} must be a whole number between {min} and {max}.");
            return min;
        }

        if (value < min || value > max)
        {
            result.AddError($"{optionName} must be between {min} and {max}.");
            return min;
        }

        return value;
    }
}
