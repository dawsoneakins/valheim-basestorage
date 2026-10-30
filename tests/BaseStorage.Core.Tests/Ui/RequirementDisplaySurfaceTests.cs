namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-1509 (AC-1; contract 3.1 and 3.4 CS1-CS3): the public surface of <see cref="RequirementDisplay"/> and
/// <see cref="RequirementDisplayFormatter"/>, the nullable annotations, and the XML documentation in BaseStorage.Core.xml (ADR-GWIN-011).
/// </summary>
public sealed class RequirementDisplaySurfaceTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1509 (3.1 R1-R4): RequirementDisplay is a public readonly struct with the contracted constructor, three get-only properties and the equality members.
    [Fact]
    public void RequirementDisplayIsAPublicReadonlyStructWithTheContractedMembers()
    {
        Type type = typeof(RequirementDisplay);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.Contains(type.GetCustomAttributesData(), attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
        Assert.Contains(typeof(IEquatable<RequirementDisplay>), type.GetInterfaces());

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Single(constructors);
        Assert.Equal(new[] { typeof(string), typeof(bool), typeof(bool) }, constructors[0].GetParameters().Select(p => p.ParameterType).ToArray());

        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        Assert.Equal(new[] { "IsShort", "ShowsChests", "Text" }, properties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.All(properties, property => Assert.Null(property.SetMethod));

        Assert.Equal(typeof(string), type.GetProperty("Text")!.PropertyType);
        Assert.Equal(typeof(bool), type.GetProperty("IsShort")!.PropertyType);
        Assert.Equal(typeof(bool), type.GetProperty("ShowsChests")!.PropertyType);

        Assert.NotNull(type.GetMethod("Equals", new[] { type }));
        Assert.NotNull(type.GetMethod("Equals", new[] { typeof(object) }));
        Assert.NotNull(type.GetMethod("GetHashCode", Type.EmptyTypes));
        Assert.NotNull(type.GetMethod("op_Equality", new[] { type, type }));
        Assert.NotNull(type.GetMethod("op_Inequality", new[] { type, type }));
    }

    // GW-U-1509 (3.1): RequirementDisplayFormatter is a static class with exactly Format and IsShort as public methods.
    [Fact]
    public void RequirementDisplayFormatterIsAStaticClassWithExactlyTwoPublicMethods()
    {
        Type type = typeof(RequirementDisplayFormatter);

        Assert.True(type.IsAbstract && type.IsSealed);
        string[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Format", "IsShort" }, methods);
        Assert.DoesNotContain(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly), f => !f.IsLiteral);
    }

    // GW-U-1509 (3.1): Equals(object?) is the only nullable parameter of the surface; the other reference parameters are not nullable.
    [Fact]
    public void OnlyEqualsObjectAcceptsNull()
    {
        var context = new NullabilityInfoContext();
        Type type = typeof(RequirementDisplay);

        ParameterInfo objectParameter = type.GetMethod("Equals", new[] { typeof(object) })!.GetParameters()[0];
        Assert.Equal(NullabilityState.Nullable, context.Create(objectParameter).WriteState);

        ParameterInfo valueParameter = type.GetMethod("Equals", new[] { type })!.GetParameters()[0];
        Assert.Equal(NullabilityState.NotNull, context.Create(valueParameter).WriteState);

        ParameterInfo vanillaParameter = typeof(RequirementDisplayFormatter).GetMethod("Format")!.GetParameters()[3];
        Assert.Equal(NullabilityState.NotNull, context.Create(vanillaParameter).WriteState);
    }

    // GW-U-1509 (3.1, CS1-CS3): every public member of the two types has a summary, the parameters are documented, and non-void members have returns.
    [Fact]
    public void EveryPublicMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in DocTargets())
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

            if (returnsValue && string.IsNullOrWhiteSpace(member.Element("returns")?.Value))
            {
                problems.Add("missing <returns>: " + id);
            }
        }

        Assert.Empty(problems);
    }

    private static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> DocTargets()
    {
        const string Display = "BaseStorage.Core.Ui.RequirementDisplay";
        const string Formatter = "BaseStorage.Core.Ui.RequirementDisplayFormatter";

        yield return ("T:" + Display, Array.Empty<string>(), false);
        yield return ("M:" + Display + ".#ctor(System.String,System.Boolean,System.Boolean)", new[] { "text", "isShort", "showsChests" }, false);
        yield return ("P:" + Display + ".Text", Array.Empty<string>(), true);
        yield return ("P:" + Display + ".IsShort", Array.Empty<string>(), true);
        yield return ("P:" + Display + ".ShowsChests", Array.Empty<string>(), true);
        yield return ("M:" + Display + ".Equals(" + Display + ")", new[] { "other" }, true);
        yield return ("M:" + Display + ".Equals(System.Object)", new[] { "obj" }, true);
        yield return ("M:" + Display + ".GetHashCode", Array.Empty<string>(), true);
        yield return ("M:" + Display + ".op_Equality(" + Display + "," + Display + ")", new[] { "left", "right" }, true);
        yield return ("M:" + Display + ".op_Inequality(" + Display + "," + Display + ")", new[] { "left", "right" }, true);
        yield return ("T:" + Formatter, Array.Empty<string>(), false);
        yield return ("M:" + Formatter + ".Format(System.Int32,System.Int32,System.Int32,System.String)", new[] { "have", "chest", "need", "vanillaText" }, true);
        yield return ("M:" + Formatter + ".IsShort(System.Int32,System.Int32,System.Int32)", new[] { "have", "chest", "need" }, true);
    }
}
