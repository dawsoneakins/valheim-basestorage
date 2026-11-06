namespace BaseStorage.Core.Tests.BaseMove;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Snapshots;

/// <summary>Base-move spec section 3, steps 1 and 6 to 9: both sides of the protocol, every timeout and disconnect, and the settle rule.</summary>
public sealed class MoveProtocolTests
{
    private const long Friend = 7L;
    private const long Other = 8L;
    private static readonly ContainerId ChestA = new ContainerId(1L, 1u);
    private static readonly ContainerId ChestB = new ContainerId(1L, 2u);
    private static readonly MoveSummary Summary = new MoveSummary(3, 1, 0, 0, new[] { "CopperOre" });

    [Fact]
    public void RequesterHappyPathReleasesOnlyAfterTheWrittenRevisionsArrive()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        Assert.True(session.Start(1, "Keep", 0));
        Assert.Equal(new[] { "request" }, port.Log);

        session.OnPreview(1, MoveRefusal.None, null, Summary);
        Assert.Equal(MoveRequesterState.Previewing, session.State);
        session.Confirm(1);
        Assert.Contains("holds", port.Log);
        port.Revisions[ChestA] = 10;
        session.OnHoldsFinished(new[] { new MoveChestRevision(ChestA, 10) }, 1);
        Assert.Contains("confirm", port.Log);
        Assert.DoesNotContain("release", port.Log);

        session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);
        Assert.Equal(MoveRequesterState.Settling, session.State);
        session.Tick(3);
        Assert.DoesNotContain("release", port.Log);

        port.Revisions[ChestA] = 11;
        session.Tick(3.5);
        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Equal(new[] { "release", "done" }, port.Log.Skip(port.Log.Count - 2));
    }

    [Fact]
    public void AfterTheConfirmATimeoutWarnsOnceAndNeverReleases()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);

        session.Tick(1000);
        session.Tick(2000);

        Assert.Equal(1, port.Log.Count(l => l == "waiting"));
        Assert.DoesNotContain("release", port.Log);
        Assert.Equal(MoveRequesterState.AwaitingWrite, session.State);
    }

    [Fact]
    public void WhileSettlingATimeoutWarnsAndAlateWriteStillFinishes()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);
        session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);

        session.Tick(1000);
        Assert.Contains("waiting", port.Log);
        Assert.DoesNotContain("release", port.Log);

        port.Revisions[ChestA] = 11;
        session.Tick(1001);
        Assert.Contains("release", port.Log);
        Assert.Contains("done", port.Log);
    }

    [Fact]
    public void AWriteThatLandedBeyondTheExpectedRevisionCountsAsArrived()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);
        port.Revisions[ChestA] = 40;
        session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Contains("done", port.Log);
    }

    [Fact]
    public void AChestThisGameUnloadedDoesNotBlockTheSettle()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);
        port.Revisions.Remove(ChestA);
        session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);

        Assert.Equal(MoveRequesterState.Idle, session.State);
    }

    [Fact]
    public void AHostCancelAfterTheConfirmReleasesAndExplains()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);

        session.OnCancelled(1, MoveRefusal.Changed, null);

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Contains("release", port.Log);
        Assert.Equal(MoveRefusal.Changed, port.LastRefusal);
    }

    [Fact]
    public void ACancelCannotUndoAWriteTheHostAlreadyReported()
    {
        var port = new RequesterPort();
        var session = ToAwaitingWrite(port);
        session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);

        session.OnCancelled(1, MoveRefusal.Failed, null);

        Assert.Equal(MoveRequesterState.Settling, session.State);
        Assert.DoesNotContain("release", port.Log);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void ADisconnectAtEachStepEndsTheSession(int step, bool held)
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);
        if (step >= 1)
        {
            session.OnPreview(1, MoveRefusal.None, null, Summary);
        }

        if (step >= 2)
        {
            session.Confirm(1);
        }

        if (step >= 3)
        {
            port.Revisions[ChestA] = 10;
            session.OnHoldsFinished(new[] { new MoveChestRevision(ChestA, 10) }, 1);
        }

        if (step >= 4)
        {
            session.OnDone(1, new[] { new MoveChestRevision(ChestA, 11) }, Summary, 2);
        }

        session.OnDisconnected();

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Equal(held, port.Log.Contains("release"));
    }

    [Fact]
    public void ARefusedPreviewEndsWithNothingHeld()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);

        session.OnPreview(1, MoveRefusal.PlayerAtBase, "Erik", null);

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Equal(MoveRefusal.PlayerAtBase, port.LastRefusal);
        Assert.Equal("Erik", port.LastPlayer);
        Assert.DoesNotContain("release", port.Log);
    }

    [Fact]
    public void NoPreviewInTimeEndsTheSession()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);

        session.Tick(4);
        Assert.Equal(MoveRequesterState.AwaitingPreview, session.State);
        session.Tick(5);

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Equal(MoveRefusal.TimedOut, port.LastRefusal);
    }

    [Fact]
    public void DecliningTheSendsACancelAndHoldsNothing()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);
        session.OnPreview(1, MoveRefusal.None, null, Summary);

        session.Decline();

        Assert.Equal(MoveRequesterState.Idle, session.State);
        Assert.Contains("cancel", port.Log);
        Assert.DoesNotContain("holds", port.Log);
    }

    [Fact]
    public void FailedHoldsReleaseAndNeverConfirm()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);
        session.OnPreview(1, MoveRefusal.None, null, Summary);
        session.Confirm(1);

        session.OnHoldsFinished(null, 2);

        Assert.Contains("release", port.Log);
        Assert.Contains("cancel", port.Log);
        Assert.DoesNotContain("confirm", port.Log);
    }

    [Fact]
    public void HoldsThatTakeTooLongReleaseAndNeverConfirm()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);
        session.OnPreview(1, MoveRefusal.None, null, Summary);
        session.Confirm(1);

        session.Tick(10);

        Assert.Contains("release", port.Log);
        Assert.Contains("cancel", port.Log);
        Assert.DoesNotContain("confirm", port.Log);
    }

    [Fact]
    public void MessagesForAnotherRequestAreIgnored()
    {
        var port = new RequesterPort();
        var session = new MoveRequesterSession(port, 5);
        session.Start(2, "Keep", 0);

        session.OnPreview(1, MoveRefusal.None, null, Summary);
        session.OnCancelled(1, MoveRefusal.Failed, null);

        Assert.Equal(MoveRequesterState.AwaitingPreview, session.State);
        Assert.False(session.Start(3, "Keep", 0));
    }

    [Fact]
    public void HostHappyPathWritesOnceAfterTheCopyCatchesUp()
    {
        var port = new HostPort();
        var host = ToAwaitingRevisions(port, requesterRevision: 10);
        port.Revisions[ChestA] = 9;

        host.Tick(2);
        Assert.Equal(0, port.Applies);

        port.Revisions[ChestA] = 10;
        host.Tick(2.5);

        Assert.Equal(1, port.Applies);
        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Equal(new[] { "end", "done" }, port.Log.Skip(port.Log.Count - 2));
    }

    [Fact]
    public void AChangedPlanCancelsWithoutWriting()
    {
        var port = new HostPort();
        port.Revisions[ChestA] = 10;
        port.NextFingerprint = 999;
        var host = ToAwaitingRevisions(port, requesterRevision: 10);

        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Equal(0, port.Applies);
        Assert.Equal(MoveRefusal.Changed, port.LastRefusal);
    }

    [Fact]
    public void AFailedReplanCancelsWithoutWriting()
    {
        var port = new HostPort();
        port.Revisions[ChestA] = 10;
        port.ReplanFails = true;
        ToAwaitingRevisions(port, requesterRevision: 10);

        Assert.Equal(0, port.Applies);
        Assert.Equal(MoveRefusal.Failed, port.LastRefusal);
    }

    [Fact]
    public void AFailedApplyIsReportedAsNothingWritten()
    {
        var port = new HostPort();
        port.Revisions[ChestA] = 10;
        port.ApplyFails = true;
        ToAwaitingRevisions(port, requesterRevision: 10);

        Assert.Equal(1, port.Applies);
        Assert.DoesNotContain("done", port.Log);
        Assert.Equal(MoveRefusal.Failed, port.LastRefusal);
    }

    [Fact]
    public void ACopyThatNeverCatchesUpTimesOutWithoutWriting()
    {
        var port = new HostPort();
        port.Revisions[ChestA] = 9;
        var host = ToAwaitingRevisions(port, requesterRevision: 10);

        host.Tick(100);

        Assert.Equal(0, port.Applies);
        Assert.Equal(MoveRefusal.TimedOut, port.LastRefusal);
        Assert.Equal(MoveHostState.Idle, host.State);
    }

    [Fact]
    public void AChestThatDisappearedNeverCountsAsCaughtUp()
    {
        var port = new HostPort();
        var host = ToAwaitingRevisions(port, requesterRevision: 10);

        host.Tick(2);
        Assert.Equal(0, port.Applies);
        host.Tick(100);
        Assert.Equal(MoveRefusal.TimedOut, port.LastRefusal);
    }

    [Fact]
    public void ASecondRequestIsRefusedAsBusyAndLeavesTheFirstAlone()
    {
        var port = new HostPort();
        var host = new MoveHostCoordinator(port, 60, 5);
        host.OnRequest(Friend, Request(1), 0);

        host.OnRequest(Other, Request(2), 0);

        Assert.Equal(MoveRefusal.Busy, port.LastRefusal);
        Assert.Equal(MoveHostState.Planning, host.State);
        Assert.Equal(1, port.Log.Count(l => l == "plan"));
    }

    [Fact]
    public void ARefusedPlanEndsTheMove()
    {
        var port = new HostPort();
        var host = new MoveHostCoordinator(port, 60, 5);
        host.OnRequest(Friend, Request(1), 0);

        host.OnPlanReady(MoveRefusal.PlayerAtBase, "Erik", 0, null, 1);

        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Contains("end", port.Log);
        Assert.Equal("Erik", port.LastPlayer);
    }

    [Fact]
    public void AnOpenPreviewExpires()
    {
        var port = new HostPort();
        var host = ToAwaitingConfirm(port);

        host.Tick(1000);

        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Contains("end", port.Log);
    }

    [Fact]
    public void AConfirmFromAnotherPeerOrRequestIsIgnored()
    {
        var port = new HostPort();
        var host = ToAwaitingConfirm(port);

        host.OnConfirm(Other, 1, Array.Empty<MoveChestRevision>(), 2);
        host.OnConfirm(Friend, 9, Array.Empty<MoveChestRevision>(), 2);

        Assert.Equal(MoveHostState.AwaitingConfirm, host.State);
    }

    [Fact]
    public void FailedHostHoldsCancelWithoutWriting()
    {
        var port = new HostPort();
        var host = ToAwaitingConfirm(port);
        host.OnConfirm(Friend, 1, Array.Empty<MoveChestRevision>(), 2);

        host.OnHostHoldsFinished(false, 3);

        Assert.Equal(0, port.Applies);
        Assert.Equal(MoveRefusal.TimedOut, port.LastRefusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheRequesterLeavingBeforeTheWriteEndsTheMoveWithNothingWritten(int step)
    {
        var port = new HostPort();
        port.Revisions[ChestA] = 1;
        var host = new MoveHostCoordinator(port, 60, 5);
        host.OnRequest(Friend, Request(1), 0);
        if (step >= 1)
        {
            host.OnPlanReady(MoveRefusal.None, null, HostPort.Fingerprint, Summary, 1);
        }

        if (step >= 2)
        {
            host.OnConfirm(Friend, 1, new[] { new MoveChestRevision(ChestA, 10) }, 2);
        }

        if (step >= 3)
        {
            host.OnHostHoldsFinished(true, 3);
        }

        host.OnPeerLeft(Other);
        Assert.NotEqual(MoveHostState.Idle, host.State);

        host.OnPeerLeft(Friend);

        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Contains("end", port.Log);
        Assert.Equal(0, port.Applies);
    }

    [Fact]
    public void ARequesterCancelEndsTheMove()
    {
        var port = new HostPort();
        var host = ToAwaitingConfirm(port);

        host.OnCancel(Other, 1);
        Assert.Equal(MoveHostState.AwaitingConfirm, host.State);
        host.OnCancel(Friend, 1);

        Assert.Equal(MoveHostState.Idle, host.State);
        Assert.Contains("end", port.Log);
    }

    [Fact]
    public void BothSidesTogetherLeaveTheSourceHeldUntilTheHostsWriteLands()
    {
        var hostPort = new HostPort();
        hostPort.Revisions[ChestA] = 10;
        var host = new MoveHostCoordinator(hostPort, 60, 5);
        var requesterPort = new RequesterPort();
        var requester = new MoveRequesterSession(requesterPort, 5);
        requesterPort.Revisions[ChestA] = 10;

        requester.Start(1, "Keep", 0);
        host.OnRequest(Friend, Request(1), 0);
        host.OnPlanReady(MoveRefusal.None, null, HostPort.Fingerprint, Summary, 0);
        requester.OnPreview(1, MoveRefusal.None, null, Summary);
        requester.Confirm(0);
        var held = new[] { new MoveChestRevision(ChestA, 10) };
        requester.OnHoldsFinished(held, 0);
        host.OnConfirm(Friend, 1, held, 0);
        host.OnHostHoldsFinished(true, 0);

        Assert.Equal(1, hostPort.Applies);
        requester.OnDone(1, hostPort.Written, Summary, 0);
        Assert.DoesNotContain("release", requesterPort.Log);

        requesterPort.Revisions[ChestA] = hostPort.Written[0].Revision;
        requester.Tick(1);
        Assert.Contains("release", requesterPort.Log);
    }

    private static MoveRequest Request(int id) => new MoveRequest(id, ChestB, 42L, "Keep");

    private static MoveRequesterSession ToAwaitingWrite(RequesterPort port)
    {
        var session = new MoveRequesterSession(port, 5);
        session.Start(1, "Keep", 0);
        session.OnPreview(1, MoveRefusal.None, null, Summary);
        session.Confirm(1);
        port.Revisions[ChestA] = 10;
        session.OnHoldsFinished(new[] { new MoveChestRevision(ChestA, 10) }, 1);
        return session;
    }

    private static MoveHostCoordinator ToAwaitingConfirm(HostPort port)
    {
        var host = new MoveHostCoordinator(port, 60, 5);
        host.OnRequest(Friend, Request(1), 0);
        host.OnPlanReady(MoveRefusal.None, null, HostPort.Fingerprint, Summary, 1);
        return host;
    }

    private static MoveHostCoordinator ToAwaitingRevisions(HostPort port, uint requesterRevision)
    {
        var host = ToAwaitingConfirm(port);
        host.OnConfirm(Friend, 1, new[] { new MoveChestRevision(ChestA, requesterRevision) }, 1);
        host.OnHostHoldsFinished(true, 1);
        return host;
    }

    private sealed class RequesterPort : IMoveRequesterPort
    {
        internal List<string> Log { get; } = new List<string>();

        internal Dictionary<ContainerId, uint> Revisions { get; } = new Dictionary<ContainerId, uint>();

        internal MoveRefusal LastRefusal { get; private set; }

        internal string? LastPlayer { get; private set; }

        public void SendRequest(int requestId, string destinationName) => Log.Add("request");

        public void ShowPreview(MoveSummary summary) => Log.Add("preview");

        public void BeginHolds() => Log.Add("holds");

        public void ReleaseHolds() => Log.Add("release");

        public void SendConfirm(int requestId, IReadOnlyList<MoveChestRevision> held) => Log.Add("confirm");

        public void SendCancel(int requestId) => Log.Add("cancel");

        public uint? LocalRevision(ContainerId id) => Revisions.TryGetValue(id, out var revision) ? revision : null;

        public void ShowDone(MoveSummary summary) => Log.Add("done");

        public void ShowRefused(MoveRefusal refusal, string? playerName)
        {
            LastRefusal = refusal;
            LastPlayer = playerName;
            Log.Add("refused");
        }

        public void ShowStillWaiting() => Log.Add("waiting");
    }

    private sealed class HostPort : IMoveHostPort
    {
        internal const ulong Fingerprint = 123UL;

        internal List<string> Log { get; } = new List<string>();

        internal Dictionary<ContainerId, uint> Revisions { get; } = new Dictionary<ContainerId, uint>();

        internal ulong NextFingerprint { get; set; } = Fingerprint;

        internal bool ReplanFails { get; set; }

        internal bool ApplyFails { get; set; }

        internal int Applies { get; private set; }

        internal MoveChestRevision[] Written { get; private set; } = Array.Empty<MoveChestRevision>();

        internal MoveRefusal LastRefusal { get; private set; }

        internal string? LastPlayer { get; private set; }

        public void BeginPlan(long peer, MoveRequest request) => Log.Add("plan");

        public void SendPreview(long peer, int requestId, MoveRefusal refusal, string? playerName, MoveSummary? summary)
        {
            LastRefusal = refusal;
            LastPlayer = playerName;
            Log.Add("preview");
        }

        public void BeginHostHolds(IReadOnlyList<MoveChestRevision> heldByRequester) => Log.Add("holds");

        public void EndMove() => Log.Add("end");

        public uint? LocalRevision(ContainerId id) => Revisions.TryGetValue(id, out var revision) ? revision : null;

        public ulong? Replan() => ReplanFails ? null : NextFingerprint;

        public IReadOnlyList<MoveChestRevision>? Apply()
        {
            Applies++;
            if (ApplyFails)
            {
                return null;
            }

            Written = new[] { new MoveChestRevision(ChestA, Revisions[ChestA] + 1) };
            return Written;
        }

        public void SendDone(long peer, int requestId, IReadOnlyList<MoveChestRevision> revisions, MoveSummary summary) => Log.Add("done");

        public void SendCancelled(long peer, int requestId, MoveRefusal refusal, string? playerName)
        {
            LastRefusal = refusal;
            LastPlayer = playerName;
            Log.Add("cancelled");
        }
    }
}
