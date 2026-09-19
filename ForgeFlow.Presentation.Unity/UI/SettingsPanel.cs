using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Settings panel. Audio, graphics, resolution, keybinds (with rebinding), UI style toggle.
    /// Reads/writes GameSettings from SettingsManager. Full-screen centered overlay.
    /// </summary>
    internal sealed class SettingsPanel : IUIWindow
    {
        public string WindowId => WindowIds.Settings;
        public event Action? OnBack;

        private readonly ForgeFullScreenOverlay _overlay;
        private readonly ForgeContainer _contentContainer;
        private readonly SettingsManager? _settings;
        private bool _isVisible;

        public VisualElement Root => _overlay;
        public bool IsVisible => _isVisible;

        public SettingsPanel(SettingsManager? settings)
        {
            _settings = settings;
            _overlay = new ForgeFullScreenOverlay(new Color(0f, 0f, 0f, 0.6f));
            _contentContainer = new ForgeContainer();

            _overlay.ContentArea.Add(ForgeLabel.Create(LocalizationKeys.SettingsTitle, ForgeLabelSize.Header)
                .FontSize(26).MarginBottom(12).Build());
            _overlay.ContentArea.Add(_contentContainer);
            ApplyTheme();
        }

        public void Show(object? context = null) { _isVisible = true; _overlay.Show(); }
        public void Hide() { _isVisible = false; _overlay.Hide(); }
        public void Dispose() { }

        public void ApplyTheme()
        {
            _overlay.ApplyTheme();
        }

        public void Refresh()
        {
            _contentContainer.Clear();
            if (_settings == null) { return; }

            var s = _settings.Settings;

            var scrollView = ForgeScrollView.Create(ScrollViewMode.Vertical)
                .MaxHeight(600).Width(500).Build();
            ApplyScrollCardTheme(scrollView);

            var inner = scrollView.Inner;

            // Audio section
            inner.Add(ForgeLabel.Create(LocalizationKeys.SettingsAudio, ForgeLabelSize.Header).MarginBottom(6).Build());
            inner.Add(ForgeSlider.Create(LocalizationKeys.SettingsMasterVolume, 0f, 1f, s.MasterVolume)
                .OnValueChanged(v => _settings.SetMasterVolume(v)).MarginBottom(4).Build());
            inner.Add(ForgeSlider.Create(LocalizationKeys.SettingsMusicVolume, 0f, 1f, s.MusicVolume)
                .OnValueChanged(v => s.MusicVolume = v).MarginBottom(4).Build());
            inner.Add(ForgeSlider.Create(LocalizationKeys.SettingsSfxVolume, 0f, 1f, s.SfxVolume)
                .OnValueChanged(v => s.SfxVolume = v).MarginBottom(8).Build());

            // Graphics section
            inner.Add(ForgeLabel.Create(LocalizationKeys.SettingsGraphics, ForgeLabelSize.Header).MarginBottom(6).Build());
            inner.Add(ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsResolution)}: {s.ResolutionWidth}x{s.ResolutionHeight}",
                ForgeLabelSize.Small).MarginBottom(2).Build());
            inner.Add(ForgeToggle.Create(LocalizationKeys.SettingsFullscreen, s.Fullscreen)
                .OnValueChanged(v => s.Fullscreen = v).MarginBottom(4).Build());
            inner.Add(ForgeToggle.Create(LocalizationKeys.SettingsVsync, s.VSync)
                .OnValueChanged(v => s.VSync = v).MarginBottom(4).Build());
            inner.Add(ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsQuality)}: {GetQualityName(s.GraphicsQuality)}",
                ForgeLabelSize.Small).MarginBottom(2).Build());

            // UI Style toggle
            inner.Add(ForgeLabel.Create(LocalizationKeys.SettingsUIStyle, ForgeLabelSize.Header).MarginBottom(6).Build());
            var styleChoices = new List<string>
            {
                ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.UIStyleFunWhimsical),
                ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.UIStyleMinimalist)
            };
            inner.Add(ForgeDropdown.Create(LocalizationKeys.SettingsUIStyle, styleChoices, (int)s.UIStyle)
                .OnSelectionChanged(v =>
                {
                    s.UIStyle = v == styleChoices[1]
                        ? UIStyleMode.MinimalistExpert
                        : UIStyleMode.FunWhimsical;
                }).MarginBottom(8).Build());

            // Language
            inner.Add(ForgeLabel.CreateRaw(
                $"{ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsLanguage)}: {s.Language}",
                ForgeLabelSize.Small).MarginBottom(2).Build());

            // Keybinds section
            var keybindsFold = ForgeFoldout.Create(LocalizationKeys.SettingsKeybinds, false);
            var keybindsContent = new ForgeContainer();
            foreach (var kvp in s.KeyBindings)
            {
                var row = ForgeContainer.Create()
                    .FlexDirection(FlexDirection.Row)
                    .JustifyContent(Justify.SpaceBetween)
                    .AlignItems(Align.Center)
                    .MarginBottom(4)
                    .Build();
                row.Add(ForgeLabel.CreateRaw(kvp.Key, ForgeLabelSize.Small)
                    .Color("text.secondary").FlexGrow(1).Build());
                row.Add(ForgeLabel.CreateRaw(kvp.Value, ForgeLabelSize.Small)
                    .Color("text.primary").Build());
                keybindsContent.Add(row);
            }
            inner.Add(keybindsFold.Content(keybindsContent).Build());

            // Reset defaults
            inner.Add(ForgeButton.Create(LocalizationKeys.SettingsResetDefaults, () =>
            {
                _settings.Apply(new GameSettings());
                Refresh();
            }).Secondary().Height(34).MarginTop(8).MarginBottom(4).Build());

            // Back button
            inner.Add(ForgeButton.Create(LocalizationKeys.SettingsBack, () => OnBack?.Invoke())
                .Primary().Height(38).MarginTop(8).Build());

            _contentContainer.Add(scrollView);
        }

        private static void ApplyScrollCardTheme(ForgeScrollView sv)
        {
            sv.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
            sv.style.paddingTop = 16;
            sv.style.paddingBottom = 16;
            sv.style.paddingLeft = 20;
            sv.style.paddingRight = 20;
            ForgeStyledVisualElement.SetBorderOn(sv, ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 8);
            sv.style.borderTopWidth = 2;
            sv.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
        }

        private static string GetQualityName(int quality) => quality switch
        {
            0 => ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsQualityLow),
            1 => ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsQualityMedium),
            2 => ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsQualityHigh),
            3 => ForgeStyledVisualElement.GetLocalizedText(LocalizationKeys.SettingsQualityUltra),
            _ => "Unknown"
        };
    }
}
