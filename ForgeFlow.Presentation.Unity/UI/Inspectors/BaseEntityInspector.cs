using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Mid-level base class for all entity inspectors. Extends <see cref="BaseInspectorPanel"/>
    /// with rich UI helpers built exclusively from Forge* components and the fluent builder API.
    /// Provides alternating-row property rows, section dividers, colored rows,
    /// small property rows, entity headers, structure display name/icon/category helpers,
    /// and CapitalizeFirst. Concrete entity inspectors (structure, villager, worker) inherit from this.
    /// </summary>
    internal abstract class BaseEntityInspector : BaseInspectorPanel
    {
        /// <summary>Tracks row index for alternating background colors.</summary>
        protected int PropertyRowIndex;

        protected BaseEntityInspector(
            string panelId,
            string titleLocKey,
            float width,
            float height,
            SimulationTicker? simulation)
            : base(panelId, titleLocKey, width, height, simulation)
        {
        }

        /// <summary>Resets the alternating row index. Call at the start of BuildContent.</summary>
        protected void ResetRowIndex()
        {
            PropertyRowIndex = 0;
        }

        // ── Alternating Row Support ─────────────────────────────────

        /// <summary>Returns the next alternating row background color key and advances the index.</summary>
        protected string NextRowColorKey()
        {
            return (PropertyRowIndex++ % 2 == 0) ? "bg.row.even" : "bg.row.odd";
        }

        /// <summary>Theme-aware border radius for alternating rows.</summary>
        protected int AlternatingRowRadius => ThemeIsMinimalist ? 0 : 2;

        // ── Rich Row Helpers ─────────────────────────────────────────

        /// <summary>Adds a localized key→value row with alternating background and themed colors.</summary>
        protected void AddRichPropertyRow(VisualElement container, string locKey, string value)
        {
            var keyLabel = ForgeLabel.CreateRaw(L(locKey), ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var valueLabel = ForgeLabel.CreateRaw(value, ForgeLabelSize.Normal)
                .Color("text.primary").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            container.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(keyLabel).Child(valueLabel)
                .Build());
        }

        /// <summary>Adds a row with a custom-colored value label.</summary>
        protected void AddColoredPropertyRow(VisualElement container, string label, string value, string colorKey)
        {
            var keyLabel = ForgeLabel.CreateRaw(label, ForgeLabelSize.Normal)
                .Color("text.secondary").FlexGrow(1).Build();
            var valueLabel = ForgeLabel.CreateRaw(value, ForgeLabelSize.Normal)
                .Color(colorKey).Bold().TextAlign(TextAnchor.MiddleRight).Build();
            container.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingTop(3).PaddingBottom(3)
                .PaddingLeft(ThemePaddingSmall).PaddingRight(ThemePaddingSmall)
                .BackgroundColor(NextRowColorKey())
                .BorderRadius(AlternatingRowRadius)
                .Child(keyLabel).Child(valueLabel)
                .Build());
        }

        /// <summary>Adds a compact indented row (used for sub-items like stored inputs, tool outputs).</summary>
        protected void AddSmallPropertyRow(VisualElement container, string label, string value)
        {
            var keyLabel = ForgeLabel.CreateRaw(label, ForgeLabelSize.Small)
                .Color("text.secondary").FlexGrow(1).Build();
            var valueLabel = ForgeLabel.CreateRaw(value, ForgeLabelSize.Small)
                .Color("text.accent").Bold().TextAlign(TextAnchor.MiddleRight).Build();
            container.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .JustifyContent(Justify.SpaceBetween)
                .PaddingLeft(ThemePaddingNormal + 4).PaddingRight(ThemePaddingSmall)
                .PaddingTop(2).PaddingBottom(2)
                .Child(keyLabel).Child(valueLabel)
                .Build());
        }

        // ── Section Helpers ──────────────────────────────────────────

        /// <summary>Adds a styled section header with accent bar and bold text.</summary>
        protected void AddRichSectionHeader(VisualElement container, string text)
        {
            int radius = ThemeIsMinimalist ? 1 : 4;
            var accentBar = ForgeContainer.Create()
                .Width(3)
                .BackgroundColor("accent.primary")
                .MarginRight(ThemePaddingSmall)
                .BorderRadius(2)
                .AlignSelf(Align.Stretch)
                .Build();
            var header = ForgeLabel.CreateRaw(text, ForgeLabelSize.Normal)
                .Color("accent.primary").Bold().Build();
            container.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .BackgroundColor("bg.header")
                .PaddingTop(ThemePaddingSmall).PaddingBottom(ThemePaddingSmall)
                .PaddingLeft(ThemePaddingNormal).PaddingRight(ThemePaddingNormal)
                .MarginTop(4).MarginBottom(4)
                .BorderRadius(radius)
                .Child(accentBar).Child(header)
                .Build());
        }

        /// <summary>Adds a thin horizontal divider line.</summary>
        protected void AddSectionDivider(VisualElement container)
        {
            container.Add(ForgeContainer.Create()
                .Height(1)
                .BackgroundColor("border.normal")
                .MarginTop(8).MarginBottom(2).MarginLeft(2).MarginRight(2)
                .Build());
        }

        // ── Entity Header Builder ────────────────────────────────────

        /// <summary>
        /// Builds a standard entity header with icon + name + optional badges + ID row.
        /// </summary>
        protected void BuildEntityHeader(
            VisualElement container,
            string icon,
            string displayName,
            string? categoryBadgeText,
            string? categoryBadgeColor,
            int tier,
            ulong entityId)
        {
            // Row 1: Icon + Name
            var iconLabel = ForgeLabel.CreateRaw(icon)
                .FontSize(ThemeFontHeader).MarginRight(6).Build();
            var nameLabel = ForgeLabel.CreateRaw(displayName, ForgeLabelSize.Large)
                .Color("text.accent").FlexGrow(1).Build();
            container.Add(ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .MarginBottom(2)
                .Child(iconLabel).Child(nameLabel)
                .Build());

            // Row 2: Category badge + Tier badge
            if (categoryBadgeText != null || tier > 0)
            {
                var badgeRowBuilder = ForgeContainer.Create()
                    .FlexDirection(FlexDirection.Row)
                    .AlignItems(Align.Center)
                    .MarginBottom(4);

                if (categoryBadgeText != null)
                {
                    badgeRowBuilder.Child(
                        ForgeStatusBadge.Create(categoryBadgeText, categoryBadgeColor ?? "accent.secondary")
                            .MarginRight(6).Build());
                }

                if (tier > 0)
                {
                    badgeRowBuilder.Child(
                        ForgeStatusBadge.Create($"Tier {tier}", "accent.secondary").Build());
                }

                container.Add(badgeRowBuilder.Build());
            }

            // Row 3: ID
            container.Add(
                ForgeLabel.CreateRaw($"#{entityId}", ForgeLabelSize.Small)
                    .Color("text.secondary").MarginBottom(2).Build());
        }

        // ── Warning / Status Helpers ─────────────────────────────────

        /// <summary>Adds a warning label (e.g. "⚠ Full — no slots").</summary>
        protected void AddWarningLabel(VisualElement container, string text)
        {
            container.Add(
                ForgeLabel.CreateRaw(text, ForgeLabelSize.Small)
                    .Color("status.error").Bold().MarginTop(2).Build());
        }

        /// <summary>Adds a bold sub-header label (e.g. "Tool Outputs:").</summary>
        protected void AddSubHeader(VisualElement container, string text)
        {
            container.Add(
                ForgeLabel.CreateRaw(text, ForgeLabelSize.Small)
                    .Color("text.secondary").Bold().MarginTop(6).Build());
        }

        // ── Structure Display Name / Icon / Category ───────────────────

        protected static string GetStructureDisplayName(string category) => category switch
        {
            "ForestryRecipeEntity" => "Forestry",
            "GatheringRecipeEntity" => "Gathering Spot",
            "MiningNode" => "Mining Node",
            "VillageSpawner" => "Village Spawner",
            "Spawner" => "Hero Spawner",
            "Inn" => "Inn",
            "CraftStation" => "Craft Station",
            "Stockpile" => "Stockpile",
            "Forge" => "Forge",
            "TrainingBuilding" => "Training Hall",
            "DungeonPortal" => "Dungeon Portal",
            "FusionAltar" => "Fusion Altar",
            "AppearanceWorkshop" => "Appearance Workshop",
            "ToolStation" => "Tool Station",
            "Armory" => "Armory",
            "JobChanger" => "Job Changer",
            "Academy" => "Academy",
            "CheckGate" => "Check Gate",
            "FilterSplitter" => "Filter Splitter",
            "Balancer" => "Balancer",
            "PathGate" => "Path Gate",
            _ => category
        };

        protected static string GetStructureIcon(string category) => category switch
        {
            "ForestryRecipeEntity" or "GatheringMachine" => "🌲",
            "MiningNodeRecipeEntity" => "⛏",
            "VillageSpawner" or "Spawner" => "🏠",
            "Inn" => "🍺",
            "CraftStation" or "Forge" => "🔨",
            "Stockpile" => "📦",
            "TrainingBuilding" or "Academy" => "📖",
            "DungeonPortal" => "🏰",
            "FusionAltar" => "✦",
            "ToolStation" or "Armory" => "🛡",
            _ => "⚙"
        };

        protected static string GetStructureCategory(string category) => category switch
        {
            "ForestryRecipeEntity" or "GatheringMachine" or "MiningNodeRecipeEntity"
                => "Gathering",
            "VillageSpawner" or "Spawner" => "Spawner",
            "Inn" => "Service",
            "CraftStation" or "Forge" => "Production",
            "Stockpile" => "Storage",
            "TrainingBuilding" or "Academy" => "Training",
            "DungeonPortal" => "Dungeon",
            "FusionAltar" or "AppearanceWorkshop" => "Special",
            "ToolStation" or "Armory" or "JobChanger" => "Service",
            "CheckGate" or "FilterSplitter" or "Balancer"
                or "PathGate" => "Automation",
            _ => "Structure"
        };

        protected static string GetStructureCategoryColor(string category) => category switch
        {
            "Forestry" or "GatheringMachine" or "MiningNode"
                => "status.success",
            "VillageSpawner" or "Spawner" => "accent.primary",
            "CraftStation" or "Forge" => "status.warning",
            "DungeonPortal" => "status.error",
            "TrainingBuilding" or "Academy" => "accent.secondary",
            _ => "accent.secondary"
        };

        protected static string CapitalizeFirst(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        // ── Theme-Aware Accessors ────────────────────────────────────

        protected static Color C(string key) => ForgeStyledVisualElement.GetThemeColor(key);
        protected static string L(string key) => ForgeStyledVisualElement.GetLocalizedText(key);
        protected static bool ThemeIsMinimalist => ForgeStyledVisualElement.IsMinimalistMode;

        protected static int ThemePaddingSmall => ForgeStyledVisualElement.ThemePaddingSmallStatic;
        protected static int ThemePaddingNormal => ForgeStyledVisualElement.ThemePaddingNormalStatic;
        protected static int ThemeFontSmall => ForgeStyledVisualElement.ThemeFontSmallStatic;
        protected static int ThemeFontNormal => ForgeStyledVisualElement.ThemeFontNormalStatic;
        protected static int ThemeFontLarge => ForgeStyledVisualElement.ThemeFontLargeStatic;
        protected static int ThemeFontHeader => ForgeStyledVisualElement.ThemeFontHeaderStatic;
        protected static int ThemeBorderWidth => ForgeStyledVisualElement.ThemeBorderWidthStatic;
    }
}
