namespace BaseStorage.Core.Ui;

/// <summary>Plural-form selection for English.</summary>
public static class Plural
{
    /// <summary>The form used when the count is exactly one.</summary>
    public const string One = "one";

    /// <summary>The form used for every other count, including zero and negatives.</summary>
    public const string Other = "other";

    /// <summary>Selects the plural form for a count.</summary>
    /// <param name="count">The count to select a form for.</param>
    /// <returns><see cref="One"/> when the count is 1, otherwise <see cref="Other"/>.</returns>
    public static string Form(int count) => count == 1 ? One : Other;
}
