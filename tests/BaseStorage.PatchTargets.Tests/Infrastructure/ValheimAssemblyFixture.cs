namespace BaseStorage.PatchTargets.Tests.Infrastructure;

using System;
using System.IO;
using System.Reflection;

/// <summary>
/// The game assembly, read-only, through <c>System.Reflection.MetadataLoadContext</c> (contract 4.6.1, ADR-GWIN-015).
/// Lazy: constructing it never touches the disk, because xunit v2 builds collection fixtures even when every test is skipped.
/// </summary>
public sealed class ValheimAssemblyFixture : IDisposable
{
    private readonly object gate = new();
    private readonly Func<(string ManagedDir, string GameDll)> locations;
    private MetadataLoadContext? context;
    private Assembly? game;

    /// <summary>The fixture for <see cref="BuildValues"/>.</summary>
    public ValheimAssemblyFixture()
    {
        locations = () => (BuildValues.ManagedDir, BuildValues.GameDll);
    }

    /// <summary>Explicit paths, for GW-U-107. Does not touch the disk.</summary>
    internal ValheimAssemblyFixture(string managedDir, string gameDll)
    {
        locations = () => (managedDir, gameDll);
    }

    /// <summary>The game assembly, loaded on first use into a MetadataLoadContext that sees only the Managed folder.</summary>
    public Assembly Game
    {
        get
        {
            lock (gate)
            {
                if (game is null)
                {
                    (string managedDir, string gameDll) = locations();
                    PathAssemblyResolver resolver = new(Directory.GetFiles(managedDir, "*.dll"));
                    MetadataLoadContext created = new MetadataLoadContext(resolver, "mscorlib");
                    try
                    {
                        game = created.LoadFromAssemblyPath(gameDll);
                    }
                    catch
                    {
                        created.Dispose();
                        throw;
                    }

                    context = created;
                }

                return game;
            }
        }
    }

    /// <summary>Disposes the MetadataLoadContext if it was created.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            context?.Dispose();
            context = null;
            game = null;
        }
    }
}

/// <summary>Shares one <see cref="ValheimAssemblyFixture"/> across the DLL-backed test classes.</summary>
[CollectionDefinition("ValheimAssembly")]
public sealed class ValheimAssemblyCollection : ICollectionFixture<ValheimAssemblyFixture>
{
}
