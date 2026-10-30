namespace BaseStorage.Core.Patching;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Logging;

/// <summary>The per-patch apply loop.</summary>
public static class PatchApplier
{
    /// <summary>Applies every patch in order, logs each failure by name and continues. Never throws because of a patch.</summary>
    /// <param name="patches">The patches to apply, in order.</param>
    /// <param name="log">The sink that receives Debug and Error lines.</param>
    /// <returns>A report with one result per input element.</returns>
    public static PatchApplyReport ApplyAll(IReadOnlyList<IPatchUnit> patches, ILogSink log)
    {
        if (patches is null)
        {
            throw new ArgumentNullException(nameof(patches));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        int count = patches.Count;
        var results = new PatchApplyResult[count];
        for (int i = 0; i < count; i++)
        {
            string name = "<unnamed patch " + i.ToString(CultureInfo.InvariantCulture) + ">";
            Exception? error = null;
            try
            {
                IPatchUnit? unit = patches[i];
                if (unit is null)
                {
                    throw new ArgumentNullException(nameof(patches));
                }

                string? raw = unit.Name;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    name = SingleLineText.Of(raw);
                }

                unit.Apply();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error is null)
            {
                if (LogPolicy.IsEnabled(log, LogCategory.PatchDetail))
                {
                    LogPolicy.Write(log, LogCategory.PatchDetail, BootMessages.PatchApplied(name));
                }
            }
            else
            {
                log.Log(LogSeverity.Error, BootMessages.PatchFailed(name, error));
            }

            results[i] = new PatchApplyResult(name, i, error is null, error);
        }

        return new PatchApplyReport(results);
    }
}
