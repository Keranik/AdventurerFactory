using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Modal configuration window for automation gates (CheckGate, FilterSplitter, Balancer).
    /// Opens when the player clicks an automation structure. Uses Forge UI components
    /// for rule editors with dropdowns, toggles, and direction selectors.
    /// Managed by UIManager.
    /// </summary>
    internal sealed class AutomationConfigWindow : IUIWindow
    {
        private readonly SimulationTicker _simulation;
        private readonly EventBus _eventBus;
        private readonly ForgePanel _panel;
            private readonly ForgeContainer _rulesContainer;
        private ulong? _selectedStructureId;
        private string _selectedStructureCategory = string.Empty;
        private bool _isVisible;

        public string WindowId => WindowIds.AutomationConfig;
        public VisualElement Root => _panel;
        public bool IsVisible => _isVisible;
        public ForgePanel Panel => _panel;

        private static readonly List<string> ConditionTypeNames = Enum.GetNames(typeof(GateConditionType)).ToList();
        private static readonly List<string> DirectionNames = Enum.GetNames(typeof(Direction)).ToList();

        public AutomationConfigWindow(SimulationTicker simulation, EventBus eventBus)
        {
            _simulation = simulation;
            _eventBus = eventBus;

            _panel = new ForgePanel("automation_config", LocalizationKeys.AutomationConfigTitle, 360f, 400f);
            _panel.SetPosition(320f, 60f);

            _rulesContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            _panel.ContentContainer.Add(_rulesContainer);

            _panel.Closed += () => Hide();

            _eventBus.Subscribe<StructureInspectedEvent>(OnStructureInspected);
        }

        public void Show(object? context = null)
        {
            _isVisible = true;
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _isVisible = false;
            _panel.style.display = DisplayStyle.None;
        }

        public void Refresh() { }
        public void ApplyTheme() { }

        public void Dispose()
        {
            _eventBus.Unsubscribe<StructureInspectedEvent>(OnStructureInspected);
        }

        private void OnStructureInspected(StructureInspectedEvent e)
        {
            if (e.StructureType != "CheckGate" &&
                e.StructureType != "FilterSplitter" &&
                e.StructureType != "Balancer")
            {
                return;
            }

            _selectedStructureId = e.StructureId;

            StructureBase? entity = _simulation.EntityManager.GetStructure(new EntityId(e.StructureId))
                                         ?? (StructureBase?)_simulation.EntityManager.GetRoutingNode(new EntityId(e.StructureId));
            if (entity == null)
            {
                return;
            }

            _selectedStructureCategory = entity.GetCategoryName();
            ShowConfigForStructure(entity);
        }

        private void ShowConfigForStructure(StructureBase entity)
        {
            _rulesContainer.Clear();

            switch (entity)
            {
                case CheckGateLogic checkGate:
                    BuildCheckGateUI(checkGate);
                    break;
                case FilterSplitterLogic filterSplitter:
                    BuildFilterSplitterUI(filterSplitter);
                    break;
                case BalancerLogic balancer:
                    BuildBalancerUI(balancer);
                    break;
            }

            Show();
        }

        // ── CheckGate UI ────────────────────────────────────────────

        private void BuildCheckGateUI(CheckGateLogic gate)
        {
            var headerLabel = ForgeLabel.Create(LocalizationKeys.CheckGateTitle, ForgeLabelSize.Normal)
                .FontSize(16).Color("text.accent").MarginBottom(6).Build();
            _rulesContainer.Add(headerLabel);

            var conditionDropdown = new ForgeDropdown(LocalizationKeys.GateCondition, ConditionTypeNames,
                (int)gate.Rule.ConditionType);
            conditionDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<GateConditionType>(val, out var ct))
                {
                    gate.Rule.ConditionType = ct;
                }
            };
            _rulesContainer.Add(conditionDropdown);

            var conditionValue = new ForgeTextField(LocalizationKeys.GateCondition, gate.Rule.ConditionValue);
            conditionValue.TextChanged += val => gate.Rule.ConditionValue = val;
            _rulesContainer.Add(conditionValue);

            var outputDropdown = new ForgeDropdown(LocalizationKeys.GateOutput, DirectionNames,
                (int)gate.Rule.OutputDirection);
            outputDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<Direction>(val, out var dir))
                {
                    gate.Rule.OutputDirection = dir;
                }
            };
            _rulesContainer.Add(outputDropdown);

            var defaultDropdown = new ForgeDropdown(LocalizationKeys.GateDefault, DirectionNames,
                (int)gate.DefaultDirection);
            defaultDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<Direction>(val, out var dir))
                {
                    gate.DefaultDirection = dir;
                }
            };
            _rulesContainer.Add(defaultDropdown);

            var applyBtn = new ForgeButton(LocalizationKeys.AutomationApply, () =>
            {
                Hide();
                Debug.Log($"[AutomationConfig] CheckGate #{_selectedStructureId} configured: {gate.Rule.ConditionType}={gate.Rule.ConditionValue} → {gate.Rule.OutputDirection}");
            });
            _rulesContainer.Add(applyBtn);
        }

        // ── FilterSplitter UI ───────────────────────────────────────

        private void BuildFilterSplitterUI(FilterSplitterLogic splitter)
        {
            var headerLabel = ForgeLabel.Create(LocalizationKeys.FilterSplitterTitle, ForgeLabelSize.Normal)
                .FontSize(16).Color("text.accent").MarginBottom(6).Build();
            _rulesContainer.Add(headerLabel);

            for (int i = 0; i < splitter.Rules.Count; i++)
            {
                int ruleIndex = i;
                var rule = splitter.Rules[i];
                AddRuleEditorRow(rule, ruleIndex, () =>
                {
                    splitter.Rules.RemoveAt(ruleIndex);
                    ShowConfigForStructure(splitter);
                });
            }

            var defaultDropdown = new ForgeDropdown(LocalizationKeys.GateDefault, DirectionNames,
                (int)splitter.DefaultDirection);
            defaultDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<Direction>(val, out var dir))
                {
                    splitter.DefaultDirection = dir;
                }
            };
            _rulesContainer.Add(defaultDropdown);

            var addBtn = new ForgeButton(LocalizationKeys.AutomationAddRule, () =>
            {
                splitter.Rules.Add(new GateRule());
                ShowConfigForStructure(splitter);
            });
            _rulesContainer.Add(addBtn);

            var applyBtn = new ForgeButton(LocalizationKeys.AutomationApply, () =>
            {
                Hide();
                Debug.Log($"[AutomationConfig] FilterSplitter #{_selectedStructureId} configured with {splitter.Rules.Count} rules");
            });
            _rulesContainer.Add(applyBtn);
        }

        private void AddRuleEditorRow(GateRule rule, int index, Action onRemove)
        {
            var ruleContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .MarginBottom(4).PaddingBottom(4)
                .BorderColor("border.normal")
                .Build();
            ruleContainer.style.borderBottomWidth = 1;

            var ruleHeader = ForgeLabel.CreateRaw($"Rule #{index + 1}", ForgeLabelSize.Small)
                .FontSize(13).Color("text.primary").Build();
            ruleContainer.Add(ruleHeader);

            var condDropdown = new ForgeDropdown(LocalizationKeys.GateCondition, ConditionTypeNames,
                (int)rule.ConditionType);
            condDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<GateConditionType>(val, out var ct))
                {
                    rule.ConditionType = ct;
                }
            };
            ruleContainer.Add(condDropdown);

            var valField = new ForgeTextField(LocalizationKeys.GateCondition, rule.ConditionValue);
            valField.TextChanged += val => rule.ConditionValue = val;
            ruleContainer.Add(valField);

            var dirDropdown = new ForgeDropdown(LocalizationKeys.GateOutput, DirectionNames,
                (int)rule.OutputDirection);
            dirDropdown.SelectionChanged += val =>
            {
                if (Enum.TryParse<Direction>(val, out var dir))
                {
                    rule.OutputDirection = dir;
                }
            };
            ruleContainer.Add(dirDropdown);

            var removeBtn = new ForgeButton(LocalizationKeys.AutomationRemoveRule, onRemove);
            ruleContainer.Add(removeBtn);

            _rulesContainer.Add(ruleContainer);
        }

        // ── Balancer UI ─────────────────────────────────────────────

        private void BuildBalancerUI(BalancerLogic balancer)
        {
            var headerLabel = ForgeLabel.Create(LocalizationKeys.BalancerTitle, ForgeLabelSize.Normal)
                .FontSize(16).Color("text.accent").MarginBottom(6).Build();
            _rulesContainer.Add(headerLabel);

            var descLabel = ForgeLabel.Create(LocalizationKeys.BalancerOutputs, ForgeLabelSize.Small)
                .FontSize(13).Color("text.secondary").MarginBottom(4).Build();
            _rulesContainer.Add(descLabel);

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                bool isActive = balancer.OutputDirections.Contains(dir);
                var dirLabel = dir.ToString();
                var toggle = new ForgeToggle(dirLabel, isActive);
                Direction capturedDir = dir;
                toggle.ValueChanged += enabled =>
                {
                    if (enabled && !balancer.OutputDirections.Contains(capturedDir))
                    {
                        balancer.OutputDirections.Add(capturedDir);
                    }
                    else if (!enabled)
                    {
                        balancer.OutputDirections.Remove(capturedDir);
                    }
                };
                _rulesContainer.Add(toggle);
            }

            var applyBtn = new ForgeButton(LocalizationKeys.AutomationApply, () =>
            {
                balancer.ResetCounter();
                Hide();
                Debug.Log($"[AutomationConfig] Balancer #{_selectedStructureId} configured with {balancer.OutputDirections.Count} outputs");
            });
            _rulesContainer.Add(applyBtn);
        }
    }
}
