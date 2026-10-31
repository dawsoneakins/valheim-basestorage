namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

/// <summary>AC-1 and contract section 2: the three persistent keys are declared once, with the contract values.</summary>
public sealed class PersistenceKeysTests
{
    private const string KeysType = "PersistenceKeys";

    // GW-U-066
    [Fact]
    public void KeysHaveContractValues()
    {
        Type type = PersistenceContract.Resolve(KeysType);
        Assert.True(type.IsPublic && type.IsAbstract && type.IsSealed, "PersistenceKeys must be a public static class.");

        Assert.Equal("basestorage.cat", Key("ChestCategory"));
        Assert.Equal("basestorage.lock", Key("OrganizeLock"));
        Assert.Equal("basestorage.fav", Key("Favourites"));
        Assert.Equal("basestorage.feed", Key("StationFeed"));

        string[] keys = AllKeys();
        Assert.All(keys, key => Assert.StartsWith("basestorage.", key, StringComparison.Ordinal));
        Assert.Equal(keys, keys.Distinct(StringComparer.Ordinal).ToArray());
    }

    // GW-U-067
    [Fact]
    public void AllIsTheSingleReadOnlyDeclaration()
    {
        Type type = PersistenceContract.Resolve(KeysType);
        object all = PersistenceContract.Static(KeysType, "All")!;
        Assert.IsType<ReadOnlyCollection<string>>(all);

        string[] expected = ["basestorage.cat", "basestorage.lock", "basestorage.fav", "basestorage.feed"];
        Assert.Equal(expected, AllKeys());
        Assert.Equal(AllKeys(), AllKeys());

        Assert.Throws<NotSupportedException>(() => { ((IList<string>)all).Add("basestorage.x"); });
        Assert.Throws<NotSupportedException>(() => { ((IList<string>)all).Remove("basestorage.cat"); });
        Assert.Throws<NotSupportedException>(() => { ((IList<string>)all).Clear(); });
        Assert.Throws<NotSupportedException>(() => { ((IList<string>)all)[0] = "basestorage.x"; });

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        Assert.Equal(
            new[] { "ChestCategory", "Favourites", "OrganizeLock", "StationFeed" },
            fields.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.All(fields, f => Assert.True(f.IsLiteral, $"{f.Name} must be a const."));

        string[] constants = fields.Select(f => (string)f.GetValue(null)!).OrderBy(v => v, StringComparer.Ordinal).ToArray();
        Assert.Equal(AllKeys().OrderBy(v => v, StringComparer.Ordinal).ToArray(), constants);
    }

    private static string Key(string name) => (string)PersistenceContract.Static(KeysType, name)!;

    private static string[] AllKeys() =>
        ((IEnumerable)PersistenceContract.Static(KeysType, "All")!).Cast<string>().ToArray();
}
