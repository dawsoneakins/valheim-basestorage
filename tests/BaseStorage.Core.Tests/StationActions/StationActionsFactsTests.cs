namespace BaseStorage.Core.Tests.StationActions;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Locking;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Organize.Running;

/// <summary>
/// GW-U-1750..1759 (AC-4, contract 9.1): the base facts the station panel shows, the holder name of a live lock, the station count
/// and the eligible chest count. The Plugin functions are reached through <see cref="StationActionsTypes"/>, so the rows are red
/// until StationActionsFacts is implemented.
/// </summary>
public sealed class StationActionsFactsTests
{
    private static readonly StationId S7 = RunHarness.Station.Id;
    private static readonly StationId S5 = new StationId(OrganizeFixtures.UserId, 5u);
    private static readonly TimeSpan Now = TimeSpan.FromSeconds(3600);
    private static readonly EligibilityOptions Options = new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: true);

    // GW-U-1750 (AC-4, 9.1): no holder when nothing is stored; the store is read once and never written or cleared.
    [Fact]
    public void NoHolderWhenNoRecordIsStoredAndTheStoreIsOnlyRead()
    {
        var store = new FakeOrganizeLockStore();

        Assert.Null(HolderName(new[] { RunHarness.Station }, store, Now, 30));

        Assert.Equal(new[] { FakeOrganizeLockStore.ReadCall(S7) }, store.Calls);
        Assert.Equal(0, store.WriteCount);
        Assert.Equal(0, store.ClearCount);
    }

    // GW-U-1751 (AC-4, 9.1): a live record gives its holder name.
    [Fact]
    public void ALiveRecordGivesItsHolderName()
    {
        var store = new FakeOrganizeLockStore();
        store.Seed(S7, Record("Astrid", 2718281828, 3630));

        Assert.Equal("Astrid", HolderName(new[] { RunHarness.Station }, store, Now, 30));
    }

    // GW-U-1752 (AC-4, 9.1): a record that is not live gives null: it expires now, it expires beyond now plus 30 s, or it does not decode.
    [Theory]
    [InlineData("expires now")]
    [InlineData("beyond now plus 30 s")]
    [InlineData("undecodable")]
    public void ARecordThatIsNotLiveGivesNull(string scenario)
    {
        var store = new FakeOrganizeLockStore();
        store.Seed(S7, scenario switch
        {
            "expires now" => Record("Astrid", 2718281828, 3600),
            "beyond now plus 30 s" => Record("Astrid", 2718281828, 3700),
            _ => "garbage",
        });

        Assert.Null(HolderName(new[] { RunHarness.Station }, store, Now, 30));
    }

    // GW-U-1753 (AC-4, 9.1): the stations are visited in ascending id order, so S5 (Astrid) wins over S7 (Erik).
    [Fact]
    public void TheStationsAreVisitedInAscendingIdOrder()
    {
        var store = new FakeOrganizeLockStore();
        store.Seed(S5, Record("Astrid", 2718281828, 3620));
        store.Seed(S7, Record("Erik", 1923487561234, 3625));

        Assert.Equal("Astrid", HolderName(new[] { Snap(S7), Snap(S5) }, store, Now, 30));
    }

    // GW-U-1754 (AC-4, 9.1): a repeated station id is read once.
    [Fact]
    public void ARepeatedStationIsReadOnce()
    {
        var store = new FakeOrganizeLockStore();
        store.Seed(S7, Record("Erik", 1923487561234, 3625));

        Assert.Equal("Erik", HolderName(new[] { Snap(S7), Snap(S5), Snap(S7) }, store, Now, 30));
        Assert.Single(store.Calls, call => call == FakeOrganizeLockStore.ReadCall(S7));
    }

    // GW-U-1755 (AC-4, 9.1): a lock duration of 0 seconds is floored to 1 second: live at 3600.5 s, not live at 3602 s.
    [Theory]
    [InlineData(3600.5, "Astrid")]
    [InlineData(3602.0, null)]
    public void TheLockDurationHasAOneSecondFloor(double expiresAt, string? expected)
    {
        var store = new FakeOrganizeLockStore();
        store.Seed(S7, Record("Astrid", 2718281828, expiresAt));

        Assert.Equal(expected, HolderName(new[] { RunHarness.Station }, store, Now, 0));
    }

    // GW-U-1756 (AC-4, 9.1): empty stations give null; null stations or a null store throw with their parameter names.
    [Fact]
    public void EmptyStationsGiveNullAndNullArgumentsThrowWithTheirNames()
    {
        var store = new FakeOrganizeLockStore();
        Assert.Null(HolderName(Array.Empty<StationSnapshot>(), store, Now, 30));

        ArgumentNullException stations = Assert.Throws<ArgumentNullException>(() => HolderName(null!, store, Now, 30));
        Assert.Equal("stations", stations.ParamName);

        ArgumentNullException nullStore = Assert.Throws<ArgumentNullException>(() => HolderName(new[] { RunHarness.Station }, null!, Now, 30));
        Assert.Equal("store", nullStore.ParamName);
    }

    // GW-U-1757 (AC-4, 9.1): base E under station S7 has one station, and the eligible chest count is the Core eligibility count.
    [Fact]
    public void BaseECountsTheStationAndTheEligibleChests()
    {
        RunHarness h = RunHarness.Make();
        BaseResolution resolution = ResolveS7(h);
        int eligible = ContainerEligibility.Evaluate(resolution.Containers, NoAssignments(), ContainerPurpose.Organize, Options).Eligible.Count;

        Assert.Equal(1, StationCount(resolution));
        Assert.True(eligible > 0, "Base E has eligible chests.");
        Assert.Equal(eligible, EligibleChestCount(resolution, NoAssignments(), Options));
    }

    // GW-U-1758 (AC-4, 9.1): one Keep gives one fewer, every container Keep gives 0, and an InUseByOther chest still counts.
    [Fact]
    public void KeepRemovesChestsAndAnInUseChestStillCounts()
    {
        RunHarness h = RunHarness.Make();
        BaseResolution resolution = ResolveS7(h);
        IReadOnlyList<ContainerSnapshot> eligible = ContainerEligibility.Evaluate(resolution.Containers, NoAssignments(), ContainerPurpose.Organize, Options).Eligible;
        int all = eligible.Count;

        var oneKeep = new Dictionary<ContainerId, CategoryAssignment> { [eligible[0].Id] = CategoryAssignment.Keep };
        Assert.Equal(all - 1, EligibleChestCount(resolution, oneKeep, Options));

        var allKeep = resolution.Containers.ToDictionary(container => container.Id, _ => CategoryAssignment.Keep);
        Assert.Equal(0, EligibleChestCount(resolution, allKeep, Options));

        ContainerSnapshot inUse = eligible.First(container => (container.Flags & ContainerFlags.InUseByOther) != 0);
        Assert.Equal(all, EligibleChestCount(resolution, NoAssignments(), Options));
        Assert.Contains(eligible, container => container.Id.Equals(inUse.Id));
    }

    // GW-U-1759 (AC-4, 9.1): a start outside every base counts 0 stations and 0 chests; null arguments throw with their names.
    [Fact]
    public void ANotInBaseResolutionCountsNothing()
    {
        Assert.Equal(0, StationCount(BaseResolution.NotInBase));
        Assert.Equal(0, EligibleChestCount(BaseResolution.NotInBase, NoAssignments(), Options));
    }

    [Fact]
    public void NullResolutionForStationCountThrowsWithItsName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => StationCount(null!));
        Assert.Equal("resolution", ex.ParamName);
    }

    [Fact]
    public void NullResolutionForEligibleChestCountThrowsWithItsName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => EligibleChestCount(null!, NoAssignments(), Options));
        Assert.Equal("resolution", ex.ParamName);
    }

    [Fact]
    public void NullAssignmentsForEligibleChestCountThrowWithTheirName()
    {
        RunHarness h = RunHarness.Make();
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => EligibleChestCount(ResolveS7(h), null!, Options));
        Assert.Equal("assignments", ex.ParamName);
    }

    private static int StationCount(BaseResolution resolution) =>
        (int)StationActionsTypes.CallStatic("StationActionsFacts", "StationCount", resolution)!;

    private static int EligibleChestCount(BaseResolution resolution, IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments, EligibilityOptions options) =>
        (int)StationActionsTypes.CallStatic("StationActionsFacts", "EligibleChestCount", resolution, assignments, options)!;

    private static string? HolderName(IReadOnlyList<StationSnapshot> stations, IOrganizeLockStore store, TimeSpan now, int organizeLockSeconds) =>
        (string?)StationActionsTypes.CallStatic("StationActionsFacts", "HolderName", stations, store, now, organizeLockSeconds);

    private static BaseResolution ResolveS7(RunHarness h) =>
        h.Cache.Resolve(BaseStart.FromStation(S7), new BaseCacheOptions(0, 64));

    private static Dictionary<ContainerId, CategoryAssignment> NoAssignments() => new Dictionary<ContainerId, CategoryAssignment>();

    private static StationSnapshot Snap(StationId id) => new StationSnapshot(id, new WorldPosition(5f, 30f, 0f), 20f);

    private static string Record(string name, long holderId, double expiresAtSeconds) =>
        OrganizeLockCodec.Encode(new OrganizeLockRecord(holderId, name, TimeSpan.FromSeconds(expiresAtSeconds)));
}
