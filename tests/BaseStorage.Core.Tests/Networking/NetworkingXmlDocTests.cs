namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>AC-6.1 (R9, ADR-GWIN-064): every public type and member of BaseStorage.Core.Networking has its summary, its params and its returns in BaseStorage.Core.xml.</summary>
public sealed class NetworkingXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string NetworkingNamespace = "BaseStorage.Core.Networking";

    // GW-U-813: XmlDocMembers.TargetsFor over the five Networking types (plus the PayloadTag const) finds a summary on each, a param for each parameter, and a returns on each non-void member.
    [Fact]
    public void EveryNetworkingTypeAndMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(VersionCheck).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == NetworkingNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(5, types.Length);

        var targets = XmlDocMembers.TargetsFor(types).Concat(XmlDocMembers.ConstantTargets(typeof(VersionHandshake))).ToArray();
        Assert.Contains(targets, target => target.Id == "F:BaseStorage.Core.Networking.VersionHandshake.PayloadTag");

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

        Assert.True(
            problems.Count == 0,
            $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }
}
