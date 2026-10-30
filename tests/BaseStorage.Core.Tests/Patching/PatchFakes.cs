namespace BaseStorage.Core.Tests.Patching;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Patching;

/// <summary>Records every Log and IsEnabled call. Never throws. Not a test row (ADR-GWIN-013).</summary>
internal sealed class RecordingLogSink : ILogSink
{
    public bool DebugEnabled { get; set; }

    /// <summary>When true, IsEnabled returns false for every severity.</summary>
    public bool DisableAll { get; set; }

    public List<(LogSeverity Severity, string Message)> Entries { get; } = new();

    public List<LogSeverity> IsEnabledCalls { get; } = new();

    public bool IsEnabled(LogSeverity severity)
    {
        IsEnabledCalls.Add(severity);
        return !DisableAll && (severity != LogSeverity.Debug || DebugEnabled);
    }

    public void Log(LogSeverity severity, string message) => Entries.Add((severity, message));
}

/// <summary>Counts Apply calls and throws the configured exception, if any.</summary>
internal sealed class FakePatch : IPatchUnit
{
    public FakePatch(string name, Exception? throws = null)
    {
        Name = name;
        Throws = throws;
    }

    public string Name { get; }

    public Exception? Throws { get; }

    public int ApplyCalls { get; private set; }

    public void Apply()
    {
        ApplyCalls++;
        if (Throws is not null)
        {
            throw Throws;
        }
    }
}

/// <summary>A patch whose Name getter throws the configured exception. Apply must not be reached.</summary>
internal sealed class ThrowingNamePatch : IPatchUnit
{
    private readonly Exception nameError;

    public ThrowingNamePatch(Exception nameError)
    {
        this.nameError = nameError;
    }

    public string Name => throw nameError;

    public int ApplyCalls { get; private set; }

    public void Apply() => ApplyCalls++;
}

/// <summary>A patch whose Name getter returns the given value (possibly null) and counts its reads.</summary>
internal sealed class CountingNamePatch : IPatchUnit
{
    private readonly string? name;

    public CountingNamePatch(string? name)
    {
        this.name = name;
    }

    public int NameReads { get; private set; }

    public int ApplyCalls { get; private set; }

    public string Name
    {
        get
        {
            NameReads++;
            return name!;
        }
    }

    public void Apply() => ApplyCalls++;
}

/// <summary>Appends its name to a shared call order when applied.</summary>
internal sealed class OrderedPatch : IPatchUnit
{
    private readonly List<string> callOrder;

    public OrderedPatch(string name, List<string> callOrder)
    {
        Name = name;
        this.callOrder = callOrder;
    }

    public string Name { get; }

    public void Apply() => callOrder.Add(Name);
}

/// <summary>An exception type that the apply loop must catch like any other.</summary>
internal sealed class CustomPatchException : Exception
{
    public CustomPatchException(string message)
        : base(message)
    {
    }
}

/// <summary>An exception whose Message getter throws. The log must still be written, without the message.</summary>
internal sealed class MessageThrowingException : Exception
{
    public override string Message => throw new InvalidOperationException("message getter failed");
}
