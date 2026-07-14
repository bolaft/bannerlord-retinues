using System;
using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Utilities;
using TaleWorlds.Library;
using static Retinues.Behaviors.Doctrines.Catalogs.DoctrineCatalog;

namespace Retinues.Behaviors.Doctrines
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
                            description: doctrineData.Description,
                            sprite: doctrineData.Sprite,
                            overridden: doctrineData.Overridden,
                            overriddenHint: doctrineData.OverriddenHint,
                            previewCharacterId: doctrineData.PreviewCharacterId,
                            previewCivilian: doctrineData.PreviewCivilian
                        );

                        _doctrines[doctrine.Id] = doctrine;

                        foreach (var featData in doctrineData.Feats)
                        {
                            var feat = doctrine.Add(
                                id: featData.Id,
                                name: featData.Name,
                                description: featData.Description,
                                target: featData.Target,
                                worth: featData.Worth,
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

        /// <summary>
        /// Resets the mutable runtime state (acquired flags + progress) of every registered doctrine
        /// and feat back to defaults.
        ///
        /// Doctrine and feat definitions are process-global singletons that hold their own acquired
        /// and progress state. That state is loaded from and saved to the campaign, but nothing
        /// clears it when a new campaign begins in the same session — so a previous character's
        /// unlocked doctrines (and, because feats only advance while their doctrine is in progress,
        /// their in-progress feats) leak into the next character. Restarting the game hid the bug by
        /// recreating the singletons from scratch. Call this on new-game start and before applying a
        /// loaded campaign's data so each character sees only its own doctrine state.
        /// </summary>
        public static void ResetRuntimeState()
        {
            EnsureRegistered();

            foreach (var doctrine in _doctrines.Values)
            {
                if (doctrine == null)
                    continue;

                doctrine.IsAcquired = false;
                doctrine.ForceSet(0);
            }

            foreach (var feat in _feats.Values)
                feat?.ForceSet(0);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrines_list", "retinues")]
        public static string DoctrinesList(List<string> args)
        {
            var doctrines = GetDoctrines();
            var lines = new List<string>(doctrines.Count + 8)
            {
                "Doctrines:",
                "  id | state | progress",
                "--------------------------------------------",
            };

            foreach (var doctrine in doctrines)
            {
                lines.Add(
                    $"  {doctrine.Id} | {doctrine.GetState()} | {doctrine.Progress}/{Doctrine.ProgressTarget}"
                );
            }

            return string.Join("\n", lines);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feats_list", "retinues")]
        public static string FeatsList(List<string> args)
        {
            var feats = GetFeats();

            var lines = new List<string>(feats.Count + 8)
            {
                "Feats:",
                "  id | repeatable | consumed | times | progress",
                "---------------------------------------------------------------",
            };

            foreach (var feat in feats)
            {
                lines.Add(
                    $"  {feat.Id} | {(feat.Repeatable ? "yes" : "no")} | {(feat.IsCompleted ? "yes" : "no")} | {feat.Progress}/{feat.Target}"
                );
            }

            return string.Join("\n", lines);
        }
    }
}
