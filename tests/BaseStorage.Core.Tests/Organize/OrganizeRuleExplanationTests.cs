namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>S-A-003: the preview says how many chests get a rule and why stacks are left over.</summary>
public sealed class OrganizeRuleExplanationTests
{
    private static CategoryCatalog Catalog2() => CategoryFile.Parse("Wood: Wood, FineWood\nStone: Stone, Flint\nFood: Bread, Honey\n").Catalog;

    private static OrganizePlan Plan(IReadOnlyList<ContainerSnapshot> chests, IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments) =>
        OrganizePlanner.Plan(chests, assignments, new Dictionary<ContainerId, string>(), Catalog2(), new Dictionary<string, ItemKind>(), Options(), 50, true);

    [Fact]
    public void TheMixedChestStepListsOnlyTheChestsItGaveACategory()
    {
        var mixed = C(1, 0f, 0f, 3, S("Wood", 1), S("Stone", 1), S("Bread", 1));
        var majority = C(2, 2f, 0f, 4, S("Wood", 1), S("Wood", 2));

        OrganizePlan plan = Plan(new[] { mixed, majority }, new Dictionary<ContainerId, CategoryAssignment>());

        ChestIds(plan, out var ids);
        Assert.Contains(mixed.Id, ids);
        Assert.DoesNotContain(majority.Id, ids);
    }

    [Fact]
    public void ThePreviewCountsTheChestsThatWillGetACategory()
    {
        var mixed = C(1, 0f, 0f, 8, S("Wood", 1), S("Stone", 1), S("Bread", 1));

        string body = OrganizeSummaryFormatter.PreviewBody(Plan(new[] { mixed }, new Dictionary<ContainerId, CategoryAssignment>()), Catalog2());

        Assert.Contains("1 chest will get a category.", body);
    }

    [Fact]
    public void AFullBaseSaysItIsFull()
    {
        var a = C(1, 0f, 0f, 1, S("Wood", 1));
        var b = C(2, 2f, 0f, 1, S("Stone", 1));
        var assignments = Assign((1, CategoryAssignment.ForCategory("Stone")), (2, CategoryAssignment.ForCategory("Wood")));

        string body = OrganizeSummaryFormatter.PreviewBody(Plan(new[] { a, b }, assignments), Catalog2());

        Assert.Contains("This base is full. Free up space or add a chest.", body);
    }

    [Fact]
    public void FreeSlotsInTheWrongCategorySayThat()
    {
        var a = C(1, 0f, 0f, 1, S("Wood", 1));
        var b = C(2, 2f, 0f, 4);
        var assignments = Assign((1, CategoryAssignment.ForCategory("Stone")), (2, CategoryAssignment.ForCategory("Stone")));

        string body = OrganizeSummaryFormatter.PreviewBody(Plan(new[] { a, b }, assignments), Catalog2());

        Assert.Contains("Some categories ran out of space.", body);
    }

    private static void ChestIds(OrganizePlan plan, out List<ContainerId> ids)
    {
        ids = new List<ContainerId>();
        foreach (var rule in plan.AutoAssign.MixedAssigned)
        {
            ids.Add(rule.Id);
        }
    }
}
