namespace BaseStorage.Core.Tests.Requirements;

using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Availability;

/// <summary>
/// Contract 4.3 fixture D of requirement-display-contracts.md v1 (S-E003-005), built from <see cref="AvailabilityFixtures"/>.
/// The fixture is the world only; every expected value in the tests is a literal copied from the contract tables. Not a test class.
/// </summary>
internal static class RequirementDisplayFixtures
{
    /// <summary>The owner id that <see cref="AvailabilityFixtures.Box"/> uses for every container of fixture D.</summary>
    internal const long OwnerId = 3141592653L;

    /// <summary>
    /// Fixture D: inventory Wood 12 and Resin 3; c5 Wood 30 and 10; c7 open by another player with Stone 20 and Wood 50; c9 Resin 2.
    /// Excludes vehicles and does not keep chests craftable. Returns a fresh snapshot on every call.
    /// </summary>
    /// <returns>A new base snapshot equal in content to fixture D.</returns>
    internal static BaseSnapshot D()
    {
        ContainerSnapshot inventory = AvailabilityFixtures.Box(
            1u,
            ContainerFlags.None,
            AvailabilityFixtures.Stack("Wood", 12, 1, 50),
            AvailabilityFixtures.Stack("Resin", 3, 1, 50));

        ContainerSnapshot c5 = AvailabilityFixtures.Box(
            5u,
            ContainerFlags.PlayerBuilt,
            AvailabilityFixtures.Stack("Wood", 30, 1, 50),
            AvailabilityFixtures.Stack("Wood", 10, 1, 50));

        ContainerSnapshot c7 = AvailabilityFixtures.Box(
            7u,
            ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther,
            AvailabilityFixtures.Stack("Stone", 20, 1, 50),
            AvailabilityFixtures.Stack("Wood", 50, 1, 50));

        ContainerSnapshot c9 = AvailabilityFixtures.Box(
            9u,
            ContainerFlags.PlayerBuilt,
            AvailabilityFixtures.Stack("Resin", 2, 1, 50));

        return new BaseSnapshot(
            inventory,
            new[] { c5, c7, c9 },
            AvailabilityFixtures.NoAssignments(),
            new EligibilityOptions(excludeVehicles: true, keepChestsCraftable: false));
    }
}
