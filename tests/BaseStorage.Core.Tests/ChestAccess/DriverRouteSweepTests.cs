namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>
/// GW-U-755, GW-U-780 and GW-U-786 (AC-1, AC-4, AC-5): the 1536-row observation grid of ADR-GWIN-058. The grid asserts
/// invariants (RA3, RA4, AC-5) rather than re-implementing Route.
/// </summary>
public sealed class DriverRouteSweepTests
{
    // GW-U-755: only the open request is sent, and only on the SendRequest route (RA3).
    [Fact]
    public void OnlyTheOpenRequestIsSentAndOnlyOnTheSendRequestRoute()
    {
        int rows = 0;
        foreach (ChestObservation o in Grid())
        {
            rows++;
            var (game, chest, clock, sink, driver) = Forced(o);

            ReservationRoute? route = driver.Begin(chest, "organize");

            int requests = game.Calls.Count(call => call.StartsWith("SendOpenRequest ", StringComparison.Ordinal));
            Assert.True(requests == (ChestAccessRules.Route(o) == ReservationRoute.SendRequest ? 1 : 0), $"request count wrong for grid row {rows}.");
            Assert.All(game.Calls, call => Assert.True(
                call.StartsWith("SendOpenRequest ", StringComparison.Ordinal)
                || call.StartsWith("ClaimOwnership ", StringComparison.Ordinal)
                || call.StartsWith("SetInUse ", StringComparison.Ordinal),
                $"unexpected command in grid row {rows}: {call}"));
            if (o.ViewValid)
            {
                Assert.Equal(ChestAccessRules.Route(o), route);
            }
        }

        Assert.Equal(1536, rows);
    }

    // GW-U-780: RA4, a listed and connected friend's chest is never claimed, and an in-use one gets no command at all.
    [Fact]
    public void AListedFriendsChestIsNeverClaimedAndAnInUseChestGetsNoCommand()
    {
        foreach (ChestObservation o in Grid())
        {
            var (game, chest, clock, sink, driver) = Forced(o);

            if (o.OwnerListed && o.OwnerConnected && o.OwnerSessionId == Erik)
            {
                driver.Begin(chest, "organize");
                Assert.DoesNotContain(game.Calls, call => call.StartsWith("ClaimOwnership ", StringComparison.Ordinal));
            }

            if (ChestAccessRules.IsInUseByOther(o))
            {
                ChestInUse? inUse = driver.QueryInUse(chest);
                Assert.NotNull(inUse);
                Assert.Equal(ChestAccessRules.HolderLabel(o.OwnerName, StringTable.English), inUse!.HolderName);

                driver.Begin(chest, "organize");
                Assert.Empty(game.Calls);
            }
        }
    }

    // GW-U-786: a valid, access-allowed chest that is the local one, or an ownerless or disconnected chest on the host, is granted with no request.
    [Fact]
    public void LocalOwnerAndHostOwnerlessChestsAreGrantedWithoutARequest()
    {
        foreach (ChestObservation o in Grid())
        {
            bool localOwner = o.OwnerSessionId == o.LocalSessionId;
            bool hostOwnerless = o.IsServer && (o.OwnerSessionId == 0 || !o.OwnerConnected);
            bool applies = o.ViewValid && o.LocalSessionId != 0 && o.AccessAllowed && (localOwner || hostOwnerless) && !ChestAccessRules.IsInUseByOther(o);
            if (!applies)
            {
                continue;
            }

            var (game, chest, clock, sink, driver) = Forced(o);

            driver.Begin(chest, "organize");

            Assert.DoesNotContain(game.Calls, call => call.StartsWith("SendOpenRequest ", StringComparison.Ordinal));
            Assert.Equal(ReservationState.Granted, driver.Find(Chest42)!.State);
        }
    }

    private static (FakeReservationGame Game, FakeChest Chest, ManualClock Clock, RecordingSink Sink, ReservationDriver<FakeChest> Driver) Forced(ChestObservation o)
    {
        var game = new FakeReservationGame { LocalSession = o.LocalSessionId, IsServer = o.IsServer, InWorld = true };
        FakeChest chest = game.AddChest(Chest42);
        chest.ViewValid = o.ViewValid;
        chest.AccessAllowed = o.AccessAllowed;
        chest.OpenLocally = o.OpenLocally;
        chest.InUseFlag = o.InUseFlag;
        chest.Owner = o.OwnerSessionId;
        chest.Forced = o;
        var clock = new ManualClock(Start);
        var sink = new RecordingSink();
        return (game, chest, clock, sink, new ReservationDriver<FakeChest>(game, clock, 1500, StringTable.English, sink));
    }

    /// <summary>The 1536 grid rows: 7 booleans, local session {0, Me}, owner {0, Me, Erik}, owner name {"Erik", null} (ADR-GWIN-058).</summary>
    private static IEnumerable<ChestObservation> Grid()
    {
        long[] locals = { 0L, Me };
        long[] owners = { 0L, Me, Erik };
        string?[] names = { "Erik", null };
        for (int mask = 0; mask < 128; mask++)
        {
            foreach (long local in locals)
            {
                foreach (long owner in owners)
                {
                    foreach (string? name in names)
                    {
                        yield return new ChestObservation(
                            (mask & 1) != 0,
                            (mask & 2) != 0,
                            (mask & 4) != 0,
                            (mask & 8) != 0,
                            local,
                            owner,
                            (mask & 16) != 0,
                            (mask & 32) != 0,
                            name,
                            (mask & 64) != 0);
                    }
                }
            }
        }
    }
}
