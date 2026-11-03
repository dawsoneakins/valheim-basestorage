namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>GW-U-2684 (ADR-GWIN-180, R9, CS1591): every public member of <c>BaseStorage.Core.Feeding</c> is documented in BaseStorage.Core.xml.</summary>
public sealed class FeedXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const string Namespace = "BaseStorage.Core.Feeding";

    // GW-U-2684
    [Fact]
    public void EveryFeedingMemberIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), DocFileName + " is missing from " + AppContext.BaseDirectory + ".");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(FeedPlanner).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(6, types.Length);

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
