using System;
using System.Collections.Generic;
using Retinues.Utilities;
using static Retinues.Game.Doctrines.Catalogs.DoctrineCatalog;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Registers built-in doctrine categories and doctrines.
    /// </summary>
    public static partial class DoctrinesRegistry
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Registries                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Categories ━━━━━━ */

        private static readonly Dictionary<string, Category> _categories = [];

        public static IReadOnlyCollection<Category> GetCategories() => _categories.Values;

        public static Category GetCategory(string id) =>
            _categories.TryGetValue(id, out var category) ? category : null;

        /* ━━━━━━━ Doctrines ━━━━━━ */

        private static readonly Dictionary<string, Doctrine> _doctrines = [];

        public static IReadOnlyCollection<Doctrine> GetDoctrines() => _doctrines.Values;

        public static Doctrine GetDoctrine(string id) =>
            _doctrines.TryGetValue(id, out var doctrine) ? doctrine : null;

        /* ━━━━━━━━━ Feats ━━━━━━━━ */

        private static readonly Dictionary<string, Feat> _feats = [];

        public static IReadOnlyCollection<Feat> GetFeats() => _feats.Values;

        public static Feat GetFeat(string id) => _feats.TryGetValue(id, out var feat) ? feat : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Registration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool RegistrationComplete = false;

        /// <summary>
        /// Registers all built-in doctrine categories, doctrines and feats.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (RegistrationComplete)
                return;

            try
            {
                List<DoctrineCategoryData> categories =
                [
                    CategoryEquipments,
                    CategoryRetinues,
                    CategoryTroops,
                    CategoryTraining,
                    CategoryLoot,
                ];

                foreach (var categoryData in categories)
                {
                    var category = new Category(id: categoryData.Id, name: categoryData.Name);

                    _categories[category.Id] = category;

                    foreach (var doctrineData in categoryData.Doctrines)
                    {
                        var doctrine = category.Add(
                            id: doctrineData.Id,
                            name: doctrineData.Name,
                            description: doctrineData.Description
                        );

                        _doctrines[doctrine.Id] = doctrine;

                        foreach (var featData in doctrineData.Feats)
                        {
                            var feat = doctrine.Add(
                                id: featData.Id,
                                name: featData.Name,
                                description: featData.Description,
                                target: featData.Target,
                                worth: 1,
                                repeatable: featData.Repeatable
                            );

                            _feats[featData.Id] = feat;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "An error occurred while registering doctrines.");
            }

            RegistrationComplete = true;
        }
    }
}
