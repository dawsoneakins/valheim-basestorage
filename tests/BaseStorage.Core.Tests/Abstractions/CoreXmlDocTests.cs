namespace BaseStorage.Core.Tests.Abstractions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// AC-8: every public member of the Snapshots and Abstractions namespaces is documented in BaseStorage.Core.xml
/// (ADR-GWIN-006, ADR-GWIN-008). The compiler checks summaries (CS1591), while this test also checks param and returns.
/// </summary>
public sealed class CoreXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";

    // GW-U-040
    [Fact]
    public void DocumentationFileIsWrittenNextToTheCoreAssembly()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);

        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");
    }

    // GW-U-040
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(ContractTypes.SurfaceTypes()))
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
