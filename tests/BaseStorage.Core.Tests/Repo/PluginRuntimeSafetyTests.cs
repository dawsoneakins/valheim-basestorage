namespace BaseStorage.Core.Tests.Repo;

using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>
/// Faults found in the first playtest on 2026-10-30. Valheim ships no System.ValueTuple.dll, so a net462 tuple fails with a
/// TypeLoadException the first time its type is touched; subtracting TimeSpan.MinValue overflows on every frame.
/// </summary>
public class PluginRuntimeSafetyTests
{
    [Fact]
    public void Plugin_does_not_reference_an_assembly_the_game_lacks()
    {
        using FileStream stream = File.OpenRead(PluginDll.EnsureBuilt("Release"));
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();

        var names = reader.AssemblyReferences
            .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
            .ToList();

        Assert.DoesNotContain("System.ValueTuple", names);
    }

    [Theory]
    [InlineData(@"src\BaseStorage.Plugin\Organize\OrganizeHotkey.cs")]
    [InlineData(@"src\BaseStorage.Plugin\Deposit\DepositHotkey.cs")]
    public void Hotkey_refresh_clock_does_not_start_at_MinValue(string relative)
    {
        string source = File.ReadAllText(RepoRoot.Combine(relative));

        Assert.DoesNotContain("TimeSpan.MinValue", source);
    }

    [Fact]
    public void Single_chest_snapshots_do_not_touch_the_memo_outside_a_build()
    {
        string source = File.ReadAllText(RepoRoot.Combine(@"src\BaseStorage.Plugin\World\GameSnapshotSource.cs"));

        Assert.Contains("building ? traitsMemo.Previous(container) : null", source);
    }
}
