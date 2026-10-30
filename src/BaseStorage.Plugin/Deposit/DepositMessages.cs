namespace BaseStorage.Plugin.Deposit;

using System;
using System.Globalization;
using BaseStorage.Core.Organize.Run;

/// <summary>The Debug log lines of the Put away inventory button (contract deposit-button-contracts.md section 3.2). They are not strings-table text.</summary>
internal static class DepositMessages
{
    /// <summary>Builds the line of a click.</summary>
    /// <param name="outcome">The outcome of the start.</param>
    /// <returns>The single-line text.</returns>
    internal static string Clicked(OrganizeStartOutcome outcome) =>
        "[station.actions.deposit-button] Deposit " +
        (Enum.IsDefined(typeof(OrganizeStartOutcome), outcome)
            ? outcome.ToString()
            : ((int)outcome).ToString(CultureInfo.InvariantCulture));
}
