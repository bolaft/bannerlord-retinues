using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Buffers unlock notifications until the player is back on the main campaign map.
    /// </summary>
    public sealed class UnlockNotifierBehavior : BaseCampaignBehavior<UnlockNotifierBehavior>
    {
        private struct PendingNotification
        {
            public TextObject Title;
            public TextObject Description;
        }

        private static readonly List<PendingNotification> _pending = [];

        /// <summary>
        /// Shows the notification immediately if on world map, otherwise queues it.
        /// </summary>
        public static void Notify(TextObject title, TextObject description)
        {
            if (title == null || description == null)
                return;

            if (IsOnWorldMap())
            {
                Show(title, description);
                return;
            }

            _pending.Add(new PendingNotification { Title = title, Description = description });

            // Avoid unbounded growth in weird cases (loading loops, etc).
            if (_pending.Count > 32)
                _pending.RemoveAt(0);
        }

        protected override void RegisterCustomEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        private void OnTick(float dt)
        {
            if (_pending.Count == 0)
                return;

            if (!IsOnWorldMap())
                return;

            // Drain one per tick to avoid stacking popups in the same frame.
            var p = _pending[0];
            _pending.RemoveAt(0);

            Show(p.Title, p.Description);
        }

        private static void Show(TextObject title, TextObject description)
        {
            switch (Settings.ItemUnlockNotification.Value)
            {
                case Settings.ItemUnlockNotificationStyle.Popup:
                    Inquiries.Popup(title, description);
                    break;

                case Settings.ItemUnlockNotificationStyle.Message:
                    Notifications.Message($"{title}\n{description}");
                    break;

                default:
                    Inquiries.Popup(title, description);
                    break;
            }
        }

        /// <summary>
        /// Returns true if the player is currently on the world map state.
        /// </summary>
        private static bool IsOnWorldMap()
        {
            var game = TaleWorlds.Core.Game.Current;
            var gsm = game?.GameStateManager;
            if (gsm == null)
                return false;

            return gsm.ActiveState is MapState;
        }
    }
}
