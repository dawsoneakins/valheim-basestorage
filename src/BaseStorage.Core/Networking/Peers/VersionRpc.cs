namespace BaseStorage.Core.Networking.Peers;

/// <summary>
/// The wire names of the version handshake (contract version-rpc-contracts.md 2.1). The names are data: Core never sends or
/// receives anything, the Plugin registers and invokes them.
/// </summary>
public static class VersionRpc
{
    /// <summary>The name of the peer RPC both sides register on a new connection and invoke once with their payload.</summary>
    public const string Name = "BaseStorage_Version";

    /// <summary>
    /// The name of the vanilla connection-error RPC the host invokes on a refused client, with the game's version-error code as its
    /// one argument. Its handler is the game method named RPC_ followed by this name.
    /// </summary>
    public const string VanillaErrorName = "Error";
}
