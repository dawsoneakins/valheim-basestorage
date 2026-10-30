namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>GW-U-569: the result is immutable: read-only lists, set-less properties, no public constructors and read-only fields (6.8, R4).</summary>
public sealed class OrganizeImmutabilityTests
{
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-569
    [Fact]
    public void EveryPlanListIsAReadOnlyCollectionThatRejectsIListAdd()
    {
        OrganizePlan plan = Run(E());

        Assert.IsType<ReadOnlyCollection<PlannedMove>>(plan.Moves);
        Assert.Throws<NotSupportedException>(() => ((IList<PlannedMove>)plan.Moves).Add(default!));
        Assert.IsType<ReadOnlyCollection<NoRoomEntry>>(plan.NoRoom);
        Assert.Throws<NotSupportedException>(() => ((IList<NoRoomEntry>)plan.NoRoom).Add(default!));
        Assert.IsType<ReadOnlyCollection<SkippedChest>>(plan.Skipped);
        Assert.Throws<NotSupportedException>(() => ((IList<SkippedChest>)plan.Skipped).Add(default!));
        Assert.IsType<ReadOnlyCollection<ChestLayout>>(plan.Layouts);
        Assert.Throws<NotSupportedException>(() => ((IList<ChestLayout>)plan.Layouts).Add(default!));
        Assert.IsType<ReadOnlyCollection<ChestLayout>>(plan.ClaimedOverflow);
        Assert.Throws<NotSupportedException>(() => ((IList<ChestLayout>)plan.ClaimedOverflow).Add(default!));
    }

    // GW-U-569
    [Fact]
    public void EveryLayoutStackListIsAReadOnlyCollectionThatRejectsIListAdd()
    {
        OrganizePlan plan = Run(E());

        Assert.All(plan.Layouts, layout =>
        {
            Assert.IsType<ReadOnlyCollection<ItemStackSnapshot>>(layout.Stacks);
            Assert.Throws<NotSupportedException>(() => ((IList<ItemStackSnapshot>)layout.Stacks).Add(default!));
        });
    }

    // GW-U-569 (reflection)
    [Fact]
    public void EveryPublicPropertyOfTheEightTypesHasNoSetter()
    {
        var offenders = new List<string>();
        foreach (Type type in ContractTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(PublicMembers))
            {
                if (property.SetMethod is not null)
                {
                    offenders.Add(type.Name + "." + property.Name);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-569 (reflection)
    [Fact]
    public void TheFiveResultClassesHaveNoPublicConstructor()
    {
        Type[] results = { typeof(OrganizePlan), typeof(PlannedMove), typeof(NoRoomEntry), typeof(SkippedChest), typeof(ChestLayout) };

        string[] offenders = results
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(type => type.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-569 (reflection)
    [Fact]
    public void EveryInstanceFieldOfTheClassesIsReadOnly()
    {
        var offenders = new List<string>();
        foreach (Type type in ContractTypes().Where(t => !t.IsEnum))
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!field.IsInitOnly)
                {
                    offenders.Add(type.Name + "." + field.Name);
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static Type[] ContractTypes() => new[]
    {
        typeof(OrganizePlanner),
        typeof(OrganizePlan),
        typeof(PlannedMove),
        typeof(NoRoomEntry),
        typeof(SkippedChest),
        typeof(SkipReason),
        typeof(ChestLayout),
        typeof(ChestRole),
    };
}
