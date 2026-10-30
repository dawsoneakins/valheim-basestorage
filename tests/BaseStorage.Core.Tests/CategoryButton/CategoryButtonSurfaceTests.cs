namespace BaseStorage.Core.Tests.CategoryButton;

using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.CategoryButton;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>GW-U-1304, GW-U-1305 (AC-1): the exported surface of BaseStorage.Core.CategoryButton is exactly the contract type, documented.</summary>
public sealed class CategoryButtonSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.CategoryButton";
    private const string DocFileName = "BaseStorage.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1304
    [Fact]
    public void NamespaceExportsExactlyTheLayoutType()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .Select(t => t.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { Namespace + ".CategoryButtonLayout" }, exported);
        Assert.True(typeof(CategoryButtonLayout).IsAbstract && typeof(CategoryButtonLayout).IsSealed, "CategoryButtonLayout must be static.");
    }

    // GW-U-1304: exactly the three contract methods and the one constant.
    [Fact]
    public void StaticClassHasExactlyTheContractMembers()
    {
        string[] methods = typeof(CategoryButtonLayout).GetMethods(PublicDeclared)
            .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + "):" + m.ReturnType.Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        FieldInfo[] fields = typeof(CategoryButtonLayout).GetFields(PublicDeclared);

        Assert.Equal(
            new[]
            {
                "AnchoredX(Single,Single,Boolean,Single):Single",
                "GrowsLeft(Single):Boolean",
                "OuterX(Single,Single,Boolean):Single",
            },
            methods);
        Assert.Single(fields);
        Assert.Equal("Gap", fields[0].Name);
        Assert.True(fields[0].IsLiteral);
        Assert.Equal(8f, fields[0].GetRawConstantValue());
    }

    // GW-U-1305
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");
        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        var problems = new List<string>();
        foreach ((string id, string[] parameters, bool returnsValue) in XmlDocMembers.TargetsFor(new[] { typeof(CategoryButtonLayout) }))
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
}
