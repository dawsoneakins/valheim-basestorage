namespace BaseStorage.Core.Tests.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-7: the interfaces and enums of contract section 3, with their exact members and pinned values.</summary>
public sealed class AbstractionsSurfaceTests
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static readonly string[] ContainerLayerMethods =
    [
        "GetContainers() -> System.Collections.Generic.IReadOnlyList`1[BaseStorage.Core.Snapshots.ContainerSnapshot]",
        "GetContainer(BaseStorage.Core.Snapshots.ContainerId id) -> BaseStorage.Core.Snapshots.ContainerSnapshot",
        "RequestReservation(BaseStorage.Core.Snapshots.ContainerId id) -> System.Void",
        "GetReservationStatus(BaseStorage.Core.Snapshots.ContainerId id) -> BaseStorage.Core.Abstractions.ReservationStatus",
        "ReleaseReservation(BaseStorage.Core.Snapshots.ContainerId id) -> System.Void",
        "AddItems(BaseStorage.Core.Snapshots.ContainerId id, BaseStorage.Core.Snapshots.ItemStackSnapshot items) -> BaseStorage.Core.Abstractions.ContainerOperationResult",
        "RemoveFromStack(BaseStorage.Core.Snapshots.ContainerId id, System.Int32 stackIndex, BaseStorage.Core.Snapshots.ItemStackSnapshot expected, System.Int32 count) -> BaseStorage.Core.Abstractions.ContainerOperationResult",
        "SelectSourceStack(BaseStorage.Core.Snapshots.ContainerId source, System.Int32 stackIndex) -> System.Void",
    ];

    private static readonly string[] LogSinkMethods =
    [
        "IsEnabled(BaseStorage.Core.Abstractions.LogSeverity severity) -> System.Boolean",
        "Log(BaseStorage.Core.Abstractions.LogSeverity severity, System.String message) -> System.Void",
    ];

    // GW-U-039
    [Theory]
    [InlineData("IClock")]
    [InlineData("ILogSink")]
    [InlineData("IContainerLayer")]
    public void ContractInterfacesArePublicInAbstractions(string name)
    {
        Type type = ContractTypes.Abstraction(name);

        Assert.True(type.IsInterface && type.IsPublic, $"{name} must be a public interface.");
        Assert.Equal(ContractTypes.AbstractionsNamespace, type.Namespace);
    }

    // GW-U-039
    [Fact]
    public void ClockExposesOnlyAGetOnlyElapsedTimeSpan()
    {
        Type clock = ContractTypes.Abstraction("IClock");
        PropertyInfo elapsed = clock.GetProperty("Elapsed")!;

        Assert.Equal(new[] { "Elapsed" }, PropertyNames(clock));
        Assert.Equal(typeof(TimeSpan), elapsed.PropertyType);
        Assert.Null(elapsed.GetSetMethod(true));
        Assert.Empty(PlainMethods(clock));
    }

    // GW-U-039
    [Fact]
    public void LogSinkHasExactlyIsEnabledAndLog()
    {
        Type sink = ContractTypes.Abstraction("ILogSink");

        Assert.Empty(PropertyNames(sink));
        Assert.Equal(Sorted(LogSinkMethods), Sorted(PlainMethods(sink)));
    }

    // GW-U-039
    [Fact]
    public void ContainerLayerHasExactlyTheContractedMembers()
    {
        Type layer = ContractTypes.Abstraction("IContainerLayer");
        PropertyInfo inventory = layer.GetProperty("PlayerInventoryId")!;

        Assert.Equal(new[] { "PlayerInventoryId" }, PropertyNames(layer));
        Assert.Equal(ContractTypes.Snapshot("ContainerId"), inventory.PropertyType);
        Assert.Null(inventory.GetSetMethod(true));
        Assert.Equal(Sorted(ContainerLayerMethods), Sorted(PlainMethods(layer)));
    }

    // GW-U-039
    [Fact]
    public void GetContainerReturnsANullableSnapshot()
    {
        MethodInfo getContainer = ContractTypes.Method(
            ContractTypes.Abstraction("IContainerLayer"),
            "GetContainer",
            PublicInstance,
            ContractTypes.Snapshot("ContainerId"));

        NullabilityInfo info = new NullabilityInfoContext().Create(getContainer.ReturnParameter);
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
    }

    // GW-U-039
    [Theory]
    [InlineData("LogSeverity", "Debug=0;Info=1;Warning=2;Error=3")]
    [InlineData("ReservationStatus", "None=0;Pending=1;Granted=2;Denied=3;Lost=4")]
    [InlineData("ContainerOperationResult", "Success=0;NotFound=1;NotReserved=2;ContainerChanged=3;DestinationFull=4")]
    public void EnumValuesArePinnedAsContracted(string name, string expected)
    {
        Type type = ContractTypes.Abstraction(name);

        Assert.True(type.IsEnum && type.IsPublic, $"{name} must be a public enum.");
        string[] actual = Enum.GetNames(type)
            .Select(member => member + "=" + Convert.ToInt32(Enum.Parse(type, member)))
            .ToArray();
        Assert.Equal(Sorted(expected.Split(';')), Sorted(actual));
    }

    private static string[] PropertyNames(Type type) =>
        type.GetProperties(PublicInstance).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    private static string[] PlainMethods(Type type) =>
        type.GetMethods(PublicInstance).Where(m => !m.IsSpecialName).Select(Signature).ToArray();

    private static string Signature(MethodInfo method) =>
        method.Name + "(" + string.Join(", ", method.GetParameters().Select(p => p.ParameterType + " " + p.Name))
        + ") -> " + method.ReturnType;

    private static string[] Sorted(IEnumerable<string> values) => values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
}
