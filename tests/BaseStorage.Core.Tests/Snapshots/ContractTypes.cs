namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

/// <summary>
/// Reflection access to the BaseStorage.Core contract of S-FOUND-002 (ADR-GWIN-008).
/// Contract types are resolved by name, so this project keeps compiling while they are missing.
/// Each test then fails at run time and names the missing type, and the existing suites keep running.
/// </summary>
public static class ContractTypes
{
    /// <summary>Namespace of the value types in contract section 2.</summary>
    public const string SnapshotNamespace = "BaseStorage.Core.Snapshots";

    /// <summary>Namespace of the interfaces and enums in contract section 3.</summary>
    public const string AbstractionsNamespace = "BaseStorage.Core.Abstractions";

    /// <summary>Every public type that S-FOUND-002 adds to BaseStorage.Core.</summary>
    public static readonly string[] SurfaceTypeNames =
    [
        "BaseStorage.Core.Abstractions.ContainerOperationResult",
        "BaseStorage.Core.Abstractions.IClock",
        "BaseStorage.Core.Abstractions.IContainerLayer",
        "BaseStorage.Core.Abstractions.ILogSink",
        "BaseStorage.Core.Abstractions.LogSeverity",
        "BaseStorage.Core.Abstractions.ReservationStatus",
        "BaseStorage.Core.Snapshots.ContainerFlags",
        "BaseStorage.Core.Snapshots.ContainerId",
        "BaseStorage.Core.Snapshots.ContainerSnapshot",
        "BaseStorage.Core.Snapshots.ItemStackSnapshot",
        "BaseStorage.Core.Snapshots.StationId",
        "BaseStorage.Core.Snapshots.StationSnapshot",
        "BaseStorage.Core.Snapshots.WorldPosition",
    ];

    /// <summary>The BaseStorage.Core assembly under test.</summary>
    public static Assembly CoreAssembly() => typeof(BaseStorage.Core.CoreAssemblyMarker).Assembly;

    /// <summary>Resolves a contract type by full name, or fails the calling test with the missing name.</summary>
    public static Type Require(string fullName)
    {
        Type? type = CoreAssembly().GetType(fullName, throwOnError: false);
        Assert.True(type is not null, $"Contract type {fullName} does not exist in BaseStorage.Core yet.");
        return type!;
    }

    /// <summary>Resolves a type in <see cref="SnapshotNamespace"/>.</summary>
    public static Type Snapshot(string simpleName) => Require(SnapshotNamespace + "." + simpleName);

    /// <summary>Resolves a type in <see cref="AbstractionsNamespace"/>.</summary>
    public static Type Abstraction(string simpleName) => Require(AbstractionsNamespace + "." + simpleName);

    /// <summary>
    /// Every exported type in the two contract namespaces. Asserts that the set is exactly
    /// <see cref="SurfaceTypeNames"/>, so a missing or an extra public type fails the calling test.
    /// </summary>
    public static IReadOnlyList<Type> SurfaceTypes()
    {
        Type[] exported = CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == SnapshotNamespace || t.Namespace == AbstractionsNamespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();
        string[] expected = SurfaceTypeNames.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, exported.Select(t => t.FullName ?? string.Empty).ToArray());
        return exported;
    }

    /// <summary>Constructs a contract value. Exceptions thrown by the constructor are rethrown unwrapped.</summary>
    public static object Make(Type type, params object?[] args)
    {
        try
        {
            return Activator.CreateInstance(type, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>The default value of a contract type, such as default(StationId).</summary>
    public static object Default(Type type) => Activator.CreateInstance(type)!;

    /// <summary>Invokes a method and rethrows the inner exception unwrapped.</summary>
    public static object? Invoke(MethodInfo method, object? target, object?[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>Finds a member method, or fails the calling test with its signature.</summary>
    public static MethodInfo Method(Type type, string name, BindingFlags flags, params Type[] parameterTypes)
    {
        MethodInfo? method = type.GetMethod(name, flags, null, parameterTypes, null);
        Assert.True(
            method is not null,
            $"{type.FullName} has no {name}({string.Join(", ", parameterTypes.Select(t => t.Name))}).");
        return method!;
    }

    /// <summary>Calls the public static == operator of a type with two (possibly null) operands.</summary>
    public static bool OpEqual(Type type, object? left, object? right) => CallOperator(type, "op_Equality", left, right);

    /// <summary>Calls the public static != operator of a type with two (possibly null) operands.</summary>
    public static bool OpNotEqual(Type type, object? left, object? right) => CallOperator(type, "op_Inequality", left, right);

    /// <summary>Builds a typed array, such as ItemStackSnapshot[], so it converts to IEnumerable of the element type.</summary>
    public static Array TypedArray(Type elementType, object?[] items)
    {
        Array array = Array.CreateInstance(elementType, items.Length);
        for (int i = 0; i < items.Length; i++)
        {
            array.SetValue(items[i], i);
        }

        return array;
    }

    /// <summary>Builds a List of the element type, for the "caller's own collection" cases.</summary>
    public static IList TypedList(Type elementType, object?[] items)
    {
        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (object? item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static bool CallOperator(Type type, string name, object? left, object? right)
    {
        MethodInfo op = Method(type, name, BindingFlags.Public | BindingFlags.Static, type, type);
        bool result = (bool)Invoke(op, null, new[] { left, right })!;
        return result;
    }
}

/// <summary>
/// Contract example values from contracts/core-contracts.md (section 2 fixtures). Every call builds a new instance.
/// </summary>
public static class Fixtures
{
    /// <summary>new StationId(userId, id).</summary>
    public static object StationIdOf(long userId, uint id) => Make("StationId", userId, id);

    /// <summary>new ContainerId(userId, id).</summary>
    public static object ContainerIdOf(long userId, uint id) => Make("ContainerId", userId, id);

    /// <summary>new WorldPosition(x, y, z).</summary>
    public static object PositionOf(float x, float y, float z) => Make("WorldPosition", x, y, z);

    /// <summary>new ItemStackSnapshot(prefabName, count, quality, maxStack).</summary>
    public static object StackOf(string? prefabName, int count, int quality, int maxStack) =>
        Make("ItemStackSnapshot", prefabName, count, quality, maxStack);

    /// <summary>new StationSnapshot(id, position, buildRange).</summary>
    public static object StationOf(object? id, object? position, float buildRange) =>
        Make("StationSnapshot", id, position, buildRange);

    /// <summary>new ContainerSnapshot(id, position, width, height, contents, flags) with flags given as raw bits.</summary>
    public static object ContainerOf(object? id, object? position, int width, int height, object? contents, int flags) =>
        Make("ContainerSnapshot", id, position, width, height, contents, FlagsOf(flags));

    /// <summary>A ContainerFlags value from raw bits. Undefined bits are kept, so validation can be tested.</summary>
    public static object FlagsOf(int bits) => Enum.ToObject(ContractTypes.Snapshot("ContainerFlags"), bits);

    /// <summary>An ItemStackSnapshot[] holding the given stacks, in order.</summary>
    public static Array StackArray(params object?[] stacks) =>
        ContractTypes.TypedArray(ContractTypes.Snapshot("ItemStackSnapshot"), stacks);

    /// <summary>A List of ItemStackSnapshot holding the given stacks, in order.</summary>
    public static IList StackList(params object?[] stacks) =>
        ContractTypes.TypedList(ContractTypes.Snapshot("ItemStackSnapshot"), stacks);

    /// <summary>new ItemStackSnapshot("CopperOre", 30, 1, 30).</summary>
    public static object Ore() => StackOf("CopperOre", 30, 1, 30);

    /// <summary>new ItemStackSnapshot("Wood", 17, 1, 50).</summary>
    public static object Wood() => StackOf("Wood", 17, 1, 50);

    /// <summary>{ ore, wood }, the contents of the chest fixture.</summary>
    public static Array OreAndWood() => StackArray(Ore(), Wood());

    /// <summary>new ContainerId(3141592653L, 42u), the id of the chest fixture.</summary>
    public static object ChestId() => ContainerIdOf(3141592653L, 42u);

    /// <summary>new WorldPosition(14f, 31.25f, -836.5f), the position of the chest fixture.</summary>
    public static object ChestPosition() => PositionOf(14f, 31.25f, -836.5f);

    /// <summary>The chest fixture "a" of contract section 2.6: 5x2, PlayerBuilt, holding a full ore stack and 17 wood.</summary>
    public static object ContainerA() => ChestOf(5, 2, OreAndWood(), 1);

    /// <summary>The chest fixture with the given size, contents and flags.</summary>
    public static object ChestOf(int width, int height, object? contents, int flags) =>
        ContainerOf(ChestId(), ChestPosition(), width, height, contents, flags);

    /// <summary>new StationSnapshot(StationId(3141592653L, 17u), WorldPosition(12.5f, 31.25f, -840.75f), 20f).</summary>
    public static object Workbench() =>
        StationOf(StationIdOf(3141592653L, 17u), PositionOf(12.5f, 31.25f, -840.75f), 20f);

    private static object Make(string simpleName, params object?[] args) =>
        ContractTypes.Make(ContractTypes.Snapshot(simpleName), args);
}
