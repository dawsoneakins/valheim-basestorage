namespace BaseStorage.Core.Tests;

/// <summary>AC-3 smoke test for the Core anchor type, and AC-12 version reach.</summary>
[Trait("Category", "Repo")]
public sealed class CoreAssemblyMarkerTests
{
    // GW-U-004
    [Fact]
    public void CoreAssemblyMarkerIsPublicStaticInBaseStorageCore()
    {
        Type marker = typeof(BaseStorage.Core.CoreAssemblyMarker);

        Assert.True(marker.IsPublic);
        Assert.True(marker.IsAbstract && marker.IsSealed, "A static class compiles to an abstract sealed type.");
        Assert.Equal("BaseStorage.Core", marker.Assembly.GetName().Name ?? string.Empty);
    }

    // GW-U-018
    [Fact]
    public void CoreAssemblyVersionIsZeroPointOneZero()
    {
        Version? version = typeof(BaseStorage.Core.CoreAssemblyMarker).Assembly.GetName().Version;

        Assert.Equal(new Version(0, 3, 0, 0), version);
    }
}
