using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Central registry for doctrine categories, doctrines, and feats.
    /// </summary>
    [SafeClass]
    public static class DoctrinesCatalog
    {
        private static bool _built;

        private static readonly Dictionary<string, DoctrineCategoryDefinition> CategoriesById = new(
            StringComparer.Ordinal
        );

        private static readonly Dictionary<string, DoctrineDefinition> DoctrinesById = new(
            StringComparer.Ordinal
        );

        private static readonly Dictionary<string, FeatDefinition> FeatsById = new(
            StringComparer.Ordinal
        );

        // featId -> list of (doctrineId, worth)
        private static readonly Dictionary<
            string,
            List<(string DoctrineId, int Worth)>
        > DoctrineLinksByFeatId = new(StringComparer.Ordinal);

        [StaticClearAction]
        public static void ClearStatic()
        {
            _built = false;
            CategoriesById.Clear();
            DoctrinesById.Clear();
            FeatsById.Clear();
            DoctrineLinksByFeatId.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Build                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void EnsureBuilt()
        {
            if (_built)
                return;

            _built = true;

            RegisterDefaults();
            RebuildFeatIndex();
        }

        static void RegisterDefaults()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
            //                        Catalog                        //
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

            Catalog.Doctrines.RegisterAll();
        }

        private static void RebuildFeatIndex()
        {
            DoctrineLinksByFeatId.Clear();

            foreach (var kvp in DoctrinesById)
            {
                var d = kvp.Value;
                if (d == null || d.Feats == null)
                    continue;

                for (var i = 0; i < d.Feats.Count; i++)
                {
                    var link = d.Feats[i];
                    if (string.IsNullOrEmpty(link.FeatId))
                        continue;

                    if (!DoctrineLinksByFeatId.TryGetValue(link.FeatId, out var list))
                    {
                        list = [];
                        DoctrineLinksByFeatId[link.FeatId] = list;
                    }

                    list.Add((d.Id, link.Worth));
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Register                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RegisterCategory(DoctrineCategoryDefinition category)
        {
            if (category == null || string.IsNullOrEmpty(category.Id))
                return;

            CategoriesById[category.Id] = category;
        }

        public static void RegisterDoctrine(DoctrineDefinition doctrine)
        {
            if (doctrine == null || string.IsNullOrEmpty(doctrine.Id))
                return;

            DoctrinesById[doctrine.Id] = doctrine;
        }

        public static void RegisterFeat(FeatDefinition feat)
        {
            if (feat == null || string.IsNullOrEmpty(feat.Id))
                return;

            FeatsById[feat.Id] = feat;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Query                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyDictionary<string, DoctrineCategoryDefinition> Categories
        {
            get
            {
                EnsureBuilt();
                return CategoriesById;
            }
        }

        public static IReadOnlyDictionary<string, DoctrineDefinition> Doctrines
        {
            get
            {
                EnsureBuilt();
                return DoctrinesById;
            }
        }

        public static IReadOnlyDictionary<string, FeatDefinition> Feats
        {
            get
            {
                EnsureBuilt();
                return FeatsById;
            }
        }

        public static bool TryGetCategory(string id, out DoctrineCategoryDefinition category)
        {
            EnsureBuilt();
            return CategoriesById.TryGetValue(id ?? string.Empty, out category);
        }

        public static bool TryGetDoctrine(string id, out DoctrineDefinition doctrine)
        {
            EnsureBuilt();
            return DoctrinesById.TryGetValue(id ?? string.Empty, out doctrine);
        }

        public static bool TryGetFeat(string id, out FeatDefinition feat)
        {
            EnsureBuilt();
            return FeatsById.TryGetValue(id ?? string.Empty, out feat);
        }

        public static IReadOnlyList<(
            string DoctrineId,
            int Worth,
            bool Required
        )> GetDoctrineLinksForFeat(string featId)
        {
            EnsureBuilt();

            if (string.IsNullOrEmpty(featId))
                return [];

            return DoctrineLinksByFeatId.TryGetValue(featId, out var list)
                ? (IReadOnlyList<(string, int, bool)>)list
                : [];
        }
    }
}
