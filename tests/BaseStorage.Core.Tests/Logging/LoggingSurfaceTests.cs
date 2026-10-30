namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// AC-3 surface (log-policy-contract.md v1, sections 2-4, L6, L8): the exported types and members of BaseStorage.Core.Logging
/// are exactly the contract ones. Checked by reflection, because the types do not compile into this project yet (ADR-GWIN-023).
/// </summary>
public sealed class LoggingSurfaceTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-212
    [Fact]
    public void ExportedTypesInTheLoggingNamespaceAreExactlyTheThreeContractTypes()
    {
        string[] names = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == LogPolicyApi.Namespace)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "DebugGatedLogSink", "LogCategory", "LogPolicy" }, names);
    }

    // GW-U-212
    [Fact]
    public void LogPolicyIsAStaticClassWithExactlyTheContractMethodsAndNullableParameters()
    {
        Type policy = LogPolicyApi.Find("LogPolicy");
        Assert.True(policy.IsAbstract && policy.IsSealed, "LogPolicy must be a static class.");

        string[] signatures = policy.GetMethods(DeclaredMembers)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ") : " + m.ReturnType.Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "IsEnabled(ILogSink,LogCategory) : Boolean",
                "SeverityOf(LogCategory) : LogSeverity",
                "Write(ILogSink,LogCategory,String) : Void",
            },
            signatures);

        var context = new NullabilityInfoContext();
        MethodInfo isEnabled = Method(policy, "IsEnabled");
        MethodInfo write = Method(policy, "Write");
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(isEnabled, "sink")).WriteState);
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(write, "sink")).WriteState);
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(write, "message")).WriteState);
    }

    // GW-U-212
    [Fact]
    public void DebugGatedLogSinkIsSealedImplementsILogSinkAndExposesOnlyTheContractMembers()
    {
        Type gate = LogPolicyApi.Find("DebugGatedLogSink");
        Assert.True(gate.IsSealed, "DebugGatedLogSink must be sealed.");
        Assert.True(typeof(ILogSink).IsAssignableFrom(gate), "DebugGatedLogSink must implement ILogSink.");

        ConstructorInfo[] constructors = gate.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Single(constructors);
        Assert.Equal(
            new[] { typeof(ILogSink), typeof(Func<bool>) },
            constructors[0].GetParameters().Select(p => p.ParameterType).ToArray());

        string[] instanceMethods = gate.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "IsEnabled", "Log" }, instanceMethods);

        FieldInfo[] fields = gate.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(2, fields.Length);
        Assert.All(fields, f => Assert.True(f.IsInitOnly, f.Name + " must be readonly."));
    }

    private static MethodInfo Method(Type type, string name)
    {
        MethodInfo? method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return method!;
    }

    private static ParameterInfo Param(MethodInfo method, string name)
    {
        ParameterInfo? parameter = method.GetParameters().SingleOrDefault(p => p.Name == name);
        Assert.NotNull(parameter);
        return parameter!;
    }
}
