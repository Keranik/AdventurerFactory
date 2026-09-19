using System;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using ForgeFlow.Presentation.Unity.UI.Observers;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Base class for all inspector panels. Provides shared infrastructure:
    /// ForgePanel setup, Show/Hide/Refresh lifecycle, content container,
    /// and reusable row/section helpers. Concrete inspectors inherit and
    /// override <see cref="BuildContent"/> to populate their specific UI.
    /// </summary>
    internal abstract class BaseInspectorPanel : IUIWindow
    {
        public abstract string WindowId { get; }

        protected readonly ForgePanel Panel;
        protected readonly ForgeContainer ContentContainer;
        protected readonly SimulationTicker? Simulation;

        /// <summary>
        /// Shared subscription bag for observer-based incremental updates.
        /// Concrete inspectors register bindings in <see cref="BuildContent"/>
        /// and call <see cref="UpdateBindings"/> from <see cref="ITickableWindow.Tick"/>
        /// instead of <see cref="Refresh"/>.
        /// Bindings are automatically cleared on each <see cref="Refresh"/> call
        /// and disposed when the inspector is disposed.
        /// </summary>
        protected readonly SubscriptionBag Bindings = new();

        private bool _isVisible;

        public VisualElement Root => Panel;
        public bool IsVisible => _isVisible;

        protected BaseInspectorPanel(
            string panelId,
            string titleLocKey,
            float width,
            float height,
            SimulationTicker? simulation)
        {
            Simulation = simulation;
            Panel = new ForgePanel(panelId, titleLocKey, width, height);
            Panel.style.right = 10;
            Panel.style.top = 10;
            Panel.style.left = StyleKeyword.Auto;
            Panel.style.height = StyleKeyword.Auto;
            Panel.Closed += Hide;

            ContentContainer = new ForgeContainer();
            Panel.ContentContainer.Add(ContentContainer);
        }

        public virtual void Show(object? context = null)
        {
            _isVisible = true;
            Panel.Show();
        }

        public virtual void Hide()
        {
            _isVisible = false;
            Panel.Hide();
        }

        public void Refresh()
        {
            Bindings.Clear();
            ContentContainer.Clear();
            if (Simulation == null)
            {
                return;
            }
            BuildContent();
        }

        /// <summary>Override to populate the content container with inspector-specific UI.</summary>
        protected abstract void BuildContent();

        public virtual void ApplyTheme()
        {
            Panel.ApplyTheme();
        }

        public virtual void Dispose()
        {
            Bindings.Dispose();
        }

        /// <summary>
        /// Visibility-aware tick helper. Evaluates all bindings registered in
        /// <see cref="Bindings"/> and fires callbacks only for values that
        /// actually changed. Returns the number of changed bindings, or 0 if
        /// the inspector is hidden (no work performed).
        /// <para>
        /// Call this from <see cref="ITickableWindow.Tick"/> instead of
        /// <see cref="Refresh"/> to get incremental, non-destructive updates.
        /// </para>
        /// </summary>
        protected int UpdateBindings()
        {
            if (!_isVisible)
            {
                return 0;
            }

            return Bindings.UpdateAll();
        }

        /// <summary>
        /// Registers an observer binding that monitors a value via a getter
        /// and invokes the callback only when the value actually changes.
        /// Convenience shorthand for <see cref="SubscriptionBag.Bind{T}"/>
        /// registered in the shared <see cref="Bindings"/> bag.
        /// <para>
        /// Usage: <c>Observe(() => entity.Property, value => label.Text = value.ToString());</c>
        /// </para>
        /// </summary>
        protected InspectorBinding<T> Observe<T>(Func<T> getter, Action<T> onChange)
        {
            return Bindings.Bind(getter, onChange);
        }

        // ── Shared Helpers ──────────────────────────────────────────

        /// <summary>Adds a localized key-value row to the content container.</summary>
        protected void AddRow(string locKey, string value)
        {
            ContentContainer.Add(CreateRow(locKey, value));
        }

        /// <summary>Adds a name/type header at the top of the inspector.</summary>
        protected void AddNameHeader(string displayName)
        {
            ContentContainer.Add(
                ForgeLabel.CreateRaw(displayName, ForgeLabelSize.Large)
                    .MarginBottom(4).Build());
        }

        /// <summary>Adds a section separator label.</summary>
        protected void AddSectionHeader(string locKey)
        {
            ContentContainer.Add(
                ForgeLabel.Create(locKey, ForgeLabelSize.Normal)
                    .Bold().MarginTop(6).MarginBottom(2).Build());
        }

        /// <summary>Creates a standard small label row with localized key + value.</summary>
        protected static ForgeLabel CreateRow(string locKey, string value)
        {
            return ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(locKey)}: {value}",
                ForgeLabelSize.Small)
                .MarginBottom(2).Build();
        }
    }
}