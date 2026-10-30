namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Tests.Logging;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The game double of contract 4.1 (S-E003-006, ADR-GWIN-132): records every call in <see cref="Calls"/>, answers the
/// abandonment and pre-flight reads from settable flags, and runs the commit callback (by default
/// <c>session.OnPlacePiece(true)</c>, which is what vanilla <c>PlacePiece</c> reaches). Not a test row.
/// </summary>
internal sealed class FakeBuildGame : IBuildGame
{
    private readonly FakeCraftWorld world;

    /// <summary>Initialises the double over a build world.</summary>
    /// <param name="world">The world whose kept clones the return and forget calls act on.</param>
    internal FakeBuildGame(FakeCraftWorld world)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <summary>Gets or sets the session the default commit calls back into.</summary>
    internal BuildSession? Session { get; set; }

    /// <summary>Gets the ordered call record.</summary>
    internal List<string> Calls { get; } = new();

    /// <summary>Gets the notice keys shown, in order.</summary>
    internal List<string> Notices { get; } = new();

    /// <summary>Gets the number of stacks the last ReturnTaken call put back.</summary>
    internal int LastReturned { get; private set; }

    /// <summary>Gets or sets the answer of IsStillSelected.</summary>
    internal bool StillSelected { get; set; } = true;

    /// <summary>Gets or sets the answer of PreFlight.</summary>
    internal bool PreFlightResult { get; set; } = true;

    /// <summary>Gets or sets the commit callback; null means the default callback calls OnPlacePiece(true).</summary>
    internal Action<BuildSelection>? CommitBody { get; set; }

    /// <inheritdoc />
    public bool IsStillSelected(BuildSelection selection)
    {
        Calls.Add("IsStillSelected");
        return StillSelected;
    }

    /// <inheritdoc />
    public bool PreFlight(BuildSelection selection)
    {
        Calls.Add("PreFlight");
        return PreFlightResult;
    }

    /// <inheritdoc />
    public void Commit(BuildSelection selection)
    {
        Calls.Add("Commit");
        if (CommitBody is not null)
        {
            CommitBody(selection);
            return;
        }

        Session!.OnPlacePiece(true);
    }

    /// <inheritdoc />
    public int ReturnTaken()
    {
        Calls.Add("ReturnTaken");
        LastReturned = world.ReturnAllTaken();
        return LastReturned;
    }

    /// <inheritdoc />
    public void ForgetTaken()
    {
        Calls.Add("ForgetTaken");
        world.ForgetTaken();
    }

    /// <inheritdoc />
    public void ShowNotice(string key)
    {
        Calls.Add("ShowNotice");
        Notices.Add(key);
    }
}

/// <summary>
/// The wired build rig of S-E003-006: the reservation fake, the clock, the sink, the build world, the driver, the layer, the
/// executor, the game double and the session, over the contract 5.9 fixture. Not a test row (ADR-GWIN-132). Its wiring mirrors
/// <c>CraftRig</c>; the build instance is separate from the craft instance (BF9).
/// </summary>
internal sealed class BuildRig
{
    private bool granted;

    /// <summary>Wires the fixture: c5 owned by the local player, c7 owned by Erik, the inventory with Wood 3.</summary>
    /// <param name="timeoutMs">The reserve timeout.</param>
    internal BuildRig(int timeoutMs = CraftFixtures.ReserveTimeoutMs)
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
        BuildGame = new FakeBuildGame(World);
        Session = new BuildSession(BuildGame, Executor, Sink);
        BuildGame.Session = Session;
        Piece = new object();
        Selection = new BuildSelection(Piece);
    }

    /// <summary>Gets the reservation game fake.</summary>
    internal FakeReservationGame Reservations { get; }

    /// <summary>Gets the manual clock at 3600 s.</summary>
    internal ManualClock Clock { get; }

    /// <summary>Gets the recording sink.</summary>
    internal RecordingSink Sink { get; }

    /// <summary>Gets the build world double.</summary>
    internal FakeCraftWorld World { get; }

    /// <summary>Gets chest c5 (owned by the local player).</summary>
    internal FakeChest Chest5 { get; }

    /// <summary>Gets chest c7 (owned by Erik).</summary>
    internal FakeChest Chest7 { get; }

    /// <summary>Gets the reservation driver.</summary>
    internal ReservationDriver<FakeChest> Driver { get; }

    /// <summary>Gets the container layer of the build instance (operation "craft").</summary>
    internal CraftContainerLayer<FakeChest> Layer { get; }

    /// <summary>Gets the consumption executor over the layer.</summary>
    internal ConsumptionExecutor Executor { get; }

    /// <summary>Gets the game double.</summary>
    internal FakeBuildGame BuildGame { get; }

    /// <summary>Gets the build session.</summary>
    internal BuildSession Session { get; }

    /// <summary>Gets the piece instance of every selection of this rig.</summary>
    internal object Piece { get; }

    /// <summary>Gets the selection of the fixture piece: Wood 15 and Resin 2.</summary>
    internal BuildSelection Selection { get; }

    /// <summary>Gets the requirement list of the fixture piece (Wood 15, Resin 2, multiplier 1).</summary>
    /// <returns>The requirements.</returns>
    internal static IReadOnlyList<CraftRequirement> PieceRequirements() => CraftFixtures.Requirements();

    /// <summary>Plans the fixture piece over the current views.</summary>
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

    /// <summary>Makes one placement attempt as the Plugin does: the guard is entered first, then the session decides.</summary>
    /// <param name="plan">The plan, or null when the materials are not there.</param>
    /// <returns>The run-vanilla answer.</returns>
    internal bool TryPlace(ConsumptionPlan? plan)
    {
        Session.EnterGuard();
        return Session.OnTryPlacePiece(Selection, plan);
    }

    /// <summary>Answers an open request as Erik would: the chest becomes owned by the local player and the response is granted.</summary>
    /// <param name="chest">The chest.</param>
    internal void Grant(FakeChest chest)
    {
        chest.Owner = CraftFixtures.Me;
        Driver.OnOpenResponse(chest, true);
    }

    /// <summary>Starts the fixture piece and ticks once: the take is pending, c5 granted at once, c7 requesting.</summary>
    internal void StartFixture()
    {
        TryPlace(Plan());
        Session.Tick();
    }

    /// <summary>Runs the fixture piece to the point where the executor has succeeded: start, tick, grant c7, tick, tick.</summary>
    internal void RunS1()
    {
        StartFixture();
        Grant(Chest7);
        Driver.Tick();
        Session.Tick();
    }

    /// <summary>Drives an execution to completion for the executor-level tests: answers the first open request with the grant.</summary>
    /// <param name="execution">The execution.</param>
    /// <returns>The result.</returns>
    internal ConsumptionResult Pump(ConsumptionExecution execution)
    {
        granted = false;
        for (int step = 0; step < 50; step++)
        {
            if (!granted && Reservations.Calls.Any(call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal)))
            {
                granted = true;
                Grant(Chest7);
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

    /// <summary>Builds an inventory view of the fixture: the player inventory at the origin.</summary>
    /// <param name="stacks">The stacks, in order.</param>
    /// <returns>The snapshot.</returns>
    internal static ContainerSnapshot InventoryOf(params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(CraftFixtures.Inventory, new WorldPosition(0f, 0f, 0f), 8, 4, stacks, ContainerFlags.None);

    /// <summary>Builds a chest view at a distance along the x axis from the player.</summary>
    /// <param name="id">The chest id.</param>
    /// <param name="distance">The distance from the player.</param>
    /// <param name="flags">The container flags.</param>
    /// <param name="stacks">The stacks, in order.</param>
    /// <returns>The snapshot.</returns>
    internal static ContainerSnapshot ChestOf(ContainerId id, float distance, ContainerFlags flags, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(id, new WorldPosition(distance, 0f, 0f), 5, 2, stacks, flags);

    /// <summary>Builds the contract 5.9 chest c5 (distance 5, Wood 8 and Resin 4, owned by the local player).</summary>
    /// <returns>The snapshot.</returns>
    internal static ContainerSnapshot C5Snapshot() => ChestOf(CraftFixtures.C5, 5f, ContainerFlags.PlayerBuilt, CraftFixtures.Wood(8), CraftFixtures.Resin(4));

    /// <summary>Builds the contract 5.9 chest c7 (distance 10, Wood 10, owned by Erik).</summary>
    /// <returns>The snapshot.</returns>
    internal static ContainerSnapshot C7Snapshot() => ChestOf(CraftFixtures.C7, 10f, ContainerFlags.PlayerBuilt, CraftFixtures.Wood(10));
}
