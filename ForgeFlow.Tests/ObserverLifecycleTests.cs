using ForgeFlow.Presentation.Unity.UI.Observers;

namespace ForgeFlow.Tests;

/// <summary>
/// Tests for the observer lifecycle patterns used in the inspector hierarchy.
/// Since BaseInspectorPanel depends on Unity types that can't be instantiated
/// in a pure .NET test, these tests validate the expected SubscriptionBag usage
/// patterns (build-once + tick-update, refresh clears bindings, dispose cleans
/// up, visibility gating) using a lightweight stand-in.
/// </summary>
public class ObserverLifecycleTests
{
    // ── Simulates the BaseInspectorPanel lifecycle ────────────────

    /// <summary>
    /// Minimal stand-in that mirrors BaseInspectorPanel's SubscriptionBag
    /// lifecycle without requiring Unity types.
    /// </summary>
    private sealed class FakeInspector : IDisposable
    {
        public readonly SubscriptionBag Bindings = new();
        public bool IsVisible { get; set; }

        // Tracks what the "UI" shows
        public string DisplayedStatus { get; set; } = "";
        public int DisplayedCount { get; set; }

        /// <summary>Mirrors BaseInspectorPanel.Refresh(): clears bindings + rebuilds.</summary>
        public void Refresh(FakeLogic logic)
        {
            Bindings.Clear();
            BuildContent(logic);
        }

        /// <summary>Mirrors BaseInspectorPanel.BuildContent(): builds UI + registers bindings.</summary>
        private void BuildContent(FakeLogic logic)
        {
            // Simulate setting initial UI state
            DisplayedStatus = logic.Status;
            DisplayedCount = logic.Count;

            // Register bindings for live updates
            Bindings.Watch(() => logic.Status)
                    .OnChanged(s => DisplayedStatus = s);
            Bindings.Watch(() => logic.Count)
                    .OnChanged(c => DisplayedCount = c);
        }

        /// <summary>Mirrors BaseInspectorPanel.UpdateBindings(): visibility-aware tick.</summary>
        public int UpdateBindings()
        {
            if (!IsVisible)
            {
                return 0;
            }

            return Bindings.UpdateAll();
        }

        public void Dispose()
        {
            Bindings.Dispose();
        }
    }

    private sealed class FakeLogic
    {
        public string Status { get; set; } = "Idle";
        public int Count { get; set; }
    }

    // ── Build-Once + Tick-Update Pattern ─────────────────────────

    [Fact]
    public void Lifecycle_BuildOnce_ThenTickUpdates_OnlyChangedValues()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };

        // Build content once
        inspector.Refresh(logic);

        Assert.Equal("Idle", inspector.DisplayedStatus);
        Assert.Equal(0, inspector.DisplayedCount);

        // Tick with no changes — nothing should fire
        int changed = inspector.UpdateBindings();
        Assert.Equal(0, changed);
        Assert.Equal("Idle", inspector.DisplayedStatus);

        // Change one value, tick
        logic.Status = "Working";
        changed = inspector.UpdateBindings();
        Assert.Equal(1, changed);
        Assert.Equal("Working", inspector.DisplayedStatus);
        Assert.Equal(0, inspector.DisplayedCount); // unchanged

        // Change both values, tick
        logic.Status = "Resting";
        logic.Count = 5;
        changed = inspector.UpdateBindings();
        Assert.Equal(2, changed);
        Assert.Equal("Resting", inspector.DisplayedStatus);
        Assert.Equal(5, inspector.DisplayedCount);

        inspector.Dispose();
    }

    // ── Refresh Clears Old Bindings ──────────────────────────────

    [Fact]
    public void Lifecycle_Refresh_ClearsOldBindings_CreatesNew()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };

        inspector.Refresh(logic);
        Assert.Equal(2, inspector.Bindings.Count);

        // Change data, then refresh (simulates switching to a new entity)
        logic.Status = "Changed";
        inspector.Refresh(logic);

        // Old bindings should be gone, new ones created
        Assert.Equal(2, inspector.Bindings.Count);
        Assert.Equal("Changed", inspector.DisplayedStatus); // set during BuildContent

        // Tick should not fire for old values since bindings are fresh
        int changed = inspector.UpdateBindings();
        Assert.Equal(0, changed);

        inspector.Dispose();
    }

    [Fact]
    public void Lifecycle_Refresh_OldBindings_AreDisposed()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };

        inspector.Refresh(logic);
        var firstBinding = inspector.Bindings.Bind(() => "extra", _ => { });
        Assert.Equal(3, inspector.Bindings.Count);

        // Refresh clears all bindings (including the extra one)
        inspector.Refresh(logic);

        // The old binding is disposed — Update should return false
        logic.Status = "Changed";
        Assert.False(firstBinding.Update());
        Assert.Equal(2, inspector.Bindings.Count);

        inspector.Dispose();
    }

    // ── Visibility Gating ────────────────────────────────────────

    [Fact]
    public void Lifecycle_HiddenInspector_UpdateBindings_ReturnsZero()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = false };

        inspector.Refresh(logic);

        logic.Status = "Working";
        logic.Count = 10;
        int changed = inspector.UpdateBindings();

        Assert.Equal(0, changed);
        // UI was set during BuildContent, but bindings didn't run
        Assert.Equal("Idle", inspector.DisplayedStatus);

        inspector.Dispose();
    }

    [Fact]
    public void Lifecycle_BecomeVisible_BindingsPickUpChanges()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = false };

        inspector.Refresh(logic);

        // Change while hidden
        logic.Status = "Working";
        inspector.UpdateBindings(); // no-op because hidden

        // Become visible
        inspector.IsVisible = true;
        int changed = inspector.UpdateBindings();

        Assert.Equal(1, changed);
        Assert.Equal("Working", inspector.DisplayedStatus);

        inspector.Dispose();
    }

    [Fact]
    public void Lifecycle_ToggleVisibility_DoesNotLoseBindings()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };

        inspector.Refresh(logic);
        Assert.Equal(2, inspector.Bindings.Count);

        // Hide
        inspector.IsVisible = false;
        Assert.Equal(2, inspector.Bindings.Count); // bindings still there

        // Show again
        inspector.IsVisible = true;
        logic.Status = "Updated";
        int changed = inspector.UpdateBindings();

        Assert.Equal(1, changed);
        Assert.Equal("Updated", inspector.DisplayedStatus);

        inspector.Dispose();
    }

    // ── Dispose ──────────────────────────────────────────────────

    [Fact]
    public void Lifecycle_Dispose_CleansUpBindings()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };

        inspector.Refresh(logic);
        Assert.Equal(2, inspector.Bindings.Count);

        inspector.Dispose();
        Assert.Equal(0, inspector.Bindings.Count);

        // Subsequent tick is safe
        int changed = inspector.UpdateBindings();
        Assert.Equal(0, changed);
    }

    [Fact]
    public void Lifecycle_Dispose_ThenRefresh_ThrowsOnNewBindings()
    {
        var logic = new FakeLogic { Status = "Idle" };
        var inspector = new FakeInspector { IsVisible = true };

        inspector.Dispose();

        // SubscriptionBag is disposed — Refresh's BuildContent will try to add bindings
        Assert.Throws<ObjectDisposedException>(() => inspector.Refresh(logic));
    }

    // ── Multi-Entity Switch ──────────────────────────────────────

    [Fact]
    public void Lifecycle_SwitchEntity_RefreshRebindsToNewData()
    {
        var logic1 = new FakeLogic { Status = "Entity1", Count = 10 };
        var logic2 = new FakeLogic { Status = "Entity2", Count = 20 };
        var inspector = new FakeInspector { IsVisible = true };

        // Inspect first entity
        inspector.Refresh(logic1);
        Assert.Equal("Entity1", inspector.DisplayedStatus);
        Assert.Equal(10, inspector.DisplayedCount);

        // Switch to second entity
        inspector.Refresh(logic2);
        Assert.Equal("Entity2", inspector.DisplayedStatus);
        Assert.Equal(20, inspector.DisplayedCount);

        // Tick — bindings now track logic2
        logic2.Count = 99;
        int changed = inspector.UpdateBindings();
        Assert.Equal(1, changed);
        Assert.Equal(99, inspector.DisplayedCount);

        // Changes to logic1 have no effect
        logic1.Status = "StaleChange";
        changed = inspector.UpdateBindings();
        Assert.Equal(0, changed);
        Assert.Equal("Entity2", inspector.DisplayedStatus);

        inspector.Dispose();
    }

    // ── Rapid Tick With No Changes ───────────────────────────────

    [Fact]
    public void Lifecycle_ManyTicksNoChanges_ZeroCost()
    {
        var logic = new FakeLogic { Status = "Idle", Count = 0 };
        var inspector = new FakeInspector { IsVisible = true };
        int callbackCount = 0;

        inspector.Refresh(logic);
        // Add an extra binding that counts invocations
        inspector.Bindings.Watch(() => logic.Status)
                          .OnChanged(_ => callbackCount++);

        // 100 ticks with no changes
        for (int i = 0; i < 100; i++)
        {
            inspector.UpdateBindings();
        }

        Assert.Equal(0, callbackCount);

        inspector.Dispose();
    }
}
