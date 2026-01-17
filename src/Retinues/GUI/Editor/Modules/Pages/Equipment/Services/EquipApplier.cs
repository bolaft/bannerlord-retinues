using Retinues.Domain;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.GUI.Editor.Modules.Pages.Equipment.Controllers;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    /// <summary>
    /// Service to apply equipment plans with economy handling.
    /// </summary>
    public static class EquipApplier
    {
        /// <summary>
        /// Applies the given equip plan within the provided context, handling economy if enabled.
        /// </summary>
        public static bool Apply(EquipContext ctx, EquipPlan plan, System.Action applyCore)
        {
            if (plan == null || applyCore == null)
                return false;

            if (!ctx.EconomyEnabled)
            {
                applyCore();
                return true;
            }

            if (Player.Gold < plan.TotalCost)
                return false;

            if (plan.TotalCost > 0 && !Player.TrySpendGold(plan.TotalCost))
                return false;

            foreach (var kv in plan.StockUseById)
            {
                var item = WItem.Get(kv.Key);
                item?.DecreaseStock(kv.Value);
            }

            ItemController.TrackRosterStock(applyCore);
            return true;
        }
    }
}
