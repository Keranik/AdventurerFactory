using ForgeFlow.Core.Commands;

namespace ForgeFlow.Tests;

public readonly struct TestCommand : IGameCommand
{
    public int Value { get; }
    public TestCommand(int value) => Value = value;
    public override string ToString() => $"TestCommand({Value})";
}

public readonly struct AnotherTestCommand : IGameCommand
{
    public string Label { get; }
    public AnotherTestCommand(string label) => Label = label;
    public override string ToString() => $"AnotherTestCommand({Label})";
}

public readonly struct UnhandledTestCommand : IGameCommand { }

public class CommandBusTests
{
    // ── Registration ─────────────────────────────────────────────────

    [Fact]
    public void Register_HandlerIsAvailable()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        Assert.True(bus.HasHandler<TestCommand>());
    }

    [Fact]
    public void Register_DuplicateHandler_Throws()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            bus.Register<TestCommand>(cmd => CommandResult.Fail("second")));

        Assert.Contains("already registered", ex.Message);
        Assert.Contains("TestCommand", ex.Message);
    }

    [Fact]
    public void Register_DifferentCommandTypes_DoNotConflict()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Register<AnotherTestCommand>(cmd => CommandResult.Ok());

        Assert.True(bus.HasHandler<TestCommand>());
        Assert.True(bus.HasHandler<AnotherTestCommand>());
    }

    // ── Dispatch ─────────────────────────────────────────────────────

    [Fact]
    public void Dispatch_InvokesHandler_ReturnsResult()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        var result = bus.Dispatch(new TestCommand(42));

        Assert.True(result.Success);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Dispatch_HandlerReceivesCommandData()
    {
        var bus = new CommandBus();
        int received = -1;
        bus.Register<TestCommand>(cmd =>
        {
            received = cmd.Value;
            return CommandResult.Ok();
        });

        bus.Dispatch(new TestCommand(99));

        Assert.Equal(99, received);
    }

    [Fact]
    public void Dispatch_FailResult_PropagatesReasonString()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Fail("Not enough gold"));

        var result = bus.Dispatch(new TestCommand(1));

        Assert.False(result.Success);
        Assert.Equal("Not enough gold", result.Reason);
    }

    [Fact]
    public void Dispatch_NoHandler_Throws()
    {
        var bus = new CommandBus();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            bus.Dispatch(new UnhandledTestCommand()));

        Assert.Contains("No handler registered", ex.Message);
        Assert.Contains("UnhandledTestCommand", ex.Message);
    }

    // ── Error Handling ───────────────────────────────────────────────

    [Fact]
    public void Dispatch_HandlerThrows_NoCallback_ExceptionPropagates()
    {
        var bus = new CommandBus();
        bus.OnHandlerException = null;
        bus.Register<TestCommand>(cmd => throw new InvalidOperationException("boom"));

        Assert.Throws<InvalidOperationException>(() =>
            bus.Dispatch(new TestCommand(1)));
    }

    [Fact]
    public void Dispatch_HandlerThrows_WithCallback_ReturnsFailResult()
    {
        var bus = new CommandBus();
        Exception? captured = null;
        bus.OnHandlerException = ex => captured = ex;
        bus.Register<TestCommand>(cmd => throw new InvalidOperationException("handler error"));

        var result = bus.Dispatch(new TestCommand(1));

        Assert.False(result.Success);
        Assert.Contains("handler error", result.Reason);
        Assert.NotNull(captured);
        Assert.IsType<InvalidOperationException>(captured);
    }

    // ── Unregister ───────────────────────────────────────────────────

    [Fact]
    public void Unregister_RemovesHandler()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        Assert.True(bus.HasHandler<TestCommand>());

        bus.Unregister<TestCommand>();

        Assert.False(bus.HasHandler<TestCommand>());
    }

    [Fact]
    public void Unregister_NonExistent_DoesNotThrow()
    {
        var bus = new CommandBus();

        // Should be safe to call without prior registration
        bus.Unregister<TestCommand>();

        Assert.False(bus.HasHandler<TestCommand>());
    }

    [Fact]
    public void Unregister_ThenDispatch_Throws()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Unregister<TestCommand>();

        Assert.Throws<InvalidOperationException>(() =>
            bus.Dispatch(new TestCommand(1)));
    }

    [Fact]
    public void Unregister_ThenReRegister_Works()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Fail("first"));
        bus.Unregister<TestCommand>();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());

        var result = bus.Dispatch(new TestCommand(1));

        Assert.True(result.Success);
    }

    // ── Clear ────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllHandlers()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Register<AnotherTestCommand>(cmd => CommandResult.Ok());

        bus.Clear();

        Assert.False(bus.HasHandler<TestCommand>());
        Assert.False(bus.HasHandler<AnotherTestCommand>());
    }

    [Fact]
    public void Clear_ThenDispatch_Throws()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.Clear();

        Assert.Throws<InvalidOperationException>(() =>
            bus.Dispatch(new TestCommand(1)));
    }

    // ── HasHandler ───────────────────────────────────────────────────

    [Fact]
    public void HasHandler_ReturnsFalse_WhenNoHandlerRegistered()
    {
        var bus = new CommandBus();

        Assert.False(bus.HasHandler<TestCommand>());
    }

    // ── CommandResult ────────────────────────────────────────────────

    [Fact]
    public void CommandResult_Ok_IsSuccess()
    {
        var result = CommandResult.Ok();

        Assert.True(result.Success);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void CommandResult_Fail_IsNotSuccess()
    {
        var result = CommandResult.Fail("insufficient gold");

        Assert.False(result.Success);
        Assert.Equal("insufficient gold", result.Reason);
    }

    [Fact]
    public void CommandResult_ToString_Ok()
    {
        var result = CommandResult.Ok();

        Assert.Equal("CommandResult(OK)", result.ToString());
    }

    [Fact]
    public void CommandResult_ToString_Fail()
    {
        var result = CommandResult.Fail("tile occupied");

        Assert.Equal("CommandResult(FAIL: tile occupied)", result.ToString());
    }

    // ── Integration: Multiple Command Types ──────────────────────────

    [Fact]
    public void Dispatch_MultipleCommandTypes_IndependentHandlers()
    {
        var bus = new CommandBus();
        int testValue = 0;
        string? anotherLabel = null;

        bus.Register<TestCommand>(cmd =>
        {
            testValue = cmd.Value;
            return CommandResult.Ok();
        });
        bus.Register<AnotherTestCommand>(cmd =>
        {
            anotherLabel = cmd.Label;
            return CommandResult.Ok();
        });

        bus.Dispatch(new TestCommand(42));
        bus.Dispatch(new AnotherTestCommand("hello"));

        Assert.Equal(42, testValue);
        Assert.Equal("hello", anotherLabel);
    }
}
