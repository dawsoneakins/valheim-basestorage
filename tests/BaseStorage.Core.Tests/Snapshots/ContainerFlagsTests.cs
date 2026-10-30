namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Linq;
using System.Reflection;

/// <summary>AC-2: ContainerFlags is a [Flags] int enum with the six contracted values.</summary>
public sealed class ContainerFlagsTests
{
    // GW-U-027
    [Fact]
    public void IsAFlagsEnumBackedByInt()
    {
        Type flags = ContractTypes.Snapshot("ContainerFlags");

        Assert.True(flags.IsEnum, "ContainerFlags must be an enum.");
        Assert.NotNull(flags.GetCustomAttribute<FlagsAttribute>());
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(flags));
    }

    // GW-U-027
    [Fact]
    public void HasExactlyTheSixContractedNamesAndValues()
    {
        Type flags = ContractTypes.Snapshot("ContainerFlags");
        string[] expected = ["AccessDenied=8", "InUseByOther=16", "None=0", "PlayerBuilt=1", "Tombstone=4", "Vehicle=2"];

        string[] actual = Enum.GetNames(flags)
            .Select(name => name + "=" + Convert.ToInt32(Enum.Parse(flags, name)))
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
