namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.ObjectModel;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-1 and AC-4.9 (contract 4.1, 4.3, 4.4, R8): the DepositNeverItems default and the ItemListText format, with the
/// contract examples verbatim (ADR-GWIN-015).
/// </summary>
public sealed class ItemListTextTests
{
    private const string DefaultText = "@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait";

    // GW-U-102 (AC-1)
    [Fact]
    public void DepositNeverItemsDefaultIsTheSevenEntryList()
    {
        string[] expected = { "@mead", "@ammo", "Hammer", "Hoe", "Cultivator", "FishingRod", "@bait" };

        Assert.Equal(7, SettingsCatalog.DepositNeverItemsDefault.Count);
        Assert.Equal(expected, SettingsCatalog.DepositNeverItemsDefault);
    }

    // GW-U-102 (AC-1)
    [Fact]
    public void DefaultListEqualsTheParsedCatalogDefaultAndRoundTripsToTheExactText()
    {
        SettingDefinition definition = SettingsCatalog.Get(SettingKeys.DepositNeverItems);

        Assert.Equal(SettingKind.ItemList, definition.Kind);
        Assert.Equal(DefaultText, definition.DefaultText);
        Assert.Equal(ItemListText.Parse(definition.DefaultText), SettingsCatalog.DepositNeverItemsDefault);
        Assert.Equal(DefaultText, ItemListText.Format(ItemListText.Parse(definition.DefaultText)));
    }

    // GW-U-103 (contract 4.4 examples)
    [Fact]
    public void ParseTrimsDropsEmptyEntriesKeepsDuplicatesAndKeepsCasing()
    {
        Assert.Equal(new[] { "Hammer", "@food", "Hammer" }, ItemListText.Parse(" Hammer , ,@food,,Hammer "));
        Assert.Equal(new[] { "hammer" }, ItemListText.Parse("hammer"));
    }

    // GW-U-103 (contract 4.4 examples)
    [Fact]
    public void ParseOfBlankTextGivesAnEmptyListNotNull()
    {
        Assert.Empty(ItemListText.Parse(""));
        Assert.Empty(ItemListText.Parse("   "));
    }

    // GW-U-103 (contract 4.4)
    [Fact]
    public void ParseReturnsAReadOnlyCollection()
    {
        Assert.IsType<ReadOnlyCollection<string>>(ItemListText.Parse("Hammer"));
    }

    // GW-U-103 (contract 4.4 examples)
    [Fact]
    public void FormatDropsEmptyEntriesTrimsTheRestAndJoinsWithCommas()
    {
        Assert.Equal("@food,Hammer", ItemListText.Format(new[] { "@food", " Hammer ", string.Empty }));
    }

    // GW-U-103 (R8): an entry that contains a comma is rejected with the exact parameter name.
    [Fact]
    public void FormatRejectsAnEntryThatContainsAComma()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ItemListText.Format(new[] { "Wood,Stone" }));

        Assert.Equal("entries", ex.ParamName);
    }

    // GW-U-103 (R8): a null entry is rejected with ArgumentException, not ArgumentNullException.
    [Fact]
    public void FormatRejectsANullEntryWithAnArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ItemListText.Format(new string[] { null! }));

        Assert.Equal("entries", ex.ParamName);
    }

    // GW-U-103 (R8)
    [Fact]
    public void ParseAndFormatRejectNullArgumentsWithTheirParameterNames()
    {
        ArgumentNullException parse = Assert.Throws<ArgumentNullException>(() => ItemListText.Parse(null!));
        Assert.Equal("text", parse.ParamName);

        ArgumentNullException format = Assert.Throws<ArgumentNullException>(() => ItemListText.Format(null!));
        Assert.Equal("entries", format.ParamName);
    }
}
