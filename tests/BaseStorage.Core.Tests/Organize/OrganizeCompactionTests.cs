namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Globalization;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-553 and GW-U-555: the compaction table of contract 6.6 and the ordinal, culture-free order (AC-6).</summary>
public sealed class OrganizeCompactionTests
{
    // GW-U-553
    [Theory]
    [InlineData("601")]
    [InlineData("602")]
    [InlineData("603")]
    [InlineData("604")]
    [InlineData("605")]
    public void CompactedLayoutEqualsTheContractLiteral(string name)
    {
        uint chest = uint.Parse(name, CultureInfo.InvariantCulture);
        OrganizePlan plan = Run(Compaction(name));
        (string Category, ItemStackSnapshot[] Stacks) expected = ExpectedCompaction(name);

        Assert.Empty(plan.Moves);
        Assert.Equal(Layout(chest, ChestRole.Category, expected.Category, expected.Stacks), LayoutRowOf(plan.FindLayout(Id(chest))!));
    }

    // GW-U-555
    [Fact]
    public void UnknownPrefabsOrderOrdinallyWhereUpperCaseSortsBeforeLowerCase()
    {
        OrganizePlan plan = Run(MiscCase());

        Assert.Equal(
            Layout(1501, ChestRole.Category, "Misc", S("Bread", 2), S("apple", 1), S("berry", 3)),
            LayoutRowOf(plan.FindLayout(Id(1501))!));
    }

    // GW-U-555
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    [InlineData("")]
    public void TheOrderIsTheSameUnderEachCulture(string cultureName)
    {
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo culture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            OrganizePlan plan = Run(MiscCase());

            Assert.Equal(
                Layout(1501, ChestRole.Category, "Misc", S("Bread", 2), S("apple", 1), S("berry", 3)),
                LayoutRowOf(plan.FindLayout(Id(1501))!));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }

    // GW-U-555
    [Fact]
    public void EqualPrefabsOrderByQualityThenByMaxStack()
    {
        PlanInput input = Make(
            new[] { C(1502, 0, 0, 4, S("berry", 1, 50, 2), S("berry", 1, 30, 1), S("berry", 1, 50, 1)) },
            Assign((1502, CategoryAssignment.ForCategory("Misc"))));
        OrganizePlan plan = Run(input);

        Assert.Equal(
            Layout(1502, ChestRole.Category, "Misc", S("berry", 1, 30, 1), S("berry", 1, 50, 1), S("berry", 1, 50, 2)),
            LayoutRowOf(plan.FindLayout(Id(1502))!));
    }

    /// <summary>The expected category and stacks of each contract 6.6 row, copied from the table.</summary>
    private static (string Category, ItemStackSnapshot[] Stacks) ExpectedCompaction(string name) => name switch
    {
        "601" => ("Stone", new[] { S("Flint", 7), S("Stone", 50), S("Stone", 10) }),
        "602" => ("Wood", new[] { S("FineWood", 5), S("RoundLog", 5), S("Wood", 10) }),
        "603" => ("Gear", new[] { S("AxeBronze", 1, 1), S("SwordIron", 1, 1), S("SwordIron", 1, 1, 2) }),
        "604" => ("Stone", new[] { S("Stone", 10, 30), S("Stone", 35, 50) }),
        "605" => ("Wood", new[] { S("Wood", 50), S("Wood", 50), S("Wood", 50) }),
        _ => throw new ArgumentException("Unknown compaction case: " + name, nameof(name)),
    };

    /// <summary>A Misc chest holding three unknown prefabs, whose case differs: berry3, Bread2 and apple1.</summary>
    private static PlanInput MiscCase() => Make(
        new[] { C(1501, 0, 0, 3, S("berry", 3), S("Bread", 2), S("apple", 1)) },
        Assign((1501, CategoryAssignment.ForCategory("Misc"))));
}
