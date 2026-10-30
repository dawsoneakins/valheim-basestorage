namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>AC-3 (contract 3.1, 3.2): the result value table, its equality, its argument checks, and the contracted status enum.</summary>
public sealed class OrganizeLockResultTests
{
    private static OrganizeLockRecord Erik() => new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630));

    // GW-U-271 (contract 3.2): Refused(erik) has the refusal properties and the English notice.
    [Fact]
    public void RefusedResultHasTheRefusalProperties()
    {
        OrganizeLockRecord erik = Erik();

        OrganizeLockResult result = OrganizeLockResult.Refused(erik);

        Assert.Equal(OrganizeLockStatus.Refused, result.Status);
        Assert.False(result.IsGranted);
        Assert.Equal("Erik", result.HolderName);
        Assert.Equal(erik, result.Record);
        Assert.Equal("message.base.in-progress", result.MessageKey);
        Assert.Equal("Organize in progress by Erik", result.FormatMessage(StringTable.English));
    }

    // GW-U-271 (contract 3.2): a name with a semicolon is formatted as given.
    [Fact]
    public void RefusedNoticeKeepsASemicolonInTheName()
    {
        var ragnar = new OrganizeLockRecord(42L, "Ragnar;the Red", TimeSpan.Zero);

        Assert.Equal("Organize in progress by Ragnar;the Red", OrganizeLockResult.Refused(ragnar).FormatMessage(StringTable.English));
    }

    // GW-U-271 (contract 3.2): Granted(erik) is granted, carries the record, and has no name, key or notice.
    [Fact]
    public void GrantedResultHasNoNoticeAndNoHolderName()
    {
        OrganizeLockRecord erik = Erik();

        OrganizeLockResult result = OrganizeLockResult.Granted(erik);

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.True(result.IsGranted);
        Assert.Equal(erik, result.Record);
        Assert.Null(result.HolderName);
        Assert.Null(result.MessageKey);
        Assert.Null(result.FormatMessage(StringTable.English));
    }

    // GW-U-271 (contract 3.2): NotHeld has every member null, and it equals itself across two reads.
    [Fact]
    public void NotHeldHasEveryMemberNullAndEqualsItself()
    {
        OrganizeLockResult first = OrganizeLockResult.NotHeld;
        OrganizeLockResult second = OrganizeLockResult.NotHeld;

        Assert.Equal(OrganizeLockStatus.NotHeld, first.Status);
        Assert.False(first.IsGranted);
        Assert.Null(first.Record);
        Assert.Null(first.HolderName);
        Assert.Null(first.MessageKey);
        Assert.Null(first.FormatMessage(StringTable.English));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first, second);
    }

    // GW-U-271 (contract 3.2): equality follows the status and the record, and the hash code agrees with it.
    [Fact]
    public void EqualityFollowsTheStatusAndTheRecord()
    {
        OrganizeLockRecord erik = Erik();
        var sameErik = new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630));
        var laterErik = new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3640));

        Assert.True(OrganizeLockResult.Granted(erik) == OrganizeLockResult.Granted(sameErik));
        Assert.False(OrganizeLockResult.Granted(erik) != OrganizeLockResult.Granted(sameErik));
        Assert.Equal(OrganizeLockResult.Granted(erik).GetHashCode(), OrganizeLockResult.Granted(sameErik).GetHashCode());
        Assert.True(OrganizeLockResult.Granted(erik).Equals((object)OrganizeLockResult.Granted(sameErik)));
        Assert.False(OrganizeLockResult.Granted(erik) == OrganizeLockResult.Refused(erik));
        Assert.True(OrganizeLockResult.Granted(erik) != OrganizeLockResult.Refused(erik));
        Assert.False(OrganizeLockResult.Granted(erik).Equals(OrganizeLockResult.Refused(erik)));
        Assert.False(OrganizeLockResult.Refused(erik) == OrganizeLockResult.Refused(laterErik));
        Assert.False(OrganizeLockResult.NotHeld == OrganizeLockResult.Granted(erik));
    }

    // GW-U-271 (contract 3.2): null arguments are rejected by the contracted ParamName, in each factory and in FormatMessage.
    [Fact]
    public void FactoriesRejectNullByParamName()
    {
        ArgumentNullException granted = Assert.Throws<ArgumentNullException>(() => OrganizeLockResult.Granted(null!));
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => OrganizeLockResult.Refused(null!));

        Assert.Equal("record", granted.ParamName);
        Assert.Equal("liveRecord", refused.ParamName);
    }

    // GW-U-271 (contract 3.2): FormatMessage rejects a null table under each of the three statuses, by ParamName "table".
    [Theory]
    [InlineData("refused")]
    [InlineData("granted")]
    [InlineData("not-held")]
    public void FormatMessageRejectsANullTableUnderEveryStatus(string status)
    {
        OrganizeLockResult result = status switch
        {
            "refused" => OrganizeLockResult.Refused(Erik()),
            "granted" => OrganizeLockResult.Granted(Erik()),
            _ => OrganizeLockResult.NotHeld,
        };

        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => result.FormatMessage(null!));

        Assert.Equal("table", thrown.ParamName);
    }

    // GW-U-272 (AC-3, contract 3.1): the status enum has exactly the three contracted names, the contracted int values, and an int underlying type.
    [Fact]
    public void StatusEnumIsContractedByNameAndValue()
    {
        Assert.Equal(new[] { "Granted", "Refused", "NotHeld" }, Enum.GetNames(typeof(OrganizeLockStatus)));
        Assert.Equal(0, (int)OrganizeLockStatus.Granted);
        Assert.Equal(1, (int)OrganizeLockStatus.Refused);
        Assert.Equal(2, (int)OrganizeLockStatus.NotHeld);
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(OrganizeLockStatus)));
    }
}
