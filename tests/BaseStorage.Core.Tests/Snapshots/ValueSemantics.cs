namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Reflection;

/// <summary>
/// Value-semantics assertions for the snapshot and id types (ADR-GWIN-008). They work on boxed values through
/// reflection, so they compile before the contract types exist. Each one checks Equals(object), the IEquatable
/// Equals(T), GetHashCode and the == and != operators.
/// </summary>
public static class ValueSemantics
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Equal values agree on every equality path and have equal hash codes.</summary>
    public static void AssertEqual(object left, object right)
    {
        Type type = left.GetType();
        Assert.True(left.Equals(right), $"{type.Name}: Equals(object) must be true for equal values.");
        Assert.True(right.Equals(left), $"{type.Name}: Equals(object) must be symmetric.");
        Assert.True(TypedEquals(left, right), $"{type.Name}: IEquatable<T>.Equals must be true for equal values.");
        Assert.True(TypedEquals(right, left), $"{type.Name}: IEquatable<T>.Equals must be symmetric.");
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.True(ContractTypes.OpEqual(type, left, right), $"{type.Name}: operator == must be true for equal values.");
        Assert.False(ContractTypes.OpNotEqual(type, left, right), $"{type.Name}: operator != must be false for equal values.");
    }

    /// <summary>Unequal values disagree on every equality path. Hash codes are deliberately not compared.</summary>
    public static void AssertNotEqual(object left, object right)
    {
        Type type = left.GetType();
        Assert.False(left.Equals(right), $"{type.Name}: Equals(object) must be false for unequal values.");
        Assert.False(right.Equals(left), $"{type.Name}: Equals(object) must be symmetric for unequal values.");
        Assert.False(TypedEquals(left, right), $"{type.Name}: IEquatable<T>.Equals must be false for unequal values.");
        Assert.False(TypedEquals(right, left), $"{type.Name}: IEquatable<T>.Equals must be symmetric for unequal values.");
        Assert.False(ContractTypes.OpEqual(type, left, right), $"{type.Name}: operator == must be false for unequal values.");
        Assert.True(ContractTypes.OpNotEqual(type, left, right), $"{type.Name}: operator != must be true for unequal values.");
    }

    /// <summary>Class null handling (contract R5): == and != handle null on either side, and Equals(null) is false.</summary>
    public static void AssertNullHandling(Type type, object value)
    {
        Assert.False(ContractTypes.OpEqual(type, value, null), "value == null must be false.");
        Assert.False(ContractTypes.OpEqual(type, null, value), "null == value must be false.");
        Assert.True(ContractTypes.OpNotEqual(type, value, null), "value != null must be true.");
        Assert.True(ContractTypes.OpNotEqual(type, null, value), "null != value must be true.");
        Assert.False(TypedEquals(value, null), "IEquatable<T>.Equals(null) must be false.");
        Assert.False(value.Equals(null), "Equals((object?)null) must be false.");
        Assert.True(ContractTypes.OpEqual(type, null, null), "null == null must be true.");
        Assert.False(ContractTypes.OpNotEqual(type, null, null), "null != null must be false.");
    }

    private static bool TypedEquals(object left, object? right)
    {
        Type type = left.GetType();
        MethodInfo equals = ContractTypes.Method(type, "Equals", PublicInstance, type);
        bool result = (bool)ContractTypes.Invoke(equals, left, new[] { right })!;
        return result;
    }
}
