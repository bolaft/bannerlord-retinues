using System.Reflection;
using HarmonyLib;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using System;
using System.Collections.Generic;

#if BL13
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
#endif

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Base class for character graph and identity queries.
    /// </summary>
    public class CharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Shared Utilities                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deep-copies key data from src to tgt and calls FillFrom.
        /// BL13 path also installs a fresh battle equipment roster cloned from src.
        /// </summary>
        public static CharacterObject CopyInto(CharacterObject src, CharacterObject tgt)
        {
#if BL13
            if (src == null || tgt == null)
                return tgt;

            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (PropertyOwner<TraitObject>)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(tgt, new PropertyOwner<TraitObject>(traitsSrc));

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            M_fillFrom.Invoke(tgt, new object[] { src });

            InstallFreshRosterFromSourceBattleSets(src, tgt);
            return tgt;
#else
            if (src == null || tgt == null)
                return tgt;

            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (CharacterTraits)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(
                tgt,
                traitsSrc != null ? new CharacterTraits(traitsSrc) : null
            );

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            M_fillFrom.Invoke(tgt, [src]);
            return tgt;
#endif
        }

#if BL13

        /// <summary>
        /// Installs a fresh equipment roster on tgt, deep-cloned from src's battle sets.
        /// </summary>
        protected static void InstallFreshRosterFromSourceBattleSets(
            CharacterObject src,
            CharacterObject tgt
        )
        {
            try
            {
                var srcEquipments = src.BattleEquipments?.ToList() ?? new List<Equipment>();
                if (srcEquipments.Count == 0)
                    srcEquipments.Add(new Equipment(Equipment.EquipmentType.Battle));

                var cloned = new List<Equipment>(srcEquipments.Count);
                foreach (var e in srcEquipments)
                {
                    var code = e != null ? e.CalculateEquipmentCode() : null;
                    var ne =
                        (code != null)
                            ? Equipment.CreateFromEquipmentCode(code)
                            : new Equipment(Equipment.EquipmentType.Battle);
                    try
                    {
                        AccessTools
                            .Field(typeof(Equipment), "_equipmentType")
                            ?.SetValue(ne, Equipment.EquipmentType.Battle);
                    }
                    catch { }
                    cloned.Add(ne);
                }

                var newRoster = (MBEquipmentRoster)
                    Activator.CreateInstance(typeof(MBEquipmentRoster), nonPublic: true);

                if (F_roster_equipments != null)
                    F_roster_equipments.SetValue(newRoster, new MBList<Equipment>(cloned));
                else
                    AccessTools
                        .Property(typeof(MBEquipmentRoster), "AllEquipments")
                        ?.SetValue(newRoster, new MBReadOnlyList<Equipment>(cloned), null);

                if (F_roster_default != null)
                    F_roster_default.SetValue(newRoster, cloned[0]);

                F_equipmentRoster.SetValue(tgt, newRoster);
            }
            catch
            {
                try
                {
                    F_equipmentRoster.SetValue(
                        tgt,
                        (MBEquipmentRoster)
                            Activator.CreateInstance(typeof(MBEquipmentRoster), nonPublic: true)
                    );
                }
                catch { }
            }
        }
#endif
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Culture Cache                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class CultureCache
        {
            public string CultureId;

            public CharacterObject BasicRoot;
            public CharacterObject EliteRoot;

            public readonly HashSet<string> BasicSet = new(StringComparer.Ordinal);
            public readonly HashSet<string> EliteSet = new(StringComparer.Ordinal);

            public readonly Dictionary<string, string> ParentMap = new(StringComparer.Ordinal);
        }

        private static readonly Dictionary<string, CultureCache> _cache = new(
            StringComparer.Ordinal
        );

        private static CultureCache GetOrBuildCache(CharacterObject sample)
        {
            var culture = sample?.Culture;
            if (culture == null)
                return null;

            var cid = culture.StringId;
            if (string.IsNullOrEmpty(cid))
                return null;

            if (_cache.TryGetValue(cid, out var c))
                return c;

            c = new CultureCache
            {
                CultureId = cid,
                BasicRoot = culture.BasicTroop,
                EliteRoot = culture.EliteBasicTroop,
            };

            void Crawl(CharacterObject root, HashSet<string> set)
            {
                if (root == null)
                    return;

                var visited = new HashSet<string>(StringComparer.Ordinal) { root.StringId };
                var q = new Queue<CharacterObject>();
                q.Enqueue(root);
                set.Add(root.StringId);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    var kids = cur.UpgradeTargets ?? [];

                    foreach (var co in kids)
                    {
                        if (co?.Culture != root.Culture)
                            continue;
                        if (!visited.Add(co.StringId))
                            continue;

                        set.Add(co.StringId);
                        c.ParentMap[co.StringId] = cur.StringId;
                        q.Enqueue(co);
                    }
                }
            }

            Crawl(c.BasicRoot, c.BasicSet);
            Crawl(c.EliteRoot, c.EliteSet);

            _cache[cid] = c;
            return c;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Vanilla Inferences                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter GetParent(WCharacter node)
        {
            if (node?.Base == null)
                return null;

            var c = GetOrBuildCache(node.Base);
            if (c == null)
                return null;

            var pid = c.ParentMap.TryGetValue(node.StringId, out var tmpPid) ? tmpPid : null;

            if (string.IsNullOrEmpty(pid))
                return null;

            var pco = GetCharacterObject(pid);

            return pco != null ? new WCharacter(pco) : null;
        }

        public static WCharacter.TroopType GetType(WCharacter node)
        {
            if (node?.Culture?.Base == null)
                return WCharacter.TroopType.Other;

            // Character object
            var co = node.Base;

            var c = GetOrBuildCache(co);

            if (c.EliteSet.Contains(co.StringId))
                return WCharacter.TroopType.Elite;
            if (c.BasicSet.Contains(co.StringId))
                return WCharacter.TroopType.Basic;

            // Helper method
            bool IsCultureRef(Func<CultureObject, CharacterObject> selector)
            {
                var target = selector(node.Culture.Base);
                return ReferenceEquals(co, target);
            }

            // Match known types
            return node switch
            {
                var _ when IsCultureRef(cul => cul.MeleeMilitiaTroop) => WCharacter.TroopType.MilitiaMelee,
                var _ when IsCultureRef(cul => cul.MeleeEliteMilitiaTroop) => WCharacter.TroopType.MilitiaMeleeElite,
                var _ when IsCultureRef(cul => cul.RangedMilitiaTroop) => WCharacter.TroopType.MilitiaRanged,
                var _ when IsCultureRef(cul => cul.RangedEliteMilitiaTroop) => WCharacter.TroopType.MilitiaRangedElite,
                var _ when IsCultureRef(cul => cul.CaravanGuard) => WCharacter.TroopType.CaravanGuard,
                var _ when IsCultureRef(cul => cul.CaravanMaster) => WCharacter.TroopType.CaravanMaster,
                var _ when IsCultureRef(cul => cul.Villager) => WCharacter.TroopType.Villager,
                _ => WCharacter.TroopType.Other,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets a CharacterObject by vanilla troop ID.
        /// </summary>
        private static CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Reflection Handles                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static readonly FieldInfo F_originCharacter = AccessTools.Field(
            typeof(CharacterObject),
            "_originCharacter"
        );
        protected static readonly FieldInfo F_occupation = AccessTools.Field(
            typeof(CharacterObject),
            "_occupation"
        );
        protected static readonly FieldInfo F_persona = AccessTools.Field(
            typeof(CharacterObject),
            "_persona"
        );
        protected static readonly FieldInfo F_characterTraits = AccessTools.Field(
            typeof(CharacterObject),
            "_characterTraits"
        );
        protected static readonly FieldInfo F_civilianEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_civilianEquipmentTemplate"
        );
        protected static readonly FieldInfo F_battleEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_battleEquipmentTemplate"
        );
        protected static readonly FieldInfo F_equipmentRoster = AccessTools.Field(
            typeof(BasicCharacterObject),
            "_equipmentRoster"
        );
        protected static readonly FieldInfo F_roster_equipments = AccessTools.Field(
            typeof(MBEquipmentRoster),
            "_equipments"
        );
        protected static readonly FieldInfo F_roster_default = AccessTools.Field(
            typeof(MBEquipmentRoster),
            "_defaultEquipment"
        );
        protected static readonly MethodInfo M_fillFrom = AccessTools.Method(
            typeof(CharacterObject),
            "FillFrom",
            [typeof(CharacterObject)]
        );
    }
}
