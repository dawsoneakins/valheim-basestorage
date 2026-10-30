namespace BaseStorage.Core.Organize.Execution;

/// <summary>The lifecycle status of one <see cref="OrganizeExecutor"/> (contract 2.1). The numeric values are contracted.</summary>
public enum ExecutionStatus
{
    /// <summary>Constructed; <see cref="OrganizeExecutor.Step"/> has not been called.</summary>
    NotStarted = 0,

    /// <summary>At least one step ran and the plan is neither finished nor stopped.</summary>
    Running = 1,

    /// <summary>Terminal: every planned move completed.</summary>
    Completed = 2,

    /// <summary>Terminal: a failure or <see cref="OrganizeExecutor.Abort"/> ended the run early; the stop reason says why.</summary>
    Stopped = 3,
}
