using TaleWorlds.Localization;

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
            TextObject name,
            TextObject description,
            int target,
            bool repeatable
        )
        {
            // Set target variable in description.
            description.SetTextVariable("TARGET", target);

            DoctrinesCatalog.RegisterFeat(
                new FeatDefinition(
                    id: id,
                    name: name,
                    description: description,
                    target: target,
                    repeatable: repeatable
                )
            );
        }
    }
}
