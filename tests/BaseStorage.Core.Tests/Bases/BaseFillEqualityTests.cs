namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Fill;

/// <summary>
/// GW-U-2248 (R3 to R5, base-fill contract 3.2): value equality of BaseFill, the readonly struct shape and IEquatable. Compile-red until
/// BaseFill exists.
/// </summary>
public sealed class BaseFillEqualityTests
{
    // GW-U-2248 (3.2): equal counts are equal with equal hash codes.
    [Fact]
    public void EqualCountsAreEqualWithEqualHashCodes()
    {
        BaseFill a = new BaseFill(3, 10);
        BaseFill b = new BaseFill(3, 10);

        Assert.True(a == b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // GW-U-2248 (3.2): a different total is not equal.
    [Fact]
    public void DifferentTotalsAreNotEqual()
    {
        Assert.True(new BaseFill(3, 10) != new BaseFill(3, 11));
        Assert.False(new BaseFill(3, 10) == new BaseFill(3, 11));
    }

    // GW-U-2248 (3.2): the same Percent of 33 with different counts is not equal, because both counts are compared.
    [Fact]
    public void TheSamePercentWithDifferentCountsIsNotEqual()
    {
        Assert.True(new BaseFill(1, 3) != new BaseFill(2, 6));
        Assert.False(new BaseFill(1, 3) == new BaseFill(2, 6));
        Assert.Equal(new BaseFill(1, 3).Percent, new BaseFill(2, 6).Percent);
    }

    // GW-U-2248 (3.2): Equals(object) is true for a boxed equal value, false for null and false for another type.
    [Fact]
    public void EqualsObjectIsTrueOnlyForABoxedEqualValue()
    {
        object boxed = new BaseFill(3, 10);

        Assert.True(new BaseFill(3, 10).Equals(boxed));
        Assert.False(new BaseFill(3, 10).Equals((object?)null));
        Assert.False(new BaseFill(3, 10).Equals((object)"3"));
    }

    // GW-U-2248 (R4, R5): == and != agree with Equals for an equal pair and for an unequal pair.
    [Fact]
    public void OperatorsAgreeWithEquals()
    {
        BaseFill x = new BaseFill(3, 10);
        BaseFill same = new BaseFill(3, 10);
        BaseFill other = new BaseFill(3, 11);

        Assert.Equal(x.Equals(same), x == same);
        Assert.Equal(!x.Equals(same), x != same);
        Assert.Equal(x.Equals(other), x == other);
        Assert.Equal(!x.Equals(other), x != other);
    }

    // GW-U-2248 (R3, 3.2): BaseFill is a readonly struct that implements IEquatable<BaseFill>.
    [Fact]
    public void BaseFillIsAReadOnlyStructThatImplementsIEquatable()
    {
        Assert.True(typeof(BaseFill).IsValueType);
        Assert.Contains(typeof(BaseFill).GetCustomAttributesData(), a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
        Assert.True(typeof(IEquatable<BaseFill>).IsAssignableFrom(typeof(BaseFill)));
    }
}
