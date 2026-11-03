namespace BaseStorage.Core.Tests.Restock.Run;

using System.Linq;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2580 and 2581: the Keep chest is a source only when KeepChestsCraftable is true (S-QR-002 contract 10.2, RR4).</summary>
public sealed class RestockRunKeepTests
{
    /// <summary>GW-U-2580: with KeepChestsCraftable false, Keep chest 803 is not a source and the call log is exact.</summary>
    [Fact]
    public void RR4_KeepChestIsNotASourceWhenKeepChestsCraftableIsFalse()
    {
        RunHarness harness = Harness(keepChestsCraftable: false);
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        RunToEnd(harness);

        FakeRestockLayer fake = harness.Port.RestockFake!;
        Assert.Equal(
            new[]
            {
                "Req 801", "Add t0 30", "Remove 801#0 30", "Add t1 20", "Remove 801#1 20", "Release 801",
                "Req 802", "Add t0 10", "Remove 802#0 10", "Release 802",
            },
            fake.Writes());
        Assert.Equal(50, fake.Chest(803)[0]!.Count);
        Assert.Equal(50, fake.Carried[0]!.Count);
        Assert.Equal(25, fake.Carried[1]!.Count);
        Assert.Equal(10, fake.Carried[2]!.Count);
        Assert.Equal(new[] { "Restocked 2 stacks, 60 items" }, Summaries(harness));
        Assert.Equal(Summaries(harness), InfoLines(harness));
    }

    /// <summary>GW-U-2581: with KeepChestsCraftable true, Keep chest 803 is a source, and the plan takes from it.</summary>
    [Fact]
    public void RR4_KeepChestIsASourceWhenKeepChestsCraftableIsTrue()
    {
        RunHarness harness = Harness(keepChestsCraftable: true);
        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));

        RunToEnd(harness);

        FakeRestockLayer fake = harness.Port.RestockFake!;
        Assert.Equal(
            new[]
            {
                "Req 803", "Add t0 40", "Remove 803#0 40", "Release 803",
                "Req 801", "Add t1 20", "Remove 801#1 20", "Release 801",
            },
            fake.Writes());
        Assert.Equal(10, fake.Chest(803)[0]!.Count);
        Assert.Contains(harness.Port.RestockLayerCalls[0].Moves, move => move.Source == Id(803));
        Assert.Equal(new[] { "Restocked 2 stacks, 60 items" }, Summaries(harness));
    }
}
