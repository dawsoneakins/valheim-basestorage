namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Networking;

/// <summary>AC-6.1 (contract 4.2, rules R4, R5, R6, R8): factories, value equality and immutability of VersionCheckResult.</summary>
public sealed class VersionCheckResultTests
{
    // GW-U-811 (contract 4.2): the three factories set status and versions as contracted.
    [Fact]
    public void FactoriesSetStatusAndVersions()
    {
        VersionCheckResult accepted = VersionCheckResult.Accepted("0.1.0", "0.2.0");
        VersionCheckResult acceptedNoClient = VersionCheckResult.Accepted("0.1.0", null);
        VersionCheckResult mismatch = VersionCheckResult.Mismatch("0.1.0", "0.2.0");
        VersionCheckResult missing = VersionCheckResult.Missing("0.1.0");

        Assert.Equal(VersionCheckStatus.Accepted, accepted.Status);
        Assert.Equal("0.2.0", accepted.ClientVersion);
        Assert.Equal(VersionCheckStatus.Accepted, acceptedNoClient.Status);
        Assert.Null(acceptedNoClient.ClientVersion);
        Assert.Equal(VersionCheckStatus.RejectedMismatch, mismatch.Status);
        Assert.Equal("0.1.0", mismatch.HostVersion);
        Assert.Equal("0.2.0", mismatch.ClientVersion);
        Assert.Equal(VersionCheckStatus.RejectedMissing, missing.Status);
        Assert.Equal("0.1.0", missing.HostVersion);
        Assert.Null(missing.ClientVersion);
        Assert.True(accepted.IsAccepted && acceptedNoClient.IsAccepted);
        Assert.False(mismatch.IsAccepted || missing.IsAccepted);
    }

    // GW-U-811 (R5): a result from a factory equals the same result from VersionCheck, with equal hash codes.
    [Fact]
    public void FactoryResultEqualsTheComparedResult()
    {
        VersionCheckResult mismatch = VersionCheckResult.Mismatch("0.1.0", "0.2.0");
        VersionCheckResult compared = VersionCheck.Compare("0.1.0", "0.2.0", true);

        Assert.True(mismatch == compared);
        Assert.True(mismatch.Equals(compared));
        Assert.True(mismatch.Equals((object)compared));
        Assert.False(mismatch != compared);
        Assert.Equal(mismatch.GetHashCode(), compared.GetHashCode());
        Assert.Equal(VersionCheckResult.Accepted("0.1.0", "0.2.0"), VersionCheck.Compare("0.1.0", "0.2.0", false));
        Assert.Equal(VersionCheckResult.Missing("0.1.0"), VersionCheck.Compare("0.1.0", null, true));
        Assert.Equal(VersionCheckResult.Accepted("0.1.0", null), VersionCheck.Compare("0.1.0", null, false));
    }

    // GW-U-811 (R5, R6): the contract 4.4 equality rows.
    [Fact]
    public void EqualityRowsMatchTheContract()
    {
        Assert.True(VersionCheckResult.Accepted("0.1.0", "0.2.0") == VersionCheckResult.Accepted("0.1.0", "0.2.0"));
        Assert.False(VersionCheckResult.Accepted("0.1.0", "0.1.0") == VersionCheckResult.Accepted("0.1.0", "0.2.0"));
        Assert.False(VersionCheckResult.Accepted("0.1.0", null) == VersionCheckResult.Missing("0.1.0"));
        Assert.False(VersionCheckResult.Missing("0.1.0") == VersionCheckResult.Missing("0.2.0"));
        Assert.True(VersionCheckResult.Missing("0.1.0") == VersionCheckResult.Missing("0.1.0"));
        Assert.Equal(VersionCheckResult.Missing("0.1.0").GetHashCode(), VersionCheckResult.Missing("0.1.0").GetHashCode());
        Assert.False(VersionCheckResult.Accepted("0.1.0", "0.2.0") == VersionCheckResult.Mismatch("0.1.0", "0.2.0"));
        Assert.False(VersionCheckResult.Mismatch("0.1.0", "0.2.0") == VersionCheckResult.Mismatch("0.2.0", "0.1.0"));
        Assert.True(VersionCheckResult.Accepted("0.1.0", null) == VersionCheckResult.Accepted("0.1.0", null));
    }

    // GW-U-811 (R5): null handling of the operators and Equals.
    [Fact]
    public void NullComparisonsFollowTheContract()
    {
        VersionCheckResult result = VersionCheckResult.Missing("0.1.0");
        VersionCheckResult? none = null;

        Assert.False(result == null);
        Assert.False(null == result);
        Assert.True(result != null);
        Assert.True(none == null);
        Assert.False(none != null);
        Assert.False(result.Equals(null));
        Assert.False(result.Equals((object?)null));
        Assert.False(result.Equals("0.1.0"));
    }

    // GW-U-811 (R10): ToString does not throw for any status.
    [Fact]
    public void ToStringDoesNotThrow()
    {
        Assert.NotNull(VersionCheckResult.Accepted("0.1.0", null).ToString());
        Assert.NotNull(VersionCheckResult.Mismatch("0.1.0", "0.2.0").ToString());
        Assert.NotNull(VersionCheckResult.Missing("0.1.0").ToString());
    }

    // GW-U-811 (R8): factory validation rows of contract 4.4.
    [Fact]
    public void FactoryValidationFollowsTheContract()
    {
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Accepted(null!, "0.1.0")).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Mismatch(null!, "0.1.0")).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Missing(null!)).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Accepted("1.0", "0.1.0")).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Mismatch("", "0.1.0")).ParamName);
        Assert.Equal("hostVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Missing("01.0.0")).ParamName);
        Assert.Equal("clientVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Accepted("0.1.0", "")).ParamName);
        Assert.Equal("clientVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Accepted("0.1.0", "0.1")).ParamName);
        Assert.Equal("clientVersion", Assert.Throws<ArgumentNullException>(() => VersionCheckResult.Mismatch("0.1.0", null!)).ParamName);
        Assert.Equal("clientVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Mismatch("0.1.0", "0.1.0-rc")).ParamName);
        Assert.Equal("clientVersion", Assert.Throws<ArgumentException>(() => VersionCheckResult.Mismatch("0.1.0", "0.1.0")).ParamName);
    }

    // GW-U-811 (R4): every public property is get-only and every instance field is readonly.
    [Fact]
    public void ResultIsImmutable()
    {
        Type type = typeof(VersionCheckResult);

        Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotEmpty(fields);
        Assert.All(fields, field => Assert.True(field.IsInitOnly, field.Name + " must be readonly."));
        Assert.DoesNotContain(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), field => !field.IsInitOnly && !field.IsLiteral);
    }
}
