namespace BaseStorage.Core.Persistence;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>The single declaration of every persistent key the mod writes into game data.</summary>
public static class PersistenceKeys
{
    /// <summary>Key of the chest category value, stored in the chest's networked object data.</summary>
    public const string ChestCategory = "basestorage.cat";

    /// <summary>Key of the organize lock value, stored in the networked object data of the lowest-id station.</summary>
    public const string OrganizeLock = "basestorage.lock";

    /// <summary>Key of the favourite slots value, stored in the player profile custom data.</summary>
    public const string Favourites = "basestorage.fav";

    private static readonly ReadOnlyCollection<string> AllKeys =
        new ReadOnlyCollection<string>(new[] { ChestCategory, OrganizeLock, Favourites });

    /// <summary>Gets every persistent key, in the order chest category, organize lock, favourites.</summary>
    public static IReadOnlyList<string> All => AllKeys;
}
