using System.Text;
using System.Text.RegularExpressions;

namespace Vacanam.Speech.Punctuation;

/// <summary>
/// Converts spoken verbal punctuation, line breaks, symbols, and emojis in transcripts
/// into standard formatted text with proper spacing and auto-capitalization.
/// </summary>
public sealed partial class SmartPunctuationProcessor
{
    // Replacements dictionary with regex boundary checks
    private static readonly (string Pattern, string Replacement)[] PunctuationRules =
    [
        // Formatting & Line breaks
        (@"(?i)\b(new paragraph|next paragraph|double enter)\b", "\n\n"),
        (@"(?i)\b(new line|next line)\b", "\n"),
        (@"(?i)\b(tab key)\b", "\t"),

        // Complex multi-word punctuation
        (@"(?i)\s*\b(question mark)\b", "?"),
        (@"(?i)\s*\b(exclamation mark|exclamation point)\b", "!"),
        (@"(?i)\s*\b(full stop)\b", "."),
        (@"(?i)\s*\b(dot dot dot|ellipsis)\b", "..."),
        (@"(?i)\b(open parenthesis|open paren|left paren)\s*", "("),
        (@"(?i)\s*\b(close parenthesis|close paren|right paren)\b", ")"),
        (@"(?i)\b(open bracket|left bracket)\s*", "["),
        (@"(?i)\s*\b(close bracket|right bracket)\b", "]"),
        (@"(?i)\b(open brace|left brace)\s*", "{"),
        (@"(?i)\s*\b(close brace|right brace)\b", "}"),
        (@"(?i)\b(open quote|start quote)\s*", "\""),
        (@"(?i)\s*\b(close quote|end quote|unquote)\b", "\""),
        (@"(?i)\b(forward slash)\b", "/"),
        (@"(?i)\b(backslash)\b", "\\"),
        (@"(?i)\b(at sign|at the rate)\b", "@"),
        (@"(?i)\b(hash sign|pound sign|hashtag)\b", "#"),
        (@"(?i)\b(dollar sign)\b", "$"),
        (@"(?i)\b(percent sign)\b", "%"),
        (@"(?i)\b(ampersand|and sign)\b", "&"),
        (@"(?i)\b(plus sign)\b", "+"),
        (@"(?i)\b(equals sign|equal to)\b", "="),
        (@"(?i)\b(asterisk)\b", "*"),

        // Single-word punctuation (safe word boundaries)
        (@"(?i)\b(comma)\b", ","),
        (@"(?i)\b(semicolon)\b", ";"),
        (@"(?i)\b(colon)\b", ":"),
        (@"(?i)\b(hyphen|dash)\b", "-"),
        (@"(?i)\b(period)\b", "."),

        // Common Emojis
        (@"(?i)\b(smiley face|smiling face|smile emoji)\b", "😊"),
        (@"(?i)\b(laughing face|lol emoji)\b", "😂"),
        (@"(?i)\b(thumbs up)\b", "👍"),
        (@"(?i)\b(thumbs down)\b", "👎"),
        (@"(?i)\b(fire emoji)\b", "🔥"),
        (@"(?i)\b(heart emoji)\b", "❤️"),
        (@"(?i)\b(rocket emoji)\b", "🚀"),
        (@"(?i)\b(party emoji)\b", "🎉")
    ];

    /// <summary>
    /// Formats a raw transcript by substituting spoken punctuation and fixing spacing &amp; casing.
    /// </summary>
    public string Format(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string result = text;

        // 1. Apply verbal punctuation replacements
        foreach (var (pattern, replacement) in PunctuationRules)
        {
            result = Regex.Replace(result, pattern, replacement);
        }

        // 2. Normalize whitespace around punctuation
        // Remove spaces before: , . ? ! : ; ) ] } %
        result = Regex.Replace(result, @"\s+([,\.\?!:;\)\]}%])", "$1");

        // Remove spaces after opening symbols: ( [ {
        result = Regex.Replace(result, @"([(\[{])\s+", "$1");

        // Ensure space after punctuation if followed by a word or number
        result = Regex.Replace(result, @"([,\.\?!:;])(?=[a-zA-Z0-9])", "$1 ");

        // Remove trailing/leading spaces on each line
        var lines = result.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = Regex.Replace(lines[i], @"[ \t]+", " ").Trim();
        }
        result = string.Join("\n", lines);

        // 3. Auto-capitalize sentences
        result = AutoCapitalize(result);

        return result;
    }

    /// <summary>
    /// Capitalizes the first character and every character following sentence-ending punctuation or newlines.
    /// </summary>
    private static string AutoCapitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        bool capitalizeNext = true;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (capitalizeNext && char.IsLetter(c))
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }

            if (c is '.' or '?' or '!' or '\n')
            {
                capitalizeNext = true;
            }
            else if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c))
            {
                // Normal word character, keep capitalizeNext false
            }
        }

        return sb.ToString();
    }
}
