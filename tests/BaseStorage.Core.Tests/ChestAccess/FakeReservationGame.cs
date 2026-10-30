namespace BaseStorage.Core.Tests.ChestAccess;

using System.Globalization;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// A test chest for the reservation driver (contract S-E005-002 section 8.1). Defaults: view valid, access allowed, owned by
/// Erik, Erik listed, connected and named "Erik". Not a test row (ADR-GWIN-058).
/// </summary>
internal sealed class FakeChest
{
    /// <summary>Gets or sets the chest id.</summary>
    public ContainerId Id { get; set; }

    /// <summary>Gets or sets a value that is true when the view is valid.</summary>
    public bool ViewValid { get; set; } = true;

    /// <summary>Gets or sets a value that is true when vanilla access allows the local player.</summary>
    public bool AccessAllowed { get; set; } = true;

    /// <summary>Gets or sets a value that is true when the local vanilla UI has this chest open.</summary>
    public bool OpenLocally { get; set; }

    /// <summary>Gets or sets the in-use flag of the chest.</summary>
    public bool InUseFlag { get; set; }

    /// <summary>Gets or sets the owner session id; 0 means no owner.</summary>
    public long Owner { get; set; } = ChestAccessFixtures.Erik;

    /// <summary>Gets or sets a value that is false when the owner has left the world (not listed and not connected).</summary>
    public bool OwnerPresent { get; set; } = true;

    /// <summary>Gets or sets the listed name of the owner.</summary>
    public string? OwnerName { get; set; } = "Erik";

    /// <summary>Gets or sets an observation that Observe returns verbatim (the grid sweeps, ADR-GWIN-058).</summary>
    public ChestObservation? Forced { get; set; }
}

/// <summary>
/// The recording game port of contract S-E005-002 section 8.1. Commands are recorded in <see cref="Calls"/> as strings, member
/// accesses (reads included) are counted in <see cref="Accesses"/>, and <see cref="FaultMember"/> with <see cref="Fault"/>
/// makes one member throw. Not a test row (ADR-GWIN-058).
/// </summary>
internal sealed class FakeReservationGame : IReservationGame<FakeChest>
{
    private readonly List<FakeChest> chests = new List<FakeChest>();
    private bool inWorld = true;

    /// <summary>Gets or sets the local session id.</summary>
    public long LocalSession { get; set; } = ChestAccessFixtures.Me;

    /// <summary>Gets or sets a value that is true when the local peer is the host or a single-player world.</summary>
    public bool IsServer { get; set; }

    /// <summary>Gets the ordered command record: SendOpenRequest, ClaimOwnership and SetInUse entries.</summary>
    public List<string> Calls { get; } = new List<string>();

    /// <summary>Gets the number of member accesses on the port, reads included.</summary>
    public int Accesses { get; private set; }

    /// <summary>Gets or sets the name of the member that throws <see cref="Fault"/> when it is accessed.</summary>
    public string? FaultMember { get; set; }

    /// <summary>Gets or sets the exception that <see cref="FaultMember"/> throws.</summary>
    public Exception? Fault { get; set; }

    /// <summary>Gets or sets a value that is true when a world exists.</summary>
    public bool InWorld
    {
        get
        {
            Touch(nameof(InWorld));
            return inWorld;
        }

        set => inWorld = value;
    }

    /// <summary>Adds a chest with the defaults of contract 8.1.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The chest.</returns>
    public FakeChest AddChest(ContainerId id)
    {
        var chest = new FakeChest { Id = id };
        chests.Add(chest);
        return chest;
    }

    /// <inheritdoc />
    public bool TryGetId(FakeChest chest, out ContainerId id)
    {
        Touch(nameof(TryGetId));
        id = chest.Id;
        return chest.ViewValid;
    }

    /// <inheritdoc />
    public ChestObservation Observe(FakeChest chest)
    {
        Touch(nameof(Observe));
        if (chest.Forced is not null)
        {
            return chest.Forced;
        }

        bool ownerIsSelf = chest.Owner == LocalSession;
        bool hasOwner = chest.Owner != 0L;
        bool listed = hasOwner && (ownerIsSelf || chest.OwnerPresent);
        bool connected = hasOwner && (ownerIsSelf || chest.OwnerPresent);
        return new ChestObservation(
            chest.ViewValid,
            chest.AccessAllowed,
            chest.OpenLocally,
            chest.InUseFlag,
            LocalSession,
            chest.Owner,
            listed,
            connected,
            listed ? chest.OwnerName : null,
            IsServer);
    }

    /// <inheritdoc />
    public bool IsViewValid(FakeChest chest)
    {
        Touch(nameof(IsViewValid));
        return chest.ViewValid;
    }

    /// <inheritdoc />
    public bool IsOwner(FakeChest chest)
    {
        Touch(nameof(IsOwner));
        return chest.Owner == LocalSession;
    }

    /// <inheritdoc />
    public bool IsOpenLocally(FakeChest chest)
    {
        Touch(nameof(IsOpenLocally));
        return chest.OpenLocally;
    }

    /// <inheritdoc />
    public void SendOpenRequest(FakeChest chest)
    {
        Touch(nameof(SendOpenRequest));
        Calls.Add("SendOpenRequest " + Format(chest.Id));
    }

    /// <inheritdoc />
    public void ClaimOwnership(FakeChest chest)
    {
        Touch(nameof(ClaimOwnership));
        Calls.Add("ClaimOwnership " + Format(chest.Id));
        chest.Owner = LocalSession;
    }

    /// <inheritdoc />
    public void SetInUse(FakeChest chest, bool inUse)
    {
        Touch(nameof(SetInUse));
        Calls.Add("SetInUse " + Format(chest.Id) + " " + (inUse ? "true" : "false"));
        if (chest.Owner == LocalSession)
        {
            chest.InUseFlag = inUse;
        }
    }

    private static string Format(ContainerId id) =>
        id.UserId.ToString(CultureInfo.InvariantCulture) + ":" + id.Id.ToString(CultureInfo.InvariantCulture);

    private void Touch(string member)
    {
        Accesses++;
        if (Fault != null && FaultMember == member)
        {
            throw Fault;
        }
    }
}
