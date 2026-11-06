namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Paths, literals and the manifest oracle of the Thunderstore package (contracts/build-layout.yml section 8,
/// ADR-GWIN-065..069). Shared by the static and the process tests. Not a test class.
/// </summary>
public static class ThunderstorePackage
{
    public const string TargetName = "PackageThunderstore";
    public const string PropertyName = "PackageThunderstore";
    public const string ManifestTemplate = "package/manifest.json";
    public const string Icon = "package/icon.png";
    public const string Readme = "package/README.md";
    public const string Changelog = "CHANGELOG.md";
    public const string SpecFile = "docs/SPEC.md";
    public const string VersionPlaceholder = "__VERSION__";
    public const string PackageName = "Dons-QOL";

    /// <summary>The Thunderstore package name in manifest.json (letters, digits and underscores only); the zip and DLLs use Dons-QOL (hyphens are allowed there).</summary>
    public const string ManifestName = "Dons_QOL";
    public const string Bs1004Prefix = "BS1004: PackageThunderstore wrote '";
    public const int DescriptionMaxLength = 250;

    /// <summary>The six zip entries, forward-slash paths, ordinal order.</summary>
    public static readonly string[] ZipEntries =
    [
        "CHANGELOG.md",
        "README.md",
        "icon.png",
        "manifest.json",
        "plugins/Dons-QOL.Core.dll",
        "plugins/Dons-QOL.dll",
    ];

    /// <summary>The maintained Valheim BepInEx pack on Thunderstore (ADR-GWIN-066); the patch number may be bumped.</summary>
    public static readonly Regex DependencyPattern = new(@"^denikson-BepInExPack_Valheim-5\.4\.\d+$", RegexOptions.CultureInvariant);

    /// <summary>The Version.props version, the only version source (spec section 3).</summary>
    public static string ExpectedVersion() =>
        RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();

    /// <summary>Zip file name for the version, e.g. Dons-QOL-0.1.0.zip.</summary>
    public static string ZipFileName(string version) => PackageName + "-" + version + ".zip";

    /// <summary>Resolves the template the way the target does: the placeholder becomes the version.</summary>
    public static string Resolve(string template, string version) =>
        template.Replace(VersionPlaceholder, version, StringComparison.Ordinal);

    /// <summary>
    /// Asserts a resolved manifest text: valid JSON, name Dons_QOL, version_number equal to <paramref name="version"/>,
    /// a string website_url, a 1-250 character description and exactly one BepInExPack_Valheim dependency.
    /// </summary>
    public static void AssertManifest(string json, string version)
    {
        Assert.DoesNotContain(VersionPlaceholder, json, StringComparison.Ordinal);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        Assert.Equal(ManifestName, root.GetProperty("name").GetString());
        Assert.Equal(version, root.GetProperty("version_number").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("website_url").ValueKind);

        string description = root.GetProperty("description").GetString() ?? string.Empty;
        Assert.True(
            description.Trim().Length > 0 && description.Length <= DescriptionMaxLength,
            $"description must be 1-{DescriptionMaxLength} characters, was {description.Length}.");

        JsonElement dependencies = root.GetProperty("dependencies");
        Assert.Equal(JsonValueKind.Array, dependencies.ValueKind);
        string[] values = dependencies.EnumerateArray().Select(d => d.GetString() ?? string.Empty).ToArray();
        string dependency = Assert.Single(values);
        Assert.Matches(DependencyPattern, dependency);
    }
}
