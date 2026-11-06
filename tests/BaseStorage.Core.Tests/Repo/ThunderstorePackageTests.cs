namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// AC-7.1 (S-E007-001): the manifest template resolves to the Version.props version, the icon is a 256x256 PNG,
/// the gates never set PackageThunderstore, and the opt-in target has the contracted shape (ADR-GWIN-065..069).
/// </summary>
[Trait("Category", "Repo")]
public sealed class ThunderstorePackageTests
{
    private static readonly string[] GateKeys = ["typecheck", "test", "lint", "build"];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // GW-U-850
    [Fact]
    public void ManifestTemplateResolvesToVersionPropsAndListsBepInExPackValheim()
    {
        string template = RepoRoot.ReadText(ThunderstorePackage.ManifestTemplate);
        string version = ThunderstorePackage.ExpectedVersion();

        int first = template.IndexOf(ThunderstorePackage.VersionPlaceholder, StringComparison.Ordinal);
        int last = template.LastIndexOf(ThunderstorePackage.VersionPlaceholder, StringComparison.Ordinal);
        Assert.True(first >= 0, "manifest.json has no " + ThunderstorePackage.VersionPlaceholder + " placeholder.");
        Assert.Equal(first, last);
        Assert.Contains("\"version_number\": \"" + ThunderstorePackage.VersionPlaceholder + "\"", template, StringComparison.Ordinal);
        Assert.DoesNotContain(version, template, StringComparison.Ordinal);

        ThunderstorePackage.AssertManifest(ThunderstorePackage.Resolve(template, version), version);
    }

    // GW-U-851
    [Fact]
    public void IconIsA256By256PngByItsHeader()
    {
        string path = RepoRoot.Combine(ThunderstorePackage.Icon);
        Assert.True(File.Exists(path), $"Required repo file is missing: {ThunderstorePackage.Icon}");
        byte[] bytes = File.ReadAllBytes(path);

        Assert.True(bytes.Length >= 8 + 25 + 12, "icon.png is too short to hold IHDR and IEND.");
        Assert.Equal(PngSignature, bytes[..8]);
        Assert.Equal(13, ReadBigEndian(bytes, 8));
        Assert.Equal("IHDR", Encoding.ASCII.GetString(bytes, 12, 4));
        Assert.Equal(256, ReadBigEndian(bytes, 16));
        Assert.Equal(256, ReadBigEndian(bytes, 20));
        Assert.Equal(8, bytes[24]);
        Assert.Contains(bytes[25], new byte[] { 2, 6 });
        Assert.Equal(0, bytes[28]);
        Assert.Equal("IEND", Encoding.ASCII.GetString(bytes, bytes.Length - 8, 4));
    }

    // GW-U-852
    [Fact]
    public void GatesNeverSetPackageThunderstore()
    {
        string config = RepoRoot.ReadText(ContractPaths.PipelineConfig);
        Assert.False(
            config.Contains(ThunderstorePackage.PropertyName, StringComparison.OrdinalIgnoreCase),
            ".pipeline/config.yml must not mention PackageThunderstore.");

        string[] gates = ContractGateStrings();
        Assert.Equal(GateKeys.Length, gates.Length);
        foreach (string gate in gates)
        {
            Assert.False(
                gate.Contains(ThunderstorePackage.PropertyName, StringComparison.OrdinalIgnoreCase),
                "Gate command must not pass PackageThunderstore: " + gate);
        }

        string specGateBlock = SpecSection4GateBlock();
        Assert.Contains("dotnet build BaseStorage.sln -c Release --nologo -warnaserror", specGateBlock, StringComparison.Ordinal);
        Assert.False(
            specGateBlock.Contains(ThunderstorePackage.PropertyName, StringComparison.OrdinalIgnoreCase),
            "docs/SPEC.md section 4 gate block must not mention PackageThunderstore.");
    }

    // GW-U-852
    [Fact]
    public void PackageThunderstoreDefaultsToFalseAndDistIsIgnored()
    {
        Assert.Equal(new[] { "false" }, RepoRoot.ValuesOf(ContractPaths.PluginProject, ThunderstorePackage.PropertyName));

        IReadOnlyList<string> files = RepoRoot.MsBuildFiles();
        Assert.Contains(ContractPaths.PluginProject, files);
        var offenders = new List<string>();
        foreach (string file in files)
        {
            foreach (XElement element in RepoRoot.Named(RepoRoot.LoadXml(file), ThunderstorePackage.PropertyName))
            {
                if (string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add(file);
                }
            }
        }

        Assert.True(offenders.Count == 0, "PackageThunderstore set to true in: " + string.Join(", ", offenders));

        string[] ignored = RepoRoot.ReadText(ContractPaths.GitIgnore).Split('\n').Select(line => line.Trim()).ToArray();
        Assert.Contains("dist/", ignored);
    }

    // GW-U-853
    [Fact]
    public void PackageTargetRunsAfterBuildOnlyWhenEnabledAndZipsTheSixSources()
    {
        XElement? found = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PluginProject), "Target")
            .SingleOrDefault(t => RepoRoot.Attr(t, "Name") == ThunderstorePackage.TargetName);
        Assert.NotNull(found);
        XElement target = found!;

        Assert.Equal("Build", RepoRoot.Attr(target, "AfterTargets"));
        Assert.Equal("'$(PackageThunderstore)' == 'true'", RepoRoot.Attr(target, "Condition"));
        Assert.DoesNotContain(target.Descendants(), e => e.Name.LocalName is "Warning" or "Error" or "Exec");

        string[] gameFolderMentions = target.DescendantsAndSelf()
            .SelectMany(e => e.Attributes())
            .Where(a => a.Value.Contains("ValheimDir", StringComparison.OrdinalIgnoreCase)
                        || a.Value.Contains("BepInEx", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Parent!.Name.LocalName + "/@" + a.Name.LocalName)
            .ToArray();
        Assert.True(gameFolderMentions.Length == 0, "PackageThunderstore mentions the game folder: " + string.Join(", ", gameFolderMentions));

        string[] includes = target.Descendants()
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .SelectMany(v => v!.Split(';'))
            .Select(v => v.Trim())
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("$(TargetPath)", includes);
        Assert.Contains("$(TargetDir)Dons-QOL.Core.dll", includes);
        Assert.Contains(includes, v => v.EndsWith("icon.png", StringComparison.Ordinal));
        Assert.Contains(includes, v => v.EndsWith("README.md", StringComparison.Ordinal));
        Assert.Contains(includes, v => v.EndsWith("CHANGELOG.md", StringComparison.Ordinal));

        XElement zip = Assert.Single(target.Descendants(), e => e.Name.LocalName == "ZipDirectory");
        Assert.Equal("true", RepoRoot.Attr(zip, "Overwrite"));
        Assert.EndsWith("Dons-QOL-$(Version).zip", RepoRoot.Attr(zip, "DestinationFile"), StringComparison.Ordinal);

        XElement message = Assert.Single(target.Descendants(), e => e.Name.LocalName == "Message");
        Assert.Equal("high", RepoRoot.Attr(message, "Importance"));
        Assert.StartsWith(ThunderstorePackage.Bs1004Prefix, RepoRoot.Attr(message, "Text"), StringComparison.Ordinal);

        foreach (string source in new[] { ThunderstorePackage.ManifestTemplate, ThunderstorePackage.Icon, ThunderstorePackage.Readme, ThunderstorePackage.Changelog })
        {
            Assert.True(RepoRoot.Exists(source), $"Package source is missing: {source}");
        }
    }

    private static int ReadBigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    // Same parse as GW-U-021: the gate lines under "gates:" in contracts/build-layout.yml.
    private static string[] ContractGateStrings()
    {
        string[] lines = RepoRoot.ReadText(ContractPaths.BuildLayoutContract).Split('\n');
        int start = Array.IndexOf(lines, "gates:");
        Assert.True(start >= 0, "The gates: block is missing from build-layout.yml.");

        var gates = new List<string>();
        foreach (string line in lines.Skip(start + 1).TakeWhile(l => l.StartsWith("  ", StringComparison.Ordinal)))
        {
            string trimmed = line.Trim();
            int colon = trimmed.IndexOf(':');
            if (colon > 0 && GateKeys.Contains(trimmed[..colon]))
            {
                gates.Add(trimmed[(colon + 1)..].Trim());
            }
        }

        return gates.ToArray();
    }

    // The text of docs/SPEC.md section 4 ("## 4. Gate commands") up to the next "## " heading.
    private static string SpecSection4GateBlock()
    {
        string[] lines = RepoRoot.ReadText(ThunderstorePackage.SpecFile).Split('\n');
        int start = Array.FindIndex(lines, l => l.StartsWith("## 4.", StringComparison.Ordinal));
        Assert.True(start >= 0, "docs/SPEC.md has no '## 4.' section.");
        int end = Array.FindIndex(lines, start + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
        return string.Join("\n", lines[(start + 1)..(end < 0 ? lines.Length : end)]);
    }
}
