namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Settings;
using BaseStorage.Plugin.World;

/// <summary>
/// AC-10 (contract section 8, ADR-GWIN-031): the D1 registry line and the D2 base summary line. Expected text is the
/// literal contract text and is never built by calling RegistryMessages. The D2 fixture is the contract section 8 fixture.
/// </summary>
public sealed class RegistryMessagesTests
{
    private static readonly StationSnapshot Bench = new(
        new StationId(3141592653L, 17u),
        new WorldPosition(12.5f, 31.25f, -840.75f),
        20f);

    private static readonly StationSnapshot SecondBench = new(
        new StationId(3141592653L, 18u),
        new WorldPosition(20f, 31.25f, -840.75f),
        20f);

    private static readonly ContainerSnapshot Chest = new(
        new ContainerId(3141592653L, 42u),
        new WorldPosition(14f, 31.25f, -836.5f),
        5,
        2,
        new[] { new ItemStackSnapshot("Wood", 17, 1, 50) },
        ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther);

    private static readonly ContainerSnapshot Cart = new(
        new ContainerId(3141592653L, 43u),
        new WorldPosition(16f, 31.25f, -836.5f),
        6,
        3,
        Array.Empty<ItemStackSnapshot>(),
        ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle);

    private static readonly ContainerSnapshot Grave = new(
        new ContainerId(3141592653L, 44u),
        new WorldPosition(10f, 31.25f, -838f),
        8,
        4,
        new[] { new ItemStackSnapshot("SwordIron", 1, 3, 1) },
        ContainerFlags.Tombstone);

    private static readonly ContainerSnapshot Erik = new(
        new ContainerId(2718281828L, 7u),
        new WorldPosition(13f, 31.25f, -842f),
        5,
        2,
        Array.Empty<ItemStackSnapshot>(),
        ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied);

    // GW-U-530
    [Theory]
    [InlineData(0, true, 37, "Registry: container added (37 loaded)")]
    [InlineData(0, false, 36, "Registry: container removed (36 loaded)")]
    [InlineData(1, true, 1, "Registry: station added (1 loaded)")]
    [InlineData(1, false, 0, "Registry: station removed (0 loaded)")]
    [InlineData(9, true, 2, "Registry: object added (2 loaded)")]
    public void ChangedFormatsTheD1LineOfContractSection8(int kind, bool added, int loaded, string expected)
    {
        Assert.Equal(expected, RegistryMessages.Changed((RegistryKind)kind, added, loaded));
    }

    // GW-U-530
    [Fact]
    public void ANegativeCountIsRenderedAsIs()
    {
        Assert.Equal("Registry: station removed (-1 loaded)", RegistryMessages.Changed(RegistryKind.Station, false, -1));
    }

    // GW-U-530
    [Theory]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void TheD1LineIsTheSameUnderEveryCulture(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Assert.Equal("Registry: container added (37 loaded)", RegistryMessages.Changed(RegistryKind.Container, true, 37));
            Assert.Equal("Registry: station removed (0 loaded)", RegistryMessages.Changed(RegistryKind.Station, false, 0));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    // GW-U-530 (the RegistryKind value case is green on arrival; it does not call RegistryMessages)
    [Fact]
    public void RegistryKindNamesAndValuesAreExactlyContainerZeroAndStationOne()
    {
        Assert.Equal(new[] { "Container", "Station" }, Enum.GetNames(typeof(RegistryKind)));
        Assert.Equal(0, (int)RegistryKind.Container);
        Assert.Equal(1, (int)RegistryKind.Station);
    }

    // GW-U-531
    [Fact]
    public void AllFourContainersWithVehiclesExcludedGiveTheContractLine()
    {
        BaseResolution resolution = BaseResolver.Resolve(new[] { Bench }, new[] { Chest, Cart, Grave, Erik }, BaseStart.FromStation(Bench.Id), 64);

        string line = RegistryMessages.BaseSummary(Bench.Id, resolution, Eligibility(resolution, excludeVehicles: true));

        Assert.Equal(
            "Base at station 3141592653:17: 1 station(s), 4 container(s), 1 eligible; excluded 1 tombstone, 1 vehicle, 0 not player-built, 1 access denied; 1 in use by another",
            line);
    }

    // GW-U-531
    [Fact]
    public void WithVehiclesNotExcludedTheCartIsEligible()
    {
        BaseResolution resolution = BaseResolver.Resolve(new[] { Bench }, new[] { Chest, Cart, Grave, Erik }, BaseStart.FromStation(Bench.Id), 64);

        string line = RegistryMessages.BaseSummary(Bench.Id, resolution, Eligibility(resolution, excludeVehicles: false));

        Assert.Equal(
            "Base at station 3141592653:17: 1 station(s), 4 container(s), 2 eligible; excluded 1 tombstone, 0 vehicle, 0 not player-built, 1 access denied; 1 in use by another",
            line);
    }

    // GW-U-531
    [Fact]
    public void ANotInBaseResolutionGivesTheNotInABaseLine()
    {
        string line = RegistryMessages.BaseSummary(Bench.Id, BaseResolution.NotInBase, Eligibility(BaseResolution.NotInBase, excludeVehicles: true));

        Assert.Equal("Base at station 3141592653:17: not in a base", line);
    }

    // GW-U-531
    [Fact]
    public void ACappedResolutionEndsWithTheCappedSuffix()
    {
        BaseResolution resolution = BaseResolver.Resolve(new[] { Bench, SecondBench }, new[] { Chest }, BaseStart.FromStation(Bench.Id), 1);
        Assert.True(resolution.IsCapped);

        string line = RegistryMessages.BaseSummary(Bench.Id, resolution, Eligibility(resolution, excludeVehicles: true));

        Assert.EndsWith(" (capped)", line, StringComparison.Ordinal);
        Assert.StartsWith("Base at station 3141592653:17: ", line, StringComparison.Ordinal);
    }

    // GW-U-531
    [Fact]
    public void ANullResolutionIsRejectedByParameterName()
    {
        EligibilityResult eligibility = Eligibility(BaseResolution.NotInBase, excludeVehicles: true);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RegistryMessages.BaseSummary(Bench.Id, null!, eligibility));

        Assert.Equal("resolution", ex.ParamName);
    }

    // GW-U-531
    [Fact]
    public void ANullEligibilityIsRejectedByParameterName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RegistryMessages.BaseSummary(Bench.Id, BaseResolution.NotInBase, null!));

        Assert.Equal("eligibility", ex.ParamName);
    }

    private static EligibilityResult Eligibility(BaseResolution resolution, bool excludeVehicles)
    {
        var settings = new BaseStorageSettings(new FakeSettingsSource().WithBool(SettingKeys.ExcludeVehicles, excludeVehicles));
        return ContainerEligibility.Evaluate(
            resolution.Containers,
            new Dictionary<ContainerId, CategoryAssignment>(),
            ContainerPurpose.CraftAvailability,
            EligibilityOptions.FromSettings(settings));
    }
}
