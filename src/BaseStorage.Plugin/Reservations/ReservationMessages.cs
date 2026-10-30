namespace BaseStorage.Plugin.Reservations;

using System;
using System.Globalization;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The Debug line formats of the reservation driver (contract S-E005-002 section 5, M1-M6). Game-free. The chest id renders as
/// UserId:Id and enum values render by name, both with the invariant culture; an undefined enum value renders as its integer.
/// </summary>
internal static class ReservationMessages
{
    private const string Prefix = "Reserve ";
    private const string LateLabel = "late";

    /// <summary>Formats M1: the route taken for a reservation.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="operation">The operation label.</param>
    /// <param name="route">The route.</param>
    /// <returns>The Debug line.</returns>
    internal static string Route(ContainerId chest, string operation, ReservationRoute route) =>
        Head(chest, operation) + "route " + EnumName(route);

    /// <summary>Formats M2: a state transition.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="operation">The operation label.</param>
    /// <param name="from">The state before.</param>
    /// <param name="to">The state after.</param>
    /// <param name="failure">The failure, written only when the state after is Failed.</param>
    /// <returns>The Debug line.</returns>
    internal static string Transition(ContainerId chest, string operation, ReservationState from, ReservationState to, ReservationFailure failure)
    {
        string line = Head(chest, operation) + EnumName(from) + " -> " + EnumName(to);
        return to == ReservationState.Failed ? line + " (" + EnumName(failure) + ")" : line;
    }

    /// <summary>Formats M3: the chest is in use by another player.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="operation">The operation label.</param>
    /// <param name="holderLabel">The holder label.</param>
    /// <returns>The Debug line.</returns>
    internal static string InUse(ContainerId chest, string operation, string holderLabel)
    {
        if (holderLabel is null)
        {
            throw new ArgumentNullException(nameof(holderLabel));
        }

        return Head(chest, operation) + "in use by " + holderLabel;
    }

    /// <summary>Formats M4: an open response was consumed for a reservation.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="operation">The operation label.</param>
    /// <param name="granted">True when the response was a grant.</param>
    /// <returns>The Debug line.</returns>
    internal static string ResponseConsumed(ContainerId chest, string operation, bool granted) =>
        Head(chest, operation) + Response(granted);

    /// <summary>Formats M5: a late open response was consumed.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="granted">True when the response was a grant.</param>
    /// <returns>The Debug line.</returns>
    internal static string LateResponseConsumed(ContainerId chest, bool granted) =>
        Head(chest, LateLabel) + Response(granted);

    /// <summary>Formats M6: the chest is already reserved.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="operation">The operation label.</param>
    /// <returns>The Debug line.</returns>
    internal static string AlreadyReserved(ContainerId chest, string operation) =>
        Head(chest, operation) + "already reserved";

    private static string Head(ContainerId chest, string operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        return Prefix + Id(chest) + " [" + operation + "]: ";
    }

    private static string Response(bool granted) =>
        "open response " + (granted ? "granted" : "refused") + " consumed, chest UI not shown";

    private static string Id(ContainerId chest) =>
        chest.UserId.ToString(CultureInfo.InvariantCulture) + ":" + chest.Id.ToString(CultureInfo.InvariantCulture);

    private static string EnumName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value)
            ? value.ToString()
            : Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }
}
