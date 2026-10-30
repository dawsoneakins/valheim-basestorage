namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and contract 3.1: CategoryAssignment exposure, name validation and value equality.</summary>
public sealed class CategoryAssignmentTests
{
    private const string TypeName = "CategoryAssignment";

    // GW-U-069
    [Fact]
    public void KeepIsKeepAndHasNoName()
    {
        _ = PersistenceContract.Resolve(TypeName);
        object keep = PersistenceContract.Keep();

        Assert.True(PersistenceContract.Flag(keep, "IsKeep"));
        Assert.Null(PersistenceContract.Text(keep, "CategoryName"));
    }

    // GW-U-069
    [Theory]
    [InlineData("Metals & Ores")]
    [InlineData("Keep")]
    [InlineData("Wood: Fine")]
    [InlineData("Fårskinn")]
    [InlineData(" Wood ")]
    [InlineData("a;b,c")]
    public void ForCategoryKeepsTheNameVerbatim(string name)
    {
        object value = PersistenceContract.CategoryOf(name);

        Assert.False(PersistenceContract.Flag(value, "IsKeep"));
        Assert.Equal(name, PersistenceContract.Text(value, "CategoryName"));
    }

    // GW-U-069
    [Fact]
    public void HasNoPublicConstructor()
    {
        Type type = PersistenceContract.Resolve(TypeName);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // GW-U-069
    [Fact]
    public void NullNameIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { PersistenceContract.CategoryOf(null!); });
        Assert.Equal("categoryName", ex.ParamName);
    }

    // GW-U-069
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" ")]
    [InlineData("Raw\nFood")]
    [InlineData("\t")]
    [InlineData("A\u0000")]
    [InlineData("A\u007F")]
    [InlineData("A\u0085")]
    [InlineData("A\u009F")]
    public void BlankOrControlCharacterNameIsAnArgumentException(string name)
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => { PersistenceContract.CategoryOf(name); });
        Assert.Equal("categoryName", ex.ParamName);
    }

    // GW-U-070
    [Fact]
    public void KeepEqualsKeepWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(PersistenceContract.Keep(), PersistenceContract.Keep());
    }

    // GW-U-070
    [Fact]
    public void EqualNamesAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(
            PersistenceContract.CategoryOf("Metals & Ores"),
            PersistenceContract.CategoryOf("Metals & Ores"));
    }

    // GW-U-070
    [Fact]
    public void CaseChangesAndTheKeepLiteralAreUnequal()
    {
        ValueSemantics.AssertNotEqual(PersistenceContract.CategoryOf("Metals & Ores"), PersistenceContract.CategoryOf("metals & ores"));
        ValueSemantics.AssertNotEqual(PersistenceContract.CategoryOf("Keep"), PersistenceContract.Keep());
    }

    // GW-U-070
    [Fact]
    public void NullHandlingFollowsR5()
    {
        Type type = PersistenceContract.Resolve(TypeName);

        ValueSemantics.AssertNullHandling(type, PersistenceContract.Keep());
        ValueSemantics.AssertNullHandling(type, PersistenceContract.CategoryOf("Wood"));
    }
}
