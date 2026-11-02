namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// Fixtures copied from contract version-handshake-contracts.md sections 2.1, 3.2 and 4.4 (ADR-GWIN-064). Expected values are
/// literals from the contract; only the build version is read from Version.props, never hard-coded.
/// </summary>
internal static class NetworkingFixtures
{
    /// <summary>The contract 3.1 payload tag.</summary>
    public const string Tag = "BSV1;";

    /// <summary>The fixed AC text (contract 4.2, ui-strings-contract.md 3.2 row 43) without placeholders, for building expectations.</summary>
    public const string MismatchPrefix = "Don's QOL version mismatch: host ";

    /// <summary>The joiner between the two versions in the fixed AC text.</summary>
    public const string MismatchJoiner = ", you ";

    /// <summary>Contract 2.1: well-formed versions.</summary>
    public static readonly string[] WellFormed = ["0.1.0", "1.0.0", "10.20.30", "0.0.0", "999999999.0.0", "0.2.0", "0.1.1", "123456789.987654321.5"];

    /// <summary>Contract 2.1: texts that are not versions (null is covered separately).</summary>
    public static readonly string[] IllFormed =
    [
        "",
        "1.0",
        "1.0.0.0",
        "01.0.0",
        "1.0.0-beta",
        "v1.0.0",
        "1..0",
        " 1.0.0",
        "1.0.0 ",
        "1.0.0\n",
        "1.0.0;",
        "1.0.0000000000",
        "١.0.0",
        "1.0.00",
        "-1.0.0",
        "+1.0.0",
        "1.0.0\0",
        ".1.0",
        "1.0.",
        "a.b.c",
    ];

    /// <summary>Contract 3.2: payload texts that decode to null.</summary>
    public static readonly string[] MalformedPayloads =
    [
        "",
        "BSV1;",
        "BSV1",
        "0.1.0",
        "bsv1;0.1.0",
        "BSV2;0.1.0",
        "L1;0.1.0",
        "c:Wood",
        "BSV1;0.1",
        "BSV1;0.1.0;",
        "BSV1;0.1.0 ",
        " BSV1;0.1.0",
        "BSV1;01.0.0",
        "BSV1;0.1.0-rc",
        "BSV1;0.1.0\0",
        "BSV1;0.1.0\n",
        "BSV1;" + new string('9', 100),
        new string('A', 100000),
        "BSV1;BSV1;0.1.0",
        "BSV1;0.1.0BSV1;0.1.0",
    ];

    /// <summary>The Version.props value, read from the file (the single source of the version).</summary>
    public static string VersionPropsVersion() => RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();

    /// <summary>The exact AC-8.1 text for a host and client version, built from the fixed wording.</summary>
    public static string MismatchText(string host, string client) => MismatchPrefix + host + MismatchJoiner + client;

    /// <summary>xunit member data of well-formed versions.</summary>
    public static IEnumerable<object[]> WellFormedRows() => WellFormed.Select(v => new object[] { v });

    /// <summary>xunit member data of ill-formed versions.</summary>
    public static IEnumerable<object[]> IllFormedRows() => IllFormed.Select(v => new object[] { v });

    /// <summary>xunit member data of malformed payloads.</summary>
    public static IEnumerable<object[]> MalformedPayloadRows() => MalformedPayloads.Select(v => new object[] { v });

    /// <summary>Runs an action with both the current and the current UI culture set, restoring them afterwards.</summary>
    public static void UnderCulture(string name, Action action)
    {
        CultureInfo culture = name.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
