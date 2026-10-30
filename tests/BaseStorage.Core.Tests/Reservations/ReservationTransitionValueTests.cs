namespace BaseStorage.Core.Tests.Reservations;

using System;
using BaseStorage.Core.Reservations;

/// <summary>AC-1: the ReservationTransition value (contract 3.3). Examples are typed as literals from the contract.</summary>
public sealed class ReservationTransitionValueTests
{
    // GW-U-155 (AC-1, 3.3): the first example, a legal row, equals the literal row and the other example row differs.
    [Fact]
    public void ContractExampleOfALegalRowEqualsTheLiteralRow()
    {
        var row = new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None);

        Assert.True(row == new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None));
        Assert.False(row == new ReservationTransition(ReservationState.Requesting, ReservationEvent.Deny, ReservationState.Failed, ReservationFailure.Denied));
    }

    // GW-U-155 (AC-1, 3.3): the four validation examples of the contract, each with its exact exception type and ParamName.
    [Fact]
    public void ContractValidationExamplesThrowTheContractedExceptions()
    {
        ArgumentException inconsistent = Assert.Throws<ArgumentException>(() =>
            new ReservationTransition(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.None));
        Assert.Equal("failure", inconsistent.ParamName);

        ArgumentException timeoutOnLegalRow = Assert.Throws<ArgumentException>(() =>
            new ReservationTransition(ReservationState.Granted, ReservationEvent.BeginWrite, ReservationState.Writing, ReservationFailure.Timeout));
        Assert.Equal("failure", timeoutOnLegalRow.ParamName);

        ArgumentOutOfRangeException badFrom = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationTransition((ReservationState)9, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None));
        Assert.Equal("from", badFrom.ParamName);

        ArgumentOutOfRangeException badTrigger = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationTransition(ReservationState.Granted, (ReservationEvent)42, ReservationState.Writing, ReservationFailure.None));
        Assert.Equal("trigger", badTrigger.ParamName);
    }

    // GW-U-155 (AC-1, 3.3): an undefined to or failure is an out-of-range error on that parameter.
    [Fact]
    public void UndefinedToAndFailureAreOutOfRangeOnTheirParameter()
    {
        ArgumentOutOfRangeException badTo = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, (ReservationState)9, ReservationFailure.None));
        Assert.Equal("to", badTo.ParamName);

        ArgumentOutOfRangeException badFailure = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, (ReservationFailure)42));
        Assert.Equal("failure", badFailure.ParamName);
    }

    // GW-U-155 (AC-1, 3.3): validation order reports from before the failure consistency check.
    [Fact]
    public void UndefinedFromIsReportedBeforeAnInconsistentFailure()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReservationTransition((ReservationState)9, ReservationEvent.Grant, ReservationState.Failed, ReservationFailure.None));

        Assert.Equal("from", ex.ParamName);
    }

    // GW-U-155 (AC-1, R5): equal values give ==, Equals(object) and equal hash codes.
    [Fact]
    public void EqualValuesAreEqualInEveryWay()
    {
        var first = new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None);
        var second = new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None);
        object boxed = second;

        Assert.True(first == second);
        Assert.False(first != second);
        Assert.True(first.Equals(boxed));
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    // GW-U-155 (AC-1, R5): != holds when any one of the four members differs.
    [Fact]
    public void InequalityHoldsWhenAnySingleMemberDiffers()
    {
        var baseline = new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None);

        var otherFrom = new ReservationTransition(ReservationState.Granted, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None);
        var otherTrigger = new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginWrite, ReservationState.Releasing, ReservationFailure.None);
        var otherTo = new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Granted, ReservationFailure.None);

        var failedBase = new ReservationTransition(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther);
        var otherFailure = new ReservationTransition(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.OwnerLost);

        Assert.True(baseline != otherFrom);
        Assert.True(baseline != otherTrigger);
        Assert.True(baseline != otherTo);
        Assert.True(failedBase != otherFailure);
        Assert.False(failedBase != new ReservationTransition(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther));
    }

    // GW-U-155 (AC-1, 3.3): default is (Requesting, Grant, Requesting, None), and it is not a legal row.
    [Fact]
    public void DefaultIsTheRequestingGrantRequestingRowThatIsNotLegal()
    {
        ReservationTransition unset = default;

        Assert.Equal(ReservationState.Requesting, unset.From);
        Assert.Equal(ReservationEvent.Grant, unset.Trigger);
        Assert.Equal(ReservationState.Requesting, unset.To);
        Assert.Equal(ReservationFailure.None, unset.Failure);
        Assert.DoesNotContain(ReservationTransitions.All, row => row.Equals(unset));
    }

    // GW-U-155 (AC-1, R5): Equals with null, or with another type, is false.
    [Fact]
    public void EqualsOfNullOrAnotherTypeIsFalse()
    {
        var row = new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None);
        object? nothing = null;

        Assert.False(row.Equals(nothing));
        Assert.False(row.Equals("x"));
    }
}
