namespace BaseStorage.Core.Logging;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>Forwards to an inner sink and suppresses Debug unless a live accessor returns true. Never throws after construction.</summary>
public sealed class DebugGatedLogSink : ILogSink
{
    private readonly ILogSink inner;
    private readonly Func<bool> debugEnabled;

    /// <summary>Creates a gate over an inner sink.</summary>
    /// <param name="inner">The sink that receives every message that passes the gate.</param>
    /// <param name="debugEnabled">Read on every Debug check and Debug write; normally reads the live DebugLogging setting.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> or <paramref name="debugEnabled"/> is null.</exception>
    public DebugGatedLogSink(ILogSink inner, Func<bool> debugEnabled)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.debugEnabled = debugEnabled ?? throw new ArgumentNullException(nameof(debugEnabled));
    }

    /// <summary>Determines whether a message at this severity would be written. Debug also requires the live accessor to return true.</summary>
    /// <param name="severity">The severity to test.</param>
    /// <returns>True when the message would be written; false when gated off or when a collaborator throws.</returns>
    public bool IsEnabled(LogSeverity severity)
    {
        try
        {
            if (severity == LogSeverity.Debug)
            {
                return DebugOn() && inner.IsEnabled(LogSeverity.Debug);
            }

            return inner.IsEnabled(severity);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Writes a message to the inner sink. Debug messages are dropped unless the live accessor returns true.</summary>
    /// <param name="severity">The message severity, forwarded unchanged.</param>
    /// <param name="message">The message text; null is forwarded as an empty string.</param>
    public void Log(LogSeverity severity, string message)
    {
        try
        {
            if (severity == LogSeverity.Debug && !DebugOn())
            {
                return;
            }

            inner.Log(severity, message ?? string.Empty);
        }
        catch (Exception)
        {
            // Logging must never fail the caller.
        }
    }

    private bool DebugOn()
    {
        try
        {
            return debugEnabled();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
