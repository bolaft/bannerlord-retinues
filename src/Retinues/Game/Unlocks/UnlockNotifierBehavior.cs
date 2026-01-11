using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Game.Troops;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem;
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

        private static readonly List<PendingNotification> Pending = new(16);

        private static bool _clonerHooked;

        [StaticClearAction]
        public static void ClearStatic()
        {
            Pending.Clear();

            if (_clonerHooked)
            {
                TroopCloner.ItemsUnlockedByCloner -= OnClonerItemsUnlocked;
                _clonerHooked = false;
            }
        }

        protected override void RegisterCustomEvents()
        {
            // Flush queued notifications (batch) once we're back in a safe UI context.
            // We do NOT check MapState here; Inquiries handles delaying.
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);

            if (_clonerHooked)
                return;

            TroopCloner.ItemsUnlockedByCloner += OnClonerItemsUnlocked;
            _clonerHooked = true;
        }

        /// <summary>
        /// Queue notification. It will be flushed (batched) on the next campaign tick.
        /// </summary>
        public static void Notify(TextObject title, TextObject description)
        {
            if (title == null || description == null)
                return;

            Pending.Add(new PendingNotification { Title = title, Description = description });

            if (Pending.Count > 64)
                Pending.RemoveAt(0);
        }

        private static void OnClonerItemsUnlocked(IReadOnlyList<WItem> items)
        {
            // UnlockNotifier will call UnlockNotifierBehavior.Notify(...) internally in your setup.
            UnlockNotifier.ItemsUnlocked(UnlockNotifier.UnlockMethod.Troops, items);
        }

        private void OnTick(float dt)
        {
            if (Pending.Count == 0)
                return;

            Flush();
        }

        private static void Flush()
        {
            if (Pending.Count == 0)
                return;

            var count = Pending.Count;

            var title = count == 1 ? Pending[0].Title : L.T("unlock_notify_title_multi", "Unlocks");

            const int maxBlocks = 8;
            var take = Math.Min(maxBlocks, count);

            var blocks = new List<string>(take + 1);
            for (var i = 0; i < take; i++)
                blocks.Add(Pending[i].Description.ToString());

            if (count > take)
                blocks.Add($"... (+{count - take})");

            Pending.Clear();

            var description = new TextObject(string.Join("\n\n", blocks));

            Show(title, description);
        }

        private static void Show(TextObject title, TextObject description)
        {
            switch (Settings.ItemUnlockNotification.Value)
            {
                case Settings.NotificationStyle.Popup:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;

                case Settings.NotificationStyle.Message:
                    Notifications.Message($"{title}\n{description}");
                    break;

                default:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;
            }
        }
    }
}
