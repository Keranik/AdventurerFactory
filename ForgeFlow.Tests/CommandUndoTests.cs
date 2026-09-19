using ForgeFlow.Core.Commands;

namespace ForgeFlow.Tests;

public class CommandUndoTests
{
    // ── Basic Undo ──────────────────────────────────────────────────────

    [Fact]
    public void Undo_EmptyStack_ReturnsFail()
    {
        var bus = new CommandBus();

        var result = bus.Undo();

        Assert.False(result.Success);
        Assert.Contains("Nothing to undo", result.Reason);
    }

    [Fact]
    public void CanUndo_EmptyStack_ReturnsFalse()
    {
        var bus = new CommandBus();

        Assert.False(bus.CanUndo);
        Assert.Equal(0, bus.UndoCount);
    }

    [Fact]
    public void PushUndo_ThenUndo_ExecutesAction()
    {
        var bus = new CommandBus();
        bool undone = false;

        bus.PushUndo(() => undone = true);

        Assert.True(bus.CanUndo);
        Assert.Equal(1, bus.UndoCount);

        var result = bus.Undo();

        Assert.True(result.Success);
        Assert.True(undone);
        Assert.False(bus.CanUndo);
        Assert.Equal(0, bus.UndoCount);
    }

    [Fact]
    public void Undo_LIFO_Order()
    {
        var bus = new CommandBus();
        var order = new List<string>();

        bus.PushUndo(() => order.Add("first"));
        bus.PushUndo(() => order.Add("second"));
        bus.PushUndo(() => order.Add("third"));

        bus.Undo();
        bus.Undo();
        bus.Undo();

        Assert.Equal(new[] { "third", "second", "first" }, order);
    }

    [Fact]
    public void PushUndo_FromHandler_IntegratesWithDispatch()
    {
        var bus = new CommandBus();
        var placed = new List<int>();

        bus.Register<TestCommand>(cmd =>
        {
            placed.Add(cmd.Value);
            var capturedValue = cmd.Value;
            bus.PushUndo(() => placed.Remove(capturedValue));
            return CommandResult.Ok();
        });

        bus.Dispatch(new TestCommand(10));
        bus.Dispatch(new TestCommand(20));

        Assert.Equal(new[] { 10, 20 }, placed);
        Assert.Equal(2, bus.UndoCount);

        bus.Undo();
        Assert.Equal(new[] { 10 }, placed);

        bus.Undo();
        Assert.Empty(placed);
    }

    // ── MaxUndoDepth ────────────────────────────────────────────────────

    [Fact]
    public void MaxUndoDepth_Default_Is50()
    {
        var bus = new CommandBus();

        Assert.Equal(50, bus.MaxUndoDepth);
    }

    [Fact]
    public void MaxUndoDepth_EnforcesLimit_DiscardsOldest()
    {
        var bus = new CommandBus();
        bus.MaxUndoDepth = 3;
        var order = new List<string>();

        bus.PushUndo(() => order.Add("a"));
        bus.PushUndo(() => order.Add("b"));
        bus.PushUndo(() => order.Add("c"));
        bus.PushUndo(() => order.Add("d")); // "a" should be discarded

        Assert.Equal(3, bus.UndoCount);

        bus.Undo();
        bus.Undo();
        bus.Undo();
        var undoAfterEmpty = bus.Undo();

        Assert.Equal(new[] { "d", "c", "b" }, order);
        Assert.False(undoAfterEmpty.Success);
    }

    // ── Error Handling ──────────────────────────────────────────────────

    [Fact]
    public void Undo_ActionThrows_NoCallback_Propagates()
    {
        var bus = new CommandBus();
        bus.OnHandlerException = null;
        bus.PushUndo(() => throw new InvalidOperationException("undo failed"));

        Assert.Throws<InvalidOperationException>(() => bus.Undo());
    }

    [Fact]
    public void Undo_ActionThrows_WithCallback_ReturnsFailResult()
    {
        var bus = new CommandBus();
        Exception? captured = null;
        bus.OnHandlerException = ex => captured = ex;
        bus.PushUndo(() => throw new InvalidOperationException("undo error"));

        var result = bus.Undo();

        Assert.False(result.Success);
        Assert.Contains("undo error", result.Reason);
        Assert.NotNull(captured);
    }

    // ── ClearUndoStack ──────────────────────────────────────────────────

    [Fact]
    public void ClearUndoStack_RemovesAllActions()
    {
        var bus = new CommandBus();
        bus.PushUndo(() => { });
        bus.PushUndo(() => { });

        bus.ClearUndoStack();

        Assert.False(bus.CanUndo);
        Assert.Equal(0, bus.UndoCount);
    }

    [Fact]
    public void Clear_AlsoClearsUndoStack()
    {
        var bus = new CommandBus();
        bus.Register<TestCommand>(cmd => CommandResult.Ok());
        bus.PushUndo(() => { });

        bus.Clear();

        Assert.False(bus.CanUndo);
        Assert.False(bus.HasHandler<TestCommand>());
    }
}
