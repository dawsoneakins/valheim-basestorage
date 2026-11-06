namespace BaseStorage.Core.Compendium;

using System;
using System.Collections.Generic;

/// <summary>What the player has discovered. The compendium shows nothing outside this.</summary>
public sealed class CompendiumKnowledge
{
    /// <summary>Gets the ids of the items they know.</summary>
    public HashSet<string> Items { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the ids of the creatures they've seen.</summary>
    public HashSet<string> Creatures { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the biomes they've been to.</summary>
    public HashSet<CompendiumBiome> Biomes { get; } = new HashSet<CompendiumBiome>();

    /// <summary>Gets the ids of the pieces they can build.</summary>
    public HashSet<string> Pieces { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the display names of the stations they know.</summary>
    public HashSet<string> Stations { get; } = new HashSet<string>(StringComparer.Ordinal);
}

/// <summary>The creatures and biomes the mod has seen the player meet, as saved on the character. The game keeps no list of its own for creatures.</summary>
public sealed class CompendiumSightings
{
    private const char Separator = ';';
    private const string BiomePrefix = "biome:";

    private readonly HashSet<string> entries = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="CompendiumSightings"/> class.</summary>
    /// <param name="saved">The saved text, which may be null, empty or damaged.</param>
    public CompendiumSightings(string? saved)
    {
        foreach (string entry in (saved ?? string.Empty).Split(Separator))
        {
            if (entry.Length > 0)
            {
                entries.Add(entry);
            }
        }
    }

    /// <summary>Records a creature.</summary>
    /// <param name="creatureId">The creature's prefab name.</param>
    /// <returns>True when it's new.</returns>
    public bool AddCreature(string? creatureId) =>
        !string.IsNullOrEmpty(creatureId) && creatureId!.IndexOf(Separator) < 0 && entries.Add(creatureId);

    /// <summary>Records a biome.</summary>
    /// <param name="biome">The biome.</param>
    /// <returns>True when it's new.</returns>
    public bool AddBiome(CompendiumBiome biome) => entries.Add(BiomePrefix + biome.ToString());

    /// <summary>Adds what's recorded to the player's knowledge. An entry for a biome that no longer exists is ignored.</summary>
    /// <param name="knowledge">The knowledge.</param>
    public void CopyTo(CompendiumKnowledge knowledge)
    {
        if (knowledge == null)
        {
            throw new ArgumentNullException(nameof(knowledge));
        }

        foreach (string entry in entries)
        {
            if (!entry.StartsWith(BiomePrefix, StringComparison.Ordinal))
            {
                knowledge.Creatures.Add(entry);
            }
            else if (Enum.TryParse(entry.Substring(BiomePrefix.Length), out CompendiumBiome biome) && Enum.IsDefined(typeof(CompendiumBiome), biome))
            {
                knowledge.Biomes.Add(biome);
            }
        }
    }

    /// <summary>Writes the text to save.</summary>
    /// <returns>The text, sorted so it doesn't change without cause.</returns>
    public string ToSaved()
    {
        var sorted = new List<string>(entries);
        sorted.Sort(StringComparer.Ordinal);
        return string.Join(Separator.ToString(), sorted);
    }
}
