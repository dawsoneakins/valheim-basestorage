namespace BaseStorage.PatchTargets.Tests.Infrastructure;

/// <summary>
/// Runtime skip for a DLL-backed test (ADR-GWIN-015). <c>Skip</c> is set at discovery from <see cref="SkipGate"/>, so
/// VSTest reports Skipped with the BST2002/BST2003 text. Red stub: discovery throws until implement.
/// </summary>
public sealed class RequiresValheimFactAttribute : FactAttribute
{
    public RequiresValheimFactAttribute()
    {
        Skip = SkipGate.ForBuild(needsPlugin: false);
    }
}

/// <summary>The theory form of <see cref="RequiresValheimFactAttribute"/>.</summary>
public sealed class RequiresValheimTheoryAttribute : TheoryAttribute
{
    public RequiresValheimTheoryAttribute()
    {
        Skip = SkipGate.ForBuild(needsPlugin: false);
    }
}

/// <summary>A Plugin-backed fact: the game DLL and the built Plugin must both exist (BST2002, BST2003, BST2004).</summary>
public sealed class RequiresPluginFactAttribute : FactAttribute
{
    public RequiresPluginFactAttribute()
    {
        Skip = SkipGate.ForBuild(needsPlugin: true);
    }
}
