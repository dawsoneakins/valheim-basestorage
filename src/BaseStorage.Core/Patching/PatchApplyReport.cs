namespace BaseStorage.Core.Patching;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Outcome of one ApplyAll call.</summary>
public sealed class PatchApplyReport
{
    /// <summary>Creates a report. Instances are created only by PatchApplier.</summary>
    /// <param name="results">One result per input element, in input order.</param>
    internal PatchApplyReport(PatchApplyResult[] results)
    {
        Results = new ReadOnlyCollection<PatchApplyResult>(results);
        int applied = 0;
        foreach (PatchApplyResult result in results)
        {
            if (result.Succeeded)
            {
                applied++;
            }
        }

        AppliedCount = applied;
        FailedCount = results.Length - applied;
    }

    /// <summary>One result per input element, in input order.</summary>
    public IReadOnlyList<PatchApplyResult> Results { get; }

    /// <summary>Number of results that succeeded.</summary>
    public int AppliedCount { get; }

    /// <summary>Number of results that failed.</summary>
    public int FailedCount { get; }
}
