namespace BaseStorage.Core.Tests.Crafting;

using System;

/// <summary>
/// The test-only reproduction of the vanilla quality-scaled amount rule (contract upgrade-multicraft 2 U1, 3.1), used to compute the
/// fixture amounts of S-E003-007. It is not a production type and is never linked into any src project. Every intermediate is a
/// float, exactly as the game's IL does it; only Math.Floor widens to double (ADR-GWIN-133 decision 4).
/// </summary>
internal static class VanillaAmountOracle
{
    /// <summary>The vanilla per-craft amount of one recipe line at a quality level.</summary>
    /// <param name="amount">The base amount (m_amount).</param>
    /// <param name="amountPerLevel">The amount added per level (m_amountPerLevel).</param>
    /// <param name="qualityLevel">The quality level; level 1 or lower is the base amount.</param>
    /// <param name="upgraderResource">True when the line is an upgrader resource (m_upgraderResource).</param>
    /// <returns>The amount vanilla asks for.</returns>
    internal static int For(int amount, int amountPerLevel, int qualityLevel, bool upgraderResource)
    {
        if (qualityLevel <= 1)
        {
            return amount;
        }

        float f = qualityLevel < 4 ? (float)(qualityLevel - 1) : 4f + (float)(qualityLevel - 4) / 2f;
        return (int)Math.Floor((double)(f * (float)amountPerLevel + (upgraderResource ? (float)amount : 0f)));
    }
}
