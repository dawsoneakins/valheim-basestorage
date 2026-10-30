namespace BaseStorage.PatchTargets.Tests.Fixtures;

using System;
using BaseStorage.PatchTargets.Tests.Checking;

/// <summary>
/// Resolves names against this test assembly and the runtime (System types), for the offline Harmony scenarios.
/// <paramref name="gameTypes"/> decides what IsGameType reports.
/// </summary>
internal sealed class FixtureResolver : ITypeNameResolver
{
    private readonly bool gameTypes;

    public FixtureResolver(bool gameTypes = true)
    {
        this.gameTypes = gameTypes;
    }

    public Type? Resolve(string typeName)
    {
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            return Resolve(typeName[..^2])?.MakeArrayType();
        }

        return typeof(FixtureResolver).Assembly.GetType(typeName, throwOnError: false)
            ?? Type.GetType(typeName, throwOnError: false);
    }

    public bool IsGameType(Type type) => gameTypes;
}
