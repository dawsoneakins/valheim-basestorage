namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Tests.Diagnostics;
using BaseStorage.Core.Tests.Patching;
using BaseStorage.Plugin.Patching;

/// <summary>Shares the static PatchSafety state. Every test resets it in Dispose (ADR-GWIN-021).</summary>
[CollectionDefinition("PatchSafetyStatic", DisableParallelization = true)]
public sealed class PatchSafetyStaticCollection
{
}

/// <summary>
/// AC-1 and AC-2: PatchSafety (linked from src/BaseStorage.Plugin/Patching/PatchSafety.cs) fails closed per patch kind and
/// reports every failure through the reporter. Expected text is copied from ADR-GWIN-020.
/// </summary>
[Collection("PatchSafetyStatic")]
public sealed class PatchSafetyTests : IDisposable
{
    private const string Key = "Fixture.Patch.Run";
    private static readonly string[] Keys =
    {
        "Fixture.Patch.Run",
        "Fixture.Patch.RunPrefix",
        "Fixture.Patch.RunPrefixRef",
        "Fixture.Patch.RunValue",
        "Fixture.Patch.RunTranspiler",
    };

    public void Dispose()
    {
        PatchSafety.Initialize(null);
    }

    // GW-U-132
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void RunSwallowsEveryExceptionAndRunsANonThrowingBodyOnce(int kind, bool withReporter)
    {
        Arrange(withReporter);
        Action throwing = () => throw Make(kind);

        Assert.Null(Record.Exception(() => PatchSafety.Run(Key, throwing)));

        int counter = 0;
        PatchSafety.Run(Key, () => { counter++; });
        Assert.Equal(1, counter);
    }

    // GW-U-133
    [Fact]
    public void RunPrefixReturnsTheBodyResultAndRunsEachBodyOnce()
    {
        Arrange(true);
        int calls = 0;

        Assert.False(PatchSafety.RunPrefix(Key, () => { calls++; return false; }));
        Assert.Equal(1, calls);
        Assert.True(PatchSafety.RunPrefix(Key, () => { calls++; return true; }));
        Assert.Equal(2, calls);
    }

    // GW-U-133
    [Fact]
    public void RunPrefixReturnsTrueSoTheOriginalRunsWhenTheBodyThrows()
    {
        Arrange(true);

        Assert.True(PatchSafety.RunPrefix(Key, () => throw new InvalidOperationException("boom")));
    }

    // GW-U-133
    [Fact]
    public void RunPrefixRefWritesTheResultBackOnSuccess()
    {
        Arrange(true);
        int value = 3;

        bool run = PatchSafety.RunPrefix<int>(Key, ref value, (ref int x) => { x = 7; return false; });

        Assert.Equal(7, value);
        Assert.False(run);
    }

    // GW-U-133
    [Fact]
    public void RunPrefixRefDoesNotCommitTheResultWhenTheBodyThrows()
    {
        Arrange(true);
        int value = 3;

        bool run = PatchSafety.RunPrefix<int>(
            Key,
            ref value,
            (ref int x) =>
            {
                x = 99;
                throw new InvalidOperationException("boom");
            });

        Assert.Equal(3, value);
        Assert.True(run);
    }

    // GW-U-134
    [Fact]
    public void RunPostfixValueReturnsTheBodyResultAndRunsOnce()
    {
        Arrange(true);
        int calls = 0;

        Assert.Equal(10, PatchSafety.Run(Key, 5, x => { calls++; return x * 2; }));
        Assert.Equal(1, calls);
    }

    // GW-U-134
    [Fact]
    public void RunPostfixValueReturnsTheCurrentValueWhenTheBodyThrows()
    {
        Arrange(true);

        Assert.Equal(5, PatchSafety.Run(Key, 5, _ => throw new InvalidOperationException("boom")));
    }

    // GW-U-134
    [Fact]
    public void FinalizerRunReturnsTheOriginalExceptionWhenTheBodyThrows()
    {
        Arrange(true);
        Exception original = new InvalidOperationException("vanilla");

        Exception? result = PatchSafety.Run<Exception?>(Key, original, _ => throw new InvalidOperationException("mod"));

        Assert.Same(original, result);
    }

    // GW-U-134
    [Fact]
    public void FinalizerRunReturnsNullWhenVanillaHadNoException()
    {
        Arrange(true);

        Exception? result = PatchSafety.Run<Exception?>(Key, null, _ => throw new InvalidOperationException("mod"));

        Assert.Null(result);
    }

    // GW-U-135 (a)
    [Fact]
    public void TranspilerSuccessIsMaterialisedSoTheBodyRunsOncePerItem()
    {
        Arrange(true);
        int mapCalls = 0;
        int[] input = { 1, 2, 3 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(
            Key,
            input,
            seq => seq.Select(x => { mapCalls++; return x + 1; }));

        Assert.Equal(new[] { 2, 3, 4 }, result);
        Assert.Equal(new[] { 2, 3, 4 }, result);
        Assert.Equal(3, mapCalls);
    }

    // GW-U-135 (b)
    [Fact]
    public void LazyTranspilerThatThrowsMidwayFallsBackToTheOriginal()
    {
        Arrange(true);
        int[] input = { 1, 2, 3 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(Key, input, seq => YieldTwoThenThrow(seq));

        Assert.Equal(input, result);
    }

    // GW-U-135 (c)
    [Fact]
    public void TranspilerReturningNullFallsBackToTheOriginal()
    {
        Arrange(true);
        int[] input = { 1, 2, 3 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(Key, input, _ => null!);

        Assert.Equal(input, result);
    }

    // GW-U-135 (d)
    [Fact]
    public void TranspilerThatMutatesItsListAndThenThrowsFallsBackToTheOriginal()
    {
        Arrange(true);
        int[] input = { 1, 2, 3 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(
            Key,
            input,
            seq =>
            {
                ((List<int>)seq).Clear();
                throw new InvalidOperationException("boom");
            });

        Assert.Equal(input, result);
    }

    // GW-U-135 (e)
    [Fact]
    public void TranspilerThatThrowsDirectlyFallsBackToTheOriginal()
    {
        Arrange(true);
        int[] input = { 1, 2, 3 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(Key, input, _ => throw new InvalidOperationException("direct"));

        Assert.Equal(input, result);
    }

    // GW-U-136
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryOverloadStillFailsClosedWhateverTheReporterDoes(bool throwingSink)
    {
        PatchSafety.Initialize(throwingSink ? new RateLimitedErrorReporter(new ThrowingLogSink()) : null);
        var boom = new InvalidOperationException("boom");
        int[] input = { 1, 2, 3 };
        int value = 3;

        PatchSafety.Run(Key, () => throw boom);
        Assert.True(PatchSafety.RunPrefix(Key, () => throw boom));
        Assert.True(PatchSafety.RunPrefix<int>(Key, ref value, (ref int x) => throw boom));
        Assert.Equal(3, value);
        Assert.Equal(5, PatchSafety.Run(Key, 5, _ => throw boom));
        Assert.Equal(input, PatchSafety.RunTranspiler<int>(Key, input, _ => throw boom));
    }

    // GW-U-137
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void FailureIsReportedOncePerOverloadKeyThroughTheHelper(int overload)
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        string key = Keys[overload];
        var boom = new InvalidOperationException("boom");

        CallWithThrowingBody(overload, key, boom);

        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(LogSeverity.Error, sink.Entries[0].Severity);
        Assert.StartsWith("Patch '" + key + "' failed and was skipped: ", sink.Entries[0].Message, StringComparison.Ordinal);
        Assert.Equal(LogSeverity.Debug, sink.Entries[1].Severity);
        Assert.StartsWith("Patch '" + key + "' stack trace: ", sink.Entries[1].Message, StringComparison.Ordinal);

        CallWithThrowingBody(overload, key, boom);

        Assert.Equal(2, sink.Entries.Count);
    }

    // GW-U-137
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SuccessfulBodiesWriteNoEntries(int overload)
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));

        CallWithSucceedingBody(overload, Keys[overload]);

        Assert.Empty(sink.Entries);
    }

    // GW-U-137
    [Fact]
    public void TranspilerNullResultIsLoggedAsTheDocumentedInvalidOperation()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        int[] input = { 1, 2 };

        IEnumerable<int> result = PatchSafety.RunTranspiler<int>(Keys[4], input, _ => null!);

        Assert.Equal(input, result);
        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(
            "Patch 'Fixture.Patch.RunTranspiler' failed and was skipped: System.InvalidOperationException: Transpiler returned null. (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    private static void Arrange(bool withReporter)
    {
        PatchSafety.Initialize(withReporter ? new RateLimitedErrorReporter(new RecordingLogSink { DebugEnabled = true }) : null);
    }

    private static Exception Make(int kind) => kind switch
    {
        0 => new InvalidOperationException("boom"),
        1 => new NullReferenceException(),
        2 => new MissingMethodException(),
        3 => new CustomPatchException("boom"),
        _ => new MessageThrowingException(),
    };

    private static IEnumerable<int> YieldTwoThenThrow(IEnumerable<int> seq)
    {
        yield return 9;
        yield return 9;
        throw new InvalidOperationException("late");
    }

    private static void CallWithThrowingBody(int overload, string key, Exception boom)
    {
        switch (overload)
        {
            case 0:
                PatchSafety.Run(key, () => throw boom);
                break;
            case 1:
                PatchSafety.RunPrefix(key, () => throw boom);
                break;
            case 2:
                {
                    int value = 3;
                    PatchSafety.RunPrefix<int>(key, ref value, (ref int x) => throw boom);
                    break;
                }

            case 3:
                PatchSafety.Run(key, 5, _ => throw boom);
                break;
            default:
                PatchSafety.RunTranspiler<int>(key, new[] { 1 }, _ => throw boom);
                break;
        }
    }

    private static void CallWithSucceedingBody(int overload, string key)
    {
        switch (overload)
        {
            case 0:
                PatchSafety.Run(key, () => { });
                break;
            case 1:
                PatchSafety.RunPrefix(key, () => true);
                break;
            case 2:
                {
                    int value = 3;
                    PatchSafety.RunPrefix<int>(key, ref value, (ref int x) => { x = 7; return false; });
                    break;
                }

            case 3:
                PatchSafety.Run(key, 5, x => x * 2);
                break;
            default:
                PatchSafety.RunTranspiler<int>(key, new[] { 1 }, seq => seq);
                break;
        }
    }
}
