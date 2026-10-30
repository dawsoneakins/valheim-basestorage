namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Tests.Patching;
using BaseStorage.Plugin.Favourites;
using BaseStorage.Plugin.Patching;

/// <summary>
/// AC-10 and AC-12 (FV3, FV6): only the favourites key is touched, and a failing dictionary write reaches the patch helper,
/// which reports it under the patch key and lets vanilla run. The helper statics are shared, so the class uses PatchSafetyStatic.
/// </summary>
[Collection("PatchSafetyStatic")]
public sealed class ProfileFavouritesKeyTests : IDisposable
{
    private const string PrefixKey = "BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch.Prefix";

    /// <summary>Resets the shared reporter, so no test leaks into another (ADR-GWIN-021).</summary>
    public void Dispose()
    {
        PatchSafety.Initialize(null);
    }

    // GW-U-705
    [Fact]
    public void ConstructionLogsNothingAndACurrentReadLogsOnlyTheFavouritesKey()
    {
        var d = new RecordingDictionary(new[]
        {
            new KeyValuePair<string, string>(FavouriteFixtures.OtherKey, FavouriteFixtures.OtherValue),
            new KeyValuePair<string, string>(FavouriteFixtures.ZzKey, FavouriteFixtures.ZzValue),
        });

        var f = new ProfileFavourites(d);
        Assert.Empty(d.Calls);

        Assert.Equal(FavouriteSlots.Empty, f.Current);
        Assert.Equal(new[] { "TryGetValue:" + PersistenceKeys.Favourites }, d.Calls);
    }

    // GW-U-705
    [Fact]
    public void EachToggleLogsExactlyOneFavouritesWriteAndNeverClearsOrAdds()
    {
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        var f = new ProfileFavourites(d);

        AssertOneWrite(d, () => f.Toggle(new InventorySlot(3, 2)), "Set:" + PersistenceKeys.Favourites);
        AssertOneWrite(d, () => f.Toggle(new InventorySlot(7, 0)), "Set:" + PersistenceKeys.Favourites);
        AssertOneWrite(d, () => f.Toggle(new InventorySlot(3, 2)), "Set:" + PersistenceKeys.Favourites);
        AssertOneWrite(d, () => f.Toggle(new InventorySlot(7, 0)), "Remove:" + PersistenceKeys.Favourites);
    }

    // GW-U-705
    [Fact]
    public void EveryLoggedKeyIsTheFavouritesKeyAndTheForeignEntriesSurviveTheSequence()
    {
        var d = new RecordingDictionary(new[]
        {
            new KeyValuePair<string, string>(FavouriteFixtures.OtherKey, FavouriteFixtures.OtherValue),
            new KeyValuePair<string, string>(FavouriteFixtures.ZzKey, FavouriteFixtures.ZzValue),
        });
        var f = new ProfileFavourites(d);

        f.Toggle(new InventorySlot(3, 2));
        f.Toggle(new InventorySlot(7, 0));
        f.Toggle(new InventorySlot(3, 2));
        f.Toggle(new InventorySlot(7, 0));

        foreach (string call in d.Calls.Where(call => call.Contains(':', StringComparison.Ordinal)))
        {
            Assert.EndsWith(":" + PersistenceKeys.Favourites, call, StringComparison.Ordinal);
        }

        Assert.Equal(FavouriteFixtures.OtherValue, d[FavouriteFixtures.OtherKey]);
        Assert.Equal(FavouriteFixtures.ZzValue, d[FavouriteFixtures.ZzKey]);
        Assert.Equal(2, d.Count);
    }

    // GW-U-708
    [Fact]
    public void ANullCustomDataIsRejectedWithTheContractParameterName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new ProfileFavourites(null!));

        Assert.Equal("customData", error.ParamName);
    }

    // GW-U-708
    [Fact]
    public void ADictionaryWriteFailurePropagatesToTheCaller()
    {
        var boom = new InvalidOperationException("boom");
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        d.ThrowOnWrite = boom;
        var f = new ProfileFavourites(d);

        Exception caught = Assert.ThrowsAny<Exception>(() => f.Toggle(new InventorySlot(3, 2)));

        Assert.Same(boom, caught);
    }

    // GW-U-708
    [Fact]
    public void ThePrefixHelperReportsAFailingWriteOnceUnderItsKeyAndReturnsTrue()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        d.ThrowOnWrite = new InvalidOperationException("boom");
        var f = new ProfileFavourites(d);

        bool run = PatchSafety.RunPrefix(PrefixKey, () =>
        {
            f.Toggle(new InventorySlot(3, 2));
            return false;
        });

        Assert.True(run);
        Assert.Single(sink.Entries, entry => entry.Severity == LogSeverity.Error && entry.Message.Contains(PrefixKey, StringComparison.Ordinal));
    }

    private static void AssertOneWrite(RecordingDictionary d, Action toggle, string expectedWrite)
    {
        d.Calls.Clear();
        toggle();

        string[] writes = d.Calls
            .Where(call => call.StartsWith("Set:", StringComparison.Ordinal)
                || call.StartsWith("Remove:", StringComparison.Ordinal)
                || call.StartsWith("Add:", StringComparison.Ordinal)
                || call == "Clear")
            .ToArray();
        Assert.Equal(new[] { expectedWrite }, writes);
    }
}
