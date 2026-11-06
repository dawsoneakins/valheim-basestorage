namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Snapshots;

/// <summary>AC-1 (contract 7.1, 7.2, R9): the exported surface of BaseStorage.Core.Bases and its XML documentation.</summary>
public sealed class BasesSurfaceTests
{
    private const string BasesNamespace = "BaseStorage.Core.Bases";
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-123 (AC-1)
    [Fact]
    public void ExportedTypesAreExactlyStationGraphAndStationGraphResult()
    {
        string[] exported = typeof(StationGraph).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == BasesNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "BaseStorage.Core.Bases.StationGraph", "BaseStorage.Core.Bases.StationGraphResult" },
            exported);
    }

    // GW-U-123 (AC-1)
    [Fact]
    public void StationGraphIsStaticWithOnlyFindBase()
    {
        Type type = typeof(StationGraph);

        Assert.True(type.IsAbstract && type.IsSealed, "StationGraph must be a static class.");
        string[] methods = type.GetMethods(PublicDeclared).Where(m => !m.IsSpecialName).Select(m => m.Name).ToArray();
        Assert.Equal(new[] { "FindBase" }, methods);

        MethodInfo findBase = type.GetMethod("FindBase", PublicDeclared)!;
        Assert.Equal(typeof(StationGraphResult), findBase.ReturnType);
        Assert.Equal(
            new[] { "stations", "start", "maxBaseStations" },
            findBase.GetParameters().Select(p => p.Name));
        Assert.Equal(
            new[] { typeof(IReadOnlyList<StationSnapshot>), typeof(StationId), typeof(int) },
            findBase.GetParameters().Select(p => p.ParameterType));
    }

    // GW-U-123 (AC-1)
    [Fact]
    public void StationGraphResultIsSealedWithNoPublicConstructorAndNoEqualityOverride()
    {
        Type type = typeof(StationGraphResult);

        Assert.True(type.IsSealed, "StationGraphResult must be sealed.");
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        string[] methods = type.GetMethods(PublicDeclared).Where(m => !m.IsSpecialName).Select(m => m.Name).ToArray();
        Assert.DoesNotContain("Equals", methods);
        Assert.DoesNotContain("GetHashCode", methods);
    }

    // GW-U-123 (AC-1)
    [Fact]
    public void StationGraphResultHasExactlyTwoGetOnlyPublicProperties()
    {
        PropertyInfo[] properties = typeof(StationGraphResult).GetProperties(PublicDeclared);

        Assert.Equal(2, properties.Length);
        PropertyInfo stations = properties.Single(p => p.Name == "Stations");
        PropertyInfo isCapped = properties.Single(p => p.Name == "IsCapped");
        Assert.Equal(typeof(IReadOnlyList<StationSnapshot>), stations.PropertyType);
        Assert.Equal(typeof(bool), isCapped.PropertyType);
        Assert.True(stations.CanRead && !stations.CanWrite, "Stations must be get-only.");
        Assert.True(isCapped.CanRead && !isCapped.CanWrite, "IsCapped must be get-only.");
    }

    // GW-U-123 (AC-1, R9): the XML file documents the 5 members and types, with param and returns on FindBase.
    [Fact]
    public void BasesMembersAreDocumentedInTheXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        XElement findBase = FindMember(members, "M:BaseStorage.Core.Bases.StationGraph.FindBase(");
        XElement[] documented =
        {
            Member(members, "T:BaseStorage.Core.Bases.StationGraph"),
            findBase,
            Member(members, "T:BaseStorage.Core.Bases.StationGraphResult"),
            Member(members, "P:BaseStorage.Core.Bases.StationGraphResult.Stations"),
            Member(members, "P:BaseStorage.Core.Bases.StationGraphResult.IsCapped"),
        };

        foreach (XElement member in documented)
        {
            Assert.False(string.IsNullOrWhiteSpace(member.Element("summary")?.Value), $"Blank summary: {member.Attribute("name")?.Value}");
        }

        string[] parameters = findBase.Elements("param").Select(p => p.Attribute("name")?.Value ?? string.Empty).ToArray();
        Assert.Equal(new[] { "stations", "start", "maxBaseStations" }, parameters);
        Assert.NotNull(findBase.Element("returns"));
    }

    private static XElement Member(Dictionary<string, XElement> members, string id) =>
        members.TryGetValue(id, out XElement? member) ? member : throw new Xunit.Sdk.XunitException("Missing <member>: " + id);

    private static XElement FindMember(Dictionary<string, XElement> members, string prefix) =>
        members.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(pair => pair.Value).SingleOrDefault()
        ?? throw new Xunit.Sdk.XunitException("Missing <member>: " + prefix);
}
