namespace BaseStorage.Plugin.Feeding;

using UnityEngine;

/// <summary>Wraps a game station in its feed target.</summary>
internal static class FeedTargets
{
    /// <summary>Wraps a station that has a valid network view.</summary>
    /// <param name="station">A smelter, fire, fermenter or cooking station.</param>
    /// <returns>The target, or null for a placement ghost or a station that takes nothing.</returns>
    internal static FeedTarget? Of(Component? station)
    {
        switch (station)
        {
            case Smelter smelter when Valid(smelter.m_nview):
                return new SmelterFeedTarget(smelter);
            case Fireplace fire when Valid(fire.m_nview) && FireplaceFeedTarget.Accepts(fire):
                return new FireplaceFeedTarget(fire);
            case Fermenter fermenter when Valid(fermenter.m_nview):
                return new FermenterFeedTarget(fermenter);
            case CookingStation cooker when Valid(cooker.m_nview):
                return new CookingFeedTarget(cooker);
            default:
                return null;
        }
    }

    private static bool Valid(ZNetView view) => view != null && view.IsValid();
}
