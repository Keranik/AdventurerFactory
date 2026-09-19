using ForgeFlow.Core.Events;
using ForgeFlow.Core.Systems;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Self-contained top-bar center component showing guild identity:
    /// Logo emoji (left) | Guild Name (center, prominent) | Banner emoji (right).
    /// Subscribes to <see cref="GuildCreatedEvent"/> to refresh when the guild changes.
    /// </summary>
    internal sealed class GuildIdentityPanel : ForgeStyledVisualElement
    {
        private readonly SimulationTicker _simulation;
        private readonly EventBus _eventBus;

        private readonly ForgeLabel _logoLabel;
        private readonly ForgeLabel _guildNameLabel;
        private readonly ForgeLabel _bannerLabel;

        public GuildIdentityPanel(SimulationTicker simulation, EventBus eventBus)
        {
            _simulation = simulation;
            _eventBus = eventBus;

            name = nameof(GuildIdentityPanel);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.flexShrink = 0;

            _logoLabel = ForgeLabel.CreateRaw("⚔", ForgeLabelSize.Large)
                .FontSize(22).MarginRight(10).Build();

            _guildNameLabel = ForgeLabel.CreateRaw("—", ForgeLabelSize.Large)
                .FontSize(20).Color("text.accent").Bold().Build();

            _bannerLabel = ForgeLabel.CreateRaw("🏴", ForgeLabelSize.Large)
                .FontSize(22).MarginLeft(10).Build();

            Add(_logoLabel);
            Add(_guildNameLabel);
            Add(_bannerLabel);

            _eventBus.Subscribe<GuildCreatedEvent>(OnGuildCreated);

            Refresh();
        }

        /// <summary>Refreshes guild name, logo, and banner from current simulation state.</summary>
        public void Refresh()
        {
            var guild = _simulation.Guild;
            if (guild == null)
            {
                _guildNameLabel.SetRawText("—");
                _logoLabel.SetRawText("⚔");
                _bannerLabel.SetRawText("🏴");
                return;
            }

            _guildNameLabel.SetRawText(guild.GuildName);
            _logoLabel.SetRawText(LogoIdToEmoji(guild.LogoId));
            _bannerLabel.SetRawText(BannerIdToEmoji(guild.BannerId));
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GuildCreatedEvent>(OnGuildCreated);
        }

        public override void ApplyTheme()
        {
            _logoLabel.ApplyTheme();
            _guildNameLabel.ApplyTheme();
            _bannerLabel.ApplyTheme();

            // Re-apply color override after theme reset
            _guildNameLabel.style.color = C("text.accent");
        }

        private void OnGuildCreated(GuildCreatedEvent e)
        {
            Refresh();
        }

        internal static string BannerIdToEmoji(string bannerId) => bannerId switch
        {
            "banner_lion" => "🦁",
            "banner_eagle" => "🦅",
            "banner_dragon" => "🐉",
            "banner_wolf" => "🐺",
            "banner_phoenix" => "🔥",
            "banner_serpent" => "🐍",
            "banner_stag" => "🦌",
            _ => "🏴"
        };

        internal static string LogoIdToEmoji(string logoId) => logoId switch
        {
            "logo_shield" => "🛡",
            "logo_crown" => "👑",
            "logo_hammer" => "🔨",
            "logo_star" => "⭐",
            "logo_anvil" => "⚒",
            "logo_tome" => "📖",
            "logo_chalice" => "🏆",
            _ => "⚔"
        };
    }
}
