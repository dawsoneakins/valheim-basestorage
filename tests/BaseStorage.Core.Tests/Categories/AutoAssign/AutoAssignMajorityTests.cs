namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using BaseStorage.Core.Categories.AutoAssign;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-1 (contract 5.2): the majority chests of base B are assigned.</summary>
public sealed class AutoAssignMajorityTests
{
    // GW-U-500
    [Theory]
    [InlineData(102u, AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData(110u, AutoAssignOutcome.Assigned, "Gear")]
    [InlineData(111u, AutoAssignOutcome.Assigned, "Wood")]
    public void MajorityChestsOfBaseBAreAssigned(uint id, AutoAssignOutcome outcome, string name)
    {
        AutoAssignResult r = ResolveB();

        AssertChest("chest " + id, outcome, name, At(r, id));
    }
}
