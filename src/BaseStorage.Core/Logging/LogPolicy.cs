namespace BaseStorage.Core.Logging;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>Maps log categories to severities and writes categorised messages. Stateless, never throws.</summary>
public static class LogPolicy
{
    /// <summary>Gets the severity a message of this category is written at. Undefined categories map to Debug.</summary>
    /// <param name="category">The category to classify.</param>
    /// <returns>Info for Load, ConfigSync, OrganizeSummary and DepositSummary; Debug for every other value.</returns>
    public static LogSeverity SeverityOf(LogCategory category)
    {
        switch (category)
        {
            case LogCategory.Load:
            case LogCategory.ConfigSync:
            case LogCategory.OrganizeSummary:
            case LogCategory.DepositSummary:
                return LogSeverity.Info;
            default:
                return LogSeverity.Debug;
        }
    }

    /// <summary>Determines whether the sink would write a message of this category.</summary>
    /// <param name="sink">The sink to ask; null yields false.</param>
    /// <param name="category">The category of the message.</param>
    /// <returns>True when the sink reports the mapped severity as enabled; false for a null or throwing sink.</returns>
    public static bool IsEnabled(ILogSink? sink, LogCategory category)
    {
        if (sink is null)
        {
            return false;
        }

        try
        {
            return sink.IsEnabled(SeverityOf(category));
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Writes one message of this category. Does not check enablement; the sink decides.</summary>
    /// <param name="sink">The sink to write to; null is a no-op.</param>
    /// <param name="category">The category of the message.</param>
    /// <param name="message">The message text; null is written as an empty string.</param>
    public static void Write(ILogSink? sink, LogCategory category, string? message)
    {
        if (sink is null)
        {
            return;
        }

        try
        {
            sink.Log(SeverityOf(category), message ?? string.Empty);
        }
        catch (Exception)
        {
            // Logging must never fail the caller.
        }
    }
}
