namespace BaseStorage.Plugin.Organize;

using BaseStorage.Core.Ui;

/// <summary>
/// Shows the Organize preview as the vanilla yes/no popup (contract section 5; design system: clone vanilla, do not paint). The
/// yes button confirms and the no button cancels the run through <see cref="OrganizeRunner"/>; both pop the popup first. The texts
/// arrive already resolved from the strings table. Game-bound, so it is not linked into the tests.
/// </summary>
internal static class OrganizePreviewPopup
{
    /// <summary>Pushes the preview popup.</summary>
    /// <param name="header">The popup header.</param>
    /// <param name="body">The preview body.</param>
    /// <returns>True when the popup was shown; false when the popup system is not available.</returns>
    internal static bool Show(string header, string body)
    {
        if (!UnifiedPopup.IsAvailable())
        {
            return false;
        }

        UnifiedPopup.Push(new YesNoPopup(SingleLine.Of(header), body, OnYes, OnNo, false, true));
        return true;
    }

    private static void OnYes()
    {
        UnifiedPopup.Pop();
        OrganizeRunner.Confirm();
    }

    private static void OnNo()
    {
        UnifiedPopup.Pop();
        OrganizeRunner.Cancel();
    }
}
