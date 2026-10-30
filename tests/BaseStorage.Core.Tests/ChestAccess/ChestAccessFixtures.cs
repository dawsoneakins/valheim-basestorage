namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// Fixtures of the S-E005-002 ChestAccess tests: the contract 2.5 observation defaults and the contract 8.1 driver setup.
/// Not a test row (ADR-GWIN-058).
/// </summary>
internal static class ChestAccessFixtures
{
    /// <summary>The local session of the contract examples.</summary>
    internal const long Me = 3141592653L;

    /// <summary>The friend session of the contract examples.</summary>
    internal const long Erik = 2718281828L;

    /// <summary>The chest id of the contract examples: 3141592653:42.</summary>
    internal static readonly ContainerId Chest42 = new ContainerId(3141592653L, 42u);

    /// <summary>The start of the manual clock: 3600 seconds.</summary>
    internal static readonly TimeSpan Start = TimeSpan.FromSeconds(3600);

    /// <summary>Builds an observation with the contract 2.5 defaults, changed by the named arguments.</summary>
    /// <param name="viewValid">The view is valid.</param>
    /// <param name="accessAllowed">Vanilla access is allowed.</param>
    /// <param name="openLocally">The local UI has the chest open.</param>
    /// <param name="inUseFlag">The in-use flag is set.</param>
    /// <param name="localSessionId">The local session.</param>
    /// <param name="ownerSessionId">The owner session.</param>
    /// <param name="ownerListed">The owner is listed.</param>
    /// <param name="ownerConnected">The owner is connected.</param>
    /// <param name="ownerName">The owner name.</param>
    /// <param name="isServer">This peer is the host or single-player.</param>
    /// <returns>The observation.</returns>
    internal static ChestObservation Obs(
        bool viewValid = true,
        bool accessAllowed = true,
        bool openLocally = false,
        bool inUseFlag = false,
        long localSessionId = Me,
        long ownerSessionId = Erik,
        bool ownerListed = true,
        bool ownerConnected = true,
        string? ownerName = "Erik",
        bool isServer = false) =>
        new ChestObservation(
            viewValid,
            accessAllowed,
            openLocally,
            inUseFlag,
            localSessionId,
            ownerSessionId,
            ownerListed,
            ownerConnected,
            ownerName,
            isServer);

    /// <summary>Builds the contract 8.1 setup: a game as the local peer, one chest owned by Erik, a manual clock at 3600 s and a recording sink.</summary>
    /// <param name="isServer">True for the host or single-player setups.</param>
    /// <param name="timeoutMs">The reservation timeout.</param>
    /// <returns>The game, the chest, the clock, the sink and a fresh driver.</returns>
    internal static (FakeReservationGame Game, FakeChest Chest, ManualClock Clock, RecordingSink Sink, ReservationDriver<FakeChest> Driver) Fresh(bool isServer = false, int timeoutMs = 1500)
    {
        var game = new FakeReservationGame { LocalSession = Me, IsServer = isServer, InWorld = true };
        FakeChest chest = game.AddChest(Chest42);
        var clock = new ManualClock(Start);
        var sink = new RecordingSink();
        var driver = new ReservationDriver<FakeChest>(game, clock, timeoutMs, StringTable.English, sink);
        return (game, chest, clock, sink, driver);
    }

    /// <summary>Gets the text of every recorded Debug line, in order.</summary>
    /// <param name="sink">The sink.</param>
    /// <returns>The messages.</returns>
    internal static string[] Lines(RecordingSink sink) => sink.Entries.Select(entry => entry.Message).ToArray();
}
