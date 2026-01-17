using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model.Attributes;
using Retinues.GUI.Services;

namespace Retinues.Domain.Equipments.Wrappers
{
    public partial class WItem
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Stocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Dictionary<string, int>> StockByHeroAttribute =>
            Attribute<Dictionary<string, int>>(initialValue: []);

        public int Stock
        {
            get => GetStock(Player.Hero);
            set => SetStock(Player.Hero, value);
        }

        /// <summary>
        /// Gets the stock for the player.
        /// </summary>
        public int GetStock() => GetStock(Player.Hero);

        /// <summary>
        /// Gets the stock for the given hero.
        /// </summary>
        public int GetStock(WHero hero)
        {
            var map = StockByHeroAttribute.Get() ?? [];

            if (map.TryGetValue(hero.StringId, out var value))
                return value;

            return 0;
        }

        /// <summary>
        /// Sets the stock to the given value.
        /// </summary>
        private void SetStock(WHero hero, int value)
        {
            value = System.Math.Max(value, 0);

            var current = StockByHeroAttribute.Get() ?? [];
            var map = new Dictionary<string, int>(current) { [hero.StringId] = value };
            StockByHeroAttribute.Set(map);
        }

        /// <summary>
        /// Increases the stock by the given amount for the player.
        /// </summary>
        public void IncreaseStock(int amount = 1) => IncreaseStock(Player.Hero, amount);

        /// <summary>
        /// Increases the stock by the given amount for the given hero.
        /// </summary>
        public void IncreaseStock(WHero hero, int amount = 1)
        {
            if (amount <= 0)
                return;

            Notifications.Message(
                L.T("stocks_add", "Added {AMOUNT} {ITEM} to stocks.")
                    .SetTextVariable("AMOUNT", amount)
                    .SetTextVariable("ITEM", Name)
            );

            SetStock(hero, GetStock(hero) + amount);
        }

        /// <summary>
        /// Decreases the stock by the given amount for the player.
        /// </summary>
        public void DecreaseStock(int amount = 1) => DecreaseStock(Player.Hero, amount);

        /// <summary>
        /// Decreases the stock by the given amount for the given hero.
        /// </summary>
        public void DecreaseStock(WHero hero, int amount = 1)
        {
            if (amount <= 0)
                return;

            Notifications.Message(
                L.T("stocks_remove", "Removed {AMOUNT} {ITEM} from stocks.")
                    .SetTextVariable("AMOUNT", amount)
                    .SetTextVariable("ITEM", Name)
            );

            SetStock(hero, System.Math.Max(GetStock(hero) - amount, 0));
        }
    }
}
