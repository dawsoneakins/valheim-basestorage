namespace BaseStorage.Core.Tests.Networking;

using BaseStorage.Core.Networking;

/// <summary>AC-6.1 and spec section 3 (contract 2.1, rules V1 and V2): the build version comes from Version.props, and the grammar check.</summary>
public sealed class ModVersionTests
{
    // GW-U-800 (V1): ModVersion.Current is the Version.props value, captured at build time, and is well formed.
    [Fact]
    public void CurrentIsTheVersionPropsValueAndIsWellFormed()
    {
        string expected = NetworkingFixtures.VersionPropsVersion();

        Assert.Equal(expected, ModVersion.Current);
        Assert.True(ModVersion.IsWellFormed(ModVersion.Current));
        Assert.Same(ModVersion.Current, ModVersion.Current);
    }

    // GW-U-801 (V2): every contract 2.1 well-formed example is accepted.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.WellFormedRows), MemberType = typeof(NetworkingFixtures))]
    public void WellFormedVersionsAreAccepted(string version)
    {
        Assert.True(ModVersion.IsWellFormed(version), version + " should be well formed.");
    }

    // GW-U-801 (V2): every contract 2.1 ill-formed example is refused, and null gives false without throwing.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.IllFormedRows), MemberType = typeof(NetworkingFixtures))]
    public void IllFormedVersionsAreRefused(string version)
    {
        Assert.False(ModVersion.IsWellFormed(version), version + " should not be well formed.");
    }

    // GW-U-801 (V2): null is not a version and does not throw.
    [Fact]
    public void NullIsNotWellFormed()
    {
        Assert.False(ModVersion.IsWellFormed(null));
    }

    // GW-U-801 (V2): the digit cap is exactly nine per part.
    [Theory]
    [InlineData("123456789.0.0", true)]
    [InlineData("0.123456789.0", true)]
    [InlineData("0.0.123456789", true)]
    [InlineData("1234567890.0.0", false)]
    [InlineData("0.1234567890.0", false)]
    [InlineData("0.0.1234567890", false)]
    public void NineDigitsPerPartIsTheCap(string version, bool expected)
    {
        Assert.Equal(expected, ModVersion.IsWellFormed(version));
    }
}
