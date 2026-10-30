namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-3 (R9, ADR-GWIN-015): every public type, member and constant of BaseStorage.Core.Settings has a summary in
/// BaseStorage.Core.xml, with one param per parameter and returns on non-void methods. The compiler checks only the summary
/// (CS1591). The ID helper is a copy of the one in CoreXmlDocTests, so that earlier files stay unedited.
/// </summary>
public sealed class SettingsXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const string SettingsNamespace = "BaseStorage.Core.Settings";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-101
    [Fact]
    public void EveryPublicSettingsMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

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

            if (returnsValue && member.Element("returns") is null)
            {
                problems.Add("missing <returns>: " + id);
            }
        }

        Assert.True(
            problems.Count == 0,
            $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> DocTargets()
    {
        foreach (Type type in typeof(SettingKind).Assembly.GetExportedTypes().Where(t => t.Namespace == SettingsNamespace))
        {
            string typeName = DocName(type);
            yield return ("T:" + typeName, Array.Empty<string>(), false);

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                yield return ("M:" + typeName + ".#ctor" + ParameterList(ctor.GetParameters()), ParameterNames(ctor.GetParameters()), false);
            }

            foreach (MethodInfo method in type.GetMethods(Members))
            {
                if (method.IsSpecialName)
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

        if (type.IsGenericType)
        {
            string definition = type.GetGenericTypeDefinition().FullName!;
            string prefix = definition[..definition.IndexOf('`')];
            return prefix + "{" + string.Join(",", type.GetGenericArguments().Select(DocId)) + "}";
        }

        return DocName(type);
    }
}
