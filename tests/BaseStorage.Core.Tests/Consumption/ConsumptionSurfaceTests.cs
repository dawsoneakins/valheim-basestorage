namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using Xunit;

/// <summary>AC-3 (contract 1, 2.1 to 2.4): the exported shape of the Consumption namespace, checked by reflection.</summary>
public sealed class ConsumptionSurfaceTests
{
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-529 (AC-3, contract 1): the namespace exports exactly the four contracted types.
    [Fact]
    public void TheNamespaceExportsExactlyTheFourContractedTypes()
    {
        string[] names = typeof(ConsumptionExecutor).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "BaseStorage.Core.Consumption")
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "ConsumptionExecution", "ConsumptionExecutor", "ConsumptionOutcome", "ConsumptionResult" }, names);
    }

    // GW-U-529 (AC-3, 2.1): the outcome names and values are contracted, with an int underlying type.
    [Fact]
    public void TheOutcomeHasTheContractedNamesAndValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ConsumptionOutcome)));
        (string Name, int Value)[] pairs = Enum.GetValues(typeof(ConsumptionOutcome))
            .Cast<ConsumptionOutcome>()
            .Select(value => (value.ToString(), (int)value))
            .ToArray();

        Assert.Equal(
            new[] { ("Success", 0), ("ReservationFailed", 1), ("ContentsChanged", 2), ("TakeFailed", 3), ("Cancelled", 4), ("Faulted", 5) },
            pairs);
    }

    // GW-U-529 (AC-3, 2.2 to 2.4): the three classes are sealed, and the two produced types have no public constructor.
    [Fact]
    public void TheClassesAreSealedAndTheProducedTypesHaveNoPublicConstructor()
    {
        Assert.True(typeof(ConsumptionExecutor).IsSealed);
        Assert.True(typeof(ConsumptionExecution).IsSealed);
        Assert.True(typeof(ConsumptionResult).IsSealed);
        Assert.Empty(typeof(ConsumptionExecution).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(typeof(ConsumptionResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // GW-U-529 (AC-3, 2.2): the executor has one public constructor over (layer, clock, settings) and one public method, Execute(plan).
    [Fact]
    public void TheExecutorHasTheContractedConstructorAndExecute()
    {
        ConstructorInfo[] ctors = typeof(ConsumptionExecutor).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Single(ctors);
        Assert.Equal(
            new[] { typeof(IContainerLayer), typeof(IClock), typeof(BaseStorageSettings) },
            ctors[0].GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(new[] { "Execute" }, MethodNames(typeof(ConsumptionExecutor)));
        Assert.Empty(PropertyNames(typeof(ConsumptionExecutor)));

        MethodInfo execute = typeof(ConsumptionExecutor).GetMethod("Execute", PublicDeclared)!;
        Assert.Equal(typeof(ConsumptionExecution), execute.ReturnType);
        Assert.Equal(new[] { typeof(ConsumptionPlan) }, execute.GetParameters().Select(parameter => parameter.ParameterType));
    }

    // GW-U-529 (AC-3, 2.3): the execution has exactly the contracted members, and Poll returns a nullable result.
    [Fact]
    public void ExecutionHasTheContractedMembers()
    {
        Type type = typeof(ConsumptionExecution);

        Assert.Equal(new[] { "IsComplete", "Plan", "Result" }, PropertyNames(type));
        Assert.Equal(new[] { "Cancel", "Poll" }, MethodNames(type));
        Assert.Equal(typeof(ConsumptionPlan), type.GetProperty("Plan", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(bool), type.GetProperty("IsComplete", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(ConsumptionResult), type.GetProperty("Result", PublicDeclared)!.PropertyType);
        Assert.Equal(NullabilityState.Nullable, ReadState(type.GetProperty("Result", PublicDeclared)!));
        Assert.Equal(typeof(ConsumptionResult), type.GetMethod("Poll", PublicDeclared)!.ReturnType);
        Assert.Equal(NullabilityState.Nullable, ReadState(type.GetMethod("Poll", PublicDeclared)!.ReturnParameter));
        Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
    }

    // GW-U-529 (AC-3, 2.4): the result has the contracted members, types and nullability, is equatable, and its properties are get-only.
    [Fact]
    public void ResultHasTheContractedSurface()
    {
        Type type = typeof(ConsumptionResult);

        Assert.Contains(typeof(IEquatable<ConsumptionResult>), type.GetInterfaces());
        Assert.Equal(
            new[] { "FailedContainer", "FailedOperation", "IsSuccess", "MessageKey", "Outcome", "ReservationFailure", "Unrestored" },
            PropertyNames(type));
        Assert.Equal(new[] { "Equals", "Equals", "GetHashCode", "MessageKeyFor", "op_Equality", "op_Inequality" }, MethodNames(type));
        Assert.Equal(typeof(ConsumptionOutcome), type.GetProperty("Outcome", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(bool), type.GetProperty("IsSuccess", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(ContainerId), type.GetProperty("FailedContainer", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(ReservationFailure), type.GetProperty("ReservationFailure", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(ContainerOperationResult?), type.GetProperty("FailedOperation", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<ItemStackSnapshot>), type.GetProperty("Unrestored", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty("MessageKey", PublicDeclared)!.PropertyType);
        Assert.Equal(NullabilityState.Nullable, ReadState(type.GetProperty("MessageKey", PublicDeclared)!));
        Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));

        MethodInfo messageKeyFor = type.GetMethod("MessageKeyFor", PublicDeclared)!;
        Assert.True(messageKeyFor.IsStatic);
        Assert.Equal(new[] { typeof(ConsumptionOutcome) }, messageKeyFor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(NullabilityState.Nullable, ReadState(messageKeyFor.ReturnParameter));
    }

    private static string[] PropertyNames(Type type) =>
        type.GetProperties(PublicDeclared).Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private static string[] MethodNames(Type type) =>
        type.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static NullabilityState ReadState(PropertyInfo property) => new NullabilityInfoContext().Create(property).ReadState;

    private static NullabilityState ReadState(ParameterInfo parameter) => new NullabilityInfoContext().Create(parameter).ReadState;
}
