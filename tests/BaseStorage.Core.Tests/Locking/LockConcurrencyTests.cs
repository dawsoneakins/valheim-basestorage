namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-5 (contract 5.5, LK4, LK7): two calls at the same clock time give one success, and the service keeps no state between calls.</summary>
public sealed class LockConcurrencyTests
{
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    // GW-U-275 (AC-5): with the clock at 3600 for both calls, exactly one call is granted, and the other is refused naming the winner.
    [Theory]
    [InlineData("erik-first", "shared")]
    [InlineData("erik-first", "two-instances")]
    [InlineData("astrid-first", "shared")]
    [InlineData("astrid-first", "two-instances")]
    public void ExactlyOneOfTwoConcurrentAcquiresIsGranted(string order, string instances)
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        var first = new OrganizeLock(store, clock, settings);
        OrganizeLock second = instances == "shared" ? first : new OrganizeLock(store, clock, settings);
        bool erikWins = order == "erik-first";
        long winnerId = erikWins ? LockFixtures.ErikId : LockFixtures.AstridId;
        long loserId = erikWins ? LockFixtures.AstridId : LockFixtures.ErikId;
        string winnerName = erikWins ? "Erik" : "Astrid";
        string loserName = erikWins ? "Astrid" : "Erik";

        OrganizeLockResult winner = first.Acquire(LockFixtures.Base12(), winnerId, winnerName);
        OrganizeLockResult loser = second.Acquire(LockFixtures.Base12(), loserId, loserName);

        Assert.Equal(1, new[] { winner, loser }.Count(result => result.IsGranted));
        Assert.True(winner.IsGranted, "The first call must be granted.");
        Assert.Equal(OrganizeLockStatus.Refused, loser.Status);
        Assert.Equal(winnerName, loser.HolderName);
        Assert.Equal(new[] { (LockFixtures.S1, erikWins ? LockFixtures.Erik3630 : LockFixtures.Astrid3630) }, store.Contents());
    }

    // GW-U-276 (LK4): the service declares exactly three instance fields (store, clock, settings), all read-only, and no static fields.
    [Fact]
    public void OrganizeLockDeclaresExactlyThreeReadOnlyInstanceFieldsAndNoStatics()
    {
        FieldInfo[] instanceFields = typeof(OrganizeLock).GetFields(AllDeclared).Where(field => !field.IsStatic).ToArray();

        Assert.Equal(3, instanceFields.Length);
        string[] fieldTypes = instanceFields
            .Select(field => field.FieldType.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] expectedTypes = new[] { typeof(IClock), typeof(BaseStorageSettings), typeof(IOrganizeLockStore) }
            .Select(type => type.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedTypes, fieldTypes);
        Assert.All(instanceFields, field => Assert.True(field.IsInitOnly, field.Name + " must be read-only."));
        FieldInfo[] allFields = typeof(OrganizeLock).GetFields(AllDeclared);
        Assert.DoesNotContain(allFields, field => field.IsStatic);
    }

    // GW-U-276 (LK4): every instance field of OrganizeLockResult is read-only.
    [Fact]
    public void OrganizeLockResultInstanceFieldsAreReadOnly()
    {
        FieldInfo[] fields = typeof(OrganizeLockResult).GetFields(AllDeclared).Where(field => !field.IsStatic).ToArray();

        Assert.All(fields, field => Assert.True(field.IsInitOnly, field.Name + " must be read-only."));
    }

    // GW-U-276 (LK4): after a sequence on instance A, a new instance B over the same fakes gives results equal to A's for the same next call.
    [Fact]
    public void ANewInstanceOverTheSameFakesBehavesLikeTheOldOne()
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        var a = new OrganizeLock(store, clock, settings);
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        a.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3620);
        a.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        var b = new OrganizeLock(store, clock, settings);
        OrganizeLockResult fromA = a.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        OrganizeLockResult fromB = b.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, fromA.Status);
        Assert.Equal(fromA, fromB);

        OrganizeLockResult renewA = a.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        OrganizeLockResult renewB = b.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, renewA.Status);
        Assert.Equal(renewA, renewB);
    }
}
