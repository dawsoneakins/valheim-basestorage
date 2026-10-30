namespace BaseStorage.Plugin.Building;

/// <summary>The game-side calls of the build session (contract build-from-base 4.1).</summary>
internal interface IBuildGame
{
    /// <summary>Tells whether the selection still holds (contract 5.7, A1-A5).</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>True when the same piece is still selected.</returns>
    bool IsStillSelected(BuildSelection selection);

    /// <summary>Tells whether the placement is still possible (contract 5.8, PF1-PF3).</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>True when the placement could proceed.</returns>
    bool PreFlight(BuildSelection selection);

    /// <summary>Places the piece through the game once and applies the after-placement steps when it was created.</summary>
    /// <param name="selection">The selection.</param>
    void Commit(BuildSelection selection);

    /// <summary>Puts every taken stack back.</summary>
    /// <returns>The number of stacks.</returns>
    int ReturnTaken();

    /// <summary>Forgets the kept stacks without touching the game.</summary>
    void ForgetTaken();

    /// <summary>Shows the notice with the given string key.</summary>
    /// <param name="key">The string key.</param>
    void ShowNotice(string key);
}
