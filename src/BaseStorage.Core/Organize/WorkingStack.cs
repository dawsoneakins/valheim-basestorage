namespace BaseStorage.Core.Organize;

/// <summary>One slot's mutable working state inside a single Plan call. Never escapes the call.</summary>
internal sealed class WorkingStack
{
    /// <summary>Creates the working state of one stack.</summary>
    /// <param name="key">The item identity of the stack.</param>
    /// <param name="count">The initial count, at least 1.</param>
    internal WorkingStack(ItemKey key, int count)
    {
        Key = key;
        Count = count;
    }

    /// <summary>Gets the item identity of the stack.</summary>
    internal ItemKey Key { get; }

    /// <summary>Gets or sets the current count. Zero means the stack has moved out and its slot is free.</summary>
    internal int Count { get; set; }
}
