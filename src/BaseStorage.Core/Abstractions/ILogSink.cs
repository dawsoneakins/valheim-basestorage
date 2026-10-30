namespace BaseStorage.Core.Abstractions;

/// <summary>Destination for Core log messages.</summary>
public interface ILogSink
{
    /// <summary>Determines whether a message at this severity would be written; check before building expensive strings.</summary>
    /// <param name="severity">The severity to test. Values outside the defined range are treated as Error.</param>
    /// <returns>True when the message would be written.</returns>
    bool IsEnabled(LogSeverity severity);

    /// <summary>Writes one single-line message with no trailing newline. Never throws; disabled severities are dropped.</summary>
    /// <param name="severity">The message severity. Values outside the defined range are treated as Error.</param>
    /// <param name="message">The plain-text message; never null, may be empty.</param>
    void Log(LogSeverity severity, string message);
}
