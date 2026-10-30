namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Organize.Execution;
using BaseStorage.Core.Tests.Organize.Running;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// GW-U-2110 to GW-U-2115 (AC-1; contract 4.3 FC8): the catalog agrees with the code that produces each line. The craft, build,
/// category and favourite mappers are checked over their code enums, and the Organize rows 30-39 and Deposit rows 50-59 are driven
/// through <see cref="RunHarness"/> one scenario each, recording the <c>ShowNotice</c> calls of the port (ADR-GWIN-153).
/// </summary>
public sealed class FeedbackCatalogAgreementTests
{
    private static readonly BaseStart AtStation = BaseStart.FromStation(RunHarness.Station.Id);

    /// <summary>The names of every member of CraftEnd, one theory case each (GW-U-2110).</summary>
    public static TheoryData<string> CraftEndNames() => NamesOf(typeof(CraftEnd));

    /// <summary>The names of every member of BuildEnd, one theory case each (GW-U-2111).</summary>
    public static TheoryData<string> BuildEndNames() => NamesOf(typeof(BuildEnd));

    /// <summary>The names of every member of CategoryWriteOutcome, one theory case each (GW-U-2112).</summary>
    public static TheoryData<string> CategoryOutcomeNames() => NamesOf(typeof(CategoryWriteOutcome));

    /// <summary>The names of every member of FavouriteToggleOutcome, one theory case each (GW-U-2113).</summary>
    public static TheoryData<string> FavouriteToggleNames() => NamesOf(typeof(FavouriteToggleOutcome));

    // GW-U-2110 (FC8 craft): CraftSession.NoticeKeyFor agrees with the Craft row of the same end.
    [Theory]
    [MemberData(nameof(CraftEndNames))]
    public void CraftNoticeKeyAgreesWithTheCatalog(string name)
    {
        var end = (CraftEnd)Enum.Parse(typeof(CraftEnd), name);

        Assert.Equal(FeedbackCatalog.For(Enum.Parse<FeedbackOutcome>("Craft" + name)).Key, CraftSession.NoticeKeyFor(end));
    }

    // GW-U-2111 (FC8 build): BuildSession.NoticeKeyFor agrees with the Build row of the same end.
    [Theory]
    [MemberData(nameof(BuildEndNames))]
    public void BuildNoticeKeyAgreesWithTheCatalog(string name)
    {
        var end = (BuildEnd)Enum.Parse(typeof(BuildEnd), name);

        Assert.Equal(FeedbackCatalog.For(Enum.Parse<FeedbackOutcome>("Build" + name)).Key, BuildSession.NoticeKeyFor(end));
    }

    // GW-U-2112 (FC8 category): the category notice and reason keys agree with the Category rows.
    [Theory]
    [MemberData(nameof(CategoryOutcomeNames))]
    public void CategoryNoticeAndReasonKeysAgreeWithTheCatalog(string name)
    {
        var outcome = (CategoryWriteOutcome)Enum.Parse(typeof(CategoryWriteOutcome), name);
        FeedbackLine row = FeedbackCatalog.For(Enum.Parse<FeedbackOutcome>("Category" + name));

        Assert.Equal(row.Key, CategoryWriteRules.NoticeKey(outcome));
        if (outcome != CategoryWriteOutcome.Applied)
        {
            Assert.Equal(row.FragmentKeys[0], CategoryWriteRules.ReasonKey(outcome));
        }
    }

    // GW-U-2113 (FC8 favourite): ProfileFavourites.MessageKeyFor agrees with the Favourite rows.
    [Theory]
    [MemberData(nameof(FavouriteToggleNames))]
    public void FavouriteMessageKeyAgreesWithTheCatalog(string name)
    {
        var toggle = (FavouriteToggleOutcome)Enum.Parse(typeof(FavouriteToggleOutcome), name);

        Assert.Equal(FeedbackCatalog.For(Enum.Parse<FeedbackOutcome>("Favourite" + name)).Key, ProfileFavourites.MessageKeyFor(toggle));
    }

    // GW-U-2114 (FC8 Organize, row 30): a completed Organize records one TopLeft summary line.
    [Fact]
    public void OrganizeRow30CompletedRecordsTheSummaryOnce()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.True(h.Coordinator.Confirm());
        h.RunToIdle(2);

        AssertRecorded(FeedbackOutcome.OrganizeCompleted, h.Port.Notices);
        AssertRendersAsOneLine(h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 31): a run stopped by a full destination records one TopLeft stopped summary line.
    [Fact]
    public void OrganizeRow31StoppedRecordsTheStoppedSummaryOnce()
    {
        RunHarness h = RunHarness.Make();
        h.Port.Arrange = fake => fake.InjectFault(OrganizeFixtures.Id(202), FakeFault.DestinationFull);
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.True(h.Coordinator.Confirm());
        h.RunToIdle(2);

        AssertRecorded(FeedbackOutcome.OrganizeStopped, h.Port.Notices);
        AssertRendersAsOneLine(h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 32): cancelling the preview records the cancelled line.
    [Fact]
    public void OrganizeRow32CancelledRecordsTheCancelledLine()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.True(h.Coordinator.Cancel());

        AssertRecorded(FeedbackOutcome.OrganizeCancelled, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 33): a start outside every base records the not-in-base line.
    [Fact]
    public void OrganizeRow33NotInBaseRecordsTheRefusal()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.NotInBase, h.Coordinator.Start(RunHarness.Outside));

        AssertRecorded(FeedbackOutcome.OrganizeNotInBase, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 34): a base with no usable chest records the no-chests line.
    [Fact]
    public void OrganizeRow34NoChestsRecordsTheRefusal()
    {
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(new ContainerSnapshot[0]));
        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.Start(RunHarness.Inside));

        AssertRecorded(FeedbackOutcome.OrganizeNoChests, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 35): a second Start while a run is active records the in-progress line with the holder's name.
    [Fact]
    public void OrganizeRow35AlreadyActiveRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make();
        h.Coordinator.Start(RunHarness.Inside);
        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.Start(RunHarness.Inside));

        AssertRecorded(FeedbackOutcome.OrganizeAlreadyActive, h.Port.Notices);
        AssertRendersAsOneLine(h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 36): a foreign live lock at the start records the in-progress line with the other holder's name.
    [Fact]
    public void OrganizeRow36LockRefusedRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make();
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.Start(RunHarness.Inside));

        AssertRecorded(FeedbackOutcome.OrganizeLockRefused, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 37): a lock taken while the preview is open refuses Confirm with the in-progress line.
    [Fact]
    public void OrganizeRow37ConfirmRefusedRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        h.Coordinator.Confirm();

        AssertRecorded(FeedbackOutcome.OrganizeConfirmRefused, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 38): no player records nothing.
    [Fact]
    public void OrganizeRow38NoPlayerRecordsNothing()
    {
        RunHarness h = RunHarness.Make();
        h.Port.HolderId = 0;
        Assert.Equal(OrganizeStartOutcome.NoPlayer, h.Coordinator.Start(RunHarness.Inside));

        AssertRecorded(FeedbackOutcome.OrganizeNoPlayer, h.Port.Notices);
    }

    // GW-U-2114 (FC8 Organize, row 39): the world gone during a run records nothing.
    [Fact]
    public void OrganizeRow39WorldGoneRecordsNothing()
    {
        RunHarness h = RunHarness.Make(movesPerFrame: 1, skipPreview: true);
        h.Coordinator.Start(RunHarness.Inside);
        h.Coordinator.Tick();
        h.Port.InWorld = false;
        h.Coordinator.Tick();

        AssertRecorded(FeedbackOutcome.OrganizeWorldGone, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 50): a completed Put away records one TopLeft summary line.
    [Fact]
    public void DepositRow50CompletedRecordsTheSummaryOnce()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));
        h.RunToIdle(2);

        AssertRecorded(FeedbackOutcome.DepositCompleted, h.Port.Notices);
        AssertRendersAsOneLine(h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 51): a stopped Put away records one TopLeft stopped summary line.
    [Fact]
    public void DepositRow51StoppedRecordsTheStoppedSummaryOnce()
    {
        RunHarness h = RunHarness.Make();
        h.Port.Arrange = fake => fake.InjectFault(OrganizeFixtures.Id(201), FakeFault.DestinationFull);
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));
        h.RunToIdle(2);

        AssertRecorded(FeedbackOutcome.DepositStopped, h.Port.Notices);
        AssertRendersAsOneLine(h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 52): cancelling the Put away preview records the cancelled line.
    [Fact]
    public void DepositRow52CancelledRecordsTheCancelledLine()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));
        Assert.True(h.Coordinator.Cancel());

        AssertRecorded(FeedbackOutcome.DepositCancelled, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 53): a Put away outside every base records the not-in-base line.
    [Fact]
    public void DepositRow53NotInBaseRecordsTheRefusal()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.NotInBase, h.Coordinator.StartDeposit(RunHarness.Outside));

        AssertRecorded(FeedbackOutcome.DepositNotInBase, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 54): a base whose chests are all Keep records the no-chests line.
    [Fact]
    public void DepositRow54NoChestsRecordsTheRefusal()
    {
        Dictionary<ContainerId, CategoryAssignment> keep = OrganizeFixtures.EChests()
            .ToDictionary(chest => chest.Id, chest => CategoryAssignment.Keep);
        RunHarness h = RunHarness.Make(OrganizeFixtures.Make(OrganizeFixtures.EChests(), keep));
        Assert.Equal(OrganizeStartOutcome.NoChests, h.Coordinator.StartDeposit(AtStation));

        AssertRecorded(FeedbackOutcome.DepositNoChests, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 55): a second Put away while one is previewing records the in-progress line with the holder's name.
    [Fact]
    public void DepositRow55AlreadyActiveRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);
        Assert.Equal(OrganizeStartOutcome.AlreadyActive, h.Coordinator.StartDeposit(AtStation));

        AssertRecorded(FeedbackOutcome.DepositAlreadyActive, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 56): a foreign live lock at the start records the in-progress line with the other holder's name.
    [Fact]
    public void DepositRow56LockRefusedRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make();
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        Assert.Equal(OrganizeStartOutcome.LockRefused, h.Coordinator.StartDeposit(AtStation));

        AssertRecorded(FeedbackOutcome.DepositLockRefused, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 57): a lock taken while the Put away preview is open refuses Confirm with the in-progress line.
    [Fact]
    public void DepositRow57ConfirmRefusedRecordsTheInProgressLine()
    {
        RunHarness h = RunHarness.Make(depositPreview: true);
        h.Coordinator.StartDeposit(AtStation);
        h.Store.Seed(RunHarness.Station.Id, LockFixtures.Astrid3630);
        h.Coordinator.Confirm();

        AssertRecorded(FeedbackOutcome.DepositConfirmRefused, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 58): no player records nothing.
    [Fact]
    public void DepositRow58NoPlayerRecordsNothing()
    {
        RunHarness h = RunHarness.Make();
        h.Port.HolderId = 0;
        Assert.Equal(OrganizeStartOutcome.NoPlayer, h.Coordinator.StartDeposit(AtStation));

        AssertRecorded(FeedbackOutcome.DepositNoPlayer, h.Port.Notices);
    }

    // GW-U-2115 (FC8 Deposit, row 59): the world gone during a Put away records nothing.
    [Fact]
    public void DepositRow59WorldGoneRecordsNothing()
    {
        RunHarness h = RunHarness.Make();
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.StartDeposit(AtStation));
        h.Port.InWorld = false;
        h.Coordinator.Tick();

        AssertRecorded(FeedbackOutcome.DepositWorldGone, h.Port.Notices);
    }

    /// <summary>The recorded notices of a row are exactly its one line (placement and key), or nothing for an exempt row.</summary>
    private static void AssertRecorded(FeedbackOutcome outcome, IEnumerable<(OrganizeNoticePlacement Placement, string Key, object?[] Args)> notices)
    {
        FeedbackLine row = FeedbackCatalog.For(outcome);
        (OrganizeNoticePlacement Placement, string Key)[] recorded = notices.Select(notice => (notice.Placement, notice.Key)).ToArray();
        if (row.Owner == FeedbackLineOwner.Mod)
        {
            Assert.Equal(new[] { (row.Placement!.Value, row.Key!) }, recorded);
        }
        else
        {
            Assert.Empty(recorded);
        }
    }

    /// <summary>FG4 for composed lines: each recorded line renders through the table as one plain line with no "&lt;".</summary>
    private static void AssertRendersAsOneLine(IEnumerable<(OrganizeNoticePlacement Placement, string Key, object?[] Args)> notices)
    {
        foreach ((OrganizeNoticePlacement _, string key, object?[] args) in notices)
        {
            string line = SingleLine.Of(StringTable.English.Format(key, args));
            Assert.DoesNotContain("<", line);
            Assert.False(line.Any(c => c is '\r' or '\n' or '\u0085' || c == (char)0x2028 || c == (char)0x2029), $"{line} is not one line.");
        }
    }

    private static TheoryData<string> NamesOf(Type type)
    {
        var data = new TheoryData<string>();
        foreach (string name in Enum.GetNames(type))
        {
            data.Add(name);
        }

        return data;
    }
}
