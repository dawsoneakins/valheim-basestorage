namespace BaseStorage.Core.Tests.StationActions;

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

/// <summary>
/// Reflection access to the game-free station-actions types of S-E004-012 (ADR-GWIN-136). Their sources are linked into this
/// assembly only at implement, so the tests reach them by full name. Until then <see cref="Require"/> fails the calling test with
/// a red reason, and the test project still compiles and every other test runs. Not a test class.
/// </summary>
internal static class StationActionsTypes
{
    /// <summary>The namespace of the station-actions files.</summary>
    internal const string Namespace = "BaseStorage.Plugin.StationActions.";

    /// <summary>Returns the type by its short name, or fails the calling test while the type does not exist.</summary>
    /// <param name="name">The short type name.</param>
    /// <returns>The type.</returns>
    internal static Type Require(string name)
    {
        Type? type = typeof(StationActionsTypes).Assembly.GetType(Namespace + name, throwOnError: false);
        Assert.True(type is not null, Namespace + name + " does not exist yet (S-E004-012, implement phase).");
        return type!;
    }

    /// <summary>Calls a static method by name.</summary>
    /// <param name="typeName">The short type name.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The arguments, in order.</param>
    /// <returns>The return value, or null for void.</returns>
    internal static object? CallStatic(string typeName, string method, params object?[] args) =>
        Invoke(Method(Require(typeName), method), null, args);

    /// <summary>Calls StationActionsFacts.FillPercent (S-E010-001, station-actions contract 12.1) through reflection. Red until that member exists.</summary>
    /// <param name="resolution">The base resolution.</param>
    /// <param name="assignments">The category assignments, not null.</param>
    /// <param name="options">The eligibility options.</param>
    /// <returns>The fill percentage, 0 to 100.</returns>
    internal static int FillPercent(object? resolution, object? assignments, object? options) =>
        (int)CallStatic("StationActionsFacts", "FillPercent", resolution, assignments, options)!;

    /// <summary>Calls an instance method by name.</summary>
    /// <param name="target">The instance.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The arguments, in order.</param>
    /// <returns>The return value, or null for void.</returns>
    internal static object? CallInstance(object target, string method, params object?[] args) =>
        Invoke(Method(target.GetType(), method), target, args);

    /// <summary>Reads a constant or a static field by name.</summary>
    /// <param name="typeName">The short type name.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The value.</returns>
    internal static object? Constant(string typeName, string field)
    {
        Type type = Require(typeName);
        FieldInfo? info = type.GetField(field, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(info is not null, type.FullName + "." + field + " does not exist yet (S-E004-012, implement phase).");
        return info!.GetValue(null);
    }

    /// <summary>Binds a delegate to an instance method, so a measured call allocates nothing of its own.</summary>
    /// <typeparam name="TDelegate">The delegate type.</typeparam>
    /// <param name="target">The instance.</param>
    /// <param name="method">The method name.</param>
    /// <returns>The bound delegate.</returns>
    internal static TDelegate Bind<TDelegate>(object target, string method)
        where TDelegate : Delegate
    {
        MethodInfo info = Method(target.GetType(), method);
        return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), target, info);
    }

    /// <summary>Creates an instance of a station-actions type with its default constructor.</summary>
    /// <param name="typeName">The short type name.</param>
    /// <returns>The new instance.</returns>
    internal static object New(string typeName) => Activator.CreateInstance(Require(typeName), nonPublic: true)!;

    private static MethodInfo Method(Type type, string name)
    {
        MethodInfo? info = type.GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(info is not null, type.FullName + "." + name + " does not exist yet (S-E004-012, implement phase).");
        return info!;
    }

    private static object? Invoke(MethodInfo info, object? target, object?[] args)
    {
        try
        {
            return info.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
