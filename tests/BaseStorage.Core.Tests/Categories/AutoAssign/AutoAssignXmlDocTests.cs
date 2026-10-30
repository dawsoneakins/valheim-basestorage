namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-10 (R9, CS1591): every public member of the AutoAssign namespace is documented in BaseStorage.Core.xml.</summary>
public sealed class AutoAssignXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const string Namespace = "BaseStorage.Core.Categories.AutoAssign";

    // GW-U-533
    [Fact]
    public void EveryTypeMemberAndEnumMemberOfTheNamespaceIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(types))
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
}
