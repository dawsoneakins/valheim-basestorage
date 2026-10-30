namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// AC-1 (R9): every public member of the Reservations namespace is documented in BaseStorage.Core.xml. The member-ID helper is
/// copied from CoreXmlDocTests (ADR-GWIN-023) so the existing files stay unedited. The copy also writes the ID forms that the shared
/// helper cannot: a generic parameter as `0 and a by-ref parameter with a trailing @.
/// </summary>
public sealed class ReservationsXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const string Namespace = "BaseStorage.Core.Reservations";

    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-158 (AC-1, R9): summary, param and returns on every public member; the failure lists every missing ID.
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        Dictionary<string, XElement> members = LoadMembers();
        var problems = new List<string>();

        foreach (Type type in ReservationTypes())
        {
            foreach ((string id, string[] parameters, bool returnsValue) in TargetsFor(type))
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
        }

        Assert.True(
            problems.Count == 0,
            $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }

    // GW-U-158 (AC-1, R9): both generic types document TOperation with a typeparam tag.
    [Fact]
    public void BothGenericTypesDocumentTheirTypeParameter()
    {
        Dictionary<string, XElement> members = LoadMembers();

        foreach (string id in new[] { "T:" + Namespace + ".Reservation`1", "T:" + Namespace + ".ChestReservations`1" })
        {
            Assert.True(members.TryGetValue(id, out XElement? member), "missing <member>: " + id);
            Assert.True(
                member!.Elements("typeparam").Any(p => p.Attribute("name")?.Value == "TOperation" && !string.IsNullOrWhiteSpace(p.Value)),
                "missing <typeparam name=\"TOperation\"> on " + id);
        }
    }

    // GW-U-158 (AC-1): the 7 exported types are the ones documented.
    [Fact]
    public void TheDocumentedTypesAreTheSevenExportedTypes()
    {
        Type[] types = ReservationTypes();

        Assert.Equal(7, types.Length);
        Assert.All(types, type => Assert.True(LoadMembers().ContainsKey("T:" + type.FullName!.Replace('+', '.'))));
    }

    private static Type[] ReservationTypes() =>
        ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

    private static Dictionary<string, XElement> LoadMembers()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        return XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);
    }

    private static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> TargetsFor(Type type)
    {
        string typeName = DocName(type);
        yield return ("T:" + typeName, Array.Empty<string>(), false);

        foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            yield return ("M:" + typeName + ".#ctor" + ParameterList(ctor.GetParameters()), ParameterNames(ctor.GetParameters()), false);
        }

        foreach (MethodInfo method in type.GetMethods(Members))
        {
            if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
            {
                continue;
            }

            yield return (
                "M:" + typeName + "." + method.Name + ParameterList(method.GetParameters()),
                ParameterNames(method.GetParameters()),
                method.ReturnType != typeof(void));
        }

        foreach (PropertyInfo property in type.GetProperties(Members))
        {
            yield return ("P:" + typeName + "." + property.Name, Array.Empty<string>(), false);
        }

        if (type.IsEnum)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                yield return ("F:" + typeName + "." + field.Name, Array.Empty<string>(), false);
            }
        }
    }

    private static string DocName(Type type) => type.FullName!.Replace('+', '.');

    private static string ParameterList(ParameterInfo[] parameters) =>
        parameters.Length == 0 ? string.Empty : "(" + string.Join(",", parameters.Select(p => DocId(p.ParameterType))) + ")";

    private static string[] ParameterNames(ParameterInfo[] parameters) =>
        parameters.Select(p => p.Name ?? string.Empty).ToArray();

    private static string DocId(Type type)
    {
        if (type.IsByRef)
        {
            return DocId(type.GetElementType()!) + "@";
        }

        if (type.IsArray)
        {
            return DocId(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return "`" + type.GenericParameterPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type.IsGenericType)
        {
            string definition = type.GetGenericTypeDefinition().FullName!;
            string prefix = definition[..definition.IndexOf('`')];
            return prefix + "{" + string.Join(",", type.GetGenericArguments().Select(DocId)) + "}";
        }

        return DocName(type);
    }
}
