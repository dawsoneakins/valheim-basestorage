namespace BaseStorage.Core.Tests.Simulation;

/// <summary>The kinds of operation a simulated peer runs.</summary>
internal enum OperationKind
{
    /// <summary>A craft: consumption plan, reserve every chest of the plan, verify, take, release (AC-3.4).</summary>
    Craft = 0,

    /// <summary>An Organize: base lock, plan, <c>OrganizeExecutor</c> steps, unlock (AC-4.8, AC-5.4).</summary>
    Organize = 1,

    /// <summary>A Deposit: base lock, Deposit plan, <c>OrganizeExecutor</c> steps, unlock (AC-4.9, AC-5.4).</summary>
    Deposit = 2,
}

/// <summary>How a simulated operation ended.</summary>
internal enum OperationOutcome
{
    /// <summary>Still running.</summary>
    Running = 0,

    /// <summary>The operation did everything it planned.</summary>
    Completed = 1,

    /// <summary>The craft could not be planned: the base does not hold the materials the peer sees.</summary>
    NotAvailable = 2,

    /// <summary>A reservation was denied; nothing was written.</summary>
    Denied = 3,

    /// <summary>A reservation stayed pending past the timeout; nothing was written.</summary>
    TimedOut = 4,

    /// <summary>A reservation was lost before or during the writes.</summary>
    Lost = 5,

    /// <summary>A chest's contents differed from the plan; nothing of that batch was written.</summary>
    Changed = 6,

    /// <summary>The executor stopped for another reason (destination full, chest unavailable, rollback failed).</summary>
    Stopped = 7,

    /// <summary>The base lock was held by another peer.</summary>
    LockRefused = 8,

    /// <summary>The base lock could not be renewed and the run was aborted.</summary>
    LockLost = 9,

    /// <summary>The plan had no move.</summary>
    Nothing = 10,

    /// <summary>The peer disconnected mid-operation; nothing more happens on its side.</summary>
    Crashed = 11,

    /// <summary>A write of the craft failed after verification: a model or Core bug, the runner fails on it.</summary>
    WriteFailed = 12,
}

/// <summary>One operation of a simulated peer, stepped once per frame. Not a test row.</summary>
internal abstract class PeerOperation
{
    /// <summary>Gets the kind.</summary>
    internal abstract OperationKind Kind { get; }

    /// <summary>Gets the outcome; Running until the operation ends.</summary>
    internal OperationOutcome Outcome { get; private protected set; }

    /// <summary>Gets a value indicating whether the operation has ended.</summary>
    internal bool IsDone => Outcome != OperationOutcome.Running;

    /// <summary>Gets a value indicating whether the operation currently holds the base lock.</summary>
    internal bool HoldsLock { get; private protected set; }

    /// <summary>Does one frame of work.</summary>
    internal abstract void Step();

    /// <summary>The peer disconnected: the operation is dropped where it stands, without releasing anything.</summary>
    internal void Crash()
    {
        Outcome = OperationOutcome.Crashed;
        HoldsLock = false;
    }

    /// <summary>A short description for the event log.</summary>
    internal abstract string Describe();
}
