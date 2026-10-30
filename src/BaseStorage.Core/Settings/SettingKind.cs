namespace BaseStorage.Core.Settings;

/// <summary>The value kind of one catalog setting. The numeric values are contracted.</summary>
public enum SettingKind
{
    /// <summary>A true or false value. The default text is "true" or "false".</summary>
    Bool = 0,

    /// <summary>A whole number. The default text is invariant-culture decimal text.</summary>
    Int = 1,

    /// <summary>A comma-separated list in the format of <see cref="ItemListText"/>.</summary>
    ItemList = 2,

    /// <summary>A key binding. The default text is empty, which means unbound.</summary>
    Hotkey = 3,
}
