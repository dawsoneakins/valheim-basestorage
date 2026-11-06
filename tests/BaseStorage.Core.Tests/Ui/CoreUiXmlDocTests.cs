namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// S-FOUND-001 R9: every public type and declared public member of BaseStorage.Core.Ui is documented in
/// BaseStorage.Core.xml. Reuses the GW-U-040 doc-ID approach (ADR-GWIN-011). CoreXmlDocTests.cs is unchanged.
/// </summary>
public sealed class CoreUiXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const BindingFlags DeclaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-060
    [Fact]
    public void EveryPublicUiTypeAndMemberHasASummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        foreach (string name in new[] { "StringTable", "StringKeys", "Plural", "SingleLine", "HoverLineFormatter" })
        {
            UiContract.Find(name);
        }

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        int documented = 0;
        foreach (var (id, parameters, returnsValue) in DocTargets())
        {
            documented++;
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
                problems.Add("blank or missing <returns>: " + id);
            }
        }

        Assert.True(documented > 0, "No public BaseStorage.Core.Ui members were found.");
        Assert.Empty(problems);
    }

    // GW-U-060
    [Fact]
    public void StringKeysDeclaresTheContractPublicConstants()
    {
        FieldInfo[] constants = UiContract.Find("StringKeys")
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral)
            .ToArray();

        Assert.Equal(UiFixtures.StringKeyCount, constants.Length);
    }

    /// <summary>Doc targets: (member ID, parameter names, returns value) for every public type and declared public member.</summary>
    private static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> DocTargets()
    {
        foreach (Type type in ContractTypes.CoreAssembly().GetExportedTypes()
                     .Where(t => t.Namespace == UiContract.Namespace)
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            yield return ("T:" + type.FullName, Array.Empty<string>(), false);

            // Edited by S-E008-001 (ADR-GWIN-153): skip the compiler-generated enum value__ field (special name).
            foreach (FieldInfo field in type.GetFields(DeclaredPublic).Where(f => !f.IsSpecialName))
            {
                yield return ("F:" + type.FullName + "." + field.Name, Array.Empty<string>(), false);
            }

            foreach (PropertyInfo property in type.GetProperties(DeclaredPublic))
            {
                yield return ("P:" + type.FullName + "." + property.Name, Array.Empty<string>(), false);
            }

            foreach (MethodInfo method in type.GetMethods(DeclaredPublic).Where(m => !m.IsSpecialName))
            {
                ParameterInfo[] parameters = method.GetParameters();
                // Edited by S-E003-005: the documentation ID of a parameterless method has no parentheses (RequirementDisplay.GetHashCode).
                string signature = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
                yield return (
                    "M:" + type.FullName + "." + method.Name + (parameters.Length == 0 ? string.Empty : "(" + signature + ")"),
                    parameters.Select(p => p.Name ?? string.Empty).ToArray(),
                    method.ReturnType != typeof(void));
            }
        }
    }
}
