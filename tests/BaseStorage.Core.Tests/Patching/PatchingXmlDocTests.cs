namespace BaseStorage.Core.Tests.Patching;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// R9 for BaseStorage.Core.Patching (contracts/plugin-boot.md section 1.1, ADR-GWIN-013): every public type and member is
/// documented in BaseStorage.Core.xml, with a non-blank summary, one param per parameter and returns on non-void methods.
/// The helper is private to this file so that CoreXmlDocTests (GW-U-040) stays unedited.
/// </summary>
public sealed class PatchingXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string PatchingNamespace = "BaseStorage.Core.Patching";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-080
    [Fact]
    public void EveryPublicPatchingMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        int checkedCount = 0;
        foreach (var (id, parameters, returnsValue) in DocTargets())
        {
            checkedCount++;
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

        Assert.True(checkedCount >= 14, $"Expected at least 14 Patching doc targets, found {checkedCount}.");
        Assert.True(
            problems.Count == 0,
            $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> DocTargets()
    {
        IEnumerable<Type> types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == PatchingNamespace);

        foreach (Type type in types)
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
        }
    }

    private static string DocName(Type type) => type.FullName!.Replace('+', '.');

    private static string ParameterList(ParameterInfo[] parameters) =>
        parameters.Length == 0 ? string.Empty : "(" + string.Join(",", parameters.Select(p => DocId(p.ParameterType))) + ")";

    private static string[] ParameterNames(ParameterInfo[] parameters) =>
        parameters.Select(p => p.Name ?? string.Empty).ToArray();

    private static string DocId(Type type)
    {
        if (type.IsArray)
        {
            return DocId(type.GetElementType()!) + "[]";
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
