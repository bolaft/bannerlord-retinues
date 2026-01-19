using System;
using System.Collections.Generic;
using Retinues.Behaviors.Troops;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using TaleWorlds.Localization;

namespace Retinues.Behaviors.Unlocks
{
    [SafeClass]
    public sealed class ItemUnlockNotifierBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Settings.EquipmentNeedsUnlocking;

        /// <summary>
        /// Pending notification data.
        /// </summary>
        private struct PendingNotification
        {
            public TextObject Title;
            public TextObject Description;
        }

        /// <summary>
        /// Pending notifications to be flushed.
        /// </summary>
        private static readonly List<PendingNotification> Pending = new(16);

        private static bool _clonerHooked;

        [StaticClearAction]
        public static void ClearStatic()
        {
            Pending.Clear();

            if (_clonerHooked)
            {
                Cloner.ItemsUnlockedByCloner -= OnClonerItemsUnlocked;
                _clonerHooked = false;
            }
        }

        /// <summary>
        /// Register custom campaign events.
        /// </summary>
        protected override void RegisterCustomEvents()
        {
            // Flush queued notifications (batch) once we're back in a safe UI context.
            // We do NOT check MapState here; Inquiries handles delaying.
            if (_clonerHooked)
                return;

            Cloner.ItemsUnlockedByCloner += OnClonerItemsUnlocked;
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

        /// <summary>
        /// Handle unlocks from TroopCloner.
        /// </summary>
        private static void OnClonerItemsUnlocked(IReadOnlyList<WItem> items)
        {
            // UnlockNotifier will call UnlockNotifierBehavior.Notify(...) internally.
        }

        /// <summary>
        /// Campaign tick handler to flush pending notifications.
        /// </summary>
        protected override void OnTick()
        {
            if (Pending.Count == 0)
                return;

            Flush();
        }

        /// <summary>
        /// Flush pending notifications as a single batched notification.
        /// </summary>
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

        /// <summary>
        /// Show a notification with the given title and description.
        /// </summary>
        private static void Show(TextObject title, TextObject description)
        {
            switch (Settings.ItemUnlockNotification.Value)
            {
                case Settings.NotificationStyle.Message:
                    Notifications.Message($"{title}\n{description}");
                    break;

                default:
                    Inquiries.Popup(
                        title,
                        description,
                        delayUntilOnWorldMap: true,
                        sound: Sounds.TraitChange
                    );
                    break;
            }
        }
    }
}
