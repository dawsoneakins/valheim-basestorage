namespace BaseStorage.Core.StationPanel;

/// <summary>Why both buttons are disabled while they are shown (contract 4.1 step 5). None means enabled, or hidden.</summary>
public enum StationPanelDisabledReason
{
    /// <summary>Enabled, or hidden.</summary>
    None = 0,

    /// <summary>An Organize or a Deposit is running in this base (AC-5.4). This wins over NoEligibleChests.</summary>
    OrganizeInProgress = 1,

    /// <summary>The base has no eligible chests (EligibleChestCount is 0).</summary>
    NoEligibleChests = 2,
}
