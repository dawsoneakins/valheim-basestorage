namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Infrastructure;

/// <summary>
/// GW-U-144 (S-FOUND-003 AC-4, ADR-GWIN-018): partial of GameTargetContractTests so the SkipGate tier rule (GW-U-103) sees these DLL-backed facts in an allow-listed class.
/// The facts pin signatures, enum literals and nested fields of the ownership model (ADR-GWIN-017 facts 1-9). The class-level collection and fixture live in GameTargetContractTests.cs.
/// </summary>
public sealed partial class GameTargetContractTests
{
    // GW-U-144 (a)
    [RequiresValheimFact]
    public void ContainerHandshakeSignaturesMatchTheInstalledBuild()
    {
        Type container = GameType("Container");

        Assert.Contains("CheckAccess(System.Int64) : System.Boolean", Describe(container, "CheckAccess"));
        Assert.Contains("IsInUse() : System.Boolean", Describe(container, "IsInUse"));
        Assert.Contains("SetInUse(System.Boolean) : System.Void", Describe(container, "SetInUse"));
        Assert.Contains("Interact(Humanoid,System.Boolean,System.Boolean) : System.Boolean", Describe(container, "Interact"));
        Assert.Contains("Save() : System.Void", Describe(container, "Save"));
        Assert.Contains("Load() : System.Boolean", Describe(container, "Load"));
    }

    // GW-U-144 (b)
    [RequiresValheimFact]
    public void ZNetViewOwnershipSignaturesMatchTheInstalledBuild()
    {
        Type view = GameType("ZNetView");

        Assert.Contains("ClaimOwnership() : System.Void", Describe(view, "ClaimOwnership"));
        Assert.Contains("IsOwner() : System.Boolean", Describe(view, "IsOwner"));
        Assert.Contains("GetZDO() : ZDO", Describe(view, "GetZDO"));
    }

    // GW-U-144 (c)
    [RequiresValheimFact]
    public void ZdoOwnerAndRevisionSignaturesMatchTheInstalledBuild()
    {
        Type zdo = GameType("ZDO");

        Assert.Contains("SetOwner(System.Int64) : System.Void", Describe(zdo, "SetOwner"));
        Assert.Contains("GetOwner() : System.Int64", Describe(zdo, "GetOwner"));
        Assert.Contains("get_DataRevision() : System.UInt32", Describe(zdo, "get_DataRevision"));
    }

    // GW-U-144 (d)
    [RequiresValheimFact]
    public void SessionAndUidGettersAreStaticParameterlessInt64()
    {
        Assert.Contains("static GetSessionID() : System.Int64", Describe(GameType("ZDOMan"), "GetSessionID"));
        Assert.Contains("static GetUID() : System.Int64", Describe(GameType("ZNet"), "GetUID"));
    }

    // GW-U-144 (e)
    [RequiresValheimFact]
    public void PrivateAreaCheckAccessIsStaticBooleanWithTheFourParameterNames()
    {
        MethodInfo check = Assert.Single(Methods(GameType("PrivateArea"), "CheckAccess"));

        Assert.True(check.IsStatic, "PrivateArea.CheckAccess must be static.");
        Assert.Equal("System.Boolean", check.ReturnType.FullName);
        Assert.Equal("point,radius,flash,wardCheck", string.Join(",", check.GetParameters().Select(p => p.Name)));
    }

    // GW-U-144 (f)
    [RequiresValheimFact]
    public void InventoryGuiShowTakesTheContainerAndAnInt()
    {
        Assert.Contains("Show(Container,System.Int32)", SignatureList(GameType("InventoryGui"), "Show"));
    }

    // GW-U-144 (g)
    [RequiresValheimFact]
    public void PrivacySettingEnumLiteralsAreExactlyPrivateGroupAndPublic()
    {
        Type privacy = GameType("Container+PrivacySetting");

        Assert.True(privacy.IsEnum, "Container+PrivacySetting must be an enum.");
        string[] literals = privacy.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => f.Name + "=" + f.GetRawConstantValue()?.ToString())
            .ToArray();

        Assert.Equal(new[] { "Group=1", "Private=0", "Public=2" }, literals);
    }

    // GW-U-144 (h)
    [RequiresValheimFact]
    public void PlayerInfoDeclaresTheNameAndCharacterIdFields()
    {
        Type info = GameType("ZNet+PlayerInfo");

        Assert.Equal("System.String", info.GetField("m_name", AllDeclared)?.FieldType.FullName);
        Assert.Equal("ZDOID", info.GetField("m_characterID", AllDeclared)?.FieldType.FullName);
    }

    // GW-U-144 (i)
    [RequiresValheimFact]
    public void GetPlayerListReturnsAListOfPlayerInfo()
    {
        MethodInfo list = Assert.Single(Methods(GameType("ZNet"), "GetPlayerList"));
        Type returned = list.ReturnType;

        Assert.True(returned.IsGenericType, "ZNet.GetPlayerList must return a generic type.");
        Assert.Equal("System.Collections.Generic.List`1", returned.GetGenericTypeDefinition().FullName);

        Type[] arguments = returned.GetGenericArguments();
        Assert.Equal("ZNet+PlayerInfo", Assert.Single(arguments).FullName);
    }

    // GW-U-144 (j)
    [RequiresValheimFact]
    public void ZdoIdUserIdIsAnInt64Getter()
    {
        Assert.Contains("get_UserID() : System.Int64", Describe(GameType("ZDOID"), "get_UserID"));
    }

    // GW-U-144 (k). Presence only: the Register overloads are generic, so their parameter FullName is null and they
    // cannot be pinned here. This fact does not check any signature.
    [RequiresValheimFact]
    public void ZNetViewDeclaresAtLeastOneRegisterMethodPresenceOnly()
    {
        Assert.NotEmpty(Methods(GameType("ZNetView"), "Register"));
    }

    private Type GameType(string name) => fixture.Game.GetType(name, throwOnError: true)!;

    private static MethodInfo[] Methods(Type type, string name) =>
        type.GetMethods(AllDeclared).Where(m => m.Name == name).ToArray();

    /// <summary>"[static ]Name(ParamFullName,...)" for each declared method with that name.</summary>
    private static string[] SignatureList(Type type, string name) =>
        Methods(type, name).Select(Signature).ToArray();

    /// <summary>"[static ]Name(ParamFullName,...) : ReturnFullName" for each declared method with that name.</summary>
    private static string[] Describe(Type type, string name) =>
        Methods(type, name).Select(m => Signature(m) + " : " + m.ReturnType.FullName).ToArray();

    private static string Signature(MethodInfo method)
    {
        string prefix = method.IsStatic ? "static " : string.Empty;
        string parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
        return prefix + method.Name + "(" + parameters + ")";
    }
}
