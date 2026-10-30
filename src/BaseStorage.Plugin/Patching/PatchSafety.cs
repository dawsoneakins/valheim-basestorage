namespace BaseStorage.Plugin.Patching;

using System;
using System.Collections.Generic;
using System.Threading;
using BaseStorage.Core.Diagnostics;

/// <summary>
/// Fail-closed wrappers for Harmony patch bodies (ADR-GWIN-020). A failing body is reported through the reporter and
/// the patch falls back to its vanilla behaviour. Uses only BCL and Core types, so the same file is compiled into the tests.
/// </summary>
internal static class PatchSafety
{
    private static RateLimitedErrorReporter? reporter;

    /// <summary>A prefix body that receives the current result by reference and returns true to run the original.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="result">The result, passed by reference.</param>
    /// <returns>True to run the original method, false to skip it.</returns>
    internal delegate bool PrefixBody<TResult>(ref TResult result);

    /// <summary>Sets the reporter used for failures. Null detaches it (tests).</summary>
    /// <param name="reporter">The reporter, or null.</param>
    internal static void Initialize(RateLimitedErrorReporter? reporter)
    {
        Volatile.Write(ref PatchSafety.reporter, reporter);
    }

    /// <summary>Runs a void prefix or postfix body. A failure is reported and swallowed.</summary>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="body">The body.</param>
    internal static void Run(string patchKey, Action body)
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
        }
    }

    /// <summary>
    /// Runs a void prefix or postfix body with its state passed explicitly, so a static lambda allocates nothing (contract 3).
    /// A failure is reported and swallowed, as for <see cref="Run(string, Action)"/>. The body runs exactly once with the state.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="state">The state passed to the body.</param>
    /// <param name="body">The body. A null body is a failure of that patch: it is reported and swallowed.</param>
    internal static void Run<TState>(string patchKey, TState state, Action<TState> body)
    {
        try
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            body(state);
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
        }
    }

    /// <summary>Runs a boolean prefix body. A failure returns true, so the original runs.</summary>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="body">The body.</param>
    /// <returns>The body result, or true on failure.</returns>
    internal static bool RunPrefix(string patchKey, Func<bool> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return true;
        }
    }

    /// <summary>Runs a prefix body that may set a result. The result is written back only when the body succeeds.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="result">The result, passed by reference.</param>
    /// <param name="body">The body.</param>
    /// <returns>The body result, or true on failure.</returns>
    internal static bool RunPrefix<TResult>(string patchKey, ref TResult result, PrefixBody<TResult> body)
    {
        try
        {
            TResult copy = result;
            bool run = body(ref copy);
            result = copy;
            return run;
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return true;
        }
    }

    /// <summary>Runs a postfix or finalizer body on a value. A failure returns the current value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="current">The value that vanilla produced.</param>
    /// <param name="body">The body.</param>
    /// <returns>The body result, or <paramref name="current"/> on failure.</returns>
    internal static T Run<T>(string patchKey, T current, Func<T, T> body)
    {
        try
        {
            return body(current);
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return current;
        }
    }

    /// <summary>
    /// Runs a postfix body that gets a state and the value vanilla produced, and returns the value to use (ADR-GWIN-032). A failure is
    /// reported once per key and returns <paramref name="current"/> itself, so the vanilla value survives. A static lambda allocates nothing.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TResult">The value type.</typeparam>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="state">The state passed to the body as its first argument.</param>
    /// <param name="current">The value that vanilla produced; returned unchanged on failure.</param>
    /// <param name="body">The body, which receives the state and the current value. A null body is a failure and is reported.</param>
    /// <returns>The body result, or <paramref name="current"/> on failure.</returns>
    internal static TResult Run<TState, TResult>(string patchKey, TState state, TResult current, Func<TState, TResult, TResult> body)
    {
        try
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return body(state, current);
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return current;
        }
    }

    /// <summary>Runs a transpiler body. A failure returns the original instructions.</summary>
    /// <typeparam name="T">The instruction type.</typeparam>
    /// <param name="patchKey">The patch key.</param>
    /// <param name="instructions">The vanilla instructions.</param>
    /// <param name="body">The body.</param>
    /// <returns>The materialised result, or the original instructions on failure.</returns>
    internal static IEnumerable<T> RunTranspiler<T>(
        string patchKey,
        IEnumerable<T> instructions,
        Func<IEnumerable<T>, IEnumerable<T>> body)
    {
        List<T> original;
        try
        {
            original = new List<T>(instructions);
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return instructions;
        }

        try
        {
            IEnumerable<T>? produced = body(new List<T>(original));
            if (produced is null)
            {
                throw new InvalidOperationException("Transpiler returned null.");
            }

            return new List<T>(produced);
        }
        catch (Exception ex)
        {
            Report(patchKey, ex);
            return original;
        }
    }

    private static void Report(string patchKey, Exception error)
    {
        try
        {
            Volatile.Read(ref reporter)?.Report(patchKey, error);
        }
        catch (Exception)
        {
            // Reporting must never throw into the game.
        }
    }
}
