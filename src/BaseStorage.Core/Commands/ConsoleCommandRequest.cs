namespace BaseStorage.Core.Commands;

/// <summary>What a console command line asks for (contract section 2.1). The numeric values are contracted.</summary>
public enum ConsoleCommandRequest
{
    /// <summary>Anything that is not exactly "organize": print the usage line and do nothing else.</summary>
    Usage = 0,

    /// <summary>"basestorage organize": start an Organize for the base containing the player.</summary>
    Organize = 1,
}
