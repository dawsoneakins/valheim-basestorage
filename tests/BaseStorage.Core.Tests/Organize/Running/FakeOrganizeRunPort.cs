namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Restock.Run;
using BaseStorage.Core.Tests.Organize.Execution;
using BaseStorage.Core.Tests.Organize.Deposit;

/// <summary>
/// A recording <see cref="IOrganizeRunPort"/> (contract organize-runner-contracts.md section 8): fixed holder, catalog and kinds,
/// facts from a plan input, a <see cref="FakeContainerLayer"/> seeded with the input's chests behind a recorder, and lists of every
/// preview and notice shown. Not a test row.
/// </summary>
internal sealed class FakeOrganizeRunPort : IOrganizeRunPort
{
    private readonly OrganizeFixtures.PlanInput input;

    /// <summary>Creates the port over one plan input.</summary>
    /// <param name="input">The base whose assignments and in-use names the facts come from and whose chests seed the layer.</param>
    public FakeOrganizeRunPort(OrganizeFixtures.PlanInput input)
    {
        this.input = input;
        Catalog = input.Catalog;
        ItemKinds = input.Kinds;
    }

    /// <inheritdoc />
    public bool InWorld { get; set; } = true;

    /// <inheritdoc />
    public long HolderId { get; set; } = 1923487561234L;

    /// <inheritdoc />
    public string HolderName { get; set; } = "Erik";

    /// <inheritdoc />
    public CategoryCatalog Catalog { get; set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ItemKind> ItemKinds { get; set; }

    /// <summary>Gets or sets an action applied to the fake layer right after it is seeded (faults).</summary>
    public Action<FakeContainerLayer>? Arrange { get; set; }

    /// <summary>Gets or sets an exception thrown by <see cref="CreateLayer"/> (after the call is recorded).</summary>
    public Exception? ThrowOnCreateLayer { get; set; }

    /// <summary>Gets or sets an exception thrown by <see cref="ShowPreview"/>.</summary>
    public Exception? ThrowOnShowPreview { get; set; }

    /// <summary>Gets the fake layer of the last <see cref="CreateLayer"/>, or null.</summary>
    public FakeContainerLayer? Fake { get; private set; }

    /// <summary>Gets the recorder over <see cref="Fake"/>, or null.</summary>
    public RecordingContainerLayer? Recorder { get; private set; }

    /// <summary>Gets the containers of every <see cref="GatherFacts"/> call, in order.</summary>
    public List<IReadOnlyList<ContainerSnapshot>> GatherCalls { get; } = new List<IReadOnlyList<ContainerSnapshot>>();

    /// <summary>Gets the plans of every <see cref="CreateLayer"/> call, in order.</summary>
    public List<OrganizePlan> LayerCalls { get; } = new List<OrganizePlan>();

    /// <summary>Gets every preview shown, in order.</summary>
    public List<(string Header, string Body)> Previews { get; } = new List<(string Header, string Body)>();

    /// <summary>Gets every notice shown, in order.</summary>
    public List<(OrganizeNoticePlacement Placement, string Key, object?[] Args)> Notices { get; } = new List<(OrganizeNoticePlacement Placement, string Key, object?[] Args)>();

    // Edited by S-E004-015 (ADR-GWIN-143): the Deposit members of IOrganizeRunPort (deposit-button contract 2.1).

    /// <summary>Gets or sets the inventory <see cref="CaptureInventory"/> returns; null means no mappable inventory. Starts as <see cref="DepositInventory"/>.</summary>
    public PlayerInventorySnapshot? Inventory { get; set; } = DepositInventory();

    /// <summary>Gets or sets the favourites <see cref="Favourites"/> returns; empty by default.</summary>
    public FavouriteSlots Favourites { get; set; } = FavouriteSlots.Empty;

    /// <summary>Gets the number of <see cref="CaptureInventory"/> calls so far.</summary>
    public int CaptureCalls { get; private set; }

    /// <summary>Gets the plans of every <see cref="CreateDepositLayer"/> call, in order.</summary>
    public List<DepositPlan> DepositLayerCalls { get; } = new List<DepositPlan>();

    /// <summary>The inventory of contract 4.1 (DI): id 99:1, 8 x 4, SwordIron x1 q2 max1 equipped at (0,0), then Wood 10 at (0,1).</summary>
    /// <returns>A new inventory snapshot.</returns>
    public static PlayerInventorySnapshot DepositInventory() => DepositFixtures.Inventory(
        DepositFixtures.At(0, 0, OrganizeFixtures.S("SwordIron", 1, 1, 2), InventoryItemTraits.Equipped),
        DepositFixtures.At(0, 1, OrganizeFixtures.S("Wood", 10)));

    /// <inheritdoc />
    public PlayerInventorySnapshot? CaptureInventory()
    {
        CaptureCalls++;
        return Inventory;
    }

    /// <inheritdoc />
    public IContainerLayer CreateDepositLayer(DepositPlan plan)
    {
        DepositLayerCalls.Add(plan);
        var fake = new FakeContainerLayer(plan.Player.Container);
        foreach (ContainerSnapshot container in input.Containers)
        {
            fake.AddChest(container);
        }

        Arrange?.Invoke(fake);
        Fake = fake;
        Recorder = new RecordingContainerLayer(fake);
        return Recorder;
    }

    /// <inheritdoc />
    public OrganizeBaseFacts GatherFacts(IReadOnlyList<ContainerSnapshot> containers)
    {
        GatherCalls.Add(containers);
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var inUse = new Dictionary<ContainerId, string>();
        foreach (ContainerSnapshot container in containers)
        {
            if (input.Assignments.TryGetValue(container.Id, out CategoryAssignment? assignment))
            {
                assignments[container.Id] = assignment;
            }

            if (input.InUseBy.TryGetValue(container.Id, out string? name))
            {
                inUse[container.Id] = name;
            }
        }

        return new OrganizeBaseFacts(assignments, inUse);
    }

    /// <inheritdoc />
    public IContainerLayer CreateLayer(OrganizePlan plan)
    {
        LayerCalls.Add(plan);
        if (ThrowOnCreateLayer is not null)
        {
            throw ThrowOnCreateLayer;
        }

        var fake = new FakeContainerLayer();
        foreach (ContainerSnapshot container in input.Containers)
        {
            fake.AddChest(container);
        }

        Arrange?.Invoke(fake);
        Fake = fake;
        Recorder = new RecordingContainerLayer(fake);
        return Recorder;
    }

    /// <inheritdoc />
    public List<IReadOnlyList<BaseStorage.Core.Categories.AutoAssign.ChestAutoAssignment>> SavedRules { get; } = new();

    public void SaveCategoryRules(IReadOnlyList<BaseStorage.Core.Categories.AutoAssign.ChestAutoAssignment> rules) => SavedRules.Add(rules);

    public void ShowPreview(string header, string body)
    {
        if (ThrowOnShowPreview is not null)
        {
            throw ThrowOnShowPreview;
        }

        Previews.Add((header, body));
    }

    /// <inheritdoc />
    public void ShowNotice(OrganizeNoticePlacement placement, string key, object?[] args) => Notices.Add((placement, key, args));
    // Edited by S-QR-002 (ADR-GWIN-176): the Restock members of IOrganizeRunPort (contract section 4.1).

    /// <summary>Gets or sets the carried stacks <see cref="CaptureCarried"/> returns; null means the capture failed (RR3). Empty by default.</summary>
    public List<CarriedStack>? Carried { get; set; } = new List<CarriedStack>();

    /// <summary>Gets the number of <see cref="CaptureCarried"/> calls so far.</summary>
    public int CarriedCalls { get; private set; }

    /// <summary>Gets the number of <see cref="CaptureChests"/> calls so far.</summary>
    public int ChestCalls { get; private set; }

    /// <summary>Gets the plans of every <see cref="CreateRestockLayer"/> call, in order.</summary>
    public List<RestockPlan> RestockLayerCalls { get; } = new List<RestockPlan>();

    /// <summary>Gets the fake restock layer of the last <see cref="CreateRestockLayer"/>, or null.</summary>
    public FakeRestockLayer? RestockFake { get; private set; }

    /// <summary>Gets or sets an action applied to the restock layer right after it is built (faults).</summary>
    public Action<FakeRestockLayer>? RestockArrange { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<CarriedStack>? CaptureCarried()
    {
        CarriedCalls++;
        return Carried;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContainerSnapshot> CaptureChests(IReadOnlyList<ContainerSnapshot> containers)
    {
        ChestCalls++;
        return containers;
    }

    /// <inheritdoc />
    public IRestockLayer CreateRestockLayer(RestockPlan plan)
    {
        RestockLayerCalls.Add(plan);
        var fake = new FakeRestockLayer(Carried ?? new List<CarriedStack>(), input.Containers);
        RestockArrange?.Invoke(fake);
        RestockFake = fake;
        return fake;
    }
}
