namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>
/// AC-5, AC-6 (ADR-GWIN-155, contract hotkey-hover-audit-contracts v1 section 2, HV1 to HV4): every chest and station hover state adds at
/// most one line to every vanilla hover text, and the state axes come from the product's own definitions. The measure is
/// <see cref="AddedLineViolation"/>. A chest or station state that adds two lines, a carriage return or a changed prefix is reported by name.
/// </summary>
public sealed class HoverOneLineAuditTests
{
    private const string Removed = "Removed Category";
    private const string VanillaMulti = "Chest\n[<color=yellow><b>$KEY_Use</b></color>] Open";

    // GW-U-2210 (AC-5): the chest base axis x the assignment axis x the vanilla axis; every combination adds at most one line.
    [Fact]
    public void EveryChestStateAddsAtMostOneLineToEveryVanillaText()
    {
        ContainerSnapshot c1 = C1();
        BaseResolution capped = FromChest(Benches(6), new[] { c1 }, c1, maxBaseStations: 2);
        Assert.True(capped.IsCapped, "The capped base must report IsCapped, or the audit does not cover the station cap.");

        var bases = new (string Name, BaseResolution Resolution)[]
        {
            ("not in base", BaseResolution.NotInBase),
            ("in base 1 station", FromChest(Benches(1), new[] { c1 }, c1)),
            ("in base 3 stations", FromChest(Benches(3), new[] { c1 }, c1)),
            ("in base capped", capped),
        };

        var violations = new List<string>();
        foreach ((string baseName, BaseResolution resolution) in bases)
        {
            foreach (CategoryAssignment? assignment in AssignmentAxis())
            {
                foreach ((string vanillaName, string? vanilla) in VanillaTexts())
                {
                    string line = HoverInspector.FormatChest(resolution, assignment);
                    Collect(violations, baseName + " / " + AssignmentName(assignment) + " / " + vanillaName, vanilla, HoverInspector.Append(vanilla, line));
                }
            }
        }

        Assert.True(violations.Count == 0, "Chest hover states that add more than one line: " + Describe(violations));
    }

    // GW-U-2211 (AC-5): the formatter's count, label and key parameters, each appended to each vanilla text.
    [Fact]
    public void TheChestFormatterAddsAtMostOneLineForEveryCountLabelAndKey()
    {
        var labels = new List<(string Name, string? Label)>();
        foreach (string name in DefaultCategories.Catalog.CategoryNames)
        {
            labels.Add((name, name));
        }

        labels.Add(("Keep", StringTable.English.Get(StringKeys.ChestCategoryKeep)));
        labels.Add(("removed category", Removed));
        labels.Add(("hostile", "Line one\nLine two\r\n"));
        labels.Add(("null", null));

        (string Name, string? Key)[] keys = { ("null", null), ("F7", "F7") };
        int[] counts = { 0, 1, 3, 2 };

        var violations = new List<string>();
        foreach (int count in counts)
        {
            foreach ((string labelName, string? label) in labels)
            {
                foreach ((string keyName, string? key) in keys)
                {
                    string line = HoverLineFormatter.Chest(count, label, key);
                    foreach ((string vanillaName, string? vanilla) in VanillaTexts())
                    {
                        Collect(violations, "count " + count + " / label " + labelName + " / key " + keyName + " / " + vanillaName, vanilla, HoverInspector.Append(vanilla, line));
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, "Chest formatter inputs that add more than one line: " + Describe(violations));
    }

    // GW-U-2212 (AC-5): the station states (not in base, chest counts 0, 1 and 5, tombstone and vehicle) and the station formatter, each appended to each vanilla text.
    [Fact]
    public void EveryStationStateAddsAtMostOneLineToEveryVanillaText()
    {
        var lines = new List<(string Name, string Line)>
        {
            ("not in base", HoverInspector.FormatStation(BaseResolution.NotInBase)),
        };

        foreach (int count in new[] { 0, 1, 5 })
        {
            BaseResolution resolution = FromStation(Benches(1), ChestsForCount(count), 17u);
            lines.Add(("in base with " + count + " chests", HoverInspector.FormatStation(resolution)));
        }

        BaseResolution tombstoneAndVehicle = FromStation(Benches(1), new[] { C1(), Cart(), Grave() }, 17u);
        lines.Add(("tombstone and vehicle", HoverInspector.FormatStation(tombstoneAndVehicle)));

        foreach (int count in new[] { 0, 1, 5 })
        {
            lines.Add(("formatter " + count, HoverLineFormatter.Station(count)));
        }

        var violations = new List<string>();
        foreach ((string stateName, string line) in lines)
        {
            foreach ((string vanillaName, string? vanilla) in VanillaTexts())
            {
                Collect(violations, stateName + " / " + vanillaName, vanilla, HoverInspector.Append(vanilla, line));
            }
        }

        Assert.True(violations.Count == 0, "Station hover states that add more than one line: " + Describe(violations));
    }

    // GW-U-2213 (AC-5): a no-base station and null or empty vanilla text add nothing.
    [Fact]
    public void ANoBaseStationAndANullOrEmptyVanillaTextAddNothing()
    {
        Assert.Equal(string.Empty, HoverInspector.FormatStation(BaseResolution.NotInBase));
        Assert.Equal(VanillaMulti, HoverInspector.Append(VanillaMulti, string.Empty));

        string line = HoverInspector.FormatChest(BaseResolution.NotInBase, null);
        Assert.Null(HoverInspector.Append(null, line));
        Assert.Equal(string.Empty, HoverInspector.Append(string.Empty, line));
    }

    // GW-U-2214 (AC-6): the assignment axis is the product cycle order plus one unknown category, built from DefaultCategories.Catalog.
    [Fact]
    public void TheAssignmentAxisIsTheProductCycleOrderPlusAnUnknownCategory()
    {
        CategoryCatalog catalog = DefaultCategories.Catalog;
        List<CategoryAssignment?> axis = AssignmentAxis();

        Assert.Equal(catalog.CategoryNames.Count + 3, axis.Count);
        Assert.Null(axis[0]);
        Assert.True(axis[axis.Count - 2]?.IsKeep == true, "The entry before the unknown category must be Keep.");
        Assert.Equal(Removed, axis[axis.Count - 1]?.CategoryName);
        Assert.DoesNotContain(Removed, catalog.CategoryNames);

        foreach (string name in catalog.CategoryNames)
        {
            Assert.Single(axis, a => a is not null && a.CategoryName == name);
        }
    }

    // GW-U-2215 (AC-6): the state inputs are exactly the audited shapes. A new formatter parameter or state flag fails here until the axes are extended.
    [Fact]
    public void TheHoverStateInputsAreExactlyTheAuditedShapes()
    {
        Shape(new[] { typeof(BaseResolution), typeof(CategoryAssignment) }, ParameterTypes(typeof(HoverInspector), "FormatChest"), "HoverInspector.FormatChest parameters");
        Shape(new[] { typeof(BaseResolution) }, ParameterTypes(typeof(HoverInspector), "FormatStation"), "HoverInspector.FormatStation parameters");
        Shape(new[] { typeof(int), typeof(string), typeof(string) }, ParameterTypes(typeof(HoverLineFormatter), "Chest"), "HoverLineFormatter.Chest parameters");
        Shape(new[] { typeof(int) }, ParameterTypes(typeof(HoverLineFormatter), "Station"), "HoverLineFormatter.Station parameters");
        Shape(new[] { "IsCapped", "IsInBase" }, BoolProperties(typeof(BaseResolution)), "BaseResolution bool properties");
        Shape(new[] { "IsKeep" }, BoolProperties(typeof(CategoryAssignment)), "CategoryAssignment bool properties");
    }

    // GW-U-2216 (AC-5): the measure fails on two added lines, a carriage return and a changed prefix, and accepts one line and no line.
    [Fact]
    public void TheLineMeasureRejectsTwoAddedLinesACarriageReturnAndAChangedPrefix()
    {
        const string vanilla = "Chest\n[E] Open";

        Assert.NotNull(AddedLineViolation(vanilla, vanilla + "\nA\nB"));
        Assert.NotNull(AddedLineViolation(vanilla, vanilla + "\nA\r\nB"));
        Assert.NotNull(AddedLineViolation(vanilla, "X" + vanilla + "\nA"));
        Assert.Null(AddedLineViolation(vanilla, vanilla + "\nA"));
        Assert.Null(AddedLineViolation(vanilla, vanilla));
    }

    // The measure (contract section 2). Null or empty vanilla must come back unchanged. Otherwise the result keeps the vanilla text as its
    // prefix, the added part has at most one '\n', and the added part has no '\r', U+0085, U+2028 or U+2029. Returns null when it holds.
    private static string? AddedLineViolation(string? vanilla, string? result)
    {
        if (string.IsNullOrEmpty(vanilla))
        {
            return string.Equals(result, vanilla, StringComparison.Ordinal) ? null : "empty vanilla text must come back unchanged";
        }

        if (result is null || !result.StartsWith(vanilla, StringComparison.Ordinal))
        {
            return "the vanilla text is not kept as the prefix";
        }

        string added = result.Substring(vanilla.Length);
        if (added.Count(c => c == '\n') > 1)
        {
            return "adds more than one line";
        }

        if (added.IndexOfAny(new[] { '\r', (char)0x85, (char)0x2028, (char)0x2029 }) >= 0)
        {
            return "adds a carriage return or another line separator";
        }

        return null;
    }

    private static void Collect(List<string> found, string row, string? vanilla, string? result)
    {
        string? reason = AddedLineViolation(vanilla, result);
        if (reason is not null)
        {
            found.Add("[" + row + "] " + reason);
        }
    }

    // The vanilla axis (HV3): null, empty and a multi-line vanilla text.
    private static (string Name, string? Text)[] VanillaTexts() =>
        new (string Name, string? Text)[] { ("null", null), ("empty", string.Empty), ("multi-line", VanillaMulti) };

    // The assignment axis (HV1, HV4): Auto, each catalog category, Keep, and one category the catalog does not know.
    private static List<CategoryAssignment?> AssignmentAxis()
    {
        var axis = CategoryCycle.Order(DefaultCategories.Catalog).ToList();
        axis.Add(CategoryAssignment.ForCategory(Removed));
        return axis;
    }

    private static string AssignmentName(CategoryAssignment? assignment) =>
        assignment is null ? "Auto" : assignment.IsKeep ? "Keep" : assignment.CategoryName!;

    private static void Shape(Type[] expected, Type[] actual, string what)
    {
        Assert.True(expected.SequenceEqual(actual), what + " changed; extend the audit axes in HoverOneLineAuditTests to cover the new state.");
    }

    private static void Shape(string[] expected, string[] actual, string what)
    {
        Assert.True(expected.SequenceEqual(actual), what + " changed; extend the audit axes in HoverOneLineAuditTests to cover the new state.");
    }

    private static Type[] ParameterTypes(Type type, string name)
    {
        MethodInfo[] methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
            .ToArray();
        Assert.Single(methods);
        return methods[0].GetParameters().Select(p => p.ParameterType).ToArray();
    }

    private static string[] BoolProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool))
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    private static string Describe(IEnumerable<string> items) => string.Join("; ", items);
}
