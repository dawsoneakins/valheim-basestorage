namespace BaseStorage.Core.Tests.ChestCategories;

using System.Globalization;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Plugin.ChestCategories;

/// <summary>
/// The recording storage port of contract S-E004-003 section 6.1 over the S-E005-002 <see cref="FakeChest"/>. Raw values live
/// in a dictionary keyed by chest id; every write is recorded in <see cref="Writes"/> as "id=value". Not a test row.
/// </summary>
internal sealed class FakeChestCategoryStorage : IChestCategoryStorage<FakeChest>
{
    private readonly Dictionary<ContainerId, string> raw = new Dictionary<ContainerId, string>();

    /// <summary>Gets the ordered record of writes as "UserId:Id=value".</summary>
    public List<string> Writes { get; } = new List<string>();

    /// <summary>Gets or sets a value that makes every write return false without storing (the view vanished mid-write).</summary>
    public bool FailWrites { get; set; }

    /// <summary>Gets the number of raw reads.</summary>
    public int Reads { get; private set; }

    /// <summary>Seeds the stored text of a chest.</summary>
    /// <param name="chest">The chest id.</param>
    /// <param name="value">The raw text.</param>
    public void Seed(ContainerId chest, string value) => raw[chest] = value;

    /// <summary>Gets the stored text of a chest, or null when it was never written or seeded.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>The raw text, or null.</returns>
    public string? Stored(ContainerId chest) => raw.TryGetValue(chest, out string? value) ? value : null;

    /// <inheritdoc />
    public bool TryGetId(FakeChest chest, out ContainerId id)
    {
        id = chest.Id;
        return chest.ViewValid;
    }

    /// <inheritdoc />
    public string? ReadRaw(FakeChest chest)
    {
        Reads++;
        if (!chest.ViewValid)
        {
            return null;
        }

        return raw.TryGetValue(chest.Id, out string? value) ? value : string.Empty;
    }

    /// <inheritdoc />
    public bool Write(FakeChest chest, string value)
    {
        if (!chest.ViewValid || FailWrites)
        {
            return false;
        }

        raw[chest.Id] = value;
        Writes.Add(chest.Id.UserId.ToString(CultureInfo.InvariantCulture) + ":" + chest.Id.Id.ToString(CultureInfo.InvariantCulture) + "=" + value);
        return true;
    }
}
