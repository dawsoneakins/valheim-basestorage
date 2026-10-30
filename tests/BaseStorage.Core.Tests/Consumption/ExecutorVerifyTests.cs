namespace BaseStorage.Core.Tests.Consumption;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>AC-2 (contract 3.3, F5, F6): the verify step checks the inventory first, then each chest's contents, and consumes nothing on a mismatch.</summary>
public sealed class ExecutorVerifyTests
{
    // GW-U-510 (AC-2, F5, CX11): a changed chest consumes nothing and maps to Materials moved.
    [Fact]
    public void AChangedChestConsumesNothingAndMapsToMaterialsMoved()
    {
        RecordingContainerLayer layer = LayerW(chest7: Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(9)));
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.ContentsChanged, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ReservationFailure.None, result.ReservationFailure);
        Assert.Null(result.FailedOperation);
        Assert.Empty(result.Unrestored);
        Assert.Equal("message.craft.materials-moved", result.MessageKey);
        Assert.Equal(
            new[]
            {
                "Request:5", "Request:7", "Status:5=Pending", "Status:7=Pending", "Status:5=Granted", "Status:7=Granted",
                "InvId", "Get:1", "Get:5", "Get:7", "Release:5", "Release:7",
            },
            layer.Calls);
        AssertUntouched(result, layer, before);
    }

    // GW-U-511 (AC-2, F6): a changed inventory is checked before the chests and consumes nothing.
    [Fact]
    public void AChangedInventoryIsReportedBeforeTheChests()
    {
        RecordingContainerLayer layer = LayerW(inventory: Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(2)));
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.ContentsChanged, result.Outcome);
        Assert.Equal(Id(1), result.FailedContainer);
        Assert.Equal(
            new[]
            {
                "Request:5", "Request:7", "Status:5=Pending", "Status:7=Pending", "Status:5=Granted", "Status:7=Granted",
                "InvId", "Get:1", "Release:5", "Release:7",
            },
            layer.Calls);
        AssertUntouched(result, layer, before);
    }

    // GW-U-511 (AC-2, F5 and F6): with both the inventory and a chest changed, the inventory is still the reported container.
    [Fact]
    public void WhenTheInventoryAndAChestBothChangedTheInventoryIsReported()
    {
        RecordingContainerLayer layer = LayerW(
            inventory: Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(2)),
            chest7: Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(9)));

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.ContentsChanged, result.Outcome);
        Assert.Equal(Id(1), result.FailedContainer);
    }

    // GW-U-512 (AC-2, 3.3 edge table): one named row per verify edge case.
    [Theory]
    [InlineData("inventory id differs")]
    [InlineData("verify fetch returns null")]
    [InlineData("chest gained an unrelated stack")]
    [InlineData("position and flags differ only")]
    [InlineData("unrelated stack in the inventory")]
    [InlineData("inventory is empty")]
    public void TheVerifyEdgeTableRowHolds(string caseName)
    {
        RecordingContainerLayer layer = caseName switch
        {
            "inventory id differs" => LayerW(inventory: Make(2u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(3))),
            "chest gained an unrelated stack" => LayerW(chest7: Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(10), Stack("Resin", 1, 1, 50))),
            "position and flags differ only" => LayerW(chest5: Make(5u, 9f, 1f, 9f, 6, 3, ContainerFlags.None, Wood(8), Stone(4))),
            "unrelated stack in the inventory" => LayerW(inventory: Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(3), Stone(9))),
            "inventory is empty" => LayerW(inventory: Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None)),
            _ => LayerW(),
        };
        if (caseName == "verify fetch returns null")
        {
            layer.GetOverride(Id(5), 1, null);
        }

        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionPlan plan = PlanW();
        ConsumptionResult result = Drive(Executor(layer).Execute(plan));

        if (caseName is "position and flags differ only" or "unrelated stack in the inventory")
        {
            Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
            Assert.Null(result.MessageKey);
            AssertConsumedExactly(before, layer.Inner.Capture(), plan.Requirements);
            if (caseName == "unrelated stack in the inventory")
            {
                Assert.Contains(Stone(9), FakeFixtures.Get(layer.Inner, Id(1)).Contents);
            }

            return;
        }

        ContainerId expected = caseName switch
        {
            "verify fetch returns null" => Id(5),
            "chest gained an unrelated stack" => Id(7),
            _ => Id(1),
        };
        Assert.Equal(ConsumptionOutcome.ContentsChanged, result.Outcome);
        Assert.Equal(expected, result.FailedContainer);
        AssertUntouched(result, layer, before);
    }
}
