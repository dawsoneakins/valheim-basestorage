namespace BaseStorage.Core.Tests.Restock.Run;

using System.Linq;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2566 and 2571: identity and marks decide what the run moves (S-QR-002 contract 4.1, RR5, 10.1).</summary>
public sealed class RestockRunIdentityTests
{
    /// <summary>GW-U-2566: the captured identity decides the stack that moves; only the matching chest stack is taken, and the add uses the Wl2 expectation.</summary>
    [Fact]
    public void RR5_IdentityFromCaptureDecidesWhichStackMoves()
    {
        RunHarness harness = Harness(keep803: false, chests: new[]
        {
            Chest(801, 5f, 6f, S("Wood", 30), WoodWl2(20)),
        });
        harness.Port.Carried = new System.Collections.Generic.List<CarriedStack> { H(WoodWl2(10)) };

        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        RunToEnd(harness);

        FakeRestockLayer fake = harness.Port.RestockFake!;
        Assert.Equal(new[] { "Req 801", "Add t0 20", "Remove 801#1 20", "Release 801" }, fake.Writes());
        Assert.Equal(Wl2Identity(), fake.Carried[0]!.Identity);
        Assert.Equal(30, fake.Carried[0]!.Count);
        Assert.Equal(30, fake.Chest(801)[0]!.Count);
        // Edited by S-QR-002 (ADR-GWIN-176): left behind is the need remaining after the moves (50 - 10 - 20), capped by the other-identity Wood (30).
        Assert.Equal(new[] { "Restocked 1 stack, 20 items; 20 items left behind" }, Summaries(harness));
    }

    /// <summary>GW-U-2571: only the hotbar and favourite stacks are topped up; the unmarked stack and the chest's Stone stay as they are.</summary>
    [Fact]
    public void RR5_OnlyMarkedCarriedStacksAreToppedUp()
    {
        RunHarness harness = Harness(keep803: false, chests: new[]
        {
            Chest(801, 5f, 6f, S("Wood", 30), S("Resin", 20), S("Stone", 30)),
        });
        harness.Port.Carried = new System.Collections.Generic.List<CarriedStack> { H(S("Wood", 10)), F(S("Resin", 5)), U(S("Stone", 10)) };

        Assert.Equal(OrganizeStartOutcome.Started, harness.Coordinator.StartRestock(Player));
        RunToEnd(harness);

        FakeRestockLayer fake = harness.Port.RestockFake!;
        Assert.DoesNotContain(fake.Writes(), line => line.StartsWith("Add t2 ", System.StringComparison.Ordinal));
        Assert.Equal(10, fake.Carried[2]!.Count);
        Assert.Equal(30, fake.Chest(801)[2]!.Count);
        Assert.Contains("Add t0 30", fake.Writes());
        Assert.Contains("Add t1 20", fake.Writes());
        Assert.True(fake.Writes().Count(line => line.StartsWith("Add ", System.StringComparison.Ordinal)) == 2);
    }

    private static BaseStorage.Core.Snapshots.Identities.ItemStackIdentity Wl2Identity() => BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures.Wl2();
}
