namespace BaseStorage.Core.Logging;

/// <summary>The closed set of message kinds written at Info or Debug. Warning and Error have no category.</summary>
public enum LogCategory
{
    /// <summary>The single "BaseStorage version loaded" line at boot. Written at Info.</summary>
    Load = 0,

    /// <summary>Messages about syncing host-synced settings. Written at Info.</summary>
    ConfigSync = 1,

    /// <summary>The one summary line per Organize operation. Written at Info.</summary>
    OrganizeSummary = 2,

    /// <summary>The one summary line per Deposit operation. Written at Info.</summary>
    DepositSummary = 3,

    /// <summary>One line per planned or executed item move. Written at Debug.</summary>
    MoveDetail = 4,

    /// <summary>Per-patch boot detail, such as a patch applied line. Written at Debug.</summary>
    PatchDetail = 5,

    /// <summary>The stack trace the patch safety wrapper writes after its Error line. Written at Debug.</summary>
    SafetyStackTrace = 6,

    /// <summary>UI degradation notes, such as a missing vanilla template. Written at Debug.</summary>
    UiFallback = 7,

    /// <summary>Any other diagnostic detail. Written at Debug.</summary>
    Detail = 8,
}
