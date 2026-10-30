namespace BaseStorage.Core.Tests.Deposit;

using System.Globalization;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Plugin.Deposit;

/// <summary>
/// GW-U-1923 (AC-4; deposit-button contract 3.2 and 4.3): the Debug line of a Deposit click. The text is one line, with the enum name of
/// the outcome, or its invariant integer when the value is undefined. Each row is replayed under en-US, de-DE, tr-TR, ar-SA and the
/// invariant culture. Red until the implement phase adds <c>DepositMessages</c>.
/// </summary>
public sealed class DepositMessagesTests
{
    // GW-U-1923 (row 1): the enum name of each defined outcome, and the integer of an undefined one.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("")]
    public void TheClickLineNamesTheOutcomeOrItsInteger(string culture)
    {
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo chosen = culture.Length == 0 ? CultureInfo.InvariantCulture : new CultureInfo(culture);
            CultureInfo.CurrentCulture = chosen;
            CultureInfo.CurrentUICulture = chosen;

            Assert.Equal("[station.actions.deposit-button] Deposit Started", DepositMessages.Clicked(OrganizeStartOutcome.Started));
            Assert.Equal("[station.actions.deposit-button] Deposit LockRefused", DepositMessages.Clicked(OrganizeStartOutcome.LockRefused));
            Assert.Equal("[station.actions.deposit-button] Deposit 99", DepositMessages.Clicked((OrganizeStartOutcome)99));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
