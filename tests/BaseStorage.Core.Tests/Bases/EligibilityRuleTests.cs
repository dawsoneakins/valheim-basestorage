namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.MembershipFixtures;

/// <summary>AC-3 to AC-10 (contract 2.1, 3, 7.3, 7.4): one test per exclusion rule, and the closed reason set.</summary>
public sealed class EligibilityRuleTests
{
    private static readonly (ContainerPurpose Purpose, bool ExcludeVehicles, bool KeepChestsCraftable)[] Columns =
    {
        (ContainerPurpose.Organize, true, true),
        (ContainerPurpose.Organize, false, false),
        (ContainerPurpose.CraftAvailability, true, true),
        (ContainerPurpose.CraftAvailability, false, false),
    };

    /// <summary>Maps an expected code from a row table to a reason: 0 means eligible (null).</summary>
    /// <param name="code">The expected code, 0 or a defined <see cref="ExclusionReason"/> value.</param>
    /// <returns>The reason, or null for 0.</returns>
    private static ExclusionReason? AsReason(int code) => code == 0 ? null : (ExclusionReason)code;

    /// <summary>Settings and Keep combinations for the 16-case theories: 2 purposes, 4 option pairs, isKeep on and off.</summary>
    public static TheoryData<ContainerPurpose, bool, bool, bool> SettingsAndKeepCases()
    {
        var data = new TheoryData<ContainerPurpose, bool, bool, bool>();
        foreach (ContainerPurpose purpose in new[] { ContainerPurpose.Organize, ContainerPurpose.CraftAvailability })
        {
            foreach ((bool excludeVehicles, bool keepChestsCraftable) in new[] { (true, true), (true, false), (false, true), (false, false) })
            {
                data.Add(purpose, excludeVehicles, keepChestsCraftable, false);
                data.Add(purpose, excludeVehicles, keepChestsCraftable, true);
            }
        }

        return data;
    }

    // GW-U-205 (AC-3): E-2 (no PlayerBuilt gives NotPlayerBuilt) and E-1 (PlayerBuilt gives null).
    [Fact]
    public void NotPlayerBuiltContainerIsExcludedAndPlayerBuiltIsEligible()
    {
        ExclusionReason? notBuilt = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.None), false, ContainerPurpose.Organize, new EligibilityOptions(true, true));
        ExclusionReason? built = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt), false, ContainerPurpose.Organize, new EligibilityOptions(true, true));

        Assert.Equal<ExclusionReason?>(ExclusionReason.NotPlayerBuilt, notBuilt);
        Assert.Null(built);
    }

    // GW-U-205 (AC-3): C7 (flags None) is (C7, NotPlayerBuilt) in all four 7.3 columns.
    [Theory]
    [InlineData(ContainerPurpose.Organize, true, true)]
    [InlineData(ContainerPurpose.Organize, false, false)]
    [InlineData(ContainerPurpose.CraftAvailability, true, true)]
    [InlineData(ContainerPurpose.CraftAvailability, false, false)]
    public void NotPlayerBuiltRowC7IsInEveryColumn(ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable)
    {
        EligibilityResult result = ContainerEligibility.Evaluate(
            MembersOfA(), Assignments(), purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        Assert.Contains((10L, 7u, ExclusionReason.NotPlayerBuilt), Excl(result));
    }

    // GW-U-206 (AC-4): E-3 and E-4. PlayerBuilt plus Vehicle is Vehicle exactly when ExcludeVehicles is on, for both purposes and both KC values.
    [Theory]
    [InlineData(ContainerPurpose.Organize, true, true, 2)]
    [InlineData(ContainerPurpose.Organize, true, false, 2)]
    [InlineData(ContainerPurpose.Organize, false, true, 0)]
    [InlineData(ContainerPurpose.Organize, false, false, 0)]
    [InlineData(ContainerPurpose.CraftAvailability, true, true, 2)]
    [InlineData(ContainerPurpose.CraftAvailability, true, false, 2)]
    [InlineData(ContainerPurpose.CraftAvailability, false, true, 0)]
    [InlineData(ContainerPurpose.CraftAvailability, false, false, 0)]
    public void VehicleIsExcludedExactlyWhenExcludeVehiclesIsOn(
        ContainerPurpose purpose,
        bool excludeVehicles,
        bool keepChestsCraftable,
        int expected)
    {
        ExclusionReason? actual = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle), false, purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        Assert.Equal(AsReason(expected), actual);
    }

    // GW-U-206 (AC-4): in 7.3, C4 is Vehicle in columns 1 and 3 and eligible in columns 2 and 4; C11 is NotPlayerBuilt with EV off.
    [Fact]
    public void CartChestFollowsExcludeVehiclesAcrossTheColumns()
    {
        EligibilityResult on = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, new EligibilityOptions(true, true));
        EligibilityResult off = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, new EligibilityOptions(false, false));

        Assert.Contains((10L, 4u, ExclusionReason.Vehicle), Excl(on));
        Assert.Contains(C4, off.Eligible);
        Assert.Contains((10L, 11u, ExclusionReason.NotPlayerBuilt), Excl(off));
    }

    // GW-U-207 (AC-5): E-5. Tombstone regardless of purpose, settings and isKeep (16 cases).
    [Theory]
    [MemberData(nameof(SettingsAndKeepCases))]
    public void TombstoneAlwaysWinsRegardlessOfSettings(ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable, bool isKeep)
    {
        ExclusionReason? actual = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt | ContainerFlags.Tombstone), isKeep, purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        Assert.Equal<ExclusionReason?>(ExclusionReason.Tombstone, actual);
    }

    // GW-U-207 (AC-5): in 7.3, C5 is (C5, Tombstone) in all four columns.
    [Fact]
    public void TombstoneRowC5IsInEveryColumn()
    {
        foreach ((ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable) in Columns)
        {
            EligibilityResult result = ContainerEligibility.Evaluate(
                MembersOfA(), Assignments(), purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

            Assert.Contains((10L, 5u, ExclusionReason.Tombstone), Excl(result));
        }
    }

    // GW-U-208 (AC-6): E-6. AccessDenied regardless of purpose, settings and isKeep (16 cases).
    [Theory]
    [MemberData(nameof(SettingsAndKeepCases))]
    public void AccessDeniedAlwaysWinsRegardlessOfSettings(ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable, bool isKeep)
    {
        ExclusionReason? actual = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied), isKeep, purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        Assert.Equal<ExclusionReason?>(ExclusionReason.AccessDenied, actual);
    }

    // GW-U-208 (AC-6): in 7.3, C6 is (C6, AccessDenied) in all four columns.
    [Fact]
    public void AccessDeniedRowC6IsInEveryColumn()
    {
        foreach ((ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable) in Columns)
        {
            EligibilityResult result = ContainerEligibility.Evaluate(
                MembersOfA(), Assignments(), purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

            Assert.Contains((10L, 6u, ExclusionReason.AccessDenied), Excl(result));
        }
    }

    // GW-U-209 (AC-7): E-7 (KC on) and E-8 (KC off). Keep blocks Organize whatever KeepChestsCraftable is.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KeepChestIsExcludedForOrganize(bool keepChestsCraftable)
    {
        ExclusionReason? actual = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt), true, ContainerPurpose.Organize, new EligibilityOptions(true, keepChestsCraftable));

        Assert.Equal<ExclusionReason?>(ExclusionReason.Keep, actual);
    }

    // GW-U-209 (AC-7, contract 3): in 7.3, C8 is (C8, Keep) in columns 1 and 2, and the named and Auto containers are eligible for Organize.
    [Fact]
    public void KeepInputShapeOnlyTreatsTheKeepKeyAsKeep()
    {
        EligibilityResult organizeOn = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, new EligibilityOptions(true, true));
        EligibilityResult organizeOff = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.Organize, new EligibilityOptions(false, false));

        Assert.Contains((10L, 8u, ExclusionReason.Keep), Excl(organizeOn));
        Assert.Contains((10L, 8u, ExclusionReason.Keep), Excl(organizeOff));
        Assert.Contains(C12, organizeOn.Eligible);
        Assert.Contains(C1, organizeOn.Eligible);
        Assert.Contains(C12, organizeOff.Eligible);
        Assert.Contains(C1, organizeOff.Eligible);

        foreach (EligibilityResult result in new[] { organizeOn, organizeOff })
        {
            Assert.All(result.Eligible, c => Assert.NotEqual(99L, c.Id.UserId));
            Assert.All(result.Excluded, e => Assert.NotEqual(99L, e.Container.Id.UserId));
        }
    }

    // GW-U-210 (AC-8): E-9 (KC on, so null) and E-10 (KC off, so Keep). EV has no effect on this rule.
    [Theory]
    [InlineData(true, true, 0)]
    [InlineData(true, false, 5)]
    [InlineData(false, true, 0)]
    [InlineData(false, false, 5)]
    public void KeepForCraftFollowsKeepChestsCraftable(bool excludeVehicles, bool keepChestsCraftable, int expected)
    {
        ExclusionReason? actual = ContainerEligibility.GetExclusionReason(
            Container(ContainerFlags.PlayerBuilt), true, ContainerPurpose.CraftAvailability, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

        Assert.Equal(AsReason(expected), actual);
    }

    // GW-U-210 (AC-8): in 7.3, C8 is eligible in column 3 and (C8, Keep) in column 4.
    [Fact]
    public void KeepChestColumnsThreeAndFourForCraft()
    {
        EligibilityResult craftOn = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.CraftAvailability, new EligibilityOptions(true, true));
        EligibilityResult craftOff = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), ContainerPurpose.CraftAvailability, new EligibilityOptions(false, false));

        Assert.Contains(C8, craftOn.Eligible);
        Assert.Contains((10L, 8u, ExclusionReason.Keep), Excl(craftOff));
    }

    // GW-U-211 (AC-9): (a) the reason set is closed: values 1..5 with the contracted names, and no zero member.
    [Fact]
    public void ExclusionReasonIsTheClosedSetOfFiveNamedValues()
    {
        int[] values = Enum.GetValues(typeof(ExclusionReason)).Cast<ExclusionReason>().Select(v => (int)v).ToArray();

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, values);
        Assert.Equal(new[] { "Tombstone", "Vehicle", "NotPlayerBuilt", "AccessDenied", "Keep" }, Enum.GetNames(typeof(ExclusionReason)));
        Assert.False(Enum.IsDefined(typeof(ExclusionReason), (ExclusionReason)0));
    }

    // GW-U-211 (AC-9): (b) in every 7.3 column, every reason is defined, the two lists are disjoint, and the counts sum to 11.
    [Fact]
    public void EveryColumnPartitionsTheElevenMembers()
    {
        var assignments = Assignments();
        foreach ((ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable) in Columns)
        {
            EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), assignments, purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

            Assert.All(result.Excluded, e => Assert.True(Enum.IsDefined(typeof(ExclusionReason), e.Reason)));
            var eligibleIds = result.Eligible.Select(c => c.Id).ToArray();
            var excludedIds = result.Excluded.Select(e => e.Container.Id).ToArray();
            Assert.Empty(eligibleIds.Intersect(excludedIds));
            Assert.Equal(11, eligibleIds.Length + excludedIds.Length);
        }
    }

    // GW-U-211 (AC-9): (c) GetExclusionReason agrees with Evaluate for every member: null when eligible, the listed Reason when excluded.
    [Fact]
    public void GetExclusionReasonAgreesWithEvaluateForEveryMember()
    {
        var assignments = Assignments();
        foreach ((ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable) in Columns)
        {
            var options = new EligibilityOptions(excludeVehicles, keepChestsCraftable);
            EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), assignments, purpose, options);

            foreach (ContainerSnapshot c in result.Eligible)
            {
                bool isKeep = assignments.TryGetValue(c.Id, out CategoryAssignment? a) && a.IsKeep;
                Assert.Null(ContainerEligibility.GetExclusionReason(c, isKeep, purpose, options));
            }

            foreach (ContainerExclusion e in result.Excluded)
            {
                bool isKeep = assignments.TryGetValue(e.Container.Id, out CategoryAssignment? a) && a.IsKeep;
                Assert.Equal<ExclusionReason?>(e.Reason, ContainerEligibility.GetExclusionReason(e.Container, isKeep, purpose, options));
            }
        }
    }

    // GW-U-213 (AC-10): the multi-rule containers of E-12..E-16. Each row gives the highest-precedence reason, and two calls agree.
    [Theory]
    [InlineData("E-12", ContainerFlags.Tombstone, false, ContainerPurpose.Organize, true, true, ExclusionReason.Tombstone)]
    [InlineData("E-13", ContainerFlags.Vehicle | ContainerFlags.Tombstone | ContainerFlags.AccessDenied, true, ContainerPurpose.Organize, true, true, ExclusionReason.Tombstone)]
    [InlineData("E-14", ContainerFlags.Vehicle | ContainerFlags.AccessDenied, true, ContainerPurpose.Organize, true, true, ExclusionReason.Vehicle)]
    [InlineData("E-15", ContainerFlags.Vehicle | ContainerFlags.AccessDenied, true, ContainerPurpose.Organize, false, true, ExclusionReason.NotPlayerBuilt)]
    [InlineData("E-16", ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, true, ContainerPurpose.Organize, true, true, ExclusionReason.AccessDenied)]
    public void MultiRuleContainerReportsTheHighestPrecedenceReason(
        string rowId,
        ContainerFlags flags,
        bool isKeep,
        ContainerPurpose purpose,
        bool excludeVehicles,
        bool keepChestsCraftable,
        ExclusionReason expected)
    {
        var options = new EligibilityOptions(excludeVehicles, keepChestsCraftable);
        ContainerSnapshot container = Container(flags);

        ExclusionReason? first = ContainerEligibility.GetExclusionReason(container, isKeep, purpose, options);
        ExclusionReason? second = ContainerEligibility.GetExclusionReason(container, isKeep, purpose, options);

        Assert.True(
            first == expected && second == expected,
            $"{rowId}: expected {expected} on both calls, got {first} then {second}.");
    }

    // GW-U-213 (AC-10): in 7.3, C10 is Tombstone in all four columns, and C11 is Vehicle in columns 1 and 3, NotPlayerBuilt in columns 2 and 4.
    [Fact]
    public void MultiRuleMembersC10AndC11AcrossTheColumns()
    {
        var expectedC11 = new[] { ExclusionReason.Vehicle, ExclusionReason.NotPlayerBuilt, ExclusionReason.Vehicle, ExclusionReason.NotPlayerBuilt };
        for (int i = 0; i < Columns.Length; i++)
        {
            (ContainerPurpose purpose, bool excludeVehicles, bool keepChestsCraftable) = Columns[i];
            EligibilityResult result = ContainerEligibility.Evaluate(MembersOfA(), Assignments(), purpose, new EligibilityOptions(excludeVehicles, keepChestsCraftable));

            Assert.Contains((10L, 10u, ExclusionReason.Tombstone), Excl(result));
            Assert.Contains((10L, 11u, expectedC11[i]), Excl(result));
        }
    }
}
