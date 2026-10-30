namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A test-local dictionary that logs every member call with its key (ADR-GWIN-045). It proves FV3: one key, one write per
/// toggle, and no enumeration. Seeding through the constructor is not logged. Not a test row.
/// </summary>
internal sealed class RecordingDictionary : IDictionary<string, string>
{
    private readonly Dictionary<string, string> inner = new(StringComparer.Ordinal);

    /// <summary>Creates an empty recorder.</summary>
    public RecordingDictionary()
    {
    }

    /// <summary>Creates a recorder seeded with entries. Seeding is not logged.</summary>
    /// <param name="seed">The entries to copy in.</param>
    public RecordingDictionary(IEnumerable<KeyValuePair<string, string>> seed)
    {
        foreach (KeyValuePair<string, string> entry in seed)
        {
            inner[entry.Key] = entry.Value;
        }
    }

    /// <summary>Gets the ordered log of member calls, for example <c>Set:basestorage.fav</c>.</summary>
    public List<string> Calls { get; } = new();

    /// <summary>Gets the number of successful writes: Set, Add, Remove and Clear.</summary>
    public int WriteCount { get; private set; }

    /// <summary>Gets the key and value of every successful Set or Add, in order.</summary>
    public List<(string Key, string Value)> Writes { get; } = new();

    /// <summary>Gets or sets the exception thrown by every write, before the write takes effect.</summary>
    public Exception? ThrowOnWrite { get; set; }

    /// <inheritdoc />
    public string this[string key]
    {
        get
        {
            Calls.Add("Get:" + key);
            return inner[key];
        }

        set
        {
            RecordWrite("Set:" + key);
            Writes.Add((key, value));
            inner[key] = value;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get
        {
            Calls.Add("Keys");
            return inner.Keys;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Values
    {
        get
        {
            Calls.Add("Values");
            return inner.Values;
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            Calls.Add("Count");
            return inner.Count;
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly
    {
        get
        {
            Calls.Add("IsReadOnly");
            return false;
        }
    }

    /// <inheritdoc />
    public void Add(string key, string value)
    {
        RecordWrite("Add:" + key);
        Writes.Add((key, value));
        inner.Add(key, value);
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, string> item)
    {
        RecordWrite("Add:" + item.Key);
        Writes.Add((item.Key, item.Value));
        inner.Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        RecordWrite("Clear");
        inner.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, string> item)
    {
        Calls.Add("Contains:" + item.Key);
        return ((ICollection<KeyValuePair<string, string>>)inner).Contains(item);
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        Calls.Add("ContainsKey:" + key);
        return inner.ContainsKey(key);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        Calls.Add("CopyTo");
        ((ICollection<KeyValuePair<string, string>>)inner).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        Calls.Add("Enumerate");
        return inner.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        Calls.Add("Enumerate");
        return inner.GetEnumerator();
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        RecordWrite("Remove:" + key);
        return inner.Remove(key);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, string> item)
    {
        RecordWrite("Remove:" + item.Key);
        return ((ICollection<KeyValuePair<string, string>>)inner).Remove(item);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        Calls.Add("TryGetValue:" + key);
        return inner.TryGetValue(key, out value);
    }

    private void RecordWrite(string call)
    {
        Calls.Add(call);
        if (ThrowOnWrite is not null)
        {
            throw ThrowOnWrite;
        }

        WriteCount++;
    }
}
