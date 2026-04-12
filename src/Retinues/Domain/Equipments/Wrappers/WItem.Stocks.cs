using Retinues.Framework.Model.Attributes;
using Retinues.Interface.Services;

namespace Retinues.Domain.Equipments.Wrappers
{
    public partial class WItem
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Stocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> StockAttribute => Attribute<int>(initialValue: 0, name: "StockAttribute");

        public int Stock
        {
            get => StockAttribute.Get();
            set => StockAttribute.Set(System.Math.Max(value, 0));
        }

        /// <summary>
        /// Gets the stock for the player.
        /// </summary>
        public int GetStock() => Stock;

        /// <summary>
        /// Increases the stock by the given amount.
        /// </summary>
        public void IncreaseStock(int amount = 1)
        {
            if (amount <= 0)
                return;

            Notifications.Message(
                L.T("stocks_add", "Added {AMOUNT} {ITEM} to stocks.")
                    .SetTextVariable("AMOUNT", amount)
                    .SetTextVariable("ITEM", Name)
            );

            Stock += amount;
        }

        /// <summary>
        /// Decreases the stock by the given amount.
        /// </summary>
        public void DecreaseStock(int amount = 1)
        {
            if (amount <= 0)
                return;

            Notifications.Message(
                L.T("stocks_remove", "Removed {AMOUNT} {ITEM} from stocks.")
                    .SetTextVariable("AMOUNT", amount)
                    .SetTextVariable("ITEM", Name)
            );

            Stock = System.Math.Max(Stock - amount, 0);
        }
    }
}
