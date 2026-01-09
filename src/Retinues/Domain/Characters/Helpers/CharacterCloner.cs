using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Characters.Helpers
{
    [SafeClass]
    public static class CharacterCloner
    {
        /// <summary>
        /// Fired when CharacterCloner unlocks items as part of troop creation.
        /// Listener is in Game layer (UnlockNotifierBehavior).
        /// </summary>
        public static event Action<IReadOnlyList<WItem>> ItemsUnlockedByCloner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter CloneVanilla(
            WCharacter template,
            bool skills = true,
            bool equipments = true,
            WCharacter intoStub = null,
            bool unlockItems = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null,
            RandomEquipmentHelper.RandomEquipmentReuseContext equipmentReuseContext = null
        )
        {
            if (template == null)
                return null;

            if (!template.IsVanilla)
                Log.Debug($"CloneVanilla called with non-vanilla troop '{template.StringId}'.");

            var clone = template.Clone(skills: skills, equipments: false, intoStub: intoStub);
            if (clone == null)
                return null;

            clone.UpgradeTargets = [];

            if (skills)
                EnforceSkillLimits(clone);

            if (!equipments)
            {
                clone.EquipmentRoster.Reset();
                return clone;
            }

            ApplyStarterEquipments(
                template: template,
                clone: clone,
                cultureContext: template.Culture,
                createCivilianSet: true,
                reuseContext: equipmentReuseContext
            );

            if (unlockItems)
            {
                var newly = UnlockAllItems(clone);
                CollectUnlocks(newly, notifyUnlocks, unlockSink);
            }

            return clone;
        }

        public static WCharacter CloneTreeFromRoot(
            WCharacter rootTemplate,
            bool skills = true,
            bool equipments = true,
            bool notifyUnlocks = true,
            List<WItem> unlockSink = null
        )
        {
            if (rootTemplate == null)
                return null;

            // IMPORTANT:
            // Do NOT use rootTemplate.Root here.
            // Root is computed from upgrade SOURCES, so Culture.Villager often becomes the component root.
            var root = rootTemplate;

            if (!root.IsVanilla)
                Log.Warn($"CloneTreeFromRoot expects vanilla root; got '{root.StringId}'.");

            // Tree is subtree: self + descendants only (follows UpgradeTargets only).
            var templates = root.Tree;

            if (templates == null || templates.Count == 0)
                templates = [root];

            var created = new List<WCharacter>(templates.Count);
            var map = new Dictionary<string, WCharacter>(templates.Count);

            // Minimize variety across the whole tree (only matters for RandomSet).
            RandomEquipmentHelper.RandomEquipmentReuseContext reuseContext = null;
            if (equipments && Settings.StarterEquipment.Value == Settings.EquipmentMode.RandomSet)
                reuseContext = new RandomEquipmentHelper.RandomEquipmentReuseContext();

            try
            {
                // 1) Clone every troop. IMPORTANT: do not unlock here.
                for (int i = 0; i < templates.Count; i++)
                {
                    var t = templates[i];
                    if (t?.Base == null)
                        continue;

                    var c =
                        CloneVanilla(
                            t,
                            skills: skills,
                            equipments: equipments,
                            intoStub: null,
                            unlockItems: false,
                            notifyUnlocks: false,
                            unlockSink: null,
                            equipmentReuseContext: reuseContext
                        )
                        ?? throw new InvalidOperationException(
                            "No free stub available for tree clone."
                        );

                    created.Add(c);
                    map[t.StringId] = c;
                }

                // 2) Re-wire upgrade targets to point to the cloned nodes
                for (int i = 0; i < templates.Count; i++)
                {
                    var src = templates[i];
                    if (src == null)
                        continue;

                    if (!map.TryGetValue(src.StringId, out var clonedSrc))
                        continue;

                    var targets = src.UpgradeTargets ?? [];
                    if (targets.Count == 0)
                    {
                        clonedSrc.UpgradeTargets = [];
                        continue;
                    }

                    var clonedTargets = new List<WCharacter>();

                    for (int j = 0; j < targets.Count; j++)
                    {
                        var tgt = targets[j];
                        if (tgt == null)
                            continue;

                        if (map.TryGetValue(tgt.StringId, out var clonedTgt))
                            clonedTargets.Add(clonedTgt);
                    }

                    clonedSrc.UpgradeTargets = clonedTargets;
                }

                // 3) Unlock items for the whole tree (single notification).
                if (equipments)
                {
                    var allNew = new List<WItem>(64);
                    var seen = new HashSet<string>(StringComparer.Ordinal);

                    for (int i = 0; i < created.Count; i++)
                    {
                        var c = created[i];
                        if (c?.Base == null)
                            continue;

                        var newly = UnlockAllItems(c);
                        if (newly == null || newly.Count == 0)
                            continue;

                        for (int k = 0; k < newly.Count; k++)
                        {
                            var it = newly[k];
                            var id = it?.StringId;
                            if (string.IsNullOrEmpty(id))
                                continue;

                            if (seen.Add(id))
                                allNew.Add(it);
                        }
                    }

                    CollectUnlocks(allNew, notifyUnlocks, unlockSink);
                }

                map.TryGetValue(root.StringId, out var clonedRoot);
                return clonedRoot;
            }
            catch (Exception ex)
            {
                Log.Error($"CloneTreeFromRoot failed: {ex}");

                CleanupCreated(created);
                return null;
            }
        }

        private static void CollectUnlocks(
            List<WItem> newlyUnlocked,
            bool notifyUnlocks,
            List<WItem> unlockSink
        )
        {
            if (newlyUnlocked == null || newlyUnlocked.Count == 0)
                return;

            if (unlockSink != null)
                unlockSink.AddRange(newlyUnlocked);

            if (!notifyUnlocks)
                return;

            ItemsUnlockedByCloner?.Invoke(newlyUnlocked);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Generic Builder Entry                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class TroopBuildRequest
        {
            public string Name { get; set; }
            public WCulture CultureContext { get; set; }
            public bool CopySkills { get; set; } = true;
            public bool CreateCivilianSet { get; set; } = true;

            public bool UnlockItems { get; set; } = true;

            /// <summary>
            /// When true, CharacterCloner will emit an unlock notification event if it unlocked new items.
            /// </summary>
            public bool NotifyUnlocks { get; set; } = true;

            public bool UnhideInEncyclopedia { get; set; } = true;
        }

        public static WCharacter BuildFromTemplate(WCharacter template, TroopBuildRequest req)
        {
            if (template?.Base == null)
                return null;

            req ??= new TroopBuildRequest();

            var troop = template.Clone(skills: req.CopySkills, equipments: false, intoStub: null);
            if (troop?.Base == null)
                return null;

            troop.UpgradeTargets = [];
            troop.Name = req.Name ?? string.Empty;

            if (req.CopySkills)
                EnforceSkillLimits(troop);

            ApplyStarterEquipments(
                template: template,
                clone: troop,
                cultureContext: req.CultureContext ?? template.Culture,
                createCivilianSet: req.CreateCivilianSet
            );

            if (req.UnlockItems)
            {
                var newly = UnlockAllItems(troop);
                CollectUnlocks(newly, req.NotifyUnlocks, unlockSink: null);
            }

            if (req.UnhideInEncyclopedia)
                troop.HiddenInEncyclopedia = false;

            return troop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Notifier                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static List<WItem> UnlockAllItems(WCharacter troop)
        {
            var list = new List<WItem>(16);

            foreach (WItem item in troop.EquipmentRoster.Items)
            {
                if (item == null)
                    continue;

                if (item.IsUnlocked)
                    continue;

                item.Unlock();
                list.Add(item);
            }

            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Equipment Strategy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyStarterEquipments(
            WCharacter template,
            WCharacter clone,
            WCulture cultureContext,
            bool createCivilianSet,
            RandomEquipmentHelper.RandomEquipmentReuseContext reuseContext = null
        )
        {
            if (template == null || clone == null)
                return;

            switch (Settings.StarterEquipment.Value)
            {
                case Settings.EquipmentMode.AllSets:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.All);
                    return;

                case Settings.EquipmentMode.SingleSet:
                    clone.EquipmentRoster.Copy(
                        template.EquipmentRoster,
                        EquipmentCopyMode.FirstOfEach
                    );
                    return;

                case Settings.EquipmentMode.EmptySet:
                    clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                    return;

                case Settings.EquipmentMode.RandomSet:
                default:
                    break;
            }

            var culture = cultureContext ?? template.Culture;

            // Pick the template's first battle/civilian sets as the "slot spec" source.
            MEquipment srcBattle = null;
            MEquipment srcCivil = null;

            var tplEquipments = template.EquipmentRoster?.Equipments;
            if (tplEquipments != null)
            {
                for (int i = 0; i < tplEquipments.Count; i++)
                {
                    var e = tplEquipments[i];
                    if (e == null)
                        continue;

                    if (e.IsCivilian)
                    {
                        if (srcCivil == null)
                            srcCivil = e;
                    }
                    else
                    {
                        if (srcBattle == null)
                            srcBattle = e;
                    }

                    if (srcBattle != null && srcCivil != null)
                        break;
                }
            }

            if (srcBattle == null && srcCivil != null)
                srcBattle = srcCivil;

            if (srcBattle == null)
            {
                Log.Warn(
                    $"ApplyStarterEquipments(RandomSet) has no source equipment for '{template.StringId}'. Resetting."
                );
                clone.EquipmentRoster.Copy(template.EquipmentRoster, EquipmentCopyMode.Reset);
                return;
            }

            if (srcCivil == null)
                srcCivil = srcBattle;

            var battle = RandomEquipmentHelper.CreateRandomEquipment(
                owner: clone,
                source: srcBattle,
                civilian: false,
                acceptableCultures: culture != null ? [culture] : null,
                acceptNeutralCulture: true,
                requireSkillForItem: true,
                itemFilter: null,
                fromStocks: false,
                pickBest: false,
                enforceLimits: true,
                reuseContext: reuseContext,
                preferUnlocked: true
            );

            MEquipment civil = null;
            if (createCivilianSet)
            {
                civil = RandomEquipmentHelper.CreateRandomEquipment(
                    owner: clone,
                    source: srcCivil,
                    civilian: true,
                    acceptableCultures: culture != null ? [culture] : null,
                    acceptNeutralCulture: true,
                    requireSkillForItem: true,
                    itemFilter: null,
                    fromStocks: false,
                    pickBest: false,
                    enforceLimits: true,
                    reuseContext: reuseContext,
                    preferUnlocked: true
                );
            }

            clone.EquipmentRoster.Equipments = createCivilianSet ? [battle, civil] : [battle];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly struct SkillValue(SkillObject skill, int value, float scaled, int floor)
        {
            public readonly SkillObject Skill = skill;
            public readonly int Value = value;
            public readonly float Scaled = scaled;
            public readonly int Floor = floor;
        }

        private static void EnforceSkillLimits(WCharacter wc)
        {
            if (wc == null || wc.Base == null)
                return;

            // Heroes are intentionally unrestricted in WCharacter.Skills.cs
            if (wc.IsHero)
                return;

            var cap = wc.SkillCapForTier;
            var totalMax = wc.SkillTotalMaxForTier;

            if (cap <= 0 || totalMax <= 0)
                return;

            var skills = SkillsHelper.GetSkillListForCharacter(
                isHeroLike: false,
                includeModded: true
            );
            if (skills == null || skills.Count == 0)
                return;

            // 1) Clamp each skill to cap (and >= 0)
            var values = new List<(SkillObject skill, int value)>(skills.Count);
            int total = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                if (s == null)
                    continue;

                int v = wc.Base.GetSkillValue(s);
                if (v < 0)
                    v = 0;
                if (v > cap)
                    v = cap;

                values.Add((s, v));
                total += v;
            }

            if (values.Count == 0)
                return;

            // If only per-skill cap mattered, apply and exit.
            if (total <= totalMax)
            {
                for (int i = 0; i < values.Count; i++)
                    wc.Skills.Set(values[i].skill, values[i].value);

                return;
            }

            // 2) Enforce total max by scaling down proportionally.
            // Use floor to guarantee we don't exceed totalMax, then distribute leftover.
            float factor = totalMax / (float)total;

            var scaled = new List<SkillValue>(values.Count);
            int sumFloor = 0;

            for (int i = 0; i < values.Count; i++)
            {
                var (skill, v) = values[i];

                float sv = v * factor;
                int fv = (int)Math.Floor(sv);

                if (fv < 0)
                    fv = 0;
                if (fv > cap)
                    fv = cap;

                scaled.Add(new SkillValue(skill, v, sv, fv));
                sumFloor += fv;
            }

            int remaining = totalMax - sumFloor;
            if (remaining < 0)
                remaining = 0;

            // Distribute remaining points to the biggest fractional parts first,
            // without exceeding per-skill cap.
            scaled.Sort(
                (a, b) =>
                {
                    float fa = a.Scaled - a.Floor;
                    float fb = b.Scaled - b.Floor;
                    return fb.CompareTo(fa);
                }
            );

            var final = new Dictionary<string, int>(scaled.Count, StringComparer.Ordinal);
            for (int i = 0; i < scaled.Count; i++)
                final[scaled[i].Skill.StringId] = scaled[i].Floor;

            for (int i = 0; i < scaled.Count && remaining > 0; i++)
            {
                var sv = scaled[i];
                var id = sv.Skill?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                int current = final[id];
                if (current >= cap)
                    continue;

                final[id] = current + 1;
                remaining--;
            }

            // Apply
            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                if (s == null || string.IsNullOrEmpty(s.StringId))
                    continue;

                if (final.TryGetValue(s.StringId, out var v))
                    wc.Skills.Set(s, v);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cleanup                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CleanupCreated(List<WCharacter> created)
        {
            if (created == null || created.Count == 0)
                return;

            for (int i = 0; i < created.Count; i++)
            {
                var c = created[i];
                if (c == null)
                    continue;

                try
                {
                    c.Remove();
                }
                catch { }
            }
        }
    }
}
