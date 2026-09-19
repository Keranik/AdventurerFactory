using System;
using System.Collections.Generic;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Events;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = ForgeFlow.Core.Entities.EntityId;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Inspector panel for HeroEntity (worker) entities. Shows class, level,
    /// durability/stamina/carry bars, wear-out status, traits, abilities,
    /// and a send-to-maintenance button.
    ///
    /// Uses the observer pattern: UI is built once, all dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class RichWorkerInspectorPanel : BaseEntityInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.WorkerInspector;

        private ulong _inspectedWorkerId;

        public event Action<ulong>? OnSendToMaintenance;

        public RichWorkerInspectorPanel(SimulationTicker? simulation)
            : base("worker_inspector", LocalizationKeys.InspectorTitle, 320f, 500f, simulation)
        {
        }

        public void InspectWorker(ulong workerId)
        {
            _inspectedWorkerId = workerId;
            Refresh();
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildContent()
        {
            ResetRowIndex();

            if (!Simulation!.EntityManager.HeroIndex.TryGetValue(new EntityId(_inspectedWorkerId), out var worker))
            {
                return;
            }

            var content = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();

            // ── Header ──
            var headerLabel = ForgeLabel.CreateRaw(
                $"Worker #{worker.Id} — {worker.ClassId}", ForgeLabelSize.Large)
                .Color("text.accent").MarginBottom(ThemePaddingSmall).Build();
            content.Add(headerLabel);

            // ── Basic Info ──
            var levelValueLabel = ForgeLabel.CreateRaw(worker.Level.ToString(), ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var levelKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.InspectorLevel), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(levelKeyLabel).Child(levelValueLabel).Build());

            var posValueLabel = ForgeLabel.CreateRaw(
                $"({worker.Position.X}, {worker.Position.Y})", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var posKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.InspectorPosition), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(posKeyLabel).Child(posValueLabel).Build());

            // Profession
            var professionLabel = ForgeLabel.CreateRaw(
                $"{L(LocalizationKeys.WorkerProfession)}: {worker.Profession}", ForgeLabelSize.Normal)
                .Color("text.accent").MarginBottom(2).Build();
            content.Add(professionLabel);

            Observe(() => worker.Level, level => { levelValueLabel.Text = level.ToString(); });
            Observe(() => worker.Position, pos => { posValueLabel.Text = $"({pos.X}, {pos.Y})"; });
            Observe(() => worker.Profession, prof =>
            {
                professionLabel.Text = $"{L(LocalizationKeys.WorkerProfession)}: {prof}";
            });

            // ── Progress Bars ──
            var durabilityBar = ForgeProgressBar.Create(LocalizationKeys.WorkerDurability, 0f, 100f)
                .Value(worker.ToolDurability).Build();
            content.Add(durabilityBar);

            var staminaBar = ForgeProgressBar.Create(LocalizationKeys.WorkerStamina, 0f, 100f)
                .Value(worker.Stamina).Build();
            content.Add(staminaBar);

            float carryPct = (worker.MaxCarryCapacity > 0) ? (worker.CarryLoad / worker.MaxCarryCapacity * 100f) : 0f;
            var carryBar = ForgeProgressBar.Create(LocalizationKeys.WorkerCarryLoad, 0f, 100f)
                .Value(carryPct).Build();
            content.Add(carryBar);

            Observe(() => worker.ToolDurability, d => { durabilityBar.Value = d; });
            Observe(() => worker.Stamina, s => { staminaBar.Value = s; });
            Observe(() => worker.CarryLoad, load =>
            {
                float pct = (worker.MaxCarryCapacity > 0) ? (load / worker.MaxCarryCapacity * 100f) : 0f;
                carryBar.Value = pct;
            });

            // ── Wear-Out Status ──
            var wearOutLabel = ForgeLabel.CreateRaw(
                $"{L(LocalizationKeys.WorkerWearOutStatus)}: {GetWearOutText(worker)}", ForgeLabelSize.Small)
                .Color(worker.LastWearOutReason != WearOutReason.None ? "status.error" : "text.secondary")
                .MarginTop(2).Build();
            content.Add(wearOutLabel);

            Observe(() => worker.LastWearOutReason, reason =>
            {
                wearOutLabel.Text = $"{L(LocalizationKeys.WorkerWearOutStatus)}: {GetWearOutText(worker)}";
                wearOutLabel.ColorKey = reason != WearOutReason.None ? "status.error" : "text.secondary";
            });

            // ── Traits ──
            var traitsHeaderLabel = ForgeLabel.CreateRaw(
                $"⚔ {L(LocalizationKeys.WorkerTraits)}", ForgeLabelSize.Normal)
                .Color("text.primary").MarginTop(4).Build();
            content.Add(traitsHeaderLabel);

            var traitsContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column).Build();
            content.Add(traitsContainer);

            RebuildTraits(traitsContainer, worker.Traits);

            Observe(() => ComputeStringListFingerprint(worker.Traits), _ =>
            {
                RebuildTraits(traitsContainer, worker.Traits);
            });

            // ── Abilities ──
            var abilitiesHeaderLabel = ForgeLabel.CreateRaw(
                $"✦ {L(LocalizationKeys.WorkerAbilities)}", ForgeLabelSize.Normal)
                .Color("text.primary").MarginTop(4).Build();
            content.Add(abilitiesHeaderLabel);

            var abilitiesContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column).Build();
            content.Add(abilitiesContainer);

            RebuildAbilities(abilitiesContainer, worker.Abilities);

            Observe(() => ComputeAbilitiesFingerprint(worker.Abilities), _ =>
            {
                RebuildAbilities(abilitiesContainer, worker.Abilities);
            });

            // ── Maintenance Button ──
            var maintenanceButton = ForgeButton.Create(LocalizationKeys.WorkerSendToMaintenance,
                () => OnSendToMaintenance?.Invoke(_inspectedWorkerId))
                .MarginTop(6).Build();
            bool canMaintain = worker.State == HeroState.WornOut || worker.State == HeroState.ReturningToMaintenance;
            maintenanceButton.Disabled = !canMaintain;
            content.Add(maintenanceButton);

            Observe(() => worker.State, state =>
            {
                bool canMaint = state == HeroState.WornOut || state == HeroState.ReturningToMaintenance;
                maintenanceButton.Disabled = !canMaint;
            });

            ContentContainer.Add(content);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private string GetWearOutText(HeroEntity worker)
        {
            return worker.LastWearOutReason != WearOutReason.None
                ? worker.LastWearOutReason.ToString()
                : L(LocalizationKeys.WorkerNone);
        }

        private void RebuildTraits(ForgeContainer container, List<string> traits)
        {
            container.Clear();
            if (traits.Count > 0)
            {
                foreach (var trait in traits)
                {
                    container.Add(
                        ForgeLabel.CreateRaw($"  • {trait}", ForgeLabelSize.Small)
                            .Color("text.secondary").Build());
                }
            }
            else
            {
                container.Add(
                    ForgeLabel.CreateRaw($"  {L(LocalizationKeys.WorkerNone)}", ForgeLabelSize.Small)
                        .Color("text.secondary").Build());
            }
        }

        private void RebuildAbilities(ForgeContainer container, List<WorkerAbility> abilities)
        {
            container.Clear();
            if (abilities.Count > 0)
            {
                foreach (var ability in abilities)
                {
                    container.Add(
                        ForgeLabel.CreateRaw($"  ★ {ability.DisplayName} (+{ability.BonusValue:F1})", ForgeLabelSize.Small)
                            .Color("status.warning")
                            .Tooltip($"Gained as: {ability.GainedAsProfession}")
                            .Build());
                }
            }
            else
            {
                container.Add(
                    ForgeLabel.CreateRaw($"  {L(LocalizationKeys.WorkerNone)}", ForgeLabelSize.Small)
                        .Color("text.secondary").Build());
            }
        }

        private static int ComputeStringListFingerprint(List<string> items)
        {
            unchecked
            {
                int hash = items.Count;
                for (int i = 0; i < items.Count; i++)
                {
                    hash ^= items[i].GetHashCode() * (i + 1);
                }
                return hash;
            }
        }

        private static int ComputeAbilitiesFingerprint(List<WorkerAbility> abilities)
        {
            unchecked
            {
                int hash = abilities.Count;
                for (int i = 0; i < abilities.Count; i++)
                {
                    hash ^= (abilities[i].Id.GetHashCode() * 397) ^ (int)(abilities[i].BonusValue * 100);
                }
                return hash;
            }
        }
    }
}
