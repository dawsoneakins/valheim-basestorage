namespace BaseStorage.Core.Tests.Organize.Deposit;

using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// S-A-001 (AC-9; mixed-chest-category-contracts.md 4 and 6.9): Put away never runs the mixed-chest step, so chest 209 stays a source
/// in a Deposit plan over base E even though Organize with the setting on makes it a Wood chest.
/// </summary>
public sealed class DepositMixedChestTests
{
    // GW-U-2445 (AC-9, 6.9 row 1): the Deposit plan keeps 209 a source and gives it nothing.
    [Fact]
    public void DepositPlannerKeepsChest209ASource()
    {
        DepositPlan deposit = DepositFixtures.Run(DepositFixtures.I(), DepositFixtures.DefaultExclusions(DepositFixtures.FavouriteSlot), E());

        Assert.Equal(AutoAssignOutcome.SourceOnly, deposit.Plan.AutoAssign.Find(Id(209))!.Outcome);
        Assert.Equal(ChestRole.Source, deposit.Plan.FindLayout(Id(209))!.Role);
    }
}
