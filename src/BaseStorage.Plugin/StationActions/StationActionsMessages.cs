namespace BaseStorage.Plugin.StationActions;

using System;
using System.Globalization;
using BaseStorage.Core.Organize.Run;

/// <summary>The Debug log lines of the station panel (contract S-E004-012 section 3.4). They are not strings-table text.</summary>
internal static class StationActionsMessages
{
    /// <summary>The line written once per session when the controls do not fit next to the craft controls.</summary>
    internal const string NoRoom = "[station.actions.row] hidden: no room next to the craft controls";

    /// <summary>Builds the line of a click.</summary>
    /// <param name="outcome">The outcome of the start.</param>
    /// <returns>The single-line text.</returns>
    internal static string Clicked(OrganizeStartOutcome outcome) =>
        "[station.actions.organize-button] Organize " +
        (Enum.IsDefined(typeof(OrganizeStartOutcome), outcome)
            ? outcome.ToString()
            : ((int)outcome).ToString(CultureInfo.InvariantCulture));
}
