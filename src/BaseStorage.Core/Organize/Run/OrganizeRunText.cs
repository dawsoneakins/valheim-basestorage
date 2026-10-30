namespace BaseStorage.Core.Organize.Run;

using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Ui;

/// <summary>The strings-table keys of the run's stop reasons (contract 4.3). Pure; the texts live only in the table.</summary>
public static class OrganizeRunText
{
    /// <summary>The key of the reason shown when the base lock was lost or taken over during the run.</summary>
    public const string LockLostKey = StringKeys.OrganizeStopReasonLockLost;

    /// <summary>Gets the strings-table key of an executor stop reason.</summary>
    /// <param name="reason">The stop reason.</param>
    /// <returns>The key; the aborted key for None or an undefined value.</returns>
    public static string StopReasonKey(ExecutionStopReason reason)
    {
        switch (reason)
        {
            case ExecutionStopReason.ReservationRefused:
                return StringKeys.OrganizeStopReasonReservationRefused;
            case ExecutionStopReason.ReservationTimedOut:
                return StringKeys.OrganizeStopReasonReservationTimedOut;
            case ExecutionStopReason.ReservationLost:
                return StringKeys.OrganizeStopReasonReservationLost;
            case ExecutionStopReason.ChestUnavailable:
                return StringKeys.OrganizeStopReasonChestUnavailable;
            case ExecutionStopReason.ChestChanged:
                return StringKeys.OrganizeStopReasonChestChanged;
            case ExecutionStopReason.DestinationFull:
                return StringKeys.OrganizeStopReasonDestinationFull;
            case ExecutionStopReason.RollbackFailed:
                return StringKeys.OrganizeStopReasonRollbackFailed;
            default:
                return StringKeys.OrganizeStopReasonAborted;
        }
    }
}
