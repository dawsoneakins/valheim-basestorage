namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// AC-3 and AC-4 (contract 3.3 static table, section 8, V5): the message key per outcome through the one strings table, and
/// the grid position range check that never throws.
/// </summary>
public sealed class ProfileFavouritesStaticTests
{
    /// <summary>Positions that must be rejected, named by their label.</summary>
    public static TheoryData<string, int, int> OutOfRangePositions => new()
    {
        { "GetButtonPos miss", -1, -1 },
        { "x past 255", 256, 0 },
        { "y past 255", 0, 256 },
        { "y below 0", 3, -1 },
        { "x int.MinValue", int.MinValue, 0 },
        { "y int.MaxValue", 0, int.MaxValue },
    };

    /// <summary>Positions at the edges of the valid range, named by their label.</summary>
    public static TheoryData<string, int, int> InRangePositions => new()
    {
        { "origin", 0, 0 },
        { "far corner", 255, 255 },
    };

    // GW-U-706
    [Fact]
    public void MessageKeysAreTheStringTableKeysForEachOutcome()
    {
        Assert.Equal("message.favourite.on", ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Added));
        Assert.Equal(StringKeys.MessageFavouriteOn, ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Added));
        Assert.Equal("message.favourite.off", ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Removed));
        Assert.Equal(StringKeys.MessageFavouriteOff, ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Removed));
    }

    // GW-U-706
    [Fact]
    public void AddedFormatsTheFavouriteLineWithTheItemName()
    {
        Assert.Equal(
            "Favourite: Wood",
            StringTable.English.Format(ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Added), "Wood"));
    }

    // GW-U-706
    [Fact]
    public void RemovedFormatsTheNoLongerFavouriteLineWithTheItemName()
    {
        Assert.Equal(
            "No longer favourite: Bronze axe",
            StringTable.English.Format(ProfileFavourites.MessageKeyFor(FavouriteToggleOutcome.Removed), "Bronze axe"));
    }

    // GW-U-706
    [Fact]
    public void AnUnknownOutcomeIsRejectedWithTheOutcomeParameterName()
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => ProfileFavourites.MessageKeyFor((FavouriteToggleOutcome)7));

        Assert.Equal("outcome", error.ParamName);
    }

    // GW-U-706
    [Fact]
    public void TheOutcomeEnumHasExactlyAddedAndRemovedWithValuesZeroAndOne()
    {
        Assert.Equal(new[] { "Added", "Removed" }, Enum.GetNames(typeof(FavouriteToggleOutcome)));
        Assert.Equal(0, (int)FavouriteToggleOutcome.Added);
        Assert.Equal(1, (int)FavouriteToggleOutcome.Removed);
    }

    // GW-U-707
    [Theory]
    [MemberData(nameof(OutOfRangePositions))]
    public void AnOutOfRangePositionReturnsFalseWithTheDefaultSlotAndDoesNotThrow(string label, int x, int y)
    {
        bool found = ProfileFavourites.TryGetSlot(x, y, out InventorySlot slot);

        Assert.False(found, label);
        Assert.Equal(default(InventorySlot), slot);
    }

    // GW-U-707
    [Theory]
    [MemberData(nameof(InRangePositions))]
    public void AnInRangePositionReturnsTrueWithTheMatchingSlot(string label, int x, int y)
    {
        bool found = ProfileFavourites.TryGetSlot(x, y, out InventorySlot slot);

        Assert.True(found, label);
        Assert.Equal(new InventorySlot(x, y), slot);
    }
}
