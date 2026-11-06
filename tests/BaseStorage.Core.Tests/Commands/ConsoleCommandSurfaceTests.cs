namespace BaseStorage.Core.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Commands;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>
/// GW-U-2316 (AC-6; organize-console-command contract 1 and 2.1, rule R9): the exported surface of <c>BaseStorage.Core.Commands</c>,
/// the enum values, the exact public members of <c>ConsoleCommandText</c> and the XML docs. The strings-table and coordinator pins of the
/// same row are edited in place (RunSurfaceTests, UiFixtures, ChestInUseTests). Not a test row.
/// </summary>
public sealed class ConsoleCommandSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Commands";

    // GW-U-2316 (1): the namespace exports exactly the two contracted types.
    [Fact]
    public void TheNamespaceExportsExactlyTheTwoContractedTypes()
    {
        string[] names = typeof(ConsoleCommandText).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "ConsoleCommandRequest", "ConsoleCommandText" }, names);
    }

    // GW-U-2316 (2.1): the enum has the contracted names and values, and an int underlying type.
    [Fact]
    public void TheRequestEnumHasTheContractedValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ConsoleCommandRequest)));
        Assert.Equal(new[] { "Usage", "Organize" }, Enum.GetNames(typeof(ConsoleCommandRequest)));
        Assert.Equal(new[] { 0, 1 }, Enum.GetValues(typeof(ConsoleCommandRequest)).Cast<int>().ToArray());
    }

    // GW-U-2316 (2.1): ConsoleCommandText has exactly the contracted public members, with the contracted signatures of the functions.
    [Fact]
    public void ConsoleCommandTextHasExactlyTheContractedPublicMembers()
    {
        string[] members = typeof(ConsoleCommandText)
            .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "CommandName", "Description", "OrganizeVerb", "Parse", "Reply", "ReplyKey", "Usage" }, members);
        Assert.Equal(typeof(string), typeof(ConsoleCommandText).GetField("CommandName")!.FieldType);
        Assert.Equal(typeof(string), typeof(ConsoleCommandText).GetField("OrganizeVerb")!.FieldType);
        Assert.NotNull(typeof(ConsoleCommandText).GetMethod("Parse", new[] { typeof(IReadOnlyList<string?>) }));
        Assert.NotNull(typeof(ConsoleCommandText).GetMethod("ReplyKey", new[] { typeof(OrganizeStartOutcome), typeof(OrganizeRunState) }));
        Assert.NotNull(typeof(ConsoleCommandText).GetMethod("Reply", new[] { typeof(OrganizeStartOutcome), typeof(OrganizeRunState), typeof(string) }));
        Assert.NotNull(typeof(ConsoleCommandText).GetMethod("Usage", Type.EmptyTypes));
        Assert.NotNull(typeof(ConsoleCommandText).GetMethod("Description", Type.EmptyTypes));
    }

    // GW-U-2316 (R9): every public member of the two types and the enum members carry a summary, and the parameters and returns.
    [Fact]
    public void EveryPublicMemberOfTheNewTypesIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Dons-QOL.Core.xml");
        Assert.True(File.Exists(path), "BaseStorage.Core.xml is missing from " + AppContext.BaseDirectory + ".");
        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(new[] { typeof(ConsoleCommandText) }))
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

        foreach (var (id, _, _) in XmlDocMembers.ConstantTargets(typeof(ConsoleCommandText)))
        {
            if (!members.ContainsKey(id))
            {
                problems.Add("missing <member>: " + id);
            }
        }

        foreach (string value in new[] { "Usage", "Organize" })
        {
            string id = "F:" + Namespace + ".ConsoleCommandRequest." + value;
            if (!members.TryGetValue(id, out XElement? member) || string.IsNullOrWhiteSpace(member.Element("summary")?.Value))
            {
                problems.Add("missing or blank <summary>: " + id);
            }
        }

        Assert.Empty(problems);
    }
}
