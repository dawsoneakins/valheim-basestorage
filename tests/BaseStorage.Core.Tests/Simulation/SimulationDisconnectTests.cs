namespace BaseStorage.Core.Tests.Simulation;

using Xunit;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// GW-U-1461 (S-E005-004, AC-1, ADR-GWIN-114 decision 2, ADR-GWIN-115 H2): a disconnect retires the peer. A peer that dies mid-operation
/// comes back as a new peer with a fresh consumption executor, lock, layer and session id; its next craft does not hit the pending-execution
/// guard (CX10); the authority reclaims the dead session's holds; the dead session's lock expires by lease; and the dead peer never cancels.
/// </summary>
public sealed class SimulationDisconnectTests
{
    // GW-U-1461: one case crashed mid-craft, one crashed mid-Organize (the kind is passed as its enum value).
    [Theory]
    [InlineData((int)OperationKind.Craft)]
    [InlineData((int)OperationKind.Organize)]
    public void ADisconnectMidOperationRetiresThePeerAndItsNextCraftRuns(int during)
    {
        SimulationProbe.DisconnectReport report = SimulationProbe.CrashAndRejoin(Generate(0), 0, (OperationKind)during);

        Assert.NotEqual(report.OldSessionId, report.NewSessionId);
        Assert.NotSame(report.OldConsumption, report.NewConsumption);
        Assert.NotSame(report.OldLock, report.NewLock);
        Assert.NotSame(report.OldLayer, report.NewLayer);
        Assert.Null(report.NextCraftException);
        Assert.Equal(0, report.HoldsOfOldSessionAfterCrash);
        Assert.Equal(0, report.CancelsByDeadPeer);
        Assert.True(report.OldLockExpiredByLease, "the dead session's lock did not expire by lease");
    }
}
