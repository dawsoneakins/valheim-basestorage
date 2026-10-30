namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// GW-U-2100 to GW-U-2109 (AC-1; contract 2.2, 2.3, 3.2 FC1-FC7 and FC9, 3.3): the feedback catalog's table test. Every expectation
/// comes from <see cref="FeedbackFixtures"/>, the contract copied verbatim, so the catalog is never its own oracle (ADR-GWIN-153).
/// </summary>
public sealed class FeedbackCatalogTests
{
    private const int RowCount = 43;

    /// <summary>Row indices of <see cref="FeedbackFixtures.Rows"/>, one theory case each.</summary>
    public static TheoryData<int> RowIndices() => Indices(Enumerable.Range(0, FeedbackFixtures.Rows.Length));

    /// <summary>Row indices of the 33 Mod rows.</summary>
    public static TheoryData<int> ModRowIndices() =>
        Indices(Enumerable.Range(0, FeedbackFixtures.Rows.Length).Where(index => FeedbackFixtures.Rows[index].Owner == "Mod"));

    // GW-U-2100 (FC1): All holds one row per defined outcome, 43 in all, in ascending numeric order, each value once.
    [Fact]
    public void AllHoldsOneRowPerDefinedOutcomeInAscendingOrder()
    {
        int[] values = FeedbackCatalog.All.Select(line => (int)line.Outcome).ToArray();

        Assert.Equal(RowCount, FeedbackCatalog.All.Count);
        Assert.Equal(RowCount, Enum.GetValues(typeof(FeedbackOutcome)).Length);
        Assert.Equal(FeedbackFixtures.Rows.Select(row => row.Value).ToArray(), values);
        Assert.Equal(values.OrderBy(value => value).ToArray(), values);
        Assert.Equal(RowCount, values.Distinct().Count());
    }

    // GW-U-2100 (FC1): For(o) is the same instance as the All element with Outcome == o.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void ForReturnsTheSameInstanceAsTheAllElement(int index)
    {
        FeedbackOutcome outcome = OutcomeOf(index);

        Assert.Equal(outcome, FeedbackCatalog.All[index].Outcome);
        Assert.Same(FeedbackCatalog.All[index], FeedbackCatalog.For(outcome));
    }

    // GW-U-2101 (FC2): Owner == Mod exactly when Key and Placement are set, and every field equals the contract row.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void EachRowHasOneKeyAndTheContractedFields(int index)
    {
        FeedbackFixtures.Row expected = FeedbackFixtures.Rows[index];
        FeedbackLine line = FeedbackCatalog.For(OutcomeOf(index));
        bool mod = line.Owner == FeedbackLineOwner.Mod;

        Assert.Equal(expected.Action, line.Action.ToString());
        Assert.Equal(expected.Owner, line.Owner.ToString());
        Assert.Equal(expected.Key, line.Key);
        Assert.Equal(expected.Placement, line.Placement);
        Assert.Equal(mod, line.Key is not null);
        Assert.Equal(mod, line.Placement is not null);
        if (mod)
        {
            Assert.True(StringTable.English.Contains(line.Key!), $"{line.Key} is not an English key of the strings table.");
            Assert.False(
                StringTable.English.Contains(line.Key + ".one") && StringTable.English.Contains(line.Key + ".other"),
                $"{line.Key} is a plural base, not a key.");
        }
    }

    // GW-U-2101 and GW-U-2102 (3.3): the example facts of the one-key and fragment rules.
    [Fact]
    public void TheSection33ExamplesHold()
    {
        FeedbackLine created = FeedbackCatalog.For(FeedbackOutcome.CraftCreated);
        Assert.Equal("message.craft.used-base", created.Key);
        Assert.Equal(OrganizeNoticePlacement.TopLeft, created.Placement);
        Assert.Equal(FeedbackLineOwner.Mod, created.Owner);
        Assert.Equal("Used materials from base chests", StringTable.English.Format(created.Key!));

        FeedbackLine returned = FeedbackCatalog.For(FeedbackOutcome.BuildReturned);
        Assert.Equal(FeedbackLineOwner.Vanilla, returned.Owner);
        Assert.Null(returned.Key);
        Assert.Null(returned.Placement);
        Assert.Empty(returned.FragmentKeys);

        Assert.Equal(FeedbackAction.Organize, FeedbackCatalog.For(FeedbackOutcome.OrganizeLockRefused).Action);
        Assert.Equal(new[] { "category.refusal.in-use", "player.unknown" }, FeedbackCatalog.For(FeedbackOutcome.CategoryInUse).FragmentKeys.ToArray());
        Assert.Equal(22, FeedbackCatalog.For(FeedbackOutcome.OrganizeStopped).FragmentKeys.Count);
        Assert.Equal(9, FeedbackCatalog.For(FeedbackOutcome.DepositStopped).FragmentKeys.Count);
    }

    // GW-U-2102 (FC3): each fragment list equals the contract list in order; each fragment is a key or a plural base; non-Mod rows have none.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void FragmentListsMatchTheContractAndEachFragmentIsAKeyOrPluralBase(int index)
    {
        FeedbackFixtures.Row expected = FeedbackFixtures.Rows[index];
        FeedbackLine line = FeedbackCatalog.For(OutcomeOf(index));
        string[] actual = line.FragmentKeys.ToArray();

        Assert.Equal(expected.Fragments, actual);
        if (line.Owner != FeedbackLineOwner.Mod)
        {
            Assert.Empty(actual);
        }

        foreach (string fragment in actual)
        {
            bool key = StringTable.English.Contains(fragment);
            bool plural = StringTable.English.Contains(fragment + ".one") && StringTable.English.Contains(fragment + ".other");
            Assert.True(key || plural, $"{fragment} is neither a key nor a plural base.");
        }
    }

    // GW-U-2103 (FC4): Mod rows that share a key share a placement, and PlacementOf returns it.
    [Fact]
    public void ModRowsSharingAKeyShareAPlacementAndPlacementOfReturnsIt()
    {
        foreach (IGrouping<string?, FeedbackLine> group in FeedbackCatalog.All.Where(line => line.Owner == FeedbackLineOwner.Mod).GroupBy(line => line.Key))
        {
            OrganizeNoticePlacement placement = FeedbackCatalog.PlacementOf(group.Key!);
            Assert.All(group, line => Assert.Equal(placement, line.Placement));
        }
    }

    // GW-U-2103 (FC4, 3.3): a key used by no Mod row is Centre, and the shared keys have their contracted placement.
    [Theory]
    [InlineData("message.base.in-progress", OrganizeNoticePlacement.Centre)]
    [InlineData("message.craft.used-base", OrganizeNoticePlacement.TopLeft)]
    [InlineData("message.category.set", OrganizeNoticePlacement.TopLeft)]
    [InlineData("network.version.mismatch", OrganizeNoticePlacement.Centre)]
    [InlineData("", OrganizeNoticePlacement.Centre)]
    [InlineData("no.such.key", OrganizeNoticePlacement.Centre)]
    public void PlacementOfReturnsTheSharedPlacementOrCentre(string key, OrganizeNoticePlacement expected)
    {
        Assert.Equal(expected, FeedbackCatalog.PlacementOf(key));
    }

    // GW-U-2103 (FC4): a null key throws ArgumentNullException with ParamName "key".
    [Fact]
    public void PlacementOfNullThrowsByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FeedbackCatalog.PlacementOf(null!));

        Assert.Equal("key", ex.ParamName);
    }

    // GW-U-2104 (FC5): every Mod key is at most 64 characters, and its example line renders exactly, as one line with no break and no "<".
    [Theory]
    [MemberData(nameof(ModRowIndices))]
    public void ModKeysAreShortAndTheExampleLinesRenderExactlyOnOneLine(int index)
    {
        FeedbackFixtures.Row expected = FeedbackFixtures.Rows[index];
        string key = expected.Key!;

        Assert.True(StringTable.English.Get(key).Length <= 64, $"{key} is longer than 64 characters.");
        string line = SingleLine.Of(StringTable.English.Format(key, expected.Args()));
        Assert.Equal(expected.Exact, line);
        Assert.False(expected.Exact!.Any(c => c is '\r' or '\n' or '\u0085' || c == (char)0x2028 || c == (char)0x2029 || c == '<'), $"{expected.Exact} is not a plain single line.");
    }

    // GW-U-2105 (FC6): All is a read-only view; a cast to IList<T> reports IsReadOnly and rejects Add.
    [Fact]
    public void AllIsAReadOnlyViewThatRejectsAdd()
    {
        var list = Assert.IsAssignableFrom<IList<FeedbackLine>>(FeedbackCatalog.All);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(FeedbackCatalog.All[0]));
    }

    // GW-U-2105 (FC6): every FragmentKeys list is a read-only view that rejects Add.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void EveryFragmentListIsAReadOnlyViewThatRejectsAdd(int index)
    {
        var list = Assert.IsAssignableFrom<IList<string>>(FeedbackCatalog.For(OutcomeOf(index)).FragmentKeys);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add("no.such.key"));
    }

    // GW-U-2106 (FC7): the non-Mod rows are exactly the ten contracted ones with their owners; 33 Mod rows, 21 Centre and 12 TopLeft.
    [Fact]
    public void TheNonModRowsAreTheContractedTenWithTheirOwners()
    {
        (string Outcome, string Owner)[] actual = FeedbackCatalog.All
            .Where(line => line.Owner != FeedbackLineOwner.Mod)
            .Select(line => (line.Outcome.ToString(), line.Owner.ToString()))
            .ToArray();

        Assert.Equal(
            new[]
            {
                ("CraftAbandoned", "Superseded"),
                ("BuildReturned", "Vanilla"),
                ("BuildAbandoned", "Superseded"),
                ("BuildPlacementRefused", "Vanilla"),
                ("CategoryAlreadyPending", "Coalesced"),
                ("CategoryChestGone", "TargetGone"),
                ("OrganizeNoPlayer", "NoHud"),
                ("OrganizeWorldGone", "NoHud"),
                ("DepositNoPlayer", "NoHud"),
                ("DepositWorldGone", "NoHud"),
            },
            actual);
        Assert.Equal(33, FeedbackCatalog.All.Count(line => line.Owner == FeedbackLineOwner.Mod));
        Assert.Equal(21, FeedbackCatalog.All.Count(line => line.Owner == FeedbackLineOwner.Mod && line.Placement == OrganizeNoticePlacement.Centre));
        Assert.Equal(12, FeedbackCatalog.All.Count(line => line.Owner == FeedbackLineOwner.Mod && line.Placement == OrganizeNoticePlacement.TopLeft));
    }

    // GW-U-2107 (FC9): an undefined outcome throws ArgumentOutOfRangeException with ParamName "outcome".
    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(6)]
    public void AnUndefinedOutcomeThrowsByName(int value)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => FeedbackCatalog.For((FeedbackOutcome)value));

        Assert.Equal("outcome", ex.ParamName);
    }

    // GW-U-2108 (3.3): the organize completed summary renders as the contract says.
    [Fact]
    public void OrganizeCompletedRendersTheContractedSummary()
    {
        string summary = OrganizeSummaryFormatter.Summary(new OrganizeRunReport(37, Array.Empty<OrganizeSkipEntry>(), 4, false, null));

        Assert.Equal("Organized 37 stacks; 4 stacks had no room", StringTable.English.Format(FeedbackCatalog.For(FeedbackOutcome.OrganizeCompleted).Key!, summary));
    }

    // GW-U-2108 (3.3): the organize stopped summary renders with its stop reason.
    [Fact]
    public void OrganizeStoppedRendersTheStopReason()
    {
        string summary = OrganizeSummaryFormatter.Summary(new OrganizeRunReport(12, Array.Empty<OrganizeSkipEntry>(), 0, true, "chest full"));

        Assert.Equal("Organized 12 stacks; stopped early: chest full", StringTable.English.Format(FeedbackCatalog.For(FeedbackOutcome.OrganizeStopped).Key!, summary));
    }

    // GW-U-2108 (3.3): the deposit stopped line renders its stop reason through the stop-reason key.
    [Fact]
    public void DepositStoppedRendersTheStopReason()
    {
        string reason = StringTable.English.Get(OrganizeRunText.StopReasonKey(ExecutionStopReason.DestinationFull));

        Assert.Equal(
            "Put away: 0 stacks moved, 0 no room; stopped early: chest full",
            StringTable.English.Format(FeedbackCatalog.For(FeedbackOutcome.DepositStopped).Key!, 0, 0, reason));
    }

    // GW-U-2108 (3.3): the craft success line renders with no argument.
    [Fact]
    public void CraftCreatedRendersTheSuccessLine()
    {
        Assert.Equal("Used materials from base chests", StringTable.English.Format(FeedbackCatalog.For(FeedbackOutcome.CraftCreated).Key!));
    }

    // GW-U-2109 (2.3): every member of a mirrored code enum has a catalog row, by name.
    [Theory]
    [InlineData("CraftEnd", "Craft")]
    [InlineData("BuildEnd", "Build")]
    [InlineData("CategoryWriteOutcome", "Category")]
    [InlineData("FavouriteToggleOutcome", "Favourite")]
    public void EveryMemberOfAMirroredCodeEnumHasARowByName(string enumName, string prefix)
    {
        foreach (string member in Enum.GetNames(MirroredEnum(enumName)))
        {
            Assert.True(Enum.TryParse<FeedbackOutcome>(prefix + member, out _), $"{prefix}{member} has no catalog row.");
        }
    }

    // GW-U-2109 (2.3): CategoryWriteStatus.Unchanged has the CategoryUnchanged row.
    [Fact]
    public void CategoryWriteStatusUnchangedHasARow()
    {
        Assert.True(Enum.TryParse<FeedbackOutcome>("Category" + nameof(CategoryWriteStatus.Unchanged), out _));
    }

    // GW-U-2109 (2.3): every OrganizeStartOutcome except Started has a row under both the Organize and the Deposit prefix.
    [Fact]
    public void EveryStartRefusalHasAnOrganizeAndADepositRow()
    {
        foreach (string member in Enum.GetNames(typeof(OrganizeStartOutcome)).Where(name => name != nameof(OrganizeStartOutcome.Started)))
        {
            Assert.True(Enum.TryParse<FeedbackOutcome>("Organize" + member, out _), $"Organize{member} has no catalog row.");
            Assert.True(Enum.TryParse<FeedbackOutcome>("Deposit" + member, out _), $"Deposit{member} has no catalog row.");
        }
    }

    // GW-U-2109 (2.3): every stop reason, None included, maps to a key in the fragments of rows 31 and 51.
    [Fact]
    public void EveryStopReasonKeyIsAFragmentOfTheStoppedRows()
    {
        foreach (ExecutionStopReason reason in Enum.GetValues<ExecutionStopReason>())
        {
            string key = OrganizeRunText.StopReasonKey(reason);
            if (reason == ExecutionStopReason.None)
            {
                Assert.Equal("organize.stop-reason.aborted", key);
            }

            Assert.Contains(key, FeedbackCatalog.For(FeedbackOutcome.OrganizeStopped).FragmentKeys);
            Assert.Contains(key, FeedbackCatalog.For(FeedbackOutcome.DepositStopped).FragmentKeys);
        }
    }

    private static FeedbackOutcome OutcomeOf(int index) => Enum.Parse<FeedbackOutcome>(FeedbackFixtures.Rows[index].Outcome);

    private static TheoryData<int> Indices(IEnumerable<int> indices)
    {
        var data = new TheoryData<int>();
        foreach (int index in indices)
        {
            data.Add(index);
        }

        return data;
    }

    private static Type MirroredEnum(string name) => name switch
    {
        "CraftEnd" => typeof(CraftEnd),
        "BuildEnd" => typeof(BuildEnd),
        "CategoryWriteOutcome" => typeof(CategoryWriteOutcome),
        "FavouriteToggleOutcome" => typeof(FavouriteToggleOutcome),
        _ => throw new ArgumentException("Not a mirrored enum: " + name, nameof(name)),
    };
}
