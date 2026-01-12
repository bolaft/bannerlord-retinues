using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Feat definitions and doctrine links for the Equipments category.
    /// </summary>
    public static partial class Feats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Registration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RegisterFeat(
            string id,
            string nameId,
            string nameFallback,
            string descId,
            string descFallback,
            int target,
            bool repeatable
        )
        {
            DoctrinesCatalog.RegisterFeat(
                new FeatDefinition(
                    id: id,
                    name: L.T(nameId, nameFallback),
                    description: L.T(descId, descFallback),
                    target: target,
                    repeatable: repeatable
                )
            );
        }
    }
}
