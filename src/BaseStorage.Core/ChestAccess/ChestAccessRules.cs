namespace BaseStorage.Core.ChestAccess;

using System;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// The pure decisions of the chest reservation adapter (contract S-E005-002 section 2.4): which route to take, what a
/// poll observation means, what an open response means, when the in-use marker is cleared, and the holder label.
/// Every member is pure: no clock, no I/O, no logging and no static mutable state.
/// </summary>
public static class ChestAccessRules
{
    /// <summary>Chooses the route for one observation. The first matching row of the contracted table wins.</summary>
    /// <param name="observation">The observed facts.</param>
    /// <returns>The route.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="observation"/> is null.</exception>
    public static ReservationRoute Route(ChestObservation observation)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        if (!observation.ViewValid || observation.LocalSessionId == 0L)
        {
            return ReservationRoute.SkipInvalid;
        }

        if (!observation.AccessAllowed)
        {
            return ReservationRoute.SkipAccessDenied;
        }

        if (observation.OpenLocally)
        {
            return ReservationRoute.SelfOpen;
        }

        if (IsInUseByOther(observation))
        {
            return ReservationRoute.SkipInUse;
        }

        if (observation.OwnerSessionId == observation.LocalSessionId)
        {
            return ReservationRoute.LocalOwner;
        }

        if (observation.OwnerSessionId == 0L || !observation.OwnerConnected)
        {
            return observation.IsServer ? ReservationRoute.HostClaim : ReservationRoute.SkipNoOwner;
        }

        return ReservationRoute.SendRequest;
    }

    /// <summary>Tells whether another listed player has the chest open.</summary>
    /// <param name="observation">The observed facts.</param>
    /// <returns>True when the chest is in use by a listed player other than the local session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="observation"/> is null.</exception>
    public static bool IsInUseByOther(ChestObservation observation)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        return observation.ViewValid
            && observation.InUseFlag
            && observation.OwnerSessionId != 0L
            && observation.OwnerSessionId != observation.LocalSessionId
            && observation.OwnerListed;
    }

    /// <summary>Builds the in-use result for a chest that is in use by another listed player.</summary>
    /// <param name="chest">The chest. It must not be the none id.</param>
    /// <param name="observation">The observed facts.</param>
    /// <param name="strings">The strings table that supplies the fallback label.</param>
    /// <returns>The in-use result, or null when the chest is not in use by another listed player.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chest"/> is the none id.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="observation"/> or <paramref name="strings"/> is null.</exception>
    public static ChestInUse? InUseBy(ContainerId chest, ChestObservation observation, StringTable strings)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        return IsInUseByOther(observation) ? new ChestInUse(chest, HolderLabel(observation.OwnerName, strings)) : null;
    }

    /// <summary>Returns the label for a player's name, or the fallback label when the name is unusable.</summary>
    /// <param name="rawName">The raw name, which may be null.</param>
    /// <param name="strings">The strings table that supplies the fallback label.</param>
    /// <returns>The raw name unchanged when it is usable; otherwise the English fallback from the table.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strings"/> is null.</exception>
    public static string HolderLabel(string? rawName, StringTable strings)
    {
        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        return IsUsableName(rawName) ? rawName! : strings.Get(StringKeys.PlayerUnknown);
    }

    /// <summary>Gets the reservation event that a route produces when it is taken.</summary>
    /// <param name="route">The route.</param>
    /// <returns>The event, or null for <see cref="ReservationRoute.SendRequest"/>. An undefined value gives OwnerLost.</returns>
    public static ReservationEvent? EventForRoute(ReservationRoute route)
    {
        switch (route)
        {
            case ReservationRoute.SendRequest:
                return null;
            case ReservationRoute.LocalOwner:
            case ReservationRoute.SelfOpen:
            case ReservationRoute.HostClaim:
                return ReservationEvent.Grant;
            case ReservationRoute.SkipAccessDenied:
                return ReservationEvent.Deny;
            case ReservationRoute.SkipInUse:
                return ReservationEvent.InUseByOther;
            default:
                return ReservationEvent.OwnerLost;
        }
    }

    /// <summary>Tells whether a route sends the open request.</summary>
    /// <param name="route">The route.</param>
    /// <returns>True only for <see cref="ReservationRoute.SendRequest"/>.</returns>
    public static bool SendsRequest(ReservationRoute route) => route == ReservationRoute.SendRequest;

    /// <summary>Tells whether a route claims the chest ownership locally.</summary>
    /// <param name="route">The route.</param>
    /// <returns>True only for <see cref="ReservationRoute.HostClaim"/>.</returns>
    public static bool ClaimsOwnership(ReservationRoute route) => route == ReservationRoute.HostClaim;

    /// <summary>Tells whether a route sets the in-use marker.</summary>
    /// <param name="route">The route.</param>
    /// <returns>True for SendRequest, LocalOwner and HostClaim; false otherwise, including SelfOpen.</returns>
    public static bool SetsInUse(ReservationRoute route) =>
        route == ReservationRoute.SendRequest || route == ReservationRoute.LocalOwner || route == ReservationRoute.HostClaim;

    /// <summary>Gets the event that an open response produces.</summary>
    /// <param name="granted">True when the owner granted the request.</param>
    /// <returns>Deny for a refusal; null for a grant, because ownership must still arrive.</returns>
    public static ReservationEvent? EventForResponse(bool granted) => granted ? null : ReservationEvent.Deny;

    /// <summary>Gets the event that one poll observation produces for a non-terminal reservation.</summary>
    /// <param name="state">The current state of the reservation.</param>
    /// <param name="viewValid">True when the chest view is valid.</param>
    /// <param name="isOwner">True when the local peer owns the chest.</param>
    /// <param name="grantReceived">True when the owner granted the request.</param>
    /// <param name="openedLocally">True when the local vanilla UI has this chest open now. Pass false for a SelfOpen reservation.</param>
    /// <returns>The event, or null when nothing applies yet. Undefined states give null.</returns>
    public static ReservationEvent? Poll(ReservationState state, bool viewValid, bool isOwner, bool grantReceived, bool openedLocally)
    {
        bool requesting = state == ReservationState.Requesting;
        bool holding = ReservationTransitions.HoldsChest(state);
        if (!requesting && !holding)
        {
            return null;
        }

        if (!viewValid)
        {
            return ReservationEvent.OwnerLost;
        }

        if (openedLocally)
        {
            return requesting ? ReservationEvent.Cancel : ReservationEvent.InUseByOther;
        }

        if (requesting)
        {
            return grantReceived && isOwner ? ReservationEvent.Grant : null;
        }

        return isOwner ? null : ReservationEvent.OwnerLost;
    }

    /// <summary>Tells whether the in-use marker must be cleared when a reservation releases the chest.</summary>
    /// <param name="setByAdapter">True when the adapter set the marker.</param>
    /// <param name="viewValid">True when the chest view is valid.</param>
    /// <param name="isOwner">True when the local peer owns the chest.</param>
    /// <param name="openLocally">True when the local vanilla UI has the chest open.</param>
    /// <returns>True only when the adapter set the marker, the view is valid, the local peer owns the chest and the UI does not have it open.</returns>
    public static bool ShouldClearInUse(bool setByAdapter, bool viewValid, bool isOwner, bool openLocally) =>
        setByAdapter && viewValid && isOwner && !openLocally;

    /// <summary>Gets the late-response window for a reservation timeout.</summary>
    /// <param name="reserveTimeoutMs">The reservation timeout in milliseconds. Values below 1 count as 1.</param>
    /// <returns>Twice the timeout, as a time span. It never throws.</returns>
    public static TimeSpan LateResponseWindow(int reserveTimeoutMs) =>
        TimeSpan.FromTicks(2L * Math.Max(1, reserveTimeoutMs) * TimeSpan.TicksPerMillisecond);

    private static bool IsUsableName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return false;
        }

        for (int i = 0; i < rawName!.Length; i++)
        {
            if (char.IsControl(rawName[i]))
            {
                return false;
            }
        }

        return true;
    }
}
