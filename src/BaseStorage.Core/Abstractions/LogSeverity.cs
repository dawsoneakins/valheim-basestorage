namespace BaseStorage.Core.Abstractions;

/// <summary>Severity of a log message.</summary>
public enum LogSeverity
{
    /// <summary>Diagnostic detail.</summary>
    Debug = 0,

    /// <summary>Normal information.</summary>
    Info = 1,

    /// <summary>Something unexpected but recoverable.</summary>
    Warning = 2,

    /// <summary>A failure.</summary>
    Error = 3,
}
