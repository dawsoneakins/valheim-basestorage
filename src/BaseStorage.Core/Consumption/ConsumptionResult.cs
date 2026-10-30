namespace BaseStorage.Core.Consumption;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// The terminal result of one consumption execution. It has value equality over the stored outcome, failure, failed operation and
/// unrestored items. It is produced only by ConsumptionExecution, so its constructor is internal.
/// </summary>
public sealed class ConsumptionResult : IEquatable<ConsumptionResult>
{
    internal ConsumptionResult(
        ConsumptionOutcome outcome,
        ContainerId failedContainer,
        ReservationFailure reservationFailure,
        ContainerOperationResult? failedOperation,
        IReadOnlyList<ItemStackSnapshot> unrestored)
    {
        Outcome = outcome;
        FailedContainer = failedContainer;
        ReservationFailure = reservationFailure;
        FailedOperation = failedOperation;
        var copy = new List<ItemStackSnapshot>(unrestored);
        Unrestored = new System.Collections.ObjectModel.ReadOnlyCollection<ItemStackSnapshot>(copy);
    }

    /// <summary>Gets the terminal outcome.</summary>
    public ConsumptionOutcome Outcome { get; }

    /// <summary>Gets a value indicating whether Outcome is Success; only then may the caller create the item or place the piece.</summary>
    public bool IsSuccess => Outcome == ConsumptionOutcome.Success;

    /// <summary>Gets the container that caused a ReservationFailed, ContentsChanged or TakeFailed outcome; the none id for every other outcome.</summary>
    public ContainerId FailedContainer { get; }

    /// <summary>Gets why the reservation failed when Outcome is ReservationFailed; otherwise None.</summary>
    public ReservationFailure ReservationFailure { get; }

    /// <summary>Gets the layer result that stopped the take phase when Outcome is TakeFailed; otherwise null.</summary>
    public ContainerOperationResult? FailedOperation { get; }

    /// <summary>Gets the items that were taken but could not be put back anywhere, in rollback order. It is empty in every normal case and is read-only.</summary>
    public IReadOnlyList<ItemStackSnapshot> Unrestored { get; }

    /// <summary>Gets the string key of the player message: null for Success, <see cref="StringKeys.MessageCraftMaterialsMoved"/> otherwise.</summary>
    public string? MessageKey => MessageKeyFor(Outcome);

    /// <summary>Gets the message key for an outcome: null for Success, <see cref="StringKeys.MessageCraftMaterialsMoved"/> for every other defined outcome.</summary>
    /// <param name="outcome">The outcome to map.</param>
    /// <returns>The message key, or null for Success.</returns>
    /// <exception cref="ArgumentOutOfRangeException">outcome is not a defined value. ParamName is "outcome".</exception>
    public static string? MessageKeyFor(ConsumptionOutcome outcome)
    {
        switch (outcome)
        {
            case ConsumptionOutcome.Success:
                return null;
            case ConsumptionOutcome.ReservationFailed:
            case ConsumptionOutcome.ContentsChanged:
            case ConsumptionOutcome.TakeFailed:
            case ConsumptionOutcome.Cancelled:
            case ConsumptionOutcome.Faulted:
                return StringKeys.MessageCraftMaterialsMoved;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }
    }

    /// <summary>Determines whether every stored member is equal to the other result's.</summary>
    /// <param name="other">The result to compare with, or null.</param>
    /// <returns>True when equal; false for null.</returns>
    public bool Equals(ConsumptionResult? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Outcome != other.Outcome
            || FailedContainer != other.FailedContainer
            || ReservationFailure != other.ReservationFailure
            || FailedOperation != other.FailedOperation
            || Unrestored.Count != other.Unrestored.Count)
        {
            return false;
        }

        for (int i = 0; i < Unrestored.Count; i++)
        {
            if (Unrestored[i] != other.Unrestored[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the object is an equal result.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ConsumptionResult);
    }

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Outcome;
            hash = (hash * 31) + FailedContainer.GetHashCode();
            hash = (hash * 31) + (int)ReservationFailure;
            hash = (hash * 31) + (FailedOperation.HasValue ? (int)FailedOperation.Value : -1);
            foreach (ItemStackSnapshot item in Unrestored)
            {
                hash = (hash * 31) + item.GetHashCode();
            }

            return hash;
        }
    }

    /// <summary>Determines whether two results are equal; null is handled on either side.</summary>
    /// <param name="left">The first result, or null.</param>
    /// <param name="right">The second result, or null.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(ConsumptionResult? left, ConsumptionResult? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>Determines whether two results differ; null is handled on either side.</summary>
    /// <param name="left">The first result, or null.</param>
    /// <param name="right">The second result, or null.</param>
    /// <returns>True when they are not equal.</returns>
    public static bool operator !=(ConsumptionResult? left, ConsumptionResult? right)
    {
        return !(left == right);
    }
}
