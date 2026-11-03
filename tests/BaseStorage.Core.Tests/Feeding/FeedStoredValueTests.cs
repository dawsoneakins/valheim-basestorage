namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;

/// <summary>
/// The saved <c>basestorage.feed</c> value (contract section 6): literal stored values decode as written, damaged values drop without throwing,
/// the key and the key list are unchanged, and no shape is stored (AC-2, D-9).
/// </summary>
public sealed class FeedStoredValueTests
{
    // GW-U-2650
    [Theory]
    [InlineData("PD1", "CopperOre=50|Coal=0", "CopperOre=50|Coal=0")]
    [InlineData("PD2", "Coal=20|IronScrap=0|CopperOre=100", "Coal=20|IronScrap=0|CopperOre=100")]
    [InlineData("PD3", "Wood=5", "Wood=5")]
    [InlineData("PD4", "Coal=10|broken|TinOre=2", "Coal=10|TinOre=2")]
    [InlineData("PD5", "Coal=1|Coal=2", "Coal=1")]
    [InlineData("PD6", "Coal=5|", "Coal=5")]
    [InlineData("PD7", "CopperOre=007", "CopperOre=7")]
    [InlineData("PD8", "Coal=2147483647", "Coal=2147483647")]
    [InlineData("PD9", "Coal=2147483648|Wood=3", "Wood=3")]
    public void StoredFeedValuesDecodeToTheSameSelections(string id, string stored, string expected)
    {
        Assert.True(expected == Describe(FeedSelection.Decode(stored)), id + " decoded to " + Describe(FeedSelection.Decode(stored)));
    }

    // GW-U-2650 (PD1 and PD7 encode back)
    [Fact]
    public void StoredFeedValuesEncodeBackAsContracted()
    {
        Assert.Equal("CopperOre=50|Coal=0", FeedSelection.Decode("CopperOre=50|Coal=0").Encode());
        Assert.Equal("CopperOre=7", FeedSelection.Decode("CopperOre=007").Encode());
    }

    // GW-U-2651
    [Theory]
    [InlineData("PD10", null)]
    [InlineData("PD10", "")]
    [InlineData("PD10", "|||")]
    [InlineData("PD10", "=5")]
    [InlineData("PD10", "CopperOre")]
    [InlineData("PD10", "CopperOre=")]
    [InlineData("PD10", "CopperOre=-1")]
    [InlineData("PD10", "CopperOre=lots")]
    [InlineData("PD10", "CopperOre=99999999999999")]
    [InlineData("PD10", "Coal=+5")]
    [InlineData("PD10", "Coal= 5")]
    [InlineData("PD10", "Coal=5.0")]
    [InlineData("PD10", "a=b=3")]
    public void DamagedStoredValuesDecodeToEmptyWithoutThrowing(string id, string? stored)
    {
        FeedSelection? decoded = null;
        Exception? thrown = Record.Exception(() => decoded = FeedSelection.Decode(stored));
        Assert.True(thrown is null, id + " threw " + thrown?.GetType().Name);
        Assert.Same(FeedSelection.Empty, decoded);
    }

    // GW-U-2652
    [Fact]
    public void ADamagedEntryIsDroppedAndTheKeyIsUnchanged()
    {
        Assert.Equal("Coal=10|TinOre=2", Describe(FeedSelection.Decode("Coal=10|broken|TinOre=2")));
        Assert.Equal("Wood=3", Describe(FeedSelection.Decode("Coal=2147483648|Wood=3")));

        Assert.Equal("basestorage.feed", PersistenceKeys.StationFeed);
        Assert.Equal(
            new[] { "basestorage.cat", "basestorage.lock", "basestorage.fav", "basestorage.feed", "basestorage.name", "basestorage.favitems" },
            PersistenceKeys.All);
    }

    // GW-U-2654
    [Fact]
    public void TheStoredSelectionCarriesNoShape()
    {
        AssertMembers(typeof(FeedSelection), "Empty", "Entries", "Decode", "Encode", "Find", "With", "Without");
        AssertMembers(typeof(FeedEntry), "PrefabName", "Keep");

        // A fuel-only selection is stored in the same format (PD3).
        Assert.Equal("Wood=5", Sel(("Wood", 5)).Encode());
    }

    private static string Describe(FeedSelection selection) =>
        string.Join("|", selection.Entries.Select(entry => entry.PrefabName + "=" + entry.Keep));

    private static void AssertMembers(Type type, params string[] expected)
    {
        MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType != MemberTypes.Constructor)
            .Where(member => !member.Name.StartsWith("get_", StringComparison.Ordinal) && !member.Name.StartsWith("set_", StringComparison.Ordinal))
            .ToArray();

        string[] names = members.Select(member => member.Name).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal), names);

        foreach (MemberInfo member in members)
        {
            Assert.False(Mentions(member, typeof(FeedStationShape)), type.Name + "." + member.Name + " mentions FeedStationShape.");
        }
    }

    private static bool Mentions(MemberInfo member, Type shape) => member switch
    {
        PropertyInfo property => property.PropertyType == shape,
        MethodInfo method => method.ReturnType == shape || method.GetParameters().Any(parameter => parameter.ParameterType == shape),
        _ => false,
    };
}
