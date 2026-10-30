namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The wired craft rig of S-E003-004: the reservation fake, the clock, the sink, the craft world, the driver, the layer, the
/// executor, the craft game and the session, over the contract 5.6 fixture. Not a test row (ADR-GWIN-124).
/// </summary>
internal sealed class CraftRig
{
    private bool granted;

    /// <summary>Wires the fixture: c5 owned by the local player, c7 owned by Erik, the inventory with Wood 3.</summary>
    /// <param name="timeoutMs">The reserve timeout.</param>
    internal CraftRig(int timeoutMs = CraftFixtures.ReserveTimeoutMs)
    {
        Reservations = new FakeReservationGame { LocalSession = CraftFixtures.Me, IsServer = false, InWorld = true };
        Clock = new ManualClock(CraftFixtures.Start);
        Sink = new RecordingSink();
        World = new FakeCraftWorld();
        World.SetInventory(CraftFixtures.Wood(3));
        Chest5 = Reservations.AddChest(CraftFixtures.C5);
        Chest5.Owner = CraftFixtures.Me;
        World.AddChest(Chest5, CraftFixtures.Wood(8), CraftFixtures.Resin(4));
        Chest7 = Reservations.AddChest(CraftFixtures.C7);
        World.AddChest(Chest7, CraftFixtures.Wood(10));
        Driver = new ReservationDriver<FakeChest>(Reservations, Clock, timeoutMs, StringTable.English, Sink);
        Layer = new CraftContainerLayer<FakeChest>(World, Driver);
        Executor = new ConsumptionExecutor(Layer, Clock, CraftFixtures.Settings(timeoutMs));
        CraftGame = new FakeCraftGame(World);
        Session = new CraftSession(CraftGame, Executor, Sink);
        CraftGame.Session = Session;
        Recipe = new object();
        Station = new object();
    }

    /// <summary>Gets the reservation game fake.</summary>
    internal FakeReservationGame Reservations { get; }

    /// <summary>Gets the manual clock at 3600 s.</summary>
    internal ManualClock Clock { get; }

    /// <summary>Gets the recording sink.</summary>
    internal RecordingSink Sink { get; }

    /// <summary>Gets the craft world double.</summary>
    internal FakeCraftWorld World { get; }

    /// <summary>Gets chest c5 (owned by the local player).</summary>
    internal FakeChest Chest5 { get; }

    /// <summary>Gets chest c7 (owned by Erik).</summary>
    internal FakeChest Chest7 { get; }

    /// <summary>Gets the reservation driver.</summary>
    internal ReservationDriver<FakeChest> Driver { get; }

    /// <summary>Gets the craft container layer.</summary>
    internal CraftContainerLayer<FakeChest> Layer { get; }

    /// <summary>Gets the consumption executor over the layer.</summary>
    internal ConsumptionExecutor Executor { get; }

    /// <summary>Gets the game double.</summary>
    internal FakeCraftGame CraftGame { get; }

    /// <summary>Gets the craft session.</summary>
    internal CraftSession Session { get; }

    /// <summary>Gets the Recipe instance of every selection of this rig.</summary>
    internal object Recipe { get; }

    /// <summary>Gets the CraftingStation instance of every selection of this rig.</summary>
    internal object Station { get; }

    /// <summary>Gets the selection of the fixture craft: Wood 15 and Resin 2, quality 1, multiplier 1.</summary>
    internal CraftSelection Selection => new CraftSelection(Recipe, null, Station, 1, 1);

    /// <summary>Plans the fixture recipe over the current views.</summary>
    /// <returns>The plan, or null.</returns>
    internal ConsumptionPlan? Plan() => CraftFixtures.PlanFor(
        World.Read(CraftFixtures.Inventory)!,
        World.Read(CraftFixtures.C5)!,
        World.Read(CraftFixtures.C7)!);

    /// <summary>Gets the stacks of a container as "prefab count" strings, in order.</summary>
    /// <param name="id">The container.</param>
    /// <returns>The descriptions.</returns>
    internal string[] Describe(ContainerId id) => World.ContentsOf(id).Select(stack => stack.PrefabName + " " + stack.Count).ToArray();

    /// <summary>Gets the reservation status the layer reports for a container.</summary>
    /// <param name="id">The container.</param>
    /// <returns>The status.</returns>
    internal ReservationStatus StatusOf(ContainerId id) => Layer.GetReservationStatus(id);

    /// <summary>Answers an open request as Erik would: the chest becomes owned by the local player and the response is granted.</summary>
    /// <param name="chest">The chest.</param>
    internal void Grant(FakeChest chest)
    {
        chest.Owner = CraftFixtures.Me;
        Driver.OnOpenResponse(chest, true);
    }

    /// <summary>Drives an execution to completion: answers the first open request with <paramref name="onRequest"/> and ticks the driver.</summary>
    /// <param name="execution">The execution.</param>
    /// <param name="onRequest">The answer to the first open request; grants chest c7 when null.</param>
    /// <returns>The result.</returns>
    internal ConsumptionResult Pump(ConsumptionExecution execution, Action<CraftRig>? onRequest = null)
    {
        granted = false;
        for (int step = 0; step < 50; step++)
        {
            if (!granted && Reservations.Calls.Any(call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal)))
            {
                granted = true;
                (onRequest ?? (rig => rig.Grant(rig.Chest7)))(this);
            }

            Driver.Tick();
            ConsumptionResult? result = execution.Poll();
            if (result is not null)
            {
                return result;
            }
        }

        throw new InvalidOperationException("The execution did not complete within 50 steps.");
    }

    /// <summary>Runs the fixture craft to the point where the executor has succeeded: start, tick, grant c7, tick, tick.</summary>
    internal void RunS1()
    {
        Session.OnDoCrafting(Selection, Plan());
        Session.Tick();
        Grant(Chest7);
        Driver.Tick();
        Session.Tick();
    }

    /// <summary>Runs the fixture craft with a commit callback.</summary>
    /// <param name="body">The commit callback.</param>
    internal void RunCommit(Action<CraftSelection> body)
    {
        CraftGame.CommitBody = body;
        RunS1();
    }
}
