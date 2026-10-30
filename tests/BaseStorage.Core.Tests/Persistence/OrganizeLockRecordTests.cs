namespace BaseStorage.Core.Tests.Persistence;

using System;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and contract 3.2: OrganizeLockRecord fields, validation, and value equality.</summary>
public sealed class OrganizeLockRecordTests
{
    private const string TypeName = "OrganizeLockRecord";
    private const long ErikId = 1923487561234L;
    private static readonly TimeSpan ErikExpiry = TimeSpan.FromSeconds(3630);

    // GW-U-071
    [Fact]
    public void ErikRecordExposesAllThreeFields()
    {
        object erik = Erik();

        Assert.Equal<object?>(ErikId, PersistenceContract.Get(erik, "HolderId"));
        Assert.Equal<object?>("Erik", PersistenceContract.Get(erik, "HolderName"));
        Assert.Equal<object?>(ErikExpiry, PersistenceContract.Get(erik, "ExpiresAt"));
    }

    // GW-U-071
    [Fact]
    public void NegativeIdAndExtremeExpiriesAreValid()
    {
        TimeSpan[] expiries = [TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, TimeSpan.FromTicks(-10000000L)];
        foreach (TimeSpan expiry in expiries)
        {
            object record = PersistenceContract.Make(TypeName, -6017393921047218534L, "Astrid", expiry);
            Assert.Equal<object?>(expiry, PersistenceContract.Get(record, "ExpiresAt"));
        }

        object minimum = PersistenceContract.Make(TypeName, long.MinValue, "Erik", ErikExpiry);
        Assert.Equal<object?>(long.MinValue, PersistenceContract.Get(minimum, "HolderId"));
    }

    // GW-U-071
    [Fact]
    public void EqualRecordsAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(Erik(), Erik());
    }

    // GW-U-071
    [Fact]
    public void OneDifferingFieldMakesRecordsUnequal()
    {
        object erik = Erik();

        ValueSemantics.AssertNotEqual(erik, PersistenceContract.Make(TypeName, ErikId + 1, "Erik", ErikExpiry));
        ValueSemantics.AssertNotEqual(erik, PersistenceContract.Make(TypeName, ErikId, "erik", ErikExpiry));
        ValueSemantics.AssertNotEqual(erik, PersistenceContract.Make(TypeName, ErikId, "Erik", TimeSpan.FromTicks(ErikExpiry.Ticks + 1)));
    }

    // GW-U-071
    [Fact]
    public void NullHandlingFollowsR5()
    {
        ValueSemantics.AssertNullHandling(PersistenceContract.Resolve(TypeName), Erik());
    }

    // GW-U-071
    [Fact]
    public void ZeroHolderIdIsOutOfRange()
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => { PersistenceContract.Make(TypeName, 0L, "Erik", ErikExpiry); });
        Assert.Equal("holderId", ex.ParamName);
    }

    // GW-U-071
    [Fact]
    public void NullHolderNameIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => { PersistenceContract.Make(TypeName, ErikId, (string?)null, ErikExpiry); });
        Assert.Equal("holderName", ex.ParamName);
    }

    // GW-U-071
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Er\nik")]
    public void BlankOrControlCharacterHolderNameIsAnArgumentException(string holderName)
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => { PersistenceContract.Make(TypeName, ErikId, holderName, ErikExpiry); });
        Assert.Equal("holderName", ex.ParamName);
    }

    // GW-U-071
    [Fact]
    public void ZeroIdWithNullNameReportsHolderIdFirst()
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => { PersistenceContract.Make(TypeName, 0L, (string?)null, ErikExpiry); });
        Assert.Equal("holderId", ex.ParamName);
    }

    private static object Erik() => PersistenceContract.Make(TypeName, ErikId, "Erik", ErikExpiry);
}
