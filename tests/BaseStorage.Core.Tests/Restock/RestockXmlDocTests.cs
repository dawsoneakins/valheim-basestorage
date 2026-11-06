namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>GW-U-2532 (S-QR-001 AC-6, R9, CS1591): every public member of the restock and identity namespaces is documented in BaseStorage.Core.xml.</summary>
public sealed class RestockXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string RestockNamespace = "BaseStorage.Core.Restock";
    private const string IdentitiesNamespace = "BaseStorage.Core.Snapshots.Identities";

    // GW-U-2532
    [Fact]
    public void EveryRestockAndIdentityMemberIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), DocFileName + " is missing from " + AppContext.BaseDirectory + ".");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(RestockPlanner).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == RestockNamespace || type.Namespace == IdentitiesNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(types);

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
