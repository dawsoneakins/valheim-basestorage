namespace BaseStorage.Plugin.ChestCategories;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.Ui;

/// <summary>
/// The Plugin write API for chest categories (contract S-E004-003 section 4.2), called by the Category button (S-E004-004) and
/// by the Organize auto-assignment (S-E004-011). It owns the one write driver, forwards the frame tick behind the fail-closed
/// wrapper, and shows the one notice per finished write: the new label on success, the reason on a refusal. Every text comes
/// from the strings table. Game-bound, so it is not linked into the tests; its facts are proven by source scans.
/// </summary>
internal static class ChestCategoryWriter
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.ChestCategories.ChestCategoryWriter.Tick";

    private static readonly HashSet<ContainerId> Quiet = new HashSet<ContainerId>();

    /// <summary>Gets the driver, or null until <see cref="Initialize"/> has run.</summary>
    internal static ChestCategoryWriteDriver<Container>? Current { get; private set; }

    /// <summary>Gets or sets where a refused quiet write is logged.</summary>
    internal static ILogSink? QuietLog { get; set; }

    /// <summary>Creates the one driver at boot. Later calls return the existing instance.</summary>
    /// <param name="reservations">The reservation driver of the plugin.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings; the reservation timeout is read once here.</param>
    /// <returns>The driver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static ChestCategoryWriteDriver<Container> Initialize(ReservationDriver<Container> reservations, IClock clock, BaseStorageSettings settings)
    {
        if (reservations == null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (Current == null)
        {
            Current = new ChestCategoryWriteDriver<Container>(reservations, new GameChestCategoryStorage(), clock, settings.ReserveTimeoutMs, OnFinished);
        }

        return Current;
    }

    /// <summary>Reads the stored category of a chest: null for Auto, no key, an invalid view or an undecodable value.</summary>
    /// <param name="chest">The chest, which may be a destroyed Unity object.</param>
    /// <returns>The stored assignment, or null.</returns>
    internal static CategoryAssignment? Read(Container chest)
    {
        ChestCategoryWriteDriver<Container>? driver = Current;
        return driver == null || chest == null ? null : driver.Read(chest);
    }

    /// <summary>Asks for a chest category write through the reservation protocol and tells the player how it ended.</summary>
    /// <param name="chest">The chest, which may be a destroyed Unity object.</param>
    /// <param name="desired">The value to write; null for Auto.</param>
    /// <returns>Applied, Pending, Unchanged (which shows the current label once) or Refused. Refused without a notice only when the plugin did not boot or the chest is gone.</returns>
    internal static CategoryWriteStatus TrySet(Container chest, CategoryAssignment? desired)
    {
        ChestCategoryWriteDriver<Container>? driver = Current;
        if (driver == null || chest == null)
        {
            return CategoryWriteStatus.Refused;
        }

        CategoryWriteStatus status = driver.TrySet(chest, desired);
        if (status == CategoryWriteStatus.Unchanged)
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageCategorySet, ChestCategoryReader.Label(desired, StringTable.English));
        }

        return status;
    }

    /// <summary>
    /// Writes a rule Organize chose (S-A-003) with no per-chest notice, so a large run shows one summary line. A chest the player
    /// has already given a value keeps it. A refused write is logged by <see cref="OnFinished"/> and the run carries on.
    /// </summary>
    /// <param name="chest">The chest.</param>
    /// <param name="id">The chest id.</param>
    /// <param name="desired">The category.</param>
    internal static void TrySetQuiet(Container chest, ContainerId id, CategoryAssignment desired)
    {
        ChestCategoryWriteDriver<Container>? driver = Current;
        if (driver == null || chest == null || driver.Read(chest) != null)
        {
            return;
        }

        Quiet.Add(id);
        if (driver.TrySet(chest, desired) == CategoryWriteStatus.Unchanged)
        {
            Quiet.Remove(id);
        }
    }

    /// <summary>Advances the pending writes by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => Current?.Tick());
    }

    private static void OnFinished(CategoryWriteResult result)
    {
        if (Quiet.Remove(result.Chest))
        {
            if (!result.Succeeded)
            {
                QuietLog?.Log(LogSeverity.Warning, "Organize could not save a chest category: " + result.Outcome);
            }

            return;
        }

        StringTable strings = StringTable.English;
        if (result.Succeeded)
        {
            Notices.Show(NoticePlacement.TopLeft, CategoryWriteRules.NoticeKey(result.Outcome), ChestCategoryReader.Label(result.Desired, strings));
            return;
        }

        string reasonKey = CategoryWriteRules.ReasonKey(result.Outcome) ?? StringKeys.CategoryRefusalUnavailable;
        string holder = result.HolderLabel ?? strings.Get(StringKeys.PlayerUnknown);
        Notices.Show(NoticePlacement.Center, CategoryWriteRules.NoticeKey(result.Outcome), strings.Format(reasonKey, holder));
    }
}
