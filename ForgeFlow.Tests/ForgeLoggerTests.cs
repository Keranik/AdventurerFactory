using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// A logger that captures messages for test assertions.
/// </summary>
public sealed class CapturingLogger : IForgeLogger
{
    public List<(LogLevel Level, string Message)> Messages { get; } = new();

    public void Log(LogLevel level, string message)
    {
        Messages.Add((level, message));
    }
}

public class ForgeLoggerTests
{
    [Fact]
    public void NullLogger_DoesNotThrow()
    {
        var logger = NullLogger.Instance;
        logger.Log(LogLevel.Debug, "test");
        logger.Log(LogLevel.Error, "error");
    }

    [Fact]
    public void CapturingLogger_CapturesMessages()
    {
        var logger = new CapturingLogger();
        logger.Debug("debug msg");
        logger.Info("info msg");
        logger.Warning("warning msg");
        logger.Error("error msg");

        Assert.Equal(4, logger.Messages.Count);
        Assert.Equal(LogLevel.Debug, logger.Messages[0].Level);
        Assert.Equal("debug msg", logger.Messages[0].Message);
        Assert.Equal(LogLevel.Error, logger.Messages[3].Level);
    }

    [Fact]
    public void ExtensionMethods_MapToCorrectLevels()
    {
        var logger = new CapturingLogger();

        logger.Debug("d");
        logger.Info("i");
        logger.Warning("w");
        logger.Error("e");

        Assert.Equal(LogLevel.Debug, logger.Messages[0].Level);
        Assert.Equal(LogLevel.Info, logger.Messages[1].Level);
        Assert.Equal(LogLevel.Warning, logger.Messages[2].Level);
        Assert.Equal(LogLevel.Error, logger.Messages[3].Level);
    }
}
