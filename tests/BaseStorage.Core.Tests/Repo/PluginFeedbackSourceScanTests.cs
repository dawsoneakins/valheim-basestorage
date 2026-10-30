namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-2120 to GW-U-2128 (AC-2; contract 5.2 FS1-FS6, 5.4 and 5.5 fixtures N1-N11, P1-P9; ADR-GWIN-153 lexer fixtures L1-L3):
/// the feedback sink scanner over in-memory fixtures and over every Plugin file. Trait "Repo".
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginFeedbackSourceScanTests
{
    private const string FixturePath = "src/BaseStorage.Plugin/Fixture/Sample.cs";
    private const string CraftGamePath = "src/BaseStorage.Plugin/Crafting/GameCraftGame.cs";
    private const string BuildGamePath = "src/BaseStorage.Plugin/Building/GameBuildGame.cs";
    private const string CraftSessionPath = "src/BaseStorage.Plugin/Crafting/CraftSession.cs";
    private const string WriterPath = "src/BaseStorage.Plugin/ChestCategories/ChestCategoryWriter.cs";
    private const string TrySetCaller = "src/BaseStorage.Plugin/CategoryButton/ChestCategoryButton.cs";

    /// <summary>GW-U-2120 (N1 to N11): each negative fixture yields exactly the listed rules, in order.</summary>
    [Theory]
    [InlineData("""Notices.Show(NoticePlacement.Center, "message.base.not-in-base");""", new[] { "FS1", "FS2" })]
    [InlineData("""Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageCategorySet, "Ores");""", new[] { "FS2" })]
    [InlineData("""Notices.Show(NoticePlacement.Center, key);""", new[] { "FS1" })]
    [InlineData("""VanillaButtonFactory.Create(template, parent, ControlId, "Organize base", OnClick);""", new[] { "FS1", "FS2" })]
    [InlineData("""label.text = "Organize base";""", new[] { "FS2" })]
    [InlineData("""label.text = $"{count}";""", new[] { "FS2" })]
    [InlineData("""hud.ShowMessage(MessageHud.MessageType.Center, "Done", 0, null);""", new[] { "FS2" })]
    [InlineData("""tooltip.m_text = "Category";""", new[] { "FS2" })]
    [InlineData("""UnifiedPopup.Push(new YesNoPopup("Organize", body, OnYes, OnNo, false, true));""", new[] { "FS2", "FS2" })]
    [InlineData("""game.ShowNotice("message.craft.materials-moved");""", new[] { "FS2" })]
    [InlineData("""VanillaButtonFactory.SetLabel(button, labelKey);""", new[] { "FS1" })]
    public void EachNegativeFixtureYieldsExactlyTheListedRules(string source, string[] rules)
    {
        IReadOnlyList<string> violations = FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source));

        Assert.Equal(rules, violations.Select(line => line.Substring(0, line.IndexOf(':', StringComparison.Ordinal))).ToArray());
    }

    /// <summary>GW-U-2121 (P1 to P9): each positive fixture is clean.</summary>
    [Theory]
    [InlineData("""Notices.Show(NoticePlacement.Center, StringKeys.MessageBaseNotInBase);""", FixturePath)]
    [InlineData("""startup.m_connectionFailedError.text = startup.m_connectionFailedError.text + "\n" + line;""", FixturePath)]
    [InlineData("""text.text = string.Empty;""", FixturePath)]
    [InlineData("""Notices.Show(NoticePlacement.TopLeft, ProfileFavourites.MessageKeyFor(outcome), name);""", FixturePath)]
    [InlineData("""VanillaButtonFactory.SetLabel(control, StringKeys.ChestCategoryButton, ChestCategoryReader.Label(x, StringTable.English));""", FixturePath)]
    [InlineData("""LogPolicy.Write(sink, LogCategory.UiFallback, "[" + id + "] " + message);""", FixturePath)]
    [InlineData("""if (!string.Equals(line.text, text, StringComparison.Ordinal))""", FixturePath)]
    [InlineData("""Notices.Show(NoticePlacement.Center, key);""", CraftGamePath)]
    [InlineData("""// Notices.Show(NoticePlacement.Center, "Not in a base");""", FixturePath)]
    public void EachPositiveFixtureHasNoViolation(string source, string path)
    {
        Assert.Empty(FeedbackSourceScan.Violations(path, FeedbackSourceScan.StripKeepingLines(source)));
    }

    /// <summary>GW-U-2122 (5.4): the N1 FS1 line and the N5 FS2 line equal the contract examples byte-for-byte.</summary>
    [Fact]
    public void TheN1AndN5LinesEqualTheContractExamples()
    {
        const string examplePath = "src/BaseStorage.Plugin/Foo/Bar.cs";
        string n1 = new string('\n', 11) + """Notices.Show(NoticePlacement.Center, "message.base.not-in-base");""";
        string n5 = new string('\n', 29) + """label.text = "Organize base";""";

        IReadOnlyList<string> fs1 = FeedbackSourceScan.Violations(examplePath, FeedbackSourceScan.StripKeepingLines(n1));
        IReadOnlyList<string> fs2 = FeedbackSourceScan.Violations(examplePath, FeedbackSourceScan.StripKeepingLines(n5));

        Assert.Equal(
            "FS1: src/BaseStorage.Plugin/Foo/Bar.cs:12: Notices.Show key argument '\"message.base.not-in-base\"' is not an allowed key form",
            fs1[0]);
        Assert.Equal("FS2: src/BaseStorage.Plugin/Foo/Bar.cs:30: .text = has wording literal '\"Organize base\"'", fs2[0]);
    }

    /// <summary>GW-U-2123 (L1): a three-line block comment before an N5 line keeps the line count, so the violation is on line 4.</summary>
    [Fact]
    public void ALineNumberSurvivesAMultiLineBlockComment()
    {
        string source = "/* a\nb\nc */\n" + """label.text = "Organize base";""";

        IReadOnlyList<string> violations = FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source));

        Assert.Equal("FS2: src/BaseStorage.Plugin/Fixture/Sample.cs:4: .text = has wording literal '\"Organize base\"'", Assert.Single(violations));
    }

    /// <summary>GW-U-2123 (L2): a char literal holding a quote does not open a string, so the N5 line after it is found.</summary>
    [Fact]
    public void ACharLiteralHoldingAQuoteDoesNotHideTheNextSink()
    {
        string source = """var q = '"'; label.text = "Organize base";""";

        IReadOnlyList<string> violations = FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source));

        Assert.Single(violations);
        Assert.StartsWith("FS2: src/BaseStorage.Plugin/Fixture/Sample.cs:1: .text = has wording literal", violations[0], StringComparison.Ordinal);
    }

    /// <summary>GW-U-2123 (L3): a key that is not a StringKeys constant is an FS1 violation.</summary>
    [Fact]
    public void AStringKeysNameThatIsNotAConstantIsAnFs1Violation()
    {
        string source = """Notices.Show(NoticePlacement.Center, StringKeys.NoSuchKey);""";

        IReadOnlyList<string> violations = FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source));

        Assert.Equal(new[] { "FS1" }, violations.Select(line => line.Substring(0, 3)).ToArray());
    }

    /// <summary>GW-U-2123 (lexer): a sink name inside a string, a verbatim, an interpolated verbatim or a raw literal is not a sink.</summary>
    [Theory]
    [InlineData("""var s = "Notices.Show(NoticePlacement.Center, key)";""")]
    [InlineData("""var v = @"x ""Notices.Show(y"" z";""")]
    [InlineData("""var i = $@"Notices.Show({key})";""")]
    [InlineData(""""var r = """Notices.Show( key)""";"""")]
    public void SinkNamesInsideLiteralsAreNotSinks(string source)
    {
        Assert.Empty(FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source)));
    }

    /// <summary>GW-U-2123 (lexer): a verbatim wording literal is recognised, and its violation shows the literal as written.</summary>
    [Fact]
    public void AVerbatimWordingLiteralIsRecognisedAsWritten()
    {
        string source = """label.text = @"Organize base";""";

        IReadOnlyList<string> violations = FeedbackSourceScan.Violations(FixturePath, FeedbackSourceScan.StripKeepingLines(source));

        Assert.Equal("FS2: src/BaseStorage.Plugin/Fixture/Sample.cs:1: .text = has wording literal '@\"Organize base\"'", Assert.Single(violations));
    }

    /// <summary>GW-U-2124 (FS1, FS2): every Plugin file has no violation; on failure every violation line is printed.</summary>
    [Fact]
    public void EveryPluginFileHasNoFeedbackViolation()
    {
        var violations = new List<string>();
        foreach (KeyValuePair<string, string> file in FeedbackSourceScan.Read())
        {
            violations.AddRange(FeedbackSourceScan.Violations(file.Key, file.Value));
        }

        Assert.True(violations.Count == 0, "Plugin violations:\n" + string.Join("\n", violations));
    }

    /// <summary>GW-U-2125 (FS5): the scan read the sink files, and found the minimum number of each sink kind.</summary>
    [Fact]
    public void TheScanReadTheSinkFilesAndFoundEachKindOfSink()
    {
        Dictionary<string, string> files = FeedbackSourceScan.Read();

        Assert.True(files.ContainsKey("src/BaseStorage.Plugin/Ui/Notices.cs"), "Notices.cs was not read.");
        Assert.True(files.ContainsKey("src/BaseStorage.Plugin/Ui/VanillaButtonFactory.cs"), "VanillaButtonFactory.cs was not read.");
        foreach (string path in FeedbackSourceScan.PassThroughFiles)
        {
            Assert.True(files.ContainsKey(path), $"{path} was not read.");
        }

        List<FeedbackSourceScan.Sink> sinks = files.Values.SelectMany(text => FeedbackSourceScan.Sinks(text)).ToList();
        Assert.True(sinks.Count(sink => sink.Name == "Notices.Show") >= 8, "Fewer than 8 Notices.Show( sinks were found.");
        Assert.True(sinks.Count(sink => sink.Name == "VanillaButtonFactory.Create") >= 3, "Fewer than 3 VanillaButtonFactory.Create( sinks were found.");
        Assert.True(sinks.Count(sink => sink.Name == "VanillaButtonFactory.SetLabel") >= 1, "No VanillaButtonFactory.SetLabel( sink was found.");
        Assert.True(sinks.Count(sink => sink.Name == ".text =") >= 1, "No .text = sink was found.");
    }

    /// <summary>GW-U-2126 (FS6): both game files place each notice by the catalog through ShowNotice.</summary>
    [Fact]
    public void BothGameFilesPlaceTheirNoticeByTheCatalog()
    {
        Dictionary<string, string> files = FeedbackSourceScan.Read();

        foreach (string path in new[] { CraftGamePath, BuildGamePath })
        {
            Assert.Matches(@"void ShowNotice\(string key\)\s*(=>|\{)[^;]*FeedbackCatalog\.PlacementOf\(key\)", files[path]);
        }
    }

    /// <summary>GW-U-2126 (FS6): the category writer shows the Unchanged line and places success Top Left and refusals Centre.</summary>
    [Fact]
    public void TheCategoryWriterShowsTheUnchangedLineAndPlacesItsNotices()
    {
        string text = FeedbackSourceScan.Read()[WriterPath];

        Assert.Contains("CategoryWriteStatus.Unchanged", text, StringComparison.Ordinal);
        Assert.Contains("Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageCategorySet,", text, StringComparison.Ordinal);
        string body = PluginSourceScan.MethodBody(text, @"void OnFinished\(");
        Assert.Contains("NoticePlacement.TopLeft", body, StringComparison.Ordinal);
        Assert.Contains("NoticePlacement.Center", body, StringComparison.Ordinal);
    }

    /// <summary>GW-U-2126 (FS6): ChestCategoryWriter.TrySet is called only by the category button, outside the writer.</summary>
    [Fact]
    public void OnlyTheCategoryButtonCallsTheWriterTrySet()
    {
        string[] callers = FeedbackSourceScan.Read()
            .Where(file => file.Value.Contains("ChestCategoryWriter.TrySet(", StringComparison.Ordinal))
            .Select(file => file.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "src/BaseStorage.Plugin/BaseMenu/CategoryDropdown.cs", TrySetCaller }, callers);
    }

    /// <summary>GW-U-2127 (CR5 wiring, the session file names the two craft keys): the craft session file holds both keys.</summary>
    [Fact]
    public void TheCraftSessionNamesBothCraftKeys()
    {
        string text = FeedbackSourceScan.Read()[CraftSessionPath];

        Assert.Contains("StringKeys.MessageCraftMaterialsMoved", text, StringComparison.Ordinal);
        Assert.Contains("StringKeys.MessageCraftUsedBase", text, StringComparison.Ordinal);
    }
}
