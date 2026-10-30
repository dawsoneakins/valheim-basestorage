namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Plugin.Crafting;

/// <summary>
/// The game double of contract 5.3: records every call in <see cref="Calls"/>, answers the pre-flight and abandonment reads
/// from settable flags, and runs the commit callback (by default the committed resources of the vanilla call). Not a test row
/// (ADR-GWIN-124).
/// </summary>
internal sealed class FakeCraftGame : ICraftGame
{
    private readonly FakeCraftWorld world;

    /// <summary>Initialises the double over a craft world.</summary>
    /// <param name="world">The world whose kept clones the return and forget calls act on.</param>
    internal FakeCraftGame(FakeCraftWorld world)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <summary>Gets or sets the session the default commit calls back into.</summary>
    internal CraftSession? Session { get; set; }

    /// <summary>Gets the ordered call record.</summary>
    internal List<string> Calls { get; } = new();

    /// <summary>Gets the notice keys shown, in order.</summary>
    internal List<string> Notices { get; } = new();

    /// <summary>Gets or sets the answer of IsStillSelected.</summary>
    internal bool StillSelected { get; set; } = true;

    /// <summary>Gets or sets the answer of PreFlight.</summary>
    internal bool PreFlightResult { get; set; } = true;

    /// <summary>Gets or sets the commit callback; null means the default callback calls OnConsumeResources(true).</summary>
    internal Action<CraftSelection>? CommitBody { get; set; }

    /// <inheritdoc />
    public bool IsStillSelected(CraftSelection selection)
    {
        Calls.Add("IsStillSelected");
        return StillSelected;
    }

    /// <inheritdoc />
    public bool PreFlight(CraftSelection selection)
    {
        Calls.Add("PreFlight");
        return PreFlightResult;
    }

    /// <inheritdoc />
    public void Commit(CraftSelection selection)
    {
        Calls.Add("Commit");
        if (CommitBody is not null)
        {
            CommitBody(selection);
            return;
        }

        Session!.OnConsumeResources(true);
    }

    /// <inheritdoc />
    public int ReturnTaken()
    {
        Calls.Add("ReturnTaken");
        return world.ReturnAllTaken();
    }

    /// <inheritdoc />
    public void ForgetTaken()
    {
        Calls.Add("ForgetTaken");
        world.ForgetTaken();
    }

    /// <inheritdoc />
    public void ShowNotice(string key)
    {
        Calls.Add("ShowNotice");
        Notices.Add(key);
    }
}
