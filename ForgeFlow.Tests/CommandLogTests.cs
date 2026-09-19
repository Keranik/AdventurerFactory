using ForgeFlow.Core.Commands;

namespace ForgeFlow.Tests;

public class CommandLogTests
{
    // ── Basic Recording ─────────────────────────────────────────────────

    [Fact]
    public void Record_WhenDisabled_DoesNotRecord()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = false;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        bus.Dispatch(new TestCommand(42));

        Assert.Equal(0, log.Count);
    }

    [Fact]
    public void Record_WhenEnabled_RecordsEntry()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        bus.Dispatch(new TestCommand(42));

        Assert.Equal(1, log.Count);
        var entry = log.GetEntry(0);
        Assert.Equal("TestCommand", entry.CommandTypeName);
        Assert.True(entry.Result.Success);
    }

    [Fact]
    public void Record_CapturesDescription_FromToString()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        bus.Dispatch(new TestCommand(99));

        var entry = log.GetEntry(0);
        Assert.Contains("99", entry.Description);
    }

    [Fact]
    public void Record_CapturesFailResult()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Fail("Not enough gold"));

        bus.Dispatch(new TestCommand(1));

        var entry = log.GetEntry(0);
        Assert.False(entry.Result.Success);
        Assert.Equal("Not enough gold", entry.Result.Reason);
    }

    [Fact]
    public void Record_CapturesTick()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        log.SetTick(42);
        bus.Dispatch(new TestCommand(1));
        log.SetTick(100);
        bus.Dispatch(new TestCommand(2));

        Assert.Equal(42UL, log.GetEntry(0).Tick);
        Assert.Equal(100UL, log.GetEntry(1).Tick);
    }

    [Fact]
    public void Record_HandlerThrows_WithCallback_RecordsFailEntry()
    {
        var bus = new CommandBus();
        bus.OnHandlerException = _ => { };
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => throw new InvalidOperationException("boom"));

        bus.Dispatch(new TestCommand(1));

        Assert.Equal(1, log.Count);
        var entry = log.GetEntry(0);
        Assert.False(entry.Result.Success);
        Assert.Contains("boom", entry.Result.Reason);
    }

    // ── Ring Buffer Behavior ────────────────────────────────────────────

    [Fact]
    public void RingBuffer_OverwritesOldestEntries_WhenFull()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 3);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        log.SetTick(1);
        bus.Dispatch(new TestCommand(1));
        log.SetTick(2);
        bus.Dispatch(new TestCommand(2));
        log.SetTick(3);
        bus.Dispatch(new TestCommand(3));
        log.SetTick(4);
        bus.Dispatch(new TestCommand(4));
        log.SetTick(5);
        bus.Dispatch(new TestCommand(5));

        Assert.Equal(3, log.Count);
        Assert.Equal(3, log.Capacity);
        var entries = log.GetAllEntries();
        Assert.Equal(3UL, entries[0].Tick);
        Assert.Equal(4UL, entries[1].Tick);
        Assert.Equal(5UL, entries[2].Tick);
    }

    [Fact]
    public void GetEntry_OldestFirst_ChronologicalOrder()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 4);
        log.IsEnabled = true;
        bus.Log = log;
        int counter = 0;
        bus.Register<TestCommand>(cmd =>
        {
            counter++;
            return CommandResult.Ok();
        });

        log.SetTick(10);
        bus.Dispatch(new TestCommand(1));
        log.SetTick(20);
        bus.Dispatch(new TestCommand(2));
        log.SetTick(30);
        bus.Dispatch(new TestCommand(3));

        Assert.Equal(10UL, log.GetEntry(0).Tick);
        Assert.Equal(20UL, log.GetEntry(1).Tick);
        Assert.Equal(30UL, log.GetEntry(2).Tick);
    }

    [Fact]
    public void GetEntry_OutOfRange_Throws()
    {
        var log = new CommandLog(capacity: 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => log.GetEntry(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => log.GetEntry(-1));
    }

    [Fact]
    public void GetAllEntries_ReturnsChronologicalSnapshot()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Register<AnotherTestCommand>(cmd => CommandResult.Fail("nope"));

        log.SetTick(1);
        bus.Dispatch(new TestCommand(10));
        log.SetTick(2);
        bus.Dispatch(new AnotherTestCommand("hello"));

        var entries = log.GetAllEntries();
        Assert.Equal(2, entries.Length);
        Assert.Equal("TestCommand", entries[0].CommandTypeName);
        Assert.Equal("AnotherTestCommand", entries[1].CommandTypeName);
        Assert.True(entries[0].Result.Success);
        Assert.False(entries[1].Result.Success);
    }

    // ── Replay ──────────────────────────────────────────────────────────

    [Fact]
    public void ReplayAll_RedispatchesAllCommands()
    {
        var logBus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        logBus.Log = log;
        logBus.Register<TestCommand>(cmd => CommandResult.Ok());

        logBus.Dispatch(new TestCommand(10));
        logBus.Dispatch(new TestCommand(20));
        logBus.Dispatch(new TestCommand(30));

        // Set up a replay bus with its own handler
        var replayBus = new CommandBus();
        var replayed = new List<int>();
        replayBus.Register<TestCommand>(cmd =>
        {
            replayed.Add(cmd.Value);
            return CommandResult.Ok();
        });

        var results = log.ReplayAll(replayBus);

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(new[] { 10, 20, 30 }, replayed);
    }

    [Fact]
    public void ReplayAll_PreservesOriginalCommandData()
    {
        var logBus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        logBus.Log = log;
        logBus.Register<AnotherTestCommand>(cmd => CommandResult.Ok());

        logBus.Dispatch(new AnotherTestCommand("alpha"));
        logBus.Dispatch(new AnotherTestCommand("beta"));

        var replayBus = new CommandBus();
        var labels = new List<string>();
        replayBus.Register<AnotherTestCommand>(cmd =>
        {
            labels.Add(cmd.Label);
            return CommandResult.Ok();
        });

        log.ReplayAll(replayBus);

        Assert.Equal(new[] { "alpha", "beta" }, labels);
    }

    // ── Clear ───────────────────────────────────────────────────────────

    [Fact]
    public void Clear_ResetsLog()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        bus.Dispatch(new TestCommand(1));
        bus.Dispatch(new TestCommand(2));
        Assert.Equal(2, log.Count);

        log.Clear();

        Assert.Equal(0, log.Count);
        Assert.Empty(log.GetAllEntries());
    }

    // ── Constructor Validation ──────────────────────────────────────────

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandLog(capacity: 0));
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandLog(capacity: -1));
    }

    // ── CommandLogEntry ToString ─────────────────────────────────────────

    [Fact]
    public void CommandLogEntry_ToString_Success()
    {
        var entry = new CommandLogEntry("PlaceStructureCommand", "Place Forestry at (5, 3)", null, CommandResult.Ok(), 42);

        var str = entry.ToString();

        Assert.Contains("Tick 42", str);
        Assert.Contains("PlaceStructureCommand", str);
        Assert.Contains("OK", str);
    }

    [Fact]
    public void CommandLogEntry_ToString_Fail()
    {
        var entry = new CommandLogEntry("DrawPathCommand", "DrawPath at (2, 2) facing North (Straight)", null, CommandResult.Fail("Not enough gold"), 10);

        var str = entry.ToString();

        Assert.Contains("Tick 10", str);
        Assert.Contains("DrawPathCommand", str);
        Assert.Contains("FAIL", str);
        Assert.Contains("Not enough gold", str);
    }

    // ── Validator Rejection Logging ─────────────────────────────────────

    [Fact]
    public void ValidatorRejection_IsLoggedWhenEnabled()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new RejectAllValidator());

        bus.Dispatch(new TestCommand(1));

        Assert.Equal(1, log.Count);
        var entry = log.GetEntry(0);
        Assert.False(entry.Result.Success);
        Assert.Equal("Rejected", entry.Result.Reason);
    }

    // ── Helper: simple validator that rejects all commands ───────────────

    private sealed class RejectAllValidator : ICommandValidator
    {
        public CommandResult? Validate<T>(T command) where T : IGameCommand
        {
            return CommandResult.Fail("Rejected");
        }
    }
}
