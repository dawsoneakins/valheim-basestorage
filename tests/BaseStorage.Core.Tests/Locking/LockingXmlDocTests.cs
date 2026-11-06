namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Locking;

/// <summary>AC-8 (R9, ADR-GWIN-027): every public type and member of BaseStorage.Core.Locking has its summary, its params and its returns in BaseStorage.Core.xml.</summary>
public sealed class LockingXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string LockingNamespace = "BaseStorage.Core.Locking";

    // GW-U-284 (AC-8): XmlDocMembers.TargetsFor over the four Locking types finds a summary on each, a param for each parameter, and a returns on each non-void member.
    [Fact]
    public void EveryLockingTypeAndMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(OrganizeLock).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == LockingNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(4, types.Length);

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
