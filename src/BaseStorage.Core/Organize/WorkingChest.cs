namespace BaseStorage.Core.Organize;

using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;

/// <summary>One usable chest's mutable working state inside a single Plan call. Never escapes the call.</summary>
internal sealed class WorkingChest
{
    /// <summary>Creates the working state of one usable chest from its input contents.</summary>
    /// <param name="container">The input snapshot.</param>
    /// <param name="index">The position of the chest among the usable chests, in snapshot order.</param>
    /// <param name="outcome">The auto-assign outcome of the chest.</param>
    /// <param name="categoryName">The chest category, or null for a source or overflow chest.</param>
    internal WorkingChest(ContainerSnapshot container, int index, AutoAssignOutcome outcome, string? categoryName)
    {
        Container = container;
        Index = index;
        Outcome = outcome;
        CategoryName = categoryName;
        var contents = container.Contents;
        Stacks = new List<WorkingStack>(contents.Count);
        for (var i = 0; i < contents.Count; i++)
        {
            var stack = contents[i];
            Stacks.Add(new WorkingStack(new ItemKey(stack.PrefabName, stack.Quality, stack.MaxStack), stack.Count));
        }

        UsedSlots = contents.Count;
    }

    /// <summary>Gets the input snapshot.</summary>
    internal ContainerSnapshot Container { get; }

    /// <summary>Gets the position of the chest among the usable chests, in snapshot order.</summary>
    internal int Index { get; }

    /// <summary>Gets the auto-assign outcome of the chest.</summary>
    internal AutoAssignOutcome Outcome { get; }

    /// <summary>Gets the chest category: the assigned category, the claimed category, or null.</summary>
    internal string? CategoryName { get; private set; }

    /// <summary>Gets a value indicating whether this overflow chest was claimed in the plan.</summary>
    internal bool Claimed { get; private set; }

    /// <summary>Gets the working stacks: the input stacks in Contents order, then the received stacks in arrival order.</summary>
    internal List<WorkingStack> Stacks { get; }

    /// <summary>Gets or sets the number of occupied slots.</summary>
    internal int UsedSlots { get; set; }

    /// <summary>Gets the role the chest plays in the plan.</summary>
    internal ChestRole Role
    {
        get
        {
            if (Outcome == AutoAssignOutcome.Overflow)
            {
                return Claimed ? ChestRole.ClaimedOverflow : ChestRole.UnusedOverflow;
            }

            return CategoryName is null ? ChestRole.Source : ChestRole.Category;
        }
    }

    /// <summary>Marks this overflow chest as claimed for one category (P11).</summary>
    /// <param name="categoryName">The category the chest now belongs to.</param>
    internal void Claim(string categoryName)
    {
        CategoryName = categoryName;
        Claimed = true;
    }
}
