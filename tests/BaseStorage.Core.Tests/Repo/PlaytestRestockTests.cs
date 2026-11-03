namespace BaseStorage.Core.Tests.Repo;

using System.Linq;
using System.Text.RegularExpressions;

/// <summary>GW-U-2620: the v0.4.3 playtest file has a numbered Restock section that covers every required step (S-QR-002 P9).</summary>
public sealed class PlaytestRestockTests
{
    private const string Playtest = "docs/PLAYTEST-v0.4.3.md";

    /// <summary>GW-U-2620: the file exists, opens with its title, its Restock section has consecutive numbered steps, and the steps name the button, the key, the narrow layout, the second player, Not in a base, the three lines and U1 to U4.</summary>
    [Fact]
    public void ThePlaytestV043FileCoversEveryRestockStep()
    {
        Assert.True(RepoRoot.Exists(Playtest), "docs/PLAYTEST-v0.4.3.md is missing.");
        string text = RepoRoot.ReadText(Playtest);
        Assert.StartsWith("# BaseStorage v0.4.3 playtest", text, System.StringComparison.Ordinal);

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        int start = System.Array.FindIndex(lines, line => line == "## Restock");
        Assert.True(start >= 0, "The ## Restock section is missing.");
        int end = System.Array.FindIndex(lines, start + 1, line => line.StartsWith("## ", System.StringComparison.Ordinal));
        string[] section = lines.Skip(start + 1).Take((end < 0 ? lines.Length : end) - start - 1).ToArray();
        int[] numbers = section.Select(line => Regex.Match(line, @"^(\d+)\. ")).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        Assert.True(numbers.Length > 0 && numbers.Select((n, i) => n == i + 1).All(ok => ok), "The Restock steps are not numbered 1, 2, 3 with no gaps.");

        string body = string.Join("\n", section);
        foreach (string topic in new[] { "Put away inventory", "RestockKey", "380", "Not in a base", "Restocked", "Nothing to restock", "items left behind", "U1", "U2", "U3", "U4" })
        {
            Assert.Contains(topic, body, System.StringComparison.Ordinal);
        }
    }
}
