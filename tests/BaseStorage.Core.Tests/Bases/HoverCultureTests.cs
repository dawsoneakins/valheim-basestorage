namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Globalization;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>AC-4 (ADR-GWIN-032): the hover lines do not depend on the culture, so no group separator or local wording appears.</summary>
public sealed class HoverCultureTests
{
    // GW-U-549 (AC-4): the same lines under five cultures, with CurrentCulture and CurrentUICulture both set and then restored.
    [Theory]
    [InlineData("InvariantCulture", "")]
    [InlineData("en-US", "en-US")]
    [InlineData("de-DE", "de-DE")]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("ar-SA", "ar-SA")]
    public void TheLinesAreTheSameUnderEveryCulture(string row, string cultureName)
    {
        CultureInfo culture = string.IsNullOrEmpty(cultureName) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            ContainerSnapshot[] thousand = ChestsForCount(1000);
            BaseResolution station = FromStation(Benches(1), thousand, 17u);
            AssertRow(row, "[Base: 1000 chests]", HoverInspector.FormatStation(station));

            ContainerSnapshot c1 = C1();
            BaseResolution chest = FromChest(Benches(2), new[] { c1 }, c1);
            Assert.Equal("[In base: 2 stations] [Iron]", HoverInspector.FormatChest(chest, CategoryAssignment.ForCategory("Iron")));

            var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
            CountingSnapshotSource source = Source(Benches(2), c1);
            var inspector = new HoverInspector(new BaseCache(source, clock));
            Assert.Equal("[In base: 2 stations] [Iron]", inspector.ChestLine(c1.Id, c1.Position, "c:Iron", Options()));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
