namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Linq;
using System.Reflection;

/// <summary>AC-6: snapshot values are immutable (contract R3, R4 and section 2.6).</summary>
public sealed class SnapshotImmutabilityTests
{
    private const BindingFlags InstanceAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    // GW-U-037
    [Theory]
    [InlineData("WorldPosition")]
    [InlineData("StationId")]
    [InlineData("ContainerId")]
    [InlineData("ItemStackSnapshot")]
    [InlineData("StationSnapshot")]
    [InlineData("ContainerSnapshot")]
    public void SnapshotValuesHaveNoWritableStateAndNoCloneMethod(string typeName)
    {
        Type type = ContractTypes.Snapshot(typeName);

        string[] setters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetSetMethod(true) is not null)
            .Select(p => p.Name)
            .ToArray();
        string[] writableFields = type.GetFields(InstanceAll)
            .Where(f => !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();

        Assert.True(setters.Length == 0, $"{typeName} has setters or init accessors: {string.Join(", ", setters)}.");
        Assert.True(writableFields.Length == 0, $"{typeName} has non-readonly fields: {string.Join(", ", writableFields)}.");

        if (type.IsValueType)
        {
            Assert.True(HasIsReadOnly(type), $"{typeName} is a struct and must be a readonly struct.");
        }
        else
        {
            Assert.True(type.IsSealed, $"{typeName} must be a sealed class.");
        }

        Assert.Null(type.GetMethod("<Clone>$", InstanceAll | BindingFlags.Static));
    }

    private static bool HasIsReadOnly(Type type) =>
        type.GetCustomAttributesData()
            .Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
}
