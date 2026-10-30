namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BaseStorage.Core.Abstractions;

/// <summary>
/// Records every Log call, every IsEnabled argument and every attempted Log call. Can throw on demand.
/// Not a test row (ADR-GWIN-023).
/// </summary>
internal sealed class RecordingSink : ILogSink
{
    /// <summary>Decides IsEnabled per severity. The default enables everything.</summary>
    public Func<LogSeverity, bool> Enabled { get; set; } = _ => true;

    /// <summary>When true, Log throws after it is counted.</summary>
    public bool ThrowOnLog { get; set; }

    /// <summary>When true, IsEnabled throws after it is counted.</summary>
    public bool ThrowOnIsEnabled { get; set; }

    /// <summary>The messages that were written, in call order.</summary>
    public List<(LogSeverity Severity, string Message)> Entries { get; } = new List<(LogSeverity Severity, string Message)>();

    /// <summary>Every severity passed to IsEnabled, in call order.</summary>
    public List<LogSeverity> IsEnabledCalls { get; } = new List<LogSeverity>();

    /// <summary>The number of Log calls, including the ones that threw.</summary>
    public int LogCalls { get; private set; }

    /// <inheritdoc />
    public bool IsEnabled(LogSeverity severity)
    {
        IsEnabledCalls.Add(severity);
        if (ThrowOnIsEnabled)
        {
            throw new InvalidOperationException("sink IsEnabled failed");
        }

        return Enabled(severity);
    }

    /// <inheritdoc />
    public void Log(LogSeverity severity, string message)
    {
        LogCalls++;
        if (ThrowOnLog)
        {
            throw new InvalidOperationException("sink Log failed");
        }

        Entries.Add((severity, message));
    }
}

/// <summary>
/// A Debug enablement accessor that counts its invocations and can throw. Not a test row (ADR-GWIN-023).
/// </summary>
internal sealed class CountingAccessor
{
    /// <summary>Creates an accessor that returns the given value.</summary>
    /// <param name="value">The value returned on each call.</param>
    public CountingAccessor(bool value = false)
    {
        Value = value;
        AsFunc = Read;
    }

    /// <summary>The value returned on each call.</summary>
    public bool Value { get; set; }

    /// <summary>The number of invocations so far, including the ones that threw.</summary>
    public int Calls { get; private set; }

    /// <summary>When set, every call throws this exception after it is counted.</summary>
    public Exception? Throws { get; set; }

    /// <summary>The accessor as a delegate, for the gate constructor.</summary>
    public Func<bool> AsFunc { get; }

    private bool Read()
    {
        Calls++;
        if (Throws is not null)
        {
            throw Throws;
        }

        return Value;
    }
}

/// <summary>
/// The contract category values (log-policy-contract.md v1, section 2), as plain integers so that the out-of-range rows
/// can sit beside the defined ones. Not a test row.
/// </summary>
internal static class Cat
{
    /// <summary>Category 0.</summary>
    public const int Load = 0;

    /// <summary>Category 1.</summary>
    public const int ConfigSync = 1;

    /// <summary>Category 2.</summary>
    public const int OrganizeSummary = 2;

    /// <summary>Category 3.</summary>
    public const int DepositSummary = 3;

    /// <summary>Category 4.</summary>
    public const int MoveDetail = 4;

    /// <summary>Category 5.</summary>
    public const int PatchDetail = 5;

    /// <summary>Category 6.</summary>
    public const int SafetyStackTrace = 6;

    /// <summary>Category 7.</summary>
    public const int UiFallback = 7;

    /// <summary>Category 8.</summary>
    public const int Detail = 8;
}

/// <summary>
/// Reflection access to the BaseStorage.Core.Logging types (log-policy-contract.md v1). The tests reach the types through
/// this facade and never name them at compile time, so the rest of the BaseStorage.Core.Tests project keeps building
/// before the types exist (ADR-GWIN-023 red-first, without stubs under src/). A missing type fails the calling test with
/// a clear message. Not a test row.
/// </summary>
internal static class LogPolicyApi
{
    /// <summary>The namespace the contract types live in.</summary>
    public const string Namespace = "BaseStorage.Core.Logging";

    /// <summary>The four categories that map to Info, in value order (contract 3.1).</summary>
    public static readonly string[] InfoCategoryNames = { "Load", "ConfigSync", "OrganizeSummary", "DepositSummary" };

    /// <summary>Finds a type in the Core assembly under <see cref="Namespace"/>. Fails the test when it is missing.</summary>
    /// <param name="name">The simple type name.</param>
    /// <returns>The type.</returns>
    public static Type Find(string name)
    {
        Type? type = typeof(ILogSink).Assembly.GetType(Namespace + "." + name, throwOnError: false);
        if (type is null)
        {
            throw new InvalidOperationException(
                Namespace + "." + name + " is not defined yet (log-policy-contract v1). The implement phase adds it.");
        }

        return type;
    }

    /// <summary>The LogCategory enum type.</summary>
    public static Type CategoryType => Find("LogCategory");

    /// <summary>Boxes an integer as a LogCategory value, including values outside the defined set.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The boxed LogCategory.</returns>
    public static object Category(int value) => Enum.ToObject(CategoryType, value);

    /// <summary>Calls LogPolicy.SeverityOf.</summary>
    /// <param name="category">The raw category value.</param>
    /// <returns>The severity the policy returns.</returns>
    public static LogSeverity SeverityOf(int category) =>
        (LogSeverity)Call("LogPolicy", "SeverityOf", Category(category))!;

    /// <summary>Calls LogPolicy.IsEnabled.</summary>
    /// <param name="sink">The sink, possibly null.</param>
    /// <param name="category">The raw category value.</param>
    /// <returns>The result of the policy call.</returns>
    public static bool IsEnabled(ILogSink? sink, int category) =>
        (bool)Call("LogPolicy", "IsEnabled", sink, Category(category))!;

    /// <summary>Calls LogPolicy.Write.</summary>
    /// <param name="sink">The sink, possibly null.</param>
    /// <param name="category">The raw category value.</param>
    /// <param name="message">The message, possibly null.</param>
    public static void Write(ILogSink? sink, int category, string? message) =>
        Call("LogPolicy", "Write", sink, Category(category), message);

    /// <summary>Builds a DebugGatedLogSink. Constructor exceptions reach the caller unwrapped.</summary>
    /// <param name="inner">The inner sink, possibly null.</param>
    /// <param name="debugEnabled">The accessor, possibly null.</param>
    /// <returns>The gate, as an ILogSink.</returns>
    public static ILogSink Gate(ILogSink? inner, Func<bool>? debugEnabled) =>
        (ILogSink)Construct("DebugGatedLogSink", inner, debugEnabled)!;

    private static object? Call(string typeName, string method, params object?[] args)
    {
        MethodInfo? info = Find(typeName).GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        if (info is null)
        {
            throw new InvalidOperationException(Namespace + "." + typeName + "." + method + " is not defined yet.");
        }

        return Unwrap(() => info.Invoke(null, args));
    }

    private static object? Construct(string typeName, params object?[] args)
    {
        Type type = Find(typeName);
        return Unwrap(() => Activator.CreateInstance(type, args));
    }

    private static object? Unwrap(Func<object?> invoke)
    {
        try
        {
            return invoke();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
