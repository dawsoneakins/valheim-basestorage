namespace BaseStorage.Plugin.Building;

using System;
using System.Globalization;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;

/// <summary>The Debug line formats of a build (contract build-from-base 6, BK1-BK6). Game-free.</summary>
internal static class BuildMessages
{
    /// <summary>Formats BK1: a build started.</summary>
    /// <param name="token">The build token.</param>
    /// <param name="takes">The number of takes.</param>
    /// <param name="chests">The number of chests.</param>
    /// <returns>The Debug line.</returns>
    internal static string Started(int token, int takes, int chests) =>
        "Build " + Int(token) + ": started, " + Int(takes) + " take(s) from " + Int(chests) + " chest(s)";

    /// <summary>Formats BK2: a build ended.</summary>
    /// <param name="token">The build token.</param>
    /// <param name="end">How it ended.</param>
    /// <returns>The Debug line.</returns>
    internal static string Ended(int token, BuildEnd end) => "Build " + Int(token) + ": " + EnumName(end);

    /// <summary>Formats BK3: the executor failed.</summary>
    /// <param name="token">The build token.</param>
    /// <param name="outcome">The executor outcome.</param>
    /// <param name="container">The failed container, or none.</param>
    /// <returns>The Debug line.</returns>
    internal static string Failed(int token, ConsumptionOutcome outcome, ContainerId container)
    {
        string line = "Build " + Int(token) + ": Failed (" + EnumName(outcome);
        if (!container.IsNone)
        {
            line += " at " + container.UserId.ToString(CultureInfo.InvariantCulture) + ":" + container.Id.ToString(CultureInfo.InvariantCulture);
        }

        return line + ")";
    }

    /// <summary>Formats BK4: taken stacks were put back.</summary>
    /// <param name="token">The build token.</param>
    /// <param name="stacks">The number of stacks put back.</param>
    /// <returns>The Debug line.</returns>
    internal static string Returned(int token, int stacks) =>
        "Build " + Int(token) + ": Returned, " + Int(stacks) + " stack(s) put back";

    /// <summary>Formats BK5: the build runs vanilla.</summary>
    /// <param name="reason">The gate reason.</param>
    /// <returns>The Debug line.</returns>
    internal static string Vanilla(BuildGateReason reason) => "Build: vanilla (" + EnumName(reason) + ")";

    /// <summary>Formats BK6: a placement was suppressed by the guard.</summary>
    /// <returns>The Debug line.</returns>
    internal static string Suppressed() => "Build: placement suppressed, inventory does not cover the piece";

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string EnumName<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Enum.IsDefined(typeof(TEnum), value)
            ? value.ToString()
            : Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
}
