namespace BaseStorage.Core.Tests.Ui;

using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Ui;

/// <summary>GW-U-2613: the twelve Restock strings and the eight StringKeys constants, appended in contract order (S-QR-002 contract 6.2).</summary>
public sealed class RestockStringsTests
{
    private static readonly (string Key, string Text)[] RestockPairs =
    {
        ("menu.restock", "Restock"),
        ("message.restock.summary", "{0}"),
        ("message.restock.moved.one.one", "Restocked 1 stack, 1 item"),
        ("message.restock.moved.one.other", "Restocked 1 stack, {1} items"),
        ("message.restock.moved.other.one", "Restocked {0} stacks, 1 item"),
        ("message.restock.moved.other.other", "Restocked {0} stacks, {1} items"),
        ("message.restock.nothing", "Nothing to restock"),
        ("message.restock.nothing-moved", "Nothing restocked"),
        ("message.restock.left-behind.one", "1 item left behind"),
        ("message.restock.left-behind.other", "{0} items left behind"),
        ("message.restock.stopped", "stopped early"),
        ("message.restock.capture-failed", "Restock failed: could not read your inventory"),
    };

    /// <summary>GW-U-2613: each of the twelve keys holds its text verbatim, the two reused keys are unchanged, no text has a tag, and every placeholder is 0 to 6.</summary>
    [Fact]
    public void RestockStringsAreAppendedVerbatim()
    {
        foreach ((string key, string text) in RestockPairs)
        {
            Assert.Equal(text, StringTable.English.Get(key));
            Assert.DoesNotContain("<", text);
            foreach (Match placeholder in Regex.Matches(text, @"\{(\d+)\}"))
            {
                Assert.InRange(int.Parse(placeholder.Groups[1].Value), 0, 6);
            }
        }

        Assert.Equal("Organize in progress by {0}", StringTable.English.Get("message.base.in-progress"));
        Assert.Equal("Not in a base", StringTable.English.Get("message.base.not-in-base"));
    }

    /// <summary>GW-U-2613: the eight StringKeys constants are the last eight, in contract order.</summary>
    [Fact]
    public void RestockStringKeysAreAppendedLast()
    {
        FieldInfo[] constants = typeof(StringKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral)
            .ToArray();
        string[] last = constants.Skip(constants.Length - 8).Select(f => f.Name).ToArray();
        Assert.Equal(
            new[]
            {
                "MenuRestock", "MessageRestockSummary", "MessageRestockMoved", "MessageRestockNothing",
                "MessageRestockNothingMoved", "MessageRestockLeftBehind", "MessageRestockStopped", "MessageRestockCaptureFailed",
            },
            last);
    }
}
