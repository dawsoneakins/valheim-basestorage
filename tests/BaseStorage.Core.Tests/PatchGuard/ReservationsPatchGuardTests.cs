namespace BaseStorage.Core.Tests.PatchGuard;

using System.Reflection;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// GW-I-750 (AC-8, ADR-GWIN-058): the built Plugin dll is read as metadata and never loaded. The patch guard finds the two
/// reservation prefixes among the patch bodies, with no G1, G2 or G3 violation.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class ReservationsPatchGuardTests
{
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";

    // GW-I-750
    [Fact]
    public void TheTwoReservationPrefixesAreGuardedPatchBodies()
    {
        AssemblyConfigurationAttribute? configuration = typeof(ReservationsPatchGuardTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        string path = PluginDll.EnsureBuilt(configuration!.Configuration ?? string.Empty);

        // Edited by S-E003-008, craft-from-base-toggle 8.3 amendment (ADR-GWIN-136)
        PatchGuardResult result = PatchGuardScanner.Scan(path, new PatchGuardRules("HarmonyLib", Helper, GateMethodFullName: "BaseStorage.Plugin.Patching.CraftFromBaseGate.IsOpen"));

        Assert.Contains("BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch.Prefix", result.PatchBodies);
        Assert.Contains("BaseStorage.Plugin.Reservations.ContainerInteractPatch.Prefix", result.PatchBodies);
        // S-E006-002 (GW-I-1250): the two Networking postfixes join the ten bodies.
        // Edited by S-E003-004 (GW-U-1157): the three crafting patch bodies join the twelve.
        // Edited by S-E003-005: plus the five Requirements display bodies (twenty in all).
        // Edited by S-E003-006 (ADR-GWIN-132): the four building patch bodies join the fifteen.
        // Edited by integration (S-E003-005 + S-E003-006): the union, 15 + 5 + 4 = 24.
        // Edited by integration (S-E004-004): plus the Category button postfix, 25.
        // Edited by S-E004-012 (ADR-GWIN-136): plus the StationActions UpdateRecipe postfix, 26.
        // Edited by S-E010-002 (organize-console-command 6.2): plus TerminalInitTerminalPatch.Postfix, 27.
        Assert.Equal(46, result.PatchBodies.Count);
        Assert.True(result.Violations.Count == 0, PatchGuardScanner.FormatViolations(result));
    }
}
