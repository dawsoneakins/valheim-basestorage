namespace BaseStorage.Core.Tests.Diagnostics;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;

/// <summary>Thread-safe recording sink for the concurrency test. Not a test row (ADR-GWIN-021).</summary>
internal sealed class LockingRecordingLogSink : ILogSink
{
    private readonly object gate = new();
    private readonly List<(LogSeverity Severity, string Message)> entries = new();

    public bool DebugEnabled { get; set; }

    public IReadOnlyList<(LogSeverity Severity, string Message)> Entries
    {
        get
        {
            lock (gate)
            {
                return entries.ToArray();
            }
        }
    }

    public bool IsEnabled(LogSeverity severity)
    {
        lock (gate)
        {
            return severity != LogSeverity.Debug || DebugEnabled;
        }
    }

    public void Log(LogSeverity severity, string message)
    {
        lock (gate)
        {
            entries.Add((severity, message));
        }
    }
}

/// <summary>A sink whose every member throws. The reporter must still never throw (GW-U-128).</summary>
internal sealed class ThrowingLogSink : ILogSink
{
    public bool IsEnabled(LogSeverity severity) => throw new InvalidOperationException("sink is broken");

    public void Log(LogSeverity severity, string message) => throw new InvalidOperationException("sink is broken");
}

/// <summary>An exception whose ToString override throws. The Error line must still be written (GW-U-128).</summary>
internal sealed class ToStringThrowingException : Exception
{
    public ToStringThrowingException(string message)
        : base(message)
    {
    }

    public override string ToString() => throw new InvalidOperationException("ToString failed");
}
