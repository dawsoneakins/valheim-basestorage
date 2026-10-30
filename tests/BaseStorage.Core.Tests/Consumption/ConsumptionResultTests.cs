namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Snapshots;
using BaseStorage.Core.Ui;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>AC-2 and AC-3 (contract 2.4, 4.3 MessageKeyFor table, CX11): the message mapping, the result invariants and value equality.</summary>
public sealed class ConsumptionResultTests
{
    // GW-U-513 (AC-2, CX11): every failure outcome maps to the materials-moved key.
    [Theory]
    [InlineData(ConsumptionOutcome.ReservationFailed)]
    [InlineData(ConsumptionOutcome.ContentsChanged)]
    [InlineData(ConsumptionOutcome.TakeFailed)]
    [InlineData(ConsumptionOutcome.Cancelled)]
    [InlineData(ConsumptionOutcome.Faulted)]
    public void EveryFailureOutcomeMapsToMaterialsMoved(ConsumptionOutcome outcome)
    {
        Assert.Equal(MaterialsMoved, ConsumptionResult.MessageKeyFor(outcome));
    }

    // GW-U-513 (AC-2, CX11): success maps to no message.
    [Fact]
    public void SuccessMapsToNoMessage()
    {
        Assert.Null(ConsumptionResult.MessageKeyFor(ConsumptionOutcome.Success));
    }

    // GW-U-513 (AC-2, 4.3): an undefined outcome is rejected, with the parameter name outcome.
    [Fact]
    public void AnUndefinedOutcomeIsRejectedWithItsParameterName()
    {
        ArgumentOutOfRangeException thrown =
            Assert.Throws<ArgumentOutOfRangeException>(() => ConsumptionResult.MessageKeyFor((ConsumptionOutcome)99));

        Assert.Equal("outcome", thrown.ParamName);
    }

    // GW-U-513 (AC-2, CX11): the English string table formats the key as the literal text.
    [Fact]
    public void TheEnglishTableFormatsTheKeyAsMaterialsMovedText()
    {
        string? key = ConsumptionResult.MessageKeyFor(ConsumptionOutcome.ContentsChanged);

        Assert.Equal(StringKeys.MessageCraftMaterialsMoved, key);
        Assert.Equal(MaterialsMovedText, StringTable.English.Format(key!));
    }

    // GW-U-528 (AC-3, 2.4): each outcome, produced by a real run, has the full section 2.4 row.
    [Theory]
    [InlineData("happy")]
    [InlineData("F1")]
    [InlineData("F5")]
    [InlineData("F7")]
    [InlineData("F12")]
    [InlineData("F10")]
    public void EachProducedResultHasTheSection24Row(string name)
    {
        ConsumptionResult result = ConsumptionScenarios.Run(name).Result;
        (ConsumptionOutcome Outcome, ContainerId Container, ReservationFailure Failure, ContainerOperationResult? Operation, string? Key) row =
            name switch
            {
                "happy" => (ConsumptionOutcome.Success, default, ReservationFailure.None, null, null),
                "F1" => (ConsumptionOutcome.ReservationFailed, Id(7), ReservationFailure.Denied, null, MaterialsMoved),
                "F5" => (ConsumptionOutcome.ContentsChanged, Id(7), ReservationFailure.None, null, MaterialsMoved),
                "F7" => (ConsumptionOutcome.TakeFailed, Id(7), ReservationFailure.None, ContainerOperationResult.ContainerChanged, MaterialsMoved),
                "F12" => (ConsumptionOutcome.Cancelled, default, ReservationFailure.None, null, MaterialsMoved),
                _ => (ConsumptionOutcome.Faulted, default, ReservationFailure.None, null, MaterialsMoved),
            };

        Assert.Equal(row.Outcome, result.Outcome);
        Assert.Equal(row.Outcome == ConsumptionOutcome.Success, result.IsSuccess);
        Assert.Equal(row.Container, result.FailedContainer);
        Assert.Equal(row.Failure, result.ReservationFailure);
        Assert.Equal(row.Operation, result.FailedOperation);
        Assert.Empty(result.Unrestored);
        Assert.Equal(row.Key, result.MessageKey);
    }

    // GW-U-528 (AC-3, 2.4): Unrestored is read-only.
    [Fact]
    public void UnrestoredIsReadOnly()
    {
        ConsumptionResult result = ConsumptionScenarios.Run("F7").Result;

        Assert.True(((ICollection<ItemStackSnapshot>)result.Unrestored).IsReadOnly);
    }

    // GW-U-528 (AC-3, 2.4): two identical F7 runs give equal results, which are not the same instance.
    [Fact]
    public void TwoIdenticalRunsGiveEqualResults()
    {
        ConsumptionResult first = ConsumptionScenarios.Run("F7").Result;
        ConsumptionResult second = ConsumptionScenarios.Run("F7").Result;

        ValueSemantics.AssertEqual(first, second);
        Assert.NotSame(first, second);
    }

    // GW-U-528 (AC-3, 2.4): F1 and F5 are not equal.
    [Fact]
    public void DifferentOutcomesAreNotEqual()
    {
        ConsumptionResult refused = ConsumptionScenarios.Run("F1").Result;
        ConsumptionResult changed = ConsumptionScenarios.Run("F5").Result;

        ValueSemantics.AssertNotEqual(refused, changed);
    }

    // GW-U-528 (AC-3, 2.4): F8 and F9 differ only in Unrestored, and are not equal.
    [Fact]
    public void ResultsThatDifferOnlyInUnrestoredAreNotEqual()
    {
        ConsumptionResult f8 = ConsumptionScenarios.Run("F8").Result;
        ConsumptionResult f9 = ConsumptionScenarios.Run("F9").Result;

        Assert.Equal(f8.Outcome, f9.Outcome);
        Assert.Equal(f8.FailedContainer, f9.FailedContainer);
        Assert.Equal(f8.FailedOperation, f9.FailedOperation);
        Assert.Equal(f8.MessageKey, f9.MessageKey);
        Assert.Empty(f8.Unrestored);
        Assert.Equal(new[] { Wood(8), Wood(3) }, f9.Unrestored);
        ValueSemantics.AssertNotEqual(f8, f9);
    }

    // GW-U-528 (AC-3, 2.4): null handling of the equality operators and Equals.
    [Fact]
    public void NullHandlingOfEqualityIsCorrect()
    {
        ConsumptionResult result = ConsumptionScenarios.Run("F7").Result;

        ValueSemantics.AssertNullHandling(typeof(ConsumptionResult), result);
    }
}
