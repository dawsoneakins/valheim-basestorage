namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Categories.Defaults;

/// <summary>
/// AC-1 and AC-9 (contract C1, C2, ADR-GWIN-028, spec NFR purity): the Defaults files are pure and never build CRLF lines. The scans
/// read exactly the 4 files of src/BaseStorage.Core/Categories/Defaults/, strip comments and normalise CRLF before matching.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreDefaultCategoriesSourceScanTests
{
    private const string DefaultsFolder = "src/BaseStorage.Core/Categories/Defaults";
    private const string MessagesFile = "DefaultCategoryFileMessages.cs";

    private static readonly string[] ExpectedFiles =
    {
        "CategoryItem.cs",
        "DefaultCategories.cs",
        "DefaultCategoryFile.cs",
        "DefaultCategoryFileMessages.cs",
    };

    private static readonly string[] ForbiddenPatterns =
    {
        @"System\.IO",
        @"(?<![A-Za-z0-9_])File\.",
        @"(?<![A-Za-z0-9_])Directory\.",
        @"\bStream\b",
        @"\bConsole\.",
        @"\bILogSink\b",
        @"\bIClock\b",
        @"\bUnityEngine\b",
        @"\bBepInEx\b",
        @"\bHarmonyLib\b",
        @"\bToLower\b",
        @"\bToUpper\b",
        @"\bCurrentCulture\b",
    };

    // GW-U-460
    [Fact]
    public void DefaultsFolderHoldsExactlyTheFourContractFiles()
    {
        Assert.Equal(ExpectedFiles, Sources().Keys.ToArray());
    }

    // GW-U-460
    [Fact]
    public void NoDefaultsSourceUsesIoLoggingUnityBepInExHarmonyOrCurrentCulture()
    {
        foreach (var pair in Sources())
        {
            foreach (string pattern in ForbiddenPatterns)
            {
                Assert.False(Regex.IsMatch(pair.Value, pattern), $"{pair.Key} matches forbidden token {pattern}.");
            }
        }
    }

    // GW-U-460
    [Fact]
    public void CultureInfoAppearsOnlyInTheMessagesFileAndOnlyAsInvariantCulture()
    {
        foreach (var pair in Sources())
        {
            int all = Regex.Matches(pair.Value, @"\bCultureInfo\b").Count;
            if (all == 0)
            {
                continue;
            }

            Assert.Equal(MessagesFile, pair.Key);
            Assert.Equal(all, Regex.Matches(pair.Value, @"\bCultureInfo\.InvariantCulture\b").Count);
        }
    }

    // GW-U-460
    [Fact]
    public void EveryStaticFieldInTheDefaultsTypesIsReadonlyOrConst()
    {
        Type[] types = typeof(CategoryItem).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "BaseStorage.Core.Categories.Defaults")
            .ToArray();

        foreach (Type type in types)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Assert.True(field.IsInitOnly || field.IsLiteral, $"{type.Name}.{field.Name} is a mutable static field.");
            }
        }
    }

    // GW-U-460 (AC-9)
    [Fact]
    public void NoDefaultsSourceBuildsLinesWithAppendLineNewlineOrCarriageReturn()
    {
        foreach (var pair in Sources())
        {
            Assert.DoesNotContain("AppendLine", pair.Value);
            Assert.DoesNotContain("Environment.NewLine", pair.Value);
            Assert.DoesNotContain("\\r", pair.Value);
        }
    }

    private static Dictionary<string, string> Sources()
    {
        string folder = RepoRoot.Combine(DefaultsFolder);
        string[] names = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        return names.ToDictionary(
            name => name,
            name => PluginSourceScan.StripComments(RepoRoot.ReadText(Path.Combine(DefaultsFolder, name))),
            StringComparer.Ordinal);
    }
}
