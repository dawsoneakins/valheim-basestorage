namespace BaseStorage.Core.Tests.Consumption;

using Xunit;

/// <summary>AC-4 (contract 4.3 F15, CX9): the same inputs give the same result, the same call sequence and the same clock reads.</summary>
public sealed class ExecutorDeterminismTests
{
    // GW-U-538 (AC-4, F15, CX9): each scenario, run twice on identical fresh worlds, gives equal results, identical calls and identical clock reads.
    [Theory]
    [InlineData("happy")]
    [InlineData("F1")]
    [InlineData("F5")]
    [InlineData("F7")]
    public void TheSameInputsGiveTheSameResultCallsAndClockReads(string name)
    {
        ScenarioRun first = ConsumptionScenarios.Run(name);
        ScenarioRun second = ConsumptionScenarios.Run(name);

        Assert.Equal(first.Result, second.Result);
        Assert.Equal(first.Layer.Calls, second.Layer.Calls);
        Assert.Equal(first.Clock.Reads, second.Clock.Reads);
    }
}
