namespace BaseStorage.PatchTargets.Tests;

/// <summary>
/// AC-3 smoke test. It is unconditional and does not depend on ValheimDir or the game folder.
/// Real patch targets arrive with source AC-1.4.
/// </summary>
public sealed class PatchTargetsSmokeTests
{
    // GW-U-005
    [Fact]
    public void AssemblyIsNamedBaseStoragePatchTargetsTests()
    {
        string name = typeof(PatchTargetsSmokeTests).Assembly.GetName().Name ?? string.Empty;

        Assert.Equal("BaseStorage.PatchTargets.Tests", name);
    }
}
