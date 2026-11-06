namespace BaseStorage.Core.Tests.Networking.Peers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>
/// AC-6.1 (contract version-rpc-contracts.md 1, 2.1, 3.1-3.4, 4; R9): the exported shape of BaseStorage.Core.Networking.Peers,
/// checked by reflection, and its XML documentation.
/// </summary>
public sealed class PeersSurfaceTests
{
    private const string PeersNamespace = "BaseStorage.Core.Networking.Peers";
    private const string DocFileName = "Dons-QOL.Core.xml";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1260 (contract 1): the exported types of the namespace are exactly the six contract names, and the parent namespace keeps its five.
    [Fact]
    public void ExportedTypesAreExactlyTheSixPeerNames()
    {
        string[] exported = typeof(PeerVersionTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == PeersNamespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "PeerVersionAction", "PeerVersionEntry", "PeerVersionMessages", "PeerVersionOutcome", "PeerVersionTable", "VersionRpc" },
            exported);
        Assert.Equal(5, typeof(PeerVersionTable).Assembly.GetExportedTypes().Count(type => type.Namespace == "BaseStorage.Core.Networking"));
    }

    // GW-U-1260 (3.4): the table's members are exactly the contract list, with one public constructor.
    [Fact]
    public void TableMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(PeerVersionTable).IsSealed);
        Assert.Single(typeof(PeerVersionTable).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(
            Sorted(
                "M Clear() : Void",
                "M Find(Int64) : PeerVersionEntry",
                "M Forget(Int64) : Boolean",
                "M Receive(Int64, Byte[], Boolean) : PeerVersionOutcome",
                "M TakeHostMismatch() : VersionCheckResult",
                "M Tick(Boolean) : IReadOnlyList<PeerVersionOutcome>",
                "M Track(Boolean) : Int64",
                "P Count : Int32",
                "P HasHostMismatch : Boolean",
                "P LocalVersion : String",
                "P PeerIds : IReadOnlyList<Int64>"),
            Members(typeof(PeerVersionTable)));

        FieldInfo[] fields = typeof(PeerVersionTable).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(new[] { "DisconnectGrace", "MissingPayloadTimeout" }, fields.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(fields, field => Assert.True(field.IsInitOnly && field.FieldType == typeof(TimeSpan)));
    }

    // GW-U-1260 (3.2, 3.3): entry and outcome have get-only properties, no public constructor, and the contracted members.
    [Fact]
    public void EntryAndOutcomeMembersAreExactlyTheContractList()
    {
        Assert.Empty(typeof(PeerVersionEntry).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(typeof(PeerVersionOutcome).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(
            Sorted(
                "P Deadline : TimeSpan",
                "P DisconnectAt : Nullable<TimeSpan>",
                "P IsDecided : Boolean",
                "P PeerId : Int64",
                "P RemoteIsServer : Boolean",
                "P Result : VersionCheckResult"),
            Members(typeof(PeerVersionEntry)));
        Assert.Equal(
            Sorted(
                "M FormatMessage(StringTable) : String",
                "P Action : PeerVersionAction",
                "P MessageKey : String",
                "P PeerId : Int64",
                "P RemoteIsServer : Boolean",
                "P Result : VersionCheckResult"),
            Members(typeof(PeerVersionOutcome)));

        foreach (Type type in new[] { typeof(PeerVersionEntry), typeof(PeerVersionOutcome), typeof(PeerVersionTable) })
        {
            Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
        }
    }

    // GW-U-1260 (3.1, 2.1): the action enum values and the RPC name constants.
    [Fact]
    public void ActionValuesAndRpcNamesAreContracted()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(PeerVersionAction)));
        Assert.Equal(
            new[] { "Ignore=0", "Accept=1", "RefuseMismatch=2", "RefuseMissing=3", "Disconnect=4", "LeaveMismatch=5" },
            Enum.GetValues(typeof(PeerVersionAction)).Cast<PeerVersionAction>().OrderBy(v => (int)v).Select(v => v + "=" + (int)v).ToArray());

        Assert.True(typeof(VersionRpc).IsAbstract && typeof(VersionRpc).IsSealed);
        Assert.Equal("BaseStorage_Version", VersionRpc.Name);
        Assert.Equal("Error", VersionRpc.VanillaErrorName);
        Assert.Equal(2, typeof(VersionRpc).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Count(f => f.IsLiteral));
        Assert.True(typeof(PeerVersionMessages).IsAbstract && typeof(PeerVersionMessages).IsSealed);
        Assert.Equal(Sorted("M Format(PeerVersionOutcome) : String"), Members(typeof(PeerVersionMessages)));
    }

    // GW-U-1260 (R9): every type, member and constant of the namespace is documented in BaseStorage.Core.xml.
    [Fact]
    public void EveryPeerTypeAndMemberIsDocumentedInTheCoreXmlFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), $"{DocFileName} is missing from {AppContext.BaseDirectory}.");
        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = typeof(PeerVersionTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == PeersNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(6, types.Length);

        var targets = XmlDocMembers.TargetsFor(types)
            .Concat(XmlDocMembers.ConstantTargets(typeof(VersionRpc)))
            .Concat(XmlDocMembers.ConstantTargets(typeof(PeerVersionTable)))
            .ToArray();
        Assert.Contains(targets, target => target.Id == "F:BaseStorage.Core.Networking.Peers.VersionRpc.Name");

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

        Assert.True(problems.Count == 0, $"{problems.Count} documentation problem(s):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
    }

    private static string[] Sorted(params string[] items) => items.OrderBy(item => item, StringComparer.Ordinal).ToArray();

    private static string[] Members(Type type)
    {
        var lines = new List<string>();
        foreach (MethodInfo method in type.GetMethods(PublicDeclared))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            string parameters = string.Join(", ", method.GetParameters().Select(parameter => Name(parameter.ParameterType)));
            lines.Add("M " + method.Name + "(" + parameters + ") : " + Name(method.ReturnType));
        }

        foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
        {
            lines.Add("P " + property.Name + " : " + Name(property.PropertyType));
        }

        return Sorted(lines.ToArray());
    }

    private static string Name(Type type)
    {
        if (type.IsGenericType)
        {
            string definition = type.Name.Substring(0, type.Name.IndexOf('`'));
            return definition + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
        }

        return type.Name;
    }
}
