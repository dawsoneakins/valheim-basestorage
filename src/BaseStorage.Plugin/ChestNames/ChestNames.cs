namespace BaseStorage.Plugin.ChestNames;

using BaseStorage.Core.ChestNames;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>
/// Chest names, stored on the chest. The chest's owner writes the value, so any player can name a chest they can open. Every read is cleaned
/// again, because a name in the world data may have come from anywhere. Main thread only.
/// </summary>
internal static class ChestNames
{
    private const string SetNameRpc = "BaseStorage_SetName";

    /// <summary>Registers a chest's name message.</summary>
    /// <param name="chest">The chest.</param>
    internal static void Register(Container chest)
    {
        ZNetView view = chest.m_nview;
        view.Register<string>(SetNameRpc, (sender, value) =>
        {
            if (view.IsValid() && view.IsOwner())
            {
                view.GetZDO().Set(PersistenceKeys.ChestName, ChestNameRules.Clean(value));
            }
        });
    }

    /// <summary>Reads a chest's name.</summary>
    /// <param name="chest">The chest, which may be a destroyed Unity object.</param>
    /// <returns>The clean name; empty for none.</returns>
    internal static string Read(Container chest)
    {
        ZNetView? view = chest == null ? null : chest.m_nview;
        return view != null && view.IsValid() ? ChestNameRules.Clean(view.GetZDO().GetString(PersistenceKeys.ChestName, string.Empty)) : string.Empty;
    }

    /// <summary>Opens the game's text entry for a chest's name. A gamepad gets the game's own on-screen keyboard from it.</summary>
    /// <param name="chest">The chest.</param>
    internal static void Ask(Container chest)
    {
        TextInput? input = TextInput.instance;
        if (chest != null && input != null)
        {
            input.RequestText(new Receiver(chest), StringTable.English.Get(StringKeys.ChestNameTopic), ChestNameRules.MaxLength);
        }
    }

    private sealed class Receiver : TextReceiver
    {
        private readonly Container chest;

        internal Receiver(Container target)
        {
            chest = target;
        }

        public string GetText() => Read(chest);

        public void SetText(string text)
        {
            ZNetView? view = chest == null ? null : chest.m_nview;
            if (view != null && view.IsValid())
            {
                view.InvokeRPC(SetNameRpc, ChestNameRules.Clean(text));
            }
        }
    }
}
