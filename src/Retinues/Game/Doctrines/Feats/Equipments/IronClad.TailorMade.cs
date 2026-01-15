using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Own a smithy for 30 days.
    /// </summary>
    public sealed class Feat_Ironclad_TailorMade : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IR_TailorMade.Id;

        protected override void OnDailyTick()
        {
            var smithy = FindOwnedSmithy();
            if (smithy == null)
                return; // No owned smithy.

            Feat.Add();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Find an owned smithy workshop.
        /// </summary>
        private static Workshop FindOwnedSmithy()
        {
            var owned = Player.Hero.Base.OwnedWorkshops;
            if (owned != null)
            {
                foreach (var w in owned)
                {
                    if (IsSmithy(w))
                        return w;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the workshop is a smithy.
        /// </summary>
        private static bool IsSmithy(Workshop w)
        {
            if (w == null)
                return false;

            var typeId = w.WorkshopType?.StringId ?? string.Empty;
            if (typeId.Length > 0 && typeId.ToLowerInvariant().Contains("smithy"))
                return true;

            var typeName = w.WorkshopType?.Name?.ToString() ?? string.Empty;
            if (typeName.Length > 0 && typeName.ToLowerInvariant().Contains("smith"))
                return true;

            return false;
        }
    }
}
