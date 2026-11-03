namespace BaseStorage.Core.Restock.Run;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Ui;

/// <summary>
/// The one result line of a Restock run (S-QR-002 contract sections 3.5 and 6.3).
/// </summary>
public static class RestockRunText
{
    /// <summary>Composes the one result line.</summary>
    /// <param name="toppedUpStacks">The distinct topped-up stacks; not negative.</param>
    /// <param name="movedItems">The moved items; not negative.</param>
    /// <param name="leftBehind">The items left behind; not negative.</param>
    /// <param name="stopped">True when the run stopped early.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A count is negative; ParamName names it.</exception>
    /// <exception cref="ArgumentException">Exactly one of <paramref name="toppedUpStacks"/> and <paramref name="movedItems"/> is 0.</exception>
    public static string Summary(int toppedUpStacks, int movedItems, int leftBehind, bool stopped)
    {
        if (toppedUpStacks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toppedUpStacks), "Must not be negative.");
        }

        if (movedItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movedItems), "Must not be negative.");
        }

        if (leftBehind < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leftBehind), "Must not be negative.");
        }

        if ((toppedUpStacks == 0) != (movedItems == 0))
        {
            throw new ArgumentException("Topped-up stacks and moved items must both be 0 or both be above 0.", nameof(toppedUpStacks));
        }

        StringTable table = StringTable.English;
        string head;
        if (movedItems > 0)
        {
            head = table.Format(
                StringKeys.MessageRestockMoved + "." + Plural.Form(toppedUpStacks) + "." + Plural.Form(movedItems),
                toppedUpStacks,
                movedItems);
        }
        else
        {
            head = table.Get(leftBehind > 0 || stopped ? StringKeys.MessageRestockNothingMoved : StringKeys.MessageRestockNothing);
        }

        var parts = new List<string> { head };
        if (leftBehind > 0)
        {
            parts.Add(table.FormatPlural(StringKeys.MessageRestockLeftBehind, leftBehind, leftBehind));
        }

        if (stopped)
        {
            parts.Add(table.Get(StringKeys.MessageRestockStopped));
        }

        return SingleLine.Of(string.Join(table.Get(StringKeys.MessageOrganizeSummaryJoin), parts));
    }

    /// <summary>Composes the one result line of a result.</summary>
    /// <param name="result">The run result; not null.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static string Summary(RestockRunResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return Summary(result.ToppedUpStacks, result.MovedItems, result.LeftBehind, result.Status == ExecutionStatus.Stopped);
    }
}
