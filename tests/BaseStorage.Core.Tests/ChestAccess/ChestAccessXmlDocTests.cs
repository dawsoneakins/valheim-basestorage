namespace BaseStorage.Core.Tests.ChestAccess;

using System.Xml.Linq;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>GW-U-761 (AC-1): every public member of the 4 ChestAccess types is documented in BaseStorage.Core.xml with summary, param and returns.</summary>
public sealed class ChestAccessXmlDocTests
{
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const string Namespace = "BaseStorage.Core.ChestAccess";

    // GW-U-761
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        Dictionary<string, XElement> members = LoadMembers();
        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(4, types.Length);

        var problems = new List<string>();
        foreach ((string id, string[] parameters, bool returnsValue) in XmlDocMembers.TargetsFor(types))
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

        Assert.True(problems.Count == 0, $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }

    private static Dictionary<string, XElement> LoadMembers()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");

        return XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);
    }
}
