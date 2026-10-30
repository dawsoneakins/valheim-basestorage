namespace BaseStorage.Core.Tests.Fakes;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Snapshots;
using Xunit;

/// <summary>
/// Asserts that an operation conserved items and respected limits. Pure function over snapshots; it does not depend on
/// <see cref="FakeContainerLayer"/>, so it works with any layer.
/// </summary>
public static class ConservationAssert
{
    /// <summary>
    /// Checks that per-item-type totals (prefab name and quality) match between <paramref name="before"/> and
    /// <paramref name="after"/>, and that no stack in <paramref name="after"/> exceeds its max stack size and no container
    /// exceeds its slot count. A container present in only one state counts as zero in the other. Every problem is
    /// reported in one failure.
    /// </summary>
    /// <param name="before">The world state before the operation, for example <c>layer.Capture()</c>.</param>
    /// <param name="after">The world state after the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="before"/> or <paramref name="after"/> is null.</exception>
    /// <exception cref="ArgumentException">A state has a null element or a duplicate container id (a setup error).</exception>
    /// <exception cref="Xunit.Sdk.XunitException">The totals differ, or a limit is broken in <paramref name="after"/>.</exception>
    public static void Holds(IEnumerable<ContainerSnapshot> before, IEnumerable<ContainerSnapshot> after)
    {
        if (before is null)
        {
            throw new ArgumentNullException(nameof(before));
        }

        if (after is null)
        {
            throw new ArgumentNullException(nameof(after));
        }

        List<ContainerSnapshot> beforeList = Validate(before, nameof(before));
        List<ContainerSnapshot> afterList = Validate(after, nameof(after));

        List<string> problems = new();
        problems.AddRange(TotalsProblems(beforeList, afterList));
        problems.AddRange(LimitProblems(afterList));

        if (problems.Count > 0)
        {
            string header = string.Format(
                CultureInfo.InvariantCulture, "Conservation check failed ({0} problems):", problems.Count);
            Assert.Fail(header + "\n" + string.Join("\n", problems));
        }
    }

    private static IEnumerable<string> TotalsProblems(List<ContainerSnapshot> before, List<ContainerSnapshot> after)
    {
        Dictionary<(string Prefab, int Quality), long> beforeTotals = Total(before);
        Dictionary<(string Prefab, int Quality), long> afterTotals = Total(after);

        IEnumerable<(string Prefab, int Quality)> keys = beforeTotals.Keys
            .Union(afterTotals.Keys)
            .OrderBy(k => k.Prefab, StringComparer.Ordinal)
            .ThenBy(k => k.Quality);
        foreach ((string Prefab, int Quality) key in keys)
        {
            beforeTotals.TryGetValue(key, out long beforeCount);
            afterTotals.TryGetValue(key, out long afterCount);
            long diff = afterCount - beforeCount;
            if (diff == 0)
            {
                continue;
            }

            string verb = diff < 0
                ? "lost " + (-diff).ToString(CultureInfo.InvariantCulture)
                : "duplicated " + diff.ToString(CultureInfo.InvariantCulture);
            yield return string.Format(
                CultureInfo.InvariantCulture,
                "item {0} (quality {1}): before {2}, after {3}, {4}",
                key.Prefab,
                key.Quality,
                beforeCount,
                afterCount,
                verb);
        }
    }

    private static IEnumerable<string> LimitProblems(List<ContainerSnapshot> state)
    {
        foreach (ContainerSnapshot c in state.OrderBy(x => x.Id))
        {
            string id = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", c.Id.UserId, c.Id.Id);
            for (int i = 0; i < c.Contents.Count; i++)
            {
                ItemStackSnapshot s = c.Contents[i];
                if (s.Count > s.MaxStack)
                {
                    yield return string.Format(
                        CultureInfo.InvariantCulture,
                        "stack over max: container {0} index {1} {2} {3}/{4}",
                        id,
                        i,
                        s.PrefabName,
                        s.Count,
                        s.MaxStack);
                }
            }

            if (c.Contents.Count > c.SlotCount)
            {
                yield return string.Format(
                    CultureInfo.InvariantCulture,
                    "container over capacity: {0} holds {1} stacks in {2} slots",
                    id,
                    c.Contents.Count,
                    c.SlotCount);
            }
        }
    }

    private static Dictionary<(string Prefab, int Quality), long> Total(List<ContainerSnapshot> state)
    {
        Dictionary<(string Prefab, int Quality), long> totals = new();
        foreach (ItemStackSnapshot s in state.SelectMany(c => c.Contents))
        {
            (string, int) key = (s.PrefabName, s.Quality);
            totals.TryGetValue(key, out long current);
            totals[key] = current + s.Count;
        }

        return totals;
    }

    private static List<ContainerSnapshot> Validate(IEnumerable<ContainerSnapshot> state, string paramName)
    {
        List<ContainerSnapshot> list = new();
        HashSet<ContainerId> seen = new();
        foreach (ContainerSnapshot c in state)
        {
            if (c is null)
            {
                throw new ArgumentException("Must not contain null elements.", paramName);
            }

            if (!seen.Add(c.Id))
            {
                throw new ArgumentException("Must not contain duplicate container ids.", paramName);
            }

            list.Add(c);
        }

        return list;
    }
}
