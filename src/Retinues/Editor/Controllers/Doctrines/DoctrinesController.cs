using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.Game.Doctrines;
using Retinues.UI.Services;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Doctrines
{
    /// <summary>
    /// Provides doctrine data for the editor and implements actions.
    /// </summary>
    public sealed class DoctrinesController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Types                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum DoctrineState
        {
            Locked,
            InProgress,
            Unlocked,
            Acquired,
        }

        public sealed class CategoryInfo
        {
            public string Id;
            public string Name;
            public string Description;

            public List<DoctrineInfo> Doctrines;
        }

        public sealed class DoctrineInfo
        {
            public string Id;

            public string CategoryId;
            public string CategoryName;

            public DoctrineState State;

            public string Name;
            public string Description;

            public TextObject DescriptionTextObject;

            public int Progress;
            public int Target;

            public int GoldCost;
            public int InfluenceCost;

            public FeatInfo[] Feats;
        }

        public sealed class FeatInfo
        {
            public string Id;
            public string Description;

            public int Progress;
            public int Target;

            public bool IsCompleted;
            public bool IsRequired;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cache                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int _cacheBurstId = -1;

        private static readonly List<CategoryInfo> _categories = [];
        private static readonly Dictionary<string, DoctrineInfo> _doctrinesById = new(
            StringComparer.Ordinal
        );

        private static void EnsureCache()
        {
            if (_cacheBurstId == EventManager.CurrentBurstId)
                return;

            _cacheBurstId = EventManager.CurrentBurstId;

            _categories.Clear();
            _doctrinesById.Clear();

            if (!Settings.EnableDoctrines)
                return;

            DoctrinesCatalog.EnsureBuilt();

            foreach (var catKvp in DoctrinesCatalog.Categories)
            {
                var cat = catKvp.Value;
                if (cat == null)
                    continue;

                var cinfo = new CategoryInfo
                {
                    Id = cat.Id,
                    Name = cat.Name?.ToString() ?? string.Empty,
                    Description = cat.Description?.ToString() ?? string.Empty,
                    Doctrines = [],
                };

                var ids = cat.DoctrineIds;
                if (ids != null)
                {
                    for (var i = 0; i < ids.Count; i++)
                    {
                        var did = ids[i];
                        if (string.IsNullOrEmpty(did))
                            continue;

                        if (!DoctrinesCatalog.TryGetDoctrine(did, out var def) || def == null)
                            continue;

                        var dinfo = BuildDoctrineInfo(def, cinfo);
                        if (dinfo == null)
                            continue;

                        cinfo.Doctrines.Add(dinfo);
                        _doctrinesById[dinfo.Id] = dinfo;
                    }
                }

                if (cinfo.Doctrines.Count > 0)
                    _categories.Add(cinfo);
            }

            // Stable sort by name for categories and doctrines (default sort behavior).
            _categories.Sort(
                (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase)
            );

            for (var i = 0; i < _categories.Count; i++)
            {
                var c = _categories[i];
                if (c?.Doctrines == null)
                    continue;

                c.Doctrines.Sort(
                    (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase)
                );
            }
        }

        private static DoctrineInfo BuildDoctrineInfo(DoctrineDefinition def, CategoryInfo category)
        {
            if (def == null)
                return null;

            var state = DoctrinesAPI.GetState(def.Id);

            return new DoctrineInfo
            {
                Id = def.Id,
                CategoryId = def.CategoryId,
                CategoryName = category?.Name ?? string.Empty,

                State = MapState(state),

                Name = def.Name?.ToString() ?? string.Empty,
                Description = def.Description?.ToString() ?? string.Empty,
                DescriptionTextObject = def.Description,

                Progress = DoctrinesAPI.GetProgress(def.Id),
                Target = def.ProgressTarget,

                GoldCost = def.GoldCost,
                InfluenceCost = def.InfluenceCost,

                Feats = BuildFeatsList(def),
            };
        }

        private static FeatInfo[] BuildFeatsList(DoctrineDefinition def)
        {
            if (def?.Feats == null || def.Feats.Count == 0)
                return [];

            var list = new List<FeatInfo>(def.Feats.Count);

            for (var i = 0; i < def.Feats.Count; i++)
            {
                var link = def.Feats[i];
                var fid = link.FeatId;

                if (string.IsNullOrEmpty(fid))
                    continue;

                // Definition
                DoctrinesCatalog.TryGetFeat(fid, out FeatDefinition featDef);

                var desc = featDef?.Description?.ToString() ?? string.Empty;

                // State
                var progress = FeatsAPI.GetProgress(fid);
                var target = featDef?.Target ?? 1;
                var completed = FeatsAPI.IsCompleted(fid);

                list.Add(
                    new FeatInfo
                    {
                        Id = fid,
                        Description = desc,
                        Progress = progress,
                        Target = target,
                        IsCompleted = completed,
                        IsRequired = link.Required,
                    }
                );
            }

            return [.. list];
        }

        private static DoctrineState MapState(Game.Doctrines.DoctrineState s)
        {
            return s switch
            {
                Game.Doctrines.DoctrineState.Locked => DoctrineState.Locked,
                Game.Doctrines.DoctrineState.InProgress => DoctrineState.InProgress,
                Game.Doctrines.DoctrineState.Unlocked => DoctrineState.Unlocked,
                Game.Doctrines.DoctrineState.Acquired => DoctrineState.Acquired,
                _ => DoctrineState.Locked,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         API                            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns all doctrine categories and their doctrines.
        /// </summary>
        public static IReadOnlyList<CategoryInfo> GetCategories()
        {
            EnsureCache();
            return _categories;
        }

        /// <summary>
        /// Returns doctrine info by id.
        /// </summary>
        public static DoctrineInfo GetDoctrineInfo(string doctrineId)
        {
            EnsureCache();

            if (string.IsNullOrEmpty(doctrineId))
                return null;

            return _doctrinesById.TryGetValue(doctrineId, out var info) ? info : null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Display Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string GetStateText(DoctrineState? state)
        {
            return state switch
            {
                DoctrineState.Locked => L.S("doctrine_state_locked", "Locked"),
                DoctrineState.InProgress => L.S("doctrine_state_in_progress", "In progress"),
                DoctrineState.Unlocked => L.S("doctrine_state_unlocked", "Unlocked"),
                DoctrineState.Acquired => L.S("doctrine_state_acquired", "Acquired"),
                _ => string.Empty,
            };
        }

        public static string GetProgressText(int progress, int target)
        {
            if (target <= 0)
                return string.Empty;

            return $"{Math.Max(0, progress)}/{Math.Max(1, target)}";
        }

        public static int GetProgressPercent(int progress, int target)
        {
            if (target <= 0)
                return 0;

            var p = Math.Max(0, progress);
            var t = Math.Max(1, target);

            var v = (int)Math.Round(p * 100.0 / t);
            if (v < 0)
                return 0;
            if (v > 100)
                return 100;

            return v;
        }

        public static string GetStateIconSprite(DoctrineState? state)
        {
            // Reuse vanilla sprites for now. Can be customized later.
            return state switch
            {
                DoctrineState.Locked => "StdAssets\\lock_closed",
                DoctrineState.InProgress => "StdAssets\\lock_closed",
                DoctrineState.Unlocked => "StdAssets\\lock_open",
                DoctrineState.Acquired => "StdAssets\\checkmark",
                _ => string.Empty,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Actions                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<string> Acquire { get; } =
            Action<string>("Acquire")
                .AddCondition(
                    doctrineId => !string.IsNullOrEmpty(doctrineId),
                    L.T("doctrine_select_reason", "Select a doctrine first.")
                )
                .AddCondition(
                    _ => Settings.EnableDoctrines,
                    L.T("doctrines_disabled", "Doctrines are disabled.")
                )
                .AddCondition(
                    doctrineId =>
                    {
                        var info = GetDoctrineInfo(doctrineId);
                        return info != null && info.State == DoctrineState.Unlocked;
                    },
                    L.T("doctrine_acquire_unavailable", "This doctrine is still locked.")
                )
                .ExecuteWith(doctrineId =>
                {
                    if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                    {
                        Notifications.Message(L.S("doctrine_not_found", "Doctrine not found."));
                        return;
                    }

                    if (!DoctrinesAPI.TryAcquire(doctrineId, out var error))
                    {
                        Notifications.Message(
                            error?.ToString()
                                ?? L.S("doctrine_acquire_failed", "Cannot acquire doctrine.")
                        );
                        return;
                    }

                    Notifications.Message(
                        L.T("doctrine_acquired_msg", "Acquired doctrine: {NAME}.")
                            .SetTextVariable("NAME", def.Name)
                    );
                })
                .Fire(UIEvent.Doctrine);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Utilities                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [StaticClearAction]
        public static void ClearStatic()
        {
            _cacheBurstId = -1;
            _categories.Clear();
            _doctrinesById.Clear();
        }
    }
}
