namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// L7 and R9 for BaseStorage.Core.Logging (log-policy-contract.md v1, section 0): every public type, member and enum field is
/// documented in BaseStorage.Core.xml, with a non-blank summary, one param per parameter and returns on non-void methods.
/// The member formatter is shared with GW-U-101 (XmlDocMembers), so the rules stay identical (ADR-GWIN-023).
/// </summary>
public sealed class LoggingXmlDocTests
{
    private const string DocFileName = "BaseStorage.Core.xml";

    // GW-U-213
    [Fact]
    public void EveryPublicLoggingMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == LogPolicyApi.Namespace)
            .ToArray();
        Assert.Equal(
            new[] { "DebugGatedLogSink", "LogCategory", "LogPolicy" },
            types.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        int checkedCount = 0;
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(types))
        {
            checkedCount++;
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

        Assert.True(checkedCount >= 18, $"Expected at least 18 Logging doc targets, found {checkedCount}.");
        Assert.True(
            problems.Count == 0,
            $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }
}
