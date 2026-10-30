namespace BaseStorage.Core.Tests.Organize.Running;

using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Ui;

/// <summary>GW-U-1375 (AC-4, contract 4.3 and 6): every stop reason has a key, and every key renders its English fragment.</summary>
public sealed class OrganizeRunTextTests
{
    // GW-U-1375
    [Theory]
    [InlineData(ExecutionStopReason.ReservationRefused, "organize.stop-reason.reservation-refused", "a chest refused the reservation")]
    [InlineData(ExecutionStopReason.ReservationTimedOut, "organize.stop-reason.reservation-timed-out", "no answer from a chest owner")]
    [InlineData(ExecutionStopReason.ReservationLost, "organize.stop-reason.reservation-lost", "a chest changed owner")]
    [InlineData(ExecutionStopReason.ChestUnavailable, "organize.stop-reason.chest-unavailable", "a chest is no longer available")]
    [InlineData(ExecutionStopReason.ChestChanged, "organize.stop-reason.chest-changed", "a chest changed during the run")]
    [InlineData(ExecutionStopReason.DestinationFull, "organize.stop-reason.destination-full", "chest full")]
    [InlineData(ExecutionStopReason.RollbackFailed, "organize.stop-reason.rollback-failed", "items could not be put back, check the log")]
    [InlineData(ExecutionStopReason.Aborted, "organize.stop-reason.aborted", "cancelled")]
    [InlineData(ExecutionStopReason.None, "organize.stop-reason.aborted", "cancelled")]
    [InlineData((ExecutionStopReason)77, "organize.stop-reason.aborted", "cancelled")]
    public void EveryStopReasonMapsToAKeyWithEnglishText(ExecutionStopReason reason, string key, string english)
    {
        Assert.Equal(key, OrganizeRunText.StopReasonKey(reason));
        Assert.Equal(english, StringTable.English.Get(key));
    }

    // GW-U-1375
    [Fact]
    public void TheLockLostKeyAndTheSummaryKeyRender()
    {
        Assert.Equal("organize.stop-reason.lock-lost", OrganizeRunText.LockLostKey);
        Assert.Equal("the base lock was lost", StringTable.English.Get(OrganizeRunText.LockLostKey));
        Assert.Equal("Organized 3 stacks", StringTable.English.Format(StringKeys.MessageOrganizeRunSummary, "Organized 3 stacks"));
    }
}
