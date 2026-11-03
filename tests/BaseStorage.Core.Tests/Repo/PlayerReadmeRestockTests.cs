namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;

/// <summary>GW-U-2560: the player README lists RestockKey directly after DepositKey (S-QR-002 contract 7).</summary>
public sealed class PlayerReadmeRestockTests
{
    private const string Readme = "package/README.md";
    private const string RestockRow = "| `RestockKey` | `[Keys]` | unbound | no | Hotkey that starts Restock for the base that contains the player. |";

    /// <summary>GW-U-2560: the exact RestockKey row is on the line right after the DepositKey row.</summary>
    [Fact]
    public void TheReadmeListsRestockKeyAfterDepositKey()
    {
        string[] lines = RepoRoot.ReadText(Readme).Replace("\r\n", "\n").Split('\n');
        int deposit = Array.FindIndex(lines, line => line.StartsWith("| `DepositKey`", StringComparison.Ordinal));
        Assert.True(deposit >= 0, "The DepositKey row is missing.");
        Assert.Equal(RestockRow, lines[deposit + 1]);
        Assert.Single(lines, line => line.StartsWith("| `RestockKey`", StringComparison.Ordinal));
    }
}
