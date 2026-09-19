using ForgeFlow.Core;
using ForgeFlow.Core.Data;
using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Localization;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Save;
using ForgeFlow.Presentation.Unity.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI
{

/// <summary>
/// New game setup panel — Factorio-style world gen configuration.
/// Features: game name, difficulty buttons, seed, biome density,
/// starting resources multiplier, starting hero count, map preview.
/// Built entirely at runtime via UI Toolkit.
/// Composes <see cref="ForgeFullScreenOverlay"/> for the overlay root.
/// </summary>
internal sealed class NewGameSetupPanel : IUIWindow
{
    public string WindowId => WindowIds.NewGameSetup;
    public event Action<NewGameSettings>? OnStartGame;
    public event Action? OnBack;

    private readonly ForgeFullScreenOverlay _overlay;
    private ForgeScrollView _cardScrollView = null!;
    private ForgeTextField _gameNameField = null!;
    private ForgeTextField _seedField = null!;
    private ForgeSlider _biomeDensitySlider = null!;
    private ForgeSlider _resourceMultiplierSlider = null!;
    private ForgeSlider _heroCountSlider = null!;
    private ForgeContainer _mapPreviewContainer = null!;
    private ForgeContainer _difficultyContainer = null!;
    private Difficulty _selectedDifficulty = Difficulty.Normal;

    private ForgeTextField _guildNameField = null!;
    private int _selectedBannerIndex;
    private int _selectedLogoIndex;

    private ForgeButton? _casualBtn;
    private ForgeButton? _easyBtn;
    private ForgeButton? _normalBtn;

    private bool _isVisible;

    public VisualElement Root => _overlay;
    public bool IsVisible => _isVisible;

    public NewGameSetupPanel()
    {
        _overlay = new ForgeFullScreenOverlay("bg.primary");
        BuildUI();
        ApplyTheme();
    }

    public void Show(object? context = null)
    {
        _isVisible = true;
        _overlay.Show();
    }

    public void Hide()
    {
        _isVisible = false;
        _overlay.Hide();
    }

    public void Refresh() { }

    public void Dispose() { }

    public void ApplyTheme()
    {
        _overlay.ApplyTheme();
        ApplyCardScrollViewTheme();
        ApplyMapPreviewTheme();
    }

    private void BuildUI()
    {
        _gameNameField = ForgeTextField.Create(LocalizationKeys.NewGameName, "Factory #1")
            .Build();

        _seedField = ForgeTextField.Create(LocalizationKeys.NewGameSeed, Environment.TickCount.ToString())
            .FlexGrow(1)
            .Build();

        _biomeDensitySlider = ForgeSlider.Create(LocalizationKeys.NewGameBiomeDensity, 0.5f, 2.0f, 1.0f)
            .Build();

        _resourceMultiplierSlider = ForgeSlider.Create(LocalizationKeys.NewGameStartingResources, 0.5f, 3.0f, 1.0f)
            .Build();

        _heroCountSlider = ForgeSlider.Create(LocalizationKeys.NewGameStartingHeroes, 1f, 5f, 2f)
            .Build();

        // Difficulty button row
        _difficultyContainer = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Stretch)
            .Build();

        // Map preview container
        _mapPreviewContainer = ForgeContainer.Create()
            .Name("MapPreview")
            .Width(256).Height(256)
            .Build();
        ForgeStyledVisualElement.SetBorderOn(_mapPreviewContainer,
            ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 4);

        // ── Card content (scrollable inner container) ──
        var cardContent = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Column)
            .AlignItems(Align.Stretch)
            .FlexShrink(0)
            .PaddingTop(24).PaddingBottom(24)
            .PaddingLeft(32).PaddingRight(32)
            .Build();

        // Title
        cardContent.Add(ForgeLabel.Create(LocalizationKeys.NewGameTitle, ForgeLabelSize.Header)
            .FontSize(28).Build());

        // Game Name
        AddSpaced(cardContent, _gameNameField, 22);

        // ── Guild Creation Section ──
        var guildSection = CreateSection();

        guildSection.Add(ForgeLabel.Create(LocalizationKeys.NewGameGuildSection, ForgeLabelSize.Large)
            .Color("text.accent").Build());

        _guildNameField = ForgeTextField.Create(LocalizationKeys.NewGameGuildName, "Unnamed Guild")
            .Build();
        AddSpaced(guildSection, _guildNameField, 12);

        // Banner picker
        AddSpaced(guildSection, ForgeLabel.Create(LocalizationKeys.NewGameGuildBanner, ForgeLabelSize.Small)
            .Color("text.secondary").Build(), 12);

        var bannerRow = CreateWrapRow();
        for (int i = 0; i < GuildData.AvailableBanners.Length; i++)
        {
            int idx = i;
            var bannerId = GuildData.AvailableBanners[i];
            var btn = ForgeButton.Create(LocalizationKeys.NewGameGuildBanner, () =>
            {
                _selectedBannerIndex = idx;
                UpdateBannerSelection(bannerRow);
            }).Text(BannerIdToEmoji(bannerId)).FontSize(18).Width(40).Height(36).Build();
            AddWrapChild(bannerRow, btn, 6, 6);
        }
        AddSpaced(guildSection, bannerRow, 12);
        UpdateBannerSelection(bannerRow);

        // Logo picker
        AddSpaced(guildSection, ForgeLabel.Create(LocalizationKeys.NewGameGuildLogo, ForgeLabelSize.Small)
            .Color("text.secondary").Build(), 12);

        var logoRow = CreateWrapRow();
        for (int i = 0; i < GuildData.AvailableLogos.Length; i++)
        {
            int idx = i;
            var logoId = GuildData.AvailableLogos[i];
            var btn = ForgeButton.Create(LocalizationKeys.NewGameGuildLogo, () =>
            {
                _selectedLogoIndex = idx;
                UpdateLogoSelection(logoRow);
            }).Text(LogoIdToEmoji(logoId)).FontSize(18).Width(40).Height(36).Build();
            AddWrapChild(logoRow, btn, 6, 6);
        }
        AddSpaced(guildSection, logoRow, 12);
        UpdateLogoSelection(logoRow);

        AddSpaced(cardContent, guildSection, 22);

        // ── Difficulty Section ──
        var difficultySection = CreateSection();

        difficultySection.Add(ForgeLabel.Create(LocalizationKeys.NewGameDifficulty, ForgeLabelSize.Normal)
            .Color("text.accent").Build());

        _casualBtn = CreateDifficultyButton(LocalizationKeys.NewGameCasual, Difficulty.Casual);
        _easyBtn = CreateDifficultyButton(LocalizationKeys.NewGameEasy, Difficulty.Easy);
        _normalBtn = CreateDifficultyButton(LocalizationKeys.NewGameNormal, Difficulty.Normal);
        _difficultyContainer.Add(_casualBtn);
        AddRowChild(_difficultyContainer, _easyBtn, 8);
        AddRowChild(_difficultyContainer, _normalBtn, 8);
        AddSpaced(difficultySection, _difficultyContainer, 12);

        AddSpaced(cardContent, difficultySection, 22);
        UpdateDifficultyButtons();

        // ── World Gen Section ──
        var worldSection = CreateSection();

        worldSection.Add(ForgeLabel.Create(LocalizationKeys.NewGameWorldGen, ForgeLabelSize.Large)
            .Color("text.accent").Build());

        // Seed + Random button
        var seedRow = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Center)
            .Build();
        seedRow.Add(_seedField);
        AddRowChild(seedRow, ForgeButton.Create(LocalizationKeys.NewGameRandomSeed, () =>
        {
            _seedField.Value = Environment.TickCount.ToString();
            RefreshMapPreview();
        }).Width(100).Build(), 8);
        AddSpaced(worldSection, seedRow, 14);

        AddSpaced(worldSection, _biomeDensitySlider, 14);
        AddSpaced(worldSection, _resourceMultiplierSlider, 14);
        AddSpaced(worldSection, _heroCountSlider, 14);

        // Map Preview
        AddSpaced(worldSection, ForgeLabel.Create(LocalizationKeys.NewGameMapPreview, ForgeLabelSize.Normal)
            .Color("text.accent").Build(), 14);
        AddSpaced(worldSection, _mapPreviewContainer, 14);

        AddSpaced(cardContent, worldSection, 22);
        RefreshMapPreview();

        // Register seed field change to refresh preview
        _seedField.TextChanged += _ => RefreshMapPreview();

        // ── Action Buttons ──
        var btnRow = ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.Center)
            .Build();
        btnRow.Add(ForgeButton.Create(LocalizationKeys.NewGameStart, () =>
        {
            int.TryParse(_seedField.Value, out int seed);
            var settings = new NewGameSettings
            {
                GameName = _gameNameField.Value,
                Difficulty = _selectedDifficulty,
                Seed = seed,
                BiomeDensity = _biomeDensitySlider.Value,
                StartingResourcesMultiplier = _resourceMultiplierSlider.Value,
                StartingHeroCount = (int)_heroCountSlider.Value,
                GuildName = _guildNameField.Value,
                BannerId = GuildData.AvailableBanners[_selectedBannerIndex],
                LogoId = GuildData.AvailableLogos[_selectedLogoIndex]
            };
            OnStartGame?.Invoke(settings);
        }).Primary().FlexGrow(1).Height(40).BorderWidth(2).Build());

        AddRowChild(btnRow, ForgeButton.Create(LocalizationKeys.NewGameBack, () => OnBack?.Invoke())
            .Width(120).Build(), 8);

        AddSpaced(cardContent, btnRow, 22);

        // ── Scrollable card ──
        _cardScrollView = ForgeScrollView.Create()
            .Width(500)
            .Content(cardContent)
            .Build();

        _overlay.ContentArea.Add(_cardScrollView);
    }

    private ForgeButton CreateDifficultyButton(string locKey, Difficulty difficulty)
    {
        return ForgeButton.Create(locKey, () =>
        {
            _selectedDifficulty = difficulty;
            UpdateDifficultyButtons();
        }).FlexGrow(1).Height(36).Build();
    }

    private void UpdateDifficultyButtons()
    {
        SetDifficultyHighlight(_casualBtn, _selectedDifficulty == Difficulty.Casual);
        SetDifficultyHighlight(_easyBtn, _selectedDifficulty == Difficulty.Easy);
        SetDifficultyHighlight(_normalBtn, _selectedDifficulty == Difficulty.Normal);
    }

    private static void SetDifficultyHighlight(ForgeButton? btn, bool selected)
    {
        if (btn == null)
        {
            return;
        }

        btn.Variant = selected ? ButtonVariant.Primary : ButtonVariant.Default;
    }

    private void RefreshMapPreview()
    {
        _mapPreviewContainer.Clear();

        int.TryParse(_seedField.Value, out int seed);
        var preview = GameBootstrapper.GeneratePreviewTerrain(seed);
        int cellSize = 8;

        for (int y = 0; y < preview.Height && y < 32; y++)
        {
            var row = ForgeContainer.Create()
                .FlexDirection(FlexDirection.Row)
                .Height(cellSize)
                .Build();

            for (int x = 0; x < preview.Width && x < 32; x++)
            {
                var cell = preview.Get(new GridPosRPG(x, y));
                var cellVe = ForgeContainer.Create()
                    .Width(cellSize).Height(cellSize)
                    .Build();
                cellVe.style.backgroundColor = BiomeToColor(cell?.Biome ?? BiomeType.Plains);
                row.Add(cellVe);
            }

            _mapPreviewContainer.Add(row);
        }
    }

    private static Color BiomeToColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Plains => new Color(0.5f, 0.7f, 0.3f),
            BiomeType.Forest => new Color(0.2f, 0.5f, 0.15f),
            BiomeType.Mountain => new Color(0.5f, 0.5f, 0.5f),
            BiomeType.Hills => new Color(0.55f, 0.55f, 0.4f),
            BiomeType.Swamp => new Color(0.3f, 0.35f, 0.2f),
            BiomeType.Desert => new Color(0.8f, 0.75f, 0.45f),
            BiomeType.Tundra => new Color(0.7f, 0.8f, 0.85f),
            BiomeType.Volcanic => new Color(0.5f, 0.2f, 0.15f),
            _ => new Color(0.4f, 0.4f, 0.4f)
        };
    }

    private void UpdateBannerSelection(VisualElement bannerRow)
    {
        for (int i = 0; i < bannerRow.childCount; i++)
        {
            if (bannerRow[i] is ForgeButton btn)
            {
                btn.Variant = i == _selectedBannerIndex ? ButtonVariant.Primary : ButtonVariant.Default;
            }
        }
    }

    private void UpdateLogoSelection(VisualElement logoRow)
    {
        for (int i = 0; i < logoRow.childCount; i++)
        {
            if (logoRow[i] is ForgeButton btn)
            {
                btn.Variant = i == _selectedLogoIndex ? ButtonVariant.Primary : ButtonVariant.Default;
            }
        }
    }

    private static string BannerIdToEmoji(string bannerId) => bannerId switch
    {
        "banner_lion" => "\ud83e\udd81",
        "banner_eagle" => "\ud83e\udd85",
        "banner_dragon" => "\ud83d\udc09",
        "banner_wolf" => "\ud83d\udc3a",
        "banner_phoenix" => "\ud83d\udd25",
        "banner_serpent" => "\ud83d\udc0d",
        "banner_stag" => "\ud83e\udd8c",
        _ => "\ud83c\udff4"
    };

    private static string LogoIdToEmoji(string logoId) => logoId switch
    {
        "logo_shield" => "\ud83d\udee1",
        "logo_crown" => "\ud83d\udc51",
        "logo_hammer" => "\ud83d\udd28",
        "logo_star" => "\u2b50",
        "logo_anvil" => "\u2692",
        "logo_tome" => "\ud83d\udcd6",
        "logo_chalice" => "\ud83c\udfc6",
        _ => "\u2694"
    };

    // ── Theme application ──

    private void ApplyCardScrollViewTheme()
    {
        _cardScrollView.style.maxHeight = new Length(85, LengthUnit.Percent);
        _cardScrollView.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.panel");
        ForgeStyledVisualElement.SetBorderOn(_cardScrollView,
            ForgeStyledVisualElement.GetThemeColor("border.normal"), 1, 10);
        _cardScrollView.style.borderTopWidth = 2;
        _cardScrollView.style.borderTopColor = ForgeStyledVisualElement.GetThemeColor("accent.primary");
    }

    private void ApplyMapPreviewTheme()
    {
        _mapPreviewContainer.style.backgroundColor = ForgeStyledVisualElement.GetThemeColor("bg.secondary");
        var borderColor = ForgeStyledVisualElement.GetThemeColor("border.normal");
        _mapPreviewContainer.style.borderTopColor = borderColor;
        _mapPreviewContainer.style.borderBottomColor = borderColor;
        _mapPreviewContainer.style.borderLeftColor = borderColor;
        _mapPreviewContainer.style.borderRightColor = borderColor;
    }

    // ── Layout helpers ──

    private static ForgeContainer CreateSection()
    {
        return ForgeContainer.Create()
            .FlexDirection(FlexDirection.Column)
            .AlignItems(Align.Stretch)
            .Build();
    }

    private static ForgeContainer CreateWrapRow()
    {
        return ForgeContainer.Create()
            .FlexDirection(FlexDirection.Row)
            .AlignItems(Align.FlexStart)
            .FlexWrap(Wrap.Wrap)
            .Build();
    }

    private static void AddSpaced(VisualElement parent, VisualElement child, float gap)
    {
        if (parent.childCount > 0)
        {
            child.style.marginTop = gap;
        }
        parent.Add(child);
    }

    private static void AddRowChild(VisualElement parent, VisualElement child, float gap)
    {
        if (parent.childCount > 0)
        {
            child.style.marginLeft = gap;
        }
        parent.Add(child);
    }

    private static void AddWrapChild(VisualElement parent, VisualElement child, float mainGap, float crossGap)
    {
        child.style.marginRight = mainGap;
        child.style.marginBottom = crossGap;
        parent.Add(child);
    }
}
}
