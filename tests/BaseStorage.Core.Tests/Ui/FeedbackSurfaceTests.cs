namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Repo;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-2119 (AC-1; contract 2.1, 3.1 and R1): the S-E008-001 Core surface. Exactly five public Feedback types exist, one per file
/// with a file-scoped namespace; the enum values are the contracted ones; FeedbackLine is immutable; FeedbackCatalog.cs holds no
/// string literal and no game or Plugin name.
/// </summary>
public sealed class FeedbackSurfaceTests
{
    private const string UiFolder = "src/BaseStorage.Core/Ui";

    private static readonly string[] NewTypes = ["FeedbackAction", "FeedbackLineOwner", "FeedbackOutcome", "FeedbackLine", "FeedbackCatalog"];

    // GW-U-2119 (3.1): exactly the five new public Feedback types, each in its own file with a file-scoped namespace.
    [Fact]
    public void TheFivePublicFeedbackTypesAreTheOnlyOnesAndEachHasItsOwnFile()
    {
        string[] publicFeedback = typeof(StringTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "BaseStorage.Core.Ui" && type.Name.StartsWith("Feedback", StringComparison.Ordinal))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(NewTypes.OrderBy(name => name, StringComparer.Ordinal).ToArray(), publicFeedback);
        foreach (string name in NewTypes)
        {
            string text = RepoRoot.ReadText($"{UiFolder}/{name}.cs");
            Assert.Matches(@"(?m)^namespace BaseStorage\.Core\.Ui;\s*$", text);
            Assert.Matches(@"(?m)^public (sealed |static )?(enum|class) " + name + @"\b", text);
        }
    }

    // GW-U-2119 (2.1): the numeric values of FeedbackAction, FeedbackLineOwner and FeedbackOutcome are the contracted ones.
    [Fact]
    public void TheEnumNumericValuesAreTheContractedOnes()
    {
        Assert.Equal(
            Sorted(new Dictionary<string, int>
            {
                ["Craft"] = 0,
                ["Build"] = 1,
                ["CategoryChange"] = 2,
                ["Organize"] = 3,
                ["Deposit"] = 4,
                ["Favourite"] = 5,
            }),
            Sorted(NamedValues<FeedbackAction>()));
        Assert.Equal(
            Sorted(new Dictionary<string, int>
            {
                ["Mod"] = 0,
                ["Vanilla"] = 1,
                ["NoHud"] = 2,
                ["Superseded"] = 3,
                ["Coalesced"] = 4,
                ["TargetGone"] = 5,
            }),
            Sorted(NamedValues<FeedbackLineOwner>()));
        Assert.Equal(
            Sorted(FeedbackFixtures.Rows.ToDictionary(row => row.Outcome, row => row.Value)),
            Sorted(NamedValues<FeedbackOutcome>()));
    }

    // GW-U-2119 (3.1): FeedbackLine is sealed, has no public constructor and no public setter.
    [Fact]
    public void FeedbackLineIsSealedWithNoPublicConstructorOrSetter()
    {
        Assert.True(typeof(FeedbackLine).IsSealed, "FeedbackLine must be sealed.");
        Assert.Empty(typeof(FeedbackLine).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(typeof(FeedbackLine).GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
    }

    // GW-U-2119 (R1, 3.1): the five files are game-free and use only System and BaseStorage.Core; FeedbackCatalog.cs has no string literal.
    [Fact]
    public void TheFeedbackFilesAreGameFreeAndTheCatalogHoldsNoStringLiteral()
    {
        foreach (string name in NewTypes)
        {
            string text = FeedbackSourceScan.StripKeepingLines(RepoRoot.ReadText($"{UiFolder}/{name}.cs"));

            Assert.DoesNotMatch(@"(?m)^using\s+(?!System\b|BaseStorage\.Core\b)", text);
            Assert.DoesNotMatch(@"\b(Valheim|UnityEngine|ZNet|Harmony|BepInEx|MessageHud|BaseStorage\.Plugin)\b", text);
        }

        string catalog = FeedbackSourceScan.StripKeepingLines(RepoRoot.ReadText($"{UiFolder}/FeedbackCatalog.cs"));
        Assert.DoesNotContain("\"", catalog);
    }

    private static Dictionary<string, int> NamedValues<T>() where T : Enum =>
        Enum.GetValues(typeof(T)).Cast<object>().ToDictionary(value => ((Enum)value).ToString(), value => Convert.ToInt32(value));

    private static KeyValuePair<string, int>[] Sorted(IEnumerable<KeyValuePair<string, int>> pairs) =>
        pairs.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();

    private static KeyValuePair<string, int>[] Sorted(Dictionary<string, int> pairs) => Sorted(pairs.AsEnumerable());
}
