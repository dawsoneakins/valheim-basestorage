namespace BaseStorage.Core.Tests.Bases;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>AC-4 (ADR-GWIN-032, ADR-GWIN-033): the exported shape of HoverInspector and the XML docs of the new cache members.</summary>
public sealed class HoverSurfaceTests
{
    private const string InspectionNamespace = "BaseStorage.Core.Bases.Inspection";
    private const string DocFileName = "BaseStorage.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-546 (AC-4): the one exported type, its sealing and constructor, its methods and their static-ness, its parameter types,
    // its nullable annotations, and its lack of static fields.
    [Fact]
    public void HoverInspectorIsTheOnlyTypeAndItsSurfaceIsExact()
    {
        Type[] exported = typeof(BaseCache).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == InspectionNamespace)
            .ToArray();
        Assert.Equal(new[] { "BaseStorage.Core.Bases.Inspection.HoverInspector" }, exported.Select(type => type.FullName!).ToArray());

        Type type = typeof(HoverInspector);
        Assert.True(type.IsSealed && !type.IsAbstract, "HoverInspector must be a sealed, non-static class.");
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        ConstructorInfo constructor = Assert.Single(constructors);
        Assert.Equal(new[] { typeof(BaseCache) }, constructor.GetParameters().Select(p => p.ParameterType).ToArray());

        MethodInfo[] methods = type.GetMethods(PublicDeclared).Where(method => !method.IsSpecialName).ToArray();
        Assert.Equal(
            new[] { "Append", "ChestLine", "CountChests", "FormatChest", "FormatStation", "StationLine" },
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        foreach (string name in new[] { "Append", "CountChests", "FormatChest", "FormatStation" })
        {
            Assert.True(Method(type, name).IsStatic, name + " must be static.");
        }

        foreach (string name in new[] { "ChestLine", "StationLine" })
        {
            Assert.False(Method(type, name).IsStatic, name + " must be an instance method.");
        }

        Assert.Equal(
            new[] { typeof(ContainerId), typeof(WorldPosition), typeof(string), typeof(BaseCacheOptions) },
            Parameters(Method(type, "ChestLine")));
        Assert.Equal(new[] { typeof(StationId), typeof(BaseCacheOptions) }, Parameters(Method(type, "StationLine")));
        Assert.Equal(new[] { typeof(BaseResolution), typeof(CategoryAssignment) }, Parameters(Method(type, "FormatChest")));
        Assert.Equal(new[] { typeof(BaseResolution) }, Parameters(Method(type, "FormatStation")));
        Assert.Equal(new[] { typeof(BaseResolution) }, Parameters(Method(type, "CountChests")));
        Assert.Equal(new[] { typeof(string), typeof(string) }, Parameters(Method(type, "Append")));
        Assert.Equal(typeof(int), Method(type, "CountChests").ReturnType);
        Assert.Equal(typeof(string), Method(type, "ChestLine").ReturnType);

        var context = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(Method(type, "ChestLine"), "encodedCategory")).WriteState);
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(Method(type, "FormatChest"), "assignment")).WriteState);
        MethodInfo append = Method(type, "Append");
        Assert.Equal(NullabilityState.Nullable, context.Create(Param(append, "hoverText")).WriteState);
        Assert.Equal(NullabilityState.Nullable, context.Create(append.ReturnParameter).WriteState);
        Assert.Equal(NullabilityState.NotNull, context.Create(Param(append, "line")).WriteState);

        FieldInfo[] staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.DoesNotContain(staticFields, field => !field.IsLiteral);
    }

    // GW-U-547 (AC-4): the new XML docs have a summary on every member, a param for every parameter, and a returns on every value member.
    [Fact]
    public void HoverInspectorAndTheNewCacheMembersAreDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        var members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var targets = XmlDocMembers.TargetsFor(new[] { typeof(HoverInspector) }).ToList();
        targets.Add(("P:BaseStorage.Core.Bases.Resolution.BaseCache.Generation", Array.Empty<string>(), false));
        targets.Add(("M:BaseStorage.Core.Bases.Resolution.BaseCache.IsCurrent(BaseStorage.Core.Bases.Resolution.BaseCacheOptions)", new[] { "options" }, true));

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in targets)
        {
            if (!members.TryGetValue(id, out XElement? member))
            {
                problems.Add("missing <member>: " + id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(member.Element("summary")?.Value))
            {
                problems.Add("blank or missing <summary>: " + id);
            }

            foreach (string parameter in parameters)
            {
                if (!member.Elements("param").Any(p => p.Attribute("name")?.Value == parameter))
                {
                    problems.Add("missing <param name=\"" + parameter + "\">: " + id);
                }
            }

            if (returnsValue && member.Element("returns") is null)
            {
                problems.Add("missing <returns>: " + id);
            }
        }

        Assert.Empty(problems);
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethods(PublicDeclared).Single(method => method.Name == name);

    private static Type[] Parameters(MethodInfo method) =>
        method.GetParameters().Select(p => p.ParameterType).ToArray();

    private static ParameterInfo Param(MethodInfo method, string name) =>
        method.GetParameters().Single(p => p.Name == name);
}
