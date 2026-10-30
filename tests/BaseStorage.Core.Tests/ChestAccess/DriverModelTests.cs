namespace BaseStorage.Core.Tests.ChestAccess;

using System.Globalization;
using System.Text;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>
/// GW-U-770 (AC-2, RA8, RA2): a seeded model check over random step sequences. After every step: a reservation that ends while
/// the adapter-set marker is up and the chest is still held locally has cleared the marker in that same step; CanWrite is never
/// true outside Writing; every new SetInUse true belongs to a non-SelfOpen grant route; and ActiveCount agrees with Find.
/// </summary>
public sealed class DriverModelTests
{
    private const int Seed = 20261027;
    private const int Sequences = 500;
    private const int MaxSteps = 12;
    private const string SetTrue = "SetInUse 3141592653:42 true";
    private const string SetFalse = "SetInUse 3141592653:42 false";

    private static readonly string[] StepNames =
    {
        "begin", "response-grant", "response-deny", "tick", "advance", "release", "write",
        "owner-me", "owner-erik", "open-local", "close-local", "view-lost", "leave-world", "interact",
    };

    // GW-U-770
    [Fact]
    public void RandomSequencesKeepTheReleaseAndWriteInvariants()
    {
        var random = new Random(Seed);
        int terminations = 0;
        for (int sequence = 0; sequence < Sequences; sequence++)
        {
            var game = new FakeReservationGame { LocalSession = Me, IsServer = random.Next(2) == 0, InWorld = true };
            FakeChest chest = game.AddChest(Chest42);
            chest.Owner = random.Next(4) switch { 0 => 0L, 1 => Erik, _ => Me };
            var clock = new ManualClock(Start);
            var sink = new RecordingSink();
            var driver = new ReservationDriver<FakeChest>(game, clock, 1500, StringTable.English, sink);
            var trace = new StringBuilder();

            int steps = 1 + random.Next(MaxSteps);
            for (int step = 0; step < steps; step++)
            {
                string name = step == 0 ? "begin" : StepNames[random.Next(StepNames.Length)];
                trace.Append(name).Append(' ');

                Reservation<string>? before = driver.Find(Chest42);
                bool wasActive = before is not null && !before.IsTerminal;
                bool markerUp = LastMarker(game) == true;
                int callsBefore = game.Calls.Count;

                Perform(name, game, chest, clock, driver);

                string context = "sequence " + sequence.ToString(CultureInfo.InvariantCulture) + ": " + trace;
                Reservation<string>? after = driver.Find(Chest42);
                ReservationRoute? route = driver.RouteOf(Chest42);

                // RA8: the step that ends a holding reservation clears the marker while the chest is still held locally.
                if (wasActive && markerUp && after is not null && after.IsTerminal
                    && chest.Owner == Me && chest.ViewValid && !chest.OpenLocally && game.InWorld)
                {
                    terminations++;
                    Assert.True(LastMarker(game) == false, "marker left set after a terminal reservation in " + context);
                    Assert.False(chest.InUseFlag, "in-use flag left set in " + context);
                }

                // RA2: CanWrite only in Writing.
                if (after is null || after.State != ReservationState.Writing)
                {
                    Assert.False(driver.CanWrite(Chest42), "CanWrite outside Writing in " + context);
                }

                // A new SetInUse true belongs to a grant route other than SelfOpen.
                if (game.Calls.Skip(callsBefore).Contains(SetTrue))
                {
                    Assert.True(
                        route is ReservationRoute.SendRequest or ReservationRoute.LocalOwner or ReservationRoute.HostClaim,
                        "SetInUse true without a grant route in " + context);
                }

                // ActiveCount agrees with Find.
                Assert.Equal(after is not null && !after.IsTerminal ? 1 : 0, driver.ActiveCount);
            }
        }

        Assert.True(terminations > 50, "the model never exercised a held termination: " + terminations.ToString(CultureInfo.InvariantCulture));
    }

    private static bool? LastMarker(FakeReservationGame game)
    {
        for (int i = game.Calls.Count - 1; i >= 0; i--)
        {
            if (game.Calls[i] == SetTrue)
            {
                return true;
            }

            if (game.Calls[i] == SetFalse)
            {
                return false;
            }
        }

        return null;
    }

    private static void Perform(string name, FakeReservationGame game, FakeChest chest, ManualClock clock, ReservationDriver<FakeChest> driver)
    {
        switch (name)
        {
            case "begin":
                driver.Begin(chest, "organize");
                break;
            case "response-grant":
                driver.OnOpenResponse(chest, true);
                break;
            case "response-deny":
                driver.OnOpenResponse(chest, false);
                break;
            case "tick":
                driver.Tick();
                break;
            case "advance":
                clock.Advance(TimeSpan.FromMilliseconds(400));
                break;
            case "release":
                driver.Release(Chest42);
                break;
            case "write":
                driver.BeginWrite(Chest42);
                break;
            case "owner-me":
                chest.Owner = Me;
                break;
            case "owner-erik":
                chest.Owner = Erik;
                break;
            case "open-local":
                chest.OpenLocally = true;
                break;
            case "close-local":
                chest.OpenLocally = false;
                break;
            case "view-lost":
                chest.ViewValid = false;
                break;
            case "leave-world":
                game.InWorld = false;
                break;
            default:
                driver.OnLocalInteract(chest, false, false, false);
                break;
        }
    }
}
