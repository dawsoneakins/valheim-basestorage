namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;

/// <summary>
/// AC-1 mapping (log-policy-contract.md v1, sections 2 and 3.1): SeverityOf is pure, total and closed. Expected values are
/// contract literals, never computed from LogPolicy (ADR-GWIN-023).
/// </summary>
public sealed class LogPolicyMappingTests
{
    private static readonly int[] UndefinedValues = { 9, 42, 99, -1, int.MaxValue, int.MinValue };

    // GW-U-200
    [Theory]
    [InlineData(0, LogSeverity.Info)]
    [InlineData(1, LogSeverity.Info)]
    [InlineData(2, LogSeverity.Info)]
    [InlineData(3, LogSeverity.Info)]
    [InlineData(4, LogSeverity.Debug)]
    [InlineData(5, LogSeverity.Debug)]
    [InlineData(6, LogSeverity.Debug)]
    [InlineData(7, LogSeverity.Debug)]
    [InlineData(8, LogSeverity.Debug)]
    public void EveryDefinedCategoryMapsToItsContractSeverityRepeatably(int category, LogSeverity expected)
    {
        Assert.Equal(expected, LogPolicyApi.SeverityOf(category));
        Assert.Equal(expected, LogPolicyApi.SeverityOf(category));
    }

    // GW-U-201
    [Theory]
    [InlineData(9)]
    [InlineData(42)]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void UndefinedCategoriesFailQuietAsDebugWithoutThrowing(int category)
    {
        Assert.Equal(LogSeverity.Debug, LogPolicyApi.SeverityOf(category));
    }

    // GW-U-201
    [Fact]
    public void NoCategoryMapsToWarningOrErrorAndOnlyTheFourNamedOnesMapToInfo()
    {
        var infoNames = new List<string>();
        foreach (int value in Enumerable.Range(0, 9).Concat(UndefinedValues))
        {
            LogSeverity severity = LogPolicyApi.SeverityOf(value);
            Assert.NotEqual(LogSeverity.Warning, severity);
            Assert.NotEqual(LogSeverity.Error, severity);
            if (severity == LogSeverity.Info)
            {
                infoNames.Add(Enum.GetName(LogPolicyApi.CategoryType, value) ?? "undefined");
            }
        }

        Assert.Equal(LogPolicyApi.InfoCategoryNames, infoNames);
    }

    // GW-U-202
    [Fact]
    public void LogCategoryIsClosedAndPinnedToTheContractNamesAndValues()
    {
        Type category = LogPolicyApi.CategoryType;
        string[] expectedNames =
        {
            "Load", "ConfigSync", "OrganizeSummary", "DepositSummary", "MoveDetail",
            "PatchDetail", "SafetyStackTrace", "UiFallback", "Detail",
        };

        Assert.True(category.IsEnum, "LogCategory must be an enum.");
        Assert.Equal(expectedNames, Enum.GetNames(category));
        Assert.Equal(Enumerable.Range(0, 9), Enum.GetValues(category).Cast<object>().Select(v => Convert.ToInt32(v)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(category));
        Assert.False(Attribute.IsDefined(category, typeof(FlagsAttribute)), "LogCategory must not be [Flags].");
    }
}
