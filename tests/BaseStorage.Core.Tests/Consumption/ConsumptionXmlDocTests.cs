namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Tests.Abstractions;
using Xunit;

/// <summary>AC-3 (contract 0.1 R9): every public member of the four Consumption types is documented in BaseStorage.Core.xml.</summary>
public sealed class ConsumptionXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";

    // GW-U-540 (AC-3, R9): each type and member has a summary, each parameter a param, and each non-void member a returns.
    [Fact]
    public void EveryPublicMemberOfTheConsumptionTypesIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);
        Type[] types = { typeof(ConsumptionOutcome), typeof(ConsumptionExecutor), typeof(ConsumptionExecution), typeof(ConsumptionResult) };
        List<string> problems = new();

        foreach ((string id, string[] parameters, bool returnsValue) in XmlDocMembers.TargetsFor(types))
        {
            if (!members.TryGetValue(id, out XElement? element))
            {
                problems.Add(id + ": no documentation entry");
                continue;
            }

            if (element.Element("summary") is null)
            {
                problems.Add(id + ": no summary");
            }

            foreach (string parameter in parameters)
            {
                if (!element.Elements("param").Any(param => param.Attribute("name")?.Value == parameter))
                {
                    problems.Add(id + ": no param " + parameter);
                }
            }

            if (returnsValue && element.Element("returns") is null)
            {
                problems.Add(id + ": no returns");
            }
        }

        Assert.Empty(problems);
    }
}
