using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Retinues
{
    public partial class RetinuesBehavior
    {
        protected override void RegisterCustomEvents()
        {
            ConfigurationManager.OptionChanged -= OnVisibilityOptionChanged;
            ConfigurationManager.OptionChanged += OnVisibilityOptionChanged;

            // Sync all encyclopedia visibility on every load unconditionally (bypasses the
            // IsActive / SafeInvoke gate), so troops are correctly shown/hidden even when
            // EnableRetinues is off or ClanTroopsUnlock / KingdomTroopsUnlock is Disabled.
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(
                this,
                _ =>
                {
                    SyncPlayerRetinueEncyclopediaVisibility();
                    SyncClanTroopsEncyclopediaVisibility();
                    SyncKingdomTroopsEncyclopediaVisibility();
                }
            );
        }

        /// <summary>
        /// Reacts to option changes that affect encyclopedia visibility of player-owned troops and troops.
        /// </summary>
        private void OnVisibilityOptionChanged(string key, object value)
        {
            if (key == nameof(Configuration.EnableRetinues))
            {
                SyncPlayerRetinueEncyclopediaVisibility();

                // If retinues are enabled mid-session, ensure the player has default
                // retinue troops so the map bar Troops button becomes available immediately.
                if (Configuration.EnableRetinues)
                {
                    EnsureDefaultRetinueForPlayerClan();
                    EnsureDefaultRetinueForPlayerKingdom();
                }
            }
            else if (key == nameof(Configuration.ClanTroopsUnlock))
            {
                SyncClanTroopsEncyclopediaVisibility();
            }
            else if (key == nameof(Configuration.KingdomTroopsUnlock))
            {
                SyncKingdomTroopsEncyclopediaVisibility();
            }
        }

        /// <summary>
        /// Shows or hides the player clan's retinues and, if the player is a ruler,
        /// the kingdom's retinues according to the master <see cref="Configuration.EnableRetinues"/> toggle.
        /// </summary>
        internal void SyncPlayerRetinueEncyclopediaVisibility()
        {
            bool shouldShow = Configuration.EnableRetinues;

            var clan = Player.Clan;
            if (clan != null)
            {
                foreach (var retinue in clan.GetRawRetinues())
                {
                    if (retinue?.Base == null)
                        continue;

                    retinue.HiddenInEncyclopedia = !shouldShow;
                }
            }

            var kingdom = Player.Kingdom;
            if (kingdom != null)
            {
                foreach (var retinue in kingdom.GetRawRetinues())
                {
                    if (retinue?.Base == null)
                        continue;

                    retinue.HiddenInEncyclopedia = !shouldShow;
                }
            }
        }

        /// <summary>
        /// Shows or hides the player clan's custom troop trees according to
        /// <see cref="Configuration.ClanTroopsUnlock"/>. Hidden when the option is <c>Disabled</c>.
        /// </summary>
        internal void SyncClanTroopsEncyclopediaVisibility()
        {
            bool shouldShow =
                Configuration.ClanTroopsUnlock != Configuration.TroopsUnlockMode.Disabled;

            var clan = Player.Clan;
            if (clan == null)
                return;

            foreach (var troop in clan.RosterBasic)
            {
                if (troop?.Base == null)
                    continue;

                troop.HiddenInEncyclopedia = !shouldShow;
            }

            foreach (var troop in clan.RosterElite)
            {
                if (troop?.Base == null)
                    continue;

                troop.HiddenInEncyclopedia = !shouldShow;
            }
        }

        /// <summary>
        /// Shows or hides the player kingdom's custom troop trees according to
        /// <see cref="Configuration.KingdomTroopsUnlock"/>. Hidden when the option is <c>Disabled</c>.
        /// </summary>
        internal void SyncKingdomTroopsEncyclopediaVisibility()
        {
            bool shouldShow =
                Configuration.KingdomTroopsUnlock != Configuration.TroopsUnlockMode.Disabled;

            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return;

            foreach (var troop in kingdom.RosterBasic)
            {
                if (troop?.Base == null)
                    continue;

                troop.HiddenInEncyclopedia = !shouldShow;
            }

            foreach (var troop in kingdom.RosterElite)
            {
                if (troop?.Base == null)
                    continue;

                troop.HiddenInEncyclopedia = !shouldShow;
            }
        }
    }
}
