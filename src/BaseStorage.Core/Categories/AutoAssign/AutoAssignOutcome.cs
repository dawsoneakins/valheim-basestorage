namespace BaseStorage.Core.Categories.AutoAssign;

/// <summary>What <see cref="AutoAssigner.Resolve"/> decided for one chest. The numeric values are part of the contract.</summary>
public enum AutoAssignOutcome
{
    /// <summary>The chest had a persisted category. It is reported as it is and never resolved or pooled.</summary>
    AlreadyAssigned = 0,

    /// <summary>The chest had a persisted Keep. It is reported as it is and never pooled.</summary>
    Keep = 1,

    /// <summary>The chest was unassigned and one category reached the threshold. It is the only outcome that is a change.</summary>
    Assigned = 2,

    /// <summary>The chest was unassigned, holds items, and no category reached the threshold. It stays unassigned as a source.</summary>
    SourceOnly = 3,

    /// <summary>The chest was unassigned and holds no items. It is placed in the overflow pool.</summary>
    Overflow = 4,
}
