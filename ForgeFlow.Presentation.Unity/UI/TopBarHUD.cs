using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Top bar HUD dashboard that composes <see cref="GlobalResourcesPanel"/> (left, right-aligned),
    /// <see cref="GuildIdentityPanel"/> (center, prominent guild name with logo/banner),
    /// and <see cref="EntityStatusPanel"/> (right, left-aligned). Positioned as a full-width strip
    /// at the top of the screen with clear vertical dividers between sections.
    /// </summary>
    internal sealed class TopBarHUD : ITickableWindow
    {
        public string WindowId => WindowIds.TopBarHUD;

        private readonly ForgeContainer _root;
        private readonly GlobalResourcesPanel _resourcesPanel;
        private readonly GuildIdentityPanel _guildIdentityPanel;
        private readonly EntityStatusPanel _entityStatusPanel;
        private readonly ForgeContainer _leftDivider;
        private readonly ForgeContainer _rightDivider;
        private bool _isVisible;
        private int _frameCounter;

        public VisualElement Root => _root;
        public bool IsVisible => _isVisible;

        public TopBarHUD(SimulationTicker simulation, EventBus eventBus)
        {
            _resourcesPanel = new GlobalResourcesPanel(simulation, eventBus);
            _guildIdentityPanel = new GuildIdentityPanel(simulation, eventBus);
            _entityStatusPanel = new EntityStatusPanel(simulation);

            _leftDivider = CreateVerticalDivider();
            _rightDivider = CreateVerticalDivider();

            _root = ForgeContainer.Create()
                .Name(nameof(TopBarHUD))
                .Position(Position.Absolute)
                .Top(0).Left(0).Right(0)
                .FlexDirection(FlexDirection.Row)
                .AlignItems(Align.Center)
                .PaddingTop(8).PaddingBottom(8)
                .PaddingLeft(16).PaddingRight(16)
                .Build();
            _root.style.height = StyleKeyword.Auto;

            // Consume pointer events so clicks on the top bar don't pass through
            _root.pickingMode = PickingMode.Ignore;

            // Left section: resources right-aligned towards center (flexGrow=1)
            _root.Add(_resourcesPanel);
            _root.Add(_leftDivider);

            // Center section: guild identity (fixed size, prominent)
            _root.Add(_guildIdentityPanel);
            _root.Add(_rightDivider);

            // Right section: entity status left-aligned towards center (flexGrow=1)
            _root.Add(_entityStatusPanel);

            ApplyTheme();
        }

        public void Show(object? context = null)
        {
            _isVisible = true;
            _root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            _isVisible = false;
            _root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            _resourcesPanel.Refresh();
            _guildIdentityPanel.Refresh();
            _entityStatusPanel.Refresh();
        }

        public void Tick(float deltaTime)
        {
            _frameCounter++;
            if (_frameCounter % 30 == 0)
            {
                Refresh();
            }
        }

        public void ApplyTheme()
        {
            _root.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.primary");
            ForgeStyledVisualElement.SetBorderOn(_root,
                ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 0);
            _root.style.borderTopWidth = 0;
            _root.style.borderBottomWidth = 2;
            _root.style.borderBottomColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");

            var dividerColor = ForgeStyledVisualElement.GetThemeColor("border.normal");
            ApplyDividerTheme(_leftDivider, dividerColor);
            ApplyDividerTheme(_rightDivider, dividerColor);

            _resourcesPanel.ApplyTheme();
            _guildIdentityPanel.ApplyTheme();
            _entityStatusPanel.ApplyTheme();
        }

        public void Dispose()
        {
            _resourcesPanel.Dispose();
            _guildIdentityPanel.Dispose();
        }

        private static ForgeContainer CreateVerticalDivider()
        {
            return ForgeContainer.Create()
                .Name("TopBarDivider")
                .Width(1)
                .AlignSelf(Align.Stretch)
                .MarginLeft(12).MarginRight(12)
                .MarginTop(2).MarginBottom(2)
                .Build();
        }

        private static void ApplyDividerTheme(ForgeContainer divider, Color color)
        {
            divider.style.backgroundColor = color;
        }
    }
}
