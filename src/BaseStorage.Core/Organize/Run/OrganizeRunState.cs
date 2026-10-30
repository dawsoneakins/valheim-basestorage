namespace BaseStorage.Core.Organize.Run;

/// <summary>The lifecycle state of one <see cref="OrganizeRunCoordinator"/> (contract section 2). The numeric values are contracted.</summary>
public enum OrganizeRunState
{
    /// <summary>No run is active; a tick does nothing.</summary>
    Idle = 0,

    /// <summary>The plan is made and the lock is held; the preview waits for Confirm or Cancel.</summary>
    Previewing = 1,

    /// <summary>The executor is stepped once per tick until it completes or stops.</summary>
    Running = 2,
}
