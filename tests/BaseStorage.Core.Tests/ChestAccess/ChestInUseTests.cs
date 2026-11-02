namespace BaseStorage.Core.Tests.ChestAccess;

using System.Reflection;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-778 and GW-U-782 (AC-4): InUseBy, HolderLabel and ChestInUse (contract 2.5 second table verbatim) and the fallback label (contract 2.6).</summary>
public sealed class ChestInUseTests
{
    private static readonly StringTable English = StringTable.English;

    // GW-U-778: InUseBy rows with a usable, empty, whitespace, null, control-character and padded name.
    [Theory]
    [InlineData("Erik", "Erik")]
    [InlineData("", "another player")]
    [InlineData("   ", "another player")]
    [InlineData(null, "another player")]
    [InlineData("Er\nik", "another player")]
    [InlineData(" Ragnar the Red ", " Ragnar the Red ")]
    public void InUseByReturnsTheChestWithTheContractedLabel(string? ownerName, string expected)
    {
        ChestInUse? result = ChestAccessRules.InUseBy(Chest42, Obs(inUseFlag: true, ownerName: ownerName), English);

        Assert.NotNull(result);
        Assert.Equal(Chest42, result!.Chest);
        Assert.Equal(expected, result.HolderName);
        Assert.Equal(new ChestInUse(Chest42, expected), result);
    }

    // GW-U-778: a chest that is not in use gives null.
    [Fact]
    public void InUseByReturnsNullWhenTheChestIsNotInUseByAnother()
    {
        Assert.Null(ChestAccessRules.InUseBy(Chest42, Obs(), English));
        Assert.Null(ChestAccessRules.InUseBy(Chest42, Obs(inUseFlag: true, ownerSessionId: Me), English));
        Assert.Null(ChestAccessRules.InUseBy(Chest42, Obs(inUseFlag: true, ownerListed: false, ownerName: null), English));
    }

    // GW-U-778: argument rules of InUseBy and HolderLabel, by ParamName.
    [Fact]
    public void InUseByAndHolderLabelRejectBadArgumentsByName()
    {
        var chest = Assert.Throws<ArgumentException>(() => ChestAccessRules.InUseBy(default(ContainerId), Obs(inUseFlag: true), English));
        Assert.Equal("chest", chest.ParamName);

        var observation = Assert.Throws<ArgumentNullException>(() => ChestAccessRules.InUseBy(Chest42, null!, English));
        Assert.Equal("observation", observation.ParamName);

        var strings = Assert.Throws<ArgumentNullException>(() => ChestAccessRules.InUseBy(Chest42, Obs(), null!));
        Assert.Equal("strings", strings.ParamName);

        var label = Assert.Throws<ArgumentNullException>(() => ChestAccessRules.HolderLabel("Astrid", null!));
        Assert.Equal("strings", label.ParamName);
    }

    // GW-U-778: HolderLabel falls back on a tab, a next-line character and a NUL, and keeps a usable name unchanged.
    [Theory]
    [InlineData("Erik\tson", "another player")]
    [InlineData("Erik\u0085", "another player")]
    [InlineData("\u0000", "another player")]
    [InlineData("Astrid", "Astrid")]
    [InlineData("Ragnar the Red", "Ragnar the Red")]
    [InlineData("Bjørn Ægir", "Bjørn Ægir")]
    public void HolderLabelFallsBackOnlyForUnusableNames(string rawName, string expected)
    {
        Assert.Equal(expected, ChestAccessRules.HolderLabel(rawName, English));
    }

    // GW-U-778: ChestInUse constructor rules.
    [Fact]
    public void ChestInUseRejectsTheNoneIdAndBadHolderNamesByName()
    {
        var chest = Assert.Throws<ArgumentException>(() => new ChestInUse(default(ContainerId), "Erik"));
        Assert.Equal("chest", chest.ParamName);

        var nullName = Assert.Throws<ArgumentNullException>(() => new ChestInUse(Chest42, null!));
        Assert.Equal("holderName", nullName.ParamName);

        foreach (string bad in new[] { "", "   ", "\t" })
        {
            var empty = Assert.Throws<ArgumentException>(() => new ChestInUse(Chest42, bad));
            Assert.Equal("holderName", empty.ParamName);
        }
    }

    // GW-U-778: equality and hash sweep.
    [Fact]
    public void ChestInUseEqualityIsOrdinalOverChestAndHolderName()
    {
        var a = new ChestInUse(Chest42, "Erik");
        var b = new ChestInUse(Chest42, "Erik");
        var differentName = new ChestInUse(Chest42, "erik");
        var differentChest = new ChestInUse(new ContainerId(Erik, 7u), "Erik");

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(a == differentName);
        Assert.False(a == differentChest);
        Assert.True(a != differentChest);
        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
        ChestInUse? none = null;
        Assert.True(none == null);
        Assert.Equal(Chest42, a.Chest);
        Assert.Equal("Erik", a.HolderName);
    }

    // GW-U-782: the fallback label lives only in the strings table, as table key 64 and the 48th StringKeys constant
    // (S-E004-003 appended the five category refusal keys after it, and S-E006-002 network.version.missing last: 70 keys, 54 constants).
    // Edited by integration: S-E004-004 appends chest.category.button after network.version.missing (71 keys, 55 constants).
    // (S-E004-003 appended the five category refusal keys after it, and S-E004-011 the ten organize run keys: 79 keys, 63 constants).
    // Edited by integration: in merged order the S-E004-011 keys follow chest.category.button (81 keys, 65 constants).
    [Fact]
    public void TheFallbackLabelIsTheLastStringsTableKey()
    {
        Assert.Equal("another player", English.Get(StringKeys.PlayerUnknown));
        Assert.Equal("player.unknown", StringKeys.PlayerUnknown);
        // Edited by S-E004-015 (ADR-GWIN-143): 81 plus the two Deposit keys (deposit-button contract 5), so 83 keys.
        // Edited by S-E008-001 (ADR-GWIN-152, feedback-audit 4.1): message.craft.used-base is appended last, so 84 keys.
        // Edited by S-E010-002 (organize-console-command 5): 84 plus the five console keys, 89.
        // Edited by S-A-003: 89 plus the rules and no-room reason sentences, 93.
        Assert.Equal(131, English.Keys.Count);

        FieldInfo[] constants = typeof(StringKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal("PlayerUnknown", constants[47].Name);
        // Edited by integration (S-E006-002 + S-E004-004 + S-E004-011): the keys are appended in story order.
        // Edited by S-E004-015 (ADR-GWIN-143): the offsets grow by 2, and the two Deposit constants follow the organize run keys.
        // Edited by S-E010-002 (organize-console-command 5): the five console constants follow MessageCraftUsedBase, so the offsets grow by 5.
        Assert.Equal("CategoryRefusalUnavailable", constants[constants.Length - 60].Name);
        Assert.Equal("NetworkVersionMissing", constants[constants.Length - 59].Name);
        Assert.Equal("ChestCategoryButton", constants[constants.Length - 58].Name);
        Assert.Equal("MessageOrganizeRunSummary", constants[constants.Length - 57].Name);
        Assert.Equal("OrganizeStopReasonLockLost", constants[constants.Length - 48].Name);
        Assert.Equal("MessageDepositCancelled", constants[constants.Length - 47].Name);
        Assert.Equal("MessageDepositSummaryStopped", constants[constants.Length - 46].Name);
        // Edited by S-E008-001 (ADR-GWIN-152): offsets from the end grow by 1; MessageCraftUsedBase is the new last constant.
        Assert.Equal("MessageCraftUsedBase", constants[constants.Length - 45].Name);
        Assert.Equal("ConsoleOrganizeStarted", constants[constants.Length - 44].Name);
        Assert.Equal("ConsoleOrganizePreview", constants[constants.Length - 43].Name);
        Assert.Equal("ConsoleOrganizeNoPlayer", constants[constants.Length - 42].Name);
        Assert.Equal("ConsoleBasestorageUsage", constants[constants.Length - 41].Name);
        Assert.Equal("ConsoleBasestorageDescription", constants[constants.Length - 40].Name);
    }

    // GW-U-782: the existing skip clause reads naturally with the fallback label.
    [Fact]
    public void TheSkipClauseFormatsWithTheFallbackLabel()
    {
        string label = ChestAccessRules.HolderLabel(null, English);
        string clause = English.FormatPlural(StringKeys.OrganizePreviewBodySkipped, 1, 0, 0, 0, 0, 0, 1, label);

        Assert.Equal("1 chest skipped (in use by another player).", clause);
    }
}
