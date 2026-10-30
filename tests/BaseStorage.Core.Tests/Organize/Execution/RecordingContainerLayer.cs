namespace BaseStorage.Core.Tests.Organize.Execution;

using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A decorator over any <see cref="IContainerLayer"/> that records every call as one line (verb, chest serial, detail) and forwards
/// it unchanged. The executor tests read <see cref="Calls"/> to prove the add-then-remove order and the batch bracketing. Not a test row.
/// </summary>
internal sealed class RecordingContainerLayer : IContainerLayer
{
    private readonly IContainerLayer inner;

    /// <summary>Wraps a layer.</summary>
    /// <param name="inner">The layer that does the work.</param>
    public RecordingContainerLayer(IContainerLayer inner)
    {
        this.inner = inner;
    }

    /// <summary>Gets the recorded calls, in order. Reads of snapshots and statuses are recorded too.</summary>
    public List<string> Calls { get; } = new List<string>();

    /// <inheritdoc />
    public ContainerId PlayerInventoryId => inner.PlayerInventoryId;

    /// <inheritdoc />
    public IReadOnlyList<ContainerSnapshot> GetContainers()
    {
        Calls.Add("GetContainers");
        return inner.GetContainers();
    }

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId id)
    {
        Calls.Add(Line("Get", id));
        return inner.GetContainer(id);
    }

    /// <inheritdoc />
    public void RequestReservation(ContainerId id)
    {
        Calls.Add(Line("Request", id));
        inner.RequestReservation(id);
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        ReservationStatus status = inner.GetReservationStatus(id);
        Calls.Add(Line("Status", id) + " " + status);
        return status;
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId id)
    {
        Calls.Add(Line("Release", id));
        inner.ReleaseReservation(id);
    }

    /// <inheritdoc />
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        ContainerOperationResult result = inner.AddItems(id, items);
        Calls.Add(Line("Add", id) + " " + items.PrefabName + " x" + items.Count.ToString(CultureInfo.InvariantCulture) + " " + result);
        return result;
    }

    /// <summary>Gets the source stacks named before each add, in call order.</summary>
    public List<(ContainerId Source, int Index)> SelectedSources { get; } = new();

    public void SelectSourceStack(ContainerId source, int stackIndex) => SelectedSources.Add((source, stackIndex));

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        ContainerOperationResult result = inner.RemoveFromStack(id, stackIndex, expected, count);
        Calls.Add(
            Line("Remove", id) + "#" + stackIndex.ToString(CultureInfo.InvariantCulture) + " " + expected.PrefabName
            + " x" + count.ToString(CultureInfo.InvariantCulture) + " " + result);
        return result;
    }

    /// <summary>Formats a verb and a chest serial, for example <c>Add 202</c>.</summary>
    internal static string Line(string verb, ContainerId id) => verb + " " + id.Id.ToString(CultureInfo.InvariantCulture);
}
