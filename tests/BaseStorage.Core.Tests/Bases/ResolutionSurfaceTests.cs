namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-4 (ADR-GWIN-024, ADR-GWIN-025): the exported shape of BaseStorage.Core.Bases.Resolution, checked by reflection.</summary>
public sealed class ResolutionSurfaceTests
{
    private const string ResolutionNamespace = "BaseStorage.Core.Bases.Resolution";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-254 (AC-4): the exported types in the namespace are exactly the seven ADR-GWIN-024 names, ordinal-sorted.
    [Fact]
    public void ExportedTypesAreExactlyTheSevenResolutionNames()
    {
        string[] exported = typeof(BaseResolver).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == ResolutionNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Bases.Resolution.BaseCache",
                "BaseStorage.Core.Bases.Resolution.BaseCacheOptions",
                "BaseStorage.Core.Bases.Resolution.BaseResolution",
                "BaseStorage.Core.Bases.Resolution.BaseResolver",
                "BaseStorage.Core.Bases.Resolution.BaseStart",
                "BaseStorage.Core.Bases.Resolution.BaseStartKind",
                "BaseStorage.Core.Bases.Resolution.IBaseSnapshotSource",
            },
            exported);
    }

    // GW-U-254 (AC-4): BaseResolution and BaseCache are sealed, and BaseResolution and BaseStart have no public constructor.
    [Fact]
    public void SealedTypesAndNoPublicConstructors()
    {
        Assert.True(typeof(BaseResolution).IsSealed, "BaseResolution must be sealed.");
        Assert.True(typeof(BaseCache).IsSealed, "BaseCache must be sealed.");
        Assert.Empty(typeof(BaseResolution).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(typeof(BaseStart).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // GW-U-254 (AC-4): BaseResolver is static, and its only public method is Resolve.
    [Fact]
    public void BaseResolverIsStaticWithOnlyResolve()
    {
        Type type = typeof(BaseResolver);
        Assert.True(type.IsAbstract && type.IsSealed, "BaseResolver must be a static class.");

        string[] methods = type.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(new[] { "Resolve" }, methods);
    }

    // GW-U-556 (AC-5, S-E002-005 edit of GW-U-254): BaseCache exposes exactly Resolve, IsCurrent and the four On* events.
    [Fact]
    public void BaseCacheExposesResolveIsCurrentAndTheFourEvents()
    {
        string[] methods = typeof(BaseCache).GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "IsCurrent", "OnContainerPlaced", "OnContainerRemoved", "OnStationPlaced", "OnStationRemoved", "Resolve" },
            methods);
    }

    // GW-U-556 (AC-5, S-E002-005): the only public property of BaseCache is Generation, a get-only long.
    [Fact]
    public void BaseCacheHasOnlyTheGenerationPropertyAndItIsAGetOnlyLong()
    {
        System.Reflection.PropertyInfo[] properties = typeof(BaseCache).GetProperties(PublicDeclared);

        Assert.Equal(new[] { "Generation" }, properties.Select(property => property.Name).ToArray());
        Assert.Equal(typeof(long), properties[0].PropertyType);
        Assert.True(properties[0].CanRead, "Generation must be readable.");
        Assert.False(properties[0].CanWrite, "Generation must not be writable.");
    }

    // GW-U-254 (AC-4): IBaseSnapshotSource declares exactly GetStations and GetContainers.
    [Fact]
    public void SnapshotSourceDeclaresOnlyTheTwoReads()
    {
        string[] methods = typeof(IBaseSnapshotSource).GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "GetContainers", "GetStations" }, methods);
    }

    // GW-U-254 (AC-4): no public type is added to the pinned Snapshots and Abstractions namespaces.
    // ContractTypes.SurfaceTypes() asserts the two namespaces hold exactly the pinned type names (it fails on any extra
    // type); the explicit check below also names the resolution types so a leak reads clearly.
    [Fact]
    public void NoTypeIsAddedToTheContractNamespaces()
    {
        string[] resolutionNames = typeof(BaseResolver).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == ResolutionNamespace)
            .Select(type => type.Name)
            .ToArray();

        string[] leaked = ContractTypes.SurfaceTypes()
            .Select(type => type.Name)
            .Where(name => resolutionNames.Contains(name, StringComparer.Ordinal))
            .ToArray();

        Assert.Empty(leaked);
    }
}
