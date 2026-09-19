namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Log severity levels, ordered from most to least verbose.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// Core-side logging abstraction. Presentation injects a concrete
/// implementation (e.g. one that forwards to UnityEngine.Debug.Log).
/// Tests can inject a capturing logger for assertions.
/// </summary>
public interface IForgeLogger
{
    void Log(LogLevel level, string message);
}

/// <summary>
/// Extension helpers for <see cref="IForgeLogger"/> to avoid ceremony at call sites.
/// </summary>
public static class ForgeLoggerExtensions
{
    public static void Debug(this IForgeLogger logger, string message)
        => logger.Log(LogLevel.Debug, message);

    public static void Info(this IForgeLogger logger, string message)
        => logger.Log(LogLevel.Info, message);

    public static void Warning(this IForgeLogger logger, string message)
        => logger.Log(LogLevel.Warning, message);

    public static void Error(this IForgeLogger logger, string message)
        => logger.Log(LogLevel.Error, message);
}

/// <summary>
/// Default logger that silently discards all messages.
/// Used when no Presentation-side logger has been injected.
/// </summary>
public sealed class NullLogger : IForgeLogger
{
    public static readonly NullLogger Instance = new();

    public void Log(LogLevel level, string message) { }
}
