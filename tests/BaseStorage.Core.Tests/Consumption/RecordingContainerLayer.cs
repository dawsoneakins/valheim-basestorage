namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;

/// <summary>
/// A decorator over <see cref="FakeContainerLayer"/> that records every <see cref="IContainerLayer"/> call, in order (ADR-GWIN-031). Keys:
/// Request:5, Status:5 (logged as Status:5=Pending), InvId, Get:1, Remove:5:0:Wood/8:8, Add:5:Wood/8, Release:5. A hook runs before the call
/// is delegated. Throw hooks record the key, then throw. Reads of the inner fake go through <see cref="Inner"/> and are not recorded.
/// Not a test class.
/// </summary>
internal sealed class RecordingContainerLayer : IContainerLayer
{
    private readonly FakeContainerLayer inner;
    private readonly List<string> calls = new();
    private readonly Dictionary<(ContainerId Id, int Fetch), ContainerSnapshot?> getOverrides = new();
    private readonly Dictionary<ContainerId, int> fetches = new();

    /// <summary>Wraps a fake layer.</summary>
    /// <param name="inner">The fake that holds the state.</param>
    internal RecordingContainerLayer(FakeContainerLayer inner)
    {
        this.inner = inner;
    }

    /// <summary>Gets the wrapped fake. Its state reads and fault injection are not recorded.</summary>
    internal FakeContainerLayer Inner => inner;

    /// <summary>Gets the ordered call log.</summary>
    internal IReadOnlyList<string> Calls => calls;

    /// <summary>Gets or sets a rule that returns an exception to throw for a call key, before delegating. Null means no throw.</summary>
    internal Func<string, Exception?>? ThrowOn { get; set; }

    /// <summary>Gets or sets a rule that returns a result for a removal or an add, without delegating. Null means delegate.</summary>
    internal Func<string, ContainerOperationResult?>? ResultOverride { get; set; }

    /// <summary>Gets or sets a hook that runs before every call with its key, and may read the execution. It never calls Poll or Cancel.</summary>
    internal Action<string>? Observe { get; set; }

    /// <summary>Replaces the result of one container fetch, without delegating. The fetch number counts every fetch of that id from 1.</summary>
    /// <param name="id">The container id.</param>
    /// <param name="fetch">The fetch number, from 1.</param>
    /// <param name="snapshot">The snapshot to return, or null to return null.</param>
    internal void GetOverride(ContainerId id, int fetch, ContainerSnapshot? snapshot) => getOverrides[(id, fetch)] = snapshot;

    /// <inheritdoc />
    public ContainerId PlayerInventoryId
    {
        get
        {
            Enter("InvId");
            calls.Add("InvId");
            return inner.PlayerInventoryId;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ContainerSnapshot> GetContainers()
    {
        Enter("Containers");
        calls.Add("Containers");
        return inner.GetContainers();
    }

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId id)
    {
        string key = "Get:" + Serial(id);
        Enter(key);
        int fetch = fetches.TryGetValue(id, out int previous) ? previous + 1 : 1;
        fetches[id] = fetch;
        calls.Add(key);
        return getOverrides.TryGetValue((id, fetch), out ContainerSnapshot? replaced) ? replaced : inner.GetContainer(id);
    }

    /// <inheritdoc />
    public void RequestReservation(ContainerId id)
    {
        string key = "Request:" + Serial(id);
        Enter(key);
        inner.RequestReservation(id);
        calls.Add(key);
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        string key = "Status:" + Serial(id);
        Enter(key);
        ReservationStatus status = inner.GetReservationStatus(id);
        calls.Add(key + "=" + status.ToString());
        return status;
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId id)
    {
        string key = "Release:" + Serial(id);
        Enter(key);
        inner.ReleaseReservation(id);
        calls.Add(key);
    }

    /// <inheritdoc />
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        string key = "Add:" + Serial(id) + ":" + items.PrefabName + "/" + Number(items.Count);
        Enter(key);
        ContainerOperationResult? forced = ResultOverride?.Invoke(key);
        calls.Add(key);
        return forced ?? inner.AddItems(id, items);
    }

    /// <inheritdoc/>
    public void SelectSourceStack(ContainerId source, int stackIndex)
    {
    }

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        if (expected is null)
        {
            throw new ArgumentNullException(nameof(expected));
        }

        string key = "Remove:" + Serial(id) + ":" + Number(stackIndex) + ":" + expected.PrefabName + "/" + Number(expected.Count) + ":" + Number(count);
        Enter(key);
        ContainerOperationResult? forced = ResultOverride?.Invoke(key);
        calls.Add(key);
        return forced ?? inner.RemoveFromStack(id, stackIndex, expected, count);
    }

    private void Enter(string key)
    {
        Observe?.Invoke(key);
        Exception? thrown = ThrowOn?.Invoke(key);
        if (thrown is not null)
        {
            calls.Add(key);
            throw thrown;
        }
    }

    private static string Serial(ContainerId id) => id.Id.ToString(CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
