namespace BaseStorage.Plugin.Crafting;

/// <summary>The game-side calls of the craft session (contract craft-from-base 5.3).</summary>
internal interface ICraftGame
{
    /// <summary>Tells whether the selection still holds (contract 5.5, A1-A4).</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>True when the same craft is still selected.</returns>
    bool IsStillSelected(CraftSelection selection);

    /// <summary>Tells whether the vanilla preconditions before item creation hold (contract 5.4, PF1-PF5).</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>True when the craft could proceed.</returns>
    bool PreFlight(CraftSelection selection);

    /// <summary>Calls the game's crafting once so it creates the item.</summary>
    /// <param name="selection">The selection.</param>
    void Commit(CraftSelection selection);

    /// <summary>Puts every taken stack back.</summary>
    /// <returns>The number of stacks.</returns>
    int ReturnTaken();

    /// <summary>Forgets the kept stacks without touching the game.</summary>
    void ForgetTaken();

    /// <summary>Shows the notice with the given string key.</summary>
    /// <param name="key">The string key.</param>
    void ShowNotice(string key);
}
