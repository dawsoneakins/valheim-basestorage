namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>S-FOUND-001 AC-1: the English table holds the contract keys with their exact text (contract 3.2, S1, S10).</summary>
public sealed class StringTableContentsTests
{
    private static readonly Regex TagPattern = new("<[^>]+>", RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)\}", RegexOptions.CultureInvariant);

    // U+0085, U+2028 and U+2029 are written as char casts: C# treats them as newlines inside source text.
    private static readonly char[] BreakCharacters = ['\r', '\n', (char)0x85, (char)0x2028, (char)0x2029];

    // GW-U-047
    [Theory]
    [MemberData(nameof(UiFixtures.EnglishRows), MemberType = typeof(UiFixtures))]
    public void EveryContractRowIsContainedWithItsExactText(string key, string expectedText)
    {
        Assert.True(UiContract.Contains(key), $"Table does not contain {key}.");
        Assert.Equal(expectedText, UiContract.Get(key));
    }

    // GW-U-047
    [Fact]
    public void FixedAcTextsArePinned()
    {
        Assert.Equal("Not in a base", UiContract.Get("message.base.not-in-base"));
        Assert.Equal("Materials moved, try again", UiContract.Get("message.craft.materials-moved"));
        Assert.Equal("Organize in progress by {0}", UiContract.Get("message.base.in-progress"));
        Assert.Equal("Organize in progress by {0}", UiContract.Get("station.actions.disabled-in-progress"));
        Assert.Equal(
            "BaseStorage version mismatch: host {0}, you {1}",
            UiContract.Get("network.version.mismatch"));
    }

    // GW-U-047
    [Fact]
    public void StatusLineBaseResolvesToItsOtherOtherVariant()
    {
        Assert.Equal(
            UiContract.Get("station.actions.status-line.other.other"),
            UiContract.Get("station.actions.status-line"));
    }

    // GW-U-047
    [Fact]
    public void StatusLineFormatsToTheStrippedAcText()
    {
        string formatted = UiContract.Format(
            "station.actions.status-line." + UiContract.PluralForm(6) + "." + UiContract.PluralForm(41),
            6,
            41);

        Assert.Equal(UiFixtures.StatusLineStrippedText, TagPattern.Replace(formatted, string.Empty));
    }

    // GW-U-049
    [Fact]
    public void NoTableTextContainsALineBreakOrSeparator()
    {
        var violations = new List<string>();
        foreach (string key in UiContract.Keys())
        {
            string text = UiContract.Get(key);
            if (text.IndexOfAny(BreakCharacters) >= 0)
            {
                violations.Add(key);
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-049
    [Fact]
    public void MessageAndNetworkTextsContainNoMarkup()
    {
        var violations = new List<string>();
        foreach (string key in UiContract.Keys())
        {
            if ((key.StartsWith("message.", StringComparison.Ordinal) || key.StartsWith("network.", StringComparison.Ordinal))
                && UiContract.Get(key).Contains('<', StringComparison.Ordinal))
            {
                violations.Add(key);
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-049
    [Fact]
    public void BracesOnlyBelongToBareZeroBasedPlaceholdersUpToIndexSix()
    {
        var violations = new List<string>();
        foreach (string key in UiContract.Keys())
        {
            string text = UiContract.Get(key);
            foreach (Match match in PlaceholderPattern.Matches(text))
            {
                if (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) > 6)
                {
                    violations.Add(key + ": " + match.Value);
                }
            }

            string withoutPlaceholders = PlaceholderPattern.Replace(text, string.Empty);
            if (withoutPlaceholders.Contains('{', StringComparison.Ordinal) || withoutPlaceholders.Contains('}', StringComparison.Ordinal))
            {
                violations.Add(key + ": stray brace");
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-049
    [Fact]
    public void NoTableTextContainsADollarSign()
    {
        var violations = UiContract.Keys().Where(key => UiContract.Get(key).Contains('$', StringComparison.Ordinal)).ToArray();

        Assert.Empty(violations);
    }
}
