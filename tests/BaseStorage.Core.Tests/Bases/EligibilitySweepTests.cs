namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;

/// <summary>AC-9, AC-10 (contract 2.1, 2): the exhaustive sweep over every rule combination against the independent rank table.</summary>
public sealed class EligibilitySweepTests
{
    /// <summary>One sweep case: a container with its flag bits, isKeep, purpose and settings, and the contract value it must give.</summary>
    private sealed class SweepCase
    {
        internal SweepCase(
            ContainerSnapshot container,
            bool isKeep,
            ContainerPurpose purpose,
            bool excludeVehicles,
            bool keepChestsCraftable,
            bool tombstone,
            bool vehicle,
            bool playerBuilt,
            bool accessDenied)
        {
            Container = container;
            IsKeep = isKeep;
            Purpose = purpose;
            ExcludeVehicles = excludeVehicles;
            KeepChestsCraftable = keepChestsCraftable;
            Tombstone = tombstone;
            Vehicle = vehicle;
            PlayerBuilt = playerBuilt;
            AccessDenied = accessDenied;
        }

        internal ContainerSnapshot Container { get; }

        internal bool IsKeep { get; }

        internal ContainerPurpose Purpose { get; }

        internal bool ExcludeVehicles { get; }

        internal bool KeepChestsCraftable { get; }

        internal bool Tombstone { get; }

        internal bool Vehicle { get; }

        internal bool PlayerBuilt { get; }

        internal bool AccessDenied { get; }

        internal ExclusionReason? Expected => EligibilityRankTable.Expected(
            Tombstone, Vehicle, PlayerBuilt, AccessDenied, IsKeep, Purpose, ExcludeVehicles, KeepChestsCraftable);

        internal string Describe() =>
            $"flags={Container.Flags}, isKeep={IsKeep}, purpose={Purpose}, ExcludeVehicles={ExcludeVehicles}, KeepChestsCraftable={KeepChestsCraftable}";
    }

    // GW-U-212 (AC-9, AC-10): 2^5 rule subsets x 2 purposes x 4 option pairs = 256 cases. InUseByOther is set on alternate cases (M8).
    // Each case is evaluated three times, at different points in the sweep, and each result must be a defined reason or null, and equal the rank table.
    [Fact]
    public void EveryRuleCombinationGivesADefinedReasonAndTheContractValue()
    {
        List<SweepCase> cases = BuildCases();
        Assert.Equal(256, cases.Count);

        var firstPass = new ExclusionReason?[cases.Count];
        for (int i = 0; i < cases.Count; i++)
        {
            firstPass[i] = Get(cases[i]);
        }

        var secondPass = new ExclusionReason?[cases.Count];
        for (int i = cases.Count - 1; i >= 0; i--)
        {
            secondPass[i] = Get(cases[i]);
        }

        for (int i = 0; i < cases.Count; i++)
        {
            SweepCase c = cases[i];
            ExclusionReason? thirdPass = Get(c);

            Assert.True(
                firstPass[i] is null || Enum.IsDefined(typeof(ExclusionReason), firstPass[i]!.Value),
                $"Case {i} returned an undefined reason. {c.Describe()}");
            Assert.True(
                firstPass[i] == c.Expected,
                $"Case {i} expected {c.Expected} and got {firstPass[i]}. {c.Describe()}");
            Assert.True(
                secondPass[i] == firstPass[i] && thirdPass == firstPass[i],
                $"Case {i} is not fixed: {firstPass[i]}, {secondPass[i]}, {thirdPass}. {c.Describe()}");
        }
    }

    private static ExclusionReason? Get(SweepCase c) =>
        ContainerEligibility.GetExclusionReason(c.Container, c.IsKeep, c.Purpose, new EligibilityOptions(c.ExcludeVehicles, c.KeepChestsCraftable));

    private static List<SweepCase> BuildCases()
    {
        var purposes = new[] { ContainerPurpose.Organize, ContainerPurpose.CraftAvailability };
        var optionPairs = new[] { (true, true), (true, false), (false, true), (false, false) };
        var cases = new List<SweepCase>();
        uint serial = 1;

        for (int mask = 0; mask < 32; mask++)
        {
            bool tombstone = (mask & 1) != 0;
            bool vehicle = (mask & 2) != 0;
            bool playerBuilt = (mask & 4) != 0;
            bool accessDenied = (mask & 8) != 0;
            bool isKeep = (mask & 16) != 0;

            foreach (ContainerPurpose purpose in purposes)
            {
                foreach ((bool excludeVehicles, bool keepChestsCraftable) in optionPairs)
                {
                    ContainerFlags flags = ContainerFlags.None;
                    flags |= tombstone ? ContainerFlags.Tombstone : ContainerFlags.None;
                    flags |= vehicle ? ContainerFlags.Vehicle : ContainerFlags.None;
                    flags |= playerBuilt ? ContainerFlags.PlayerBuilt : ContainerFlags.None;
                    flags |= accessDenied ? ContainerFlags.AccessDenied : ContainerFlags.None;
                    if (serial % 2 == 0)
                    {
                        flags |= ContainerFlags.InUseByOther;
                    }

                    var container = new ContainerSnapshot(
                        new ContainerId(30, serial), new WorldPosition(0f, 0f, 0f), 4, 2, Array.Empty<ItemStackSnapshot>(), flags);
                    cases.Add(new SweepCase(
                        container, isKeep, purpose, excludeVehicles, keepChestsCraftable, tombstone, vehicle, playerBuilt, accessDenied));
                    serial++;
                }
            }
        }

        return cases;
    }
}
