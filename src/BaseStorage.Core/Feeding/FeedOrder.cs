namespace BaseStorage.Core.Feeding;

using BaseStorage.Core.Snapshots;

/// <summary>One transfer from a chest into a station.</summary>
public sealed class FeedOrder
{
    /// <summary>Creates an order. Only the planner creates orders.</summary>
    /// <param name="chest">The chest to take from.</param>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <param name="amount">How many to move.</param>
    /// <param name="isFuel">True when the item goes in as fuel.</param>
    internal FeedOrder(ContainerId chest, string prefabName, int amount, bool isFuel)
    {
        Chest = chest;
        PrefabName = prefabName;
        Amount = amount;
        IsFuel = isFuel;
    }

    /// <summary>Gets the chest to take from.</summary>
    public ContainerId Chest { get; }

    /// <summary>Gets the item's prefab name.</summary>
    public string PrefabName { get; }

    /// <summary>Gets how many to move.</summary>
    public int Amount { get; }

    /// <summary>Gets a value indicating whether the item goes in as fuel.</summary>
    public bool IsFuel { get; }
}
