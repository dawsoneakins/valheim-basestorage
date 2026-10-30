namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>AC-2 (ADR-GWIN-032, persistence-contracts.md): the category or Keep shows on the chest line, and nothing shows for Auto.</summary>
public sealed class HoverChestCategoryTests
{
    // GW-U-539 (AC-2): FormatChest puts the category or Keep on the same line, and adds nothing for Auto.
    [Theory]
    [InlineData("two stations with Ores", 2, false, "Ores", "[In base: 2 stations] [Ores]")]
    [InlineData("six stations with Keep", 6, true, null, "[In base: 6 stations] [Keep]")]
    [InlineData("no base with Ores", 0, false, "Ores", "[Not in a base] [Ores]")]
    [InlineData("no base with Keep", 0, true, null, "[Not in a base] [Keep]")]
    [InlineData("one station and Auto", 1, false, null, "[In base: 1 station]")]
    [InlineData("two stations with Ore Metal", 2, false, "Ore Metal", "[In base: 2 stations] [Ore Metal]")]
    public void FormatChestPutsTheCategoryOrKeepOnTheSameLine(string row, int benches, bool keep, string? category, string expected)
    {
        ContainerSnapshot c1 = C1();
        BaseResolution resolution = FromChest(Benches(benches), new[] { c1 }, c1);
        CategoryAssignment? assignment = null;
        if (keep)
        {
            assignment = CategoryAssignment.Keep;
        }
        else if (category is not null)
        {
            assignment = CategoryAssignment.ForCategory(category);
        }

        string actual = HoverInspector.FormatChest(resolution, assignment);

        AssertRow(row, expected, actual);
        AssertSingleLine(actual);
    }

    // GW-U-540 (AC-2): ChestLine decodes the stored value; anything that is not "k" or "c:<name>" gives no value.
    [Theory]
    [InlineData("null is no value", null, "[In base: 2 stations]")]
    [InlineData("empty is no value", "", "[In base: 2 stations]")]
    [InlineData("an unknown prefix is no value", "x", "[In base: 2 stations]")]
    [InlineData("c: with no name is no value", "c:", "[In base: 2 stations]")]
    [InlineData("an upper-case K is no value", "K", "[In base: 2 stations]")]
    [InlineData("k is Keep", "k", "[In base: 2 stations] [Keep]")]
    [InlineData("c:Ores is Ores", "c:Ores", "[In base: 2 stations] [Ores]")]
    public void ChestLineDecodesTheStoredValue(string row, string? encoded, string expected)
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Benches(2), c1);
        var inspector = new HoverInspector(new BaseCache(source, clock));

        string actual = inspector.ChestLine(c1.Id, c1.Position, encoded, Options());

        AssertRow(row, expected, actual);
        AssertSingleLine(actual);
    }

    // GW-U-541 (AC-2): no cycle hint, no yellow markup, at most two brackets, and one line, for every value and every world.
    [Fact]
    public void NoCycleHintAndOneLineForEveryValueAndEveryWorld()
    {
        string?[] encodings = { null, string.Empty, "x", "c:", "K", "k", "c:Ores" };
        int[] benchCounts = { 1, 2, 0 };
        foreach (int benches in benchCounts)
        {
            foreach (string? encoded in encodings)
            {
                var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
                ContainerSnapshot c1 = C1();
                CountingSnapshotSource source = Source(Benches(benches), c1);
                var inspector = new HoverInspector(new BaseCache(source, clock));

                string line = inspector.ChestLine(c1.Id, c1.Position, encoded, Options());

                Assert.DoesNotContain("<color=yellow>", line);
                Assert.DoesNotContain("] Category", line);
                Assert.True(line.Count(c => c == '[') <= 2, $"More than two brackets in '{line}'.");
                AssertSingleLine(line);
            }
        }
    }
}
