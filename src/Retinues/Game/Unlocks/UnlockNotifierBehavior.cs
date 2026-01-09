using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;

namespace Retinues.Game.Unlocks
{
    [SafeClass]
    public sealed class UnlockNotifierBehavior : BaseCampaignBehavior
    {
        private struct PendingNotification
        {
            public TextObject Title;
            public TextObject Description;
        }

        private static readonly List<PendingNotification> Pending = new(8);

        /// <summary>
        /// Show immediately if on world map, otherwise queue until the player returns to the campaign map.
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

            Pending.Add(new PendingNotification { Title = title, Description = description });

            // Safety guard: keep queue bounded in edge cases.
            if (Pending.Count > 32)
                Pending.RemoveAt(0);
        }

        protected override void RegisterCustomEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        private void OnTick(float dt)
        {
            if (Pending.Count == 0)
                return;

            if (!IsOnWorldMap())
                return;

            Flush();
        }

        private static void Flush()
        {
            if (Pending.Count == 0)
                return;

            var count = Pending.Count;

            var title = count == 1 ? Pending[0].Title : L.T("unlock_notify_title_multi", "Unlocks");

            const int maxLines = 8;
            var take = Math.Min(maxLines, count);

            var lines = new List<string>(take + 1);
            for (var i = 0; i < take; i++)
                lines.Add(Pending[i].Description.ToString());

            if (count > take)
                lines.Add($"... (+{count - take})");

            Pending.Clear();

            // IMPORTANT: blank line between method paragraphs.
            var desc = new TextObject(string.Join("\n\n", lines));

            Show(title, desc);
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
