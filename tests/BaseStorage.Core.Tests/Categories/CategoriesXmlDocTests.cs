namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-1 (R9): every public member of BaseStorage.Core.Categories is documented in BaseStorage.Core.xml (ADR-GWIN-019).</summary>
public sealed class CategoriesXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";

    // GW-U-159
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(type => type.Namespace == "BaseStorage.Core.Categories")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        List<(string Id, string[] Parameters, bool ReturnsValue)> targets = XmlDocMembers.TargetsFor(types).ToList();
        targets.AddRange(XmlDocMembers.ConstantTargets(typeof(FallbackCategories)));

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
}
