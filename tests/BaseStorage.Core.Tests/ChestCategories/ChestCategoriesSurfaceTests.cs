namespace BaseStorage.Core.Tests.ChestCategories;

using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Tests.Abstractions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>GW-U-1212, GW-U-1213 (AC-1..AC-4): the exported surface of BaseStorage.Core.ChestCategories is exactly the 8 contract types, documented.</summary>
public sealed class ChestCategoriesSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.ChestCategories";
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1212
    [Fact]
    public void NamespaceExportsExactlyTheEightContractedTypes()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .Select(t => t.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                Namespace + ".CategoryCycle",
                Namespace + ".CategoryWriteOutcome",
                Namespace + ".CategoryWriteResult",
                Namespace + ".CategoryWriteRules",
                Namespace + ".CategoryWriteStatus",
                Namespace + ".ChestCategoryReader",
                Namespace + ".PendingCategoryWrite",
                Namespace + ".PendingWriteAction",
            },
            exported);
    }

    // GW-U-1212: the two static classes have exactly the contract methods.
    [Fact]
    public void StaticClassesHaveExactlyTheContractMethods()
    {
        Assert.Equal(
            new[]
            {
                "IndexOf(CategoryAssignment,CategoryCatalog):Int32",
                "Next(CategoryAssignment,CategoryCatalog):CategoryAssignment",
                "Order(CategoryCatalog):IReadOnlyList`1",
                "Previous(CategoryAssignment,CategoryCatalog):CategoryAssignment",
            },
            Signatures(typeof(CategoryCycle)));

        Assert.Equal(
            new[]
            {
                "Effective(CategoryAssignment,CategoryCatalog):CategoryAssignment",
                "IsKnown(CategoryAssignment,CategoryCatalog):Boolean",
                "Label(CategoryAssignment,StringTable):String",
                "Read(String):CategoryAssignment",
                "StoredValue(CategoryAssignment):String",
                "WouldChange(CategoryAssignment,String):Boolean",
            },
            Signatures(typeof(ChestCategoryReader)));

        Assert.Equal(
            new[]
            {
                "ActionFor(Nullable`1):PendingWriteAction",
                "Deadline(TimeSpan,Int32):TimeSpan",
                "FailureOutcome(Nullable`1,ReservationFailure):CategoryWriteOutcome",
                "NoticeKey(CategoryWriteOutcome):String",
                "OutcomeFor(ReservationFailure):CategoryWriteOutcome",
                "ReasonKey(CategoryWriteOutcome):String",
                "StatusOf(CategoryWriteOutcome):CategoryWriteStatus",
            },
            Signatures(typeof(CategoryWriteRules)));
    }

    // GW-U-1212: the two value types are sealed with get-only properties and no setter.
    [Theory]
    [InlineData(typeof(PendingCategoryWrite))]
    [InlineData(typeof(CategoryWriteResult))]
    public void ValueTypesAreSealedWithGetOnlyProperties(Type type)
    {
        Assert.True(type.IsSealed);
        Assert.All(type.GetProperties(PublicDeclared), p => Assert.Null(p.SetMethod));
        Assert.All(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic), f => Assert.True(f.IsInitOnly));
    }

    // GW-U-1213
    [Fact]
    public void EveryPublicMemberHasItsSummaryParamAndReturnsDocumentation()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");
        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(8, types.Length);

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

    private static string[] Signatures(Type type)
    {
        Assert.True(type.IsAbstract && type.IsSealed, type.Name + " must be static.");
        return type.GetMethods(PublicDeclared)
            .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + "):" + m.ReturnType.Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }
}
