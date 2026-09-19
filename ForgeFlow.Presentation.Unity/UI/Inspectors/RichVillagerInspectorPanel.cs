using System;
using System.Collections.Generic;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto.Logic;
using ForgeFlow.Core.Proto.Prototypes;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Inspector panel for VillagerLogic entities. Shows whimsical styled header
    /// with name+level badge, state/class badges, stamina/tool durability bars,
    /// equipment, inventory grid, traits, and current task.
    ///
    /// Uses the observer pattern: UI is built once, all dynamic data bound
    /// via <see cref="BaseInspectorPanel.Observe{T}"/>.
    /// </summary>
    internal sealed class RichVillagerInspectorPanel : BaseEntityInspector, ITickableWindow, IAutoRegisteredInspector
    {
        public override string WindowId => WindowIds.VillagerInspector;

        private ulong _inspectedVillagerId;

        public RichVillagerInspectorPanel(SimulationTicker? simulation)
            : base("villager_inspector", LocalizationKeys.VillagerTitle, 320f, 500f, simulation)
        {
        }

        public void InspectVillager(ulong villagerId)
        {
            _inspectedVillagerId = villagerId;
            Refresh();
        }

        public void Tick(float deltaTime)
        {
            UpdateBindings();
        }

        protected override void BuildContent()
        {
            ResetRowIndex();

            var villager = Simulation?.VillagerSystem.VillagerIndex.TryGetValue(_inspectedVillagerId, out var v) == true ? v : null;
            if (villager == null)
            {
                return;
            }

            var content = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();

            // ── Header: Name + Level Badge ──
            string displayName = string.IsNullOrEmpty(villager.Name)
                ? $"Villager #{villager.Id}"
                : villager.Name;

            var iconLabel = ForgeLabel.CreateRaw("🧑")
                .FontSize(ThemeFontHeader).MarginRight(6).Build();
            var nameLabel = ForgeLabel.CreateRaw(displayName, ForgeLabelSize.Large)
                .Color("text.accent").FlexGrow(1).Build();
            var levelBadge = ForgeStatusBadge.Create($"Lv.{villager.Level}", "accent.secondary").Build();

            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .MarginBottom(2)
                .Child(iconLabel).Child(nameLabel).Child(levelBadge)
                .Build());

            Observe(() => villager.Level, level =>
            {
                levelBadge.Text = $"Lv.{level}";
            });

            // ── Badges: State + Class ──
            var stateBadge = ForgeStatusBadge.Create(villager.State.ToString(), GetStateColor(villager.State))
                .MarginRight(6).Build();

            var classBadge = ForgeStatusBadge.Create(villager.TrainedClass.ToString(), "accent.secondary").Build();
            classBadge.style.display = villager.TrainedClass != VillagerClass.Untrained
                ? DisplayStyle.Flex : DisplayStyle.None;

            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .MarginBottom(4)
                .Child(stateBadge).Child(classBadge)
                .Build());

            Observe(() => villager.State, state =>
            {
                stateBadge.Text = state.ToString();
                stateBadge.ColorKey = GetStateColor(state);
            });

            Observe(() => villager.TrainedClass, cls =>
            {
                classBadge.Text = cls.ToString();
                classBadge.style.display = cls != VillagerClass.Untrained
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });

            // ── Basic Info ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.InspectorSectionStatus));

            var jobValueLabel = ForgeLabel.CreateRaw(GetProfessionDisplay(villager.Profession), ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var jobKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.VillagerProfession), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(jobKeyLabel).Child(jobValueLabel).Build());

            var workRateValueLabel = ForgeLabel.CreateRaw($"{villager.EffectiveWorkRate:F2}", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var workRateKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.VillagerWorkRate), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(workRateKeyLabel).Child(workRateValueLabel).Build());

            var posValueLabel = ForgeLabel.CreateRaw(
                $"({villager.Position.X}, {villager.Position.Y})", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var posKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.InspectorPosition), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(posKeyLabel).Child(posValueLabel).Build());

            Observe(() => villager.Profession, job => { jobValueLabel.Text = GetProfessionDisplay(job); });
            Observe(() => villager.EffectiveWorkRate, rate => { workRateValueLabel.Text = $"{rate:F2}"; });
            Observe(() => villager.Position, pos => { posValueLabel.Text = $"({pos.X}, {pos.Y})"; });

            // ── Stamina Bar ──
            var staminaBar = ForgeProgressBar.Create(
                LocalizationKeys.VillagerStamina,
                0f, villager.MaxStamina)
                .Value(villager.Stamina)
                .Color(GetStaminaColorKey(villager.Stamina, villager.MaxStamina))
                .MarginTop(4).MarginBottom(4)
                .Build();
            content.Add(staminaBar);

            Observe(() => villager.Stamina, stamina =>
            {
                staminaBar.Value = stamina;
                staminaBar.ColorKey = GetStaminaColorKey(stamina, villager.MaxStamina);
            });

            // ── Tool Durability Bar (pre-created, visibility toggled) ──
            var toolDurabilityBar = ForgeProgressBar.Create(
                LocalizationKeys.VillagerToolDurability,
                0f, villager.EquippedToolMaxDurability)
                .Value(villager.EquippedToolDurability)
                .Color(GetStaminaColorKey(villager.EquippedToolDurability, villager.EquippedToolMaxDurability))
                .MarginTop(2).MarginBottom(4)
                .Build();
            toolDurabilityBar.style.display = villager.EquippedToolId != null
                ? DisplayStyle.Flex : DisplayStyle.None;
            content.Add(toolDurabilityBar);

            var toolKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.InspectorTool), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var toolValueLabel = ForgeLabel.CreateRaw(
                villager.EquippedToolId ?? "none", ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            var toolRow = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(toolKeyLabel).Child(toolValueLabel).Build();
            toolRow.style.display = villager.EquippedToolId != null
                ? DisplayStyle.Flex : DisplayStyle.None;
            content.Add(toolRow);

            Observe(() => villager.EquippedToolId, toolId =>
            {
                bool hasTool = toolId != null;
                toolDurabilityBar.style.display = hasTool ? DisplayStyle.Flex : DisplayStyle.None;
                toolRow.style.display = hasTool ? DisplayStyle.Flex : DisplayStyle.None;
                toolValueLabel.Text = toolId ?? "none";
            });

            Observe(() => villager.EquippedToolDurability, durability =>
            {
                toolDurabilityBar.Value = durability;
                toolDurabilityBar.ColorKey = GetStaminaColorKey(durability, villager.EquippedToolMaxDurability);
            });

            // ── Equipment: Weapon + Armor (pre-created, visibility toggled) ──
            var equipDivider = ForgeContainer.Create()
                .Height(1).BackgroundColor("border.normal")
                .MarginTop(8).MarginBottom(2).MarginLeft(2).MarginRight(2)
                .Build();
            content.Add(equipDivider);
            var equipHeader = ForgeContainer.Create().Build();
            AddRichSectionHeader(equipHeader, "▸ ⚔ " + L(LocalizationKeys.InspectorSectionEquipment));
            content.Add(equipHeader);

            var weaponKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.VillagerEquippedWeapon), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var weaponValueLabel = ForgeLabel.CreateRaw(
                villager.EquippedWeaponId ?? L(LocalizationKeys.WorkerNone), ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(weaponKeyLabel).Child(weaponValueLabel).Build());

            var armorKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.VillagerEquippedArmor), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var armorValueLabel = ForgeLabel.CreateRaw(
                villager.EquippedArmorId ?? L(LocalizationKeys.WorkerNone), ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(armorKeyLabel).Child(armorValueLabel).Build());

            bool hasEquipment = villager.EquippedWeaponId != null || villager.EquippedArmorId != null;
            ApplyEquipmentVisibility(hasEquipment, equipDivider, equipHeader);

            Observe(() => villager.EquippedWeaponId, id =>
            {
                weaponValueLabel.Text = id ?? L(LocalizationKeys.WorkerNone);
                ApplyEquipmentVisibility(
                    id != null || villager.EquippedArmorId != null,
                    equipDivider, equipHeader);
            });

            Observe(() => villager.EquippedArmorId, id =>
            {
                armorValueLabel.Text = id ?? L(LocalizationKeys.WorkerNone);
                ApplyEquipmentVisibility(
                    villager.EquippedWeaponId != null || id != null,
                    equipDivider, equipHeader);
            });

            // ── Inventory Grid ──
            AddSectionDivider(content);
            AddRichSectionHeader(content, "▸ " + L(LocalizationKeys.VillagerInventorySection));

            var inventoryGrid = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .FlexWrap(Wrap.Wrap)
                .MarginTop(4).MarginBottom(4)
                .Build();
            content.Add(inventoryGrid);

            RebuildInventoryGrid(inventoryGrid, villager);

            Observe(() => ComputeInventoryFingerprint(villager), _ =>
            {
                RebuildInventoryGrid(inventoryGrid, villager);
            });

            // ── Traits ──
            var traitsDivider = ForgeContainer.Create()
                .Height(1).BackgroundColor("border.normal")
                .MarginTop(8).MarginBottom(2).MarginLeft(2).MarginRight(2)
                .Build();
            content.Add(traitsDivider);
            var traitsHeader = ForgeContainer.Create().Build();
            AddRichSectionHeader(traitsHeader, "▸ " + L(LocalizationKeys.VillagerTraits));
            content.Add(traitsHeader);

            var traitsContainer = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Column)
                .Build();
            content.Add(traitsContainer);

            ApplyTraitsVisibility(villager.Traits.Count > 0, traitsDivider, traitsHeader);
            RebuildTraits(traitsContainer, villager.Traits);

            Observe(() => ComputeTraitsFingerprint(villager.Traits), _ =>
            {
                ApplyTraitsVisibility(villager.Traits.Count > 0, traitsDivider, traitsHeader);
                RebuildTraits(traitsContainer, villager.Traits);
            });

            // ── Current Task ──
            AddSectionDivider(content);
            var taskKeyLabel = ForgeLabel.CreateRaw(L(LocalizationKeys.VillagerCurrentTask), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var taskValueLabel = ForgeLabel.CreateRaw(
                GetTaskText(villager), ForgeLabelSize.Normal)
                .Color(villager.State == VillagerState.Working ? "status.success" : "text.secondary")
                .Bold().TextAlign(TextAnchor.MiddleRight).Build();
            content.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row).JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey()).BorderRadius(AlternatingRowRadius)
                .Child(taskKeyLabel).Child(taskValueLabel).Build());

            Observe(() => villager.State, state =>
            {
                taskValueLabel.Text = GetTaskText(villager);
                taskValueLabel.ColorKey = state == VillagerState.Working ? "status.success" : "text.secondary";
            });

            Observe(() => villager.CurrentActivity, _ =>
            {
                taskValueLabel.Text = GetTaskText(villager);
            });

            ContentContainer.Add(content);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static string GetStateColor(VillagerState state) => state switch
        {
            VillagerState.Working => "status.success",
            VillagerState.Resting => "status.warning",
            VillagerState.Injured or VillagerState.Dead => "status.error",
            _ => "accent.primary"
        };

        private static string GetStaminaColorKey(float current, float max)
        {
            float pct = max > 0f ? current / max : 0f;
            return pct > 0.5f ? "status.success" : pct > 0.25f ? "status.warning" : "status.error";
        }

        private static string GetTaskText(VillagerLogic villager) => villager.State switch
        {
            VillagerState.Working => villager.CurrentActivity ?? $"Working as {GetProfessionDisplay(villager.Profession)}",
            VillagerState.Resting => "Resting...",
            VillagerState.Travelling => "Travelling on path",
            VillagerState.Training => $"Training → {villager.TrainedClass}",
            VillagerState.EnteringBuilding => "Entering building",
            VillagerState.LeavingBuilding => "Leaving building",
            VillagerState.InDungeon => "In dungeon",
            VillagerState.Injured => "Injured",
            VillagerState.Dead => "Dead",
            _ => "Idle"
        };

        private static string GetProfessionDisplay(VillagerJob job) =>
            job == VillagerJob.Idle ? "Villager" : job.ToString();

        private static void ApplyEquipmentVisibility(bool visible, params VisualElement[] elements)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].style.display = display;
            }
        }

        private static void ApplyTraitsVisibility(bool hasTraits, VisualElement divider, VisualElement header)
        {
            var display = hasTraits ? DisplayStyle.Flex : DisplayStyle.None;
            divider.style.display = display;
            header.style.display = display;
        }

        private void RebuildInventoryGrid(ForgeContainer grid, VillagerLogic villager)
        {
            grid.Clear();
            for (int i = 0; i < villager.MaxInventorySlots; i++)
            {
                var slotRadius = ThemeIsMinimalist ? 2 : 6;
                var slotBuilder = ForgeContainer.Create()
                    .Width(48).Height(48)
                    .MarginRight(4).MarginBottom(4)
                    .BackgroundColor("bg.primary")
                    .BorderRadius(slotRadius)
                    .BorderColor("border.normal");

                if (i < villager.Inventory.Count)
                {
                    var item = villager.Inventory[i];
                    slotBuilder.Child(
                        ForgeLabel.CreateRaw("📦")
                            .FontSize(20).TextAlign(TextAnchor.MiddleCenter).FlexGrow(1)
                            .Build());

                    if (item.Quantity > 1)
                    {
                        slotBuilder.Child(
                            ForgeLabel.CreateRaw($"×{item.Quantity}", ForgeLabelSize.Small)
                                .Color("text.primary").Bold()
                                .TextAlign(TextAnchor.LowerRight)
                                .Position(Position.Absolute).Bottom(2).Right(4)
                                .Build());
                    }

                    slotBuilder.Tooltip($"{CapitalizeFirst(item.ProtoId)} ×{item.Quantity}");
                }

                grid.Add(slotBuilder.Build());
            }
        }

        private static void RebuildTraits(ForgeContainer container, List<string> traits)
        {
            container.Clear();
            foreach (var trait in traits)
            {
                container.Add(
                    ForgeLabel.CreateRaw($"  • {trait}", ForgeLabelSize.Small)
                        .Color("text.secondary").Build());
            }
        }

        private static int ComputeInventoryFingerprint(VillagerLogic villager)
        {
            unchecked
            {
                int hash = villager.Inventory.Count ^ (villager.MaxInventorySlots * 397);
                for (int i = 0; i < villager.Inventory.Count; i++)
                {
                    var item = villager.Inventory[i];
                    hash ^= (item.ProtoId.GetHashCode() * 31) ^ (item.Quantity * 17);
                }
                return hash;
            }
        }

        private static int ComputeTraitsFingerprint(List<string> traits)
        {
            unchecked
            {
                int hash = traits.Count;
                for (int i = 0; i < traits.Count; i++)
                {
                    hash ^= traits[i].GetHashCode() * (i + 1);
                }
                return hash;
            }
        }
    }
}
