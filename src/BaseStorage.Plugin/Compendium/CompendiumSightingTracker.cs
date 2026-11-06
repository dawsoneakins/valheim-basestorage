namespace BaseStorage.Plugin.Compendium;

using System.Collections.Generic;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Persistence;
using UnityEngine;

/// <summary>Notes the creatures near the player and the biome they're in, once a second, and saves them on the character. Main thread only.</summary>
internal static class CompendiumSightingTracker
{
    private const float IntervalSeconds = 1f;
    private const float SightRange = 30f;

    private static float nextCheck;
    private static Player? owner;
    private static CompendiumSightings? sightings;

    /// <summary>Checks the surroundings when it's time.</summary>
    internal static void Tick()
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.m_customData == null || Time.unscaledTime < nextCheck)
        {
            return;
        }

        nextCheck = Time.unscaledTime + IntervalSeconds;
        CompendiumSightings current = For(player);
        bool changed = false;
        if (GameCompendiumSource.TryMapBiome(player.GetCurrentBiome(), out CompendiumBiome biome))
        {
            changed = current.AddBiome(biome);
        }

        Vector3 position = player.transform.position;
        List<Character> characters = Character.GetAllCharacters();
        for (int i = 0; i < characters.Count; i++)
        {
            Character character = characters[i];
            if (character != null && !(character is Player) && Vector3.Distance(character.transform.position, position) <= SightRange)
            {
                changed |= current.AddCreature(Utils.GetPrefabName(character.gameObject));
            }
        }

        if (changed)
        {
            player.m_customData[PersistenceKeys.CompendiumSightings] = current.ToSaved();
        }
    }

    /// <summary>Gets a player's sightings, loading them when the character changed.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The sightings.</returns>
    internal static CompendiumSightings For(Player player)
    {
        if (sightings == null || owner != player)
        {
            string? saved = null;
            player.m_customData?.TryGetValue(PersistenceKeys.CompendiumSightings, out saved);
            sightings = new CompendiumSightings(saved);
            owner = player;
        }

        return sightings;
    }
}
