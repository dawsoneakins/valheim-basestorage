namespace BaseStorage.Core.Tests.Diagnostics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseStorage.Core;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-7: the Diagnostics surface is exactly RateLimitedErrorReporter, and Core stays free of game and Harmony references.
/// </summary>
public sealed class DiagnosticsSurfaceTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-129
    [Fact]
    public void DiagnosticsNamespaceExportsExactlyTheReporterFromTheCoreAssembly()
    {
        Type[] exported = typeof(RateLimitedErrorReporter).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "BaseStorage.Core.Diagnostics" && !type.IsNested)
            .ToArray();

        Assert.Equal(new[] { "RateLimitedErrorReporter" }, exported.Select(type => type.Name).ToArray());
        Assert.Same(typeof(CoreAssemblyMarker).Assembly, typeof(RateLimitedErrorReporter).Assembly);
    }

    // GW-U-129
    [Fact]
    public void ReporterIsSealedWithOneConstructorTakingTheSink()
    {
        Type type = typeof(RateLimitedErrorReporter);

        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(new[] { typeof(ILogSink) }, constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }

    // GW-U-129
    [Fact]
    public void ReporterDeclaresExactlyReportAndResetSessionAsPublicMethods()
    {
        MethodInfo[] methods = typeof(RateLimitedErrorReporter).GetMethods(Members)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Report", "ResetSession" }, methods.Select(method => method.Name).ToArray());

        MethodInfo report = methods[0];
        Assert.Equal(typeof(bool), report.ReturnType);
        Assert.Equal(new[] { typeof(string), typeof(Exception) }, report.GetParameters().Select(parameter => parameter.ParameterType).ToArray());

        MethodInfo reset = methods[1];
        Assert.Equal(typeof(void), reset.ReturnType);
        Assert.Empty(reset.GetParameters());
    }

    // GW-U-129
    [Fact]
    public void ReporterHasNoPublicFieldsOrSettableProperties()
    {
        Type type = typeof(RateLimitedErrorReporter);

        Assert.Empty(type.GetFields(Members));
        Assert.DoesNotContain(type.GetProperties(Members), property => property.SetMethod?.IsPublic == true);
    }

    // GW-U-129
    [Fact]
    public void BothReportParametersAreNullable()
    {
        MethodInfo report = typeof(RateLimitedErrorReporter).GetMethod(nameof(RateLimitedErrorReporter.Report))!;
        var context = new NullabilityInfoContext();

        foreach (ParameterInfo parameter in report.GetParameters())
        {
            Assert.Equal(NullabilityState.Nullable, context.Create(parameter).WriteState);
        }
    }

    // GW-U-130
    [Fact]
    public void DiagnosticsSourcesDoNotReferenceGameOrHarmony()
    {
        string folder = RepoRoot.Combine("src", "BaseStorage.Core", "Diagnostics");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        string[] forbidden = { "UnityEngine", "BepInEx", "HarmonyLib", "Harmony", "Valheim", "assembly_" };
        foreach (string file in files)
        {
            string code = PluginSourceScan.StripComments(RepoRoot.Normalise(File.ReadAllText(file)));
            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, code);
            }
        }
    }

    // GW-U-130
    [Fact]
    public void CoreProjectStillHasNoPackageReferences()
    {
        IEnumerable<System.Xml.Linq.XElement> packages = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.CoreProject), "PackageReference");

        Assert.Empty(packages);
    }
}
