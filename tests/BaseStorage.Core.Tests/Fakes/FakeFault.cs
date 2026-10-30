namespace BaseStorage.Core.Tests.Fakes;

/// <summary>Failure modes that <see cref="FakeContainerLayer"/> can be told to inject, as the real game can produce them.</summary>
public enum FakeFault
{
    /// <summary>A pending reservation resolves to Denied on every later resolution. An existing Granted is not revoked.</summary>
    ReservationRefused = 0,

    /// <summary>Every RemoveFromStack returns ContainerChanged once NotFound and NotReserved have passed. AddItems is unaffected.</summary>
    ChestChanged = 1,

    /// <summary>Every AddItems returns DestinationFull once NotFound and NotReserved have passed. RemoveFromStack is unaffected.</summary>
    DestinationFull = 2,

    /// <summary>The owner left: Pending or Granted becomes Lost immediately, and any new request resolves to Lost while armed.</summary>
    OwnerDisconnect = 3,
}
