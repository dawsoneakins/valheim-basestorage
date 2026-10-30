namespace BaseStorage.Core.HostSync.Session;

/// <summary>
/// One config-sync decision for one peer (contract config-sync-contracts.md 3.5). Outcomes come only from ConfigSyncSession.
/// </summary>
public sealed class ConfigSyncOutcome
{
    private static readonly ConfigSyncOutcome NoneInstance = new ConfigSyncOutcome(0, ConfigSyncAction.None, ConfigSyncRejectReason.None, null, 0);

    internal ConfigSyncOutcome(long peerId, ConfigSyncAction action, ConfigSyncRejectReason reason, HostSyncPayload? payload, int categoryCount)
    {
        PeerId = peerId;
        Action = action;
        Reason = reason;
        Payload = payload;
        SettingCount = payload?.Settings.Count ?? 0;
        CategoryCount = categoryCount;
    }

    /// <summary>Gets the shared None outcome (PeerId 0).</summary>
    public static ConfigSyncOutcome None => NoneInstance;

    /// <summary>Gets the peer the outcome is about; 0 for None.</summary>
    public long PeerId { get; }

    /// <summary>Gets the action the Plugin takes.</summary>
    public ConfigSyncAction Action { get; }

    /// <summary>Gets the reason; None unless Action is Rejected.</summary>
    public ConfigSyncRejectReason Reason { get; }

    /// <summary>Gets the applied payload for Applied; null otherwise.</summary>
    public HostSyncPayload? Payload { get; }

    /// <summary>Gets the number of synced settings for Applied; 0 otherwise.</summary>
    public int SettingCount { get; }

    /// <summary>Gets the number of categories in the session catalog for Applied; 0 otherwise.</summary>
    public int CategoryCount { get; }
}
