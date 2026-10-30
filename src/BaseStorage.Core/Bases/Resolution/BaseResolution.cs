namespace BaseStorage.Core.Bases.Resolution;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>
/// The answer to "which base is this start in?" (ADR-GWIN-024). Results are created only by <c>BaseResolver</c> and <c>BaseCache</c>.
/// There is no value equality; compare with the singleton <see cref="NotInBase"/> by reference.
/// </summary>
public sealed class BaseResolution
{
    /// <summary>Creates a result. Internal, so only the resolver and the cache create results.</summary>
    /// <param name="isInBase">Whether the start is in a base.</param>
    /// <param name="stations">The stations of the base, in fill order; never null.</param>
    /// <param name="containers">The member containers, in ascending container id order; never null.</param>
    /// <param name="isCapped">Whether the station cap cut the base short.</param>
    /// <param name="messageKey">The message key for a no-base result, or null for an in-base result.</param>
    internal BaseResolution(
        bool isInBase,
        IReadOnlyList<StationSnapshot> stations,
        IReadOnlyList<ContainerSnapshot> containers,
        bool isCapped,
        string? messageKey)
    {
        IsInBase = isInBase;
        Stations = stations ?? throw new ArgumentNullException(nameof(stations));
        Containers = containers ?? throw new ArgumentNullException(nameof(containers));
        IsCapped = isCapped;
        MessageKey = messageKey;
    }

    /// <summary>Gets the singleton result for a start that is in no base: no stations, no containers, and the "Not in a base" message key.</summary>
    public static BaseResolution NotInBase { get; } = new BaseResolution(
        false,
        Array.AsReadOnly(Array.Empty<StationSnapshot>()),
        Array.AsReadOnly(Array.Empty<ContainerSnapshot>()),
        false,
        StringKeys.MessageBaseNotInBase);

    /// <summary>Gets a value indicating whether the start is in a base.</summary>
    public bool IsInBase { get; }

    /// <summary>Gets the stations of the base in fill order; never null, and read-only. Empty when the start is in no base.</summary>
    public IReadOnlyList<StationSnapshot> Stations { get; }

    /// <summary>Gets the member containers in ascending <c>ContainerId</c> order; never null, and read-only. Empty when the start is in no base.</summary>
    public IReadOnlyList<ContainerSnapshot> Containers { get; }

    /// <summary>Gets a value indicating whether more stations were connected than the station cap allowed.</summary>
    public bool IsCapped { get; }

    /// <summary>Gets the message key for a no-base result (<c>message.base.not-in-base</c>), or null for an in-base result.</summary>
    public string? MessageKey { get; }
}
