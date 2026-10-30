namespace BaseStorage.Core.Tests.Persistence;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Verbatim example tables of contracts/persistence-contracts.md v1 (sections 3.1-4.4), copied from the contract and not
/// derived from the implementation (ADR-GWIN-013). Control characters and lone surrogates are \u escapes in source.
/// </summary>
public static class PersistenceFixtures
{
    /// <summary>Contract 4.1 encode rows: the category name (null is Keep) and the encoded text.</summary>
    public static readonly (string? Name, string Encoded)[] CategoryEncodings =
    [
        (null, "k"),
        ("Metals & Ores", "c:Metals & Ores"),
        ("Keep", "c:Keep"),
        ("Wood: Fine", "c:Wood: Fine"),
        ("Fårskinn", "c:Fårskinn"),
    ];

    /// <summary>Contract 4.1 positive decode rows: the encoded text and the expected name (null is Keep).</summary>
    public static readonly (string Encoded, string? Name)[] CategoryDecodes =
    [
        ("k", null),
        ("c:Metals & Ores", "Metals & Ores"),
        ("c:Keep", "Keep"),
        ("c:Wood: Fine", "Wood: Fine"),
        ("c:Fårskinn", "Fårskinn"),
    ];

    /// <summary>Contract 4.1 decode-null rows, plus the control and whitespace cases of GW-U-080.</summary>
    public static readonly string?[] CategoryMalformed =
    [
        null, "", " ", "K", "keep", "Keep", "k ", " k", "c:", "c:   ", "c", "C:Wood", "c;Wood",
        "c:Raw\nFood", "c:Wood\u0000", "Wood", "\t", "c:\u00A0", "c:Wood\u007F", "c:Wood\u0085", "kk",
    ];

    /// <summary>Contract 4.2 encode rows: holder id, holder name, ExpiresAt ticks and the encoded text.</summary>
    public static readonly (long HolderId, string HolderName, long Ticks, string Encoded)[] LockEncodings =
    [
        (1923487561234L, "Erik", 36300000000L, "L1;1923487561234;36300000000;Erik"),
        (-6017393921047218534L, "Astrid", 955000000L, "L1;-6017393921047218534;955000000;Astrid"),
        (42L, "Ragnar;the Red", 0L, "L1;42;0;Ragnar;the Red"),
        (long.MinValue, "Sigrún", long.MaxValue, "L1;-9223372036854775808;9223372036854775807;Sigrún"),
        (7L, "Bo", -10000000L, "L1;7;-10000000;Bo"),
    ];

    /// <summary>Contract 4.2 decode-null rows, plus the extra cases of GW-U-081.</summary>
    public static readonly string?[] LockMalformed =
    [
        null, "", "L1", "L1;", "L1;1923487561234", "L1;1923487561234;36300000000",
        "L1;1923487561234;36300000000;", "L1;1923487561234;36300000000;   ",
        "L1;0;36300000000;Erik",
        "L1;+1923487561234;36300000000;Erik", "L1;01923487561234;36300000000;Erik", "L1;-0;36300000000;Erik",
        "L1;1923487561234;3.63E10;Erik", "L1;1923487561234;36 300 000 000;Erik", "L1;1923487561234;36.300.000.000;Erik",
        "L1;\u0661\u0662\u0663;36300000000;Erik",
        "L1;9223372036854775808;36300000000;Erik",
        "L2;1923487561234;36300000000;Erik", "l1;1923487561234;36300000000;Erik", " L1;1923487561234;36300000000;Erik",
        "L1;1923487561234;36300000000;Er\nik",
        "Erik", "k", "F1",
        "L1;-9223372036854775809;0;Bo", "L1;1;99999999999999999999;Bo", "L1;1;2;Bo\r", "L1;;0;Bo",
    ];

    /// <summary>Contract 4.3 encode rows: the input slots (x, y), duplicates included, and the encoded text.</summary>
    public static readonly ((int X, int Y)[] Slots, string Encoded)[] FavouritesEncodings =
    [
        (new (int X, int Y)[] { }, "F1"),
        (new (int X, int Y)[] { (3, 2) }, "F1;3,2"),
        (new (int X, int Y)[] { (2, 1), (7, 0), (0, 1), (2, 1) }, "F1;7,0;0,1;2,1"),
        (new (int X, int Y)[] { (255, 255), (0, 0) }, "F1;0,0;255,255"),
    ];

    /// <summary>Contract 4.3 decode-null rows, plus the extra cases of GW-U-082.</summary>
    public static readonly string?[] FavouritesMalformed =
    [
        null, "", "F", "F1;", "F1;3,2;", "F1;;3,2",
        "F1;0,1;7,0", "F1;2,1;0,1", "F1;3,2;3,2",
        "F1;256,0", "F1;-1,0", "F1;03,2", "F1;+3,2",
        "F1;3", "F1;3,2,1", "F1;a,b", "F1;3, 2", "F1;3.2",
        "F2;3,2", "f1;3,2", " F1", "F1 ",
        "L1;1923487561234;36300000000;Erik", "c:Wood",
        "F1;1000,0", "F1;3,2;\u0000", "F1;0,0;0,0",
    ];

    /// <summary>Culture names of the NFR P2 check. The empty name stands for InvariantCulture.</summary>
    public static readonly string[] Cultures = ["en-US", "de-DE", "tr-TR", "ar-SA", ""];

    /// <summary>Every valid encoded example of the three codecs, in contract order.</summary>
    public static IEnumerable<string> AllEncodedExamples() =>
        CategoryEncodings.Select(e => e.Encoded)
            .Concat(LockEncodings.Select(e => e.Encoded))
            .Concat(FavouritesEncodings.Select(e => e.Encoded));

    /// <summary>MemberData rows for the category decode-null table.</summary>
    public static IEnumerable<object?[]> CategoryMalformedRows() => Rows(CategoryMalformed);

    /// <summary>MemberData rows for the organize lock decode-null table.</summary>
    public static IEnumerable<object?[]> LockMalformedRows() => Rows(LockMalformed);

    /// <summary>MemberData rows for the favourites decode-null table.</summary>
    public static IEnumerable<object?[]> FavouritesMalformedRows() => Rows(FavouritesMalformed);

    private static IEnumerable<object?[]> Rows(IEnumerable<string?> values) => values.Select(v => new object?[] { v });
}
