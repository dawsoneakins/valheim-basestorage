namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.StationPanel;

/// <summary>AC-4 (R9, ADR-GWIN-041): every public type and member of BaseStorage.Core.StationPanel is documented in BaseStorage.Core.xml.</summary>
public sealed class StationPanelXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string StationPanelNamespace = "BaseStorage.Core.StationPanel";

    // GW-U-548 (AC-4, R9): XmlDocMembers.TargetsFor over the seven types finds a summary on each, a param for each parameter, and a returns on each
    // non-void member. Problems are listed by member id.
    [Fact]
    public void EveryStationPanelTypeAndMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(PanelRect).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == StationPanelNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(7, types.Length);

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in BaseStorage.Core.Tests.Abstractions.XmlDocMembers.TargetsFor(types))
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
}
