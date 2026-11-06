namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>
/// R9 (contract 0.1): every public member of BaseStorage.Core.Persistence has a summary, param and returns in
/// BaseStorage.Core.xml. It reuses the GW-U-040 doc-ID formatter (ADR-GWIN-013).
/// </summary>
public sealed class PersistenceXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";

    // GW-U-079
    [Fact]
    public void EveryPublicPersistenceMemberIsDocumented()
    {
        Type[] types = PersistenceContract.ResolveAll();
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var targets = XmlDocMembers.TargetsFor(types)
            .Concat(XmlDocMembers.ConstantTargets(PersistenceContract.Resolve("PersistenceKeys")));

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
