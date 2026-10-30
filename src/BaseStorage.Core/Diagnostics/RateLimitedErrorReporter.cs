namespace BaseStorage.Core.Diagnostics;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Patching;

/// <summary>
/// Reports a failing patch once per patch key per session: the first failure is written at Error, its stack trace at Debug
/// (when Debug is enabled), and every repeat is dropped. Never throws, and is safe to call from several threads.
/// </summary>
public sealed class RateLimitedErrorReporter
{
    private readonly ILogSink log;
    private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
    private readonly object gate = new object();

    /// <summary>Creates a reporter that writes through the given sink.</summary>
    /// <param name="log">The sink that receives the Error and Debug lines. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="log"/> is null.</exception>
    public RateLimitedErrorReporter(ILogSink log)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>Reports one failure of a patch. Only the first call for a key writes anything.</summary>
    /// <param name="patchKey">The patch key, normally the patch class full name and method name. Null or blank shows as an unnamed patch.</param>
    /// <param name="error">The exception that the patch threw. A null value writes nothing and does not mark the key.</param>
    /// <returns>True only when this call wrote the Error line for the key; otherwise false.</returns>
    public bool Report(string? patchKey, Exception? error)
    {
        if (error is null)
        {
            return false;
        }

        try
        {
            string display = SingleLineText.Of(patchKey);
            if (display.Length == 0)
            {
                display = "<unnamed patch>";
            }

            bool isNew;
            lock (gate)
            {
                isNew = reported.Add(display);
            }

            if (!isNew)
            {
                return false;
            }

            log.Log(
                LogSeverity.Error,
                "Patch '" + display + "' failed and was skipped: " + SingleLineText.DescribeWithInner(error) + " (repeats suppressed this session)");
            if (LogPolicy.IsEnabled(log, LogCategory.SafetyStackTrace))
            {
                LogPolicy.Write(log, LogCategory.SafetyStackTrace, "Patch '" + display + "' stack trace: " + StackText(error));
            }

            return true;
        }
        catch (Exception)
        {
            // Reporting must never throw into the game.
            return false;
        }
    }

    /// <summary>Forgets every reported key, so the next failure of each patch is logged again.</summary>
    public void ResetSession()
    {
        lock (gate)
        {
            reported.Clear();
        }
    }

    private static string StackText(Exception error)
    {
        try
        {
            return SingleLineText.Of(error.ToString());
        }
        catch (Exception)
        {
            try
            {
                string? trace = error.StackTrace;
                return trace is null ? "<unavailable>" : SingleLineText.Of(trace);
            }
            catch (Exception)
            {
                return "<unavailable>";
            }
        }
    }
}
