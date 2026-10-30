namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Settings;

/// <summary>
/// A dictionary-backed <see cref="ISettingsSource"/> for tests (ADR-GWIN-015). It counts calls to each TryGet method, and
/// when <see cref="Throws"/> is set, every TryGet method throws it. Fluent With methods add values.
/// </summary>
internal sealed class FakeSettingsSource : ISettingsSource
{
    private readonly Dictionary<string, bool> bools = new Dictionary<string, bool>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> ints = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> texts = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Number of TryGetBool calls so far.</summary>
    public int BoolCalls { get; private set; }

    /// <summary>Number of TryGetInt calls so far.</summary>
    public int IntCalls { get; private set; }

    /// <summary>Number of TryGetText calls so far.</summary>
    public int TextCalls { get; private set; }

    /// <summary>When set, every TryGet method throws this exception after it is counted.</summary>
    public Exception? Throws { get; set; }

    /// <summary>Adds a Bool value.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>This fake, for chaining.</returns>
    public FakeSettingsSource WithBool(string key, bool value)
    {
        bools[key] = value;
        return this;
    }

    /// <summary>Adds an Int value.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>This fake, for chaining.</returns>
    public FakeSettingsSource WithInt(string key, int value)
    {
        ints[key] = value;
        return this;
    }

    /// <summary>Adds an ItemList or Hotkey text value.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The text.</param>
    /// <returns>This fake, for chaining.</returns>
    public FakeSettingsSource WithText(string key, string value)
    {
        texts[key] = value;
        return this;
    }

    /// <inheritdoc />
    public bool TryGetBool(string key, out bool value)
    {
        BoolCalls++;
        Rethrow();
        return bools.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public bool TryGetInt(string key, out int value)
    {
        IntCalls++;
        Rethrow();
        return ints.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public bool TryGetText(string key, out string value)
    {
        TextCalls++;
        Rethrow();
        if (texts.TryGetValue(key, out string? found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private void Rethrow()
    {
        if (Throws is not null)
        {
            throw Throws;
        }
    }
}
