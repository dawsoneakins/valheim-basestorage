namespace BaseStorage.Core.Tests.Commands;

using System;
using System.Globalization;
using BaseStorage.Core.Commands;

/// <summary>
/// GW-U-2300 and GW-U-2301 (AC-6; organize-console-command contract 2.2 and 8.1, rows P1-P14): the argument parse of the console
/// command. Red until <c>BaseStorage.Core.Commands.ConsoleCommandText</c> exists. Not a test row.
/// </summary>
public sealed class ConsoleCommandParseTests
{
    // GW-U-2300 (P1-P7): the accepting argument lists. Index 0 is ignored whatever its case, and blank tokens are dropped.
    [Fact]
    public void TheAcceptingArgumentListsParseToOrganize()
    {
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", "organize" }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", "ORGANIZE" }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "BaseStorage", "Organize" }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", string.Empty, "organize" }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", " organize " }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", "organize", string.Empty }));
        Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new string?[] { "basestorage", null, "organize" }));
    }

    // GW-U-2300 (P14): the comparison ignores the current culture (ui-strings U2), so tr-TR does not change the result.
    [Fact]
    public void TheParseIgnoresTheCurrentCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal(ConsoleCommandRequest.Organize, ConsoleCommandText.Parse(new[] { "basestorage", "ORGANIZE" }));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // GW-U-2301 (P8-P13, CC8): any other argument list is Usage, and Parse never throws.
    [Fact]
    public void AnyOtherArgumentListParsesToUsageWithoutThrowing()
    {
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(new[] { "basestorage" }));
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(new[] { "basestorage", "sort" }));
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(new[] { "basestorage", "organize", "now" }));
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(new[] { "basestorage", "organise" }));
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(Array.Empty<string?>()));
        Assert.Equal(ConsoleCommandRequest.Usage, ConsoleCommandText.Parse(null));
    }
}
