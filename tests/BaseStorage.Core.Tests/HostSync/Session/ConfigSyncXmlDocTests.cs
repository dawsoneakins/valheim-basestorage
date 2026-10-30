namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>AC-8 (R9, ADR-GWIN-121): every public type and member of BaseStorage.Core.HostSync.Session has its summary, params and returns in BaseStorage.Core.xml.</summary>
public sealed class ConfigSyncXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";
    private const string SessionNamespace = "BaseStorage.Core.HostSync.Session";

    // GW-U-1401: XmlDocMembers.TargetsFor over the eight Session types (plus the ConfigSyncRpc constant) finds a summary on each, a param for each parameter, and a returns on each non-void member.
    [Fact]
    public void EveryConfigSyncTypeAndMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(ConfigSyncSession).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == SessionNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(8, types.Length);

        var targets = XmlDocMembers.TargetsFor(types).Concat(XmlDocMembers.ConstantTargets(typeof(ConfigSyncRpc))).ToArray();
        Assert.Contains(targets, target => target.Id == "F:BaseStorage.Core.HostSync.Session.ConfigSyncRpc.Name");

        var problems = new List<string>();
        foreach (var (rawId, parameters, returnsValue) in targets)
        {
            // XmlDocMembers writes by-reference parameters as "Type&"; the compiler's doc IDs write them as "Type@".
            string id = rawId.Replace('&', '@');
            if (!members.TryGetValue(id, out XElement? member))
            {
                problems.Add("missing <member>: " + id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(member.Element("summary")?.Value) && member.Element("inheritdoc") is null)
            {
                problems.Add("blank or missing <summary>: " + id);
            }

            if (member.Element("inheritdoc") is not null)
            {
                continue;
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
