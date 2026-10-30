namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-3 (contract 2.4, S3, S4, R8): the catalog lookup API and its immutability (ADR-GWIN-015).
/// </summary>
public sealed class SettingsCatalogApiTests
{
    // GW-U-098 (AC-3)
    [Theory]
    [MemberData(nameof(Section7Table.IndexData), MemberType = typeof(Section7Table))]
    public void GetAndFindReturnTheSameInstanceAsTheCatalogRow(int index)
    {
        string key = Section7Table.Rows[index].Key;

        Assert.Same(SettingsCatalog.All[index], SettingsCatalog.Get(key));
        Assert.Same(SettingsCatalog.All[index], SettingsCatalog.Find(key));
    }

    // GW-U-098 (AC-3)
    [Fact]
    public void RepeatedReadsReturnTheSameCollectionsAndInstances()
    {
        Assert.Same(SettingsCatalog.All, SettingsCatalog.All);
        Assert.Same(SettingsCatalog.HostSynced, SettingsCatalog.HostSynced);
        Assert.Same(SettingsCatalog.DepositNeverItemsDefault, SettingsCatalog.DepositNeverItemsDefault);
        Assert.Same(SettingsCatalog.All[0], SettingsCatalog.All[0]);
    }

    // GW-U-098 (S3)
    [Fact]
    public void FindIsOrdinalAndReturnsNullForUnknownKeys()
    {
        Assert.Null(SettingsCatalog.Find("craftfrombase"));
        Assert.Null(SettingsCatalog.Find("Unknown"));
    }

    // GW-U-098 (R8)
    [Fact]
    public void GetThrowsArgumentExceptionForAnUnknownKey()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => SettingsCatalog.Get("Unknown"));

        Assert.Equal("key", ex.ParamName);
    }

    // GW-U-098 (R8)
    [Fact]
    public void FindAndGetRejectNullKeysWithArgumentNullException()
    {
        ArgumentNullException find = Assert.Throws<ArgumentNullException>(() => SettingsCatalog.Find(null!));
        Assert.Equal("key", find.ParamName);

        ArgumentNullException get = Assert.Throws<ArgumentNullException>(() => SettingsCatalog.Get(null!));
        Assert.Equal("key", get.ParamName);
    }

    // GW-U-098 (S4)
    [Fact]
    public void PublishedCollectionsAreReadOnlyCollections()
    {
        Assert.IsType<ReadOnlyCollection<SettingDefinition>>(SettingsCatalog.All);
        Assert.IsType<ReadOnlyCollection<SettingDefinition>>(SettingsCatalog.HostSynced);
        Assert.IsType<ReadOnlyCollection<string>>(SettingsCatalog.DepositNeverItemsDefault);
    }

    // GW-U-098 (S4)
    [Fact]
    public void WritingThroughACastCollectionThrowsNotSupported()
    {
        AssertReadOnly<SettingDefinition>(SettingsCatalog.All);
        AssertReadOnly<SettingDefinition>(SettingsCatalog.HostSynced);
        AssertReadOnly<string>(SettingsCatalog.DepositNeverItemsDefault);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> list)
    {
        IList<T> writable = (IList<T>)list;

        Assert.Throws<NotSupportedException>(() => writable.Add(default!));
        Assert.Throws<NotSupportedException>(() => writable.Clear());
        Assert.Throws<NotSupportedException>(() => writable[0] = default!);
    }
}
