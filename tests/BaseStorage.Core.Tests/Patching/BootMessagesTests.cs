namespace BaseStorage.Core.Tests.Patching;

using System;
using System.Globalization;
using BaseStorage.Core.Patching;

/// <summary>
/// AC-2 and AC-3: exact boot log text (contracts/plugin-boot.md sections 1.4 and 1.6). Expected strings are literals
/// copied from the contract and never built by calling BootMessages (ADR-GWIN-013).
/// </summary>
public sealed class BootMessagesTests
{
    // GW-U-077
    [Fact]
    public void LoadedFormatsTheVersionVerbatim()
    {
        Assert.Equal("Dons-QOL 0.1.0 loaded", BootMessages.Loaded("0.1.0"));
        Assert.Equal("Dons-QOL 1.12.3 loaded", BootMessages.Loaded("1.12.3"));
    }

    // GW-U-077
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void LoadedRejectsEmptyOrWhitespaceVersion(string version)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => BootMessages.Loaded(version));

        Assert.Equal("version", ex.ParamName);
    }

    // GW-U-077
    [Fact]
    public void LoadedRejectsNullVersion()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BootMessages.Loaded(null!));

        Assert.Equal("version", ex.ParamName);
    }

    // GW-U-077 and GW-U-078 (culture theory)
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("de-DE")]
    public void MessagesAreIdenticalUnderEveryCulture(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Assert.Equal("Dons-QOL 0.1.0 loaded", BootMessages.Loaded("0.1.0"));
            Assert.Equal("Dons-QOL 1.12.3 loaded", BootMessages.Loaded("1.12.3"));
            Assert.Equal(
                "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied",
                BootMessages.PatchApplied("BaseStorage.Plugin.Patches.ContainerHoverPatch"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    // GW-U-078
    [Fact]
    public void PatchAppliedFormatsTheName()
    {
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied",
            BootMessages.PatchApplied("BaseStorage.Plugin.Patches.ContainerHoverPatch"));
    }

    // GW-U-078
    [Fact]
    public void PatchFailedFormatsNameTypeAndMessage()
    {
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.HaveRequirementsPatch' failed to apply: System.InvalidOperationException: Could not find method",
            BootMessages.PatchFailed(
                "BaseStorage.Plugin.Patches.HaveRequirementsPatch",
                new InvalidOperationException("Could not find method")));
    }

    // GW-U-078
    [Fact]
    public void PatchFailedAddsOnlyTheImmediateInnerException()
    {
        var error = new Exception("outer", new InvalidOperationException("inner", new FormatException("deep")));

        Assert.Equal(
            "Patch 'A.B' failed to apply: System.Exception: outer ---> System.InvalidOperationException: inner",
            BootMessages.PatchFailed("A.B", error));
    }

    // GW-U-078
    [Fact]
    public void PatchFailedRejectsNullError()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BootMessages.PatchFailed("A.B", null!));

        Assert.Equal("error", ex.ParamName);
    }

    // GW-U-078
    [Fact]
    public void PatchFailedRejectsNullPatchName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BootMessages.PatchFailed(null!, new Exception("x")));

        Assert.Equal("patchName", ex.ParamName);
    }

    // GW-U-078
    [Fact]
    public void LoadFailedFormatsVersionTypeAndMessage()
    {
        Assert.Equal(
            "Dons-QOL 0.1.0 failed to load: System.TypeLoadException: Could not load type 'HarmonyLib.Harmony'",
            BootMessages.LoadFailed("0.1.0", new TypeLoadException("Could not load type 'HarmonyLib.Harmony'")));
    }

    // GW-U-078
    [Fact]
    public void LoadFailedAddsTheImmediateInnerExceptionWithTheArrowSuffix()
    {
        var error = new InvalidOperationException("outer", new TypeLoadException("inner"));

        Assert.Equal(
            "Dons-QOL 0.1.0 failed to load: System.InvalidOperationException: outer ---> System.TypeLoadException: inner",
            BootMessages.LoadFailed("0.1.0", error));
    }

    // GW-U-078
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void LoadFailedRejectsEmptyOrWhitespaceVersion(string version)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => BootMessages.LoadFailed(version, new Exception("x")));

        Assert.Equal("version", ex.ParamName);
    }

    // GW-U-078 (<unnamed patch 12> under the culture theory, driven through PatchApplier)
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("de-DE")]
    public void UnnamedPatchPlaceholderUsesInvariantDigits(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var patches = new IPatchUnit[13];
            for (int i = 0; i < 12; i++)
            {
                patches[i] = new FakePatch("P." + i.ToString(CultureInfo.InvariantCulture));
            }

            PatchApplyReport report = PatchApplier.ApplyAll(patches, new RecordingLogSink { DebugEnabled = true });

            Assert.Equal("<unnamed patch 12>", report.Results[12].Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
