using ForgeFlow.Core.Commands;

namespace ForgeFlow.Tests;

public class CommandValidatorTests
{
    // ── Basic Validation ────────────────────────────────────────────────

    [Fact]
    public void NoValidators_HandlerExecutesNormally()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        var result = bus.Dispatch(new TestCommand(1));

        Assert.True(result.Success);
    }

    [Fact]
    public void Validator_ReturnsNull_HandlerExecutes()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new PassAllValidator());

        var result = bus.Dispatch(new TestCommand(1));

        Assert.True(result.Success);
    }

    [Fact]
    public void Validator_RejectsFail_HandlerNeverRuns()
    {
        var bus = new CommandBus();
        bool handlerCalled = false;
        bus.Register<TestCommand>(cmd =>
        {
            handlerCalled = true;
            return CommandResult.Ok();
        });
        bus.AddValidator(new RejectAllValidator("Blocked by validator"));

        var result = bus.Dispatch(new TestCommand(1));

        Assert.False(result.Success);
        Assert.Equal("Blocked by validator", result.Reason);
        Assert.False(handlerCalled);
    }

    [Fact]
    public void MultipleValidators_FirstRejectionWins()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new PassAllValidator());
        bus.AddValidator(new RejectAllValidator("Second validator"));
        bus.AddValidator(new RejectAllValidator("Third validator")); // should never run

        var result = bus.Dispatch(new TestCommand(1));

        Assert.False(result.Success);
        Assert.Equal("Second validator", result.Reason);
    }

    [Fact]
    public void MultipleValidators_AllPass_HandlerExecutes()
    {
        var bus = new CommandBus();
        int handlerValue = 0;
        bus.Register<TestCommand>(cmd =>
        {
            handlerValue = cmd.Value;
            return CommandResult.Ok();
        });
        bus.AddValidator(new PassAllValidator());
        bus.AddValidator(new PassAllValidator());

        bus.Dispatch(new TestCommand(42));

        Assert.Equal(42, handlerValue);
    }

    // ── Type-Specific Validation ────────────────────────────────────────

    [Fact]
    public void Validator_CanInspectCommandType_SelectiveRejection()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Register<AnotherTestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new RejectTestCommandOnly());

        var testResult = bus.Dispatch(new TestCommand(1));
        var anotherResult = bus.Dispatch(new AnotherTestCommand("hello"));

        Assert.False(testResult.Success);
        Assert.Equal("TestCommand not allowed", testResult.Reason);
        Assert.True(anotherResult.Success);
    }

    // ── RemoveValidator ─────────────────────────────────────────────────

    [Fact]
    public void RemoveValidator_AllowsPreviouslyRejectedCommands()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        var validator = new RejectAllValidator("Blocked");
        bus.AddValidator(validator);

        var blocked = bus.Dispatch(new TestCommand(1));
        Assert.False(blocked.Success);

        bus.RemoveValidator(validator);

        var allowed = bus.Dispatch(new TestCommand(1));
        Assert.True(allowed.Success);
    }

    [Fact]
    public void RemoveValidator_NonExistent_DoesNotThrow()
    {
        var bus = new CommandBus();

        bus.RemoveValidator(new PassAllValidator());
    }

    // ── Clear Includes Validators ───────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllValidators()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new RejectAllValidator("Blocked"));

        bus.Clear();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        var result = bus.Dispatch(new TestCommand(1));
        Assert.True(result.Success);
    }

    // ── Validator + Logging Integration ─────────────────────────────────

    [Fact]
    public void ValidatorRejection_RecordedInLog()
    {
        var bus = new CommandBus();
        var log = new CommandLog(capacity: 16);
        log.IsEnabled = true;
        bus.Log = log;
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.AddValidator(new RejectAllValidator("Tutorial lock"));

        bus.Dispatch(new TestCommand(1));

        Assert.Equal(1, log.Count);
        var entry = log.GetEntry(0);
        Assert.False(entry.Result.Success);
        Assert.Equal("Tutorial lock", entry.Result.Reason);
    }

    // ── Helper Validators ───────────────────────────────────────────────

    private sealed class PassAllValidator : ICommandValidator
    {
        public CommandResult? Validate<T>(T command) where T : IGameCommand
        {
            return null;
        }
    }

    private sealed class RejectAllValidator : ICommandValidator
    {
        private readonly string _reason;

        public RejectAllValidator(string reason = "Rejected")
        {
            _reason = reason;
        }

        public CommandResult? Validate<T>(T command) where T : IGameCommand
        {
            return CommandResult.Fail(_reason);
        }
    }

    private sealed class RejectTestCommandOnly : ICommandValidator
    {
        public CommandResult? Validate<T>(T command) where T : IGameCommand
        {
            if (command is TestCommand)
            {
                return CommandResult.Fail("TestCommand not allowed");
            }
            return null;
        }
    }
}
