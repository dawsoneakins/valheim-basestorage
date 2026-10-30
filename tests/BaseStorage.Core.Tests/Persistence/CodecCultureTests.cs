namespace BaseStorage.Core.Tests.Persistence;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// NFR P2 (contract 0.2): every codec gives the same text and the same values under en-US, de-DE, tr-TR, ar-SA and
/// InvariantCulture. The culture is set for the current thread in try/finally (ADR-GWIN-013).
/// </summary>
public sealed class CodecCultureTests
{
    // GW-U-077
    [Theory]
    [MemberData(nameof(CultureRows))]
    public void EncodeAndDecodeAreCultureIndependent(string cultureName)
    {
        CultureInfo culture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
        CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo previousUiCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CheckCategoryRows();
            CheckLockRows();
            CheckFavouriteRows();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            Thread.CurrentThread.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>The culture names of the NFR check, as MemberData rows.</summary>
    public static IEnumerable<object[]> CultureRows() => PersistenceFixtures.Cultures.Select(c => new object[] { c });

    private static void CheckCategoryRows()
    {
        foreach (var (name, encoded) in PersistenceFixtures.CategoryEncodings)
        {
            Assert.Equal(encoded, PersistenceContract.Encode("CategoryCodec", PersistenceContract.CategoryOrKeep(name)));
        }

        foreach (var (encoded, name) in PersistenceFixtures.CategoryDecodes)
        {
            object? decoded = PersistenceContract.Decode("CategoryCodec", encoded);

            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(PersistenceContract.CategoryOrKeep(name), decoded!);
        }
    }

    private static void CheckLockRows()
    {
        foreach (var row in PersistenceFixtures.LockEncodings)
        {
            object value = PersistenceContract.LockOf(row.HolderId, row.HolderName, row.Ticks);
            Assert.Equal(row.Encoded, PersistenceContract.Encode("OrganizeLockCodec", value));

            object? decoded = PersistenceContract.Decode("OrganizeLockCodec", row.Encoded);
            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
        }
    }

    private static void CheckFavouriteRows()
    {
        foreach (var row in PersistenceFixtures.FavouritesEncodings)
        {
            object value = PersistenceContract.FavouritesOf(row.Slots);
            Assert.Equal(row.Encoded, PersistenceContract.Encode("FavouritesCodec", value));

            object? decoded = PersistenceContract.Decode("FavouritesCodec", row.Encoded);
            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
        }
    }
}
