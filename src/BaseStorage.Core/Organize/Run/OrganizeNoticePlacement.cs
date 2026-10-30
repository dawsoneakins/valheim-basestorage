namespace BaseStorage.Core.Organize.Run;

/// <summary>Where the Plugin shows a notice of the run (contract section 2); the Plugin maps it to its MessageHud placement.</summary>
public enum OrganizeNoticePlacement
{
    /// <summary>Centre of the screen: refusals and errors.</summary>
    Centre = 0,

    /// <summary>Top left of the screen: confirmations and summaries.</summary>
    TopLeft = 1,
}
