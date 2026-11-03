namespace BaseStorage.Core.Tests.Restock.Run;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots.Identities;
using BaseStorage.Core.Tests.Snapshots.Identities;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2600: the recording Restock layer models every add mode of contract 10.1 (test code, proven here).</summary>
public sealed class FakeRestockLayerTests
{
    /// <summary>GW-U-2600: Normal adds what fits and reports true only when all was added; PartialOnce merges once and returns false; Refuse adds nothing; Corrupt swaps the identity; scripted failures fire once; every call is recorded in order.</summary>
    [Fact]
    public void FakeRestockLayerModelsEveryAddMode()
    {
        var layer = new FakeRestockLayer(Carried(), Chests());

        Assert.True(layer.AddToCarried(0, S("Wood", 10), 30));
        Assert.Equal(40, layer.Carried[0]!.Count);
        Assert.False(layer.AddToCarried(0, S("Wood", 40), 20));
        Assert.Equal(50, layer.Carried[0]!.Count);

        layer.MutateCarried(1, S("Resin", 5));
        layer.SetMode(1, RestockAddMode.PartialOnce, 25);
        Assert.False(layer.AddToCarried(1, S("Resin", 5), 30));
        Assert.Equal(30, layer.Carried[1]!.Count);
        Assert.True(layer.AddToCarried(1, S("Resin", 30), 10));
        Assert.Equal(40, layer.Carried[1]!.Count);

        layer.SetMode(2, RestockAddMode.Refuse);
        Assert.False(layer.AddToCarried(2, S("Stone", 10), 5));
        Assert.Equal(10, layer.Carried[2]!.Count);

        layer.SetMode(0, RestockAddMode.Corrupt);
        Assert.True(layer.AddToCarried(0, S("Wood", 50), 0));
        Assert.Equal(IdentityFixtures.Wl2(), layer.Carried[0]!.Identity);

        layer.SetReservation(801, ReservationStatus.Granted);
        layer.FailRemoveOnce(801, 0, ContainerOperationResult.ContainerChanged);
        layer.RequestReservation(Id(801));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(Id(801)));
        Assert.Equal(ContainerOperationResult.ContainerChanged, layer.RemoveFromStack(Id(801), 0, S("Wood", 30), 30));
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(Id(801), 0, S("Wood", 30), 30));
        layer.ReleaseReservation(Id(801));

        layer.RollbackFails = true;
        Assert.False(layer.RemoveFromCarried(1, S("Resin", 40), 10));
        layer.RollbackFails = false;
        Assert.True(layer.RemoveFromCarried(1, S("Resin", 40), 10));

        Assert.Equal(
            new[]
            {
                "Add t0 30", "Add t0 20", "Add t1 30", "Add t1 10", "Add t2 5", "Add t0 0",
                "Req 801", "Remove 801#0 30", "Remove 801#0 30", "Release 801",
                "Unadd t1 10", "Unadd t1 10",
            },
            layer.Calls);
    }
}
