namespace BaseStorage.Plugin.Boot;

using System;
using BaseStorage.Core.Abstractions;
using BepInEx.Logging;

/// <summary>Adapts the BepInEx ManualLogSource to ILogSink. Never throws.</summary>
internal sealed class BepInExLogSink : ILogSink
{
    private readonly ManualLogSource source;

    internal BepInExLogSink(ManualLogSource source)
    {
        this.source = source;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogSeverity severity) => true;

    /// <inheritdoc />
    public void Log(LogSeverity severity, string message)
    {
        try
        {
            switch (severity)
            {
                case LogSeverity.Debug:
                    source.LogDebug(message);
                    break;
                case LogSeverity.Info:
                    source.LogInfo(message);
                    break;
                case LogSeverity.Warning:
                    source.LogWarning(message);
                    break;
                case LogSeverity.Error:
                default:
                    source.LogError(message);
                    break;
            }
        }
        catch (Exception)
        {
            // Logging must never fail the caller.
        }
    }
}
