namespace BaseStorage.Plugin.Crafting;

using System;
using System.Globalization;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;

/// <summary>The Debug line formats of a craft (contract craft-from-base 6, K1-K5). Game-free.</summary>
internal static class CraftMessages
{
    /// <summary>Formats K1: a craft started.</summary>
    /// <param name="token">The craft token.</param>
    /// <param name="takes">The number of takes.</param>
    /// <param name="chests">The number of chests.</param>
    /// <returns>The Debug line.</returns>
    internal static string Started(int token, int takes, int chests) =>
        "Craft " + Int(token) + ": started, " + Int(takes) + " take(s) from " + Int(chests) + " chest(s)";

    /// <summary>Formats K2: a craft ended.</summary>
    /// <param name="token">The craft token.</param>
    /// <param name="end">How it ended.</param>
    /// <returns>The Debug line.</returns>
    internal static string Ended(int token, CraftEnd end) => "Craft " + Int(token) + ": " + EnumName(end);

    /// <summary>Formats K3: the executor failed.</summary>
    /// <param name="token">The craft token.</param>
    /// <param name="outcome">The executor outcome.</param>
    /// <param name="container">The failed container, or none.</param>
    /// <returns>The Debug line.</returns>
    internal static string Failed(int token, ConsumptionOutcome outcome, ContainerId container)
    {
        string line = "Craft " + Int(token) + ": Failed (" + EnumName(outcome);
        if (!container.IsNone)
        {
            line += " at " + container.UserId.ToString(CultureInfo.InvariantCulture) + ":" + container.Id.ToString(CultureInfo.InvariantCulture);
        }

        return line + ")";
    }

    /// <summary>Formats K4: taken stacks were put back.</summary>
    /// <param name="token">The craft token.</param>
    /// <param name="stacks">The number of stacks put back.</param>
    /// <returns>The Debug line.</returns>
    internal static string Returned(int token, int stacks) =>
        "Craft " + Int(token) + ": Returned, " + Int(stacks) + " stack(s) put back";

    /// <summary>Formats K5: the craft runs vanilla.</summary>
    /// <param name="reason">The gate reason.</param>
    /// <returns>The Debug line.</returns>
    internal static string Vanilla(CraftGateReason reason) => "Craft: vanilla (" + EnumName(reason) + ")";

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string EnumName<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Enum.IsDefined(typeof(TEnum), value)
            ? value.ToString()
            : Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
}
