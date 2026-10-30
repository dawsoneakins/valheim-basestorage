namespace BaseStorage.PatchTargets.Tests.Infrastructure;

using System;
using BaseStorage.PatchTargets.Tests.Checking;

/// <summary>
/// Resolves serialized type names against the game assembly loaded by <see cref="ValheimAssemblyFixture"/>
/// (MetadataLoadContext, read-only). It is the single game-type lookup: the contract tests pass <see cref="Resolve"/> as the
/// checker's findType delegate instead of keeping private copies.
/// </summary>
internal sealed class GameTypeResolver : ITypeNameResolver
{
    private readonly ValheimAssemblyFixture fixture;

    public GameTypeResolver(ValheimAssemblyFixture fixture)
    {
        this.fixture = fixture;
    }

    public Type? Resolve(string typeName)
    {
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            return Resolve(typeName[..^2])?.MakeArrayType();
        }

        return fixture.Game.GetType(typeName, throwOnError: false, ignoreCase: false);
    }

    public bool IsGameType(Type type)
    {
        Type element = type;
        while (element.HasElementType)
        {
            element = element.GetElementType()!;
        }

        return element.Assembly == fixture.Game;
    }
}
