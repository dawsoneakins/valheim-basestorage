namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-9 (A3; contract 4.1, 5.2 AC-9 paragraph, 5.3): the supplied threshold decides, and the setting value flows through.</summary>
public sealed class AutoAssignSettingTests
{
    private static readonly (uint Id, AutoAssignOutcome Outcome, string? Name)[] Table25 =
    {
        (101, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (102, AutoAssignOutcome.Assigned, "Metals & Ores"),
        (103, AutoAssignOutcome.Assigned, "Metals & Ores"),
        (104, AutoAssignOutcome.Assigned, "Wood"),
        (105, AutoAssignOutcome.Overflow, null),
        (106, AutoAssignOutcome.Keep, null),
        (107, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (108, AutoAssignOutcome.Overflow, null),
        (109, AutoAssignOutcome.Overflow, null),
        (110, AutoAssignOutcome.Assigned, "Gear"),
        (111, AutoAssignOutcome.Assigned, "Wood"),
    };

    private static readonly (uint Id, AutoAssignOutcome Outcome, string? Name)[] Table70 =
    {
        (101, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (102, AutoAssignOutcome.Assigned, "Metals & Ores"),
        (103, AutoAssignOutcome.SourceOnly, null),
        (104, AutoAssignOutcome.SourceOnly, null),
        (105, AutoAssignOutcome.Overflow, null),
        (106, AutoAssignOutcome.Keep, null),
        (107, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (108, AutoAssignOutcome.Overflow, null),
        (109, AutoAssignOutcome.Overflow, null),
        (110, AutoAssignOutcome.SourceOnly, null),
        (111, AutoAssignOutcome.Assigned, "Wood"),
    };

    private static readonly (uint Id, AutoAssignOutcome Outcome, string? Name)[] Table76 =
    {
        (101, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (102, AutoAssignOutcome.SourceOnly, null),
        (103, AutoAssignOutcome.SourceOnly, null),
        (104, AutoAssignOutcome.SourceOnly, null),
        (105, AutoAssignOutcome.Overflow, null),
        (106, AutoAssignOutcome.Keep, null),
        (107, AutoAssignOutcome.AlreadyAssigned, "Wood"),
        (108, AutoAssignOutcome.Overflow, null),
        (109, AutoAssignOutcome.Overflow, null),
        (110, AutoAssignOutcome.SourceOnly, null),
        (111, AutoAssignOutcome.SourceOnly, null),
    };

    // GW-U-527
    [Theory]
    [InlineData("60 M,M,M,W,St", 60, "M,M,M,W,St", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("61 M,M,M,W,St", 61, "M,M,M,W,St", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("34 M,W,St", 34, "M,W,St", AutoAssignOutcome.SourceOnly, null)]
    [InlineData("33 M,W,St", 33, "M,W,St", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("0 M,W,W,St,F", 0, "M,W,W,St,F", AutoAssignOutcome.Assigned, "Wood")]
    [InlineData("-5 M,W", -5, "M,W", AutoAssignOutcome.Assigned, "Metals & Ores")]
    [InlineData("int.MinValue W", int.MinValue, "W", AutoAssignOutcome.Assigned, "Wood")]
    [InlineData("int.MaxValue W", int.MaxValue, "W", AutoAssignOutcome.SourceOnly, null)]
    public void TheSuppliedThresholdDecidesTheOutcome(
        string label,
        int threshold,
        string codes,
        AutoAssignOutcome outcome,
        string? name)
    {
        AutoAssignResult r = ResolveCodes(threshold, codes);

        Assert.Equal(threshold, r.AutoAssignThresholdPct);
        AssertChest(label, outcome, name, At(r, 201));
    }

    // GW-U-528
    [Theory]
    [InlineData(25)]
    [InlineData(70)]
    [InlineData(76)]
    public void BaseBAtOtherThresholdsGivesTheContractRows(int threshold)
    {
        AutoAssignResult r = ResolveB(threshold);

        AssertTable("threshold " + threshold, r, TableFor(threshold));
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
    }

    // GW-U-529
    [Fact]
    public void TheDefaultSettingValueFlowsIntoResolve()
    {
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        int threshold = settings.AutoAssignThresholdPct;

        AutoAssignResult r = ResolveWith(B(), Assignments(), threshold: threshold);

        Assert.Equal(50, r.AutoAssignThresholdPct);
        AssertTable("default setting", r, BaseTable50);
    }

    // GW-U-529
    [Fact]
    public void ChangingTheSettingToSeventyChangesTheOutcomeToTheSeventyRow()
    {
        var source = new FakeSettingsSource().WithInt(SettingKeys.AutoAssignThresholdPct, 70);
        var settings = new BaseStorageSettings(source);

        AutoAssignResult r = ResolveWith(B(), Assignments(), threshold: settings.AutoAssignThresholdPct);

        Assert.Equal(70, r.AutoAssignThresholdPct);
        AssertTable("setting 70", r, Table70);
    }

    private static (uint Id, AutoAssignOutcome Outcome, string? Name)[] TableFor(int threshold) => threshold switch
    {
        25 => Table25,
        70 => Table70,
        _ => Table76,
    };
}
