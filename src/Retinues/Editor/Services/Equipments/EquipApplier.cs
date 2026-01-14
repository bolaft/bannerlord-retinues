using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Game;

namespace Retinues.Editor.Services.Equipments
{
    public static class EquipApplier
    {
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
