namespace BaseStorage.Core.Tests.Bases;

using System.Globalization;
using System.Linq;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Snapshots;

/// <summary>
/// GW-U-2249 (AC-2, BF1, base-fill contract 2.2 and 3.4, spec NFR read-only): FromContainers gives the same value under every culture
/// the contract names, and it leaves the array and the snapshots exactly as they were. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillPurityTests
{
    // GW-U-2249 (BF1, 3.4): two calls give equal values under en-US, de-DE, tr-TR and ar-SA, the culture is restored, and the input is unchanged.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void FromContainersGivesEqualValuesUnderEveryCultureAndLeavesTheInputUnchanged(string cultureName)
    {
        ContainerSnapshot[] containers = { Chest(1, 5, 2, 3), Chest(2, 5, 2, 0), Chest(3, 6, 4, 12) };
        ContainerSnapshot[] references = (ContainerSnapshot[])containers.Clone();
        string before = Describe(containers);

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        BaseFill first;
        BaseFill second;
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            first = BaseFill.FromContainers(containers);
            second = BaseFill.FromContainers(containers);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        Assert.Equal(first, second);
        Assert.Equal((15, 44, 34), (first.OccupiedSlots, first.TotalSlots, first.Percent));

        for (int i = 0; i < containers.Length; i++)
        {
            Assert.Same(references[i], containers[i]);
        }

        Assert.Equal(before, Describe(containers));
    }

    private static string Describe(ContainerSnapshot[] containers) =>
        string.Join(
            ";",
            containers.Select(container => string.Join(
                ",",
                container.SlotCount.ToString(CultureInfo.InvariantCulture),
                container.Width.ToString(CultureInfo.InvariantCulture),
                container.Height.ToString(CultureInfo.InvariantCulture),
                ((int)container.Flags).ToString(CultureInfo.InvariantCulture),
                string.Join("/", container.Contents.Select(stack => stack.PrefabName + ":" + stack.Count + ":" + stack.Quality + ":" + stack.MaxStack)))));

    private static ContainerSnapshot Chest(uint serial, int width, int height, int stacks) =>
        new ContainerSnapshot(
            new ContainerId(1L, serial),
            new WorldPosition(serial * 2f, 0f, 0f),
            width,
            height,
            Enumerable.Range(0, stacks).Select(_ => new ItemStackSnapshot("Wood", 10, 1, 50)).ToArray(),
            ContainerFlags.PlayerBuilt);
}
