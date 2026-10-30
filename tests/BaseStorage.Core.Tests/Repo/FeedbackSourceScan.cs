namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Ui;

/// <summary>
/// S-E008-001 section 5 (ADR-GWIN-153): the lexer-based sink scanner over the Plugin sources. It reads comments and literals
/// correctly (verbatim, interpolated, raw and char literals), keeps line numbers through comment stripping, and reports FS1 and
/// FS2 violations in the 5.4 format. Pure test-side functions over text. Not a test class.
/// </summary>
public static class FeedbackSourceScan
{
    /// <summary>The Plugin files that may pass a key to a sink as a bare identifier (FS1 form e).</summary>
    public static readonly IReadOnlyList<string> PassThroughFiles = new[]
    {
        "src/BaseStorage.Plugin/Crafting/GameCraftGame.cs",
        "src/BaseStorage.Plugin/Building/GameBuildGame.cs",
        "src/BaseStorage.Plugin/Organize/GameOrganizeRunPort.cs",
        "src/BaseStorage.Plugin/Networking/PluginVersionHandshake.cs",
    };

    private static readonly (string Name, Regex Pattern)[] CallSinks =
    [
        ("Notices.Show", new Regex(@"\bNotices\.Show\(", RegexOptions.CultureInvariant)),
        (".ShowNotice", new Regex(@"\.ShowNotice\(", RegexOptions.CultureInvariant)),
        (".ShowMessage", new Regex(@"\.ShowMessage\(", RegexOptions.CultureInvariant)),
        (".Message", new Regex(@"\.Message\(", RegexOptions.CultureInvariant)),
        ("VanillaButtonFactory.Create", new Regex(@"\bVanillaButtonFactory\.Create\(", RegexOptions.CultureInvariant)),
        ("VanillaButtonFactory.SetLabel", new Regex(@"\bVanillaButtonFactory\.SetLabel\(", RegexOptions.CultureInvariant)),
        ("VanillaButtonFactory.CreateText", new Regex(@"\bVanillaButtonFactory\.CreateText\(", RegexOptions.CultureInvariant)),
        ("new YesNoPopup", new Regex(@"\bnew\s+YesNoPopup\(", RegexOptions.CultureInvariant)),
        ("UnifiedPopup.Push", new Regex(@"\bUnifiedPopup\.Push\(", RegexOptions.CultureInvariant)),
        (".SetText", new Regex(@"\.SetText\(", RegexOptions.CultureInvariant)),
    ];

    private static readonly Regex AssignmentSink = new(@"\.(text|m_text|m_topic)\s*=(?!=)", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> StringKeyNames = typeof(StringKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => field.Name)
        .ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> FeedbackOutcomeNames = Enum.GetNames(typeof(FeedbackOutcome)).ToHashSet(StringComparer.Ordinal);

    /// <summary>The kinds of lexed spans.</summary>
    public enum SpanKind
    {
        /// <summary>A // comment, up to but not including the line break.</summary>
        LineComment,

        /// <summary>A /* */ comment.</summary>
        BlockComment,

        /// <summary>A "..." string literal.</summary>
        Regular,

        /// <summary>A @"..." string literal.</summary>
        Verbatim,

        /// <summary>A $"...", $@"..." or @$"..." string literal.</summary>
        Interpolated,

        /// <summary>A """...""" raw string literal.</summary>
        Raw,

        /// <summary>A 'x' character literal.</summary>
        Char,
    }

    /// <summary>One lexed span: its kind and the half-open index range [Start, End).</summary>
    public readonly record struct LexSpan(SpanKind Kind, int Start, int End);

    /// <summary>One top-level argument of a sink: its text, trimmed, and the index of its first character.</summary>
    public sealed record Argument(string Text, int Index);

    /// <summary>One sink occurrence: its display name, the index of its first character and its top-level arguments.</summary>
    public sealed record Sink(string Name, int Index, IReadOnlyList<Argument> Arguments);

    /// <summary>
    /// Every Plugin source file, as read by <see cref="PluginSourceScan.Sources"/> (the same file set), CRLF-normalised and with
    /// comments blanked by <see cref="StripKeepingLines"/>. Keys are forward-slash repo-relative paths, in ordinal order.
    /// </summary>
    /// <returns>The stripped text of every Plugin file.</returns>
    public static Dictionary<string, string> Read()
    {
        Dictionary<string, string> files = PluginSourceScan.Sources();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in files.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            result[path] = StripKeepingLines(RepoRoot.ReadText(path));
        }

        return result;
    }

    /// <summary>
    /// Blanks every comment character of the text, except line breaks, so that the line numbers and the columns of the code
    /// survive. Literals are left intact.
    /// </summary>
    /// <param name="text">The source text, CRLF-normalised.</param>
    /// <returns>The text with comment characters replaced by spaces.</returns>
    public static string StripKeepingLines(string text)
    {
        char[] chars = text.ToCharArray();
        foreach (LexSpan span in Lex(text))
        {
            if (span.Kind is SpanKind.LineComment or SpanKind.BlockComment)
            {
                Fill(chars, span, ' ');
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Finds the sink calls and assignment sinks of a comment-stripped text, in position order. Sinks inside literals never match,
    /// because the literal contents are masked before the search.
    /// </summary>
    /// <param name="strippedText">Text from <see cref="StripKeepingLines"/>.</param>
    /// <returns>Every sink, ordered by its index.</returns>
    public static IReadOnlyList<Sink> Sinks(string strippedText)
    {
        string masked = Mask(strippedText);
        var sinks = new List<Sink>();
        foreach ((string name, Regex pattern) in CallSinks)
        {
            foreach (Match match in pattern.Matches(masked))
            {
                int open = match.Index + match.Length - 1;
                int close = MatchingClose(masked, open);
                sinks.Add(new Sink(name, match.Index, CallArguments(strippedText, masked, open + 1, close)));
            }
        }

        foreach (Match match in AssignmentSink.Matches(masked))
        {
            int start = match.Index + match.Length;
            int end = EndOfAssignment(masked, start);
            var arguments = new List<Argument>();
            AddArgument(arguments, strippedText, start, end);
            sinks.Add(new Sink("." + match.Groups[1].Value + " =", match.Index, arguments));
        }

        return sinks.OrderBy(sink => sink.Index).ToList();
    }

    /// <summary>
    /// The FS1 and FS2 violations of one comment-stripped Plugin file, in the 5.4 format, in sink order. FS1 applies to the key
    /// argument of Notices.Show, VanillaButtonFactory.Create and VanillaButtonFactory.SetLabel; FS2 to every wording literal at any
    /// sink. FS3 (the single MessageHud path) is checked by its own existing scan.
    /// </summary>
    /// <param name="relativePath">The forward-slash repo-relative path of the file.</param>
    /// <param name="strippedText">The comment-stripped text of the file.</param>
    /// <returns>One line per violation.</returns>
    public static IReadOnlyList<string> Violations(string relativePath, string strippedText)
    {
        string path = relativePath.Replace('\\', '/');
        var result = new List<string>();
        foreach (Sink sink in Sinks(strippedText))
        {
            int keyPosition = KeyPositionOf(sink.Name);
            if (keyPosition >= 0)
            {
                Argument? key = keyPosition < sink.Arguments.Count ? sink.Arguments[keyPosition] : null;
                string text = key?.Text ?? string.Empty;
                if (!KeyFormAllowed(text, path))
                {
                    int index = key?.Index ?? sink.Index;
                    result.Add($"FS1: {path}:{LineOf(strippedText, index)}: {sink.Name} key argument '{text}' is not an allowed key form");
                }
            }

            foreach (Argument argument in sink.Arguments)
            {
                foreach ((int index, string literal) in WordingLiterals(argument.Text, argument.Index))
                {
                    result.Add($"FS2: {path}:{LineOf(strippedText, index)}: {sink.Name} has wording literal '{literal}'");
                }
            }
        }

        return result;
    }

    /// <summary>The 1-based line of an index in a text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="index">A character index into the text.</param>
    /// <returns>The line number.</returns>
    public static int LineOf(string text, int index)
    {
        int line = 1;
        for (int k = 0; k < index && k < text.Length; k++)
        {
            if (text[k] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>
    /// Lexes a text into comment and literal spans. Comments and literals are recognised in the order C# recognises them, so a
    /// quote inside a comment, a comment marker inside a string and a quote inside a char literal are all read correctly.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The spans in text order.</returns>
    public static List<LexSpan> Lex(string text)
    {
        var spans = new List<LexSpan>();
        int n = text.Length;
        int i = 0;
        while (i < n)
        {
            char c = text[i];
            if (c == '/' && i + 1 < n && text[i + 1] == '/')
            {
                int end = text.IndexOf('\n', i);
                end = end < 0 ? n : end;
                spans.Add(new LexSpan(SpanKind.LineComment, i, end));
                i = end;
                continue;
            }

            if (c == '/' && i + 1 < n && text[i + 1] == '*')
            {
                int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                int end = close < 0 ? n : close + 2;
                spans.Add(new LexSpan(SpanKind.BlockComment, i, end));
                i = end;
                continue;
            }

            if (c == '"')
            {
                bool verbatim = (i >= 1 && text[i - 1] == '@') || (i >= 2 && text[i - 1] == '$' && text[i - 2] == '@');
                bool interpolated = (i >= 1 && text[i - 1] == '$') || (i >= 2 && text[i - 1] == '@' && text[i - 2] == '$');
                if (i + 2 < n && text[i + 1] == '"' && text[i + 2] == '"')
                {
                    int end = SkipRaw(text, i);
                    spans.Add(new LexSpan(SpanKind.Raw, i, end));
                    i = end;
                }
                else
                {
                    SpanKind kind = interpolated ? SpanKind.Interpolated : verbatim ? SpanKind.Verbatim : SpanKind.Regular;
                    int end = SkipString(text, i, verbatim, interpolated);
                    spans.Add(new LexSpan(kind, i, end));
                    i = end;
                }

                continue;
            }

            if (c == '\'')
            {
                int end = SkipChar(text, i);
                spans.Add(new LexSpan(SpanKind.Char, i, end));
                i = end;
                continue;
            }

            i++;
        }

        return spans;
    }

    private static void Fill(char[] chars, LexSpan span, char fill)
    {
        for (int k = span.Start; k < span.End; k++)
        {
            if (chars[k] != '\n')
            {
                chars[k] = fill;
            }
        }
    }

    /// <summary>Masks comments (to spaces) and literal contents (to underscores), keeping every index and line break.</summary>
    private static string Mask(string text)
    {
        char[] chars = text.ToCharArray();
        foreach (LexSpan span in Lex(text))
        {
            bool comment = span.Kind is SpanKind.LineComment or SpanKind.BlockComment;
            Fill(chars, span, comment ? ' ' : '_');
        }

        return new string(chars);
    }

    /// <summary>Returns the index after a regular, verbatim or interpolated string that starts at <paramref name="start"/>.</summary>
    private static int SkipString(string text, int start, bool verbatim, bool interpolated)
    {
        int n = text.Length;
        int j = start + 1;
        while (j < n)
        {
            char d = text[j];
            if (!verbatim && d == '\\')
            {
                j += 2;
                continue;
            }

            if (!verbatim && d == '\n')
            {
                return j;
            }

            if (interpolated && d == '{')
            {
                if (j + 1 < n && text[j + 1] == '{')
                {
                    j += 2;
                    continue;
                }

                j = SkipHole(text, j);
                continue;
            }

            if (d == '"')
            {
                if (verbatim && j + 1 < n && text[j + 1] == '"')
                {
                    j += 2;
                    continue;
                }

                return j + 1;
            }

            j++;
        }

        return n;
    }

    /// <summary>Returns the index after the interpolation hole that opens at <paramref name="open"/>.</summary>
    private static int SkipHole(string text, int open)
    {
        int depth = 0;
        int j = open;
        while (j < text.Length)
        {
            char d = text[j];
            if (d == '{')
            {
                depth++;
            }
            else if (d == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return j + 1;
                }
            }
            else if (d == '"')
            {
                j = SkipString(text, j, false, false);
                continue;
            }
            else if (d == '\'')
            {
                j = SkipChar(text, j);
                continue;
            }

            j++;
        }

        return text.Length;
    }

    private static int SkipRaw(string text, int start)
    {
        int close = text.IndexOf("\"\"\"", start + 3, StringComparison.Ordinal);
        return close < 0 ? text.Length : close + 3;
    }

    private static int SkipChar(string text, int start)
    {
        int j = start + 1;
        j = j < text.Length && text[j] == '\\' ? j + 2 : j + 1;
        while (j < text.Length && text[j] != '\'' && text[j] != '\n')
        {
            j++;
        }

        return j < text.Length && text[j] == '\'' ? j + 1 : Math.Min(j, text.Length);
    }

    private static int MatchingClose(string masked, int open)
    {
        int depth = 0;
        for (int k = open; k < masked.Length; k++)
        {
            if (masked[k] == '(')
            {
                depth++;
            }
            else if (masked[k] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return k;
                }
            }
        }

        return masked.Length;
    }

    private static int EndOfAssignment(string masked, int start)
    {
        int depth = 0;
        for (int k = start; k < masked.Length; k++)
        {
            char c = masked[k];
            if (c is '(' or '[' or '{')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}')
            {
                depth--;
            }
            else if (c == ';' && depth <= 0)
            {
                return k;
            }
        }

        return masked.Length;
    }

    private static List<Argument> CallArguments(string text, string masked, int from, int to)
    {
        var arguments = new List<Argument>();
        if (to <= from)
        {
            return arguments;
        }

        int depth = 0;
        int start = from;
        for (int k = from; k < to; k++)
        {
            char c = masked[k];
            if (c is '(' or '[' or '{')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                AddArgument(arguments, text, start, k);
                start = k + 1;
            }
        }

        AddArgument(arguments, text, start, to);
        return arguments;
    }

    private static void AddArgument(List<Argument> arguments, string text, int start, int end)
    {
        int s = start;
        while (s < end && char.IsWhiteSpace(text[s]))
        {
            s++;
        }

        int e = end;
        while (e > s && char.IsWhiteSpace(text[e - 1]))
        {
            e--;
        }

        arguments.Add(new Argument(text.Substring(s, e - s), s));
    }

    private static int KeyPositionOf(string sinkName) => sinkName switch
    {
        "Notices.Show" => 1,
        "VanillaButtonFactory.Create" => 3,
        "VanillaButtonFactory.SetLabel" => 1,
        _ => -1,
    };

    /// <summary>The FS1 forms (a) to (e) of 5.2.</summary>
    private static bool KeyFormAllowed(string argument, string path)
    {
        Match stringKey = Regex.Match(argument, @"^StringKeys\.([A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.CultureInvariant);
        if (stringKey.Success)
        {
            return StringKeyNames.Contains(stringKey.Groups[1].Value);
        }

        if (Regex.IsMatch(argument, @"^CategoryWriteRules\.NoticeKey\(.+\)$", RegexOptions.Singleline | RegexOptions.CultureInvariant))
        {
            return true;
        }

        if (Regex.IsMatch(argument, @"^ProfileFavourites\.MessageKeyFor\(.+\)$", RegexOptions.Singleline | RegexOptions.CultureInvariant))
        {
            return true;
        }

        Match feedback = Regex.Match(argument, @"^FeedbackCatalog\.For\(FeedbackOutcome\.([A-Za-z_][A-Za-z0-9_]*)\)\.Key!?$", RegexOptions.CultureInvariant);
        if (feedback.Success)
        {
            return FeedbackOutcomeNames.Contains(feedback.Groups[1].Value);
        }

        return argument == "key" && PassThroughFiles.Contains(path);
    }

    private static IEnumerable<(int Index, string Literal)> WordingLiterals(string argumentText, int argumentIndex)
    {
        foreach (LexSpan span in Lex(argumentText))
        {
            if (span.Kind is SpanKind.LineComment or SpanKind.BlockComment or SpanKind.Char)
            {
                continue;
            }

            int start = span.Start;
            while (start > 0 && span.Start - start < 2 && (argumentText[start - 1] == '@' || argumentText[start - 1] == '$'))
            {
                start--;
            }

            string literal = argumentText.Substring(start, span.End - start);
            if (IsWording(span.Kind, literal))
            {
                yield return (argumentIndex + start, literal);
            }
        }
    }

    /// <summary>
    /// A wording literal (5.2): an interpolated string whatever it holds, or a string whose content, after the escape sequences are
    /// removed, has an ASCII letter.
    /// </summary>
    private static bool IsWording(SpanKind kind, string literal)
    {
        if (kind == SpanKind.Interpolated)
        {
            return true;
        }

        int first = literal.IndexOf('"');
        int last = literal.LastIndexOf('"');
        if (first < 0 || last <= first)
        {
            return false;
        }

        string content = literal.Substring(first + 1, last - first - 1);
        string withoutEscapes = Regex.Replace(content, @"\\(u[0-9A-Fa-f]{4}|x[0-9A-Fa-f]{1,4}|.)", string.Empty, RegexOptions.CultureInvariant);
        return Regex.IsMatch(withoutEscapes, "[A-Za-z]", RegexOptions.CultureInvariant);
    }
}
