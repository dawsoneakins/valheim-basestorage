namespace BaseStorage.Core.Tests.Organize.Execution;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// The executor test harness (contract organize-execution-contracts.md section 5): a plan from the S-E004-006 fixtures, a
/// <see cref="FakeContainerLayer"/> seeded with the same containers behind a <see cref="RecordingContainerLayer"/>, a manual clock,
/// a fake settings source, and the executor over them. Totals helpers compare live chests with layouts order-free. Not a test row.
/// </summary>
internal sealed class ExecutionHarness
{
    private ExecutionHarness(
        OrganizeFixtures.PlanInput input,
        OrganizePlan plan,
        FakeContainerLayer fake,
        RecordingContainerLayer recorder,
        ManualClock clock,
        FakeSettingsSource source,
        BaseStorageSettings settings,
        OrganizeExecutor executor,
        IReadOnlyList<ContainerSnapshot> before)
    {
        Input = input;
        Plan = plan;
        Fake = fake;
        Recorder = recorder;
        Clock = clock;
        Source = source;
        Settings = settings;
        Executor = executor;
        Before = before;
    }

    /// <summary>Gets the plan input.</summary>
    public OrganizeFixtures.PlanInput Input { get; }

    /// <summary>Gets the plan being executed.</summary>
    public OrganizePlan Plan { get; }

    /// <summary>Gets the fake layer that holds the chests and the injected faults.</summary>
    public FakeContainerLayer Fake { get; }

    /// <summary>Gets the recording decorator the executor talks to.</summary>
    public RecordingContainerLayer Recorder { get; }

    /// <summary>Gets the manual clock.</summary>
    public ManualClock Clock { get; }

    /// <summary>Gets the settings source, so a test can change MovesPerFrame between steps.</summary>
    public FakeSettingsSource Source { get; }

    /// <summary>Gets the settings read path over <see cref="Source"/>.</summary>
    public BaseStorageSettings Settings { get; }

    /// <summary>Gets the executor under test.</summary>
    public OrganizeExecutor Executor { get; }

    /// <summary>Gets the world captured right after the arrange step and before the first <c>Step()</c>.</summary>
    public IReadOnlyList<ContainerSnapshot> Before { get; }

    /// <summary>Gets the executor's result.</summary>
    public OrganizeExecutionResult Result => Executor.Result;

    /// <summary>
    /// Builds a harness: plans the input, seeds the fake with the input's containers, applies the arrange action (faults), captures
    /// the world, and creates the executor. The clock starts at 0.
    /// </summary>
    /// <param name="input">The plan input.</param>
    /// <param name="movesPerFrame">The MovesPerFrame value in the settings source.</param>
    /// <param name="reserveTimeoutMs">The ReserveTimeoutMs value in the settings source.</param>
    /// <param name="arrange">Optional faults or edits applied to the fake before the capture.</param>
    /// <param name="playerInventory">Optional player inventory for the fake layer.</param>
    /// <param name="omit">Chest serials to leave out of the fake although the plan knows them.</param>
    /// <returns>A new harness.</returns>
    public static ExecutionHarness For(
        OrganizeFixtures.PlanInput input,
        int movesPerFrame = 20,
        int reserveTimeoutMs = 1500,
        Action<FakeContainerLayer>? arrange = null,
        ContainerSnapshot? playerInventory = null,
        params uint[] omit)
    {
        OrganizePlan plan = OrganizeFixtures.Run(input);
        var fake = new FakeContainerLayer(playerInventory);
        foreach (ContainerSnapshot container in input.Containers)
        {
            if (playerInventory is not null && container.Id == playerInventory.Id)
            {
                continue;
            }

            if (!omit.Contains(container.Id.Id))
            {
                fake.AddChest(container);
            }
        }

        arrange?.Invoke(fake);
        IReadOnlyList<ContainerSnapshot> before = fake.Capture();

        var recorder = new RecordingContainerLayer(fake);
        var clock = new ManualClock(TimeSpan.Zero);
        FakeSettingsSource source = new FakeSettingsSource()
            .WithInt(SettingKeys.MovesPerFrame, movesPerFrame)
            .WithInt(SettingKeys.ReserveTimeoutMs, reserveTimeoutMs);
        var settings = new BaseStorageSettings(source);
        var executor = new OrganizeExecutor(plan, recorder, clock, settings);
        return new ExecutionHarness(input, plan, fake, recorder, clock, source, settings, executor, before);
    }

    /// <summary>Builds a harness over a named S-E004-006 fixture.</summary>
    /// <param name="fixture">A name from <see cref="OrganizeFixtures.AllFixtureNames"/>.</param>
    /// <param name="movesPerFrame">The MovesPerFrame value.</param>
    /// <param name="reserveTimeoutMs">The ReserveTimeoutMs value.</param>
    /// <param name="arrange">Optional faults or edits.</param>
    /// <returns>A new harness.</returns>
    public static ExecutionHarness Named(string fixture, int movesPerFrame = 20, int reserveTimeoutMs = 1500, Action<FakeContainerLayer>? arrange = null) =>
        For(OrganizeFixtures.Named(fixture), movesPerFrame, reserveTimeoutMs, arrange);

    /// <summary>
    /// Steps until the executor is complete, asserting on every call that the returned count is at most <c>max(1, bound)</c> and
    /// that the executor does not run away.
    /// </summary>
    /// <param name="bound">The MovesPerFrame value in force.</param>
    /// <param name="maxSteps">The step count after which the run is a failure.</param>
    /// <returns>The value returned by each <c>Step()</c>, in order, including the final call that completed the run.</returns>
    public List<int> Run(int bound, int maxSteps = 10000)
    {
        var steps = new List<int>();
        while (!Executor.IsComplete)
        {
            Assert.True(steps.Count < maxSteps, "The executor did not complete within " + maxSteps + " steps.");
            int done = Executor.Step();
            Assert.True(done >= 0, "Step returned a negative count.");
            Assert.True(done <= Math.Max(1, bound), "Step completed " + done + " moves, above the bound " + Math.Max(1, bound) + ".");
            steps.Add(done);
        }

        return steps;
    }

    /// <summary>The live snapshot of a fixture chest.</summary>
    /// <param name="serial">The chest serial of the fixture id.</param>
    /// <returns>The current snapshot.</returns>
    public ContainerSnapshot Live(uint serial) => FakeFixtures.Get(Fake, OrganizeFixtures.Id(serial));

    /// <summary>Asserts the conservation invariant between <see cref="Before"/> and the current world.</summary>
    public void AssertConserved() => ConservationAssert.Holds(Before, Fake.Capture());

    /// <summary>Asserts that every usable chest's live per-item totals equal the totals of its layout's stacks (order-free).</summary>
    public void AssertLiveTotalsMatchLayouts()
    {
        foreach (ChestLayout layout in Plan.Layouts)
        {
            ContainerSnapshot live = FakeFixtures.Get(Fake, layout.Id);
            Assert.Equal(Totals(layout.Stacks), Totals(live.Contents));
        }
    }

    /// <summary>Asserts that the named chests equal their seed snapshots, so nothing of them moved.</summary>
    /// <param name="serials">The chest serials.</param>
    public void AssertUntouched(params uint[] serials)
    {
        foreach (uint serial in serials)
        {
            ContainerSnapshot seed = Input.Containers.Single(c => c.Id.Id == serial);
            Assert.Equal(seed, Live(serial));
        }
    }

    /// <summary>Asserts that every chest of the fake reports a None reservation status.</summary>
    public void AssertAllReleased()
    {
        foreach (ContainerSnapshot chest in Fake.GetContainers())
        {
            Assert.Equal(ReservationStatus.None, Fake.GetReservationStatus(chest.Id));
        }
    }

    /// <summary>Per-item totals of a list of stacks, keyed by prefab, quality and max stack.</summary>
    /// <param name="stacks">The stacks to total.</param>
    /// <returns>A sorted map of describe-keys to counts.</returns>
    public static SortedDictionary<string, long> Totals(IEnumerable<ItemStackSnapshot> stacks)
    {
        var totals = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (ItemStackSnapshot stack in stacks)
        {
            string key = stack.PrefabName + " q" + stack.Quality.ToString(CultureInfo.InvariantCulture) + " max" + stack.MaxStack.ToString(CultureInfo.InvariantCulture);
            totals.TryGetValue(key, out long current);
            totals[key] = current + stack.Count;
        }

        return totals;
    }

    /// <summary>Per-item totals from literal (prefab, count) pairs at quality 1 and max stack 50 unless given.</summary>
    /// <param name="items">The expected items.</param>
    /// <returns>A sorted map of describe-keys to counts.</returns>
    public static SortedDictionary<string, long> Expect(params (string Prefab, long Count, int MaxStack)[] items)
    {
        var totals = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach ((string prefab, long count, int maxStack) in items)
        {
            totals[prefab + " q1 max" + maxStack.ToString(CultureInfo.InvariantCulture)] = count;
        }

        return totals;
    }

    /// <summary>The recorded calls that start with a verb, for example <c>Add</c>.</summary>
    /// <param name="verb">The verb.</param>
    /// <returns>The matching lines, in order.</returns>
    public List<string> CallsOf(string verb) => Recorder.Calls.Where(line => line.StartsWith(verb + " ", StringComparison.Ordinal)).ToList();
}
