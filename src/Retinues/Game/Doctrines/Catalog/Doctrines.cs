using System.Collections.Generic;
using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalog
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class Doctrines
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Registration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterAll()
        {
            RegisterSpoils();
            RegisterEquipments();
            RegisterTroops();
            RegisterTraining();
            RegisterRetinues();
        }

        private static void RegisterDoctrine(
            string id,
            string categoryId,
            int indexInCategory,
            string nameId,
            string nameFallback,
            string descId,
            string descFallback,
            IReadOnlyList<DoctrineFeatLink> feats
        )
        {
            DoctrinesCatalog.RegisterDoctrine(
                new DoctrineDefinition(
                    id: id,
                    categoryId: categoryId,
                    indexInCategory: indexInCategory,
                    name: L.T(nameId, nameFallback),
                    description: L.T(descId, descFallback),
                    goldCost: GoldCost(indexInCategory),
                    influenceCost: InfluenceCost(indexInCategory),
                    feats: feats ?? []
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int GoldCost(int indexInCategory)
        {
            return indexInCategory switch
            {
                0 => 1000,
                1 => 5000,
                2 => 25000,
                3 => 100000,
                _ => 100000,
            };
        }

        private static int InfluenceCost(int indexInCategory)
        {
            return indexInCategory switch
            {
                0 => 50,
                1 => 100,
                2 => 200,
                3 => 500,
                _ => 500,
            };
        }
    }
}
